'use strict';

const fs = require('fs');

const POLICY_SCHEMA = 1;

function validateCompletedScopePolicy(policy) {
  if (!policy || Number(policy.schema) !== POLICY_SCHEMA || !Array.isArray(policy.entries)) {
    throw new Error('COMPLETED_SCOPE_POLICY_INVALID');
  }
  const ids = new Set();
  for (const entry of policy.entries) {
    if (!entry || typeof entry.id !== 'string' || !entry.id || ids.has(entry.id)) {
      throw new Error(`COMPLETED_SCOPE_POLICY_ID_INVALID: ${entry && entry.id}`);
    }
    ids.add(entry.id);
    if (entry.status !== 'completed' || entry.protected !== true) {
      throw new Error(`COMPLETED_SCOPE_POLICY_STATE_INVALID: ${entry.id}`);
    }
    if (!Array.isArray(entry.routeIds) || !Array.isArray(entry.routePrefixes)
      || entry.routeIds.concat(entry.routePrefixes).some(value => typeof value !== 'string' || !value)) {
      throw new Error(`COMPLETED_SCOPE_POLICY_ROUTES_INVALID: ${entry.id}`);
    }
    if (!entry.routeIds.length && !entry.routePrefixes.length) {
      throw new Error(`COMPLETED_SCOPE_POLICY_ROUTES_EMPTY: ${entry.id}`);
    }
    if (!entry.observedAt || Number.isNaN(Date.parse(entry.observedAt))) {
      throw new Error(`COMPLETED_SCOPE_POLICY_TIME_INVALID: ${entry.id}`);
    }
  }
  return policy;
}

function loadCompletedScopePolicy(filePath) {
  return validateCompletedScopePolicy(JSON.parse(fs.readFileSync(filePath, 'utf8')));
}

function routeMatches(entry, routeId) {
  return entry.routeIds.includes(routeId)
    || entry.routePrefixes.some(prefix => routeId === prefix || routeId.startsWith(`${prefix}.`));
}

function validReopen(reopen, entry) {
  if (!reopen || reopen.scopeId !== entry.id) return false;
  if (typeof reopen.reason !== 'string' || reopen.reason.trim().length < 8) return false;
  if (!['user-runtime', 'new-runtime-evidence', 'shared-impact'].includes(reopen.source)) return false;
  if (!reopen.observedAt || Number.isNaN(Date.parse(reopen.observedAt))) return false;
  if (Date.parse(reopen.observedAt) <= Date.parse(entry.observedAt)) return false;
  return !!(reopen.evidence && typeof reopen.evidence.reference === 'string' && reopen.evidence.reference.trim());
}

function evaluateCompletedScope(route, policy) {
  validateCompletedScopePolicy(policy);
  const routeId = route && route.id || '';
  const matches = policy.entries.filter(entry => routeMatches(entry, routeId));
  if (!matches.length) {
    return { pass: true, code: 'COMPLETED_SCOPE_NOT_TARGETED', routeId, matchedScopes: [], acceptedReopens: [] };
  }
  const reopenList = route && route.scope && Array.isArray(route.scope.reopen) ? route.scope.reopen : [];
  const missingScopes = [];
  const acceptedReopens = [];
  for (const entry of matches) {
    const reopen = reopenList.find(candidate => candidate && candidate.scopeId === entry.id);
    if (!validReopen(reopen, entry)) missingScopes.push(entry.id);
    else acceptedReopens.push({ scopeId: entry.id, source: reopen.source, observedAt: reopen.observedAt, evidence: reopen.evidence });
  }
  return {
    pass: missingScopes.length === 0,
    code: missingScopes.length ? 'COMPLETED_SCOPE_REOPEN_REQUIRED' : 'COMPLETED_SCOPE_REOPEN_ACCEPTED',
    routeId,
    matchedScopes: matches.map(entry => ({ id: entry.id, label: entry.label, reason: entry.reason, observedAt: entry.observedAt })),
    missingScopes,
    acceptedReopens,
  };
}

module.exports = {
  POLICY_SCHEMA,
  validateCompletedScopePolicy,
  loadCompletedScopePolicy,
  routeMatches,
  evaluateCompletedScope,
};
