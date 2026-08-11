'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { cloneWithoutCycles, safeStringify } = require('../lib/safe-json.cjs');

test('cycle-safe JSON removes only ancestor cycles', () => {
  const root = { name: 'root' };
  root.self = root;
  root.child = { owner: root, value: 7 };
  const cloned = cloneWithoutCycles(root);
  assert.equal(cloned.name, 'root');
  assert.equal(Object.hasOwn(cloned, 'self'), false);
  assert.deepEqual(cloned.child, { value: 7 });
  assert.doesNotThrow(() => JSON.parse(safeStringify(root)));
});

test('shared references are preserved as values instead of being mistaken for cycles', () => {
  const shared = { token: 'same-object' };
  const cloned = cloneWithoutCycles({ left: shared, right: shared });
  assert.deepEqual(cloned.left, { token: 'same-object' });
  assert.deepEqual(cloned.right, { token: 'same-object' });
});
