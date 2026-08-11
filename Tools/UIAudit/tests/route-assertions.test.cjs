'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { normalizedNode } = require('../lib/runtime-tree.cjs');
const {
  evaluateNodeAssertions,
  evaluateGeometryAssertion,
  evaluateScrollAssertion,
  evaluateBranchCondition,
} = require('../lib/route-assertions.cjs');

function node(name, bounds, extra = {}) {
  return normalizedNode({ name, visible: true, bounds, ...extra }, { source: 'laya-stage', view: 'FixtureView', path: name });
}

test('positive and negative node assertions are both data-driven', () => {
  const snapshot = { nodes: [node('present', { x: 0, y: 0, width: 10, height: 10 })] };
  const result = evaluateNodeAssertions(snapshot, [
    { selector: { name: 'present' }, exists: true },
    { selector: { name: 'forbidden' }, exists: false },
  ]);
  assert.equal(result.pass, true);
  assert.equal(evaluateBranchCondition(snapshot, { kind: 'nodes', selector: { name: 'present' }, exists: true }).pass, true);
});

test('geometry expresses inside, partial and exact numeric constraints', () => {
  const snapshot = { nodes: [
    node('viewport', { x: 10, y: 10, width: 100, height: 100 }),
    node('item', { x: 90, y: 20, width: 40, height: 20 }),
  ] };
  const result = evaluateGeometryAssertion(snapshot, {
    selector: { name: 'item' }, bounds: { x: { eq: 90 } },
    withinSelector: { name: 'viewport' }, clipMode: 'partial',
  });
  assert.equal(result.pass, true);
});

test('scroll assertion proves movement, clipping and last-item reachability', () => {
  const before = { nodes: [
    node('list', { x: 0, y: 0, width: 100, height: 100 }, { scroll: { v: { value: 0, min: 0, max: 200 } } }),
  ] };
  const after = { nodes: [
    node('list', { x: 0, y: 0, width: 100, height: 100 }, { scroll: { v: { value: 80, min: 0, max: 200 } } }),
    node('row', { x: 0, y: -20, width: 100, height: 30 }),
    node('last', { x: 0, y: 70, width: 100, height: 30 }),
  ] };
  const result = evaluateScrollAssertion(before, after, {
    containerSelector: { name: 'list' }, axis: 'y', minAbsDelta: 50,
    itemSelector: { name: 'row' }, requireClipped: true,
    lastItemSelector: { name: 'last' }, lastItemClipMode: 'inside',
  });
  assert.equal(result.pass, true);
  assert.deepEqual(result.checks.map(value => value.kind), ['movement', 'clipping', 'last-item']);
});
