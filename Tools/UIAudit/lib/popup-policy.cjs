'use strict';

const fs = require('fs');
const path = require('path');
const { POPUP_POLICY_SCHEMA_VERSION } = require('./version.cjs');
const { classifyPopupCloseFailure } = require('./popup-lifecycle.cjs');
const { runtimeOverlayViews } = require('./runtime-overlay.cjs');

function validatePopupPolicy(policy) {
  const errors = [];
  if (!policy || Number(policy.schema) !== POPUP_POLICY_SCHEMA_VERSION) errors.push('schema');
  if (!['unknown-hard-stop', 'hard_stop'].includes(policy && policy.default)) errors.push('default');
  if (!Array.isArray(policy && policy.entries)) errors.push('entries');
  const names = new Set();
  for (const entry of policy && policy.entries || []) {
    if (!entry || !entry.view || names.has(entry.view)) errors.push(`duplicate-or-empty:${entry && entry.view}`);
    names.add(entry && entry.view);
    if (!['allow', 'forbid', 'wait'].includes(entry && entry.action)) errors.push(`action:${entry && entry.view}`);
    if (entry && entry.action === 'allow' && (!entry.safeClose || !entry.safeClose.kind)) errors.push(`safeClose:${entry.view}`);
    if (entry && entry.action === 'forbid' && entry.safeClose) errors.push(`forbid-safeClose:${entry.view}`);
    if (entry && entry.action === 'wait') {
      if (entry.safeClose) errors.push(`wait-safeClose:${entry.view}`);
      if (!entry.waitForRelease || !Number.isInteger(Number(entry.waitForRelease.timeoutMs))
          || Number(entry.waitForRelease.timeoutMs) <= 0) errors.push(`waitForRelease:${entry.view}`);
    }
    const stability = entry && entry.safeClose && entry.safeClose.stability;
    const point = entry && entry.safeClose && entry.safeClose.point;
    if (point && (!Number.isFinite(Number(point.x)) || !Number.isFinite(Number(point.y)))) {
      errors.push(`safe-close-point:${entry.view}`);
    }
    if (stability) {
      if (stability.kind !== 'absent-advancing-laya-frames') errors.push(`stability-kind:${entry.view}`);
      for (const field of ['consecutiveFrames', 'timeoutMs', 'pollMs']) {
        if (!Number.isInteger(Number(stability[field])) || Number(stability[field]) <= 0) {
          errors.push(`stability-${field}:${entry.view}`);
        }
      }
    }
    if (entry && entry.queue && entry.queue.order === 'observed-top-first') {
      if (entry.queue.configured !== false || entry.sort != null) errors.push(`runtime-stack-order:${entry.view}`);
    }
    if (!Array.isArray(entry && entry.closeProtocols) || !Array.isArray(entry && entry.closeWrites)) {
      errors.push(`side-effect-schema:${entry && entry.view}`);
    }
  }
  if (errors.length) throw new Error(`POPUP_POLICY_INVALID: ${errors.join(',')}`);
  return policy;
}

function loadPopupPolicy(filePath) {
  const absolute = path.resolve(filePath);
  const policy = JSON.parse(fs.readFileSync(absolute, 'utf8'));
  validatePopupPolicy(policy);
  Object.defineProperty(policy, '__file', { value: absolute, enumerable: false });
  return policy;
}

function getPopupEntry(policy, viewName) {
  return (policy.entries || []).find(entry => entry.view === viewName) || null;
}

function decidePopup(policy, viewName) {
  validatePopupPolicy(policy);
  const entry = getPopupEntry(policy, viewName);
  if (!entry) {
    return { view: viewName, action: 'unknown-hard-stop', entry: null, reason: 'unknown popup; policy default is hard stop' };
  }
  return { view: viewName, action: entry.action, entry, reason: entry.reason || null };
}

function dedupePopupQueue(items) {
  const result = [];
  const indexByName = new Map();
  for (const item of items || []) {
    const value = typeof item === 'string' ? { view: item } : { ...item };
    const name = String(value.view || '');
    if (!name) continue;
    if (indexByName.has(name)) result[indexByName.get(name)] = { ...result[indexByName.get(name)], ...value, view: name };
    else {
      indexByName.set(name, result.length);
      result.push({ ...value, view: name });
    }
  }
  return result;
}

function compareStagePathsTopFirst(leftPath, rightPath) {
  const left = Array.isArray(leftPath) ? leftPath.map(Number) : [];
  const right = Array.isArray(rightPath) ? rightPath.map(Number) : [];
  const length = Math.min(left.length, right.length);
  for (let index = 0; index < length; index++) {
    if (left[index] !== right[index]) return right[index] - left[index];
  }
  return right.length - left.length;
}

