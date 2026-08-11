'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const {
  runtimePaths, writeOwnerState, verifyProcessOwnership, redactCommandLine,
  serverRecovery, probeServerRoute, serverStatus, startServer, stopServer,
} = require('../lib/server-lifecycle.cjs');

function profile(readiness = {}) {
  return {
    schema: 1, id: 'legacy-h5-local', matchOrigins: ['http://127.0.0.1:8091'], host: '127.0.0.1', port: 8091,
    routePath: '/index.html', cwdFromRepo: '../yu_client/h5', staticRootFromRepo: '../yu_client/cdn',
    workerFromTool: 'server/legacy-h5-worker.cjs', readiness,
  };
}

function endpoint(pid = 11300, commandLine = 'python external.py --secret=value') {
  return {
    schema: 'ui-audit.server-endpoint-observation.v1', observedAt: '2026-08-11T09:00:00.000Z', elapsedMs: 1,
    host: '127.0.0.1', port: 8091, inspectError: null,
    listener: {
      up: true, identity: String(pid), listeners: [{
        localAddress: '127.0.0.1', localPort: 8091, pid, state: 'Listen',
        process: { pid, name: 'python.exe', executablePath: 'C:/Python/python.exe', commandLine, creationDate: '2026-08-11T08:00:00.000Z', ageMs: 3600000 },
      }],
    },
  };
}

function endpointAt(port, pid = 11300, commandLine = 'python external.py --secret=value') {
  const result = endpoint(pid, commandLine);
  result.port = Number(port);
  result.listener.listeners.forEach(value => { value.localPort = Number(port); });
  return result;
}

const noListener = {
  schema: 'ui-audit.server-endpoint-observation.v1', observedAt: '2026-08-11T09:00:00.000Z', elapsedMs: 1,
  host: '127.0.0.1', port: 8091, inspectError: null, listener: { up: false, identity: '', listeners: [] },
};

test('owned process identity requires PID, worker path and private owner token', () => {
  const owner = { pid: 123, worker: 'legacy-h5-worker.cjs', ownerToken: 'private-token' };
  assert.equal(verifyProcessOwnership(owner, { ProcessId: 123, CommandLine: 'node legacy-h5-worker.cjs --owner-token private-token' }), true);
  assert.equal(verifyProcessOwnership(owner, { ProcessId: 123, CommandLine: 'node another-server.cjs' }), false);
  assert.equal(verifyProcessOwnership(owner, { ProcessId: 999, CommandLine: 'node legacy-h5-worker.cjs --owner-token private-token' }), false);
});

test('public process command lines redact lifecycle and application secrets', () => {
  assert.equal(redactCommandLine('python app.py --secret=value --owner-token private --password=p'),
    'python app.py --secret=<redacted> --owner-token=<redacted> --password=<redacted>');
});

test('listener-stable timeout is retried once and can become ready', async () => {
  const results = [
    { pass: false, ready: false, code: 'ROUTE_URL_TIMEOUT', requests: [{ method: 'GET', elapsedMs: 50 }] },
    { pass: true, ready: true, code: 'ROUTE_READY', requests: [{ method: 'GET', statusCode: 200, elapsedMs: 2 }] },
  ];
  const result = await probeServerRoute({ ...profile({ transientRetry: { maxAttempts: 2, backoffMs: [10] } }), url: 'http://127.0.0.1:8091/index.html' },
    { owned: false }, {
      inspectEndpoint: () => endpoint(), probeRouteUrl: async () => results.shift(), sleep: async () => {},
    });
  assert.equal(result.code, 'ROUTE_READY');
  assert.equal(result.attempts.length, 2);
  assert.equal(result.attempts[0].retryEvidence.listenerIdentityStable, true);
  assert.equal(result.attempts[0].retryEvidence.retryScheduled, true);
});

