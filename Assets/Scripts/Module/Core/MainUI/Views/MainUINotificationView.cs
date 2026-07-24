using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Guild;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面临时通知区。只承载消息/邀请/协助类入口；活动图标统一由 MainUIActivityView 按配置管理。
    /// </summary>
    public sealed class MainUINotificationView : MainUINotificationViewBind
    {
        protected override void OnInit()
        {
            HideTransientEntries();
            RouteClick(_box_help, "guildhelp");
            RouteClick(_box_team, "team_invite");
            RouteClick(_box_red_packet, "redpacket");
            RouteClick(_box_email, "email");
            RouteClick(_box_chat, "chat");
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, RefreshGuildHelp);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_TIME_REFRESH, RefreshGuildHelp);
            RefreshGuildHelp();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, RefreshGuildHelp);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_TIME_REFRESH, RefreshGuildHelp);
        }

        private void HideTransientEntries()
        {
            SetActive(_box_team, false);
            SetActive(_box_red_packet, false);
            SetActive(_box_email, false);
            SetActive(_box_chat, false);
            SetActive(_box_help_tips, false);
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
