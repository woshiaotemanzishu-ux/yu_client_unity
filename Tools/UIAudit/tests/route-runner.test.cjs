'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const crypto = require('node:crypto');
const { runRoute, waitForView } = require('../lib/route-runner.cjs');

test('wait-view timeout preserves the final three-source runtime diagnostic', async () => {
  const snapshot = {
    schema: 3,
    capturedAt: '2026-08-12T02:00:00.000Z',
    visibleViews: ['MainUITopView', 'RoleView'],
    loaded: { nodes: [] },
    managed: { nodes: [] },
    stage: { nodes: [], frameToken: 10 },
    sources: { loadedViews: 2, managedViews: 2, stageNodes: 1 },
  };
  await assert.rejects(
    () => waitForView({ snapshot: async () => snapshot }, 'EquipmentView', true, 1),
    error => error.code === 'WAIT_VIEW_TIMEOUT'
      && error.diagnostic.context.kind === 'wait-view-timeout'
      && error.diagnostic.context.visibleViews.includes('RoleView'),
  );
});

test('wait-view accepts a visible content node nested inside a shared BaseWindowSkin', async () => {
  const snapshot = {
    schema: 3,
    capturedAt: '2026-08-12T02:00:00.000Z',
    visibleViews: ['BaseWindowSkin'],
    loaded: { nodes: [] },
    nodes: [{
      schema: 'ui-audit.runtime-node.v3', source: 'managed-view', view: 'BaseWindowSkin',
      name: 'EquipmentView', path: 'BaseWindowSkin[0]/_gp_item_con[0]/EquipmentView[0]',
      visible: true, displayed: true, depth: 3,
    }],
    stage: { nodes: [], frameToken: 10 },
    sources: { loadedViews: 1, managedViews: 1, stageNodes: 1 },
  };
  const result = await waitForView({ snapshot: async () => snapshot }, 'EquipmentView', true, 1);
  assert.equal(result.visibleViews.includes('EquipmentView'), false);
  assert.equal(result.nodes[0].name, 'EquipmentView');
});

test('failed route readiness creates no run and starts no browser session', async () => {
  const repoRoot = path.resolve(__dirname, '..', '..', '..');
  const outputDir = path.join(repoRoot, 'output', `ui-audit-preflight-only-${crypto.randomUUID()}`);
  const routePath = path.join(os.tmpdir(), `ui-audit-route-${crypto.randomUUID()}.json`);
  const popupPolicy = path.join(__dirname, '..', 'policies', 'startup-popups.json');
  const protocolPolicy = path.join(__dirname, '..', 'policies', 'protocols.json');
  fs.writeFileSync(routePath, JSON.stringify({
    schema: 1,
    id: 'readiness-before-browser',
    engine: 'legacy-laya',
    url: 'http://127.0.0.1:8091/index.html',
    snapshotSource: __filename,
    popupPolicy,
    protocolPolicy,
    verifyAuthority: false,
    session: { itemUse: { mode: 'hard-stop' } },
    steps: [],
  }));
  let sessions = 0;
  try {
    await assert.rejects(() => runRoute({
      repoRoot,
      routePath,
      outputDir,
      edgeExecutable: process.execPath,
      sessionFactory: () => { sessions += 1; throw new Error('must not construct browser session'); },
      preflightOptions: {
        puppeteerPackage: __filename,
        inspectEndpoint: () => ({
          schema: 'ui-audit.server-endpoint-observation.v1', host: '127.0.0.1', port: 8091,
          listener: { up: false, identity: '', listeners: [] },
        }),
        probeRouteUrl: async url => ({ pass: false, ready: false, code: 'SERVER_NOT_RUNNING', category: 'server-lifecycle', url }),
      },
    }), /UI_AUDIT_PREFLIGHT_FAILED/);
    assert.equal(sessions, 0);
    assert.equal(fs.existsSync(outputDir), false);
  } finally {
    fs.unlinkSync(routePath);
  }
});
