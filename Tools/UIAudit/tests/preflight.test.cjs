'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('path');
const { loadPopupPolicy } = require('../lib/popup-policy.cjs');
const { loadProtocolPolicy } = require('../lib/protocol-probe.cjs');
const { runPreflight, assertPreflight } = require('../lib/preflight.cjs');

const popupPolicy = loadPopupPolicy(path.join(__dirname, '..', 'policies', 'startup-popups.json'));
const protocolPolicy = loadProtocolPolicy(path.join(__dirname, '..', 'policies', 'protocols.json'));
const route = {
  schema: 1, id: 'fixture', engine: 'legacy-laya', url: 'http://127.0.0.1:8091/index.html',
  snapshotSource: 'Z:/missing/pageSnapshot.js',
  session: { accountEnv: 'FIXTURE_ACCOUNT', passwordEnv: 'FIXTURE_PASSWORD' }, steps: [], verifyAuthority: false,
};

test('preflight reports all deterministic hard failures and assert rejects them', () => {
  const repoRoot = path.resolve(__dirname, '..', '..', '..');
  const result = runPreflight({
    repoRoot,
    routePath: path.join(__dirname, 'fixture-route.json'),
    route,
    outputDir: path.join(repoRoot, 'output', 'fixture-preflight'),
    popupPolicy,
    protocolPolicy,
    nodeVersion: '20.0.0',
    edgeCandidates: ['Z:/missing/msedge.exe'],
    puppeteerPackage: 'Z:/missing/puppeteer/package.json',
    existsSync: () => false,
    env: {},
  });
  assert.equal(result.pass, false);
  assert.deepEqual(result.checks.filter(check => !check.pass).map(check => check.id), [
    'node-version', 'headless-edge', 'puppeteer-runtime', 'snapshot-source', 'account-env', 'password-env',
  ]);
  assert.throws(() => assertPreflight(result), /UI_AUDIT_PREFLIGHT_FAILED/);
});
