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
  const point = options.point || centerOf(target.bounds);
  assertPointInViewport(point, viewportOf(page));
  const hit = await probeStageHit(page, target, point);
  if (hit.applicable && !hit.pass) {
    throw new Error(`CANVAS_HIT_REJECTED path=${target.path} reason=${hit.reason}`);
  }
  await page.mouse.click(point.x, point.y, {
    button: options.button || 'left',
    clickCount: options.clickCount || 1,
    delay: options.delay || 0,
  });
  return { action: 'click', target, point, hit };
}

async function dragRuntimeTarget(page, snapshot, selector, options = {}) {
  const target = resolveTarget(snapshot, selector);
  const start = options.start || centerOf(target.bounds);
  const end = options.end || {
    x: start.x + Number(options.deltaX || 0),
    y: start.y + Number(options.deltaY || 0),
  };
  const viewport = viewportOf(page);
  assertPointInViewport(start, viewport);
  assertPointInViewport(end, viewport);
  const hit = await probeStageHit(page, target, start);
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
  return { action: 'drag', target, start, end, hit };
}

module.exports = {
  centerOf,
  viewportOf,
  assertPointInViewport,
  resolveTarget,
  probeStageHit,
  clickRuntimeTarget,
  dragRuntimeTarget,
};
