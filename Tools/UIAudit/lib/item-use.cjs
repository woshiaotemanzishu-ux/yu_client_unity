'use strict';

const { collectRuntimeSnapshot, findNodes } = require('./runtime-tree.cjs');
const { clickRuntimeTarget } = require('./canvas-input.cjs');
const { readLegacyProtocolTrace, evaluateProtocolAssertions } = require('./protocol-probe.cjs');

const DEFAULT_IDENTITY = Object.freeze({
  view: 'ItemUseView',
  closeNode: 'close_btn',
  enterNode: 'enter_btn',
});

function validateItemUseRouteConfig(config) {
  if (!config) return { pass: false, mode: null, errors: ['session.itemUse'] };
  if (config.mode === 'hard-stop') return { pass: true, mode: 'hard-stop', errors: [] };
  const errors = [];
  const authorization = config.authorization || {};
  if (authorization.allowQueueMutation !== true) errors.push('authorization.allowQueueMutation');
  if (Number(authorization.maxCloseClicks) !== 1) errors.push('authorization.maxCloseClicks');
  if (typeof authorization.scope !== 'string' || !authorization.scope.trim()) errors.push('authorization.scope');
  if (config.mode === 'controlled-current-read-only') {
    const maxInstances = Number(authorization.maxInstances);
    if (!Number.isInteger(maxInstances) || maxInstances < 1 || maxInstances > 8) errors.push('authorization.maxInstances');
    if (config.identitySource !== 'ItemUseView.GetTypeId/GetGoodsId') errors.push('identitySource');
    if (config.requireBagNonDecrease !== true) errors.push('requireBagNonDecrease');
    if (!config.protocolAssertions || config.protocolAssertions.mode !== 'read-only') errors.push('protocolAssertions.mode');
    return { pass: errors.length === 0, mode: 'controlled-current-read-only', errors };
  }
  const expected = config.expected || {};
  for (const key of ['view', 'typeId', 'name', 'bottom', 'enter', 'closeNode']) {
    if (expected[key] == null || expected[key] === '') errors.push(`expected.${key}`);
  }
  if (expected.view && expected.view !== 'ItemUseView') errors.push('expected.view');
  if (!Array.isArray(config.queueSpecs) || !config.queueSpecs.length) errors.push('queueSpecs');
  const queueIds = new Set();
  for (const [index, spec] of (config.queueSpecs || []).entries()) {
    if (!spec.id || queueIds.has(spec.id)) errors.push(`queueSpecs[${index}].id`);
    queueIds.add(spec.id);
    if (!spec.root || !Array.isArray(spec.path)) errors.push(`queueSpecs[${index}].path`);
    if ((spec.path || []).some(value => typeof value !== 'string' || !/^[A-Za-z_$][A-Za-z0-9_$]*$/.test(value))) errors.push(`queueSpecs[${index}].unsafePath`);
  }
  const unchanged = config.queueAssertions && config.queueAssertions.unchanged || [];
  const changed = config.queueAssertions && config.queueAssertions.changed || [];
  if (!unchanged.length && !changed.length) errors.push('queueAssertions');
  for (const id of [...unchanged, ...changed]) if (!queueIds.has(id)) errors.push(`queueAssertions.unknown:${id}`);
  for (const id of unchanged) if (changed.includes(id)) errors.push(`queueAssertions.conflict:${id}`);
  if (!config.protocolAssertions || config.protocolAssertions.mode !== 'read-only') errors.push('protocolAssertions.mode');
  return { pass: errors.length === 0, mode: 'controlled-close', errors };
}

function finiteRect(rect) {
  return !!rect && ['x', 'y', 'width', 'height'].every(key => Number.isFinite(Number(rect[key])))
    && Number(rect.width) > 0 && Number(rect.height) > 0;
}

