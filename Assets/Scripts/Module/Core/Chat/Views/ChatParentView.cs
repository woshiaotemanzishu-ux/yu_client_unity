using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Chat;
using Shenxiao.Module.Core.Role;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Chat
{
    /// <summary>
    /// Runtime chat window. The converted prefab only supplies templates; the final tab/message
    /// shape must be rebuilt from runtime role state and chat protocol data, matching old Laya.
    /// </summary>
    public sealed class ChatParentView : ChatParentViewBind
    {
        private const float BottomCloseAreaHeight = 120f;
        private const float ChannelTabWidth = 96f;
        private const float ChannelTabHeight = 45f;
        private const float ChatItemHeight = 150f;
        private const float SystemItemHeight = 78f;

        private static readonly int[] ChannelOrder =
        {
            ChatModel.ChannelWorld,
            ChatModel.ChannelGuild,
            ChatModel.ChannelTeam,
            ChatModel.ChannelSmallKuafu,
            ChatModel.ChannelWorldKuafu,
            ChatModel.ChannelCamp,
            ChatModel.ChannelSea,
            ChatModel.ChannelSystem
        };

        private readonly List<ChatParentTab> _tabs = new List<ChatParentTab>();
        private readonly List<int> _visibleChannels = new List<int>();
        private readonly List<GameObject> _renderedChatItems = new List<GameObject>();
        private readonly List<GameObject> _renderedSystemItems = new List<GameObject>();

        private int _curTabIndex = -1;
        private GameObject _chatItemTemplate;

        protected override void OnInit()
        {
            HideTemplates();
            HideUnbacked();
            BindClose(_close);
            BindClose(_btn_close);
            BindButtons();
            EventDispatcher.On<int>(GlobalEvent.EVT_CHAT_MESSAGES_UPDATED, OnChatMessagesUpdated);
        }

        protected override void OnShow(object args)
        {
            ChatModel.Instance.EnsureWelcomeSystemMessage();
            RebuildChannelTabs();
            SelectChannel(_curTabIndex < 0 ? 0 : _curTabIndex);
            ApplyRuntimePosition();
            GameLog.Info("Chat", "ChatParentView open channels={0}", _visibleChannels.Count);
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off<int>(GlobalEvent.EVT_CHAT_MESSAGES_UPDATED, OnChatMessagesUpdated);
            ClearRendered(_renderedChatItems);
            ClearRendered(_renderedSystemItems);
        }

        private void ApplyRuntimePosition()
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null) return;

            RectTransform parent = rt.parent as RectTransform;
            float stageHeight = parent != null && parent.rect.height > 0f ? parent.rect.height : Screen.height;
            float topY = Mathf.Max(0f, stageHeight - rt.rect.height - BottomCloseAreaHeight);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -topY);
        }

        private void RebuildChannelTabs()
        {
            if (_tpl_ChatParentTab == null)
            {
                GameLog.Warn("Chat", "ChatParentView missing _tpl_ChatParentTab");
                return;
            }

            Transform parent = Content_tab != null && Content_tab.content != null
                ? Content_tab.content
                : _tpl_ChatParentTab.transform.parent;

            ClearConvertedTabs(parent);
            BuildVisibleChannels();

            for (int i = 0; i < _visibleChannels.Count; i++)
            {
                int channel = _visibleChannels[i];
                GameObject go = Instantiate(_tpl_ChatParentTab, parent);
                go.name = _tpl_ChatParentTab.name + "_runtime_" + channel;
                go.SetActive(true);

                ChatParentTab tab = go.GetComponent<ChatParentTab>();
                if (tab == null)
                {
                    GameLog.Warn("Chat", "ChatParentTab template missing business script");
                    DestroyUiObject(go);
                    continue;
                }

                RectTransform rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.sizeDelta = new Vector2(ChannelTabWidth, ChannelTabHeight);
                    rt.anchoredPosition = new Vector2(0f, -ChannelTabHeight * i);
                    rt.localScale = Vector3.one;
                }

                tab.SetData(ChatModel.ChannelLabel(channel), i, OnChannelClick);
                _tabs.Add(tab);
            }

            RectTransform content = parent as RectTransform;
            if (content != null)
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 1f);
                content.sizeDelta = new Vector2(ChannelTabWidth, ChannelTabHeight * _visibleChannels.Count);
                content.anchoredPosition = Vector2.zero;
            }

            if (_curTabIndex >= _visibleChannels.Count) _curTabIndex = 0;
        }

        private void BuildVisibleChannels()
        {
            _visibleChannels.Clear();
            for (int i = 0; i < ChannelOrder.Length; i++)
            {
                int channel = ChannelOrder[i];
                if (IsChannelVisible(channel)) _visibleChannels.Add(channel);
            }

            if (_visibleChannels.Count == 0)
            {
                _visibleChannels.Add(ChatModel.ChannelWorld);
                _visibleChannels.Add(ChatModel.ChannelSystem);
            }
        }

        private static bool IsChannelVisible(int channel)
        {
            RoleModel role = RoleModel.Instance;
            switch (channel)
            {
                case ChatModel.ChannelWorld:
                case ChatModel.ChannelSystem:
                    return true;
                case ChatModel.ChannelGuild:
                    return role.GuildId > 0;
                case ChatModel.ChannelSmallKuafu:
                    return role.Level >= 200;
                case ChatModel.ChannelWorldKuafu:
                    return role.Level >= 100;
                case ChatModel.ChannelTeam:
                case ChatModel.ChannelCamp:
                case ChatModel.ChannelSea:
                    return false;
                default:
                    return false;
            }
        }

        private void ClearConvertedTabs(Transform parent)
        {
            _tabs.Clear();
            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null || child.gameObject == _tpl_ChatParentTab) continue;
                if (child.GetComponent<ChatParentTab>() == null) continue;

                DestroyUiObject(child.gameObject);
            }
        }

        private void OnChannelClick(int index)
        {
            SelectChannel(index);
        }

        private void SelectChannel(int index)
        {
            if (_visibleChannels.Count == 0) RebuildChannelTabs();
            if (_visibleChannels.Count == 0) return;

            if (index < 0 || index >= _visibleChannels.Count) index = 0;
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] != null) _tabs[i].SetSelected(i == index);
            }

            _curTabIndex = index;
            int channel = _visibleChannels[index];
            bool isSystem = channel == ChatModel.ChannelSystem;

            if (Content_chatitem != null) Content_chatitem.gameObject.SetActive(!isSystem);
            if (Content_sysitem != null) Content_sysitem.gameObject.SetActive(isSystem);
            if (content_Scroller != null)
            {
                content_Scroller.content = isSystem ? Content_sysitem : Content_chatitem;
            }
            if (chatGroup != null) chatGroup.gameObject.SetActive(!isSystem);
            if (tips != null) tips.gameObject.SetActive(isSystem);

            RenderMessages(channel);
            GameLog.Info("Chat", "select channel {0}({1})", ChatModel.ChannelLabel(channel), channel);
        }

        private void OnChatMessagesUpdated(int channel)
        {
            if (!IsShown) return;
            if (_visibleChannels.Count == 0 || _curTabIndex < 0 || _curTabIndex >= _visibleChannels.Count) return;
            if (_visibleChannels[_curTabIndex] != channel) return;
            RenderMessages(channel);
        }

        private void RenderMessages(int channel)
        {
            HideTemplates();
            IReadOnlyList<ChatMessage> messages = ChatModel.Instance.GetMessages(channel);
            if (channel == ChatModel.ChannelSystem)
            {
                RenderSystemMessages(messages);
            }
            else
            {
                RenderChatMessages(messages);
            }

            if (content_Scroller != null) content_Scroller.verticalNormalizedPosition = 0f;
        }

        private void RenderSystemMessages(IReadOnlyList<ChatMessage> messages)
        {
            ClearRendered(_renderedChatItems);
            ClearRendered(_renderedSystemItems);

            if (_tpl_SystemItem == null || Content_sysitem == null)
            {
                GameLog.Warn("Chat", "system message template missing");
                return;
            }

            for (int i = 0; i < messages.Count; i++)
            {
                ChatMessage message = messages[i];
                if (message == null) continue;

                GameObject go = Instantiate(_tpl_SystemItem, Content_sysitem);
                go.name = _tpl_SystemItem.name + "_runtime_" + i;
                go.SetActive(true);
                PlaceItem(go, Content_sysitem, i, SystemItemHeight);

                SystemItemBind item = go.GetComponent<SystemItemBind>();
                if (item != null)
                {
                    if (item.sysCon != null) item.sysCon.gameObject.SetActive(true);
                    if (item.SpriteGraphic != null) item.SpriteGraphic.gameObject.SetActive(true);
                    if (item._Group1 != null) item._Group1.gameObject.SetActive(true);
                    if (item.txt_sys_channel != null) item.txt_sys_channel.gameObject.SetActive(true);
                    if (item.txt_sys_content != null) item.txt_sys_content.gameObject.SetActive(true);
                    SetText(item.txt_sys_channel, "[" + ChatModel.ChannelLabel(ChatModel.ChannelSystem) + "]");
                    SetText(item.txt_sys_content, GetMessageText(message));
                    if (item.txt_sys_content != null) item.txt_sys_content.richText = true;
                }

                _renderedSystemItems.Add(go);
            }

            ApplyContentSize(Content_sysitem, messages.Count, SystemItemHeight);
        }

        private void RenderChatMessages(IReadOnlyList<ChatMessage> messages)
        {
            ClearRendered(_renderedChatItems);
            ClearRendered(_renderedSystemItems);

            GameObject template = FindChatItemTemplate();
            if (template == null || Content_chatitem == null)
            {
                GameLog.Warn("Chat", "ChatItem template missing; converter should expose chat/ChatItem to ChatParentView");
                return;
            }

            for (int i = 0; i < messages.Count; i++)
            {
                ChatMessage message = messages[i];
                if (message == null) continue;

                GameObject go = Instantiate(template, Content_chatitem);
                go.name = template.name + "_runtime_" + i;
                go.SetActive(true);
                PlaceItem(go, Content_chatitem, i, ChatItemHeight);

                ChatItemBind item = go.GetComponent<ChatItemBind>();
                if (item != null) ApplyChatItem(item, message);

                _renderedChatItems.Add(go);
            }

            ApplyContentSize(Content_chatitem, messages.Count, ChatItemHeight);
        }

        private void ApplyChatItem(ChatItemBind item, ChatMessage message)
        {
            bool isSelf = message.PlayerId != 0 && message.PlayerId == RoleModel.Instance.RoleId;
            if (item.mainRoleCon != null) item.mainRoleCon.gameObject.SetActive(isSelf);
            if (item.playerRoleCon != null) item.playerRoleCon.gameObject.SetActive(!isSelf);

            string playerName = GetPlayerName(message, isSelf);
            string level = GetLevelText(message, isSelf);
            string content = GetMessageText(message);

            SetText(item.txt_name, playerName);
            SetText(item.txt_name1, playerName);
            SetText(item.txt_lv1, level);
            SetText(item.txt_lv, level);
            SetText(item.txt_content1, content);
            SetText(item.txt_content111, content);
            SetText(item.txt_content11, string.Empty);
            SetText(item.txt_content, string.Empty);
            SetText(item.txt, string.Empty);
            SetText(item.txt1, string.Empty);
            SetText(item.test, string.Empty);
            SetText(item.test2, string.Empty);

            HideRect(item.btn_qipao_voice);
            HideRect(item.btn_qipao_voice1);
            HideRect(item.img_voice_txt);
            HideRect(item.img_voice_txt1);
            HideImage(item.img_voice_img);
            HideImage(item.img_voice_img1);
        }

        private GameObject FindChatItemTemplate()
        {
            if (_chatItemTemplate != null) return _chatItemTemplate;

            Transform moduleRoot = transform.parent;
            Transform direct = moduleRoot != null ? moduleRoot.Find("ChatItem") : null;
            if (direct != null && direct.GetComponent<ChatItemBind>() != null)
            {
                _chatItemTemplate = direct.gameObject;
                _chatItemTemplate.SetActive(false);
                return _chatItemTemplate;
            }

            ChatItemBind bind = moduleRoot != null ? moduleRoot.GetComponentInChildren<ChatItemBind>(true) : null;
            if (bind != null && bind.gameObject != gameObject)
            {
                _chatItemTemplate = bind.gameObject;
                _chatItemTemplate.SetActive(false);
            }

            return _chatItemTemplate;
        }

        private void HideTemplates()
        {
            if (_tpl_SystemItem != null) _tpl_SystemItem.SetActive(false);
            if (_tpl_ChatParentTab != null) _tpl_ChatParentTab.SetActive(false);
            GameObject chatTemplate = FindChatItemTemplate();
            if (chatTemplate != null) chatTemplate.SetActive(false);
        }

        private void HideUnbacked()
        {
            HideNode(_gp_read);
            HideNode(_img_no_read);
        }

        private void BindButtons()
        {
            BindToggle(faceBtn, "ChatToolPanel", "face");
            BindToggle(_trumpet, "ChatTrumpetView", "trumpet");
            BindToggle(_bag, "ChatBagPanel", "chat bag");
            BindSend(sendBtn);
            BindBtn(btn_speak, "voice input");
            BindBtn(voice, "voice switch");
            BindBtn(_dress_up, "dress up");
            BindBtn(_position, "position share");
            BindBtn(_to_bottom, "scroll bottom");
        }

        private void BindToggle(Component target, string viewType, string label)
        {
            Image img = FindClickableImage(target);
            if (img == null) return;

            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Chat", "click {0} toggle {1}", label, viewType);
                ChatFlow.ToggleSub(viewType);
            });
        }

        private void BindClose(Component target)
        {
            Image img = FindClickableImage(target);
            if (img == null) return;

            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        private void BindSend(Component target)
        {
            Image img = FindClickableImage(target);
            if (img == null) return;

            img.raycastTarget = true;
            UIUtil.ClearClicks(img);
            UIUtil.AddClick(img, SendCurrentMessage);
        }

        private void BindBtn(Component target, string label)
        {
            Image img = FindClickableImage(target);
            if (img == null) return;

            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Chat", "click {0}; feature pending", label));
        }

        private void SendCurrentMessage()
        {
            int channel = CurrentChannel();
            if (channel == ChatModel.ChannelSystem)
            {
                GameLog.Warn("Chat", "system channel is read-only");
                return;
            }

            string content = textDisplay != null ? textDisplay.text : string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return;

            // 预校验(空文本/等级/CD)集中在 ChatController.SendChat,View 层不再手写协议格式串。
            ChatController.Instance.SendChat(channel, content);
            if (textDisplay != null) textDisplay.text = string.Empty;
        }

        private int CurrentChannel()
        {
            if (_visibleChannels.Count == 0) return ChatModel.ChannelWorld;
            if (_curTabIndex < 0 || _curTabIndex >= _visibleChannels.Count) return _visibleChannels[0];
            return _visibleChannels[_curTabIndex];
        }

        private static Image FindClickableImage(Component target)
        {
            if (target == null) return null;
            Image img = target as Image;
            return img != null ? img : target.GetComponentInChildren<Image>(true);
        }

        private static void PlaceItem(GameObject go, RectTransform parent, int index, float height)
        {
            RectTransform rt = go.transform as RectTransform;
            if (rt == null) return;

            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = new Vector2(0f, -height * index);

            float width = parent != null && parent.rect.width > 1f ? parent.rect.width : rt.rect.width;
            if (width > 1f) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (rt.rect.height < 1f) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private float GetViewportHeight()
        {
            if (content_Scroller != null && content_Scroller.viewport != null && content_Scroller.viewport.rect.height > 1f)
                return content_Scroller.viewport.rect.height;
            return 1f;
        }

        private void ApplyContentSize(RectTransform content, int count, float itemHeight)
        {
            if (content == null) return;
            float height = Mathf.Max(GetViewportHeight(), count * itemHeight);
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private static string GetPlayerName(ChatMessage message, bool isSelf)
        {
            if (isSelf && !string.IsNullOrEmpty(RoleModel.Instance.Name)) return RoleModel.Instance.Name;
            FigureProto figure = message.Figure;
            if (figure != null && !string.IsNullOrEmpty(figure.name)) return figure.name;
            if (!string.IsNullOrEmpty(message.ServerName)) return message.ServerName;
            return message.PlayerId > 0 ? "Player" + message.PlayerId : string.Empty;
        }

        private static string GetLevelText(ChatMessage message, bool isSelf)
        {
            int level = 0;
            if (message.Figure != null) level = message.Figure.level;
            if (level <= 0 && isSelf) level = RoleModel.Instance.Level;
            return level > 0 ? "Lv." + level : string.Empty;
        }

        private static string GetMessageText(ChatMessage message)
        {
            if (message == null) return string.Empty;
            if (!string.IsNullOrEmpty(message.Message)) return message.Message;
            return message.Args ?? string.Empty;
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target == null) return;
            target.richText = true;
            target.text = value ?? string.Empty;
        }

        private static void HideRect(RectTransform rt)
        {
            if (rt != null) rt.gameObject.SetActive(false);
        }

        private static void HideImage(Image img)
        {
            if (img != null) img.gameObject.SetActive(false);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }

        private static void ClearRendered(List<GameObject> items)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] != null) DestroyUiObject(items[i]);
            }
            items.Clear();
        }

        private static void DestroyUiObject(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
