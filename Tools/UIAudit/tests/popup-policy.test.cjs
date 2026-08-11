'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('path');
const { loadPopupPolicy, decidePopup, orderPopupQueue, planPopupDrain, assertSafePopupDecision } = require('../lib/popup-policy.cjs');

const policy = loadPopupPolicy(path.join(__dirname, '..', 'policies', 'startup-popups.json'));

test('unknown popup is an unconditional hard stop', () => {
  assert.equal(policy.entries.length, 16);
  const decision = decidePopup(policy, 'NeverAuditedPopup');
  assert.equal(decision.action, 'unknown-hard-stop');
  assert.throws(() => assertSafePopupDecision(decision), /POPUP_HARD_STOP/);
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
