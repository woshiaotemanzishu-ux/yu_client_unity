using System;
using Shenxiao.Framework.Res;
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
        private bool _inited;

        protected override void OnInit()
        {
            EnsureInit();
        }

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            // 所有动态覆盖层默认隐藏(数据接上后按需开),保留底板 item_bg + 图标 icon
            HideAll(@lock, select_image, stren, _img_grade_bg, grade, num_text, red_dot, up_image,
                lung_con, star_bg, _Group1, lung_star_num, skillImg, _bad_icon, _img_awaken, _img_tips,
                starEquipGp, _pet_equip_gp, god_icon, star_group, effect_con, _lb_name,
                effBox, effBox1, effBox2, time_limit);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            BindClick();
        }

        /// <summary>填装备/物品(对标 SetData 核心:type_id + 数量 + 锁 + 选中)。</summary>
        public void SetData(int typeId, long num, bool isLock = false, bool select = false)
        {
            EnsureInit();
            _typeId = typeId;
            SetCount(num);
            SetLock(isLock);
            SetSelect(select);
            RefreshIcon();
        }

        /// <summary>数量(>1 才显示)。大数缩写对标 BaseAwardItem/老端 FormatNumber2(20000→"2W"),避免长数字溢出格子。</summary>
        public void SetCount(long num)
        {
            if (num_text == null) return;
            bool show = num > 1;
            num_text.gameObject.SetActive(show);
            if (show) num_text.text = GoodsModel.FormatCountNum(num);
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
            RefreshIconAsync();
        }

        private async void RefreshIconAsync()
        {
            int typeId = _typeId;
            if (icon == null) return;
            if (typeId <= 0)
            {
                icon.enabled = false;
                return;
            }

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (basic == null)
            {
                icon.enabled = false;
                GameLog.Warn("Common", "EquipmentItem typeId={0} 不在 config_goods(或未加载)→ 图标降级隐藏", typeId);
                return;
            }

            if (item_bg != null)
            {
                string plateKey = GameResPath.GetIcon("common", "com_goods_plate_" + GoodsModel.GetDisplayColor(typeId));
                await ResManager.SetImageAsync(item_bg, plateKey, false, false);
                if (_typeId != typeId) return;
            }

            string iconPath = GameResPath.GetGoodsIconPath(basic.Icon);
            bool ok = await ResManager.SetImageAsync(icon, iconPath, false, false);
            if (_typeId != typeId) return;
            icon.enabled = ok;
            if (!ok)
            {
                GameLog.Warn("Common", "EquipmentItem 物品[{0}]{1} 图标未加载: key={2}", typeId, basic.Name, iconPath);
            }
        }

        private void BindClick()
        {
            if (click_group == null) return;
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
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