test('external listener with repeated timeout hard-stops as unresponsive without unsafe recovery', async () => {
  let probes = 0;
  const result = await serverStatus({
    profile: profile({ transientRetry: { maxAttempts: 2, backoffMs: [0] } }),
    inspectEndpoint: () => endpoint(),
    probeRouteUrl: async () => { probes += 1; return { pass: false, ready: false, code: 'ROUTE_URL_TIMEOUT', requests: [] }; },
    sleep: async () => {},
  });
  assert.equal(probes, 2);
  assert.equal(result.code, 'EXTERNAL_SERVER_UNRESPONSIVE');
  assert.equal(result.probe.causeCode, 'ROUTE_URL_TIMEOUT');
  assert.equal(result.observation.endpoint.before.listener.listeners[0].process.commandLine.includes('value'), false);
  assert.equal(result.observation.externalState.identityStable, true);
  assert.equal(result.observation.externalState.repeatedFailure, true);
  assert.equal(result.observation.externalState.stale, null);
  assert.equal(result.recovery.automatic.startAllowed, false);
  assert.equal(result.recovery.automatic.stopAllowed, false);
  assert.equal(result.recovery.automatic.retryAllowed, false);
  assert.equal(result.recovery.automatic.retryPerformed, true);
  assert.equal(result.recovery.start, null);
  assert.equal(result.recovery.stopOwned, null);
  assert.equal(result.recovery.userActionRequired, true);
});

test('a ready external listener passes without granting process ownership', async () => {
  const result = await serverStatus({
    profile: profile(), inspectEndpoint: () => endpoint(),
    probeRouteUrl: async () => ({ pass: true, ready: true, code: 'ROUTE_READY', requests: [{ method: 'GET', statusCode: 200, elapsedMs: 1 }] }),
  });
  assert.equal(result.pass, true);
  assert.equal(result.code, 'ROUTE_READY');
  assert.equal(result.ownership.owned, false);
  assert.equal(result.recovery.userActionRequired, false);
  assert.equal(result.recovery.start, null);
  assert.equal(result.recovery.stopOwned, null);
  assert.deepEqual(result.recovery.safeActions, []);
});

test('external route content mismatch is not retried', async () => {
  let probes = 0;
  const result = await serverStatus({
    profile: profile({ transientRetry: { maxAttempts: 3, backoffMs: [0] } }), inspectEndpoint: () => endpoint(),
    probeRouteUrl: async () => { probes += 1; return { pass: false, ready: false, code: 'ROUTE_CONTENT_NOT_READY', requests: [] }; },
  });
  assert.equal(probes, 1);
  assert.equal(result.code, 'EXTERNAL_SERVER_ROUTE_MISMATCH');
  assert.equal(result.probe.classification.state, 'listener-up-route-mismatch');
});

test('connection refusal contradicting a stable listener is external unresponsive and is not retried', async () => {
  let probes = 0;
  const result = await serverStatus({
    profile: profile({ transientRetry: { maxAttempts: 3, backoffMs: [0] } }), inspectEndpoint: () => endpoint(),
    probeRouteUrl: async () => { probes += 1; return { pass: false, ready: false, code: 'SERVER_NOT_RUNNING', networkCode: 'ECONNREFUSED', requests: [] }; },
  });
  assert.equal(probes, 1);
  assert.equal(result.code, 'EXTERNAL_SERVER_UNRESPONSIVE');
  assert.equal(result.probe.classification.state, 'listener-up-connection-refused');
  assert.equal(result.probe.causeCode, 'SERVER_NOT_RUNNING');
});

test('listener PID change during a timeout is a hard stop with no retry', async () => {
  const observations = [endpoint(11300), endpoint(11400)];
  let probes = 0;
  const result = await serverStatus({
    profile: profile({ transientRetry: { maxAttempts: 3, backoffMs: [0] } }),
    inspectEndpoint: () => observations.shift() || endpoint(11400),
    probeRouteUrl: async () => { probes += 1; return { pass: false, ready: false, code: 'ROUTE_URL_TIMEOUT', requests: [] }; },
  });
  assert.equal(probes, 1);
  assert.equal(result.code, 'EXTERNAL_SERVER_STATE_CHANGED');
  assert.equal(result.recovery.automatic.retryPerformed, false);
});

test('start never spawns over an external listener even when HTTP is unresponsive', async () => {
  let spawned = false;
  const result = await startServer({
    profile: profile({ transientRetry: { maxAttempts: 1 } }), inspectEndpoint: () => endpoint(),
    probeRouteUrl: async () => ({ pass: false, ready: false, code: 'ROUTE_URL_TIMEOUT', requests: [] }),
    spawn: () => { spawned = true; return { pid: 999, unref() {} }; },
  });
  assert.equal(result.code, 'SERVER_PORT_OR_CONTENT_CONFLICT');
  assert.equal(spawned, false);
});

