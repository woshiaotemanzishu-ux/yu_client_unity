'use strict';

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { ROUTE_SCHEMA_VERSION } = require('./version.cjs');
const { validatePopupPolicy } = require('./popup-policy.cjs');
const { validateRuntimeOverlayPolicy, verifyRuntimeOverlayAuthority } = require('./runtime-overlay.cjs');
const { validateProtocolPolicy, validateRouteProtocolContract, verifyProtocolAuthority } = require('./protocol-probe.cjs');
const { validateItemUseRouteConfig } = require('./item-use.cjs');
const {
  ROUTE_URL_CHECK_ID,
  findServerProfileForUrl,
  resolvedServerProfile,
  probeRouteUrl,
} = require('./server-readiness.cjs');
const { serverRecovery, serverStatus } = require('./server-lifecycle.cjs');

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
  try {
    const parsed = new URL(route && route.url);
    if (!['http:', 'https:'].includes(parsed.protocol)) errors.push('url.protocol');
  } catch (_) { errors.push('url'); }
  if (!route || typeof route.snapshotSource !== 'string' || !route.snapshotSource.trim()) errors.push('snapshotSource');
  if (!Array.isArray(route && route.steps)) errors.push('steps');
  const actions = new Set([
    'snapshot', 'click', 'drag', 'wait-view', 'set-viewport', 'protocol-read', 'assert-protocol',
    'assert-nodes', 'assert-geometry', 'assert-scroll', 'branch', 'reset-sound', 'assert-sound', 'wait-render-ready',
  ]);
  const selectorPresent = value => value && typeof value === 'object' && !Array.isArray(value);
  const walk = (steps, prefix, inheritedClickAnchor = false) => {
    let hasClickAnchor = inheritedClickAnchor;
    for (const [index, step] of (steps || []).entries()) {
      const location = `${prefix}[${index}]`;
      if (!step || !actions.has(step.action)) errors.push(`${location}.action`);
      if (step && step.expect != null) errors.push(`${location}.expect-unsupported`);
      if (step && ['click', 'drag'].includes(step.action) && !selectorPresent(step.selector)) errors.push(`${location}.selector`);
      if (step && step.action === 'snapshot' && step.samplingTargetMs != null) {
        if (!hasClickAnchor) errors.push(`${location}.samplingTargetMs-no-click`);
        if (!Number.isFinite(Number(step.samplingTargetMs)) || Number(step.samplingTargetMs) < 0) errors.push(`${location}.samplingTargetMs`);
        if (step.samplingToleranceMs != null && (!Number.isFinite(Number(step.samplingToleranceMs)) || Number(step.samplingToleranceMs) < 0)) errors.push(`${location}.samplingToleranceMs`);
      }
      if (step && step.action === 'assert-nodes' && (!Array.isArray(step.assertions) || !step.assertions.length
        || step.assertions.some(assertion => !selectorPresent(assertion.selector)))) errors.push(`${location}.assertions`);
      if (step && step.action === 'assert-geometry') {
        const assertions = step.assertions || (step.assertion ? [step.assertion] : []);
        if (!Array.isArray(assertions) || !assertions.length || assertions.some(assertion => !selectorPresent(assertion.selector))) errors.push(`${location}.assertions`);
      }
      if (step && step.action === 'assert-scroll' && (typeof step.beforeLabel !== 'string' || !step.beforeLabel
        || !selectorPresent(step.assertion))) errors.push(`${location}.scroll`);
      if (step && step.action === 'protocol-read' && !selectorPresent(step.request)) errors.push(`${location}.request`);
      if (step && step.action === 'assert-protocol' && !selectorPresent(step.assertions)) errors.push(`${location}.assertions`);
      if (step && step.action === 'assert-sound' && !selectorPresent(step.assertions)) errors.push(`${location}.assertions`);
      if (step && step.action === 'wait-render-ready' && (!selectorPresent(step.probe)
        || !selectorPresent(step.probe.selector) || !Array.isArray(step.probe.propertyPath) || !step.probe.propertyPath.length)) errors.push(`${location}.probe`);
      if (step && step.action === 'click') hasClickAnchor = true;
      if (step && step.action === 'branch') {
        if (!step.condition || !Array.isArray(step.then) || !Array.isArray(step.else)) errors.push(`${location}.branch`);
        walk(step.then, `${location}.then`, hasClickAnchor);
        walk(step.else, `${location}.else`, hasClickAnchor);
      }
    }
  };
  walk(route && route.steps, 'steps');
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

