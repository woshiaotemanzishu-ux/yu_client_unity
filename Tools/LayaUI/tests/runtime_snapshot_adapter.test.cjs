'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { adaptRuntimeSnapshot } = require('../runtime_snapshot_adapter.cjs');

test('adapts one visible managed runtime subtree with final geometry and state', () => {
  const base = { source: 'managed-view', view: 'BaseWindowSkin', displayed: true, anchor: { x: 0, y: 0 }, pivot: { x: 0, y: 0 }, scale: { x: 1, y: 1 }, alpha: 1, state: {} };
  const result = adaptRuntimeSnapshot({ capturedAt: 'now', stage: { width: 720, height: 1280 }, nodes: [
    { ...base, path: 'Base/OutWardBaseView[1]', parentPath: 'Base', name: 'OutWardBaseView', type: 'Sprite', visible: true, bounds: { x: 0, y: 80, width: 720, height: 992 } },
    { ...base, path: 'Base/OutWardBaseView[1]/name[0]', parentPath: 'Base/OutWardBaseView[1]', name: 'name', type: 'Label', text: '垂神翼影', visible: true, bounds: { x: 20, y: 100, width: 120, height: 30 } },
    { ...base, path: 'Base/hidden[2]', parentPath: 'Base', name: 'Other', type: 'Sprite', visible: false, bounds: { x: 0, y: 0, width: 10, height: 10 } },
  ] }, { rootView: 'BaseWindowSkin', rootName: 'OutWardBaseView', viewName: 'Candidate' });
  assert.equal(result.views[0].nodeCount, 2);
  assert.equal(result.views[0].nodeTree.children[0].x, 20);
  assert.equal(result.views[0].nodeTree.children[0].y, 20);
  assert.equal(result.views[0].nodeTree.children[0].runtime.text, '垂神翼影');
  assert.equal(result.metrics.runtimeGeometryNodes, 2);
});

test('rejects an ambiguous runtime root instead of guessing', () => {
  const node = { source: 'managed-view', view: 'BaseWindowSkin', path: 'x', parentPath: null, name: 'Root', visible: true, displayed: true, bounds: { x: 0, y: 0, width: 1, height: 1 } };
  assert.throws(() => adaptRuntimeSnapshot({ nodes: [node, { ...node, path: 'y' }] }, { rootView: 'BaseWindowSkin', rootName: 'Root', viewName: 'Candidate' }), /ROOT_IDENTITY_MISMATCH/);
});
