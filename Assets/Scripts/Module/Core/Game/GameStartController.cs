using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.CustomActivity;
using Shenxiao.Module.Core.CycleimpActlist;
using Shenxiao.Module.Core.FirstRecharge;
using Shenxiao.Module.Core.FunctionOpen;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Setting;
using Shenxiao.Module.Core.Svip;
using Shenxiao.Module.Core.Vip;
using Shenxiao.Module.Core.WeekCard;

namespace Shenxiao.Module.Core.Game
{
    /// <summary>
    /// Minimal old-client GAME_START gate protocols requested immediately after 10004 succeeds.
    /// </summary>
    public sealed class GameStartController : BaseController
    {
        public static readonly GameStartController Instance = new GameStartController();

        private const int SYS_SETTING = 3;
        private const int FIRST_OPEN_MODULE_ID = 300;
        private const int FIRST_OPEN_SUB_MODULE_ID = 1;
        private static readonly int[] FIRST_OPEN_KEYS = { 1, 2, 3, 4, 5 };

        private GameStartController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.SERVER_TIME, On10201);
            RegisterProtocal(Proto.SETTING_LIST, On10202);
            RegisterProtocal(Proto.ROLE_LIFELONG_COUNT, On13088);
        }

        public void RequestStartupPackets()
        {
            SendFmt(Proto.ROLE_INFO);
            SendFmt(Proto.SERVER_TIME);
            SendFmt(Proto.TASK_LATEST_FINISHED);
            SendFirstOpenStateRequest();
            SendFmt(Proto.SETTING_LIST, "c", SYS_SETTING);
            RoleController.Instance.RequestGrowthPackets(); // 轮5:13011/13017/13046/13080/13086
            VipController.Instance.RequestRechargeProducts();
            FirstRechargeController.Instance.RequestStartupState();
            CustomActivityController.Instance.RequestActivityList();
            SvipController.Instance.RequestSvipInfo();
            WeekCardController.Instance.RequestStartup();
            CycleimpActlistController.Instance.RequestStartup();
            FunctionOpenController.Instance.RequestList();
            GameLog.Info("Game", "requested startup packets: 13001,10201,30005,13088,10202,15800,15905,15908,33101,45120,22700,13800,13011,13017,13046,13080,13086");
        }

        private void SendFirstOpenStateRequest()
        {
            object[] args = new object[3 + FIRST_OPEN_KEYS.Length];
            args[0] = FIRST_OPEN_MODULE_ID;
            args[1] = FIRST_OPEN_SUB_MODULE_ID;
            args[2] = FIRST_OPEN_KEYS.Length;
            for (int i = 0; i < FIRST_OPEN_KEYS.Length; i++)
            {
                args[3 + i] = FIRST_OPEN_KEYS[i];
            }

            SendFmt(Proto.ROLE_LIFELONG_COUNT, new string('h', args.Length), args);
        }

        private void On10201(NetReader reader)
        {
            int openTime = (int)reader.ReadU32();
            int mergeTime = (int)reader.ReadU32();
            int mergeStartTime = (int)reader.ReadU32();
            int mergeCount = (int)reader.ReadU32();
            long serverTime = reader.ReadU64();

            TimeUtil.SyncServerTime(serverTime);
            ServerTimeModel.SetServerTime(openTime, mergeStartTime);
            GameLog.Info("Game", "10201 server time ready: open={0} merge={1} mergeStart={2} mergeCount={3}",
                openTime, mergeTime, mergeStartTime, mergeCount);
            EventDispatcher.Emit(GlobalEvent.EVT_GAME_START_FLAG_READY, "10201");
        }

        private void On10202(NetReader reader)
        {
            int type = reader.ReadU8();
            int count = reader.ReadU16();
            var entries = new List<KeyValuePair<int, int>>(count);
            for (int i = 0; i < count; i++)
            {
                // 严格对标 yu_server pt_102.erl item_to_bin_4:Subtype:16, IsOpen:8。
                // (2026-07-06 修:此前按 {c,c} 读导致整包错位,SettingModel 全是乱数据。)
                int subtype = reader.ReadU16();
                int isOpen = reader.ReadU8();
                entries.Add(new KeyValuePair<int, int>(subtype, isOpen));
            }
            SettingModel.Apply10202(type, entries); // 落设置模型(音量项顺带同步 AudioManager,对标老端 onBlockSettingChange)

            GameLog.Info("Game", "10202 settings ready: type={0} count={1}", type, count);
            if (type == SYS_SETTING)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START_FLAG_READY, "10202@3");
            }
        }

        private void On13088(NetReader reader)
        {
            int moduleId = reader.ReadU16();
            int subModuleId = reader.ReadU16();
            int count = reader.ReadU16();
            for (int i = 0; i < count; i++)
            {
                // 轮5:补通用存储(此前"读完即弃",角色面板等其余 module_id 的终身次数无处可查)。
                int type = reader.ReadU16();
                int val = reader.ReadU16();
                RoleModel.Instance.SetLifelongCount(moduleId, subModuleId, type, val);
            }

            GameLog.Info("Game", "13088 lifelong counts ready: module={0} sub={1} count={2}",
                moduleId, subModuleId, count);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_LIFELONG_COUNT_UPDATE, moduleId, subModuleId);
            if (moduleId == FIRST_OPEN_MODULE_ID && subModuleId == FIRST_OPEN_SUB_MODULE_ID)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START_FLAG_READY, "13088@300@1");
            }
        }
    }
}
