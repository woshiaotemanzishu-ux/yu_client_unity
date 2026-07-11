using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Market
{
    /// <summary>
    /// 市场(交易行)控制器(对标老客户端 commonController/MarketController,模块 151,本期只做图标)。
    /// 进游戏请求 15121 拿跨服市场开放时间 open_time;回包据 open_time 与当前服务器时间在两个主界面图标间
    /// 切换:未到开放时间显示本服市场 151(删 151@1),已到显示跨服市场 151@1(删 151)——对标老端 on15121→showIcon。
    /// 图标是否真正显示仍过图标配置门(open_lv,AddIconAsync 与老端 addIcon 一致)。等级变化(EVT_ROLE_INFO_UPDATE)
    /// 复请求 15121(对标老端 CHANGE_LEVEL→发 15121),让升级到市场开启后图标及时出现。
    /// 交易/上架/下架/购买/求购/记录等玩法协议(15100-15120/15122)与所有面板全部不移植。
    /// </summary>
    public sealed class MarketController : BaseController
    {
        public static readonly MarketController Instance = new MarketController();
        private MarketController() { }

        public const string ICON_TYPE_LOCAL = MarketModel.ICON_TYPE_LOCAL;
        public const string ICON_TYPE_KF = MarketModel.ICON_TYPE_KF;

        // 复请求 15121 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.MARKET_ICON_INFO, On15121);
            // 对标老端 CHANGE_LEVEL→发 15121:等级变化时复请求(市场按 151 图标配置 open_lv 开启)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 两个图标都删(对标老端 showIcon 只留一个,断线时两者都清)。
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_LOCAL);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_KF);
            MarketModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 15121)。</summary>
        public void RequestStartup()
        {
            // read(15121,_)->{ok,[]}:请求无字段,裸发。
            SendFmt(Proto.MARKET_ICON_INFO);
        }

        // 15121: open_time:i(跨服市场开放时间戳;write(15121,[OpenTime]) -> <<OpenTime:32>>)。
        private void On15121(NetReader r)
        {
            int openTime = (int)r.ReadU32();
            // 对标老端 on15121:kf_open = open_time>0 && serverTime>open_time
            //(TimeUtil.NowSec 为 10201 同步后的服务器时间秒)。
            bool kfOpen = openTime > 0 && TimeUtil.NowSec() > openTime;
            MarketModel.Instance.SetKfOpen(openTime, kfOpen);
            RefreshIcon();

            GameLog.Info("Market", "15121 市场: open_time={0} kf_open={1} show={2}",
                openTime, kfOpen, MarketModel.Instance.GetShowIconType());
        }

        // 对标老端 showIcon:先删另一图标,再加当前图标(AddIconAsync 过图标配置门 open_lv,与老端 addIcon 一致)。
        private void RefreshIcon()
        {
            MarketModel m = MarketModel.Instance;
            ActivityIconManager.Instance.DeleteIcon(m.GetHideIconType());
            _ = ActivityIconManager.Instance.AddIconAsync(m.GetShowIconType());
        }

        // 对标老端:主角等级变化复请求 15121(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }
    }
}
