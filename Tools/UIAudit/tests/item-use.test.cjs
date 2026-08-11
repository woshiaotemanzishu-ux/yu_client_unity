'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const { normalizeRuntimeSources } = require('../lib/runtime-tree.cjs');
const { inspectItemUseSnapshot, evaluateStableItemUseFrames, evaluateQueueTransition } = require('../lib/item-use.cjs');

const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-sources.json'), 'utf8'));
const expected = { view: 'ItemUseView', typeId: 38070001, name: '离线挂机卡', bottom: '剩余不足2小时', enter: '购买', closeNode: 'close_btn' };

test('ItemUseView identity is exact across loaded view, stage root, content and type id', () => {
  const inspected = inspectItemUseSnapshot(normalizeRuntimeSources(fixture), expected);
  assert.equal(inspected.exact, true);
  assert.equal(inspected.actualTypeId, 38070001);
  assert.equal(inspected.counts.stageRoots, 1);
  assert.equal(inspected.runtime.closeHittable, true);
});

test('ItemUseView stable close needs two advancing frames with identical geometry', () => {
  const first = inspectItemUseSnapshot(normalizeRuntimeSources(fixture), expected);
  const secondFixture = structuredClone(fixture);
  secondFixture.stage.meta.frameToken = 101;
  secondFixture.stage.nodes.find(node => node.name === 'ItemUseView').frameToken = 101;
  const second = inspectItemUseSnapshot(normalizeRuntimeSources(secondFixture), expected);
  assert.equal(evaluateStableItemUseFrames([first, second]).pass, true);
  second.runtime.closeBounds.x += 2;
  assert.equal(evaluateStableItemUseFrames([first, second]).pass, false);
});

test('duplicate ItemUseView stage root invalidates exact identity', () => {
  const duplicate = structuredClone(fixture);
  duplicate.stage.nodes.push({ ...structuredClone(duplicate.stage.nodes.find(node => node.name === 'ItemUseView')), path: 'stage[0]/ItemUseView[1]', indexPath: [0, 1] });
  const inspected = inspectItemUseSnapshot(normalizeRuntimeSources(duplicate), expected);
  assert.equal(inspected.exact, false);
  assert.equal(inspected.counts.stageRoots, 2);
});

test('ItemUseView queue assertions record required change without losing unrelated queues', () => {
  const result = evaluateQueueTransition(
    { pending: [38070001, 1], recommendations: [10, 11] },
    { pending: [38070001], recommendations: [10, 11] },
    { changed: ['pending'], unchanged: ['recommendations'] },
  );
  assert.equal(result.pass, true);
  assert.deepEqual(result.changed, [{ id: 'pending', pass: true }]);
  assert.deepEqual(result.unchanged, [{ id: 'recommendations', pass: true }]);
});
