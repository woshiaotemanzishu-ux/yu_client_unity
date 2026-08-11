'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('path');
const { loadPopupPolicy } = require('../lib/popup-policy.cjs');
const { loadRuntimeOverlayPolicy } = require('../lib/runtime-overlay.cjs');
const { loadProtocolPolicy } = require('../lib/protocol-probe.cjs');
const { validateRoute, runPreflight, assertPreflight } = require('../lib/preflight.cjs');

const popupPolicy = loadPopupPolicy(path.join(__dirname, '..', 'policies', 'startup-popups.json'));
const runtimeOverlayPolicy = loadRuntimeOverlayPolicy(path.join(__dirname, '..', 'policies', 'runtime-overlays.json'));
const protocolPolicy = loadProtocolPolicy(path.join(__dirname, '..', 'policies', 'protocols.json'));
const route = {
  schema: 1, id: 'fixture', engine: 'legacy-laya', url: 'http://127.0.0.1:8091/index.html',
  snapshotSource: 'Z:/missing/pageSnapshot.js',
  session: { accountEnv: 'FIXTURE_ACCOUNT', passwordEnv: 'FIXTURE_PASSWORD', itemUse: { mode: 'hard-stop' } }, steps: [], verifyAuthority: false,
};
const noListener = () => ({
  schema: 'ui-audit.server-endpoint-observation.v1', observedAt: '2026-08-11T09:00:00.000Z', elapsedMs: 0,
  host: '127.0.0.1', port: 8091, inspectError: null, listener: { up: false, identity: '', listeners: [] },
});
const externalListener = profile => {
  const port = Number(profile && profile.port || 8091);
  return ({
  schema: 'ui-audit.server-endpoint-observation.v1', observedAt: '2026-08-11T09:00:00.000Z', elapsedMs: 0,
  host: '127.0.0.1', port, inspectError: null,
  listener: { up: true, identity: '11300', listeners: [{ localAddress: '127.0.0.1', localPort: port, pid: 11300, state: 'Listen', process: { pid: 11300, name: 'python.exe', executablePath: 'C:/Python/python.exe', commandLine: 'python E:/GitProject/yu_client/tools/yu-resource-tool/python/main.py --port=7074' } }] },
  });
};
const previewProviderReady = async () => ({
  pass: true, code: 'RESOURCE_TOOL_STATUS_READY',
  data: { running: true, port: 8091, url: 'http://127.0.0.1:8091/index.html' },
  request: { method: 'GET', elapsedMs: 1 },
});

test('preflight reports all deterministic hard failures and assert rejects them', async () => {
  const repoRoot = path.resolve(__dirname, '..', '..', '..');
  const result = await runPreflight({
    repoRoot,
    routePath: path.join(__dirname, 'fixture-route.json'),
    route,
    outputDir: path.join(repoRoot, 'output', 'fixture-preflight'),
    popupPolicy,
    runtimeOverlayPolicy,
    protocolPolicy,
    nodeVersion: '20.0.0',
    edgeCandidates: ['Z:/missing/msedge.exe'],
    puppeteerPackage: 'Z:/missing/puppeteer/package.json',
    existsSync: () => false,
    env: {},
    inspectEndpoint: externalListener,
    probePreviewProviderStatus: previewProviderReady,
    probeRouteUrl: async url => ({ pass: true, ready: true, code: 'ROUTE_READY', url }),
  });
  assert.equal(result.pass, false);
  assert.deepEqual(result.checks.filter(check => !check.pass).map(check => check.id), [
    'node-version', 'headless-edge', 'puppeteer-runtime', 'snapshot-source', 'account-env', 'password-env',
  ]);
  assert.throws(() => assertPreflight(result), /UI_AUDIT_PREFLIGHT_FAILED/);
});

test('route URL readiness is a stable pre-browser hard gate', async () => {
  const repoRoot = path.resolve(__dirname, '..', '..', '..');
  const result = await runPreflight({
    repoRoot,
    routePath: path.join(__dirname, 'fixture-route.json'),
    route: { ...route, snapshotSource: __filename, session: { itemUse: { mode: 'hard-stop' } } },
    outputDir: path.join(repoRoot, 'output', 'fixture-route-url'),
    popupPolicy,
    runtimeOverlayPolicy,
    protocolPolicy,
    edgeExecutable: process.execPath,
    puppeteerPackage: __filename,
    inspectEndpoint: noListener,
    probeRouteUrl: async url => ({ pass: false, ready: false, code: 'SERVER_NOT_RUNNING', category: 'server-lifecycle', url }),
  });
  const check = result.checks.find(value => value.id === 'route-url-readiness');
  assert.equal(check.pass, false);
  assert.equal(check.detail.code, 'SERVER_NOT_RUNNING');
  assert.match(check.detail.recovery.start, /server start --profile legacy-h5-local/);
});

