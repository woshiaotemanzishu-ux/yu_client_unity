using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Tasks;

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
            RegisterProtocal(Proto.AUTOBRUSH_ENTER_EXIT, On13305);
            RegisterProtocal(Proto.AUTOBRUSH_RESULT, On13306);
            RegisterProtocal(Proto.AUTOBRUSH_TOGGLE, On13307);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        /// <summary>
        /// Toggle auto-brush state. Old client sends 13307 "c" with 0=open, 1=close.
        /// </summary>
        public void RequestToggle()
        {
            RequestAutoBrushState(!AutoBrushModel.Instance.AutoBrushState);
        }

        /// <summary>
        /// Set auto-brush state. Old client sends 13307 "c" with 0=open, 1=close.
        /// </summary>
        public void RequestAutoBrushState(bool enabled)
        {
            byte type = enabled ? (byte)0 : (byte)1;
            SendFmt(Proto.AUTOBRUSH_TOGGLE, "c", type);
            GameLog.Info("AutoBrush", "request auto-brush state proto={0} enabled={1} type={2}",
                Proto.AUTOBRUSH_TOGGLE, enabled, type);
        }

        /// <summary>
        /// Enter or exit the main-line auto-brush dungeon. Old client sends 13305 "c" with 0=enter/exit request.
        /// </summary>
        public void RequestEnterOrExit(byte type = 0)
        {
            SendFmt(Proto.AUTOBRUSH_ENTER_EXIT, "c", type);
            GameLog.Info("AutoBrush", "request auto-brush dungeon proto={0} type={1}",
                Proto.AUTOBRUSH_ENTER_EXIT, type);
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
            string topRankName = "";
            int topRankLevel = 0;

            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                r.ReadU32();    // server_id
                int serverNum = (int)r.ReadU32();
                r.ReadU64();    // role_id
                string roleName = r.ReadString();
                int rank = (int)r.ReadU32();
                int rankLevel = (int)r.ReadU32();
                r.ReadU64();    // combat
                if (rank == 1)
                {
                    topRankLevel = rankLevel;
                    topRankName = rankType == 1 ? "S" + serverNum + "." + roleName : roleName;
                }
            }

            AutoBrushModel.Instance.SetRankInfo(rankType, roleRank, level, topRankName, topRankLevel);
            GameLog.Info("AutoBrush", "13301 level={0} rankType={1} roleRank={2} top={3}/{4}",
                level, rankType, roleRank, topRankName, topRankLevel);
        }

        private void On13305(NetReader r)
        {
            int code = r.ReadI32();
            if (code != 1)
            {
                GameLog.Warn("AutoBrush", "13305 enter/exit failed code={0}", code);
                return;
            }

            GameLog.Info("AutoBrush", "13305 enter/exit accepted");
        }

        private void On13306(NetReader r)
        {
            int code = r.ReadI32();
            int state = r.ReadU8();
            int coin = r.ReadI32();
            int exp = r.ReadI32();
            List<AutoBrushModel.RewardEntry> rewards = ReadResultRewards(r);

            if (code != 1)
            {
                GameLog.Warn("AutoBrush", "13306 result failed code={0} state={1}", code, state);
                return;
            }

            if (state == 0)
            {
                AutoBrushModel.Instance.SetFailureState(false);
                AutoBrushModel.Instance.SetLevel(AutoBrushModel.Instance.Level + 1);
                if (coin > 0) rewards.Add(new AutoBrushModel.RewardEntry(3, 0, coin));
                if (exp > 0) rewards.Add(new AutoBrushModel.RewardEntry(5, 0, exp));
                AutoBrushFlow.OpenResult(rewards, coin, exp);
                GameLog.Info("AutoBrush", "13306 pass success level={0} rewards={1} coin={2} exp={3}",
                    AutoBrushModel.Instance.Level, rewards.Count, coin, exp);
                return;
            }

            if (state == 1)
            {
                AutoBrushModel.Instance.SetFailureState(true, AutoBrushModel.Instance.Level + 1);
                GameLog.Warn("AutoBrush", "13306 pass failed nextLevel={0}", AutoBrushModel.Instance.LastFailureLevel);
                return;
            }

            GameLog.Warn("AutoBrush", "13306 unknown result state={0}", state);
        }

        private static List<AutoBrushModel.RewardEntry> ReadResultRewards(NetReader r)
        {
            var rewards = new List<AutoBrushModel.RewardEntry>();
            int rewardArrayCount = r.ReadU16();
            for (int i = 0; i < rewardArrayCount; i++)
            {
                r.ReadU8(); // type
                int rewardCount = r.ReadU16();
                for (int j = 0; j < rewardCount; j++)
                {
                    int style = r.ReadU8();
                    int typeId = r.ReadI32();
                    int count = r.ReadI32();
                    r.ReadU64(); // goods_id, instance id for tips in old client; display still uses style/typeId.
                    if (count > 0) rewards.Add(new AutoBrushModel.RewardEntry(style, typeId, count));
                }
            }
            return rewards;
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

            bool enabled = type == 0;
            AutoBrushModel.Instance.SetAutoBrushStrangeState(enabled);
            if (enabled && TaskModel.Instance.MainLineTaskVo?.TaskTipsType == TaskModel.TIP_PASS_MAIN_DUNGEON)
            {
                bool resumed = TaskModel.Instance.ResumeCurrentTaskAutoFight();
                GameLog.Info("AutoBrush", "13307 opened -> resume PassMainDungeon auto fight resumed={0}", resumed);
            }
        }
    }
}