function observePopupStack(snapshot, items, runtimeOverlayPolicy = null) {
  const overlayViews = runtimeOverlayPolicy ? runtimeOverlayViews(snapshot, runtimeOverlayPolicy) : [];
  const deduped = dedupePopupQueue([...(items || []), ...overlayViews]);
  const nodes = Array.isArray(snapshot && snapshot.nodes) ? snapshot.nodes : [];
  const rootsByView = new Map();
  for (const node of nodes) {
    const owner = node && node.identity && node.identity.owner;
    if (node.source !== 'laya-stage' || !node.visible || !node.displayed || !owner || !owner.isRoot || !owner.view) continue;
    if (!rootsByView.has(owner.view)) rootsByView.set(owner.view, []);
    rootsByView.get(owner.view).push(node);
  }
  const observed = deduped.map(item => {
    const roots = (rootsByView.get(item.view) || []).sort((left, right) => compareStagePathsTopFirst(left.indexPath, right.indexPath));
    const overlay = overlayViews.find(value => value.view === item.view) || null;
    const root = roots[0] || null;
    return {
      ...item,
      source: root ? 'laya-stage' : overlay ? 'runtime-overlay' : item.source || null,
      resolved: !!root || !!overlay,
      stagePath: root && root.indexPath || overlay && overlay.stagePath || null,
      rootPath: root && root.path || null,
      childIndex: root && root.childIndex != null ? root.childIndex
        : root && Array.isArray(root.indexPath) ? root.indexPath[root.indexPath.length - 1]
          : overlay && overlay.childIndex != null ? overlay.childIndex : null,
      zOrder: root && Number.isFinite(Number(root.zOrder)) ? Number(root.zOrder) : null,
      instance: root && root.identity && root.identity.owner && root.identity.owner.instances
        || overlay && overlay.instance || [],
      overlay: overlay && overlay.overlay || item.overlay || null,
    };
  });
  return observed.sort((left, right) => {
    if (left.resolved !== right.resolved) return left.resolved ? -1 : 1;
    if (!left.resolved) return 0;
    return compareStagePathsTopFirst(left.stagePath, right.stagePath);
  });
}

function orderPopupQueue(items, policy) {
  const deduped = dedupePopupQueue(items);
  const runtimeStackViews = deduped
    .filter(item => getPopupEntry(policy, item.view)?.queue?.order === 'observed-top-first')
    .map(item => item.view);
  if (runtimeStackViews.length) {
    throw new Error(`POPUP_OBSERVED_TOP_FIRST_REQUIRED: ${runtimeStackViews.join(',')}`);
  }
  return deduped.sort((left, right) => {
    const leftEntry = getPopupEntry(policy, left.view);
    const rightEntry = getPopupEntry(policy, right.view);
    const leftSort = Number.isFinite(Number(left.sort)) ? Number(left.sort) : Number(leftEntry && leftEntry.sort);
    const rightSort = Number.isFinite(Number(right.sort)) ? Number(right.sort) : Number(rightEntry && rightEntry.sort);
    if (Number.isFinite(leftSort) || Number.isFinite(rightSort)) {
      return (Number.isFinite(rightSort) ? rightSort : -Infinity) - (Number.isFinite(leftSort) ? leftSort : -Infinity);
    }
    return 0;
  });
}

function popupCloseStability(samples, viewName, stability, options = {}) {
  if (!stability || stability.kind !== 'absent-advancing-laya-frames') {
    throw new Error(`POPUP_STABILITY_INVALID: ${viewName}`);
  }
  const requiredFrames = Number(stability.consecutiveFrames);
  let stableFrames = 0;
  let lastCountedFrame = null;
  let lastFrameToken = null;
  let present = null;
  const frameTokens = [];
  const phases = [];
  for (const sample of samples || []) {
    const visibleViews = Array.isArray(sample && sample.visibleViews) ? sample.visibleViews : [];
    present = sample && sample.lifecycle && typeof sample.lifecycle.present === 'boolean'
      ? sample.lifecycle.present : visibleViews.includes(viewName);
    const frameToken = Number(sample && sample.lifecycle && sample.lifecycle.frameToken != null
      ? sample.lifecycle.frameToken : sample && sample.stage && sample.stage.frameToken);
    lastFrameToken = Number.isFinite(frameToken) ? frameToken : null;
    frameTokens.push(lastFrameToken);
    phases.push(sample && sample.lifecycle && sample.lifecycle.phase || (present ? 'present-legacy' : 'absent-legacy'));
    if (present) {
      stableFrames = 0;
      lastCountedFrame = null;
      continue;
    }
    if (!Number.isFinite(frameToken) || (lastCountedFrame != null && frameToken <= lastCountedFrame)) continue;
    lastCountedFrame = frameToken;
    stableFrames++;
    if (stableFrames >= requiredFrames) break;
  }
  return {
    pass: stableFrames >= requiredFrames,
    view: viewName,
    kind: stability.kind,
    requiredFrames,
    stableFrames,
    lastFrameToken,
    present,
    samples: Array.isArray(samples) ? samples.length : 0,
    frameTokens,
    phases,
    classification: stableFrames >= requiredFrames ? 'closed-stable' : classifyPopupCloseFailure(samples, options.input),
  };
}

function planPopupDrain(items, policy, options = {}) {
  validatePopupPolicy(policy);
  const ordered = options.observedTopFirst
    ? dedupePopupQueue(items)
    : orderPopupQueue(items, policy);
  const steps = [];
  for (const item of ordered) {
    const decision = decidePopup(policy, item.view);
    steps.push({ ...decision, observed: item });
    if (decision.action !== 'allow') {
      return {
        pass: false,
        blockedBy: decision,
        steps,
        executable: steps.filter(step => step.action === 'allow' && step !== decision),
      };
    }
  }
  return { pass: true, blockedBy: null, steps, executable: steps };
}

function assertSafePopupDecision(decision) {
  if (!decision || decision.action !== 'allow' || !decision.entry || !decision.entry.safeClose) {
    throw new Error(`POPUP_HARD_STOP: ${JSON.stringify(decision)}`);
  }
  if (decision.entry.closeProtocols.length || decision.entry.closeWrites.length) {
    throw new Error(`POPUP_SAFE_CLOSE_HAS_SIDE_EFFECTS: ${decision.view}`);
  }
  return decision.entry.safeClose;
}

module.exports = {
  validatePopupPolicy,
  loadPopupPolicy,
  getPopupEntry,
  decidePopup,
  dedupePopupQueue,
  compareStagePathsTopFirst,
  observePopupStack,
  orderPopupQueue,
  planPopupDrain,
  assertSafePopupDecision,
  popupCloseStability,
};