test('preflight elevates a same-process resource-tool false-running state above route timeout', async () => {
  const repoRoot = path.resolve(__dirname, '..', '..', '..');
  const result = await runPreflight({
    repoRoot,
    routePath: path.join(__dirname, 'fixture-route.json'),
    route: { ...route, snapshotSource: __filename, session: { itemUse: { mode: 'hard-stop' } } },
    outputDir: path.join(repoRoot, 'output', 'fixture-external-timeout'),
    popupPolicy,
    runtimeOverlayPolicy,
    protocolPolicy,
    edgeExecutable: process.execPath,
    puppeteerPackage: __filename,
    inspectEndpoint: externalListener,
    probePreviewProviderStatus: previewProviderReady,
    probeRouteUrl: async url => ({ pass: false, ready: false, code: 'ROUTE_URL_TIMEOUT', category: 'route-readiness', url, requests: [] }),
    sleep: async () => {},
    routeReadiness: { transientRetry: { maxAttempts: 2, backoffMs: [0] } },
  });
  const check = result.checks.find(value => value.id === 'route-url-readiness');
  assert.equal(check.pass, false);
  assert.equal(check.detail.code, 'RESOURCE_TOOL_PREVIEW_STALE_STATE');
  assert.equal(check.detail.causeCode, 'EXTERNAL_SERVER_UNRESPONSIVE');
  assert.equal(check.detail.transportCauseCode, 'ROUTE_URL_TIMEOUT');
  assert.equal(check.detail.ownership.owned, false);
  assert.equal(check.detail.recovery.userActionRequired, true);
  assert.equal(check.detail.recovery.start, null);
  assert.equal(check.detail.recovery.stopOwned, null);
  assert.equal(check.detail.recovery.previewProvider.code, 'RESOURCE_TOOL_PREVIEW_PROVIDER_CAS_REQUIRED');
});

test('preflight blocks a protected completed scope and accepts only explicit scoped evidence', async () => {
  const repoRoot = path.resolve(__dirname, '..', '..', '..');
  const completedScopePolicy = {
    schema: 1,
    entries: [{
      id: 'mainui.role.person', label: '角色-人物', status: 'completed', protected: true,
      observedAt: '2026-08-11T22:20:00+08:00', reason: 'user confirmed complete',
      routeIds: ['mainui.role.person'], routePrefixes: ['mainui.role.full-tabs.v3.person'],
    }],
  };
  const serverStatus = async () => ({
    code: 'ROUTE_READY', probe: { pass: true, ready: true, code: 'ROUTE_READY', url: route.url },
    ownership: { owned: false }, observation: null, recovery: null,
  });
  const baseOptions = {
    repoRoot,
    routePath: path.join(__dirname, 'person-route.json'),
    outputDir: path.join(repoRoot, 'output', 'fixture-completed-scope'),
    popupPolicy, runtimeOverlayPolicy, protocolPolicy, completedScopePolicy,
    edgeExecutable: process.execPath, puppeteerPackage: __filename, serverStatus,
  };
  const blocked = await runPreflight({
    ...baseOptions,
    route: { ...route, id: 'mainui.role.person', snapshotSource: __filename, session: { itemUse: { mode: 'hard-stop' } } },
  });
  const blockedCheck = blocked.checks.find(value => value.id === 'completed-scope-guard');
  assert.equal(blockedCheck.pass, false);
  assert.equal(blockedCheck.detail.code, 'COMPLETED_SCOPE_REOPEN_REQUIRED');

  const reopened = await runPreflight({
    ...baseOptions,
    route: {
      ...route, id: 'mainui.role.person', snapshotSource: __filename, session: { itemUse: { mode: 'hard-stop' } },
      scope: { reopen: [{
        scopeId: 'mainui.role.person', reason: 'new user runtime screenshot shows regression',
        source: 'user-runtime', observedAt: '2026-08-11T23:00:00+08:00', evidence: { reference: 'user-message:runtime-screenshot' },
      }] },
    },
  });
  const reopenedCheck = reopened.checks.find(value => value.id === 'completed-scope-guard');
  assert.equal(reopenedCheck.pass, true);
  assert.equal(reopenedCheck.detail.code, 'COMPLETED_SCOPE_REOPEN_ACCEPTED');
});

test('decorative expect fields and incomplete generic assertions are rejected before execution', () => {
  assert.throws(() => validateRoute({
    ...route,
    steps: [{ action: 'snapshot', expect: { visible: true } }],
  }), /expect-unsupported/);
  assert.throws(() => validateRoute({
    ...route,
    steps: [{ action: 'assert-scroll', beforeLabel: 'before' }],
  }), /steps\[0\]\.scroll/);
  assert.doesNotThrow(() => validateRoute({
    ...route,
    steps: [{ action: 'branch', condition: { kind: 'nodes', selector: { name: 'optional' }, exists: true }, then: [], else: [] }],
  }));
});
