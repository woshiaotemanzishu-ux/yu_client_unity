using System.Collections.Generic;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Chat;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面聊天/系统消息条 + 设置/好友/商城入口(对标老客户端 MainUIChatView.ts:LoadSuccess + GetComponents)。
    /// 聊天内容直接消费 ChatModel 的真实协议数据:
    /// - 关掉两个滚动面板的滚动条(对标 GetComponents 末尾的 _panel_chat/_panel_sys.vScrollBar.visible = false)。
    /// - 上半区合并世界/仙宗/跨服等普通频道,下半区显示系统频道；两区都实时响应 11001/11010。
    /// - 好友红点 _img_friend_red、商城红点 _img_shop_red、限购商城特效盒 _box_shop_effect 在拿到数据/特效前隐藏
    ///   (老客户端由 MainUIModel.friend_red / ShopModel 红点 / ActivityIcon 特效驱动可见性,这些模型尚未移植)。
    /// </summary>
    public sealed class MainUIChatView : MainUIChatViewBind
    {
        private ActivityIcon _strengthenIcon;
        private readonly List<GameObject> _renderedChatItems = new List<GameObject>();
        private readonly List<GameObject> _renderedSystemItems = new List<GameObject>();

        protected override void OnInit()
        {
            HideScrollBars();
            HideUnbackedIndicators();
            HideTemplates();
            CreateStrengthenIcon();
            WireHudEntries();
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On<bool>(GlobalEvent.EVT_SHOP_RED_DOT, OnShopRedDot);
            EventDispatcher.On<int>(GlobalEvent.EVT_CHAT_MESSAGES_UPDATED, OnChatMessagesUpdated);
            ChatModel.Instance.EnsureWelcomeSystemMessage();
            RenderAllMessages();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off<bool>(GlobalEvent.EVT_SHOP_RED_DOT, OnShopRedDot);
            EventDispatcher.Off<int>(GlobalEvent.EVT_CHAT_MESSAGES_UPDATED, OnChatMessagesUpdated);
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off<bool>(GlobalEvent.EVT_SHOP_RED_DOT, OnShopRedDot);
            EventDispatcher.Off<int>(GlobalEvent.EVT_CHAT_MESSAGES_UPDATED, OnChatMessagesUpdated);
            ClearRendered(_renderedChatItems);
            ClearRendered(_renderedSystemItems);
        }

        private void OnShopRedDot(bool show)
        {
            if (_img_shop_red != null)
            {
                _img_shop_red.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// 接 HUD 聊天条上的入口按钮,经 MainUIRouter 解耦打开对应面板(各模块 Bootstrap 注册 key,MainUI 不直接依赖它们):
        /// - 聊天底图 _img_bg → "chat"(对标老端点 _panel_chat/_panel_sys 区域 Fire OPEN_CHAT_VIEW);点击热区精度归预制体。
        /// - 设置按钮 _img_setting → "setting"(对标老端 SettingBtn → SettingView)。
        /// - 好友按钮 _img_friend → "friend"(对标老端 FriendBtn → FriendView)。
        /// - 商城按钮 _img_shop → "shop"(对标老端 ShopBtn → ShopCommonView)。
        /// </summary>
        private void WireHudEntries()
        {
            RouteClick(_img_bg, "chat");
            RouteClick(_panel_chat, "chat");
            RouteClick(_panel_sys, "chat");
            RouteClick(_box_chat_con, "chat");
            RouteClick(_box_sys_con, "chat");
            // 固定入口必须把 Button 挂在玩家实际看到并命中的 Graphic 上。只给外层 box 补透明
            // Image/Button 时，WebGL 的可见子图可能先吃掉射线，导致齿轮可见却没有点击回调。
            RouteClick(_img_setting, "setting");
            RouteClick(_img_friend, "friend");
            RouteClick(_img_shop, "shop");
        }

        private static void RouteClick(Component target, string viewKey)
        {
            if (target == null) return;
            UIUtil.AddClick(target, () => MainUIRouter.Open(viewKey));
        }

        /// <summary>
        /// 对标 GetComponents: this._panel_chat.vScrollBar.visible = false; this._panel_sys.vScrollBar.visible = false。
        /// Unity 的 ScrollRect 滚动条是可选挂载,存在才隐藏(水平/垂直都关,匹配老客户端不显滚动条)。
        /// </summary>
        private void HideScrollBars()
        {
            HideScrollBars(_panel_chat);
            HideScrollBars(_panel_sys);
        }

        private static void HideScrollBars(ScrollRect scroll)
        {
            if (scroll == null)
            {
                return;
            }
            if (scroll.verticalScrollbar != null)
            {
                scroll.verticalScrollbar.gameObject.SetActive(false);
            }
            if (scroll.horizontalScrollbar != null)
            {
                scroll.horizontalScrollbar.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 红点/特效未移植:好友红点、商城红点、限购商城特效盒先隐藏(老客户端由 MainUIModel.friend_red、
        /// ShopModel.UpdateShopRedState、ActivityIcon 特效驱动)。不造假数据,沿用 gameObject.SetActive(false)。
        /// </summary>
        private void HideUnbackedIndicators()
        {
            _img_friend_red.gameObject.SetActive(false);
            _img_shop_red.gameObject.SetActive(false);
            _box_shop_effect.gameObject.SetActive(false);
        }

        private void HideTemplates()
        {
            if (_tpl_MainUIChatItem != null) _tpl_MainUIChatItem.SetActive(false);
            if (_tpl_ActivityIcon != null) _tpl_ActivityIcon.SetActive(false);
        }

        private void OnChatMessagesUpdated(int channel)
        {
            if (!IsShown) return;
            if (channel == ChatModel.ChannelSystem) RenderSystemMessages();
            else if (channel != ChatModel.ChannelPrivate) RenderChatMessages();
        }

        private void RenderAllMessages()
        {
            RenderChatMessages();
            RenderSystemMessages();
        }

        private void RenderChatMessages()
        {
            RenderMessages(ChatModel.Instance.GetMainHudMessages(), _box_chat_con, _panel_chat, _renderedChatItems, "chat");
        }

        private void RenderSystemMessages()
        {
            RenderMessages(ChatModel.Instance.GetSystemHudMessages(), _box_sys_con, _panel_sys, _renderedSystemItems, "system");
        }

        private void RenderMessages(IReadOnlyList<ChatMessage> messages, RectTransform parent, ScrollRect scroll,
            List<GameObject> rendered, string kind)
        {
            ClearRendered(rendered);
            HideTemplates();

            if (_tpl_MainUIChatItem == null || parent == null)
            {
                GameLog.Error("MainUI", "MainUIChatView missing MainUIChatItem template or " + kind + " content");
                return;
            }

            for (int i = 0; i < messages.Count; i++)
            {
                ChatMessage message = messages[i];
                if (message == null) continue;

                GameObject go = Instantiate(_tpl_MainUIChatItem, parent);
                go.name = "MainUIChatItem_runtime_" + kind + "_" + i;
                go.SetActive(true);

                MainUIChatItem item = go.GetComponent<MainUIChatItem>();
                if (item == null)
                {
                    GameLog.Error("MainUI", "MainUIChatItem template missing business component");
                    DestroyUiObject(go);
                    continue;
                }

                item.SetData(message);
                rendered.Add(go);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            Canvas.ForceUpdateCanvases();
            if (scroll != null) scroll.verticalNormalizedPosition = 0f;
        }

        private static void ClearRendered(List<GameObject> rendered)
        {
            for (int i = rendered.Count - 1; i >= 0; i--)
                DestroyUiObject(rendered[i]);
            rendered.Clear();
        }

        private static void DestroyUiObject(GameObject target)
        {
            if (target == null) return;
            if (Application.isPlaying)
            {
                // Destroy 延迟到帧末执行；先退出布局，避免实时消息刷新时同一帧短暂出现新旧两批条目。
                target.SetActive(false);
                Destroy(target);
            }
            else DestroyImmediate(target);
        }

        /// <summary>
        /// 对标 yu_client MainUIChatView.ts:CreateStrengthenIcon。
        /// 强化入口是 ActivityIcon("158") 挂在 _box_strengthen,不是 ActivityView 的列表图标。
        /// </summary>
        private void CreateStrengthenIcon()
        {
            if (_strengthenIcon != null)
            {
                return;
            }

            if (_tpl_ActivityIcon == null || _box_strengthen == null)
            {
                GameLog.Error("MainUI", "MainUIChatView missing ActivityIcon template or _box_strengthen");
                return;
            }

            GameObject go = Instantiate(_tpl_ActivityIcon, _box_strengthen);
            go.SetActive(true);

            ActivityIcon icon = go.GetComponent<ActivityIcon>();
            if (icon == null)
            {
                GameLog.Error("MainUI", "MainUIChatView ActivityIcon template is not rebound to business script");
                Destroy(go);
                return;
            }

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
            rt.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
            rt.anchoredPosition = UnityEngine.Vector2.zero;

            icon.Show();
            icon.SetIconType("158");
            _strengthenIcon = icon;
        }
    }
}
