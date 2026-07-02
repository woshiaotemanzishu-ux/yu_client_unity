using System;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包熔炼弹窗(对标老客户端 bag/BagSmeltView.ts):熔炼物品换经验,含物品列表、经验条/等级、
    /// 自动选择/一星 过滤勾选、属性按钮、熔炼按钮。
    ///
    /// 降级:熔炼数据(SmeltModel)+ 物品列表(_Scroller1 勾选源)未移植 → 模板隐藏、列表空 + nothingLb 空提示、
    /// propBtn 点击打日志「待对接」;两个过滤勾选(自动/一星)做成本地视觉切换(select/Unselect 互显)。
    /// 薄增量六件套(第20轮工单):15024/15025 协议已由 <see cref="BagFusionController"/> 打通(查询/熔炼发包+回包落库),
    /// 但本 View 尚无「选中物品」数据源(列表未建)→ useBtn 暂不接 Fuse(需要 goods_id/num 列表,不臆造选择逻辑);
    /// 打开时改发 15024 查询当前等级/经验(对标老端 LoadSuccess 首查),真实但只读,不影响现有降级展示。
    /// close → Hide。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class BagSmeltView : BagSmeltViewBind
    {
        private bool _autoOn;
        private bool _oneStarOn;

        protected override void OnInit()
        {
            HideNode(_tpl_DownDropBtn);
            HideNode(_tpl_FightingShowSmallItem);

            BindBtn(closeBtn, () => Hide());
            BindBtn(propBtn, () => GameLog.Info("Bag", "熔炼属性 → 待对接 SmeltPropView/属性数据"));
            BindBtn(useBtn, () => GameLog.Info("Bag", "熔炼 → BagFusionController.Fuse 协议已打通,但物品选中列表未建(_Scroller1 无数据源),待接 UI"));
            BindBtn(autoGp, () => SetAuto(!_autoOn));
            BindBtn(oneStarGp, () => SetOneStar(!_oneStarOn));

            SetAuto(false);
            SetOneStar(false);
        }

        protected override void OnShow(object args)
        {
            if (nothingLb != null) nothingLb.gameObject.SetActive(true);
            BagFusionController.Instance.RequestInfo();   // 15024 查询当前熔炼等级/经验(对标老端 LoadSuccess 首查),真实只读
            GameLog.Info("Bag", "熔炼打开 → 已发 15024 查询;可熔炼物品列表(BagModel/SmeltModel)待对接");
        }

        /// <summary>自动选择 勾选切换(本地视觉:select0/Unselect0 互显)。</summary>
        private void SetAuto(bool on)
        {
            _autoOn = on;
            if (select0 != null) select0.gameObject.SetActive(on);
            if (Unselect0 != null) Unselect0.gameObject.SetActive(!on);
        }

        /// <summary>一星 勾选切换(本地视觉:select1/Unselect1 互显)。</summary>
        private void SetOneStar(bool on)
        {
            _oneStarOn = on;
            if (select1 != null) select1.gameObject.SetActive(on);
            if (Unselect1 != null) Unselect1.gameObject.SetActive(!on);
        }

        private void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }

        private static void HideNode(GameObject go)
        {
            if (go != null) go.SetActive(false);
        }
    }
}
