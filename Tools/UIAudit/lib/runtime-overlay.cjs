'use strict';

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { RUNTIME_OVERLAY_POLICY_SCHEMA_VERSION } = require('./version.cjs');

const RUNTIME_OVERLAY_SCHEMA = 'ui-audit.runtime-overlay.v1';

function stagePath(value) {
  if (!Array.isArray(value)) return null;
  const result = value.map(Number);
  return result.every(Number.isInteger) ? result : null;
}

function compareStagePathsTopFirst(leftPath, rightPath) {
  const left = stagePath(leftPath) || [];
  const right = stagePath(rightPath) || [];
  const length = Math.min(left.length, right.length);
  for (let index = 0; index < length; index++) {
    if (left[index] !== right[index]) return right[index] - left[index];
  }
  return right.length - left.length;
}

function normalizeRuntimeView(value) {
  if (!value || typeof value !== 'object') return null;
  const name = String(value.name || value.view || '');
  if (!name) return null;
  return {
    name,
    rawName: String(value.rawName || ''),
    layoutFile: String(value.layoutFile || ''),
    constructorName: String(value.constructorName || ''),
    hashCode: value.hashCode == null ? null : String(value.hashCode),
    stagePath: stagePath(value.stagePath),
    visible: value.visible !== false,
    displayed: value.displayed !== false,
    open: value.open !== false,
    useBackground: value.useBackground == null ? null : !!value.useBackground,
    clickBackgroundToClose: value.clickBackgroundToClose == null ? null : !!value.clickBackgroundToClose,
    backgroundTouchEnabled: value.backgroundTouchEnabled == null ? null : !!value.backgroundTouchEnabled,
    instanceSource: String(value.instanceSource || ''),
    instanceKey: String(value.instanceKey || ''),
  };
}

function normalizeRuntimeOverlay(value) {
  if (!value || typeof value !== 'object') return null;
  const kind = String(value.kind || 'unknown-interactive-overlay');
  const authority = String(value.authority || 'unknown');
  const nodeStagePath = stagePath(value.nodeStagePath);
  if (!nodeStagePath) return null;
  return {
    schema: RUNTIME_OVERLAY_SCHEMA,
    id: String(value.id || `${kind}:${nodeStagePath.join('.')}`),
    kind,
    authority,
    manager: String(value.manager || ''),
    managerField: String(value.managerField || ''),
    layer: value.layer && typeof value.layer === 'object' ? {
      name: String(value.layer.name || ''),
      index: value.layer.index == null ? null : Number(value.layer.index),
      stagePath: stagePath(value.layer.stagePath),
    } : null,
    nodeStagePath,
    nodePath: String(value.nodePath || ''),
    active: value.active !== false,
    visible: value.visible !== false,
    displayed: value.displayed !== false,
    interactive: value.interactive !== false,
    currentView: normalizeRuntimeView(value.currentView),
    candidates: (Array.isArray(value.candidates) ? value.candidates : []).map(normalizeRuntimeView).filter(Boolean),
    gate: value.gate && typeof value.gate === 'object' ? {
      pendingKeys: Array.isArray(value.gate.pendingKeys) ? value.gate.pendingKeys.map(String) : [],
      ready: value.gate.ready === true,
      visible: value.gate.visible !== false,
      releaseCondition: String(value.gate.releaseCondition || ''),
    } : null,
    node: value.node && typeof value.node === 'object' ? value.node : null,
    evidence: Array.isArray(value.evidence) ? value.evidence : [],
  };
}

function validateRuntimeOverlayPolicy(policy) {
  const errors = [];
  if (!policy || Number(policy.schema) !== RUNTIME_OVERLAY_POLICY_SCHEMA_VERSION) errors.push('schema');
  if (policy && policy.default !== 'unknown-hard-stop') errors.push('default');
  if (!Array.isArray(policy && policy.entries)) errors.push('entries');
  const ids = new Set();
  for (const entry of policy && policy.entries || []) {
    if (!entry || !entry.id || ids.has(entry.id)) errors.push(`duplicate-or-empty:${entry && entry.id}`);
    ids.add(entry && entry.id);
    if (!entry || !entry.match || !entry.match.kind || !entry.match.authority) errors.push(`match:${entry && entry.id}`);
    if (!['resolve-current-view', 'wait-for-release'].includes(entry && entry.action)) errors.push(`action:${entry && entry.id}`);
    if (entry && entry.action === 'wait-for-release' && (!Number.isInteger(Number(entry.timeoutMs)) || Number(entry.timeoutMs) <= 0)) {
      errors.push(`timeoutMs:${entry.id}`);
    }
    if (!entry || !entry.source || !entry.source.authorityId) errors.push(`source:${entry && entry.id}`);
  }
  if (errors.length) throw new Error(`RUNTIME_OVERLAY_POLICY_INVALID: ${errors.join(',')}`);
  return policy;
}

