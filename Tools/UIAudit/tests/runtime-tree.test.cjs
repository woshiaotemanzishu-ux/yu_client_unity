'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const { NODE_SCHEMA, normalizeRuntimeSources, findNodes } = require('../lib/runtime-tree.cjs');

const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-sources.json'), 'utf8'));
const ownerBindingFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-owner-bindings.json'), 'utf8'));
const runtimeOverlayFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-overlays.json'), 'utf8'));

test('loaded, managed and Laya.stage sources normalize into one node schema', () => {
  const snapshot = normalizeRuntimeSources(fixture);
  assert.deepEqual(snapshot.sources, { loadedViews: 2, managedViews: 1, stageNodes: 7, runtimeOverlays: 0 });
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

test('stage normalization retains render order, effective alpha, hit policy and mask diagnostics', () => {
  const raw = structuredClone(fixture);
  const target = raw.stage.nodes.find(node => node.name === 'close_btn');
  Object.assign(target, {
    childIndex: target.indexPath[target.indexPath.length - 1],
    alpha: 0.5,
    effectiveAlpha: 0.25,
    zOrder: 7,
    hitTestPrior: true,
    mouseState: 2,
    mask: {
      name: 'modal_mask', type: 'Sprite', visible: true, alpha: 1,
      bounds: { x: 0, y: 0, width: 720, height: 1280 }, hitTestCenter: true,
    },
  });
  const snapshot = normalizeRuntimeSources(raw);
  const node = findNodes(snapshot, { source: 'laya-stage', view: 'ItemUseView', name: 'close_btn' })[0];
  assert.equal(node.schema, 'ui-audit.runtime-node.v3');
  assert.equal(node.childIndex, target.childIndex);
  assert.equal(node.effectiveAlpha, 0.25);
  assert.equal(node.zOrder, 7);
  assert.equal(node.interaction.hitTestPrior, true);
  assert.equal(node.interaction.mouseState, 2);
  assert.equal(node.state.mask.name, 'modal_mask');
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

test('a registered cached view with open=false is normalized but excluded from visibleViews', () => {
  const cached = structuredClone(ownerBindingFixture);
  const popup = cached.loaded.find(view => view.name === 'CycleimpActlistYesterday');
  Object.assign(popup, {
    stagePath: [], visible: true, loaded: true, open: false,
    instances: [{ source: 'RuntimeRegistry', key: 'root_42' }],
  });
  cached.managed.views[0].meta = {
    ...cached.managed.views[0].meta,
    stagePath: [], visible: true, loaded: true, open: false,
    instances: [{ source: 'RuntimeRegistry', key: 'root_42' }],
  };
  cached.stage.nodes = cached.stage.nodes.filter(row => !(Array.isArray(row.indexPath)
    && row.indexPath.length >= 2 && row.indexPath[0] === 0 && row.indexPath[1] === 1));

  const snapshot = normalizeRuntimeSources(cached);
  const loaded = findNodes(snapshot, { source: 'loaded-view', view: 'CycleimpActlistYesterday', visible: false })[0];
  assert.equal(snapshot.visibleViews.includes('CycleimpActlistYesterday'), false);
  assert.equal(loaded.visible, true);
  assert.equal(loaded.displayed, false);
  assert.equal(loaded.state.dataIdentity.lifecycle.open, false);
  assert.deepEqual(loaded.identity.owner.instances, [{ source: 'RuntimeRegistry', key: 'root_42' }]);
});

test('runtime overlay source normalizes manager ownership and current view without dropping diagnostics', () => {
  const raw = structuredClone(ownerBindingFixture);
  const overlay = runtimeOverlayFixture.runtimeOverlays[0];
  raw.stage.overlays = [overlay];
  raw.stage.nodes.push({
    name: 'o', type: 'Image', runtimeClass: 'laya.ui.Image', path: overlay.nodePath,
    indexPath: overlay.nodeStagePath, depth: 3, visible: true, displayedInStage: true,
    bounds: { x: -5, y: -5, width: 730, height: 1290 }, mouseEnabled: true, mouseState: 2,
    systemOverlay: overlay, hitArea: overlay.node.hitArea, eventListeners: overlay.node.eventListeners,
  });
  const snapshot = normalizeRuntimeSources(raw);
  assert.equal(snapshot.sources.runtimeOverlays, 1);
  assert.equal(snapshot.visibleViews.includes('DailyActTipView'), true);
  assert.equal(snapshot.runtimeOverlays[0].currentView.instanceKey, 'root_upper');
  const node = snapshot.nodes.find(value => value.source === 'laya-stage' && value.name === 'o');
  assert.equal(node.identity.systemOverlay.authority, 'ViewManager.GetBackGround');
  assert.equal(node.interaction.eventListeners[0].type, 'click');
  assert.equal(node.interaction.hitArea.width, 730);
});
