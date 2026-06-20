using Shenxiao.Generated.UI.Chat;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Chat
{
    /// <summary>
    /// 喇叭发送窗(对标老客户端 chat/ChatTrumpetView.ts):喇叭类型菜单(t_gp_menu/_lb_type/_lb_num)+ 输入框
    /// (_gp_text_input/_lb_tips/_lb_font_num)+ 表情(faceBtn → ChatToolPanel)/背包(_bag → ChatBagPanel,_img_bag_select 选中态)/
    /// 发送(_btn_send → CHAT_SEND_MSG 喇叭道具消耗校验)+ 关闭(_btn_close)。
    ///
    /// 降级:ChatModel(喇叭类型/道具消耗 config_horn_kv/config_goods_price、协议)、GoodsModel(道具数量)、子面板
    /// (ChatToolPanel/ChatBagPanel/ChatTrumpetMenu)均未移植 → _img_bag_select 选中态隐藏;faceBtn/_btn_send/_bag/t_gp_menu
    /// 按钮点击打日志「待对接」;输入框保留可见;_btn_close 关闭可用。叠在主聊天窗上的弹窗,事件驱动打开。
    /// </summary>
    public sealed class ChatTrumpetView : ChatTrumpetViewBind
    {
        protected override void OnInit()
        {
            // _img_bag_select 是背包道具选中态,依赖 ChatModel CHAT_BAG_OPEN 事件,未移植先隐藏。
            HideNode(_img_bag_select);
            BindClose(_btn_close);
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Chat", "ChatTrumpetView 打开 → 待对接 ChatModel(列表空/默认降级)");
        }

        private void BindButtons()
        {
            BindBtn(faceBtn, "表情");
            BindBtn(_btn_send, "发送喇叭(协议待接)");
            BindBtn(_bag, "聊天背包");
            BindBtn(t_gp_menu, "喇叭类型菜单");
        }

        /// <summary>关闭按钮(Image 或含 Image 容器)→ Hide(关闭本窗)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/子面板待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Chat", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
