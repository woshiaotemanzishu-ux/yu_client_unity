'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('path');
const {
  loadCompletedScopePolicy,
  evaluateCompletedScope,
} = require('../lib/completed-scope.cjs');

const policy = loadCompletedScopePolicy(path.join(__dirname, '..', 'policies', 'completed-scopes.json'));

test('completed role-person scope blocks direct and descendant routes but permits sibling wing work', () => {
  assert.equal(evaluateCompletedScope({ id: 'mainui.role.person' }, policy).code, 'COMPLETED_SCOPE_REOPEN_REQUIRED');
  assert.equal(evaluateCompletedScope({ id: 'mainui.role.full-tabs.v3.person.realm' }, policy).pass, false);
  assert.equal(evaluateCompletedScope({ id: 'mainui.role.fashion-main-pos1' }, policy).pass, false);
  assert.equal(evaluateCompletedScope({ id: 'mainui.role.outward.wing' }, policy).pass, true);
});

test('new scoped evidence explicitly reopens only the protected route it names', () => {
  const result = evaluateCompletedScope({
    id: 'mainui.role.full-tabs.v3.person.realm',
    scope: {
      reopen: [{
        scopeId: 'mainui.role.person',
        reason: '新运行截图显示人物页境界入口再次错位',
        source: 'user-runtime',
        observedAt: '2026-08-11T23:00:00+08:00',
        evidence: { reference: 'user-runtime:role-person-realm-offset' },
      }],
    },
  }, policy);
  assert.equal(result.pass, true);
  assert.equal(result.code, 'COMPLETED_SCOPE_REOPEN_ACCEPTED');
  assert.deepEqual(result.acceptedReopens.map(value => value.scopeId), ['mainui.role.person']);
});

test('evidence at or before the user-confirmed completion time cannot reopen the scope', () => {
  const result = evaluateCompletedScope({
    id: 'mainui.role.person',
    scope: { reopen: [{
      scopeId: 'mainui.role.person', reason: 'older screenshot is not new evidence',
      source: 'new-runtime-evidence', observedAt: '2026-08-11T22:20:00+08:00',
      evidence: { reference: 'old-run' },
    }] },
  }, policy);
  assert.equal(result.pass, false);
  assert.equal(result.code, 'COMPLETED_SCOPE_REOPEN_REQUIRED');
});
