'use strict';

const { findNodes } = require('./runtime-tree.cjs');

function centerOf(rect) {
  if (!rect || ![rect.x, rect.y, rect.width, rect.height].every(value => Number.isFinite(Number(value)))) {
    throw new Error(`INVALID_RUNTIME_BOUNDS: ${JSON.stringify(rect)}`);
  }
  if (Number(rect.width) <= 0 || Number(rect.height) <= 0) {
    throw new Error(`EMPTY_RUNTIME_BOUNDS: ${JSON.stringify(rect)}`);
  }
  return { x: Number(rect.x) + Number(rect.width) / 2, y: Number(rect.y) + Number(rect.height) / 2 };
}

function viewportOf(page) {
  if (typeof page.viewport === 'function') return page.viewport();
  if (typeof page.viewportSize === 'function') return page.viewportSize();
  return null;
}

function assertPointInViewport(point, viewport) {
  if (!viewport || !Number.isFinite(Number(viewport.width)) || !Number.isFinite(Number(viewport.height))) {
    throw new Error('VIEWPORT_UNAVAILABLE');
  }
  const pass = Number(point.x) >= 0 && Number(point.y) >= 0
    && Number(point.x) <= Number(viewport.width) && Number(point.y) <= Number(viewport.height);
  if (!pass) throw new Error(`POINT_OUTSIDE_VIEWPORT point=${JSON.stringify(point)} viewport=${JSON.stringify(viewport)}`);
  return true;
}

async function readCanvasMetrics(page, snapshot) {
  const stage = snapshot && snapshot.stage || {};
  return page.evaluate(({ logicalWidth, logicalHeight }) => {
    const canvases = [...document.querySelectorAll('canvas')]
      .map(canvas => ({ canvas, rect: canvas.getBoundingClientRect() }))
      .filter(value => value.rect.width > 0 && value.rect.height > 0)
      .sort((left, right) => right.rect.width * right.rect.height - left.rect.width * left.rect.height);
    if (!canvases.length) return null;
    const { canvas, rect } = canvases[0];
    const layaStage = window.Laya && Laya.stage;
    return {
      selector: canvas.id ? `#${canvas.id}` : 'canvas',
      x: Number(rect.x), y: Number(rect.y), width: Number(rect.width), height: Number(rect.height),
      logicalWidth: Number(logicalWidth || layaStage && layaStage.width || canvas.width || rect.width),
      logicalHeight: Number(logicalHeight || layaStage && layaStage.height || canvas.height || rect.height),
      backingWidth: Number(canvas.width || 0), backingHeight: Number(canvas.height || 0),
    };
  }, { logicalWidth: Number(stage.width || 0), logicalHeight: Number(stage.height || 0) });
}

function logicalToDomPoint(point, metrics) {
  if (!metrics || ![metrics.x, metrics.y, metrics.width, metrics.height, metrics.logicalWidth, metrics.logicalHeight]
    .every(value => Number.isFinite(Number(value)))) throw new Error('CANVAS_METRICS_UNAVAILABLE');
  if (Number(metrics.width) <= 0 || Number(metrics.height) <= 0 || Number(metrics.logicalWidth) <= 0 || Number(metrics.logicalHeight) <= 0) {
    throw new Error(`CANVAS_METRICS_INVALID: ${JSON.stringify(metrics)}`);
  }
  return {
    x: Number(metrics.x) + Number(point.x) * Number(metrics.width) / Number(metrics.logicalWidth),
    y: Number(metrics.y) + Number(point.y) * Number(metrics.height) / Number(metrics.logicalHeight),
  };
}

function resolveTarget(snapshot, selector = {}) {
  const matches = findNodes(snapshot, selector);
  const expectedCount = selector.expectedCount == null ? 1 : Number(selector.expectedCount);
  if (matches.length !== expectedCount) {
    throw new Error(`CANVAS_TARGET_IDENTITY_MISMATCH expected=${expectedCount} actual=${matches.length} selector=${JSON.stringify(selector)}`);
  }
  const index = selector.index == null ? 0 : Number(selector.index);
  const node = matches[index];
  if (!node) throw new Error(`CANVAS_TARGET_INDEX_MISSING index=${index}`);
  if (!node.interaction.mouseEnabled || node.interaction.disabled || !node.displayed) {
    throw new Error(`CANVAS_TARGET_NOT_INTERACTIVE: ${node.path}`);
  }
  return node;
}

