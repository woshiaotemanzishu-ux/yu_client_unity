'use strict';

const fs = require('fs');
const path = require('path');
const { POPUP_POLICY_SCHEMA_VERSION } = require('./version.cjs');

function validatePopupPolicy(policy) {
  const errors = [];
  if (!policy || Number(policy.schema) !== POPUP_POLICY_SCHEMA_VERSION) errors.push('schema');
  if (!['unknown-hard-stop', 'hard_stop'].includes(policy && policy.default)) errors.push('default');
  if (!Array.isArray(policy && policy.entries)) errors.push('entries');
  const names = new Set();
  for (const entry of policy && policy.entries || []) {
    if (!entry || !entry.view || names.has(entry.view)) errors.push(`duplicate-or-empty:${entry && entry.view}`);
    names.add(entry && entry.view);
    if (!['allow', 'forbid'].includes(entry && entry.action)) errors.push(`action:${entry && entry.view}`);
    if (entry && entry.action === 'allow' && (!entry.safeClose || !entry.safeClose.kind)) errors.push(`safeClose:${entry.view}`);
    if (entry && entry.action === 'forbid' && entry.safeClose) errors.push(`forbid-safeClose:${entry.view}`);
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

function orderPopupQueue(items, policy) {
  return dedupePopupQueue(items).sort((left, right) => {
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
  orderPopupQueue,
  planPopupDrain,
  assertSafePopupDecision,
};
