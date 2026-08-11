'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const crypto = require('node:crypto');
const { runRoute } = require('../lib/route-runner.cjs');

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
        probeRouteUrl: async url => ({ pass: false, ready: false, code: 'SERVER_NOT_RUNNING', category: 'server-lifecycle', url }),
      },
    }), /UI_AUDIT_PREFLIGHT_FAILED/);
    assert.equal(sessions, 0);
    assert.equal(fs.existsSync(outputDir), false);
  } finally {
    fs.unlinkSync(routePath);
  }
});
