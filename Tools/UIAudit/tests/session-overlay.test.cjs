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

test('login loading view defers a source-backed popup instead of being treated as an unknown popup', async () => {
  const loadingBackground = structuredClone(overlays[0]);
  loadingBackground.currentView.name = 'LoginLoadingView';
  loadingBackground.currentView.layoutFile = 'LoginLoadingView';
  loadingBackground.currentView.constructorName = 'LoginLoadingView';
  loadingBackground.currentView.stagePath = [0, 2, 3, 2];
  const starRoot = {
    source: 'laya-stage',
    visible: true,
    displayed: true,
    indexPath: [0, 2, 2, 1],
    path: 'Stage[0]/UIRoot[2]/Activity[2]/StarEquipUpMasterView[1]',
    childIndex: 1,
    zOrder: 0,
    identity: {
      owner: { isRoot: true, view: 'StarEquipUpMasterView', instances: [{ key: 'star-master' }] },
    },
  };
  const covered = {
    ...main,
    visibleViews: ['MainUITopView', 'LoginLoadingView', 'StarEquipUpMasterView'],
    nodes: [starRoot],
    runtimeOverlays: [loadingBackground],
  };
  const { session, mouseCalls } = configuredSession([login, login, covered, main, main, main, main]);
  const result = await session.loginAndReachMainUi({
    account: '111111', password: '111111', popupPolicy, runtimeOverlayPolicy: runtimePolicy,
    postSubmitWaitMs: 1, pollMs: 1, maxIterations: 6,
  });
  assert.equal(result.visibleViews.includes('MainUITopView'), true);
  assert.equal(session.events.filter(event => event.kind === 'popup-drain-deferred-by-passive-overlay').length, 1);
  assert.equal(mouseCalls.length, 1, 'the transient loading overlay causes no click after login submit');
});

test('read-only login may capture beneath ItemUseView without clicking or advancing its queue', async () => {
  const itemUseRoot = {
    source: 'laya-stage', visible: true, displayed: true,
    indexPath: [0, 2, 3, 2], path: 'Stage[0]/UIRoot[2]/Top[3]/ItemUseView[2]',
    childIndex: 2, zOrder: 0,
    identity: { owner: { isRoot: true, view: 'ItemUseView', instances: [{ key: 'item-use' }] } },
  };
  const blocked = {
    ...main,
    visibleViews: ['MainUITopView', 'ItemUseView'],
    nodes: [itemUseRoot],
  };
  const { session, mouseCalls } = configuredSession([login, login, blocked]);
  const result = await session.loginAndReachMainUi({
    account: '111111', password: '111111', popupPolicy, runtimeOverlayPolicy: runtimePolicy,
    allowBlockedReadOnly: true, postSubmitWaitMs: 1, pollMs: 1, maxIterations: 2,
  });
  assert.equal(result.readOnlyBlockedBy.view, 'ItemUseView');
  assert.equal(session.events.filter(event => event.kind === 'login-read-only-blocked-snapshot').length, 1);
  assert.equal(mouseCalls.length, 1, 'only the login submit click is allowed');
});

test('function-open presentation is allowed to close on its own timer without automation input', async () => {
  const background = structuredClone(overlays[0]);
  background.currentView.name = 'FunctionOpenAutoView';
  background.currentView.layoutFile = 'FunctionOpenAutoView';
  background.currentView.constructorName = 'FunctionOpenAutoView';
  background.currentView.hashCode = '813';
  background.currentView.stagePath = [0, 2, 2, 3];
  const root = {
    source: 'laya-stage', visible: true, displayed: true,
    indexPath: [0, 2, 2, 3], path: 'Stage[0]/UIRoot[2]/Activity[2]/FunctionOpenAutoView[3]',
    childIndex: 3, zOrder: 0,
    identity: { owner: { isRoot: true, view: 'FunctionOpenAutoView', instances: [{ key: 'function-open' }] } },
  };
  const waiting = {
    ...main,
    visibleViews: ['MainUITopView', 'FunctionOpenAutoView'],
    nodes: [root],
    runtimeOverlays: [background],
  };
  const { session, mouseCalls } = configuredSession([login, login, waiting, main, main, main, main]);
  const result = await session.loginAndReachMainUi({
    account: '111111', password: '111111', popupPolicy, runtimeOverlayPolicy: runtimePolicy,
    postSubmitWaitMs: 1, pollMs: 1, maxIterations: 6,
  });
  assert.equal(result.visibleViews.includes('MainUITopView'), true);
  assert.equal(session.events.filter(event => event.kind === 'popup-wait-for-natural-release').length, 1);
  assert.equal(mouseCalls.length, 1, 'only the login submit click is allowed');
});
