'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const {
  loadPopupPolicy,
  decidePopup,
  observePopupStack,
  orderPopupQueue,
  planPopupDrain,
  assertSafePopupDecision,
  popupCloseStability,
} = require('../lib/popup-policy.cjs');
const { normalizeRuntimeSources } = require('../lib/runtime-tree.cjs');
const { HeadlessUiSession } = require('../lib/session.cjs');
const { loadRuntimeOverlayPolicy } = require('../lib/runtime-overlay.cjs');

const policy = loadPopupPolicy(path.join(__dirname, '..', 'policies', 'startup-popups.json'));
const runtimeOverlayPolicy = loadRuntimeOverlayPolicy(path.join(__dirname, '..', 'policies', 'runtime-overlays.json'));
const runtimeOverlayFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-overlays.json'), 'utf8'));
const cycleimpFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'startup-popup-cycleimp-yesterday.json'), 'utf8'));
const runtimeOwnerBindingFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-owner-bindings.json'), 'utf8'));

test('unknown popup is an unconditional hard stop', () => {
  assert.equal(policy.entries.length, 21);
  const decision = decidePopup(policy, 'NeverAuditedPopup');
  assert.equal(decision.action, 'unknown-hard-stop');
  assert.throws(() => assertSafePopupDecision(decision), /POPUP_HARD_STOP/);
});

test('cross-server holy-area push notice closes only through its owned shared background', () => {
  const decision = decidePopup(policy, 'KfHolyAreaRebornTipsView');
  assert.equal(decision.action, 'allow');
  assert.equal(decision.entry.queue.order, 'observed-top-first');
  assert.equal(decision.entry.safeClose.kind, 'shared-background');
  assert.equal(decision.entry.safeClose.requiresCurrentView, true);
  assert.deepEqual(decision.entry.closeProtocols, []);
  assert.deepEqual(decision.entry.closeWrites, []);
  assert.deepEqual(assertSafePopupDecision(decision), decision.entry.safeClose);
});

test('shared base window startup cleanup uses only the source-backed return surface', () => {
  const decision = decidePopup(policy, 'BaseWindowSkin');
  assert.equal(decision.action, 'allow');
  assert.equal(decision.entry.queue.order, 'observed-top-first');
  assert.equal(decision.entry.safeClose.node, '_img_return');
  assert.equal(decision.entry.safeClose.requiresCurrentView, true);
  assert.equal(decision.entry.safeClose.stability.consecutiveFrames, 2);
  assert.equal(decision.entry.closeProtocols.length, 0);
  assert.equal(decision.entry.closeWrites.length, 0);
  assert.deepEqual(assertSafePopupDecision(decision), decision.entry.safeClose);
});

test('function-open presentation waits for its authoritative timer instead of clicking', () => {
  const decision = decidePopup(policy, 'FunctionOpenAutoView');
  assert.equal(decision.action, 'wait');
  assert.equal(decision.entry.waitForRelease.timeoutMs, 15000);
  assert.deepEqual(decision.entry.closeProtocols, [13801]);
  assert.throws(() => assertSafePopupDecision(decision), /POPUP_HARD_STOP/);
});

test('star equipment master startup notice has a source-backed pure close surface', () => {
  const decision = decidePopup(policy, 'StarEquipUpMasterView');
  assert.equal(decision.action, 'allow');
  assert.equal(decision.entry.queue.order, 'observed-top-first');
  assert.equal(decision.entry.safeClose.node, 'closeBtn');
  assert.equal(decision.entry.closeProtocols.length, 0);
  assert.equal(decision.entry.closeWrites.length, 0);
  assert.deepEqual(assertSafePopupDecision(decision), decision.entry.safeClose);
});

