using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 花房/送花动态流弹窗(对标老客户端 marriage/MarriageFlowView.ts):从婚恋姻缘页按钮打开的二级弹窗。
    /// 送花动态流列表(_list 克隆 _tpl_MarriageFlowItem,老端 LoopScrowViewMgr 铺 MarriageFlowItem,数据源 MarriageFlowCfg)+
    /// 关闭(_btn_close 关闭返回婚恋姻缘页)。
    ///
    /// 降级:MarriageModel(MarriageFlowCfg 配置)、MarriageFlowItem 列表项、LoopScrowViewMgr 均未移植 →
    /// 列表空、_tpl_MarriageFlowItem 模板隐藏;Bind 无 _group_empty 空态字段 → OnShow 仅打日志降级。
    /// _btn_close → Hide(老端 Close,关闭返回婚恋姻缘页)。
    /// </summary>
    public sealed class MarriageFlowView : MarriageFlowViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_MarriageFlowItem != null) _tpl_MarriageFlowItem.SetActive(false);

            BindClose(_btn_close);
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess → InitEvent + UpdateView(UpdateItems(MarriageFlowCfg) 铺送花动态流)。
            // MarriageModel/MarriageFlowItem/LoopScrowViewMgr 未移植 → 列表空、默认降级。Bind 无 _group_empty → 仅日志。
            GameLog.Info("Marriage", "MarriageFlowView 打开 → 待对接 MarriageModel(列表空/默认降级)");
        }

        /// <summary>关闭按钮 → Hide(关闭返回婚恋姻缘页)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>动作按钮 → 日志降级(待对接 MarriageModel 协议)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Marriage", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
