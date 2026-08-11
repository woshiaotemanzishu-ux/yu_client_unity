'use strict';

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { ROUTE_SCHEMA_VERSION } = require('./version.cjs');
const { validatePopupPolicy } = require('./popup-policy.cjs');
const { validateProtocolPolicy } = require('./protocol-probe.cjs');

const DEFAULT_EDGE_PATHS = [
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
  'C:/Program Files/Microsoft/Edge/Application/msedge.exe',
];

function isInside(parent, child) {
  const relative = path.relative(path.resolve(parent), path.resolve(child));
  return relative === '' || (!relative.startsWith('..') && !path.isAbsolute(relative));
}

function validateRoute(route) {
  const errors = [];
  if (!route || Number(route.schema) !== ROUTE_SCHEMA_VERSION) errors.push('schema');
  if (!route || typeof route.id !== 'string' || !route.id.trim()) errors.push('id');
  if (!route || route.engine !== 'legacy-laya') errors.push('engine');
  try { new URL(route && route.url); } catch (_) { errors.push('url'); }
  if (!route || typeof route.snapshotSource !== 'string' || !route.snapshotSource.trim()) errors.push('snapshotSource');
  if (!Array.isArray(route && route.steps)) errors.push('steps');
  for (const [index, step] of (route && route.steps || []).entries()) {
    if (!step || !['snapshot', 'click', 'drag', 'wait-view', 'set-viewport', 'protocol-read', 'assert-protocol'].includes(step.action)) {
      errors.push(`steps[${index}].action`);
    }
  }
  if (errors.length) throw new Error(`ROUTE_SCHEMA_INVALID: ${errors.join(',')}`);
  return route;
}

function findEdgeExecutable(existsSync = fs.existsSync, candidates = DEFAULT_EDGE_PATHS) {
  return candidates.find(candidate => existsSync(candidate)) || null;
}

function sha256File(filePath) {
  return crypto.createHash('sha256').update(fs.readFileSync(filePath)).digest('hex');
}

function verifyAuthority(policy, legacyRoot, existsSync = fs.existsSync) {
  const results = [];
  for (const [id, authority] of Object.entries(policy && policy.authority || {})) {
    const target = path.resolve(legacyRoot, authority.pathFromLegacyRoot || '');
    const exists = existsSync(target);
    const actual = exists ? sha256File(target) : null;
    results.push({ id, target, exists, expectedSha256: authority.sha256 || null, actualSha256: actual, pass: exists && (!authority.sha256 || actual === authority.sha256) });
  }
  for (const entry of policy && policy.entries || []) {
    if (!entry.source || !entry.source.pathFromLegacyRoot) continue;
    const target = path.resolve(legacyRoot, entry.source.pathFromLegacyRoot);
    const exists = existsSync(target);
    const actual = exists ? sha256File(target) : null;
    results.push({
      id: `entry:${entry.view}`,
      target,
      exists,
      expectedSha256: entry.source.sha256 || null,
      actualSha256: actual,
      pass: exists && (!entry.source.sha256 || actual === entry.source.sha256),
    });
  }
  return results;
}

function runPreflight(options = {}) {
  const repoRoot = path.resolve(options.repoRoot || path.join(__dirname, '..', '..', '..'));
  const existsSync = options.existsSync || fs.existsSync;
  const env = options.env || process.env;
  const nodeVersion = options.nodeVersion || process.versions.node;
  const route = validateRoute(options.route);
  validatePopupPolicy(options.popupPolicy);
  validateProtocolPolicy(options.protocolPolicy);
  const checks = [];
  const add = (id, pass, detail) => checks.push({ id, pass: !!pass, detail });

  const nodeMajor = Number(String(nodeVersion).split('.')[0]);
  add('node-version', nodeMajor >= 22, { actual: nodeVersion, minimum: '22.0.0' });
  const edge = options.edgeExecutable || findEdgeExecutable(existsSync, options.edgeCandidates || DEFAULT_EDGE_PATHS);
  add('headless-edge', !!edge && existsSync(edge), { executable: edge });
  const puppeteerPackage = options.puppeteerPackage || path.join(repoRoot, 'Tools', 'headless', 'node_modules', 'puppeteer', 'package.json');
  add('puppeteer-runtime', existsSync(puppeteerPackage), { path: puppeteerPackage });
  add('route-is-data', !options.routePath || path.extname(options.routePath).toLowerCase() === '.json', { path: options.routePath || null });
  const snapshotSource = path.isAbsolute(route.snapshotSource)
    ? route.snapshotSource
    : path.resolve(options.routePath ? path.dirname(options.routePath) : repoRoot, route.snapshotSource);
  add('snapshot-source', existsSync(snapshotSource), { path: snapshotSource });

  const outputRoot = path.join(repoRoot, 'output');
  const outputDir = path.resolve(options.outputDir || path.join(outputRoot, 'ui-audit-preflight-placeholder'));
  add('output-location', isInside(outputRoot, outputDir) && outputDir !== outputRoot, { outputRoot, outputDir });
  let outputFresh = true;
  if (existsSync(outputDir)) {
    try { outputFresh = fs.readdirSync(outputDir).length === 0; } catch (_) { outputFresh = false; }
  }
  add('immutable-output-fresh', outputFresh, { outputDir });

  const accountEnv = route.session && route.session.accountEnv;
  const passwordEnv = route.session && route.session.passwordEnv;
  if (accountEnv) add('account-env', typeof env[accountEnv] === 'string' && env[accountEnv].length > 0, { name: accountEnv });
  if (passwordEnv) add('password-env', typeof env[passwordEnv] === 'string' && env[passwordEnv].length > 0, { name: passwordEnv });

  const authority = [];
  if (route.legacyRoot && route.verifyAuthority !== false) {
    authority.push(...verifyAuthority(options.popupPolicy, route.legacyRoot, existsSync));
    for (const result of authority) add(`authority:${result.id}`, result.pass, result);
  }

  return {
    schema: 1,
    checkedAt: new Date().toISOString(),
    pass: checks.every(check => check.pass),
    checks,
    authority,
    resolved: { repoRoot, outputDir, edgeExecutable: edge, puppeteerPackage },
  };
}

function assertPreflight(result) {
  if (!result || !result.pass) {
    const failed = result && result.checks ? result.checks.filter(check => !check.pass) : [];
    throw new Error(`UI_AUDIT_PREFLIGHT_FAILED: ${JSON.stringify(failed)}`);
  }
  return result;
}

module.exports = {
  DEFAULT_EDGE_PATHS,
  isInside,
  validateRoute,
  findEdgeExecutable,
  verifyAuthority,
  runPreflight,
  assertPreflight,
};
