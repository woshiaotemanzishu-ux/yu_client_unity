using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Lung
{
    /// <summary>
    /// 神纹熔炉控制器(对标老客户端 LungController,模块 181)。进游戏请求 18105 拿熔炉数据(stove_data);
    /// 回包据 GetStoveOpenState(有炉、crucible_id!=0、未过期)增删主界面图标 181(对标老端 setStoveIcon,
    /// 由 SetStoveData→setStoveIcon 触发的 addIcon/deleteIcon,是唯一直接驱动图标的协议)。等级变化
    /// (EVT_ROLE_INFO_UPDATE)复请求 18105(对标老端 CHANGE_LEVEL→Fire LUNG_REQUEST_PROTO 18105),
    /// 让满足开服/等级门槛后图标及时出现。图标的 open_lv/open_day 门由 ActivityIconManager 图标配置统一把控。
    /// 本期只做图标与下一炉开启快照:18112 只落 crucible_id/start_time 后无条件重拉 18105,
    /// 不直接驱动图标、不创建倒计时；神纹穿戴/升级/兑换/商店/红点及面板仍不移植。
    /// </summary>
    public sealed class LungController : BaseController
    {
        public static readonly LungController Instance = new LungController();
#if UNITY_EDITOR
        // CliVerify temporarily intercepts real encoded frames; Player builds omit this seam.
        private static System.Func<byte[], bool> s_outboundIntercept;
#endif
        private LungController() { }

        public const string ICON_TYPE = LungModel.ICON_TYPE;

        // 复请求 18105 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.LUNG_STOVE_INFO, On18105);
            RegisterProtocal(Proto.LUNG_STOVE_OPEN_STATE, On18112);
            // 对标老端 CHANGE_LEVEL→Fire LUNG_REQUEST_PROTO 18105:等级变化时复请求。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnServerTimeRefresh);
            // 老端每次 REFRESH_SERVER_TIME 均发 18112，不做本地去重。
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnServerTimeRefresh);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            LungModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求：对标老端 GAME_START 同时发 18105 与 18112。</summary>
        public void RequestStartup()
        {
            // read(18105,_)->{ok,[]}:请求无字段,裸发。
            RequestStoveInfo();
            RequestOpenSchedule();
        }

        /// <summary>18105 严格空包；等级变化和 18112 回包仅重拉本包。</summary>
        public void RequestStoveInfo() => SendEmpty(Proto.LUNG_STOVE_INFO);

        /// <summary>18112 严格空包，服务端返回下一炉开启快照。</summary>
        public void RequestOpenSchedule() => SendEmpty(Proto.LUNG_STOVE_OPEN_STATE);

        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
                if (s_outboundIntercept(frame)) return;
            }
#endif
            SendFmt(protoId);
        }

        // 18105: crucible_id:h, start_time:i, end_time:i, count:i,
        //        status_list[u16 count × { count:i, status:c }], free_times:h, next_free_time:i
        private void On18105(NetReader r)
        {
            int crucibleId = r.ReadU16();
            long startTime = r.ReadU32();
            long endTime = r.ReadU32();
            r.ReadU32();                 // count(熔炉阶段总次数,面板用,本期不存)
            int statusCount = r.ReadU16();
            for (int i = 0; i < statusCount; i++)
            {
                r.ReadU32();             // status_list.count(阶段次数)
                r.ReadU8();              // status_list.status(阶段奖励领取态)
            }
            r.ReadU16();                 // free_times(免费召唤次数,面板/红点用,本期不存)
            r.ReadU32();                 // next_free_time(下次免费时间,面板用,本期不存)

            LungModel m = LungModel.Instance;
            m.SetStoveData(crucibleId, startTime, endTime);

            // 对标老端 setStoveIcon:开启则加图标(带熔炉结束时间做倒计时),关闭则删。
            // AddIconAsync 内部再过一遍图标配置门(open_lv/open_day),与老端 addIcon 一致。
            if (m.GetStoveOpenState()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, (int)endTime);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("Lung", "18105 神纹熔炉: crucible_id={0} end_time={1} open={2}",
                crucibleId, endTime, m.GetStoveOpenState());
        }

        // pt_181 18112: crucible_id:h,start_time:i。读到尾后无条件追发 18105。
        private void On18112(NetReader r)
        {
            int crucibleId = r.ReadU16();
            long startTime = r.ReadU32();
            LungModel.Instance.ApplyOpenSchedule(crucibleId, startTime);
            RequestStoveInfo();
        }

        // 对标老端:主角等级变化复请求 18105(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStoveInfo();
        }

        // 老端每次 REFRESH_SERVER_TIME 都发 18112，不自行去重双触发。
        private void OnServerTimeRefresh() => RequestOpenSchedule();
    }
}
