'use strict';

const { clickRuntimeTarget } = require('./canvas-input.cjs');

const ITEM_USE_CLOSE_AUTHORITY = Object.freeze({
  source: '../yu_client/h5/src/common/ItemUseView.ts',
  sha256: '61b8f5d0a75962ab3501c9c025793da2a4eafcb4fe91163cc21d5db5f8c9762f',
  lines: '302-312,382-409',
  meaning: 'close_btn only removes the recommendation from ItemUseModel and closes or advances the popup; it never calls EnterFunc or a use protocol',
});

function sleep(ms) { return new Promise(resolve => setTimeout(resolve, ms)); }

async function readItemUseState(page, tracked = null) {
  return page.evaluate(trackedItem => {
    const number = value => Number.isFinite(Number(value)) ? Number(value) : 0;
    const Manager = window.ViewManager;
    const manager = Manager && Manager.GetInstance && Manager.GetInstance();
    const view = manager && manager.GetView && manager.GetView('ItemUseView');
    const Goods = window.GoodsModel;
    const goods = Goods && Goods.GetInstance && Goods.GetInstance();
    const readBag = value => {
      if (!value || !goods) return null;
      const typeId = number(value.typeId);
      const goodsId = number(value.goodsId);
      const vo = goodsId && goods.GetGoodsVoByGoodsId ? goods.GetGoodsVoByGoodsId(goodsId) : null;
      return {
        typeId,
        goodsId,
        goodsCount: vo ? number(vo.goods_num) : 0,
        typeCount: typeId && goods.GetBagGoodsNum ? number(goods.GetBagGoodsNum(typeId)) : 0,
      };
    };
    if (!view || typeof view.GetTypeId !== 'function' || typeof view.GetGoodsId !== 'function') {
      return { open: false, trackedBag: readBag(trackedItem) };
    }
    const display = view.display_obj;
    const displayedInStage = !!(display && display.displayedInStage);
    const isPop = view.isPop === true;
    if (!isPop || !displayedInStage || display.visible === false) {
      return {
        open: false,
        cached: true,
        viewHash: String(view.hashCode == null ? '' : view.hashCode),
        isPop,
        displayedInStage,
        trackedBag: readBag(trackedItem),
      };
    }
    const typeId = number(view.GetTypeId());
    const goodsId = number(view.GetGoodsId());
    const nameLabel = view.name_label;
    const enterButtonText = view.enter_btn_text;
    const optionalText = target => {
      try { return String(target && target.text || ''); } catch (_) { return ''; }
    };
    return {
      open: true,
      viewHash: String(view.hashCode == null ? '' : view.hashCode),
      typeId,
      goodsId,
      identityReady: typeId > 0 && goodsId > 0,
      isPop,
      displayedInStage,
      itemName: optionalText(nameLabel),
      enterLabel: optionalText(enterButtonText),
      animating: !!view.is_anim,
      closeReady: !!(view.close_btn && view.close_btn.visible !== false && view.close_btn.mouseEnabled !== false),
      currentBag: readBag({ typeId, goodsId }),
      trackedBag: readBag(trackedItem),
    };
  }, tracked);
}

function verifyItemUseDismissal(before, after) {
  if (!before || !before.open || !before.closeReady || before.identityReady === false
      || Number(before.typeId) <= 0 || Number(before.goodsId) <= 0) {
    return { pass: false, reason: 'item-use-close-not-ready' };
  }
  const tracked = after && after.trackedBag;
  const original = before.currentBag;
  if (original && tracked && (tracked.goodsCount < original.goodsCount || tracked.typeCount < original.typeCount)) {
    return { pass: false, reason: 'bag-count-decreased' };
  }
  if (after && after.open
      && Number(after.typeId) === Number(before.typeId)
      && Number(after.goodsId) === Number(before.goodsId)) {
    return { pass: false, reason: 'same-item-still-open' };
  }
  return {
    pass: true,
    reason: after && after.open ? 'dismissed-and-next-item-requeued' : 'dismissed-and-closed',
  };
}

function createControlledItemUseHandler(session) {
  if (!session || !session.page || typeof session.snapshot !== 'function') {
    throw new Error('ITEM_USE_HANDLER_SESSION_INVALID');
  }
  return async () => {
    let before = await readItemUseState(session.page);
    const animationDeadline = Date.now() + 3000;
    while (before.open && before.animating && Date.now() < animationDeadline) {
      await sleep(150);
      before = await readItemUseState(session.page);
    }
    if (!before.open || !before.closeReady || !before.identityReady || before.animating) {
      throw new Error(`ITEM_USE_CONTROLLED_CLOSE_NOT_READY: ${JSON.stringify(before)}`);
    }
    const snapshot = await session.snapshot();
    const selector = {
      source: 'laya-stage',
      ownerView: 'ItemUseView',
      boundField: 'close_btn',
      expectedCount: 1,
    };
    const click = await clickRuntimeTarget(session.page, snapshot, selector);
    let after = await readItemUseState(session.page, before);
    const closeDeadline = Date.now() + 3000;
    while (after.open
      && Number(after.typeId) === Number(before.typeId)
      && Number(after.goodsId) === Number(before.goodsId)
      && Date.now() < closeDeadline) {
      await sleep(150);
      after = await readItemUseState(session.page, before);
    }
    const verification = verifyItemUseDismissal(before, after);
    session.note('item-use-controlled-dismiss', {
      authority: ITEM_USE_CLOSE_AUTHORITY,
      before,
      after,
      verification,
      input: click.input,
    });
    if (!verification.pass) {
      throw new Error(`ITEM_USE_CONTROLLED_CLOSE_FAILED: ${JSON.stringify(verification)}`);
    }
    return { before, after, verification, input: click.input };
  };
}

module.exports = {
  ITEM_USE_CLOSE_AUTHORITY,
  readItemUseState,
  verifyItemUseDismissal,
  createControlledItemUseHandler,
};
