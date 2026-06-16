using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

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
            GameLog.Info("Game", "requested startup packets: 13001,10201,30005,13088,10202");
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
            for (int i = 0; i < count; i++)
            {
                reader.ReadU8();
                reader.ReadU8();
            }

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
                reader.ReadU16();
                reader.ReadU16();
            }

            GameLog.Info("Game", "13088 lifelong counts ready: module={0} sub={1} count={2}",
                moduleId, subModuleId, count);
            if (moduleId == FIRST_OPEN_MODULE_ID && subModuleId == FIRST_OPEN_SUB_MODULE_ID)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START_FLAG_READY, "13088@300@1");
            }
        }
    }
}
