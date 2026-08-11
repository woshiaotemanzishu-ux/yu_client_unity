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
