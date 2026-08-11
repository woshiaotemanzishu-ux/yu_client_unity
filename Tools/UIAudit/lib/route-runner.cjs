'use strict';

const fs = require('fs');
const path = require('path');
const { runPreflight, assertPreflight } = require('./preflight.cjs');
const { loadPopupPolicy } = require('./popup-policy.cjs');
const { loadRuntimeOverlayPolicy } = require('./runtime-overlay.cjs');
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
const {
  evaluateNodeAssertions,
  evaluateGeometryAssertion,
  evaluateScrollAssertion,
  evaluateBranchCondition,
} = require('./route-assertions.cjs');
const {
  installSoundTrace,
  resetSoundTrace,
  readSoundTrace,
  evaluateSoundAssertions,
  waitRenderTextureReady,
} = require('./runtime-probes.cjs');
const { findServerProfileForUrl } = require('./server-readiness.cjs');
const { ensureServer } = require('./server-lifecycle.cjs');
const { writeJsonAtomic } = require('./safe-json.cjs');
const { createReport, addArtifact, finalizeReport, writeReport, measurePhase } = require('./report.cjs');
const { writeSelectorDiagnostic } = require('./selector-diagnostic.cjs');

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

async function saveSnapshotEvidence(session, outputDir, label, screenshot = true, suppliedSnapshot = null) {
  const snapshot = suppliedSnapshot || await session.snapshot();
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
  const startedAtMs = Date.now();
  const startedAt = new Date(startedAtMs).toISOString();
  let samplingWaitMs = 0;
  let configuredSettleMs = 0;
  let result;
  const category = ['click', 'drag', 'set-viewport'].includes(step.action) ? 'interaction'
    : step.action === 'snapshot' ? 'evidence'
      : ['wait-view', 'wait-render-ready'].includes(step.action) ? 'runtime-ready-wait'
        : step.action.startsWith('assert-') ? 'assertion' : 'runtime-operation';
  try {
  if (step.action === 'snapshot' && step.samplingTargetMs != null) {
    if (!context.lastClickAt) throw new Error(`SAMPLING_TARGET_WITHOUT_CLICK: ${label}`);
    const remaining = context.lastClickAt + Number(step.samplingTargetMs) - Date.now();
    if (remaining > 0) {
      samplingWaitMs = remaining;
      await sleep(remaining);
    }
  }
  const before = await session.snapshot();
  if (step.action === 'snapshot') {
    const sampledAt = Date.now();
    result = await saveSnapshotEvidence(session, outputDir, label, step.screenshot !== false, before);
    context.snapshots.set(label, result.snapshot);
    if (step.samplingTargetMs != null) {
      result.timing = {
        targetMs: Number(step.samplingTargetMs),
        actualMs: sampledAt - context.lastClickAt,
        toleranceMs: Number(step.samplingToleranceMs == null ? 250 : step.samplingToleranceMs),
        anchor: context.lastClickLabel,
      };
      result.timing.errorMs = result.timing.actualMs - result.timing.targetMs;
      result.timing.pass = Math.abs(result.timing.errorMs) <= result.timing.toleranceMs;
    }
    addArtifact(report, outputDir, result.jsonPath, 'runtime-snapshot');
    if (result.screenshotPath) addArtifact(report, outputDir, result.screenshotPath, 'real-browser-screenshot');
    if (result.timing && !result.timing.pass) throw new Error(`SAMPLING_TARGET_MISSED: ${JSON.stringify(result.timing)}`);
  } else if (step.action === 'click') {
    result = await clickRuntimeTarget(session.page, before, step.selector, step.options || {});
    context.lastClickAt = Date.now();
    context.lastClickLabel = label;
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
  } else if (step.action === 'assert-nodes') {
    result = evaluateNodeAssertions(before, step.assertions || []);
    if (!result.pass) throw new Error(`NODE_ASSERTION_FAILED: ${JSON.stringify(result)}`);
  } else if (step.action === 'assert-geometry') {
    const assertions = step.assertions || (step.assertion ? [step.assertion] : []);
    const results = assertions.map(assertion => evaluateGeometryAssertion(before, assertion));
    result = { pass: results.every(value => value.pass), results };
    if (!result.pass) throw new Error(`GEOMETRY_ASSERTION_FAILED: ${JSON.stringify(result)}`);
  } else if (step.action === 'assert-scroll') {
    const beforeSnapshot = context.snapshots.get(step.beforeLabel);
    if (!beforeSnapshot) throw new Error(`SCROLL_BASELINE_MISSING: ${step.beforeLabel}`);
    result = evaluateScrollAssertion(beforeSnapshot, before, step.assertion || step);
    if (!result.pass) throw new Error(`SCROLL_ASSERTION_FAILED: ${JSON.stringify(result)}`);
  } else if (step.action === 'branch') {
    const condition = evaluateBranchCondition(before, step.condition);
    const branch = condition.pass ? 'then' : 'else';
    const nested = [];
    for (let child = 0; child < step[branch].length; child++) {
      nested.push(await executeStep(context, step[branch][child], `${index}.${branch}.${child}`));
    }
    result = { pass: true, condition, branch, nestedSteps: nested.map(value => value.index) };
  } else if (step.action === 'reset-sound') {
    result = await resetSoundTrace(session.page, step.traceLabel || label);
  } else if (step.action === 'assert-sound') {
    const trace = await readSoundTrace(session.page);
    result = evaluateSoundAssertions(trace, step.assertions || {});
    if (!result.pass) throw new Error(`SOUND_ASSERTION_FAILED: ${JSON.stringify(result)}`);
  } else if (step.action === 'wait-render-ready') {
    result = await waitRenderTextureReady(session.page, before, step.probe || {}, sleep);
    if (!result.pass) throw new Error(`RENDER_TEXTURE_NOT_READY: ${JSON.stringify(result)}`);
  } else {
    throw new Error(`UNSUPPORTED_ROUTE_ACTION: ${step.action}`);
  }
  if (step.settleMs) {
    configuredSettleMs = Number(step.settleMs);
    await sleep(configuredSettleMs);
  }
  const endedAtMs = Date.now();
  const entry = {
    index, label, action: step.action, at: new Date(endedAtMs).toISOString(), status: 'passed', result,
    timing: {
      category,
      startedAt,
      endedAt: new Date(endedAtMs).toISOString(),
      durationMs: endedAtMs - startedAtMs,
      samplingWaitMs,
      configuredSettleMs,
      fixedWaitCandidateMs: configuredSettleMs,
      unclassifiedMs: Math.max(0, endedAtMs - startedAtMs - samplingWaitMs - configuredSettleMs),
    },
  };
  report.steps.push(entry);
  return entry;
  } catch (error) {
    const endedAtMs = Date.now();
    report.steps.push({
      index, label, action: step.action, at: new Date(endedAtMs).toISOString(), status: 'failed',
      result: null,
      error: String(error && error.stack || error),
      timing: {
        category,
        startedAt,
        endedAt: new Date(endedAtMs).toISOString(),
        durationMs: endedAtMs - startedAtMs,
        samplingWaitMs,
        configuredSettleMs,
        fixedWaitCandidateMs: configuredSettleMs,
        unclassifiedMs: Math.max(0, endedAtMs - startedAtMs - samplingWaitMs - configuredSettleMs),
      },
    });
    throw error;
  }
}

