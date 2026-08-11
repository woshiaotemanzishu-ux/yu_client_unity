'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const { normalizeRuntimeSources } = require('../lib/runtime-tree.cjs');
const { clickRuntimeTarget, dragRuntimeTarget } = require('../lib/canvas-input.cjs');

const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-sources.json'), 'utf8'));
const snapshot = normalizeRuntimeSources(fixture);
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
