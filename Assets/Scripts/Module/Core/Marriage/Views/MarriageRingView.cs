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
    /// 当前:MarriageController/MarriageModel 已具备 17210 查询/收包地基；Goods/Role 数据消费、config_ring_stage、
    /// 指环模型、FightingShowSmallItem、BaseAwardItem、星图、进度补间、红点与事件表现尚未接线。
    /// 17211/17213 升级/解锁属于未授权写事务，继续保持 blocked；OnShow 仅恢复权威只读快照请求。
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
            MarriageController.Instance.RequestRingInfo();
            GameLog.Info("Marriage", "MarriageRingView 打开 → 已请求17210权威快照,表现接线待运行态验证");
        }

        /// <summary>红点:升级 _reddot；状态消费尚未接线，先隐藏。</summary>
        private void HideReds()
        {
            HideNode(_reddot);
        }

        private void BindButtons()
        {
            BindBtn(_btn_go, "一键提升/解锁 协议17211/17213");
            // 老端 MarriageRingView.ts 没有给 _btn_stop 绑定点击；该节点只属于提升演出状态，不能伪造交互语义。
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
