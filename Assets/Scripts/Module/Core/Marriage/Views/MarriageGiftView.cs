using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚恋-真爱礼包页(对标老客户端 marriage/MarriageGiftView.ts,婚恋五页签之一「锦囊」):
    /// 伴侣模型展示位(_group_um 走 ResManager.SetRoleModel,无伴侣时显示占位图 _img_mate)+ 伴侣名(_lb_name)/性别图(_img_sex)+
    /// 左侧购买返还礼包(_btn_get「领取」+ 文案 _lb_get + 红点 _red_get,按 _buy_state 灰化)+
    /// 右侧每日登陆领取(_btn_get_day「领取/已领取」+ _lb_get_day + 红点 _red_get_day + 状态图 _img_state)+
    /// 倒计时(_lb_time)/剩余天数(_lb_day)/每日可领提示(txt2)/未购买提示(_lb_tips)+
    /// 请Ta赠送入口(_lb_give,协议 17240)+ 真爱礼包购买(_btn_buy,Alert 确认 → 协议 17237)+ 每日礼包格子列表(_group_item 克隆 BaseAwardItem)。
    ///
    /// 当前:MarriageController/MarriageModel 已具备 17238/17232 查询/收包地基；Goods/Role 数据消费、伴侣模型、
    /// BaseAwardItem、确认弹窗和红点表现尚未接线。17237/17239/17240 属于未授权写事务，继续保持 blocked。
    /// OnShow 仅恢复权威只读快照请求。
    /// </summary>
    public sealed class MarriageGiftView : MarriageGiftViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 load_callback/LoadSuccess/UpdateView:请求礼包信息(17238/17232) + 渲染伴侣模型 + 刷购买/每日领取态/倒计时/格子列表。
            MarriageController.Instance.RequestGiftInfo();
            MarriageController.Instance.RequestMyMate();
            GameLog.Info("Marriage", "MarriageGiftView 打开 → 已请求17238/17232权威快照,表现接线待运行态验证");
        }

        /// <summary>红点:购买返还 _red_get、每日领取 _red_get_day；状态消费尚未接线，先隐藏。</summary>
        private void HideReds()
        {
            HideNode(_red_get);
            HideNode(_red_get_day);
        }

        private void BindButtons()
        {
            BindBtn(_btn_get, "购买礼包领取 协议17239(c=1)");
            BindBtn(_btn_get_day, "每日登陆领取 协议17239(c=2)");
            BindBtn(_btn_buy, "购买真爱礼包 Alert→协议17237");
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