test('Cycleimp yesterday is source-backed, deduplicated and requires observed runtime stack order', () => {
  const decision = decidePopup(policy, cycleimpFixture.view);
  assert.equal(decision.action, 'allow');
  assert.equal(decision.entry.sort, undefined);
  assert.deepEqual(decision.entry.queue, {
    kind: 'direct-response-open',
    configured: false,
    configKey: 'CycleimpActlistYesterday',
    order: 'observed-top-first',
    reason: 'the view is absent from ClientConfigPopupLevel and opens directly from the 22703 response handler',
  });
  assert.deepEqual(assertSafePopupDecision(decision), decision.entry.safeClose);
  assert.throws(() => orderPopupQueue(cycleimpFixture.detectedTopFirst, policy), /POPUP_OBSERVED_TOP_FIRST_REQUIRED/);

  const plan = planPopupDrain(cycleimpFixture.detectedTopFirst, policy, { observedTopFirst: true });
  assert.equal(plan.pass, true);
  assert.deepEqual(plan.steps.map(step => step.view), ['CycleimpActlistYesterday', 'DailyActTipView']);
  assert.equal(plan.steps[0].observed.source, 'laya-stage');
  assert.equal(plan.steps[0].entry.closeProtocols.length, 0);
  assert.equal(plan.steps[0].entry.closeWrites.length, 0);
});

test('observed runtime stack uses the current Laya child order instead of loaded-view order', () => {
  const snapshot = normalizeRuntimeSources(runtimeOwnerBindingFixture);
  const cycleRoot = snapshot.nodes.find(node => node.source === 'laya-stage'
    && node.identity && node.identity.owner && node.identity.owner.isRoot
    && node.identity.owner.view === 'CycleimpActlistYesterday');
  const upperRoot = structuredClone(cycleRoot);
  upperRoot.view = 'DailyActTipView';
  upperRoot.name = 'DailyActTipView';
  upperRoot.path = 'Stage[0]/DailyActTipView[2]';
  upperRoot.indexPath = [0, 2];
  upperRoot.childIndex = 2;
  upperRoot.identity.ownerView = 'DailyActTipView';
  upperRoot.identity.runtimeName = 'DailyActTipView';
  upperRoot.identity.owner.view = 'DailyActTipView';
  upperRoot.identity.owner.rootStagePath = [2];
  upperRoot.identity.owner.instances = [{ source: 'RuntimeRegistry', key: 'root_upper' }];
  snapshot.nodes.push(upperRoot);

  const stack = observePopupStack(snapshot, ['CycleimpActlistYesterday', 'DailyActTipView']);
  assert.deepEqual(stack.map(item => item.view), ['DailyActTipView', 'CycleimpActlistYesterday']);
  assert.deepEqual(stack[0].stagePath, [0, 2]);
  assert.equal(stack[0].instance[0].key, 'root_upper');
});

test('managed background current view joins the observed stack even when no loaded-view owner exists', () => {
  const snapshot = normalizeRuntimeSources(runtimeOwnerBindingFixture);
  snapshot.runtimeOverlays = [runtimeOverlayFixture.runtimeOverlays[0]];
  const stack = observePopupStack(snapshot, ['CycleimpActlistYesterday'], runtimeOverlayPolicy);
  assert.deepEqual(stack.map(item => item.view), ['DailyActTipView', 'CycleimpActlistYesterday']);
  assert.equal(stack[0].source, 'runtime-overlay');
  assert.deepEqual(stack[0].stagePath, [0, 2, 2]);
  assert.equal(stack[0].overlay.kind, 'managed-view-background');
});

test('popup close is deferred without input when an authoritative overlay view becomes the real stack top', async () => {
  const snapshot = normalizeRuntimeSources(runtimeOwnerBindingFixture);
  snapshot.runtimeOverlays = [runtimeOverlayFixture.runtimeOverlays[0]];
  const session = new HeadlessUiSession({});
  session.snapshot = async () => snapshot;
  session.page = { mouse: { click: async () => { throw new Error('must not click a covered popup'); } } };
  const result = await session.closeAllowlistedPopup('CycleimpActlistYesterday', policy, {
    preset: require('../lib/session.cjs').LEGACY_720_LOGIN,
    runtimeOverlayPolicy,
  });
  assert.equal(result.deferred, true);
  assert.equal(result.clicks, 0);
  assert.equal(result.observedTopFirst[0].view, 'DailyActTipView');
});

