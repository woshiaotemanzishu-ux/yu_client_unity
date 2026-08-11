'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const { normalizeRuntimeSources } = require('../lib/runtime-tree.cjs');
const {
  resolveTarget,
  resolveTargetAncestor,
  logicalToDomPoint,
  domToLogicalPoint,
  classifyPreInput,
  classifyInputConsumption,
  clickRuntimeTarget,
  dragRuntimeTarget,
} = require('../lib/canvas-input.cjs');

const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-sources.json'), 'utf8'));
const ownerBindingFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-owner-bindings.json'), 'utf8'));
const inputFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'canvas-input-consumption.json'), 'utf8'));
const snapshot = normalizeRuntimeSources(fixture);
const ownerBindingSnapshot = normalizeRuntimeSources(ownerBindingFixture);
const canvasMetrics = { x: 0, y: 0, width: 720, height: 1280, logicalWidth: 720, logicalHeight: 1280 };

function fakePage() {
  const calls = [];
  const evaluations = [];
  return {
    calls,
    evaluations,
    viewport: () => ({ width: 720, height: 1280 }),
    evaluate: async (_function, payload) => {
      evaluations.push(payload);
      if (payload && payload.operation === 'inspect-canvas-input') {
        const target = {
          path: payload.indexPath.join('/'), indexPath: payload.indexPath,
          ownerView: payload.selector.ownerView || payload.selector.view || null,
          hitAtPoint: true,
        };
        return {
          schema: 'ui-audit.canvas-input.v1', applicable: true,
          targetResolution: { actualCount: 1, currentIndexPath: payload.indexPath },
          target, targetChain: [target], topmost: target, topmostChain: [target], capture: null,
          mapping: { roundTripPass: true, pointInsideCanvas: true, domCanvasTop: true },
        };
      }
      if (payload && payload.operation === 'install-canvas-input-probe') {
        return { pass: true, probeId: payload.probeId, listenersBefore: { click: 1 } };
      }
      if (payload && payload.operation === 'finish-canvas-input-probe') {
        return {
          schema: 'ui-audit.canvas-input.v1', probeId: payload.probeId,
          domEvents: [{ type: 'mousedown' }, { type: 'mouseup' }, { type: 'click' }],
          targetEvents: [{ type: 'click', listenerCountBefore: 1, dispatched: true }],
          semanticCalls: [{ name: 'Close' }],
        };
      }
      throw new Error(`unexpected page.evaluate payload: ${JSON.stringify(payload)}`);
    },
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

test('stage selector index is applied once before live exact-path resolution', async () => {
  const multi = structuredClone(snapshot);
  const first = multi.nodes.find(node => node.source === 'laya-stage' && node.view === 'ItemUseView' && node.name === 'close_btn');
  const duplicate = structuredClone(first);
  duplicate.path = `${first.path}-second`;
  duplicate.indexPath = [...first.indexPath.slice(0, -1), Number(first.indexPath.at(-1)) + 10];
  multi.nodes.push(duplicate);
  const page = fakePage();
  await clickRuntimeTarget(page, multi, {
    source: 'laya-stage', view: 'ItemUseView', name: 'close_btn', expectedCount: 2, index: 1,
  }, { canvasMetrics });
  const inspect = page.evaluations.find(payload => payload.operation === 'inspect-canvas-input');
  const install = page.evaluations.find(payload => payload.operation === 'install-canvas-input-probe');
  assert.equal(inspect.liveCandidateIndex, 0);
  assert.equal(install.liveCandidateIndex, 0);
  assert.deepEqual(inspect.indexPath, duplicate.indexPath);
});

test('text identity may resolve only to one explicitly declared interactive ancestor', async () => {
  const identitySnapshot = {
    stage: { width: 720, height: 1280 },
    nodes: [
      {
        schema: 'ui-audit.runtime-node.v3', source: 'laya-stage', view: 'BaseWindowSkin',
        path: 'stage/item1', indexPath: [0, 1], name: 'item1', type: 'WindowComponentTabButtonOne',
        visible: true, displayed: true, bounds: { x: 150, y: 1077, width: 150, height: 90 },
        interaction: { mouseEnabled: true, disabled: false, hitTestCenter: true }, identity: {},
      },
      {
        schema: 'ui-audit.runtime-node.v3', source: 'laya-stage', view: 'BaseWindowSkin',
        path: 'stage/item1/skin/group/labelDisplay', indexPath: [0, 1, 0, 1, 1],
        name: 'labelDisplay', type: 'Label', text: '垂神翼影', visible: true, displayed: true,
        bounds: { x: 181, y: 1089, width: 88, height: 63 },
        interaction: { mouseEnabled: false, disabled: false, hitTestCenter: true }, identity: {},
      },
    ],
  };
  const selector = { source: 'laya-stage', name: 'labelDisplay', text: '垂神翼影', expectedCount: 1 };
  const identity = resolveTarget(identitySnapshot, selector, { allowNonInteractive: true });
  const ancestorSpec = { name: 'item1', type: 'WindowComponentTabButtonOne', maxDepth: 4 };
  assert.equal(resolveTargetAncestor(identitySnapshot, identity, ancestorSpec).name, 'item1');
  const page = fakePage();
  const result = await clickRuntimeTarget(page, identitySnapshot, selector, {
    canvasMetrics, targetAncestor: ancestorSpec,
  });
  assert.equal(result.identityTarget.text, '垂神翼影');
  assert.equal(result.target.type, 'WindowComponentTabButtonOne');
  assert.throws(() => resolveTargetAncestor(identitySnapshot, identity, { ...ancestorSpec, type: 'WrongType' }),
    error => error.code === 'CANVAS_TARGET_ANCESTOR_IDENTITY_MISMATCH');
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

test('canvas transform mapping round-trips logical coordinates in a scrolled wide viewport', () => {
  const metrics = {
    x: 420, y: 0, width: 1080, height: 1080, logicalWidth: 720, logicalHeight: 1280,
    scrollX: 20, scrollY: 40,
    canvasTransform: { a: 1.5, b: 0, c: 0, d: 0.84375, tx: 440, ty: 40 },
  };
  const logical = { x: 594, y: 344 };
  const dom = logicalToDomPoint(logical, metrics);
  assert.deepEqual(dom, { x: 1311, y: 290.25 });
  assert.deepEqual(domToLogicalPoint(dom, metrics), logical);
});

test('pre-input classifier distinguishes coordinate, overlay and runtime stack failures', () => {
  const base = {
    schema: inputFixture.schema,
    targetResolution: { actualCount: 1 },
    target: inputFixture.target,
    targetChain: [inputFixture.target],
    mapping: inputFixture.mappingPass,
    capture: null,
  };
  assert.deepEqual(classifyPreInput({ ...base, topmost: inputFixture.target }), {
    pass: true, classification: 'topmost-target-ready', reason: null,
  });
  assert.equal(classifyPreInput({ ...base, mapping: inputFixture.mappingMismatch, topmost: inputFixture.target }).classification, 'canvas-coordinate-mismatch');
  assert.equal(classifyPreInput({ ...base, topmost: inputFixture.sameViewOverlay }).classification, 'overlay-intercepted');
  assert.equal(classifyPreInput({ ...base, topmost: inputFixture.otherViewTop }).classification, 'stack-order-wrong');
  assert.equal(inputFixture.otherViewTop.mask.name, 'modal_mask');
});

test('input consumption requires a real canvas down/up and a dispatched target click listener', () => {
  assert.deepEqual(classifyInputConsumption(inputFixture.consumed), {
    pass: true, classification: 'target-click-consumed', reason: null, closeSemanticObserved: true,
  });
  assert.equal(classifyInputConsumption(inputFixture.notDispatched).classification, 'event-not-dispatched');
});

test('topmost overlay hard-stops before browser input and preserves the occlusion chain', async () => {
  const page = fakePage();
  page.evaluate = async (_function, payload) => {
    if (payload.operation === 'inspect-canvas-input') {
      return {
        schema: inputFixture.schema, applicable: true,
        targetResolution: { actualCount: 1 }, target: inputFixture.target,
        targetChain: [inputFixture.target], topmost: inputFixture.otherViewTop,
        topmostChain: [inputFixture.otherViewTop], capture: null, mapping: inputFixture.mappingPass,
      };
    }
    throw new Error(`unexpected operation: ${payload.operation}`);
  };
  await assert.rejects(
    clickRuntimeTarget(page, ownerBindingSnapshot, {
      source: 'laya-stage', ownerView: 'CycleimpActlistYesterday', boundField: '_btn_close', expectedCount: 1,
    }, { canvasMetrics }),
    error => {
      assert.equal(error.code, 'CANVAS_STACK_ORDER_WRONG');
      assert.equal(error.diagnostic.context.input.topmost.ownerView, 'DailyActTipView');
      assert.equal(error.diagnostic.context.input.topmostChain[0].mask.name, 'modal_mask');
      return true;
    },
  );
  assert.equal(page.calls.length, 0);
});

test('manager-owned full-screen blocker records ownership, listeners, hit area and normalized obstacle subtree', async () => {
  const page = fakePage();
  const withObstacle = structuredClone(ownerBindingSnapshot);
  withObstacle.nodes.push({
    ...withObstacle.nodes[0],
    source: 'laya-stage', view: null, path: inputFixture.globalBackgroundTop.path,
    indexPath: inputFixture.globalBackgroundTop.indexPath, parentPath: 'Ue[0]/UIRoot[2]/Top[3]',
    name: 'o', type: 'Image', identity: {
      ownerView: null, runtimeName: 'o', runtimeClass: 'laya.ui.Image', owner: null, bindings: [],
      systemOverlay: inputFixture.globalBackgroundTop.systemOverlay,
    },
    interaction: {
      mouseEnabled: true, mouseThrough: false, mouseState: 2, disabled: false,
      hitArea: inputFixture.globalBackgroundTop.hitArea,
      eventListeners: inputFixture.globalBackgroundTop.eventListeners,
    },
  });
  withObstacle.runtimeOverlays = [{
    ...inputFixture.globalBackgroundTop.systemOverlay,
    nodeStagePath: inputFixture.globalBackgroundTop.indexPath,
  }];
  page.evaluate = async (_function, payload) => {
    if (payload.operation === 'inspect-canvas-input') return {
      schema: inputFixture.schema, applicable: true,
      targetResolution: { actualCount: 1 }, target: inputFixture.target,
      targetChain: [inputFixture.target], topmost: inputFixture.globalBackgroundTop,
      topmostChain: [inputFixture.globalBackgroundTop], capture: null, mapping: inputFixture.mappingPass,
    };
    throw new Error(`unexpected operation: ${payload.operation}`);
  };
  await assert.rejects(clickRuntimeTarget(page, withObstacle, {
    source: 'laya-stage', ownerView: 'CycleimpActlistYesterday', boundField: '_btn_close', expectedCount: 1,
  }, { canvasMetrics }), error => {
    assert.equal(error.code, 'CANVAS_STACK_ORDER_WRONG');
    const obstacle = error.diagnostic.context.obstacle;
    assert.equal(obstacle.systemOverlay.authority, 'ViewManager.GetBackGround');
    assert.equal(obstacle.subtree[0].interaction.eventListeners[0].type, 'click');
    assert.equal(obstacle.subtree[0].interaction.hitArea.width, 730);
    assert.match(obstacle.sha256, /^[a-f0-9]{64}$/);
    return true;
  });
  assert.equal(page.calls.length, 0);
});

test('missing Laya target click is classified after exactly one browser click', async () => {
  const page = fakePage();
  const originalEvaluate = page.evaluate;
  page.evaluate = async (fn, payload) => {
    if (payload.operation === 'finish-canvas-input-probe') return { ...inputFixture.notDispatched, schema: inputFixture.schema, probeId: payload.probeId };
    return originalEvaluate(fn, payload);
  };
  await assert.rejects(
    clickRuntimeTarget(page, snapshot, {
      source: 'laya-stage', view: 'ItemUseView', name: 'close_btn', expectedCount: 1,
    }, { canvasMetrics }),
    error => {
      assert.equal(error.code, 'CANVAS_EVENT_NOT_DISPATCHED');
      assert.equal(error.diagnostic.context.input.consumption.classification, 'event-not-dispatched');
      return true;
    },
  );
  assert.equal(page.calls.filter(call => call[0] === 'click').length, 1);
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
