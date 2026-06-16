using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.AutoBrush
{
    /// <summary>
    /// Minimal old-client AutoBrushController slice used by MainUIAutoBrushView.
    /// Old client requests 13300/13301 on GAME_START; the broader auto-brush
    /// feature remains deferred.
    /// </summary>
    public sealed class AutoBrushController : BaseController
    {
        public static readonly AutoBrushController Instance = new AutoBrushController();

        private AutoBrushController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.AUTOBRUSH_INFO, On13300);
            RegisterProtocal(Proto.AUTOBRUSH_RANK, On13301);
            RegisterProtocal(Proto.AUTOBRUSH_TOGGLE, On13307);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            base.Dispose();
        }

        private void OnGameStart()
        {
            AutoBrushModel.Instance.ResetData();
            SendFmt(Proto.AUTOBRUSH_INFO);
            SendFmt(Proto.AUTOBRUSH_RANK);
            GameLog.Info("AutoBrush", "request auto-brush info proto={0},{1}",
                Proto.AUTOBRUSH_INFO, Proto.AUTOBRUSH_RANK);
        }

        private void On13300(NetReader r)
        {
            AutoBrushModel.Instance.SetBrushStrangeInfo(new AutoBrushModel.BrushStrangeInfo
            {
                Code = r.ReadI32(),
                CurrentTimes = r.ReadI32(),
                NeedTimes = r.ReadI32(),
                AssistId = r.ReadU64(),
                AssisterId = r.ReadU64(),
            });

            AutoBrushModel.BrushStrangeInfo info = AutoBrushModel.Instance.BrushInfo;
            GameLog.Info("AutoBrush", "13300 progress={0}/{1}", info.CurrentTimes, info.NeedTimes);
        }

        private void On13301(NetReader r)
        {
            int rankType = r.ReadU8();
            int roleRank = r.ReadI32();
            int level = r.ReadI32();

            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                r.ReadU32();    // server_id
                r.ReadU32();    // server_num
                r.ReadU64();    // role_id
                r.ReadString(); // role_name
                r.ReadU32();    // rank
                r.ReadU32();    // level
                r.ReadU64();    // combat
            }

            AutoBrushModel.Instance.SetRankInfo(rankType, roleRank, level);
            GameLog.Info("AutoBrush", "13301 level={0} rankType={1} roleRank={2}",
                level, rankType, roleRank);
        }

        private void On13307(NetReader r)
        {
            int code = r.ReadI32();
            int type = r.ReadU8();
            if (code != 1)
            {
                GameLog.Warn("AutoBrush", "13307 toggle failed code={0} type={1}", code, type);
                return;
            }

            AutoBrushModel.Instance.SetAutoBrushStrangeState(type == 0);
        }
    }
}