function attachFailureDiagnostic(report, outputDir, error) {
  if (!error || !error.diagnostic) return null;
  const diagnosticPath = writeSelectorDiagnostic(outputDir, error.diagnostic);
  const contextKind = error.diagnostic.context && error.diagnostic.context.kind;
  const lifecycle = contextKind === 'popup-close-lifecycle';
  const canvasInput = contextKind === 'canvas-input';
  const popupStack = contextKind === 'popup-runtime-stack';
  const runtimeOverlay = contextKind === 'runtime-overlay';
  const artifact = addArtifact(report, outputDir, diagnosticPath,
    lifecycle ? 'popup-close-lifecycle-diagnostic'
      : canvasInput ? 'canvas-input-diagnostic'
        : popupStack ? 'popup-runtime-stack-diagnostic'
          : runtimeOverlay ? 'runtime-overlay-diagnostic' : 'selector-identity-diagnostic');
  report.failure = {
    code: error.code || 'SELECTOR_IDENTITY_FAILURE',
    diagnostic: {
      schema: error.diagnostic.schema,
      diagnosticSha256: error.diagnostic.sha256,
      selectorSha256: error.diagnostic.sha256,
      artifact,
    },
  };
  return report.failure;
}

async function runRoute(options) {
  const routePath = path.resolve(options.routePath);
  const route = JSON.parse(fs.readFileSync(routePath, 'utf8'));
  const repoRoot = path.resolve(options.repoRoot || path.join(__dirname, '..', '..', '..'));
  const outputDir = path.resolve(options.outputDir);
  const popupPolicyPath = resolveRoutePath(routePath, route.popupPolicy || '../policies/startup-popups.json');
  const protocolPolicyPath = resolveRoutePath(routePath, route.protocolPolicy || '../policies/protocols.json');
  const popupPolicy = loadPopupPolicy(popupPolicyPath);
  const runtimeOverlayPolicy = loadRuntimeOverlayPolicy(path.join(__dirname, '..', 'policies', 'runtime-overlays.json'));
  const protocolPolicy = loadProtocolPolicy(protocolPolicyPath);
  let ensuredServer = null;
  if (options.ensureServer) {
    const profile = findServerProfileForUrl(route.url);
    if (!profile) throw new Error(`SERVER_PROFILE_NOT_FOUND_FOR_ROUTE: ${route.url}`);
    ensuredServer = await (options.ensureServerFn || ensureServer)({ repoRoot, profile });
  }
  const preflight = assertPreflight(await runPreflight({
    repoRoot, routePath, route, outputDir, popupPolicy, runtimeOverlayPolicy, protocolPolicy,
    edgeExecutable: options.edgeExecutable, env: options.env || process.env,
    ...(options.preflightOptions || {}),
  }));
  fs.mkdirSync(outputDir, { recursive: true });
  writeJsonAtomic(path.join(outputDir, 'preflight.json'), preflight);
  const report = createReport(route, preflight);
  const env = options.env || process.env;
  const sessionOptions = {
    repoRoot,
    url: route.url,
    viewport: route.viewport || { width: 720, height: 1280 },
    edgeExecutable: preflight.resolved.edgeExecutable,
    snapshotSource: resolveRoutePath(routePath, route.snapshotSource),
  };
  const session = options.sessionFactory ? options.sessionFactory(sessionOptions) : new HeadlessUiSession(sessionOptions);
  let error = null;
  try {
    await measurePhase(report, 'session-start', 'environment-wait', () => session.start());
    await measurePhase(report, 'protocol-bootstrap', 'runtime-operation', async () => {
      await installLegacyProtocolTrace(session.page, { inboundCommands: route.protocol && route.protocol.inboundCommands || [] });
      await resetLegacyProtocolTrace(session.page, 'login-and-route');
    });
    const account = options.account || env[route.session.accountEnv];
    const password = options.password || env[route.session.passwordEnv];
    const itemUseHandler = route.session.itemUse && route.session.itemUse.mode !== 'hard-stop' ? async () => {
      await resetLegacyProtocolTrace(session.page, 'item-use-controlled');
      return closeItemUseControlled(session.page, {
        ...route.session.itemUse,
        protocolPolicy,
        protocolAssertions: route.session.itemUse.protocolAssertions || { mode: 'read-only' },
      });
    } : null;
    await measurePhase(report, 'login-mainui', 'runtime-wait', () => session.loginAndReachMainUi({ account, password, popupPolicy, runtimeOverlayPolicy, itemUseHandler }));
    await resetLegacyProtocolTrace(session.page, 'page-route');
    if (routeUsesAction(route.steps, new Set(['reset-sound', 'assert-sound']))) {
      await installSoundTrace(session.page);
      await resetSoundTrace(session.page, 'page-route');
    }
    report.session = { id: session.sessionId, hotSession: true };
    if (ensuredServer) report.server = { profileId: ensuredServer.profile.id, code: ensuredServer.code, started: ensuredServer.started };
    await measurePhase(report, 'route-protocol-reads', 'runtime-wait', async () => {
      for (const request of route.protocol && route.protocol.reads || []) {
        await sendReadProbe(session.page, request, protocolPolicy);
        if (request.waitInboundCmd != null) {
          await session.page.waitForFunction(cmd => {
            const trace = window.__uiAuditProtocolTrace;
            return trace && trace.inbound.some(event => Number(event.cmd) === Number(cmd));
          }, { timeout: request.timeoutMs || 12000 }, Number(request.waitInboundCmd));
        }
      }
    });
    const context = { session, outputDir, protocolPolicy, report, snapshots: new Map(), lastClickAt: null, lastClickLabel: null };
    await measurePhase(report, 'route-steps', 'mixed', async () => {
      for (let index = 0; index < route.steps.length; index++) await executeStep(context, route.steps[index], index);
    });
    await measurePhase(report, 'final-protocol', 'assertion', async () => {
      const trace = await readLegacyProtocolTrace(session.page);
      report.protocol = evaluateProtocolAssertions(trace, route.protocol && route.protocol.assertions || {}, protocolPolicy);
    });
    if (!report.protocol.pass) throw new Error(`FINAL_PROTOCOL_ASSERTION_FAILED: ${JSON.stringify(report.protocol)}`);
    report.events = session.events;
  } catch (caught) {
    error = caught;
    report.events = session.events;
    try {
      attachFailureDiagnostic(report, outputDir, caught);
    } catch (diagnosticError) {
      report.failure = {
        code: caught && caught.code || 'SELECTOR_IDENTITY_FAILURE',
        diagnosticPersistenceError: String(diagnosticError && diagnosticError.stack || diagnosticError),
      };
    }
  } finally {
    try { await measurePhase(report, 'session-close', 'cleanup', () => session.close()); } catch (closeError) { if (!error) error = closeError; }
    finalizeReport(report, error ? 'failed' : 'passed', error);
    writeReport(outputDir, report);
  }
  if (error) throw error;
  return report;
}

function routeUsesAction(steps, actions) {
  for (const step of steps || []) {
    if (actions.has(step.action)) return true;
    if (step.action === 'branch' && (routeUsesAction(step.then, actions) || routeUsesAction(step.else, actions))) return true;
  }
  return false;
}

module.exports = {
  resolveRoutePath,
  waitForView,
  saveSnapshotEvidence,
  executeStep,
  attachFailureDiagnostic,
  routeUsesAction,
  runRoute,
};
