using System;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Kf1vn
{
    /// <summary>
    /// 诸天王者(跨服1vn)控制器(对标老客户端 Kf1vnController,模块 621)。进游戏请求 62101 拿活动阶段;
    /// 回包(62101 stage push,老端 Handler62101→ShowIcon)据 stage 增删主界面图标 621(loc6 通知区):
    /// stage>=1 且 stage!=6 显示(报名中/进行中),否则删除。等级变化(EVT_ROLE_INFO_UPDATE)复请求 62101
    /// (对标老端 CHANGE_LEVEL→InitRequest→发 62101),让升到开启等级后图标及时出现;实际是否显示由图标
    /// 配置门(open_lv/open_day,ActivityIconManager.AddIcon 内校验)与 stage 共同把控。
    /// 62100 活动报名快照只补齐既有图标精度；62102 报名交易、竞猜/战斗/结算/兑换等玩法协议
    /// 与所有面板均未移植。R222仅补62107 C2S-only严格空包退出请求，未绑定UI；服务端从不下发
    /// 62107，严禁接收、结果模型或等待回执。
    /// </summary>
    public sealed class Kf1vnController : BaseController
    {
        public static readonly Kf1vnController Instance = new Kf1vnController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_exitOutboundIntercept = null;
        private static Func<byte[], bool> s_activityInfoOutboundIntercept = null;
#endif
        private Kf1vnController() { }

        public const string ICON_TYPE = Kf1vnModel.ICON_TYPE;

        // 复请求 62101 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.KF1VN_STAGE_INFO, On62101);
            RegisterProtocal(Proto.KF1VN_ACTIVITY_INFO, On62100);
            RegisterProtocal(Proto.KF1VN_ERROR, On62103);
            RegisterProtocal(Proto.KF1VN_QUIZ_ERROR, On62132);
            // 对标老端 CHANGE_LEVEL→InitRequest 发 62101:等级变化时复请求(达开启等级后图标出现)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 Kf1vnController.ts:130-137:DAY_CHANGE→若开服天倒退到低于 ConfigFuncOpenCondition
            // [Kf1vnEnterView].open_day 则删图标(不重发 62101)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            Kf1vnModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START→InitRequest 发 62101)。</summary>
        public void RequestStartup()
        {
            // read(62101,_)->{ok,[]}:请求无字段,裸发。老端 InitRequest 会先按 open_lv/open_day 门判再发,
            // 此处统一裸发——是否显示图标最终由 AddIconAsync 内的图标配置门(open_lv/open_day)+ stage 决定。
            SendFmt(Proto.KF1VN_STAGE_INFO);
        }

        /// <summary>62107 退出诸天王者场景。仅 C2S 严格空包；场景切换是服务端权威结果，无专属回执。</summary>
        public void RequestExit()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.KF1VN_EXIT, null, null);
            if (s_exitOutboundIntercept != null && s_exitOutboundIntercept(frame)) return;
#endif
            SendFmt(Proto.KF1VN_EXIT);
        }

        // 62101(stage push,老端 Handler62101→ShowIcon 的驱动协议):
        // Stage:8, Turn:16, Edtime:32, SubStage:8, SubEdtime:32
        private void On62101(NetReader r)
        {
            int stage = r.ReadU8();
            int turn = r.ReadU16();
            long edtime = r.ReadU32();
            int subStage = r.ReadU8();
            long subEdtime = r.ReadU32();

            Kf1vnModel m = Kf1vnModel.Instance;
            int oldStage = m.Stage;
            bool hadStage = m.HasStageInfo;
            m.SetStageInfo(stage, turn, edtime, subStage, subEdtime);
            if (!hadStage || oldStage != stage) RequestActivityInfo();

            // 对标老端 ShowIcon:stage>=1 且 stage!=6 → addIcon(带阶段文字),否则 deleteIcon。
            if (m.GetEntranceOpenState()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, 0, m.GetIconText());
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("Kf1vn", "62101 诸天王者: stage={0} turn={1} sub={2} open={3}",
                stage, turn, subStage, m.GetEntranceOpenState());
        }

        /// <summary>62100 活动报名原始快照：is_sign:u8, sign_num:u32, def_num:u16, zone:u8。</summary>
        private void On62100(NetReader r)
        {
            Kf1vnModel m = Kf1vnModel.Instance;
            m.SetActivityInfo(r.ReadU8(), r.ReadU32(), r.ReadU16(), r.ReadU8());
            if (m.GetEntranceOpenState())
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, 0, m.GetIconText());
            else
                ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
        }

        /// <summary>62100 仅由 62101 首包或阶段变化后的内部对账触发。</summary>
        private void RequestActivityInfo()
        {
#if UNITY_EDITOR
            if (s_activityInfoOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.KF1VN_ACTIVITY_INFO, null, null);
                if (s_activityInfoOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.KF1VN_ACTIVITY_INFO);
        }

        // 对标老端 DAY_CHANGE(见 Register 内注释):老端读 Config.PRELOAD_CLIENT_CONFIG.ConfigFuncOpenCondition
        // .Kf1vnEnterView.open_day;本端无权改 FuncOpenConfig.cs(不在本包文件所有权内)加窄口径访问器,
        // 改用图标 621 自身配置的 OpenDay(MainUIConfigs.GetFunctionIconCfg,configfunctionicon.json 表内
        // open_day 与 Kf1vnEnterView.open_day 现值均为 5,语义等价)。openDay 倒退到门槛以下时删图标。
        private void OnServerDayChange()
        {
            MainUIConfigs.FunctionIconCfg cfg = MainUIConfigs.GetFunctionIconCfg(ICON_TYPE);
            if (cfg != null && ServerTimeModel.GetOpenServerDay() < cfg.OpenDay)
                ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
        }

        // 对标老端:主角等级变化复请求 62101(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        /// <summary>62103 错误出口(对标老端 Kf1vnController.ts:242-245 Handler62103:
        /// 无条件 ErrorCodeShow(error_code),无其它副作用)。错误码表未移植,显码降级。</summary>
        private void On62103(NetReader r)
        {
            int code = (int)r.ReadU32();
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("Kf1vn", "62103 code={0}", code);
        }

        /// <summary>62132 竞猜/匹配相关错误出口(对标老端 Kf1vnController.ts:444-447 Handler62132:
        /// 无条件 ErrorCodeShow(error_code),忽略 error_args,无其它副作用)。错误码表/args 格式化未移植,显码降级。</summary>
        private void On62132(NetReader r)
        {
            int code = (int)r.ReadU32();
            string args = r.ReadString();
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("Kf1vn", "62132 code={0} args={1}", code, args);
        }
    }
}
