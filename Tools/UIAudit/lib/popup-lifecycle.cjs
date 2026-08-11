'use strict';

const { findNodes } = require('./runtime-tree.cjs');

const POPUP_INSTANCE_SCHEMA = 'ui-audit.popup-instance.v1';
const POPUP_LIFECYCLE_SCHEMA = 'ui-audit.popup-lifecycle-sample.v1';

function normalizedInstances(value) {
  const result = [];
  const seen = new Set();
  for (const item of Array.isArray(value) ? value : []) {
    const source = String(item && item.source || '');
    const key = String(item && item.key || '');
    if (!source || !key || seen.has(`${source}\u0000${key}`)) continue;
    seen.add(`${source}\u0000${key}`);
    result.push({ source, key });
  }
  return result;
}

function ownerOf(node) {
  return node && node.identity && node.identity.owner || null;
}

function samePath(left, right) {
  return Array.isArray(left) && Array.isArray(right) && left.length === right.length
    && left.every((part, index) => Number(part) === Number(right[index]));
}

function sharesInstance(left, right) {
  const rightKeys = new Set(normalizedInstances(right).map(item => `${item.source}\u0000${item.key}`));
  return normalizedInstances(left).some(item => rightKeys.has(`${item.source}\u0000${item.key}`));
}

function createPopupInstanceRef(snapshot, viewName, selector, clickedTarget = null) {
  const roots = findNodes(snapshot, { source: 'laya-stage', ownerView: viewName })
    .filter(node => ownerOf(node) && ownerOf(node).isRoot);
  const targetOwner = ownerOf(clickedTarget);
  const rootOwner = roots.map(ownerOf).find(candidate => targetOwner && candidate
    && samePath(candidate.rootStagePath, targetOwner.rootStagePath))
    || roots.length === 1 && ownerOf(roots[0]) || null;
  const owner = targetOwner || rootOwner;
  return {
    schema: POPUP_INSTANCE_SCHEMA,
    view: String(viewName || ''),
    selector: selector || null,
    rootStagePath: owner && owner.rootStagePath || null,
    instances: normalizedInstances([...(owner && owner.instances || []), ...(rootOwner && rootOwner.instances || [])]),
    clickedTarget: clickedTarget ? {
      path: clickedTarget.path,
      indexPath: clickedTarget.indexPath,
      runtimeName: clickedTarget.name,
      bindings: clickedTarget.identity && clickedTarget.identity.bindings || [],
    } : null,
  };
}

function nodeLifecycle(node) {
  const identity = node && node.state && node.state.dataIdentity || {};
  return identity.lifecycle || identity;
}

function matchesExactInstance(node, instanceRef) {
  const owner = ownerOf(node);
  const nodeInstances = owner && owner.instances || [];
  if (instanceRef.instances.length && nodeInstances.length) return sharesInstance(instanceRef.instances, nodeInstances);
  if (instanceRef.rootStagePath && owner && owner.rootStagePath) {
    return samePath(instanceRef.rootStagePath, owner.rootStagePath);
  }
  return node && node.view === instanceRef.view;
}

function projectLifecycleNode(node, exactInstance) {
  const lifecycle = nodeLifecycle(node);
  const owner = ownerOf(node);
  return {
    source: node.source,
    path: node.path,
    exactInstance,
    visible: !!node.visible,
    displayed: !!node.displayed,
    open: lifecycle.open == null ? null : !!lifecycle.open,
    loaded: lifecycle.loaded == null ? null : !!lifecycle.loaded,
    stagePath: lifecycle.stagePath || owner && owner.rootStagePath || null,
    instances: normalizedInstances(owner && owner.instances),
  };
}

function observePopupLifecycle(snapshot, instanceRef) {
  if (!instanceRef || instanceRef.schema !== POPUP_INSTANCE_SCHEMA) throw new Error('POPUP_INSTANCE_REF_INVALID');
  const viewNodes = (snapshot && snapshot.nodes || []).filter(node => node.view === instanceRef.view
    || node.identity && node.identity.ownerView === instanceRef.view);
  const roots = viewNodes.filter(node => node.source === 'loaded-view'
    || node.source === 'managed-view' && Number(node.depth) === 0
    || node.source === 'laya-stage' && ownerOf(node) && ownerOf(node).isRoot);
  const sources = roots.map(node => projectLifecycleNode(node, matchesExactInstance(node, instanceRef)));
  const exact = sources.filter(source => source.exactInstance);
  const other = sources.filter(source => !source.exactInstance);
  const active = source => {
    if (source.source === 'laya-stage') return source.visible && source.displayed;
    if (source.open === false) return false;
    return source.visible && (source.source !== 'loaded-view' || source.open === true);
  };
  const exactActive = exact.filter(active);
  const otherActive = other.filter(active);
  const closeRequested = exact.some(source => source.open === false);
  const exactRegistered = exact.length > 0;
  const requeued = otherActive.length > 0;
  const present = exactActive.length > 0 || requeued;
  let phase = 'closed-absent';
  if (requeued) phase = 'requeued';
  else if (exactActive.length && closeRequested) phase = 'closing';
  else if (exactActive.length) phase = 'open';
  else if (exactRegistered) phase = 'closed-cached';
  return {
    schema: POPUP_LIFECYCLE_SCHEMA,
    view: instanceRef.view,
    frameToken: Number.isFinite(Number(snapshot && snapshot.stage && snapshot.stage.frameToken))
      ? Number(snapshot.stage.frameToken) : null,
    present,
    phase,
    closeRequested,
    exactRegistered,
    requeued,
    presentReasons: [...exactActive, ...otherActive].map(source => `${source.source}:${source.path}`),
    sources,
  };
}

function classifyPopupCloseFailure(samples, inputEvidence = null) {
  const observations = (samples || []).map(sample => sample && sample.lifecycle).filter(Boolean);
  if (!observations.length) return 'unclassified';
  if (observations.some(item => item.requeued)) return 'requeued';
  let sawAbsent = false;
  for (const item of observations) {
    if (!item.present) sawAbsent = true;
    else if (sawAbsent) return 'requeued';
  }
  if (observations[observations.length - 1].present === false) return 'frame-not-advancing';
  if (observations.some(item => item.phase === 'closing')) return 'closing-timeout';
  if (observations.every(item => item.phase === 'open' && !item.closeRequested)) {
    const consumption = inputEvidence && inputEvidence.consumption;
    if (consumption && consumption.classification === 'target-click-consumed') return 'business-handled-but-not-closed';
    if (consumption && consumption.classification === 'event-not-dispatched') return 'event-not-dispatched';
    return 'click-not-consumed';
  }
  if (observations.some(item => item.exactRegistered)) return 'still-visible-or-managed';
  return 'unclassified';
}

module.exports = {
  POPUP_INSTANCE_SCHEMA,
  POPUP_LIFECYCLE_SCHEMA,
  createPopupInstanceRef,
  observePopupLifecycle,
  classifyPopupCloseFailure,
};
