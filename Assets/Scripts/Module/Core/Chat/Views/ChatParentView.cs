using System.Collections.Generic;
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
        // 频道页签(对标老端 ViewClassCFG:世界/仙宗/队伍/跨服/活动/阵营/海域/系统)。
        private static readonly string[] ChannelLabels = { "世界", "仙宗", "队伍", "跨服", "活动", "阵营", "海域", "系统" };

        private readonly List<ChatParentTab> _tabs = new List<ChatParentTab>();
        private int _curChannel = -1;
        private bool _tabsBuilt;

        protected override void OnInit()
        {
            HideTemplates();
            HideUnbacked();
            BindClose(_close);
            BindClose(_btn_close);
            BindButtons();
            BuildChannelTabs();
        }

        protected override void OnShow(object args)
        {
            // 老端 open → CustomMehod(0, chatViewIndex(channel)) 选频道 + 铺消息列表。ChatModel/协议未移植 → 消息空、仅切频道高亮。
            SelectChannel(_curChannel < 0 ? 0 : _curChannel);
            GameLog.Info("Chat", "聊天窗口打开 → 待对接 ChatModel/ChatController(消息空降级,频道页签可切)");
        }

        /// <summary>对标老端 构建频道页签条:克隆 _tpl_ChatParentTab 到 Content_tab 内容区,填频道名 + 绑点击切换。</summary>
        private void BuildChannelTabs()
        {
            if (_tabsBuilt) return;
            if (_tpl_ChatParentTab == null)
            {
                GameLog.Warn("Chat", "ChatParentView 缺 _tpl_ChatParentTab,频道页签无法构建(重跑 chat 流水线)");
                return;
            }

            // ScrollRect 的内容区作父节点(布局/位置归预制体);取不到则退回模板原父级。
            Transform parent = (Content_tab != null && Content_tab.content != null)
                ? Content_tab.content
                : _tpl_ChatParentTab.transform.parent;

            for (int i = 0; i < ChannelLabels.Length; i++)
            {
                GameObject go = Instantiate(_tpl_ChatParentTab, parent);
                go.SetActive(true);
                ChatParentTab tab = go.GetComponent<ChatParentTab>();
                if (tab == null)
                {
                    GameLog.Warn("Chat", "ChatParentTab 模板缺业务脚本(重跑回填)");
                    Destroy(go);
                    continue;
                }
                tab.SetData(ChannelLabels[i], i, OnChannelClick);
                _tabs.Add(tab);
            }
            _tabsBuilt = true;
        }

        private void OnChannelClick(int index)
        {
            SelectChannel(index);
        }

        /// <summary>切频道:更新页签高亮 + 打日志(消息列表过滤待 ChatModel)。</summary>
        private void SelectChannel(int index)
        {
            if (index < 0 || index >= _tabs.Count) index = 0;
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] != null) _tabs[i].SetSelected(i == index);
            }
            _curChannel = index;
            string name = index >= 0 && index < ChannelLabels.Length ? ChannelLabels[index] : index.ToString();
            GameLog.Info("Chat", "切换频道 [{0}] → 待对接 ChatModel 消息过滤(列表空)", name);
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
            // 已移植子面板 → 真实切换打开(ChatFlow.ToggleSub:再点关闭;表情/背包是 arrow 弹层、喇叭是带 _btn_close 的窗)。
            BindToggle(faceBtn, "ChatToolPanel", "表情");
            BindToggle(_trumpet, "ChatTrumpetView", "喇叭");
            BindToggle(_bag, "ChatBagPanel", "聊天背包");
            // 未移植/纯逻辑 → 暂打日志。
            BindBtn(sendBtn, "发送消息(协议待接)");
            BindBtn(btn_speak, "语音输入");
            BindBtn(voice, "语音切换");
            BindBtn(_dress_up, "装扮");
            BindBtn(_position, "定位/坐标分享");
            BindBtn(_to_bottom, "回到底部");
        }

        /// <summary>按钮 → 切换聊天模块内子面板(ChatFlow.ToggleSub 按 View 子类名查找,叠在主窗上,再点关闭)。</summary>
        private void BindToggle(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Chat", "点击[{0}] → 切换 {1}", label, viewType);
                ChatFlow.ToggleSub(viewType);
            });
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
