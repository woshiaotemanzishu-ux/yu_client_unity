using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Chat;
using Shenxiao.Module.Core.Daily;
using Shenxiao.Module.Core.FirstBlood;
using Shenxiao.Module.Core.Friend;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Guild;
using Shenxiao.Module.Core.Mail;
using Shenxiao.Module.Core.PushGift;
using Shenxiao.Module.Core.RedPacket;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.RushGift;
using Shenxiao.Module.Core.Team;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面动态消息通知队列。预制体只维护一个完整模板和一个自动布局容器；
    /// 运行时按各模型的真实消息状态生成，不把活动入口或固定按钮写死在这里。
    /// </summary>
    public sealed class MainUINotificationView : MainUINotificationViewBind
    {
        public MainUINotificationItem NotificationItemTemplate;
        public RectTransform NotificationContent;

        private readonly List<MainUINotificationItem> _items = new List<MainUINotificationItem>();

        private readonly struct NotificationData
        {
            public NotificationData(string iconPath, string route, int count = 0)
                : this(iconPath, () => MainUIRouter.Open(route), count)
            {
            }

            public NotificationData(string iconPath, Action onClick, int count = 0)
            {
                IconPath = iconPath;
                OnClick = onClick;
                Count = count;
            }

            public string IconPath { get; }
            public Action OnClick { get; }
            public int Count { get; }
        }

        protected override void OnInit()
        {
            if (NotificationItemTemplate != null)
            {
                NotificationItemTemplate.gameObject.SetActive(false);
            }
            if (_box_notification_bar != null) _box_notification_bar.gameObject.SetActive(false);
            SetActive(_box_help_tips, false);
            RouteClick(_box_help, "guildhelp");
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, RefreshGuildHelp);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_TIME_REFRESH, RefreshGuildHelp);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshAll);
            EventDispatcher.On(GlobalEvent.EVT_MAIL_UNREAD_UPDATE, RefreshNotifications);
            EventDispatcher.On<long>(GlobalEvent.EVT_CHAT_PRIVATE_UPDATE, OnPrivateChatUpdate);
            EventDispatcher.On(GlobalEvent.EVT_FRIEND_REDDOT_UPDATE, RefreshNotifications);
            EventDispatcher.On(GlobalEvent.EVT_DAILY_RES_FIND_UPDATE, RefreshNotifications);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_FIRSTBLOOD_UPDATE, OnFirstBloodUpdate);
            EventDispatcher.On<long>(GlobalEvent.EVT_REDPACKET_UPDATE, OnRedPacketUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TEAM_BE_INVITED_UPDATE, RefreshNotifications);
            EventDispatcher.On(GlobalEvent.EVT_PUSH_GIFT_UPDATE, RefreshNotifications);
            EventDispatcher.On(GlobalEvent.EVT_RUSH_GIFT_UPDATE, RefreshNotifications);
            RefreshAll();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, RefreshGuildHelp);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_TIME_REFRESH, RefreshGuildHelp);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshAll);
            EventDispatcher.Off(GlobalEvent.EVT_MAIL_UNREAD_UPDATE, RefreshNotifications);
            EventDispatcher.Off<long>(GlobalEvent.EVT_CHAT_PRIVATE_UPDATE, OnPrivateChatUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_FRIEND_REDDOT_UPDATE, RefreshNotifications);
            EventDispatcher.Off(GlobalEvent.EVT_DAILY_RES_FIND_UPDATE, RefreshNotifications);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_FIRSTBLOOD_UPDATE, OnFirstBloodUpdate);
            EventDispatcher.Off<long>(GlobalEvent.EVT_REDPACKET_UPDATE, OnRedPacketUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_TEAM_BE_INVITED_UPDATE, RefreshNotifications);
            EventDispatcher.Off(GlobalEvent.EVT_PUSH_GIFT_UPDATE, RefreshNotifications);
            EventDispatcher.Off(GlobalEvent.EVT_RUSH_GIFT_UPDATE, RefreshNotifications);
        }

        private void RefreshAll()
        {
            RefreshGuildHelp();
            RefreshNotifications();
        }

        private void OnPrivateChatUpdate(long _) => RefreshNotifications();
        private void OnFirstBloodUpdate(int _, int __) => RefreshNotifications();
        private void OnRedPacketUpdate(long _) => RefreshNotifications();

        private void RefreshNotifications()
        {
            if (NotificationItemTemplate == null || NotificationContent == null) return;

            List<NotificationData> active = CollectActiveNotifications();
            EnsureItems(active.Count);

            for (int i = 0; i < _items.Count; i++)
            {
                MainUINotificationItem item = _items[i];
                if (item == null) continue;

                bool visible = i < active.Count;
                item.gameObject.SetActive(true);
                if (!visible)
                {
                    item.gameObject.SetActive(false);
                    continue;
                }

                NotificationData data = active[i];
                item.SetData(
                    data.IconPath,
                    true,
                    data.Count,
                    data.OnClick);
            }

            if (_box_notification_bar != null)
            {
                _box_notification_bar.gameObject.SetActive(active.Count > 0);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(NotificationContent);
        }

        private static List<NotificationData> CollectActiveNotifications()
        {
            var list = new List<NotificationData>(9);

            int teamCount = TeamModel.Instance.BeInvitedList.Count;
            if (TeamModel.Instance.IsOpenTeam(out _) && teamCount > 0)
            {
                list.Add(new NotificationData(
                    GameResPath.GetIcon("mainUI", "ui_notice_4"),
                    TeamController.Instance.ShowPendingInvites,
                    teamCount));
            }

            int redPacketCount = RedPacketModel.Instance.GetMainNotificationCount(RoleModel.Instance.RoleId);
            if (redPacketCount > 0)
            {
                list.Add(new NotificationData(
                    GameResPath.GetIcon("mainUI", "ui_notice_6"),
                    "redpacket",
                    redPacketCount));
            }

            if (FirstBloodModel.Instance.HasMainNotification())
            {
                list.Add(new NotificationData(
                    GameResPath.GetIcon("mainUI", "UI_bsss_001"),
                    "firstblood"));
            }
            if (MailModel.Instance.HasUnread)
            {
                list.Add(new NotificationData(
                    GameResPath.GetIcon("mainUI", "ui_notice_3"),
                    "email"));
            }

            int privateUnread = ChatModel.Instance.TotalPrivateUnread;
            if (FriendModel.Instance.HaveNewApply || privateUnread > 0)
            {
                list.Add(new NotificationData(
                    GameResPath.GetIcon("mainUI", "ui_notice_7"),
                    "chat",
                    privateUnread));
            }
            if (DailyModel.Instance.ResNormalCanFind())
            {
                list.Add(new NotificationData(
                    GameResPath.GetIcon("mainUI", "ui_notice_8"),
                    "dailyfind"));
            }

            int giftCount = PushGiftModel.Instance.GetMainNotificationCount();
            if (giftCount > 0)
            {
                list.Add(new NotificationData(
                    GameResPath.GetIcon("mainUI", "icon_ts_34"),
                    "pushgift",
                    giftCount));
            }

            int levelRewardCount = RushGiftModel.Instance.GetMainNotificationCount();
            if (levelRewardCount > 0)
            {
                list.Add(new NotificationData(
                    GameResPath.GetIcon("mainUI", "icon_ts_35"),
                    "levelreward",
                    levelRewardCount));
            }

            return list;
        }

        private void EnsureItems(int count)
        {
            while (_items.Count < count)
            {
                MainUINotificationItem item = Instantiate(NotificationItemTemplate, NotificationContent, false);
                item.gameObject.name = "NotificationItem";
                item.gameObject.SetActive(false);
                _items.Add(item);
            }
        }

        private async void RefreshGuildHelp()
        {
            await GuildConfigs.EnsureLoaded();
            if (this == null || _box_help == null) return;

            int.TryParse(GuildConfigs.GetKv(26), out int conditionLevel);
            int.TryParse(GuildConfigs.GetKv(28), out int openDay);
            bool visible = RoleModel.Instance.GuildId > 0
                           && RoleModel.Instance.Level >= conditionLevel
                           && ServerTimeModel.GetOpenServerDay() >= openDay;
            _box_help.gameObject.SetActive(visible);
        }

        private static void RouteClick(Component target, string viewKey)
        {
            if (target != null) UIUtil.AddClick(target, () => MainUIRouter.Open(viewKey));
        }

        private static void SetActive(Component target, bool active)
        {
            if (target != null) target.gameObject.SetActive(active);
        }
    }

}
