'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const { NODE_SCHEMA, normalizeRuntimeSources, findNodes } = require('../lib/runtime-tree.cjs');

const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-sources.json'), 'utf8'));

test('loaded, managed and Laya.stage sources normalize into one node schema', () => {
  const snapshot = normalizeRuntimeSources(fixture);
  assert.deepEqual(snapshot.sources, { loadedViews: 2, managedViews: 1, stageNodes: 7 });
  assert.deepEqual(new Set(snapshot.nodes.map(node => node.source)), new Set(['loaded-view', 'managed-view', 'laya-stage']));
  assert.equal(snapshot.nodes.every(node => node.schema === NODE_SCHEMA), true);
  assert.equal(snapshot.visibleViews.includes('ItemUseView'), true);
  assert.equal(findNodes(snapshot, { source: 'laya-stage', view: 'ItemUseView', name: 'close_btn' }).length, 1);
});

test('runtime selectors match an exact data identity subset', () => {
  const withIdentity = structuredClone(fixture);
  withIdentity.stage.nodes.push({
    name: 'fashion_group', type: 'FashionItem', view: 'FashionMainView', path: 'stage[0]/fashion_group[0]',
    indexPath: [0, 1], depth: 1, visible: true, displayedInStage: true,
    dataIdentity: { fashion_id: 12010008, pos_id: 1, name: 'sweetheart' },
    bounds: { x: 10, y: 10, width: 100, height: 100 },
  });
  const snapshot = normalizeRuntimeSources(withIdentity);
  assert.equal(findNodes(snapshot, { source: 'laya-stage', name: 'fashion_group', dataIdentity: { fashion_id: 12010008 } }).length, 1);
  assert.equal(findNodes(snapshot, { source: 'laya-stage', name: 'fashion_group', dataIdentity: { fashion_id: 1 } }).length, 0);
});