function rectEquals(left, right, tolerance = 0.25) {
  return finiteRect(left) && finiteRect(right)
    && ['x', 'y', 'width', 'height'].every(key => Math.abs(Number(left[key]) - Number(right[key])) <= tolerance);
}

function rectsOverlap(left, right) {
  return finiteRect(left) && finiteRect(right)
    && left.x < right.x + right.width && left.x + left.width > right.x
    && left.y < right.y + right.height && left.y + left.height > right.y;
}

function identityTypeId(node) {
  const value = node && node.state && node.state.dataIdentity;
  if (!value) return null;
  for (const key of ['type_id', 'typeId', 'goods_id', 'goodsId', 'id']) {
    if (value[key] != null && Number.isFinite(Number(value[key]))) return Number(value[key]);
  }
  return null;
}

function inspectItemUseSnapshot(snapshot, expected = {}) {
  const identity = { ...DEFAULT_IDENTITY, ...expected };
  const loaded = findNodes(snapshot, { source: 'loaded-view', name: identity.view });
  const managedRoots = findNodes(snapshot, { source: 'managed-view', view: identity.view, name: identity.view })
    .filter(node => node.depth === 0);
  const stageRoots = findNodes(snapshot, { source: 'laya-stage', name: identity.view });
  const subtree = findNodes(snapshot, { source: 'laya-stage', view: identity.view });
  const byName = name => subtree.filter(node => node.name === name && node.visible);
  const byText = text => subtree.filter(node => node.text.trim() === String(text).trim() && node.visible);
  const closeNodes = byName(identity.closeNode);
  const enterNodes = byName(identity.enterNode);
  const nameNodes = identity.name == null ? [] : byText(identity.name);
  const bottomNodes = identity.bottom == null ? [] : byText(identity.bottom);
  const enterTextNodes = identity.enter == null ? [] : byText(identity.enter);
  const root = stageRoots.length === 1 ? stageRoots[0] : null;
  const close = closeNodes.length === 1 ? closeNodes[0] : null;
  const enter = enterNodes.length === 1 ? enterNodes[0] : null;
  const actualTypeId = identityTypeId(root) || subtree.map(identityTypeId).find(value => value != null) || null;
  const checks = {
    oneLoadedView: loaded.length === 1,
    oneManagedView: managedRoots.length === 1,
    oneStageRoot: stageRoots.length === 1,
    oneCloseNode: closeNodes.length === 1,
    oneEnterNode: enterNodes.length === 1,
    nameMatches: identity.name == null || nameNodes.length === 1,
    bottomMatches: identity.bottom == null || bottomNodes.length === 1,
    enterMatches: identity.enter == null || enterTextNodes.length >= 1,
    typeIdMatches: identity.typeId == null || actualTypeId === Number(identity.typeId),
    closeOutsideEnter: !!close && !!enter && !rectsOverlap(close.bounds, enter.bounds),
  };
  const runtime = {
    isAnim: root && root.state.isAnim,
    frameToken: root && root.state.frameToken != null ? root.state.frameToken : snapshot && snapshot.stage && snapshot.stage.frameToken,
    closeBounds: close && close.bounds || null,
    enterBounds: enter && enter.bounds || null,
    closeHittable: !!close && close.displayed && close.interaction.mouseEnabled
      && !close.interaction.disabled && close.interaction.hitTestCenter === true,
  };
  return {
    expected: identity,
    actualTypeId,
    exact: Object.values(checks).every(Boolean),
    checks,
    counts: { loaded: loaded.length, managedRoots: managedRoots.length, stageRoots: stageRoots.length, close: closeNodes.length, enter: enterNodes.length },
    runtime,
    close,
    enter,
  };
}

