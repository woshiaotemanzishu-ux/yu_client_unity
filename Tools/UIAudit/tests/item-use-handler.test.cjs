'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { ITEM_USE_CLOSE_AUTHORITY, readItemUseState, verifyItemUseDismissal } = require('../lib/item-use-handler.cjs');

const before = {
  open: true,
  closeReady: true,
  viewHash: '101',
  typeId: 18030001,
  goodsId: 9001,
  identityReady: true,
  currentBag: { typeId: 18030001, goodsId: 9001, goodsCount: 1, typeCount: 1 },
};

test('controlled ItemUse dismissal accepts a new queued instance without consuming the item', () => {
  const result = verifyItemUseDismissal(before, {
    open: true,
    viewHash: '102',
    typeId: 38070001,
    goodsId: 9002,
    trackedBag: { typeId: 18030001, goodsId: 9001, goodsCount: 1, typeCount: 1 },
  });
  assert.deepEqual(result, { pass: true, reason: 'dismissed-and-next-item-requeued' });
});

test('controlled ItemUse dismissal accepts a changed item in a reused view instance', () => {
  const result = verifyItemUseDismissal(before, {
    open: true,
    viewHash: '101',
    typeId: 38070001,
    goodsId: 9002,
    trackedBag: { typeId: 18030001, goodsId: 9001, goodsCount: 1, typeCount: 1 },
  });
  assert.deepEqual(result, { pass: true, reason: 'dismissed-and-next-item-requeued' });
});

test('controlled ItemUse dismissal rejects any bag count decrease', () => {
  const result = verifyItemUseDismissal(before, {
    open: false,
    trackedBag: { typeId: 18030001, goodsId: 9001, goodsCount: 0, typeCount: 0 },
  });
  assert.deepEqual(result, { pass: false, reason: 'bag-count-decreased' });
});

test('controlled ItemUse dismissal rejects the same live view instance', () => {
  const result = verifyItemUseDismissal(before, {
    open: true,
    viewHash: '101',
    typeId: 18030001,
    goodsId: 9001,
    trackedBag: { typeId: 18030001, goodsId: 9001, goodsCount: 1, typeCount: 1 },
  });
  assert.deepEqual(result, { pass: false, reason: 'same-item-still-open' });
});

test('controlled ItemUse dismissal requires numeric runtime item identity but not display text', () => {
  const result = verifyItemUseDismissal({
    open: true,
    closeReady: true,
    identityReady: false,
    typeId: 0,
    goodsId: 0,
    itemName: '',
  }, { open: false });
  assert.deepEqual(result, { pass: false, reason: 'item-use-close-not-ready' });
});

test('ItemUse runtime identity survives disposed optional text renderers', async () => {
  const previousWindow = global.window;
  const disposedText = {};
  Object.defineProperty(disposedText, 'text', { get() { throw new Error('disposed'); } });
  global.window = {
    ViewManager: { GetInstance: () => ({ getView: true, GetView: () => ({
      hashCode: 33,
      GetTypeId: () => 38070001,
      GetGoodsId: () => 4294967001,
      name_label: disposedText,
      enter_btn_text: disposedText,
      is_anim: false,
      isPop: true,
      display_obj: { displayedInStage: true, visible: true },
      close_btn: { visible: true, mouseEnabled: true },
    }) }) },
  };
  try {
    const state = await readItemUseState({ evaluate: async (fn, value) => fn(value) });
    assert.equal(state.identityReady, true);
    assert.equal(state.typeId, 38070001);
    assert.equal(state.goodsId, 4294967001);
    assert.equal(state.itemName, '');
    assert.equal(state.enterLabel, '');
  } finally {
    global.window = previousWindow;
  }
});

test('cached ItemUse view is not treated as a runtime-open popup', async () => {
  const previousWindow = global.window;
  global.window = {
    ViewManager: { GetInstance: () => ({ GetView: () => ({
      hashCode: 44,
      GetTypeId: () => 38070001,
      GetGoodsId: () => 4294967001,
      isPop: false,
      display_obj: { displayedInStage: false, visible: true },
    }) }) },
  };
  try {
    const state = await readItemUseState({ evaluate: async (fn, value) => fn(value) });
    assert.equal(state.open, false);
    assert.equal(state.cached, true);
    assert.equal(state.isPop, false);
    assert.equal(state.displayedInStage, false);
  } finally {
    global.window = previousWindow;
  }
});

test('controlled ItemUse authority is pinned to source evidence', () => {
  assert.match(ITEM_USE_CLOSE_AUTHORITY.sha256, /^[a-f0-9]{64}$/);
  assert.equal(ITEM_USE_CLOSE_AUTHORITY.lines, '302-312,382-409');
});
