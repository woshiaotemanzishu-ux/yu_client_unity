'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const {
  probePreviewProviderStatus,
  inspectResourceToolPreview,
  recoverPreviewProvider,
  applyProviderObservationToProbe,
  applyProviderObservationToRecovery,
} = require('../lib/resource-tool-preview.cjs');

const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'fixtures', 'resource-tool-preview-states.v1.json'), 'utf8'));
const provider = fixture.provider;

function endpoint(port, pid) {
  const listeners = pid == null ? [] : [{
    localAddress: '127.0.0.1', localPort: port, pid, state: 'Listen',
    process: {
      pid, name: 'python.exe', executablePath: 'C:/Python/python.exe',
      commandLine: 'E:/GitProject/yu_client/tools/yu-resource-tool/python/.venv/Scripts/python.exe E:/GitProject/yu_client/tools/yu-resource-tool/python/main.py --port=7074 --secret=<redacted>',
    },
  }];
  return {
    schema: 'ui-audit.server-endpoint-observation.v1', host: '127.0.0.1', port,
    listener: { up: listeners.length > 0, identity: listeners.map(value => value.pid).join(','), listeners },
  };
}

function providerStatus(data) {
  return { pass: true, code: 'RESOURCE_TOOL_STATUS_READY', data, request: { method: 'GET', elapsedMs: 1 } };
}

for (const state of fixture.cases) {
  test(`resource-tool preview fixture: ${state.id}`, async () => {
    const routeProbe = {
      ...state.route,
      endpoint: { before: endpoint(8091, state.previewPid), after: endpoint(8091, state.previewPid) },
      attempts: [{ attempt: 1, code: state.route.code, pass: state.route.pass }],
    };
    const result = await inspectResourceToolPreview({ port: 8091, previewProvider: provider }, routeProbe, { owned: false }, {
      inspectEndpoint: () => endpoint(7074, state.controlPid),
      probePreviewProviderStatus: async () => providerStatus(state.api),
    });
    assert.equal(result.code, state.expected);
    assert.equal(result.identity.controlPids[0], state.controlPid);
    assert.deepEqual(result.identity.previewPids, state.previewPid == null ? [] : [state.previewPid]);
    if (state.expected === 'RESOURCE_TOOL_PREVIEW_READY') {
      assert.equal(result.blocking, false);
      assert.equal(result.recovery.blocking, false);
    } else {
      assert.equal(result.blocking, true);
      assert.equal(result.recovery.automaticRecoverySupported, false);
      assert.equal(result.recovery.writeEndpointsAllowed, false);
    }
  });
}

test('provider status probe accepts only code=0 with an explicit running boolean', async () => {
  const ready = await probePreviewProviderStatus(provider, {
    requestJson: async url => ({
      response: { statusCode: 200 }, elapsedMs: 4,
      body: JSON.stringify({ code: 0, data: { running: true, port: 8091, url: 'http://127.0.0.1:8091/index.html' } }),
      url,
    }),
  });
  assert.equal(ready.code, 'RESOURCE_TOOL_STATUS_READY');
  assert.equal(ready.request.method, 'GET');
  const invalid = await probePreviewProviderStatus(provider, {
    requestJson: async () => ({ response: { statusCode: 200 }, elapsedMs: 1, body: JSON.stringify({ code: 0, data: { port: 8091 } }) }),
  });
  assert.equal(invalid.code, 'RESOURCE_TOOL_STATUS_INVALID_PAYLOAD');
});

test('CAS-capable stale provider exposes one exact recover request', async () => {
  const routeProbe = {
    pass: false, code: 'EXTERNAL_SERVER_UNRESPONSIVE', causeCode: 'SERVER_NOT_RUNNING',
    endpoint: { before: endpoint(8091, 11300), after: endpoint(8091, 11300) }, attempts: [],
  };
  const statusData = {
    running: false, state: 'stale', port: 8091, url: null,
    providerPid: 11300, controlPid: 11300, previewPid: 11300, generation: 7,
    threadAlive: false, socketBound: true, httpReady: false,
  };
  const observation = await inspectResourceToolPreview({ port: 8091, previewProvider: provider }, routeProbe, { owned: false }, {
    inspectEndpoint: () => endpoint(7074, 11300),
    probePreviewProviderStatus: async () => providerStatus(statusData),
  });
  assert.equal(observation.code, 'RESOURCE_TOOL_PREVIEW_STALE_STATE');
  assert.equal(observation.recovery.code, 'RESOURCE_TOOL_PREVIEW_PROVIDER_CAS_RECOVERY_AVAILABLE');
  assert.equal(observation.recovery.automaticRecoverySupported, true);
  assert.deepEqual(observation.recovery.request.body, {
    expectedControlPid: 11300, expectedPreviewPid: 11300, expectedGeneration: 7, port: 8091,
  });

  let received;
  const recovered = await recoverPreviewProvider({ port: 8091, previewProvider: provider }, observation, {
    recoverRequestJson: async (url, timeoutMs, requestOptions) => {
      received = { url, timeoutMs, requestOptions };
      return { response: { statusCode: 200 }, elapsedMs: 3, body: JSON.stringify({ code: 0, data: { running: true } }) };
    },
  });
  assert.equal(recovered.code, 'RESOURCE_TOOL_PREVIEW_RECOVERED');
  assert.equal(received.requestOptions.method, 'POST');
  assert.deepEqual(received.requestOptions.body, observation.recovery.request.body);
});

