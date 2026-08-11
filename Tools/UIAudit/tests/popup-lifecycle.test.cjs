'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { normalizeRuntimeSources, findNodes } = require('../lib/runtime-tree.cjs');
const { popupCloseStability } = require('../lib/popup-policy.cjs');
const {
  createPopupInstanceRef,
  observePopupLifecycle,
  classifyPopupCloseFailure,
} = require('../lib/popup-lifecycle.cjs');

const rawOpen = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-owner-bindings.json'), 'utf8'));
const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'popup-close-lifecycle.json'), 'utf8'));
const stabilityPolicy = { kind: 'absent-advancing-laya-frames', consecutiveFrames: 2, timeoutMs: 2000, pollMs: 50 };

function rawSnapshot(state) {
  const raw = structuredClone(rawOpen);
  raw.stage.meta.frameToken = state.frameToken;
  const loaded = raw.loaded.find(item => item.name === fixture.view);
  Object.assign(loaded, {
    open: state.loaded.open,
    visible: state.loaded.visible,
    stagePath: state.loaded.stagePath,
    instances: [{ source: 'RuntimeRegistry', key: state.loaded.instanceKey }],
  });
  const managed = raw.managed.views.find(item => item.meta.name === fixture.view);
  Object.assign(managed.meta, {
    open: state.managed.open,
    visible: state.managed.visible,
    loaded: true,
    stagePath: state.managed.stagePath,
    instances: [{ source: 'RuntimeRegistry', key: state.managed.instanceKey }],
  });
  managed.nodeTree.visible = state.managed.visible;
  managed.nodeTree.effectiveVisible = state.managed.visible;
  if (!state.stage.present) {
    raw.stage.nodes = raw.stage.nodes.filter(row => !(Array.isArray(row.indexPath)
      && row.indexPath.length >= 2 && row.indexPath[0] === 0 && row.indexPath[1] === 1));
  } else {
    const root = raw.stage.nodes.find(row => row.ownerIdentity && row.ownerIdentity.isRoot
      && row.ownerIdentity.view === fixture.view);
    root.visible = state.stage.visible;
    root.displayedInStage = state.stage.visible;
    root.ownerIdentity.instances = [{ source: 'RuntimeRegistry', key: state.stage.instanceKey }];
    for (const row of raw.stage.nodes.filter(item => Array.isArray(item.indexPath)
      && item.indexPath.length > 2 && item.indexPath[0] === 0 && item.indexPath[1] === 1)) {
      row.visible = state.stage.visible;
      row.displayedInStage = state.stage.visible;
      if (Array.isArray(row.bindings)) {
        for (const binding of row.bindings) binding.instanceKey = state.stage.instanceKey;
      }
    }
  }
  return normalizeRuntimeSources(raw);
}

function instanceRef() {
  const snapshot = normalizeRuntimeSources(rawOpen);
  const target = findNodes(snapshot, fixture.selector)[0];
  return createPopupInstanceRef(snapshot, fixture.view, fixture.selector, target);
}

function lifecycleSamples(states) {
  const ref = instanceRef();
  return states.map(state => {
    const snapshot = rawSnapshot(state);
    return { stage: snapshot.stage, lifecycle: observePopupLifecycle(snapshot, ref) };
  });
}

test('a closing instance remains present during stage teardown, then a visible cached root counts as closed', () => {
  const samples = lifecycleSamples(fixture.stableClose);
  assert.equal(samples[0].lifecycle.phase, 'closing');
  assert.equal(samples[0].lifecycle.present, true);
  assert.equal(samples[1].lifecycle.phase, 'closed-cached');
  assert.equal(samples[1].lifecycle.present, false);
  assert.equal(samples[1].lifecycle.sources.some(source => source.source === 'loaded-view'
    && source.visible && source.open === false), true);

  const result = popupCloseStability(samples, fixture.view, stabilityPolicy);
  assert.equal(result.pass, true);
  assert.equal(result.stableFrames, 2);
  assert.deepEqual(result.frameTokens, [501, 502, 503]);
  assert.deepEqual(result.phases, ['closing', 'closed-cached', 'closed-cached']);
  assert.equal(result.classification, 'closed-stable');
});

test('an open exact instance after the click is classified as click-not-consumed', () => {
  const samples = lifecycleSamples(fixture.clickNotConsumed);
  const result = popupCloseStability(samples, fixture.view, stabilityPolicy);
  assert.equal(result.pass, false);
  assert.equal(result.stableFrames, 0);
  assert.equal(result.classification, 'click-not-consumed');
  assert.equal(classifyPopupCloseFailure(samples), 'click-not-consumed');
});

test('a dispatched bound-field click with an unchanged open lifecycle is business-handled-but-not-closed', () => {
  const samples = lifecycleSamples(fixture.clickNotConsumed);
  const input = {
    consumption: { pass: true, classification: 'target-click-consumed' },
    evidence: {
      targetEvents: [{ type: 'click', listenerCountBefore: 3, dispatched: true }],
      semanticCalls: [{ name: 'Close' }],
    },
  };
  const result = popupCloseStability(samples, fixture.view, stabilityPolicy, { input });
  assert.equal(result.pass, false);
  assert.equal(result.classification, 'business-handled-but-not-closed');
  assert.equal(classifyPopupCloseFailure(samples, input), 'business-handled-but-not-closed');
});

test('a newly registered same-view instance is requeued, not mistaken for the clicked instance closing', () => {
  const samples = lifecycleSamples(fixture.requeued);
  assert.equal(samples[0].lifecycle.present, false);
  assert.equal(samples[1].lifecycle.phase, 'requeued');
  assert.equal(samples[1].lifecycle.requeued, true);
  const result = popupCloseStability(samples, fixture.view, stabilityPolicy);
  assert.equal(result.pass, false);
  assert.equal(result.stableFrames, 0);
  assert.equal(result.classification, 'requeued');
});

test('the same cached instance reopening after absence is also classified as requeued', () => {
  const states = [fixture.stableClose[1], fixture.clickNotConsumed[0]];
  const result = popupCloseStability(lifecycleSamples(states), fixture.view, stabilityPolicy);
  assert.equal(result.pass, false);
  assert.equal(result.classification, 'requeued');
});

test('duplicate Laya frame tokens do not satisfy the consecutive advancing-frame gate', () => {
  const repeated = [fixture.stableClose[1], { ...fixture.stableClose[2], frameToken: 502 }];
  const result = popupCloseStability(lifecycleSamples(repeated), fixture.view, stabilityPolicy);
  assert.equal(result.pass, false);
  assert.equal(result.stableFrames, 1);
  assert.equal(result.classification, 'frame-not-advancing');
});