function evaluateStableItemUseFrames(frames) {
  const pair = Array.isArray(frames) ? frames.slice(-2) : [];
  const perFrame = pair.map(frame => ({
    exactIdentity: !!(frame && frame.exact),
    animationFinished: !!(frame && frame.runtime && frame.runtime.isAnim === false),
    closeHittable: !!(frame && frame.runtime && frame.runtime.closeHittable),
    closeBoundsValid: finiteRect(frame && frame.runtime && frame.runtime.closeBounds),
  }));
  const first = pair[0] && pair[0].runtime;
  const second = pair[1] && pair[1].runtime;
  const checks = {
    exactlyTwoFrames: pair.length === 2,
    bothFramesReady: perFrame.length === 2 && perFrame.every(item => Object.values(item).every(Boolean)),
    frameTokensAdvance: !first || !second || first.frameToken == null || second.frameToken == null
      || Number(second.frameToken) > Number(first.frameToken),
    stableGeometry: !!first && !!second && rectEquals(first.closeBounds, second.closeBounds),
  };
  return { pass: Object.values(checks).every(Boolean), checks, perFrame, frames: pair };
}

async function waitOneLayaFrame(page) {
  return page.evaluate(() => new Promise((resolve, reject) => {
    if (!window.Laya || !Laya.timer || typeof Laya.timer.frameOnce !== 'function') {
      reject(new Error('LAYA_FRAME_ONCE_UNAVAILABLE'));
      return;
    }
    Laya.timer.frameOnce(1, null, () => resolve(true));
  }));
}

async function waitForStableItemUse(page, expected, options = {}) {
  const timeoutMs = options.timeoutMs || 12000;
  const deadline = Date.now() + timeoutMs;
  const attempts = [];
  while (Date.now() < deadline) {
    const first = inspectItemUseSnapshot(await collectRuntimeSnapshot(page), expected);
    if (!first.exact) throw new Error(`ITEM_USE_IDENTITY_CHANGED: ${JSON.stringify(first)}`);
    await waitOneLayaFrame(page);
    const second = inspectItemUseSnapshot(await collectRuntimeSnapshot(page), expected);
    if (!second.exact) throw new Error(`ITEM_USE_IDENTITY_CHANGED: ${JSON.stringify(second)}`);
    const evaluation = evaluateStableItemUseFrames([first, second]);
    attempts.push(evaluation);
    if (evaluation.pass) return { ...evaluation, attempts };
    await new Promise(resolve => setTimeout(resolve, options.pollMs || 16));
  }
  throw new Error(`ITEM_USE_STABLE_TIMEOUT: ${JSON.stringify(attempts.slice(-4))}`);
}

async function captureRuntimePaths(page, specs = []) {
  return page.evaluate(specs => {
    const clone = value => {
      const ancestors = new Set();
      const visit = input => {
        if (input == null || typeof input !== 'object') return input;
        if (ancestors.has(input)) return undefined;
        ancestors.add(input);
        const output = Array.isArray(input) ? [] : {};
        for (const key of Object.keys(input)) {
          const child = visit(input[key]);
          if (child !== undefined) output[key] = child;
        }
        ancestors.delete(input);
        return output;
      };
      try { return visit(value); } catch (_) { return String(value); }
    };
    const result = {};
    for (const spec of specs) {
      let value = window[spec.root];
      if (value && spec.singleton) {
        const method = value[spec.singleton];
        if (typeof method !== 'function') throw new Error(`queue singleton missing: ${spec.root}.${spec.singleton}`);
        value = method.call(value);
      }
      for (const key of spec.path || []) value = value == null ? value : value[key];
      result[spec.id] = clone(value);
    }
    return result;
  }, specs);
}

function evaluateQueueTransition(before, after, assertions = {}) {
  const stable = (assertions.unchanged || []).map(id => ({
    id,
    pass: JSON.stringify(before && before[id]) === JSON.stringify(after && after[id]),
  }));
  const changed = (assertions.changed || []).map(id => ({
    id,
    pass: JSON.stringify(before && before[id]) !== JSON.stringify(after && after[id]),
  }));
  return { pass: [...stable, ...changed].every(item => item.pass), unchanged: stable, changed };
}

