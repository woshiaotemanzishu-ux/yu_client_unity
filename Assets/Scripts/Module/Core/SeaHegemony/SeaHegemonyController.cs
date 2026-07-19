using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.SeaHegemony
{
    /// <summary>
    /// 四海争霸(海域)控制器(对标老客户端 SeaHegemonyController,大系统只移植主界面图标 18601)。
    /// 进游戏请求 18600 拿阵营(报名态),回包后据老端流程再请求 18625 拿报名结束时间;
    /// 18625 回包据报名窗口(结束前 24h)增删图标 18601,文字取 "报名中"/"已报名"(GetIconText)。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求 18600(对标老端 GAME_START/等级变化重发链),
    /// 让升到 400 级、开服 30 天开启四海争霸后图标及时出现。
    /// 砖块/攻防/阵营/官职/特权等所有玩法协议(18601-18624 玩法段/18650-18715)一律不注册,本期只做图标。
    /// 注:老端图标由 18625(报名/结束时间)驱动 addIcon(18601);18607(活动开始/时间)只刷红点、不增删图标,故不注册。
    /// 轮22 族错误出口批补 18614/18616(舰船/职务错误壳)+ 18700(pt_187 日常家族错误壳,老端也共用本控制器,
    /// 不新建 187 族 Controller)。
    /// </summary>
    public sealed class SeaHegemonyController : BaseController
    {
        public static readonly SeaHegemonyController Instance = new SeaHegemonyController();
        private SeaHegemonyController() { }

        public const string ICON_TYPE = SeaHegemonyModel.ICON_TYPE;

        // 复请求 18600 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.SEAHEGEMONY_INFO, On18600);
            RegisterProtocal(Proto.SEAHEGEMONY_SIGNUP, On18625);
            RegisterProtocal(Proto.SEACRAFT_ERROR_18614, On18614);
            RegisterProtocal(Proto.SEACRAFT_ERROR_18616, On18616);
            RegisterProtocal(Proto.SEACRAFT_DAILY_ERROR, On18700);
            // 对标老端 GAME_START→发 18600、等级变化重发链:等级变化时复请求(400级/开服30天开启)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            SeaHegemonyModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 18600)。</summary>
        public void RequestStartup()
        {
            // read(18600,_)->{ok,[]}:请求无字段,裸发。
            SendFmt(Proto.SEAHEGEMONY_INFO);
        }

        // 18600: camp:i, server_id:i, server_num:h, guild_id:l, guild_name:string, king_name:string,
        //        fight:l, count:l, self_level:h, reward_status:c。只取 camp 判报名态,余为面板/玩法字段读掉。
        // 对标老端 18600 handler 里在收到基础信息后 SendFmtToGame(18625) 拿报名结束时间(驱动图标)。
        private void On18600(NetReader r)
        {
            int camp = (int)r.ReadU32();
            r.ReadU32();     // server_id(跨服id,本期图标不需)
            r.ReadU16();     // server_num(跨服编号)
            r.ReadU64();     // guild_id(所属公会id)
            r.ReadString();  // guild_name(公会名)
            r.ReadString();  // king_name(海王名)
            r.ReadU64();     // fight(战功/积分)
            r.ReadU64();     // count(计数)
            r.ReadU16();     // self_level(海域官职等级)
            r.ReadU8();      // reward_status(每日奖励领取态,面板红点用)

            SeaHegemonyModel.Instance.SetSeaInfo(camp);

            // 拿报名结束时间(驱动图标 18601)。read(18625,_)->{ok,[]}:裸发。
            SendFmt(Proto.SEAHEGEMONY_SIGNUP);
        }

        // 18625: end_time:i(报名结束时间戳)。据报名窗口(结束前 24h)增删图标 18601(对标老端 18625 handler)。
        private void On18625(NetReader r)
        {
            long endTime = r.ReadU32();
            SeaHegemonyModel.Instance.SetSignupEndTime(endTime);
            RefreshIcon();
        }

        private void RefreshIcon()
        {
            SeaHegemonyModel m = SeaHegemonyModel.Instance;
            bool open = m.GetEntranceOpenState();
            // 18601 图标配置 controll_by_own_fun=true(通用扫描跳过,由本控制器负责增删);
            // open_lv=400/open_day=30 为真实门槛,故走 AddIconAsync 过一遍图标配置门(与老端 addIcon 一致),
            // 不用 AddOwnerIcon(那是给 open_lv=999 这类"配置永关、纯自管理"图标绕门用的)。
            if (open) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, 0, m.GetIconText());
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("SeaHegemony", "18625 四海争霸: camp={0} endTime={1} open={2}",
                m.Camp, m.SignupEndTime, open);
        }

        // 对标老端:主角等级变化复请求 18600(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        /// <summary>18614 舰船错误出口(对标老端 SeaHegemonyController.ts:301-308:
        /// scmd&amp;&amp;code!=1→ErrorCodeShow,无其它副作用)。错误码表未移植,显码降级。</summary>
        private void On18614(NetReader r)
        {
            int code = (int)r.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("操作失败(" + code + ")");
                GameLog.Warn("SeaHegemony", "18614 code={0}", code);
            }
        }

        /// <summary>18616 舰船职务/分配错误出口(对标老端 SeaHegemonyController.ts:318-325:
        /// scmd&amp;&amp;code!=1→ErrorCodeShow,无其它副作用)。错误码表未移植,显码降级。</summary>
        private void On18616(NetReader r)
        {
            int code = (int)r.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("操作失败(" + code + ")");
                GameLog.Warn("SeaHegemony", "18616 code={0}", code);
            }
        }

        /// <summary>18700 四海争霸日常(pt_187)家族统一错误出口(对标老端 SeaHegemonyController.ts:590-595:
        /// 无条件 ErrorCodeShow(code)——服务端 send_error/2(pp_seacraft_daily.erl:373-376)只在错误分支
        /// 调用,回包恒为错误码,故老端无 if 守卫直接显码,本端如实镜像)。错误码表未移植,显码降级。</summary>
        private void On18700(NetReader r)
        {
            int code = (int)r.ReadU32();
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("SeaHegemony", "18700 日常错误壳 code={0}", code);
        }
    }
}
