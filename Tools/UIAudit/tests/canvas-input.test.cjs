'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const { normalizeRuntimeSources } = require('../lib/runtime-tree.cjs');
const { resolveTarget, clickRuntimeTarget, dragRuntimeTarget } = require('../lib/canvas-input.cjs');

const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-sources.json'), 'utf8'));
const ownerBindingFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-owner-bindings.json'), 'utf8'));
const snapshot = normalizeRuntimeSources(fixture);
const ownerBindingSnapshot = normalizeRuntimeSources(ownerBindingFixture);
const canvasMetrics = { x: 0, y: 0, width: 720, height: 1280, logicalWidth: 720, logicalHeight: 1280 };

function fakePage() {
  const calls = [];
  return {
    calls,
    viewport: () => ({ width: 720, height: 1280 }),
    evaluate: async () => ({ applicable: true, pass: true, reason: null }),
    mouse: {
      click: async (...args) => calls.push(['click', ...args]),
      move: async (...args) => calls.push(['move', ...args]),
      down: async (...args) => calls.push(['down', ...args]),
      up: async (...args) => calls.push(['up', ...args]),
    },
  };
}

test('canvas click verifies exact identity, viewport and runtime hit before mouse input', async () => {
  const page = fakePage();
  const result = await clickRuntimeTarget(page, snapshot, { source: 'laya-stage', view: 'ItemUseView', name: 'close_btn', expectedCount: 1 }, { canvasMetrics });
  assert.equal(result.hit.pass, true);
  assert.deepEqual(page.calls[0].slice(0, 3), ['click', 594, 344]);
});

test('canvas drag starts on a hittable runtime node and always releases the mouse', async () => {
  const page = fakePage();
  const result = await dragRuntimeTarget(page, snapshot, { source: 'laya-stage', view: 'ItemUseView', name: 'enter_btn', expectedCount: 1 }, { deltaY: -100, steps: 5, canvasMetrics });
  assert.equal(result.hit.pass, true);
  assert.deepEqual(page.calls.map(call => call[0]), ['move', 'down', 'move', 'up']);
});

test('wide canvas maps logical Laya coordinates through the real DOM rectangle', async () => {
  const page = fakePage();
  page.viewport = () => ({ width: 1920, height: 1080 });
  const wideCanvas = { x: 420, y: 0, width: 1080, height: 1080, logicalWidth: 720, logicalHeight: 1280 };
  const result = await clickRuntimeTarget(page, snapshot, { source: 'laya-stage', view: 'ItemUseView', name: 'close_btn', expectedCount: 1 }, { canvasMetrics: wideCanvas });
  assert.deepEqual(result.logicalPoint, { x: 594, y: 344 });
  assert.deepEqual(result.point, { x: 1311, y: 290.25 });
});

test('owner-view plus bound-field resolves one hittable node when field and runtime names differ', () => {
  const target = resolveTarget(ownerBindingSnapshot, {
    source: 'laya-stage', ownerView: 'CycleimpActlistYesterday', boundField: '_btn_close', expectedCount: 1,
  });
  assert.equal(target.name, 'runtime_close_image');
  assert.equal(target.interaction.hitTestCenter, true);
});

test('ambiguous and missing bound-field selectors hard-stop with auditable candidates and subtree', () => {
  const ambiguous = structuredClone(ownerBindingSnapshot);
  const duplicate = structuredClone(ambiguous.nodes.find(node => node.source === 'laya-stage' && node.name === 'runtime_close_image'));
  duplicate.path = `${duplicate.path}-duplicate`;
  duplicate.indexPath = [0, 1, 1];
  ambiguous.nodes.push(duplicate);

  for (const [candidate, actual] of [[ambiguous, 2], [ownerBindingSnapshot, 0]]) {
    const boundField = actual === 0 ? '_missing_close' : '_btn_close';
    assert.throws(() => resolveTarget(candidate, {
      source: 'laya-stage', ownerView: 'CycleimpActlistYesterday', boundField, expectedCount: 1,
    }), error => {
      assert.equal(error.code, 'CANVAS_TARGET_IDENTITY_MISMATCH');
      assert.equal(error.diagnostic.actualCount, actual);
      assert.equal(error.diagnostic.subtree.total >= 2, true);
      assert.equal(error.diagnostic.candidates.length > 0, true);
      assert.match(error.diagnostic.sha256, /^[a-f0-9]{64}$/);
      return true;
    });
  }
});