async function waitViewAbsent(page, viewName, timeoutMs = 8000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const snapshot = await collectRuntimeSnapshot(page);
    if (!snapshot.visibleViews.includes(viewName)) return snapshot;
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  throw new Error(`VIEW_DID_NOT_CLOSE: ${viewName}`);
}

async function closeItemUseControlled(page, options) {
  const authorization = options && options.authorization || {};
  if (authorization.allowQueueMutation !== true || Number(authorization.maxCloseClicks) !== 1) {
    throw new Error('ITEM_USE_CLOSE_NOT_AUTHORIZED: require allowQueueMutation=true and maxCloseClicks=1');
  }
  const expected = options && options.expected || {};
  const missingIdentity = ['view', 'typeId', 'name', 'bottom', 'enter', 'closeNode']
    .filter(key => expected[key] == null || expected[key] === '');
  if (missingIdentity.length) throw new Error(`ITEM_USE_IDENTITY_INCOMPLETE: ${missingIdentity.join(',')}`);
  if (!Array.isArray(options.queueSpecs) || options.queueSpecs.length === 0) {
    throw new Error('ITEM_USE_QUEUE_SPECS_REQUIRED');
  }
  const queueAssertionCount = (options.queueAssertions && options.queueAssertions.changed || []).length
    + (options.queueAssertions && options.queueAssertions.unchanged || []).length;
  if (queueAssertionCount === 0) throw new Error('ITEM_USE_QUEUE_ASSERTIONS_REQUIRED');
  const beforeQueue = await captureRuntimePaths(page, options.queueSpecs || []);
  const stable = await waitForStableItemUse(page, expected, options);
  const beforeTrace = await readLegacyProtocolTrace(page);
  const beforeCheck = evaluateProtocolAssertions(beforeTrace, options.protocolAssertions || { mode: 'read-only' }, options.protocolPolicy);
  if (!beforeCheck.pass) throw new Error(`ITEM_USE_PRECLICK_PROTOCOL_FAILED: ${JSON.stringify(beforeCheck)}`);
  const snapshot = await collectRuntimeSnapshot(page);
  const click = await clickRuntimeTarget(page, snapshot, {
    source: 'laya-stage', view: expected.view,
    name: expected.closeNode, expectedCount: 1,
  });
  const closedSnapshot = await waitViewAbsent(page, expected.view, options.timeoutMs || 8000);
  const afterQueue = await captureRuntimePaths(page, options.queueSpecs || []);
  const queueCheck = evaluateQueueTransition(beforeQueue, afterQueue, options.queueAssertions || {});
  if (!queueCheck.pass) throw new Error(`ITEM_USE_QUEUE_ASSERTION_FAILED: ${JSON.stringify(queueCheck)}`);
  const afterTrace = await readLegacyProtocolTrace(page);
  const afterCheck = evaluateProtocolAssertions(afterTrace, options.protocolAssertions || { mode: 'read-only' }, options.protocolPolicy);
  if (!afterCheck.pass) throw new Error(`ITEM_USE_POSTCLICK_PROTOCOL_FAILED: ${JSON.stringify(afterCheck)}`);
  return {
    schema: 1,
    stable,
    click,
    queue: { before: beforeQueue, after: afterQueue, check: queueCheck },
    protocol: { before: beforeCheck, after: afterCheck },
    closed: !closedSnapshot.visibleViews.includes(expected.view),
    closeClicks: 1,
  };
}

module.exports = {
  DEFAULT_IDENTITY,
  validateItemUseRouteConfig,
  finiteRect,
  rectEquals,
  rectsOverlap,
  inspectItemUseSnapshot,
  evaluateStableItemUseFrames,
  waitOneLayaFrame,
  waitForStableItemUse,
  captureRuntimePaths,
  evaluateQueueTransition,
  waitViewAbsent,
  closeItemUseControlled,
};
