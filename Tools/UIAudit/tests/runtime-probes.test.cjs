'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { normalizedNode } = require('../lib/runtime-tree.cjs');
const { evaluateSoundAssertions, probeRenderTexture, waitRenderTextureReady } = require('../lib/runtime-probes.cjs');

test('sound assertions count logical calls and reject duplicates', () => {
  const trace = { events: [
    { method: 'PlaySoundEffect', key: 'ui/2_dianji' },
    { method: 'PlaySceneSound', key: 'scene/role' },
  ] };
  assert.equal(evaluateSoundAssertions(trace, {
    required: [{ method: 'PlaySoundEffect', key: 'ui/2_dianji', min: 1, max: 1 }],
    forbidden: [{ key: 'ui/duplicate' }],
  }).pass, true);
  trace.events.push({ method: 'PlaySoundEffect', key: 'ui/2_dianji' });
  assert.equal(evaluateSoundAssertions(trace, { required: [{ key: 'ui/2_dianji', max: 1 }] }).pass, false);
});

test('render readiness uses actual non-transparent RenderTexture pixels and stable samples', async () => {
  const pixels = new Uint8Array(4 * 4 * 4);
  for (let index = 0; index < 8; index++) pixels[index * 4 + 3] = 255;
  const texture = { width: 4, height: 4, getData: () => pixels };
  const target = { model: { renderTexture: texture } };
  global.window = { Laya: { stage: { _children: [target] } } };
  global.Laya = global.window.Laya;
  const page = { evaluate: async (fn, arg) => fn(arg) };
  const snapshot = { nodes: [normalizedNode({ name: 'model', visible: true, indexPath: [0, 0], bounds: { x: 0, y: 0, width: 10, height: 10 } }, { source: 'laya-stage', view: 'FixtureView', path: 'model' })] };
  const spec = { selector: { name: 'model' }, propertyPath: ['model'], minNonTransparentPixels: 8, stableFrames: 2, timeoutMs: 100 };
  try {
    const sample = await probeRenderTexture(page, snapshot, spec);
    assert.equal(sample.nonTransparentPixels, 8);
    const ready = await waitRenderTextureReady(page, snapshot, spec, async () => {});
    assert.equal(ready.pass, true);
    assert.equal(ready.samples.length, 2);
  } finally {
    delete global.window;
    delete global.Laya;
  }
});