test('Cycleimp close needs two distinct advancing Laya frames with the view absent', () => {
  const entry = decidePopup(policy, cycleimpFixture.view).entry;
  const stable = popupCloseStability(cycleimpFixture.stableCloseSamples, cycleimpFixture.view, entry.safeClose.stability);
  assert.equal(stable.pass, true);
  assert.equal(stable.stableFrames, 2);
  assert.equal(stable.lastFrameToken, 502);

  const duplicateFrame = popupCloseStability(cycleimpFixture.stableCloseSamples.slice(0, 3), cycleimpFixture.view, entry.safeClose.stability);
  assert.equal(duplicateFrame.pass, false);
  assert.equal(duplicateFrame.stableFrames, 1);

  const reappearing = popupCloseStability(cycleimpFixture.reappearingSamples, cycleimpFixture.view, entry.safeClose.stability);
  assert.equal(reappearing.pass, false);
  assert.equal(reappearing.stableFrames, 1);
});

test('Cycleimp exact bound-field close clicks once and waits for two advancing absent frames', async () => {
  const before = normalizeRuntimeSources(runtimeOwnerBindingFixture);
  const samples = [
    before,
    { visibleViews: ['MainUITopView'], stage: { width: 720, height: 1280, frameToken: 501 } },
    { visibleViews: ['MainUITopView'], stage: { width: 720, height: 1280, frameToken: 502 } },
  ];
  const mouseCalls = [];
  const session = new HeadlessUiSession({});
  session.page = {
    viewport: () => ({ width: 720, height: 1280 }),
    evaluate: async (_function, payload) => {
      if (payload && Object.hasOwn(payload, 'logicalWidth')) {
        return { x: 0, y: 0, width: 720, height: 1280, logicalWidth: 720, logicalHeight: 1280, canvasIndex: 0 };
      }
      if (payload && payload.operation === 'inspect-canvas-input') {
        const target = {
          path: payload.indexPath.join('/'), indexPath: payload.indexPath,
          ownerView: payload.selector.ownerView, hitAtPoint: true,
        };
        return {
          schema: 'ui-audit.canvas-input.v1', applicable: true,
          targetResolution: { actualCount: 1, currentIndexPath: payload.indexPath },
          target, targetChain: [target], topmost: target, topmostChain: [target], capture: null,
          mapping: { roundTripPass: true, pointInsideCanvas: true, domCanvasTop: true },
        };
      }
      if (payload && payload.operation === 'install-canvas-input-probe') return { pass: true, probeId: payload.probeId };
      if (payload && payload.operation === 'finish-canvas-input-probe') {
        return {
          schema: 'ui-audit.canvas-input.v1', probeId: payload.probeId,
          domEvents: [{ type: 'mousedown' }, { type: 'mouseup' }],
          targetEvents: [{ type: 'click', listenerCountBefore: 1, dispatched: true }],
          semanticCalls: [{ name: 'Close' }],
        };
      }
      throw new Error(`unexpected page.evaluate payload: ${JSON.stringify(payload)}`);
    },
    mouse: { click: async (...args) => mouseCalls.push(args) },
  };
  session.snapshot = async () => {
    const next = samples.shift();
    if (!next) throw new Error('unexpected extra snapshot');
    return next;
  };

  const result = await session.closeAllowlistedPopup('CycleimpActlistYesterday', policy);
  assert.equal(result.closed, true);
  assert.equal(result.clicks, 1);
  assert.equal(result.stability.stableFrames, 2);
  assert.equal(result.stability.lastFrameToken, 502);
  assert.equal(mouseCalls.length, 1);
  assert.deepEqual(mouseCalls[0].slice(0, 2), [608, 184]);
});