function loadRuntimeOverlayPolicy(filePath) {
  const absolute = path.resolve(filePath);
  const policy = JSON.parse(fs.readFileSync(absolute, 'utf8'));
  validateRuntimeOverlayPolicy(policy);
  Object.defineProperty(policy, '__file', { value: absolute, enumerable: false });
  return policy;
}

function matchesRule(overlay, rule) {
  return !!(overlay && rule && rule.match
    && overlay.kind === rule.match.kind && overlay.authority === rule.match.authority);
}

function classifyRuntimeOverlay(value, policy) {
  validateRuntimeOverlayPolicy(policy);
  const overlay = normalizeRuntimeOverlay(value);
  if (!overlay || !overlay.active || !overlay.visible || !overlay.displayed || !overlay.interactive) {
    return { pass: true, action: 'inactive', overlay, rule: null, view: null, reason: 'overlay is not an active input blocker' };
  }
  const rule = policy.entries.find(entry => matchesRule(overlay, entry)) || null;
  if (!rule) {
    return { pass: false, action: 'unknown-hard-stop', overlay, rule: null, view: null, reason: 'interactive runtime overlay has no source-backed policy' };
  }
  if (rule.action === 'resolve-current-view') {
    const view = overlay.currentView && overlay.currentView.name || null;
    if (!view || !overlay.currentView.visible || !overlay.currentView.displayed || !overlay.currentView.open) {
      return { pass: false, action: 'unknown-hard-stop', overlay, rule, view, reason: 'managed background current view is missing or inactive' };
    }
    return { pass: true, action: rule.action, overlay, rule, view, reason: 'overlay is owned by the authoritative current managed view' };
  }
  const ready = !!(overlay.gate && overlay.gate.ready);
  return {
    pass: ready,
    action: ready ? 'released' : 'wait-for-release',
    overlay,
    rule,
    view: null,
    timeoutMs: Number(rule.timeoutMs),
    reason: ready ? 'input gate release condition is satisfied' : 'source-backed global input gate is still active',
  };
}

function runtimeOverlayDecisions(snapshot, policy) {
  const overlays = Array.isArray(snapshot && snapshot.runtimeOverlays) ? snapshot.runtimeOverlays : [];
  return overlays.map(value => classifyRuntimeOverlay(value, policy))
    .filter(decision => decision.action !== 'inactive' && decision.action !== 'released')
    .sort((left, right) => compareStagePathsTopFirst(
      left.overlay && (left.overlay.currentView && left.overlay.currentView.stagePath || left.overlay.nodeStagePath),
      right.overlay && (right.overlay.currentView && right.overlay.currentView.stagePath || right.overlay.nodeStagePath),
    ));
}

function runtimeOverlayViews(snapshot, policy) {
  const seen = new Set();
  const result = [];
  for (const decision of runtimeOverlayDecisions(snapshot, policy)) {
    if (decision.action !== 'resolve-current-view' || !decision.view || seen.has(decision.view)) continue;
    seen.add(decision.view);
    result.push({
      view: decision.view,
      source: 'runtime-overlay',
      resolved: true,
      stagePath: decision.overlay.currentView.stagePath || decision.overlay.nodeStagePath,
      rootPath: null,
      childIndex: (decision.overlay.currentView.stagePath || decision.overlay.nodeStagePath).slice(-1)[0],
      zOrder: decision.overlay.node && Number.isFinite(Number(decision.overlay.node.zOrder))
        ? Number(decision.overlay.node.zOrder) : null,
      instance: decision.overlay.currentView.instanceSource && decision.overlay.currentView.instanceKey
        ? [{ source: decision.overlay.currentView.instanceSource, key: decision.overlay.currentView.instanceKey }] : [],
      overlay: decision.overlay,
    });
  }
  return result;
}

function verifyRuntimeOverlayAuthority(policy, legacyRoot, existsSync = fs.existsSync) {
  validateRuntimeOverlayPolicy(policy);
  const results = [];
  for (const [id, authority] of Object.entries(policy.authority || {})) {
    const target = path.resolve(legacyRoot, authority.pathFromLegacyRoot || '');
    const exists = existsSync(target);
    const actual = exists ? crypto.createHash('sha256').update(fs.readFileSync(target)).digest('hex') : null;
    results.push({
      id: `runtime-overlay:${id}`,
      target,
      exists,
      expectedSha256: authority.sha256 || null,
      actualSha256: actual,
      pass: exists && (!authority.sha256 || authority.sha256 === actual),
    });
  }
  return results;
}

module.exports = {
  RUNTIME_OVERLAY_SCHEMA,
  stagePath,
  compareStagePathsTopFirst,
  normalizeRuntimeView,
  normalizeRuntimeOverlay,
  validateRuntimeOverlayPolicy,
  loadRuntimeOverlayPolicy,
  classifyRuntimeOverlay,
  runtimeOverlayDecisions,
  runtimeOverlayViews,
  verifyRuntimeOverlayAuthority,
};
