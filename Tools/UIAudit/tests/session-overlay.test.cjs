'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const { HeadlessUiSession } = require('../lib/session.cjs');
const { loadRuntimeOverlayPolicy } = require('../lib/runtime-overlay.cjs');
const { loadPopupPolicy } = require('../lib/popup-policy.cjs');

const runtimePolicy = loadRuntimeOverlayPolicy(path.join(__dirname, '..', 'policies', 'runtime-overlays.json'));
const popupPolicy = loadPopupPolicy(path.join(__dirname, '..', 'policies', 'startup-popups.json'));
const overlays = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-overlays.json'), 'utf8')).runtimeOverlays;
const inputNodes = [
  { source: 'laya-stage', name: 'account', visible: true, text: '111111' },
  { source: 'laya-stage', name: 'password', visible: true, text: '111111' },
];
const login = { visibleViews: ['LoginView'], nodes: inputNodes, runtimeOverlays: [], stage: {} };
const main = { visibleViews: ['MainUITopView'], nodes: [], runtimeOverlays: [], stage: {} };

function configuredSession(sequence) {
  const mouseCalls = [];
  const session = new HeadlessUiSession({});
  session.page = { mouse: { click: async (...args) => mouseCalls.push(args) } };
  session.typeAt = async () => {};
  session.snapshot = async () => {
    const next = sequence.shift();
    if (!next) throw new Error('unexpected extra snapshot');
    return structuredClone(next);
  };
  return { session, mouseCalls };
}

test('login session waits for the source-backed global input gate and sends no overlay click', async () => {
  const waiting = { ...main, runtimeOverlays: [overlays[1]] };
  const { session, mouseCalls } = configuredSession([login, login, waiting, main, main, main, main]);
  const result = await session.loginAndReachMainUi({
    account: '111111', password: '111111', popupPolicy, runtimeOverlayPolicy: runtimePolicy,
    postSubmitWaitMs: 1, pollMs: 1, maxIterations: 6,
  });
  assert.equal(result.visibleViews.includes('MainUITopView'), true);
  assert.equal(session.events.filter(event => event.kind === 'runtime-overlay-wait').length, 1);
  assert.equal(mouseCalls.length, 1, 'only the explicit login submit click is allowed');
});

test('unknown full-screen overlay hard-stops before any popup or route click', async () => {
  const blocked = { ...main, runtimeOverlays: [overlays[2]] };
  const { session, mouseCalls } = configuredSession([login, login, blocked]);
  await assert.rejects(session.loginAndReachMainUi({
    account: '111111', password: '111111', popupPolicy, runtimeOverlayPolicy: runtimePolicy,
    postSubmitWaitMs: 1, pollMs: 1, maxIterations: 2,
  }), error => {
    assert.equal(error.code, 'RUNTIME_OVERLAY_UNKNOWN');
    assert.equal(error.diagnostic.context.decision.overlay.node.runtimeClass, 'laya.display.Sprite');
    return true;
  });
  assert.equal(mouseCalls.length, 1, 'unknown overlay causes no additional input after login submit');
});
