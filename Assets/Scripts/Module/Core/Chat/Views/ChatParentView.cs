using Shenxiao.Generated.UI.Chat;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Chat
{
    /// <summary>
    /// 全屏聊天窗口(对标老客户端 chat/ChatParentView.ts):频道页签条(tab_Scroller/Content_tab 克隆 ChatParentTab:
    /// 世界/仙宗/队伍/跨服/活动/阵营/海域/系统)+ 消息列表(content_Scroller/Content_chatitem/Content_sysitem)+
    /// 输入框(textDisplay)+ 发送(sendBtn)/表情(faceBtn → ChatToolPanel)/语音(voice/btn_speak)/喇叭(_trumpet → ChatTrumpetView)/
    /// 背包(_bag → ChatBagPanel)/装扮(_dress_up)/定位(_position)/未读(_gp_read/_no_read_cnt/_to_bottom)/锁定(_gp_lock)+ 关闭(_close/_btn_close)。
    ///
    /// 降级:ChatModel/ChatController(频道/消息数据、协议)、各子面板(ChatToolPanel/ChatTrumpetView/ChatBagPanel/ChatMenuView)、
    /// 表情/语音/喇叭系统均未移植 → 页签/消息列表空、未读隐藏、_tpl_* 模板隐藏;输入框可见但发送打日志;按钮点击打日志「待对接」;
    /// _close/_btn_close 关闭可用。事件驱动窗口(主 HUD 点聊天框 → 打开),默认关闭、不进 FirstPass。其余子面板后续 tick 补。
    /// </summary>
    public sealed class ChatParentView : ChatParentViewBind
    {
        protected override void OnInit()
        {
            HideTemplates();
            HideUnbacked();
            BindClose(_close);
            BindClose(_btn_close);
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 open → CustomMehod(0, chatViewIndex(channel)) 选频道 + 铺消息列表。ChatModel/协议未移植 → 频道/消息空降级。
            GameLog.Info("Chat", "聊天窗口打开 → 待对接 ChatModel/ChatController(频道/消息空降级)");
        }

        private void HideTemplates()
        {
            if (_tpl_SystemItem != null) _tpl_SystemItem.SetActive(false);
            if (_tpl_ChatParentTab != null) _tpl_ChatParentTab.SetActive(false);
        }

        /// <summary>未读提示/红点依赖 ChatModel 未读计数,未移植先隐藏。</summary>
        private void HideUnbacked()
        {
            HideNode(_gp_read);
            HideNode(_img_no_read);
        }

        private void BindButtons()
        {
            BindBtn(sendBtn, "发送消息(协议待接)");
            BindBtn(faceBtn, "表情 ChatToolPanel");
            BindBtn(btn_speak, "语音输入");
            BindBtn(voice, "语音切换");
            BindBtn(_trumpet, "喇叭 ChatTrumpetView");
            BindBtn(_bag, "聊天背包 ChatBagPanel");
            BindBtn(_dress_up, "装扮");
            BindBtn(_position, "定位/坐标分享");
            BindBtn(_to_bottom, "回到底部");
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
