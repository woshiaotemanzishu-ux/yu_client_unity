'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const {
  loadRuntimeOverlayPolicy,
  classifyRuntimeOverlay,
  runtimeOverlayDecisions,
  runtimeOverlayViews,
} = require('../lib/runtime-overlay.cjs');

const policy = loadRuntimeOverlayPolicy(path.join(__dirname, '..', 'policies', 'runtime-overlays.json'));
const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-overlays.json'), 'utf8'));

test('ViewManager shared background resolves its authoritative current view into runtime stack', () => {
  const snapshot = { runtimeOverlays: [fixture.runtimeOverlays[0]] };
  const decision = runtimeOverlayDecisions(snapshot, policy)[0];
  assert.equal(decision.action, 'resolve-current-view');
  assert.equal(decision.view, 'DailyActTipView');
  const views = runtimeOverlayViews(snapshot, policy);
  assert.deepEqual(views.map(item => item.view), ['DailyActTipView']);
  assert.deepEqual(views[0].stagePath, [0, 2, 2]);
  assert.equal(views[0].instance[0].key, 'root_upper');
});

test('source-backed global input gate waits until its pending dictionary releases', () => {
  const waiting = classifyRuntimeOverlay(fixture.runtimeOverlays[1], policy);
  assert.equal(waiting.action, 'wait-for-release');
  assert.equal(waiting.timeoutMs, 16000);
  const released = structuredClone(fixture.runtimeOverlays[1]);
  released.gate.pendingKeys = [];
  released.gate.ready = true;
  assert.equal(classifyRuntimeOverlay(released, policy).action, 'released');
});

test('unowned interactive full-screen overlay remains an unknown hard stop', () => {
  const decision = classifyRuntimeOverlay(fixture.runtimeOverlays[2], policy);
  assert.equal(decision.action, 'unknown-hard-stop');
  assert.equal(decision.pass, false);
  assert.equal(decision.overlay.node.runtimeClass, 'laya.display.Sprite');
});
