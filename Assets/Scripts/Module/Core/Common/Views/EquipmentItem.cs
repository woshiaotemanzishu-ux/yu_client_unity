using System;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 通用装备格子(对标老客户端 common/EquipmentItem.ts):比 BaseAwardItem 更重的装备件 —— 底板+图标 外加
    /// 强化等级/品阶/星级/觉醒/技能/星装/宠物装/神装/龙魂/限时/红点/特效 等一大票覆盖层。被装备/背包/角色等复用。
    ///
    /// 公开 API 对标:SetData(typeId,num,lock,select)/SetCount/SetSelect/SetLock/SetScale/SetClickCallBack。
    /// 降级:EquipModel/GoodsModel(type_id→图标/品质/强化/星级…)+ tips 未移植 → 所有动态覆盖层 OnInit 默认隐藏、
    /// 图标/品质待接打 TODO;数量/锁/选中/缩放/点击回调即时可用。保留底板 item_bg + 图标 icon。
    /// </summary>
    public sealed class EquipmentItem : EquipmentItemBind
    {
        private Action _clickCb;
        private int _typeId;

        protected override void OnInit()
        {
            // 所有动态覆盖层默认隐藏(数据接上后按需开),保留底板 item_bg + 图标 icon
            HideAll(@lock, select_image, stren, _img_grade_bg, grade, num_text, red_dot, up_image,
                lung_con, star_bg, _Group1, lung_star_num, skillImg, _bad_icon, _img_awaken, _img_tips,
                starEquipGp, _pet_equip_gp, god_icon, star_group, effect_con, _lb_name,
                effBox, effBox1, effBox2, time_limit);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            BindClick();
        }

        /// <summary>填装备(对标 SetData 核心:type_id + 数量 + 锁 + 选中)。图标/品质/强化/星级待 EquipModel/GoodsModel。</summary>
        public void SetData(int typeId, long num, bool isLock = false, bool select = false)
        {
            _typeId = typeId;
            SetCount(num);
            SetLock(isLock);
            SetSelect(select);
            RefreshIcon();
        }

        /// <summary>数量(>1 才显示)。</summary>
        public void SetCount(long num)
        {
            if (num_text == null) return;
            bool show = num > 1;
            num_text.gameObject.SetActive(show);
            if (show) num_text.text = num.ToString();
        }

        /// <summary>选中态。</summary>
        public void SetSelect(bool select)
        {
            if (select_image != null) select_image.gameObject.SetActive(select);
        }

        /// <summary>锁定态。</summary>
        public void SetLock(bool locked)
        {
            if (@lock != null) @lock.gameObject.SetActive(locked);
        }

        /// <summary>整体缩放(基准 127px 格子)。</summary>
        public void SetScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
        }

        /// <summary>点击回调;未设则默认装备 tips(待移植)。</summary>
        public void SetClickCallBack(Action callback)
        {
            _clickCb = callback;
        }

        private void RefreshIcon()
        {
            // TODO 待对接 EquipModel/GoodsModel:type_id→goods_icon/品质 color/强化 stren/品阶 grade/星级/觉醒…
            if (_typeId != 0)
                GameLog.Info("Common", "EquipmentItem typeId={0} 图标/品质/强化/星级 待对接 EquipModel/GoodsModel", _typeId);
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
            GameLog.Info("Common", "装备点击 typeId={0} → 待对接 装备 tips(UIToolTipMgr)", _typeId);
        }

        private static void HideAll(params Component[] comps)
        {
            foreach (var c in comps)
                if (c != null) c.gameObject.SetActive(false);
        }
    }
}
