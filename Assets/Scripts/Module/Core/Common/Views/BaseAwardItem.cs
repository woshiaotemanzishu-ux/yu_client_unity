using System;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 通用奖励/物品格子(对标老客户端 common/BaseAwardItem.ts):全项目最常用的物品显示件 —— 品质底板 + 图标 +
    /// 数量 + 锁/选中/限时 覆盖层 + 点击 tips。被背包/装备/活动等几乎所有模块复用(克隆 BaseAwardItem.prefab)。
    ///
    /// 公开 API 对标 Laya:SetData(typeId,num,lock,select)、SetCount、SetSelect、SetLock、SetScale、SetClickCallBack。
    /// 降级:GoodsModel(type_id→goods_icon/color/类型)与 UIToolTipMgr 未移植 → 图标/品质底板待接(打 TODO,不崩);
    /// 数量/锁/选中/缩放/点击回调 即时可用;特效层(effect_con)未移植先隐藏。覆盖层 OnInit 默认隐藏。
    /// </summary>
    public sealed class BaseAwardItem : BaseAwardItemBind
    {
        private Action _clickCb;
        private int _typeId;

        protected override void OnInit()
        {
            // 动态覆盖层默认隐藏(SetData/SetSelect 再按需打开)
            if (@lock != null) @lock.gameObject.SetActive(false);
            if (select_image != null) select_image.gameObject.SetActive(false);
            if (time_limit != null) time_limit.gameObject.SetActive(false);
            if (effect_con != null) effect_con.gameObject.SetActive(false); // 物品特效待移植
            BindClick();
        }

        /// <summary>填物品(对标 SetData 核心:type_id + 数量 + 锁 + 选中)。图标/品质底板待 GoodsModel。</summary>
        public void SetData(int typeId, long num, bool isLock = false, bool select = false)
        {
            _typeId = typeId;
            SetCount(num);
            SetLock(isLock);
            SetSelect(select);
            RefreshIcon();
        }

        /// <summary>数量(对标 ChangeCountVisible:>1 才显示,堆叠物常规)。</summary>
        public void SetCount(long num)
        {
            if (num_text == null) return;
            bool show = num > 1;
            num_text.gameObject.SetActive(show);
            if (show) num_text.text = num.ToString();
        }

        /// <summary>选中态(对标 SetSelect)。</summary>
        public void SetSelect(bool select)
        {
            if (select_image != null) select_image.gameObject.SetActive(select);
        }

        /// <summary>锁定态(对标 is_lock==1)。</summary>
        public void SetLock(bool locked)
        {
            if (@lock != null) @lock.gameObject.SetActive(locked);
        }

        /// <summary>整体缩放(对标 SetScale:基准 127px 格子)。</summary>
        public void SetScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
        }

        /// <summary>点击回调(对标 SetClickCallBack);未设则走默认 tips(待 UIToolTipMgr)。</summary>
        public void SetClickCallBack(Action callback)
        {
            _clickCb = callback;
        }

        /// <summary>图标 + 品质底板:待 GoodsModel(type_id→goods_icon/color)移植后接;现降级不设、不崩。</summary>
        private void RefreshIcon()
        {
            // TODO 待对接 GoodsModel:
            //   var basic = GoodsModel.GetGoodsBasicByTypeId(_typeId);
            //   item_bg ← AtlasUrl("common","com_goods_plate_"+basic.color); icon ← GameResPath.GetGoodsIconPath(basic.goods_icon)
            if (_typeId != 0)
                GameLog.Info("Common", "BaseAwardItem typeId={0} 图标/品质底板 待对接 GoodsModel", _typeId);
        }

        private void BindClick()
        {
            if (click_group == null) return;
            Image img = click_group.GetComponent<Image>();
            if (img == null) img = click_group.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, OnClick);
        }

        private void OnClick()
        {
            if (_clickCb != null) { _clickCb(); return; }
            GameLog.Info("Common", "物品点击 typeId={0} → 待对接 UIToolTipMgr 物品 tips", _typeId);
        }
    }
}