test('stale resource-tool preview blocks start without spawn or provider write calls', async () => {
  let spawned = false;
  let statusReads = 0;
  const resourceProfile = {
    ...profile({ transientRetry: { maxAttempts: 1 } }),
    previewProvider: {
      schema: 1, id: 'yu-resource-tool-preview', controlHost: '127.0.0.1', controlPort: 7074,
      statusPath: '/api/preview/status', startPath: '/api/preview/start', stopPath: '/api/preview/stop', recoveryPath: '/api/preview/recover',
      expectedProcessCommandIncludes: ['tools/yu-resource-tool/python/main.py', '--port=7074'],
      recoveryContractFromTool: 'contracts/yu-resource-tool-preview-lifecycle.v1.json',
    },
  };
  const resourceCommand = 'python E:/GitProject/yu_client/tools/yu-resource-tool/python/main.py --port=7074 --secret=value';
  const result = await startServer({
    profile: resourceProfile,
    inspectEndpoint: value => endpointAt(value.port, 11300, resourceCommand),
    probeRouteUrl: async () => ({ pass: false, ready: false, code: 'SERVER_NOT_RUNNING', networkCode: 'ECONNREFUSED', requests: [] }),
    probePreviewProviderStatus: async () => {
      statusReads += 1;
      return { pass: true, code: 'RESOURCE_TOOL_STATUS_READY', data: { running: true, port: 8091 }, request: { method: 'GET' } };
    },
    spawn: () => { spawned = true; return { pid: 999, unref() {} }; },
  });
  assert.equal(result.code, 'SERVER_PORT_OR_CONTENT_CONFLICT');
  assert.equal(result.probe.code, 'RESOURCE_TOOL_PREVIEW_STALE_STATE');
  assert.equal(result.recovery.previewProvider.code, 'RESOURCE_TOOL_PREVIEW_PROVIDER_CAS_REQUIRED');
  assert.equal(result.recovery.previewProvider.writeEndpointsAllowed, false);
  assert.equal(statusReads, 1);
  assert.equal(spawned, false);
});

test('start performs one verified CAS recovery for a capable stale provider', async () => {
  let recovered = false;
  let spawned = false;
  let recoveryCalls = 0;
  const resourceProfile = {
    ...profile({ transientRetry: { maxAttempts: 1 } }),
    previewProvider: {
      schema: 1, id: 'yu-resource-tool-preview', controlHost: '127.0.0.1', controlPort: 7074,
      statusPath: '/api/preview/status', startPath: '/api/preview/start', stopPath: '/api/preview/stop', recoveryPath: '/api/preview/recover',
      expectedProcessCommandIncludes: ['tools/yu-resource-tool/python/main.py', '--port=7074'],
      recoveryContractFromTool: 'contracts/yu-resource-tool-preview-lifecycle.v1.json',
    },
  };
  const resourceCommand = 'python E:/GitProject/yu_client/tools/yu-resource-tool/python/main.py --port=7074 --secret=value';
  const result = await startServer({
    profile: resourceProfile,
    inspectEndpoint: value => endpointAt(value.port, 11300, resourceCommand),
    probeRouteUrl: async () => recovered
      ? ({ pass: true, ready: true, code: 'ROUTE_READY', requests: [] })
      : ({ pass: false, ready: false, code: 'SERVER_NOT_RUNNING', networkCode: 'ECONNREFUSED', requests: [] }),
    probePreviewProviderStatus: async () => ({
      pass: true, code: 'RESOURCE_TOOL_STATUS_READY', request: { method: 'GET' },
      data: recovered
        ? { running: true, state: 'ready', port: 8091, providerPid: 11300, controlPid: 11300, previewPid: 11300, generation: 8, threadAlive: true, socketBound: true, httpReady: true }
        : { running: false, state: 'stale', port: 8091, providerPid: 11300, controlPid: 11300, previewPid: 11300, generation: 7, threadAlive: false, socketBound: true, httpReady: false },
    }),
    recoverRequestJson: async (_url, _timeoutMs, requestOptions) => {
      recoveryCalls += 1;
      assert.deepEqual(requestOptions.body, {
        expectedControlPid: 11300, expectedPreviewPid: 11300, expectedGeneration: 7, port: 8091,
      });
      recovered = true;
      return { response: { statusCode: 200 }, elapsedMs: 2, body: JSON.stringify({ code: 0, data: { running: true } }) };
    },
    spawn: () => { spawned = true; return { pid: 999, unref() {} }; },
  });
  assert.equal(result.pass, true);
  assert.equal(result.code, 'RESOURCE_TOOL_PREVIEW_RECOVERED');
  assert.equal(result.providerRecovered, true);
  assert.equal(recoveryCalls, 1);
  assert.equal(spawned, false);
});

