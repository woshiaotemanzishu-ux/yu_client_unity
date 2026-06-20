using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚恋-指环页(对标老客户端 marriage/MarriageRingView.ts):婚恋五页签之一(大厅/姻缘/指环/锦囊/副本)。
    /// 顶部指环模型/图标(ring_icon 上下浮动动画 + _group_um 模型位)+ 名称(_lb_name)+ 战力(fight_con 克隆 FightingShowSmallItem)+
    /// 夫妻在/失状态(_img_active / _img_lose 据 RoleVo.is_marriage)+ 星级(_group_heart 克隆星图 uijh_112a/b)+
    /// 主属性(_lb_sub_attr)/词条属性(_group_attr 列表)+ 培养进度条(_img_progress/_group_eff_pro/_lb_progress + 补间动画)+
    /// 消耗道具格(_group_item 克隆 BaseAwardItem)+ 一键提升/解锁按钮(_btn_go,协议 17211/17213)+ 停止(_btn_stop)+ 红点(_reddot)。
    ///
    /// 降级:MarriageModel(GetRingInfo/GetRingStarData/GetNextRingStarData/GetRingStageData/CheckRedStatus/GetConstantData 等)、
    /// GoodsModel/RoleManager、config_ring_stage 配置、升级/解锁协议(17210/17211/17213)、ResManager 指环模型、
    /// FightingShowSmallItem 战力 / BaseAwardItem 消耗格 / 星图 / 进度补间 / MarriageEvent 均未移植 →
    /// 红点先隐藏;按钮点击打日志「待对接」;OnShow 列表空/默认降级。Null-guard 每处访问。
    /// </summary>
    public sealed class MarriageRingView : MarriageRingViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 load_callback/LoadSuccess → UpdateView:请求 17210 + 渲染指环模型/名称/战力/星级/属性/进度/消耗格/夫妻态。
            // MarriageModel/协议/配置/模型/列表项均未移植 → 列表空、模型/默认降级。
            GameLog.Info("Marriage", "MarriageRingView 打开 → 待对接 MarriageModel(列表空/默认降级)");
        }

        /// <summary>红点:升级红点 _reddot(CheckRedStatus(MARRIAGE_RING,1)),数据未移植先隐藏。</summary>
        private void HideReds()
        {
            HideNode(_reddot);
        }

        private void BindButtons()
        {
            BindBtn(_btn_go, "一键提升/解锁 协议17211/17213");
            BindBtn(_btn_stop, "停止提升");
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/子窗待对接)。</summary>
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
