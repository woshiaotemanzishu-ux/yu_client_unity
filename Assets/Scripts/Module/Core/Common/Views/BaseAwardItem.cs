using System;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.Res;
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
    /// 图标走 <see cref="GoodsModel"/>(type_id→goods_icon/color)+ ResManager.SetImageAsync:真实图标 + 品质底板
    /// (com_goods_plate_{color},common 图集)均已接,缺图降级隐藏 + 精确 blocker(见 <see cref="RefreshIcon"/>);
    /// 数量/锁/选中/缩放/点击回调 即时可用;特效层(effect_con)、点击 tips(UIToolTipMgr)仍待移植。
    /// 注:本类逻辑就绪,但 Assets/Prefabs/UI/Common/BaseAwardItem.prefab 当前【未挂本组件】(根仅 RectTransform,
    /// 对照 ItemInfoItem.prefab 有挂),故经 prefab 实例化复用尚被阻断 —— 见第 6 轮任务包(回填 Bind 组件/修转换器)。
    /// </summary>
    public sealed class BaseAwardItem : BaseAwardItemBind
    {
        private Action _clickCb;
        private int _typeId;
        private long _num = 1;   // 堆叠数量(SetData/SetCount 同步;点击 tips 透传给 ItemTipsView,对标 GoodsTooltips quantity)
        private bool _inited;

        protected override void OnInit()
        {
            EnsureInit();
        }

        /// <summary>
        /// 幂等初始化:隐藏动态覆盖层 + 绑定点击。列表项(完成弹层/背包格)常被克隆后直接 SetData(不经 BaseView.Show),
        /// OnInit 不会自动跑 → 由 SetData 兜底调本方法,保证点击 tips(<see cref="OnClick"/>)与默认态在任何用法下就位。
        /// </summary>
        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            if (@lock != null) @lock.gameObject.SetActive(false);
            if (select_image != null) select_image.gameObject.SetActive(false);
            if (time_limit != null) time_limit.gameObject.SetActive(false);
            if (effect_con != null) effect_con.gameObject.SetActive(false); // 物品特效待移植
            BindClick();
        }

        /// <summary>填物品(对标 SetData 核心:type_id + 数量 + 锁 + 选中)。图标/品质底板待 GoodsModel。</summary>
        public void SetData(int typeId, long num, bool isLock = false, bool select = false)
        {
            EnsureInit();
            _typeId = typeId;
            SetCount(num);
            SetLock(isLock);
            SetSelect(select);
            RefreshIcon();
        }

        /// <summary>数量(对标 ChangeCountVisible:>1 才显示,堆叠物常规)。同步 <see cref="_num"/> 供点击 tips 透传。
        /// 大数缩写走 <see cref="GoodsModel.FormatCountNum"/>(对标 BaseAwardItem.ts num_text=FormatNumber2(goods_num):150000→"15W"),
        /// 避免长数字溢出 127px 格子。</summary>
        public void SetCount(long num)
        {
            _num = num;
            if (num_text == null) return;
            bool show = num > 1;
            num_text.gameObject.SetActive(show);
            if (show) num_text.text = GoodsModel.FormatCountNum(num);
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

        /// <summary>
        /// 图标 + 品质底板(对标老端 BaseAwardItem.ts SetData → item_bg.skin = AtlasUrl("common","com_goods_plate_"+color)
        /// + icon 走 GoodsModel.GetGoodsBasicByTypeId.goods_icon):底板与图标均走 <see cref="ResManager.SetImageAsync"/>
        /// (Laya SetTexture 对等;Editor 下缺图会从 yu_client/cdn 自动兜底导入)。
        /// 降级(精确 blocker):某 png 在 yu_client/cdn 与 Assets/GameRes 都找不到 → 隐藏图标位(无 skin 的 Image 占位
        /// 约定 enabled=false,icon 非点击件不影响交互),并写明缺哪个 key;真实名称仍由列表文本
        /// (<see cref="Shenxiao.Module.Core.Tasks.TaskReward.ToText"/>)经 <see cref="GoodsModel"/> 呈现。
        /// </summary>
        private async void RefreshIcon()
        {
            if (icon == null) return;
            int typeId = _typeId;
            if (typeId <= 0)
            {
                // 货币/经验数值或空格子:无 goods 图标,隐藏图标位(不画假图);底板保留 prefab 默认。
                icon.enabled = false;
                return;
            }

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (basic == null)
            {
                icon.enabled = false;
                GameLog.Warn("Common", "BaseAwardItem typeId={0} 不在 config_goods(或未加载)→ 图标降级隐藏", typeId);
                return;
            }

            // 品质底板(对标 BaseAwardItem.ts:279):color → com_goods_plate_{color}(common 图集)。
            if (item_bg != null)
            {
                string plateKey = GameResPath.GetIcon("common", "com_goods_plate_" + GoodsModel.GetDisplayColor(typeId));
                bool plateOk = await ResManager.SetImageAsync(item_bg, plateKey, false, false);
                if (_typeId != typeId) return; // 期间该格被复用:丢弃
                if (!plateOk)
                    GameLog.Warn("Common", "BaseAwardItem 物品[{0}]{1} 品质底板未加载: key={2}", typeId, basic.Name, plateKey);
            }

            string iconPath = GameResPath.GetGoodsIconPath(basic.Icon);
            bool ok = await ResManager.SetImageAsync(icon, iconPath, false, false);
            if (_typeId != typeId) return; // 期间该格被复用为别的物品:丢弃本次结果
            if (!ok)
            {
                icon.enabled = false;
                GameLog.Warn("Common",
                    "BaseAwardItem 物品[{0}]{1} 图标未导入(blocker): key={2} —— goodsIcon png 在 yu_client/cdn 与 " +
                    "Assets/GameRes/resource/game/goodsIcon/ 均缺(用 神霄/资源 导该图集),先降级隐藏(名称见列表文本)。",
                    typeId, basic.Name, iconPath);
            }
        }

        private void BindClick()
        {
            if (click_group == null) return;
            // 格子只能由 click_group 命中；图标、底板、数量和选中框都是装饰层，不能截走 PointerClick。
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
            // 默认分支(未设回调)= 弹物品详情 tips(对标老端 UIToolTipMgr.DefaultAppendTips → GoodsTooltips/EquipToolTips);数量透传。
            if (_typeId > 0) ItemTipsView.Show(_typeId, _num);
        }
    }
}