async function runPreflight(options = {}) {
  const repoRoot = path.resolve(options.repoRoot || path.join(__dirname, '..', '..', '..'));
  const existsSync = options.existsSync || fs.existsSync;
  const env = options.env || process.env;
  const nodeVersion = options.nodeVersion || process.versions.node;
  const route = validateRoute(options.route);
  validatePopupPolicy(options.popupPolicy);
  validateRuntimeOverlayPolicy(options.runtimeOverlayPolicy);
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

  const itemUse = validateItemUseRouteConfig(route.session && route.session.itemUse);
  add('item-use-session-policy', itemUse.pass, itemUse);
  const protocolContract = validateRouteProtocolContract(route, options.protocolPolicy);
  add('route-protocol-contract', protocolContract.pass, protocolContract);

  const authority = [];
  if (route.legacyRoot && route.verifyAuthority !== false) {
    authority.push(...verifyAuthority(options.popupPolicy, route.legacyRoot, existsSync));
    authority.push(...verifyRuntimeOverlayAuthority(options.runtimeOverlayPolicy, route.legacyRoot, existsSync));
    authority.push(...verifyProtocolAuthority(options.protocolPolicy, route.legacyRoot, existsSync));
    for (const result of authority) add(`authority:${result.id}`, result.pass, result);
  }

  const rawServerProfile = options.serverProfile || findServerProfileForUrl(route.url, options.serverProfiles);
  const serverProfile = resolvedServerProfile(rawServerProfile, repoRoot);
  const localRoute = ['127.0.0.1', 'localhost'].includes(new URL(route.url).hostname.toLowerCase());
  add('route-server-profile', !localRoute || !!serverProfile, serverProfile
    ? { pass: true, profileId: serverProfile.id, cwd: serverProfile.cwd, staticRoot: serverProfile.staticRoot, url: serverProfile.url }
    : { pass: !localRoute, code: 'SERVER_PROFILE_NOT_FOUND', url: route.url });
  const readinessOptions = { ...(serverProfile && serverProfile.readiness || {}), ...(route.readiness || {}), ...(options.routeReadiness || {}) };
  let routeProbe;
  let serverContext = null;
  if (serverProfile) {
    serverContext = await (options.serverStatus || serverStatus)({
      repoRoot,
      profile: rawServerProfile,
      url: route.url,
      readiness: readinessOptions,
      probeRouteUrl: options.probeRouteUrl,
      inspectEndpoint: options.inspectEndpoint,
      inspectProcess: options.inspectProcess,
      sleep: options.sleep,
      runtimeDirectory: options.runtimeDirectory,
    });
    routeProbe = serverContext.probe;
  } else {
    routeProbe = await (options.probeRouteUrl || probeRouteUrl)(route.url, readinessOptions);
  }
  const readinessDetail = {
    ...routeProbe,
    profileId: serverProfile && serverProfile.id || null,
    serverCode: serverContext && serverContext.code || null,
    ownership: serverContext && serverContext.ownership || null,
    observation: serverContext && serverContext.observation || null,
    recovery: serverContext ? serverContext.recovery : serverProfile ? serverRecovery(serverProfile) : null,
  };
  add(ROUTE_URL_CHECK_ID, routeProbe.pass, readinessDetail);

  return {
    schema: 1,
    checkedAt: new Date().toISOString(),
    pass: checks.every(check => check.pass),
    checks,
    authority,
    resolved: { repoRoot, outputDir, edgeExecutable: edge, puppeteerPackage, serverProfile },
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
