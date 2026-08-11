'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const { NODE_SCHEMA, normalizeRuntimeSources, findNodes } = require('../lib/runtime-tree.cjs');

const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-sources.json'), 'utf8'));
const ownerBindingFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-owner-bindings.json'), 'utf8'));

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

test('loaded and managed stagePath assign a non-View-suffixed owner to its complete stage subtree', () => {
  const snapshot = normalizeRuntimeSources(ownerBindingFixture);
  const root = findNodes(snapshot, {
    source: 'laya-stage', ownerView: 'CycleimpActlistYesterday', runtimeName: 'CycleimpActlistYesterday',
  });
  const close = findNodes(snapshot, {
    source: 'laya-stage', ownerView: 'CycleimpActlistYesterday', boundField: '_btn_close',
  });
  assert.equal(root.length, 1);
  assert.equal(root[0].identity.owner.isRoot, true);
  assert.equal(root[0].identity.owner.evidence.some(item => item.source === 'loaded-view-stage-path'), true);
  assert.equal(close.length, 1);
  assert.equal(close[0].name, 'runtime_close_image');
  assert.deepEqual(close[0].identity.bindings.map(binding => binding.field), ['_btn_close']);
  assert.equal(findNodes(snapshot, {
    source: 'laya-stage', ownerView: 'CycleimpActlistYesterday', runtimeName: '_btn_close',
  }).length, 0);
});

test('a detached cached view with an empty stagePath cannot claim the stage root', () => {
  const withDetached = structuredClone(ownerBindingFixture);
  withDetached.loaded.unshift({
    name: 'DetachedCachedView', stagePath: [], visible: false, loaded: true, open: false,
  });
  const snapshot = normalizeRuntimeSources(withDetached);
  const stageRoot = snapshot.nodes.find(node => node.source === 'laya-stage' && node.indexPath.length === 1);
  assert.equal(stageRoot.view, null);
  assert.equal(findNodes(snapshot, {
    source: 'laya-stage', ownerView: 'CycleimpActlistYesterday', boundField: '_btn_close',
  }).length, 1);
});
