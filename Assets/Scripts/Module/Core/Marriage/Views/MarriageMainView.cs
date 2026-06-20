using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚恋-大厅主界面(对标老客户端 marriage/MarriageMainView.ts),5 个婚恋页签(大厅/姻缘/指环/锦囊/副本)之一:
    /// 背景(bg_img)+ 本人/伴侣 3D 模型位(_group_um_self/_group_um_mate 走 ResManager.SetRoleModel)+
    /// 伴侣信息框(_box_info 点击开 MarriageRoleMenuView)+ 无伴侣占位贴图(_img_mate/_group_shadow)+
    /// 本人/伴侣性别(_img_sex_self/_img_sex_mate)与名字(_lb_name_self/_lb_name_mate)+
    /// 礼物剩余天数(_lb_time_self/_lb_time_mate)+ 亲密度(_lb_intimacy)与结缘天数(_group_day/_lb_day)+
    /// 红点(banqRed:婚宴申请红点 RedDotManager.CheckRedStatus(MARRIAGE_ASK))+
    /// 按钮:寻缘(_btn_find→MarriageAskListView)、求婚(_btn_ask→MarriageAskView)、再续(_btn_again→MarriageAskView)、
    /// 解除(_btn_break→MarriageBreakView)、送花(_btn_flower→MarriageFlowerView)、花房(_btn_flow→MarriageFlowView)、
    /// 婚宴(_btn_banquet→BanquetApplyView)、设计图(_btn_dsgt→MarriageDsgtView)。
    ///
    /// 降级:MarriageModel(GetMateInfo/GetGiftInfo/BindVar)、BanquetModel、FriendModel/RoleManager、
    /// RedDotManager、ResManager 角色模型、各 MarriageEvent/协议(17232/17238)与子窗均未移植 →
    /// 红点先隐藏;按钮点击打日志「待对接」;OnShow 走列表空/默认降级。Null-guard 每处访问。
    /// </summary>
    public sealed class MarriageMainView : MarriageMainViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 load_callback/LoadSuccess → InitEvent + 请求协议(17232/17238)+ SetSelfData/UpdateView:
            // 铺本人/伴侣模型、刷名字/性别/亲密度/天数、按是否已婚切换控件显隐。
            // MarriageModel/BanquetModel/协议/角色模型均未移植 → 列表空、模型/默认降级。
            GameLog.Info("Marriage", "MarriageMainView 打开 → 待对接 MarriageModel(列表空/默认降级)");
        }

        /// <summary>红点:婚宴申请红点 banqRed(RedDotManager.CheckRedStatus(MARRIAGE_ASK)),数据未移植先隐藏。</summary>
        private void HideReds()
        {
            HideNode(banqRed);
        }

        private void BindButtons()
        {
            // 已移植子窗 → 真实打开(经 MarriageFlow.OpenSub 叠加在主面板上,自带 _btn_close 返回)。
            BindOpen(_btn_ask, "MarriageAskView", "求婚");
            BindOpen(_btn_again, "MarriageAskView", "再续前缘");
            BindOpen(_btn_break, "MarriageBreakView", "解除婚约");
            BindOpen(_btn_flower, "MarriageFlowerView", "送花");
            BindOpen(_btn_find, "MarriageAskListView", "寻缘");
            BindOpen(_btn_flow, "MarriageFlowView", "花房");
            BindOpen(_btn_dsgt, "MarriageDsgtView", "婚戒设计图");
            // 婚宴属 marriage2 独立模块,未移植 → 暂打日志。
            BindBtn(_btn_banquet, "婚宴 OPEN_VIEW(BanquetApplyView)");
        }

        /// <summary>按钮 → 打开婚恋模块内二级弹窗(MarriageFlow.OpenSub 按 View 子类名查找并置顶)。</summary>
        private void BindOpen(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Marriage", "点击[{0}] → 打开 {1}", label, viewType);
                MarriageFlow.OpenSub(viewType);
            });
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
