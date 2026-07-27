using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
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

        private int _exitRetryCount;

        private AutoBrushController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.AUTOBRUSH_INFO, On13300);
            RegisterProtocal(Proto.AUTOBRUSH_RANK, On13301);
            RegisterProtocal(Proto.AUTOBRUSH_ENTER_EXIT, On13305);
            RegisterProtocal(Proto.AUTOBRUSH_RESULT, On13306);
            RegisterProtocal(Proto.AUTOBRUSH_TOGGLE, On13307);
            RegisterProtocal(Proto.DUNGEON_EXIT, On61002);
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
            if (type == 0 && RoleModel.Instance.DunId == 0)
            {
                if (AutoBrushBattleFlow.Current == AutoBrushBattleFlow.Phase.Entering
                    || AutoBrushBattleFlow.Current == AutoBrushBattleFlow.Phase.Intro)
                {
                    GameLog.Info("AutoBrush", "ignore duplicate enter request phase={0}", AutoBrushBattleFlow.Current);
                    return;
                }
                AutoBrushBattleFlow.BeginEntering();
            }
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
            GameLog.Info("AutoBrush", "13300 code={0} progress={1}/{2}",
                info.Code, info.CurrentTimes, info.NeedTimes);

            // 服务端在最后一只小怪计数达到 NeedTimes 时先推 13300，再延迟 1 秒执行 do_info_enter。
            // 这 1 秒已经是“等待进副本”，不能继续选下一只野外怪；否则组合演出前后会夹一段追怪动作。
            TaskVo task = TaskModel.Instance.MainLineTaskVo;
            if (info.Code == 1
                && info.CurrentTimes == info.NeedTimes
                && AutoBrushModel.Instance.AutoBrushState
                && RoleModel.Instance.DunId == 0
                && task?.TaskTipsType == TaskModel.TIP_PASS_MAIN_DUNGEON)
            {
                AutoBrushBattleFlow.BeginEntering();
                GameLog.Info("AutoBrush", "13300 progress ready -> wait authoritative dungeon enter task={0}",
                    task.TaskId);
            }
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
                AutoBrushBattleFlow.OnEnterRejected();
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

            // 主线"过副本"任务:无论通关(state==0)还是失败(state==1),服务端都不会自动把玩家踢回野外
            // (实测 13306 后无 12005 回场景);必须客户端主动发 61002 退副本(对标老端 AutoBrushResultView /
            // DungeonFailureView 关闭时 Fire 61002),否则卡在副本(dunId!=0)→ 后续跨场景任务全被服务端拒
            // (errorCode=1200001 当前场景不能进入)。
            // ★门禁按"当前是否在副本(DunId!=0)"判,而非按当前主线任务类型——对标老端 IsMainLineDungeonScene
            //   (看场景而非任务):实测玩家通关过副本任务后,主线任务已推进到找NPC,但 auto-brush 又farm了下一关
            //   并失败,此时若按任务类型 gate(已非过副本)就不会退→卡死。ExitDungeonAfterResultAsync 内部
            //   再按 DunId!=0 复判,不在副本则跳过,安全。
            if (state == 0)
            {
                _exitRetryCount = 0;
                AutoBrushBattleFlow.BeginSettling();
                AutoBrushModel.Instance.SetFailureState(false);
                AutoBrushModel.Instance.SetLevel(AutoBrushModel.Instance.Level + 1);
                if (coin > 0) rewards.Add(new AutoBrushModel.RewardEntry(3, 0, coin));
                if (exp > 0) rewards.Add(new AutoBrushModel.RewardEntry(5, 0, exp));
                AutoBrushFlow.OpenResult(rewards, coin, exp,
                    () => _ = ExitDungeonAfterResultAsync(true, 0));
                GameLog.Info("AutoBrush", "13306 pass success level={0} rewards={1} coin={2} exp={3}",
                    AutoBrushModel.Instance.Level, rewards.Count, coin, exp);
                return;
            }

            if (state == 1)
            {
                _exitRetryCount = 0;
                AutoBrushBattleFlow.BeginSettling();
                AutoBrushModel.Instance.SetFailureState(true, AutoBrushModel.Instance.Level + 1);
                GameLog.Warn("AutoBrush", "13306 pass failed nextLevel={0}", AutoBrushModel.Instance.LastFailureLevel);
                _ = ExitDungeonAfterResultAsync(false, 2000);
                return;
            }

            GameLog.Warn("AutoBrush", "13306 unknown result state={0}", state);
        }

        /// <summary>
        /// 主线过副本结算后发 61002 退出。成功分支由结果页点击/10 秒倒计时触发；失败页尚未迁移，保留 2 秒降级窗口。
        /// 退出前若已不在副本(已退/重连)则跳过。服务端收 61002 后用 12005 把玩家切回野外(dunId=0)。
        /// fire-and-forget,异常只记录。
        /// </summary>
        private async Task ExitDungeonAfterResultAsync(bool success, int delayMs)
        {
            try
            {
                if (delayMs > 0) await Shenxiao.Framework.Util.TimeUtil.Delay(delayMs);
                if (RoleModel.Instance.DunId == 0)
                {
                    GameLog.Info("AutoBrush", "auto exit dungeon skip: 已不在副本(dunId=0)");
                    return;
                }
                if (AutoBrushBattleFlow.Current == AutoBrushBattleFlow.Phase.Exiting) return;
                AutoBrushBattleFlow.BeginExiting();
                GameLog.Info("AutoBrush", "主线过副本{0} → 发 61002 退出副本(dunId={1}),等服务端 12005 切回野外",
                    success ? "通关" : "失败", RoleModel.Instance.DunId);
                SendFmt(Proto.DUNGEON_EXIT);

                // 退副本后关掉挂机(13307 "c" 1),除非当前主线任务是"过副本"且**尚未完成**(还需继续 farm 该任务下一关)。
                // 否则 auto-brush 一直 on,服务端会**无声地**把玩家再循环拉进副本(实测:退大妖副本→回野外打小怪后,
                // 被服务端拉回副本,因副本复用野外同地图→画面看着还在主场景,但野外 NPC 被 SceneManager.Clear 清掉
                // ="NPC消失";且常打不过失败又卡死)。下次真有未完成的过副本任务时 DoPassMainDungeonTask 会重发 13307"c"0 重开。
                // ★必须按 HasFinish==0 判,不能只按"任务类型!=过副本":退副本那刻过副本任务可能已 finish=1 但
                //   MainLineTaskVo 还没翻篇到下一个,只按类型判会漏关→仍被服务端再拉回(本次实测就是漏关导致再进副本)。
                TaskVo curTask = TaskModel.Instance.MainLineTaskVo;
                bool needDungeonFarm = curTask != null
                    && curTask.TaskTipsType == TaskModel.TIP_PASS_MAIN_DUNGEON
                    && curTask.HasFinish == 0;
                if (!needDungeonFarm)
                {
                    RequestAutoBrushState(false);
                    GameLog.Info("AutoBrush", "退副本后无未完成的过副本任务 → 关挂机(13307 c1),防服务端自动循环再拉回副本");
                }
            }
            catch (Exception e)
            {
                GameLog.Warn("AutoBrush", "auto exit dungeon 异常: {0}", e.Message);
            }
        }

        /// <summary>61002 退副本回包:error_code==1 成功(服务端随后推 12005 切回野外);否则记告警。</summary>
        private void On61002(NetReader r)
        {
            int errorCode = r.Remaining >= 4 ? r.ReadI32() : -1;
            if (errorCode == 1)
            {
                _exitRetryCount = 0;
                // 对标老端 BaseDungeonController 61002 成功分支:重置副本波数(curr_wave_num=1)。
                // 61002 是 AutoBrush 与 Dungeon 两条链唯一共用的出口协议(轮9 副本家族),波数状态挂 DungeonModel。
                Shenxiao.Module.Core.Dungeon.DungeonModel.Instance.ResetWaveNum();
                GameLog.Info("AutoBrush", "61002 退副本成功,等服务端 12005 切回野外");
            }
            else
            {
                GameLog.Warn("AutoBrush", "61002 退副本失败 errorCode={0}", errorCode);
                if (AutoBrushBattleFlow.Current == AutoBrushBattleFlow.Phase.Exiting && _exitRetryCount < 1)
                {
                    _exitRetryCount++;
                    _ = RetryExitDungeonAsync();
                }
                else
                {
                    AutoBrushBattleFlow.Reset();
                }
            }
        }

        private async Task RetryExitDungeonAsync()
        {
            try
            {
                await Shenxiao.Framework.Util.TimeUtil.Delay(1000);
                if (RoleModel.Instance.DunId == 0)
                {
                    AutoBrushBattleFlow.Reset();
                    return;
                }
                GameLog.Warn("AutoBrush", "61002 退副本自动重试 retry={0}", _exitRetryCount);
                SendFmt(Proto.DUNGEON_EXIT);
            }
            catch (Exception e)
            {
                AutoBrushBattleFlow.Reset();
                GameLog.Warn("AutoBrush", "61002 退副本重试异常:{0}", e.Message);
            }
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
                AutoBrushModel.BrushStrangeInfo info = AutoBrushModel.Instance.BrushInfo;
                // 对标老端 AutoBrushModel.SetAutoBrushStrangeState：只有 current_times != need_times
                // 才启动野外自动战斗。相等表示服务端会在 1 秒后自动拉入副本，此时直接进入等待态。
                if (info == null)
                {
                    GameLog.Warn("AutoBrush", "13307 opened without progress snapshot -> keep field fight stopped");
                    return;
                }

                if (info.CurrentTimes == info.NeedTimes)
                {
                    AutoBrushBattleFlow.BeginEntering();
                    GameLog.Info("AutoBrush", "13307 opened with progress ready {0}/{1} -> wait dungeon enter",
                        info.CurrentTimes, info.NeedTimes);
                    return;
                }

                bool resumed = TaskModel.Instance.ResumeCurrentTaskAutoFight();
                GameLog.Info("AutoBrush", "13307 opened with progress pending {0}/{1} -> resume field fight={2}",
                    info.CurrentTimes, info.NeedTimes, resumed);
            }
        }
    }
}