test('popup close failure diagnostic retains topmost input consumption and lifecycle samples', async () => {
  const before = normalizeRuntimeSources(runtimeOwnerBindingFixture);
  const shortPolicy = structuredClone(policy);
  const entry = shortPolicy.entries.find(item => item.view === 'CycleimpActlistYesterday');
  entry.safeClose.stability.timeoutMs = 3;
  entry.safeClose.stability.pollMs = 1;
  const mouseCalls = [];
  const session = new HeadlessUiSession({});
  session.page = {
    viewport: () => ({ width: 720, height: 1280 }),
    evaluate: async (_function, payload) => {
      if (payload && Object.hasOwn(payload, 'logicalWidth')) {
        return { x: 0, y: 0, width: 720, height: 1280, logicalWidth: 720, logicalHeight: 1280, canvasIndex: 0 };
      }
      if (payload.operation === 'inspect-canvas-input') {
        const target = {
          path: payload.indexPath.join('/'), indexPath: payload.indexPath,
          ownerView: payload.selector.ownerView, hitAtPoint: true,
        };
        return {
          schema: 'ui-audit.canvas-input.v1', applicable: true,
          targetResolution: { actualCount: 1 }, target, targetChain: [target],
          topmost: target, topmostChain: [target], capture: null,
          mapping: { roundTripPass: true, pointInsideCanvas: true, domCanvasTop: true },
        };
      }
      if (payload.operation === 'install-canvas-input-probe') return { pass: true, probeId: payload.probeId };
      if (payload.operation === 'finish-canvas-input-probe') {
        return {
          schema: 'ui-audit.canvas-input.v1', probeId: payload.probeId,
          domEvents: [{ type: 'mousedown' }, { type: 'mouseup' }],
          targetEvents: [{ type: 'click', listenerCountBefore: 3, dispatched: true }],
          semanticCalls: [{ name: 'Close' }],
        };
      }
      throw new Error(`unexpected payload: ${JSON.stringify(payload)}`);
    },
    mouse: { click: async (...args) => mouseCalls.push(args) },
  };
  session.snapshot = async () => before;

  await assert.rejects(session.closeAllowlistedPopup('CycleimpActlistYesterday', shortPolicy), error => {
    assert.equal(error.code, 'POPUP_CLOSE_NOT_STABLE');
    assert.equal(error.diagnostic.context.evaluation.classification, 'business-handled-but-not-closed');
    assert.equal(error.diagnostic.context.input.consumption.classification, 'target-click-consumed');
    assert.equal(error.diagnostic.context.input.preflight.topmost.ownerView, 'CycleimpActlistYesterday');
    assert.equal(error.diagnostic.context.samples.length > 0, true);
    return true;
  });
  assert.equal(mouseCalls.length, 1);
});

test('safe startup popups are deduplicated and ordered by authoritative sort', () => {
  const ordered = orderPopupQueue(['DailyActTipView', 'OnHookMainView', 'DailyActTipView'], policy);
  assert.deepEqual(ordered.map(item => item.view), ['OnHookMainView', 'DailyActTipView']);
  const plan = planPopupDrain(ordered, policy, { observedTopFirst: true });
  assert.equal(plan.pass, true);
  assert.equal(plan.steps.every(step => step.entry.closeProtocols.length === 0 && step.entry.closeWrites.length === 0), true);
});

test('congratulation popup closes below the reward list instead of clicking an item at center', () => {
  const decision = decidePopup(policy, 'CongratulationObtainView');
  assert.deepEqual(decision.entry.safeClose.point, { x: 360, y: 1100 });
  assert.equal(decision.entry.dangerousNodes.some(item => item.node === 'bt_use'), true);
});

test('dangerous popup blocks the drain before a close action is fabricated', () => {
  for (const view of ['ItemUseView', 'PartnerAwakeShowView', 'kfStageShowView']) {
    const plan = planPopupDrain([{ view }], policy, { observedTopFirst: true });
    assert.equal(plan.pass, false);
    assert.equal(plan.blockedBy.action, 'forbid');
    assert.equal(plan.blockedBy.entry.safeClose, null);
  }
});
