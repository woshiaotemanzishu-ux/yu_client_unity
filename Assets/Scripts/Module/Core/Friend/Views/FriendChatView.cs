using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Chat;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Chat;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 私聊窗口(对标老客户端 friend/FriendChatView.ts;Bind 类烤在 ChatModule.prefab,经
    /// <see cref="Shenxiao.Editor.UiCreator.Friend.FriendBindUpgrader"/> 嫁接进 FriendModule.prefab 顶层)。
    /// 顶部会话 Tab 列表(Content/tabScroller,FriendChatTabItem)+ 消息列表(Content1/contentScroller,
    /// FriendChatItem=对方/FriendMineChatItem=自己)+ 输入框 + 发送(60级门槛,对标老端)。
    ///
    /// 数据面 100% 复用轮6 ChatModel 私聊 API(GetPrivateMessages/PrivateChatTabList/AddPrivateChatTab)+
    /// ChatController(SendChat/SendClickCache=11027/SendViewPrivatePlayerInfo=11028)。展示信息(昵称/职业/转生/
    /// 等级)因 ChatModel.PrivateChatTabList 只存 role_id,由 FriendModel.ChatPartnerInfo 缓存补全
    /// (来源:打开时传入的 FriendVo,或 11028/11002 回包 Figure)。
    ///
    /// 降级(TODO,按规格允许的"接不全列清单"):表情面板(faceBtn→FriendFacePanel)、聊天物品面板(_bag)、
    /// 位置分享(_position)、赠花(_send_flower→MarriageFlowerView)、防诈骗提示条(lb_preventfraud,
    /// ClientPreventFraud 配置未接,默认隐藏)、正文物品/位置超链接解析——均未接线,仅日志/隐藏降级。
    /// </summary>
    public sealed class FriendChatView : FriendChatViewBind
    {
        public sealed class OpenArgs
        {
            public long RoleId;
            public string RoleName = "";
            public int Career;
            public int Turn;
            public int Lv;
        }

        private long _roleId;
        private readonly List<FriendChatTabItem> _tabPool = new List<FriendChatTabItem>();
        private readonly List<GameObject> _msgCells = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_FriendChatTabItem != null) _tpl_FriendChatTabItem.SetActive(false);
            if (_tpl_FriendMineChatItem != null) _tpl_FriendMineChatItem.SetActive(false);
            if (_tpl_FriendChatItem != null) _tpl_FriendChatItem.SetActive(false);
            // ClientPreventFraud 配置未迁移(config 缓存表),防诈骗提示条默认隐藏(老端按配置显示提示文案)。
            if (lb_preventfraud != null) lb_preventfraud.gameObject.SetActive(false);
            if (bg != null) bg.gameObject.SetActive(false);
            if (_img_bag_select != null) _img_bag_select.gameObject.SetActive(false);

            UIUtil.AddClick(sendBtn, OnClickSend);
            UIUtil.AddClick(faceBtn, () => GameLog.Info("Friend", "私聊[表情] → FriendFacePanel 未接线,TODO"));
            UIUtil.AddClick(_bag, () => GameLog.Info("Friend", "私聊[物品] → 聊天物品面板未接线,TODO"));
            UIUtil.AddClick(_position, () => GameLog.Info("Friend", "私聊[位置分享] → 未接线,TODO"));
            UIUtil.AddClick(_send_flower, () => GameLog.Info("Friend", "私聊[赠花] → MarriageFlowerView 未接线,TODO"));
        }

        protected override void OnShow(object args)
        {
            if (args is OpenArgs oa)
            {
                _roleId = oa.RoleId;
                FriendModel.Instance.RememberChatPartner(oa.RoleId, oa.RoleName, oa.Career, oa.Turn, oa.Lv);
            }
            if (_roleId == 0) { Hide(); return; }

            Subscribe();
            OpenChatFor(_roleId);
        }

        protected override void OnHide()
        {
            Unsubscribe();
            _refreshToken++; // 使任何仍在途的 LoadOtherPartyCell 异步实例失效(关窗后落地即丢弃)
            ClearMessageCells();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On<long>(GlobalEvent.EVT_CHAT_PRIVATE_UPDATE, OnPrivateUpdate);
            EventDispatcher.On<ChatModel.PrivatePlayerInfo>(GlobalEvent.EVT_CHAT_PRIVATE_PLAYER_INFO, OnPlayerInfo);
            EventDispatcher.On<FriendModel.FriendVo>(GlobalEvent.EVT_FRIEND_ONLINE_UPDATE, OnFriendOnline);
            EventDispatcher.On<long, int>(GlobalEvent.EVT_FRIEND_INTIMACY_UPDATE, OnIntimacy);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off<long>(GlobalEvent.EVT_CHAT_PRIVATE_UPDATE, OnPrivateUpdate);
            EventDispatcher.Off<ChatModel.PrivatePlayerInfo>(GlobalEvent.EVT_CHAT_PRIVATE_PLAYER_INFO, OnPlayerInfo);
            EventDispatcher.Off<FriendModel.FriendVo>(GlobalEvent.EVT_FRIEND_ONLINE_UPDATE, OnFriendOnline);
            EventDispatcher.Off<long, int>(GlobalEvent.EVT_FRIEND_INTIMACY_UPDATE, OnIntimacy);
        }

        // 对标老端 update(id,data):本会话有新消息 → 消红点+刷新;否则(新对象来信)加一个 Tab。
        private void OnPrivateUpdate(long targetId)
        {
            if (targetId == _roleId)
            {
                ChatController.Instance.SendClickCache(_roleId);
                RefreshMessages();
            }
            else
            {
                IReadOnlyList<ChatMessage> msgs = ChatModel.Instance.GetPrivateMessages(targetId);
                if (msgs.Count > 0)
                {
                    ChatMessage last = msgs[msgs.Count - 1];
                    if (last.Figure != null)
                        FriendModel.Instance.RememberChatPartner(targetId, last.Figure.name, last.Figure.career, last.Figure.turn, last.Figure.level);
                    ChatModel.Instance.AddPrivateChatTab(targetId, newChat: true);
                }
            }
            RefreshTabs();
        }

        private void OnPlayerInfo(ChatModel.PrivatePlayerInfo info)
        {
            if (info == null || info.RoleId != _roleId) return;
            RefreshHeader(info);
        }

        private void OnFriendOnline(FriendModel.FriendVo vo)
        {
            if (vo == null || vo.RoleId != _roleId || _lb_online == null) return;
            _lb_online.text = vo.OnlineFlag == 1 ? "(在线)" : "(离线)";
            _lb_online.color = vo.OnlineFlag == 1 ? new Color(0.039f, 0.584f, 0.243f) : new Color(1f, 0.31f, 0.31f);
        }

        private void OnIntimacy(long roleId, int intimacy)
        {
            if (roleId == _roleId && _lb_intimacy != null) _lb_intimacy.text = intimacy.ToString();
        }

        /// <summary>切换/首次打开某会话(对标老端 setData+initView):置顶 Tab + 消红点(11027)+ 拉资料(11028)。</summary>
        private void OpenChatFor(long roleId)
        {
            _roleId = roleId;
            ChatModel.Instance.AddPrivateChatTab(roleId); // newChat=false → 置顶(对标老端 addPrivateChatTab 无第二参)
            ChatController.Instance.SendClickCache(roleId);
            ChatController.Instance.SendViewPrivatePlayerInfo(roleId);

            FriendModel.ChatPartnerInfo info = FriendModel.Instance.GetChatPartner(roleId);
            if (chatName != null) chatName.text = info != null ? info.Name : ("角色" + roleId);
            if (_input != null) _input.text = "";

            RefreshTabs();
            RefreshMessages();
        }

        private void RefreshTabs()
        {
            IReadOnlyList<long> ids = ChatModel.Instance.PrivateChatTabList;
            EnsureTabPool(ids.Count);
            for (int i = 0; i < _tabPool.Count; i++)
            {
                bool active = i < ids.Count;
                _tabPool[i].gameObject.SetActive(active);
                if (!active) continue;
                _tabPool[i].SetOnClick(OnClickTab);
                _tabPool[i].SetData(ids[i], ids[i] == _roleId);
            }
        }

        private void EnsureTabPool(int count)
        {
            if (_tpl_FriendChatTabItem == null || Content == null) return;
            while (_tabPool.Count < count)
            {
                GameObject go = Instantiate(_tpl_FriendChatTabItem, Content);
                go.SetActive(true);
                _tabPool.Add(go.GetComponent<FriendChatTabItem>());
            }
        }

        private void OnClickTab(long roleId)
        {
            if (roleId == _roleId) return;
            OpenChatFor(roleId);
        }

        /// <summary>私聊气泡"对方"走独立 prefab(Assets/Prefabs/UI/Friend/FriendChatItem.prefab)Addressable
        /// 懒加载——FriendChatViewBind._tpl_FriendChatItem 这个内嵌占位节点经核查(r7_unity §3)未挂真组件
        /// (批转换器烤入产物残留),故不使用它,改走 <see cref="ResManager.InstantiateAsync"/>(对标 FriendUiUtil.EnsureHead
        /// 同款懒加载路径);"自己"气泡(FriendMineChatItem)是 FriendChatView 自带局部 __Templates 里的正常模板,直接
        /// Instantiate 即可。</summary>
        private int _refreshToken;

        private void RefreshMessages()
        {
            ClearMessageCells();
            if (Content1 == null) return;
            int token = ++_refreshToken;
            IReadOnlyList<ChatMessage> list = ChatModel.Instance.GetPrivateMessages(_roleId);
            long myId = RoleModel.Instance.RoleId;
            foreach (ChatMessage msg in list)
            {
                bool isMine = msg.PlayerId == myId;
                if (isMine)
                {
                    if (_tpl_FriendMineChatItem == null) continue;
                    GameObject cell = Instantiate(_tpl_FriendMineChatItem, Content1);
                    cell.SetActive(true);
                    cell.GetComponent<FriendMineChatItem>()?.SetData(msg);
                    _msgCells.Add(cell);
                }
                else
                {
                    _ = LoadOtherPartyCell(msg, token);
                }
            }
        }

        private async System.Threading.Tasks.Task LoadOtherPartyCell(ChatMessage msg, int token)
        {
            GameObject cell = await Shenxiao.Framework.Res.ResManager.InstantiateAsync(
                Shenxiao.Framework.Res.GameResPath.GetUIPrefab("friend", "FriendChatItem"), Content1);
            if (cell == null) return;
            if (token != _refreshToken || Content1 == null) { Destroy(cell); return; } // 已切会话/关闭窗口,丢弃迟到实例
            cell.SetActive(true);
            cell.GetComponent<FriendChatItem>()?.SetData(msg);
            _msgCells.Add(cell);
        }

        private void ClearMessageCells()
        {
            foreach (GameObject c in _msgCells) if (c != null) Destroy(c);
            _msgCells.Clear();
        }

        private void RefreshHeader(ChatModel.PrivatePlayerInfo info)
        {
            if (chatName != null && info.Figure != null) chatName.text = info.Figure.name;
            if (_lb_online != null)
            {
                _lb_online.text = info.Online ? "(在线)" : "(离线)";
                _lb_online.color = info.Online ? new Color(0.039f, 0.584f, 0.243f) : new Color(1f, 0.31f, 0.31f);
            }
            if (_lb_fighting != null) _lb_fighting.text = info.CombatPower.ToString();
            if (_lb_intimacy != null) _lb_intimacy.text = info.Intimacy.ToString();
            if (_vip_icon != null) _vip_icon.gameObject.SetActive(false); // VIP 角标素材未接,TODO

            if (info.Figure != null)
                FriendModel.Instance.RememberChatPartner(info.RoleId, info.Figure.name, info.Figure.career, info.Figure.turn, info.Figure.level);
        }

        private void OnClickSend()
        {
            if (RoleModel.Instance.Level < 60)
            {
                TipsManager.Toast("60级开启私聊好友功能");
                return;
            }
            string text = _input != null ? _input.text : "";
            if (string.IsNullOrEmpty(text))
            {
                TipsManager.Toast("请输入聊天内容");
                return;
            }
            ChatController.Instance.SendChat(ChatModel.ChannelPrivate, text, _roleId);
            if (_input != null) _input.text = "";
        }
    }
}