async function probeStageHit(page, node, point = centerOf(node.bounds)) {
  if (node.source !== 'laya-stage' || !Array.isArray(node.indexPath)) {
    return { applicable: false, pass: null, reason: 'target is not a Laya.stage node' };
  }
  return page.evaluate(({ indexPath, point }) => {
    const stage = window.Laya && Laya.stage;
    if (!stage) return { applicable: true, pass: false, reason: 'Laya.stage missing' };
    let current = stage;
    const childrenOf = value => value && (value._children || (value.numChildren
      ? Array.from({ length: value.numChildren }, (_, index) => value.getChildAt(index)) : [])) || [];
    for (const index of indexPath.slice(1)) {
      current = childrenOf(current)[Number(index)];
      if (!current) return { applicable: true, pass: false, reason: `index path missing at ${index}` };
    }
    if (current.displayedInStage === false || current.visible === false || current.mouseEnabled === false) {
      return { applicable: true, pass: false, reason: 'runtime node is not displayed/mouse-enabled' };
    }
    if (typeof current.hitTestPoint !== 'function') {
      return { applicable: true, pass: false, reason: 'hitTestPoint unavailable' };
    }
    let pass = false;
    try { pass = !!current.hitTestPoint(Number(point.x), Number(point.y)); }
    catch (error) { return { applicable: true, pass: false, reason: String(error) }; }
    return { applicable: true, pass, reason: pass ? null : 'hitTestPoint rejected center' };
  }, { indexPath: node.indexPath, point });
}

async function clickRuntimeTarget(page, snapshot, selector, options = {}) {
  const target = resolveTarget(snapshot, selector);
  const logicalPoint = options.point || centerOf(target.bounds);
  const canvas = options.canvasMetrics || await readCanvasMetrics(page, snapshot);
  const point = logicalToDomPoint(logicalPoint, canvas);
  assertPointInViewport(point, viewportOf(page));
  const hit = await probeStageHit(page, target, logicalPoint);
  if (hit.applicable && !hit.pass) {
    throw new Error(`CANVAS_HIT_REJECTED path=${target.path} reason=${hit.reason}`);
  }
  await page.mouse.click(point.x, point.y, {
    button: options.button || 'left',
    clickCount: options.clickCount || 1,
    delay: options.delay || 0,
  });
  return { action: 'click', target, logicalPoint, point, canvas, hit };
}

async function dragRuntimeTarget(page, snapshot, selector, options = {}) {
  const target = resolveTarget(snapshot, selector);
  const logicalStart = options.start || centerOf(target.bounds);
  const logicalEnd = options.end || {
    x: logicalStart.x + Number(options.deltaX || 0),
    y: logicalStart.y + Number(options.deltaY || 0),
  };
  const canvas = options.canvasMetrics || await readCanvasMetrics(page, snapshot);
  const start = logicalToDomPoint(logicalStart, canvas);
  const end = logicalToDomPoint(logicalEnd, canvas);
  const viewport = viewportOf(page);
  assertPointInViewport(start, viewport);
  assertPointInViewport(end, viewport);
  const hit = await probeStageHit(page, target, logicalStart);
  if (hit.applicable && !hit.pass) {
    throw new Error(`CANVAS_DRAG_HIT_REJECTED path=${target.path} reason=${hit.reason}`);
  }
  await page.mouse.move(start.x, start.y);
  await page.mouse.down({ button: options.button || 'left' });
  try {
    await page.mouse.move(end.x, end.y, { steps: options.steps || 12 });
  } finally {
    await page.mouse.up({ button: options.button || 'left' });
  }
  return { action: 'drag', target, logicalStart, logicalEnd, start, end, canvas, hit };
}

module.exports = {
  centerOf,
  viewportOf,
  assertPointInViewport,
  readCanvasMetrics,
  logicalToDomPoint,
  resolveTarget,
  probeStageHit,
  clickRuntimeTarget,
  dragRuntimeTarget,
};
