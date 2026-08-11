'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const {
  loadPopupPolicy,
  decidePopup,
  orderPopupQueue,
  planPopupDrain,
  assertSafePopupDecision,
  popupCloseStability,
} = require('../lib/popup-policy.cjs');
const { normalizeRuntimeSources } = require('../lib/runtime-tree.cjs');
const { HeadlessUiSession } = require('../lib/session.cjs');

const policy = loadPopupPolicy(path.join(__dirname, '..', 'policies', 'startup-popups.json'));
const cycleimpFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'startup-popup-cycleimp-yesterday.json'), 'utf8'));
const runtimeOwnerBindingFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-owner-bindings.json'), 'utf8'));

test('unknown popup is an unconditional hard stop', () => {
  assert.equal(policy.entries.length, 17);
  const decision = decidePopup(policy, 'NeverAuditedPopup');
  assert.equal(decision.action, 'unknown-hard-stop');
  assert.throws(() => assertSafePopupDecision(decision), /POPUP_HARD_STOP/);
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
        return { x: 0, y: 0, width: 720, height: 1280, logicalWidth: 720, logicalHeight: 1280 };
      }
      if (payload && payload.indexPath) return { applicable: true, pass: true, reason: null };
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

test('safe startup popups are deduplicated and ordered by authoritative sort', () => {
  const ordered = orderPopupQueue(['DailyActTipView', 'OnHookMainView', 'DailyActTipView'], policy);
  assert.deepEqual(ordered.map(item => item.view), ['OnHookMainView', 'DailyActTipView']);
  const plan = planPopupDrain(ordered, policy, { observedTopFirst: true });
  assert.equal(plan.pass, true);
  assert.equal(plan.steps.every(step => step.entry.closeProtocols.length === 0 && step.entry.closeWrites.length === 0), true);
});

test('dangerous popup blocks the drain before a close action is fabricated', () => {
  for (const view of ['ItemUseView', 'PartnerAwakeShowView', 'kfStageShowView']) {
    const plan = planPopupDrain([{ view }], policy, { observedTopFirst: true });
    assert.equal(plan.pass, false);
    assert.equal(plan.blockedBy.action, 'forbid');
    assert.equal(plan.blockedBy.entry.safeClose, null);
  }
});
