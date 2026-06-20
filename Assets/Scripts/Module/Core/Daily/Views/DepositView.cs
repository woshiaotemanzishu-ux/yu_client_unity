using Shenxiao.Generated.UI.Deposit;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 托管中心页(对标老客户端 deposit/DepositView.ts,DailyView 标签4「托管中心」内容):托管项列表(_list_item_con 克隆 _tpl_DepositItem)+
    /// 托管收益(txt_deposit_value)+ 兑换(img_exchange → DepositExchangeView)/记录(img_record → DepositRecordView)+ 提示(img_tip/img_tip2)。
    ///
    /// 降级:DepositModel/托管协议、DepositItem 列表项均未移植 → 模板隐藏、列表空、收益默认;兑换/记录按钮打日志降级(子窗 DepositExchangeView/DepositRecordView 待对接)。
    /// 无独立关闭按钮(每日窗框统一关闭)→ 作 DailyView 标签4 内容由 DailyFlow 跨模块 reparent 进窗框内容区。事件驱动,不进 FirstPass。
    /// </summary>
    public sealed class DepositView : DepositViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_DepositItem != null) _tpl_DepositItem.SetActive(false);
            if (_tpl_DailyBottomView != null) _tpl_DailyBottomView.SetActive(false);
            BindBtn(img_exchange, "托管·兑换");
            BindBtn(img_record, "托管·记录");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 读 DepositModel 铺托管项/收益。数据未移植 → 列表空降级。
            GameLog.Info("Daily", "托管中心页打开 → 待对接 DepositModel(列表空降级)");
        }

        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Daily", "点击[{0}] → 待对接", label));
        }
    }
}