test('no listener retains explicit safe start recovery', async () => {
  const result = await serverStatus({
    profile: profile(), inspectEndpoint: () => noListener,
    probeRouteUrl: async () => ({ pass: false, ready: false, code: 'SERVER_NOT_RUNNING', requests: [] }),
  });
  assert.equal(result.code, 'SERVER_NOT_RUNNING');
  assert.equal(result.recovery.automatic.startAllowed, true);
  assert.match(result.recovery.start, /server start --profile legacy-h5-local/);
});

test('an owned process never receives a second start action even while its listener is down', () => {
  const recovery = serverRecovery({ id: 'legacy-h5-local' }, { listenerUp: false, owned: true, ready: false });
  assert.equal(recovery.automatic.startAllowed, false);
  assert.equal(recovery.start, null);
  assert.equal(recovery.automatic.stopAllowed, true);
});

test('unknown listener inspection never grants automatic start', async () => {
  const unknown = { ...noListener, inspectError: 'INSPECTION_FAILED', listener: { up: null, listeners: [] } };
  const result = await serverStatus({
    profile: profile(), inspectEndpoint: () => unknown,
    probeRouteUrl: async () => ({ pass: false, ready: false, code: 'ROUTE_URL_TIMEOUT', requests: [] }),
  });
  assert.equal(result.recovery.automatic.startAllowed, false);
  assert.equal(result.recovery.start, null);
  assert.equal(result.recovery.userActionRequired, true);
});

test('stop refuses an unowned PID and never calls the killer', async () => {
  const directory = fs.mkdtempSync(path.join(os.tmpdir(), 'ui-audit-server-test-'));
  const paths = runtimePaths('legacy-h5-local', directory);
  const profile = {
    schema: 1, id: 'legacy-h5-local', matchOrigins: ['http://127.0.0.1:8091'], host: '127.0.0.1', port: 8091,
    cwdFromRepo: '../yu_client/h5', staticRootFromRepo: '../yu_client/cdn', workerFromTool: 'server/legacy-h5-worker.cjs',
  };
  writeOwnerState(paths.state, { pid: 123, worker: 'legacy-h5-worker.cjs', ownerToken: 'private-token' });
  let killed = false;
  const result = await stopServer({
    profile,
    runtimeDirectory: directory,
    inspectProcess: () => ({ ProcessId: 123, CommandLine: 'node unrelated.cjs' }),
    kill: () => { killed = true; },
  });
  assert.equal(result.code, 'SERVER_OWNER_MISMATCH');
  assert.equal(killed, false);
  assert.equal(fs.existsSync(paths.state), true);
});

test('stale owner state is reported but never deleted or treated as listener ownership', async () => {
  const directory = fs.mkdtempSync(path.join(os.tmpdir(), 'ui-audit-server-stale-test-'));
  const paths = runtimePaths('legacy-h5-local', directory);
  writeOwnerState(paths.state, { pid: 222, worker: 'legacy-h5-worker.cjs', ownerToken: 'private-token' });
  const result = await serverStatus({
    profile: profile(), runtimeDirectory: directory, inspectEndpoint: () => endpoint(11300),
    inspectProcess: () => ({ ProcessId: 222, Name: 'node.exe', CommandLine: 'node unrelated.cjs', ExecutablePath: 'C:/node.exe' }),
    probeRouteUrl: async () => ({ pass: true, ready: true, code: 'ROUTE_READY', requests: [] }),
  });
  assert.equal(result.ownership.owned, false);
  assert.equal(result.observation.ownerState.code, 'OWNER_IDENTITY_MISMATCH');
  assert.equal(result.observation.ownerState.stale, true);
  assert.equal(fs.existsSync(paths.state), true);
});
