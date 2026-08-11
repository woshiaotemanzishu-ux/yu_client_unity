'use strict';

const { findNodes, findExactNode } = require('./runtime-tree.cjs');

function numericConstraint(value, constraint) {
  if (typeof constraint === 'number') return Number(value) === Number(constraint);
  if (!constraint || typeof constraint !== 'object') return true;
  const actual = Number(value);
  if (!Number.isFinite(actual)) return false;
  const tolerance = Number(constraint.tolerance || 0);
  if (constraint.eq != null && Math.abs(actual - Number(constraint.eq)) > tolerance) return false;
  if (constraint.min != null && actual < Number(constraint.min) - tolerance) return false;
  if (constraint.max != null && actual > Number(constraint.max) + tolerance) return false;
  return true;
}

function countConstraint(count, assertion) {
  if (assertion.exists === false) return count === 0;
  const expected = assertion.count == null ? { min: 1 } : assertion.count;
  if (typeof expected === 'number') return count === Number(expected);
  return numericConstraint(count, expected);
}

function evaluateNodeAssertions(snapshot, assertions) {
  const results = (assertions || []).map(assertion => {
    const matches = findNodes(snapshot, assertion.selector || {});
    return { assertion, count: matches.length, pass: countConstraint(matches.length, assertion), matches };
  });
  return { pass: results.every(result => result.pass), results };
}

function intersection(left, right) {
  if (!left || !right) return null;
  const x = Math.max(Number(left.x), Number(right.x));
  const y = Math.max(Number(left.y), Number(right.y));
  const rightEdge = Math.min(Number(left.x) + Number(left.width), Number(right.x) + Number(right.width));
  const bottomEdge = Math.min(Number(left.y) + Number(left.height), Number(right.y) + Number(right.height));
  return { x, y, width: Math.max(0, rightEdge - x), height: Math.max(0, bottomEdge - y) };
}

function rectArea(rect) {
  return rect ? Math.max(0, Number(rect.width)) * Math.max(0, Number(rect.height)) : 0;
}

function clipModePass(target, viewport, mode) {
  const overlap = intersection(target, viewport);
  const targetArea = rectArea(target);
  const overlapArea = rectArea(overlap);
  if (mode === 'outside') return overlapArea === 0;
  if (mode === 'partial') return overlapArea > 0 && overlapArea < targetArea;
  if (mode === 'inside') return targetArea > 0 && overlapArea === targetArea;
  if (mode === 'intersects') return overlapArea > 0;
  return false;
}

function evaluateGeometryAssertion(snapshot, assertion) {
  const target = findExactNode(snapshot, assertion.selector || {});
  const checks = [];
  for (const [field, constraint] of Object.entries(assertion.bounds || {})) {
    checks.push({ kind: `bounds.${field}`, actual: target.bounds && target.bounds[field], constraint, pass: numericConstraint(target.bounds && target.bounds[field], constraint) });
  }
  if (assertion.withinSelector) {
    const viewport = findExactNode(snapshot, assertion.withinSelector);
    const mode = assertion.clipMode || 'inside';
    checks.push({ kind: 'clip', mode, target: target.bounds, viewport: viewport.bounds, pass: clipModePass(target.bounds, viewport.bounds, mode) });
  }
  return { pass: checks.every(check => check.pass), assertion, target, checks };
}

function scrollValue(node, axis) {
  const key = axis === 'x' || axis === 'h' ? 'h' : 'v';
  const value = node && node.state && node.state.scroll && node.state.scroll[key];
  return value ? Number(value.value) : null;
}

function evaluateScrollAssertion(before, after, assertion) {
  const axis = assertion.axis === 'x' || assertion.axis === 'h' ? 'x' : 'y';
  const beforeContainer = findExactNode(before, assertion.containerSelector || assertion.selector || {});
  const afterContainer = findExactNode(after, assertion.containerSelector || assertion.selector || {});
  let beforeValue = scrollValue(beforeContainer, axis);
  let afterValue = scrollValue(afterContainer, axis);
  let source = 'scrollbar';
  if ((beforeValue == null || afterValue == null) && assertion.contentSelector) {
    const beforeContent = findExactNode(before, assertion.contentSelector);
    const afterContent = findExactNode(after, assertion.contentSelector);
    beforeValue = Number(beforeContent.bounds && beforeContent.bounds[axis]);
    afterValue = Number(afterContent.bounds && afterContent.bounds[axis]);
    source = 'content-bounds';
  }
  const delta = beforeValue == null || afterValue == null ? null : afterValue - beforeValue;
  const checks = [{
    kind: 'movement', source, before: beforeValue, after: afterValue, delta,
    pass: delta != null && Math.abs(delta) >= Number(assertion.minAbsDelta == null ? 1 : assertion.minAbsDelta),
  }];
  let viewport = null;
  if (assertion.viewportSelector) viewport = findExactNode(after, assertion.viewportSelector);
  else viewport = afterContainer;
  if (assertion.itemSelector && assertion.requireClipped) {
    const items = findNodes(after, { ...assertion.itemSelector, visible: false });
    const clipped = items.filter(item => !clipModePass(item.bounds, viewport.bounds, 'inside'));
    checks.push({ kind: 'clipping', total: items.length, clipped: clipped.length, pass: items.length > 0 && clipped.length > 0 });
  }
  if (assertion.lastItemSelector) {
    const last = findExactNode(after, assertion.lastItemSelector);
    checks.push({ kind: 'last-item', target: last.bounds, viewport: viewport.bounds, pass: clipModePass(last.bounds, viewport.bounds, assertion.lastItemClipMode || 'inside') });
  }
  return { pass: checks.every(check => check.pass), assertion, checks };
}

function evaluateBranchCondition(snapshot, condition) {
  if (!condition || condition.kind === 'nodes') return evaluateNodeAssertions(snapshot, [condition || {}]);
  if (condition.kind === 'geometry') return evaluateGeometryAssertion(snapshot, condition);
  throw new Error(`UNSUPPORTED_BRANCH_CONDITION: ${condition && condition.kind}`);
}

module.exports = {
  numericConstraint,
  countConstraint,
  evaluateNodeAssertions,
  intersection,
  rectArea,
  clipModePass,
  evaluateGeometryAssertion,
  scrollValue,
  evaluateScrollAssertion,
  evaluateBranchCondition,
};
