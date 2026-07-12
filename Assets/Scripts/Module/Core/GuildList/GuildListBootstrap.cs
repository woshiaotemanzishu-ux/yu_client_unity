using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Guild;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.GuildList
{
    /// <summary>
    /// 公会列表模块装配(对标 BagBootstrap/RoleBootstrap):启动即向主界面功能入口中央路由登记 "guild" 打开器,
    /// 使点击 HUD 底部公会功能图标即可打开/关闭公会面板(MainUIRouter.Open("guild") → <see cref="OpenGuildEntry"/>)。
    /// 对标老端 GuildModel.OpenGuildView 按会籍分支(mainRoleVo.guild_id>0):有会 → <see cref="GuildMainFlow"/>
    /// (信息/成员真接线,轮13a);无会 → <see cref="GuildListFlow"/>(公会列表/建会/申请,已接真)。
    /// 断线(非游戏内自动重连)时清两侧面板。
    /// </summary>
    public static class GuildListBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("guild", OpenGuildEntry);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        /// <summary>会籍分支入口(对标老端 GuildModel.OpenGuildView:role_vo.guild_id>0 ? 主界面 : 列表)。</summary>
        private static void OpenGuildEntry()
        {
            if (RoleModel.Instance.GuildId > 0) GuildMainFlow.Toggle();
            else GuildListFlow.Toggle();
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            GuildListFlow.Reset();
            GuildMainFlow.Reset();
        }
    }
}