test('legacy stale provider without a verified token remains read-only blocked', async () => {
  const state = fixture.cases.find(value => value.id === 'same-pid-api-running-route-refused');
  const routeProbe = {
    ...state.route,
    endpoint: { before: endpoint(8091, state.previewPid), after: endpoint(8091, state.previewPid) }, attempts: [],
  };
  const observation = await inspectResourceToolPreview({ port: 8091, previewProvider: provider }, routeProbe, { owned: false }, {
    inspectEndpoint: () => endpoint(7074, state.controlPid),
    probePreviewProviderStatus: async () => providerStatus(state.api),
  });
  const recovered = await recoverPreviewProvider({ port: 8091, previewProvider: provider }, observation, {});
  assert.equal(recovered.code, 'RESOURCE_TOOL_PREVIEW_RECOVERY_NOT_ALLOWED');
  assert.equal(recovered.attempted, false);
});

test('stale provider elevates route failure and removes every write recovery action', async () => {
  const state = fixture.cases.find(value => value.id === 'same-pid-api-running-route-refused');
  const routeProbe = {
    ...state.route,
    endpoint: { before: endpoint(8091, state.previewPid), after: endpoint(8091, state.previewPid) }, attempts: [],
  };
  const observation = await inspectResourceToolPreview({ port: 8091, previewProvider: provider }, routeProbe, { owned: false }, {
    inspectEndpoint: () => endpoint(7074, state.controlPid),
    probePreviewProviderStatus: async () => providerStatus(state.api),
  });
  const effectiveProbe = applyProviderObservationToProbe(routeProbe, observation);
  const recovery = applyProviderObservationToRecovery({
    start: 'unsafe-start', stopOwned: 'unsafe-stop', runWithEnsure: 'unsafe-ensure', status: 'safe-status',
    automatic: { startAllowed: true, stopAllowed: true, retryAllowed: true },
  }, observation);
  assert.equal(effectiveProbe.code, 'RESOURCE_TOOL_PREVIEW_STALE_STATE');
  assert.equal(effectiveProbe.causeCode, 'EXTERNAL_SERVER_UNRESPONSIVE');
  assert.equal(recovery.start, null);
  assert.equal(recovery.stopOwned, null);
  assert.equal(recovery.runWithEnsure, null);
  assert.equal(recovery.previewProvider.code, 'RESOURCE_TOOL_PREVIEW_PROVIDER_CAS_REQUIRED');
  assert.equal(recovery.previewProvider.forbidden.some(value => value.includes('/api/preview/start')), true);
});

test('provider identity mismatch never treats an arbitrary 7074 process as the resource tool', async () => {
  const control = endpoint(7074, 11300);
  control.listener.listeners[0].process.commandLine = 'python unrelated.py --port=7074';
  const routeProbe = { pass: false, code: 'SERVER_NOT_RUNNING', endpoint: { before: endpoint(8091, null), after: endpoint(8091, null) }, attempts: [] };
  const result = await inspectResourceToolPreview({ port: 8091, previewProvider: provider }, routeProbe, { owned: false }, {
    inspectEndpoint: () => control,
    probePreviewProviderStatus: async () => { throw new Error('status must not be trusted for the wrong process'); },
  });
  assert.equal(result.code, 'RESOURCE_TOOL_CONTROL_IDENTITY_MISMATCH');
  assert.equal(result.blocking, true);
});

test('versioned sibling recovery contract records the verified provider implementation', () => {
  const contract = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'contracts', 'yu-resource-tool-preview-lifecycle.v1.json'), 'utf8'));
  assert.equal(contract.status, 'implemented-and-verified');
  assert.equal(contract.requiredRecoveryRequest.path, '/api/preview/recover');
  assert.deepEqual(Object.keys(contract.requiredRecoveryRequest.body), [
    'expectedControlPid', 'expectedPreviewPid', 'expectedGeneration', 'port',
  ]);
  for (const field of ['controlPid', 'previewPid', 'generation', 'threadAlive', 'socketBound', 'httpReady']) {
    assert.equal(contract.requiredStatusResponse.fields.includes(field), true, field);
  }
  assert.equal(contract.requiredAtomicSemantics.some(value => value.includes('never kill the occupant')), true);
  assert.equal(contract.forbidden.includes('os.kill'), true);
  assert.equal(contract.forbidden.includes('killing a port occupant'), true);
});
