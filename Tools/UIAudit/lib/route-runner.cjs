'use strict';

const fs = require('fs');
const path = require('path');
const { runPreflight, assertPreflight } = require('./preflight.cjs');
const { loadPopupPolicy } = require('./popup-policy.cjs');
const {
  loadProtocolPolicy,
  installLegacyProtocolTrace,
  resetLegacyProtocolTrace,
  readLegacyProtocolTrace,
  sendReadProbe,
  evaluateProtocolAssertions,
} = require('./protocol-probe.cjs');
const { HeadlessUiSession, sleep } = require('./session.cjs');
const { clickRuntimeTarget, dragRuntimeTarget } = require('./canvas-input.cjs');
const { closeItemUseControlled } = require('./item-use.cjs');
const { writeJsonAtomic } = require('./safe-json.cjs');
const { createReport, addArtifact, finalizeReport, writeReport } = require('./report.cjs');

function resolveRoutePath(routePath, value) {
  return path.isAbsolute(value) ? value : path.resolve(path.dirname(routePath), value);
}

function immutablePath(target) {
  if (fs.existsSync(target)) throw new Error(`IMMUTABLE_EVIDENCE_EXISTS: ${target}`);
  fs.mkdirSync(path.dirname(target), { recursive: true });
  return target;
}

async function waitForView(session, viewName, visible = true, timeoutMs = 12000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const snapshot = await session.snapshot();
    if (snapshot.visibleViews.includes(viewName) === visible) return snapshot;
    await sleep(100);
  }
  throw new Error(`WAIT_VIEW_TIMEOUT view=${viewName} visible=${visible}`);
}

async function saveSnapshotEvidence(session, outputDir, label, screenshot = true) {
  const snapshot = await session.snapshot();
  const jsonPath = writeJsonAtomic(path.join(outputDir, `${label}.runtime.json`), snapshot);
  let screenshotPath = null;
  if (screenshot) {
    screenshotPath = immutablePath(path.join(outputDir, `${label}.png`));
    await session.page.screenshot({ path: screenshotPath });
  }
  return { snapshot, jsonPath, screenshotPath };
}

async function executeStep(context, step, index) {
  const { session, outputDir, protocolPolicy, report } = context;
  const label = step.label || `${String(index).padStart(3, '0')}_${step.action}`;
  const before = await session.snapshot();
  let result;
  if (step.action === 'snapshot') {
    result = await saveSnapshotEvidence(session, outputDir, label, step.screenshot !== false);
    addArtifact(report, outputDir, result.jsonPath, 'runtime-snapshot');
    if (result.screenshotPath) addArtifact(report, outputDir, result.screenshotPath, 'real-browser-screenshot');
  } else if (step.action === 'click') {
    result = await clickRuntimeTarget(session.page, before, step.selector, step.options || {});
  } else if (step.action === 'drag') {
    result = await dragRuntimeTarget(session.page, before, step.selector, step.options || {});
  } else if (step.action === 'wait-view') {
    result = await waitForView(session, step.view, step.visible !== false, step.timeoutMs || 12000);
  } else if (step.action === 'set-viewport') {
    await session.page.setViewport({ width: Number(step.width), height: Number(step.height) });
    result = { width: Number(step.width), height: Number(step.height) };
  } else if (step.action === 'protocol-read') {
    result = await sendReadProbe(session.page, step.request, protocolPolicy);
    if (step.waitInboundCmd != null) {
      await session.page.waitForFunction(cmd => {
        const trace = window.__uiAuditProtocolTrace;
        return trace && trace.inbound.some(event => Number(event.cmd) === Number(cmd));
      }, { timeout: step.timeoutMs || 12000 }, Number(step.waitInboundCmd));
    }
  } else if (step.action === 'assert-protocol') {
    const trace = await readLegacyProtocolTrace(session.page);
    result = evaluateProtocolAssertions(trace, step.assertions, protocolPolicy);
    if (!result.pass) throw new Error(`PROTOCOL_ASSERTION_FAILED: ${JSON.stringify(result)}`);
  } else {
    throw new Error(`UNSUPPORTED_ROUTE_ACTION: ${step.action}`);
  }
  if (step.settleMs) await sleep(Number(step.settleMs));
  const entry = { index, label, action: step.action, at: new Date().toISOString(), result };
  report.steps.push(entry);
  return entry;
}

async function runRoute(options) {
  const routePath = path.resolve(options.routePath);
  const route = JSON.parse(fs.readFileSync(routePath, 'utf8'));
  const repoRoot = path.resolve(options.repoRoot || path.join(__dirname, '..', '..', '..'));
  const outputDir = path.resolve(options.outputDir);
  const popupPolicyPath = resolveRoutePath(routePath, route.popupPolicy || '../policies/startup-popups.json');
  const protocolPolicyPath = resolveRoutePath(routePath, route.protocolPolicy || '../policies/protocols.json');
  const popupPolicy = loadPopupPolicy(popupPolicyPath);
  const protocolPolicy = loadProtocolPolicy(protocolPolicyPath);
  const preflight = assertPreflight(runPreflight({
    repoRoot, routePath, route, outputDir, popupPolicy, protocolPolicy,
    edgeExecutable: options.edgeExecutable, env: options.env || process.env,
  }));
  fs.mkdirSync(outputDir, { recursive: true });
  writeJsonAtomic(path.join(outputDir, 'preflight.json'), preflight);
  const report = createReport(route, preflight);
  const env = options.env || process.env;
  const session = new HeadlessUiSession({
    repoRoot,
    url: route.url,
    viewport: route.viewport || { width: 720, height: 1280 },
    edgeExecutable: preflight.resolved.edgeExecutable,
    snapshotSource: resolveRoutePath(routePath, route.snapshotSource),
  });
  let error = null;
  try {
    await session.start();
    await installLegacyProtocolTrace(session.page, { inboundCommands: route.protocol && route.protocol.inboundCommands || [] });
    await resetLegacyProtocolTrace(session.page, 'login-and-route');
    const account = options.account || env[route.session.accountEnv];
    const password = options.password || env[route.session.passwordEnv];
    const itemUseHandler = route.session.itemUse ? async () => {
      await resetLegacyProtocolTrace(session.page, 'item-use-controlled');
      return closeItemUseControlled(session.page, {
        ...route.session.itemUse,
        protocolPolicy,
        protocolAssertions: route.session.itemUse.protocolAssertions || { mode: 'read-only' },
      });
    } : null;
    await session.loginAndReachMainUi({ account, password, popupPolicy, itemUseHandler });
    await resetLegacyProtocolTrace(session.page, 'page-route');
    report.session = { id: session.sessionId, hotSession: true };
    for (let index = 0; index < route.steps.length; index++) await executeStep({ session, outputDir, protocolPolicy, report }, route.steps[index], index);
    const trace = await readLegacyProtocolTrace(session.page);
    report.protocol = evaluateProtocolAssertions(trace, route.protocol && route.protocol.assertions || {}, protocolPolicy);
    if (!report.protocol.pass) throw new Error(`FINAL_PROTOCOL_ASSERTION_FAILED: ${JSON.stringify(report.protocol)}`);
    report.events = session.events;
    finalizeReport(report, 'passed');
  } catch (caught) {
    error = caught;
    report.events = session.events;
    finalizeReport(report, 'failed', caught);
  } finally {
    try { await session.close(); } catch (closeError) { if (!error) error = closeError; }
    writeReport(outputDir, report);
  }
  if (error) throw error;
  return report;
}

module.exports = {
  resolveRoutePath,
  waitForView,
  saveSnapshotEvidence,
  executeStep,
  runRoute,
};
