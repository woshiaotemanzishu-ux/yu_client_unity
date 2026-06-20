using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚恋-求婚界面(对标老客户端 marriage/MarriageAskView.ts):
    /// 二级弹窗,从婚恋主面板「姻缘」页按钮打开。自己头像(_head_self_con 克隆 CustomHeadItem + 名字 _lb_name_self)+
    /// 对方头像(_head_other_con)+ 好友下拉(_drop_btn_con 克隆 DownDropBtn,选求婚对象)+
    /// 戒指列表(_Scroller1/_list 循环 MarriageAskItem,选戒指)+ 提示(_lb_tips/_Label1)+
    /// 示爱(_btn_ask → 校验对象/戒指/货币后发 17231 求婚协议)+ 说明(_btn_help → OPEN_INSTRUCTION_VIEW 172)+
    /// 关闭(_btn_close → 关闭返回主面板)。
    ///
    /// 降级:MarriageModel(GetCanMarriageFriendList/GetAskDataList/_mate_info)、FriendModel(好友数据)、
    /// GoodsModel/DressModel/RoleManager、config_propose_cfg 配置、求婚/示爱协议(17231/17232)、
    /// CustomHeadItem 头像、DownDropBtn 下拉、MarriageAskItem/MarriageDropBtn 列表项与各 MarriageEvent 均未移植 →
    /// 无红点字段(本界面无红点);_tpl_* 模板隐藏;按钮点击打日志「待对接」;OnShow 列表空/默认降级。
    /// _btn_close 关闭返回主面板(Hide)。Null-guard 每处访问。
    /// </summary>
    public sealed class MarriageAskView : MarriageAskViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 Local_Open/open_callback → LoadSuccess/UpdateFriendBtnData/UpdateView:
            // 渲染双方头像 + 好友下拉 + 戒指列表 + 名字/提示。MarriageModel/FriendModel/协议/配置/头像均未移植 → 列表空、默认降级。
            GameLog.Info("Marriage", "MarriageAskView 打开 → 待对接 MarriageModel(列表空/默认降级)");
        }

        /// <summary>红点:本界面 Bind 无红点字段,留空(占位以对齐其他视图结构)。</summary>
        private void HideReds()
        {
            // MarriageAskViewBind 无红点节点,无需隐藏。
        }

        /// <summary>列表项模板(求婚戒指格/头像项/下拉按钮),由各 Item View 克隆,数据未移植先隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_MarriageAskItem != null) _tpl_MarriageAskItem.SetActive(false);
            if (_tpl_CustomHeadItem != null) _tpl_CustomHeadItem.SetActive(false);
            if (_tpl_DownDropBtn != null) _tpl_DownDropBtn.SetActive(false);
            if (_tpl_MarriageDropBtn != null) _tpl_MarriageDropBtn.SetActive(false);
        }

        private void BindButtons()
        {
            // _btn_close:关闭本二级弹窗,返回婚恋主面板。
            BindClose(_btn_close);
            BindBtn(_btn_help, "说明 OPEN_INSTRUCTION_VIEW(172)");
            BindBtn(_btn_ask, "示爱/求婚 协议17231");
        }

        /// <summary>关闭按钮 → Hide(BaseView 继承方法,关闭本子窗回到主面板)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
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
