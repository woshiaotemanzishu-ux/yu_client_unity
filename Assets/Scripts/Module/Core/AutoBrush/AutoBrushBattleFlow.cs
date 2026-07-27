using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using UnityEngine;

namespace Shenxiao.Module.Core.AutoBrush
{
    /// <summary>
    /// 主线大妖副本的表现状态机。协议仍由 AutoBrushController 负责；这里统一拥有“何时允许角色移动/攻击”，
    /// 防止进入前、Boss 横幅期间和结算后残留寻路或旧目标。
    /// </summary>
    public static class AutoBrushBattleFlow
    {
        public enum Phase
        {
            Idle,
            Entering,
            Intro,
            Fighting,
            Settling,
            Exiting
        }

        public static Phase Current { get; private set; }

        /// <summary>当前大妖副本由场景快照下发的 Boss 实例 id；0 表示尚未绑定。</summary>
        public static int BossInstanceId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On(GlobalEvent.EVT_SCENE_MAP_READY, OnSceneMapReady);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, Reset);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, Reset);
        }

        public static void BeginEntering()
        {
            BossInstanceId = 0;
            SetPhase(Phase.Entering, freeze: true);
            AutoFightModel.Instance.SetAutoFightWeight(AutoFightModel.AUTO_WEIGHT_TASK);
            AutoFightController.Instance.EnsureRunning();
        }

        public static void BindBoss(int instanceId)
        {
            if (instanceId <= 0) return;
            if (BossInstanceId == instanceId) return;
            BossInstanceId = instanceId;
            GameLog.Info("AutoBrush", "battle boss bound ins={0}", instanceId);
        }

        public static void OnEnterRejected()
        {
            if (Current != Phase.Entering) return;
            SetPhase(Phase.Idle, freeze: false);
        }

        public static void OnBossIntroStarted()
        {
            SetPhase(Phase.Intro, freeze: true);
        }

        public static void OnBossIntroUnavailable()
        {
            BeginFighting("intro-unavailable");
        }

        public static void OnBossIntroFinished()
        {
            BeginFighting("intro-finished");
        }

        public static void BeginSettling()
        {
            SetPhase(Phase.Settling, freeze: true);
        }

        public static void BeginExiting()
        {
            // 退出阶段彻底撤掉本轮任务战斗权重；下一轮副本会在 BeginEntering 重新武装。
            // 这样 12005 回野外后不会用已经结算的旧任务/旧目标先跑一步。
            AutoFightModel.Instance.SetAutoFightWeight(AutoFightModel.AUTO_WEIGHT_CLOSE);
            SetPhase(Phase.Exiting, freeze: true);
        }

        public static void Reset()
        {
            BossInstanceId = 0;
            SetPhase(Phase.Idle, freeze: false);
        }

        /// <summary>优先锁定本轮已绑定实例，绑定失效时只在可攻击 Boss 集合中兜底。</summary>
        public static bool TryLockBossTarget()
        {
            if (BossInstanceId > 0 && SceneCombat.Instance.TrySetAttackableBoss(BossInstanceId)) return true;

            RoleModel role = RoleModel.Instance;
            if (!SceneCombat.Instance.TrySetNearestBoss(role.X, role.Y)) return false;
            BossInstanceId = SceneCombat.Instance.CurrentTargetId;
            return true;
        }

        private static void BeginFighting(string reason)
        {
            if (RoleModel.Instance.DunId == 0)
            {
                SetPhase(Phase.Idle, freeze: false);
                return;
            }
            if (!TryLockBossTarget())
                GameLog.Warn("AutoBrush", "battle presentation cannot lock boss before unfreeze boundIns={0}", BossInstanceId);
            SetPhase(Phase.Fighting, freeze: false);
            AutoFightModel.Instance.SetAutoFightWeight(AutoFightModel.AUTO_WEIGHT_TASK);
            AutoFightController.Instance.EnsureRunning();
            GameLog.Info("AutoBrush", "battle presentation -> Fighting reason={0}", reason);
        }

        private static void OnSceneMapReady()
        {
            if (RoleModel.Instance.DunId == 0)
            {
                if (Current != Phase.Idle) Reset();
                return;
            }

            if (Current == Phase.Entering || Current == Phase.Intro || Current == Phase.Exiting)
                FreezeActor();
        }

        private static void SetPhase(Phase next, bool freeze)
        {
            if (Current != next)
                GameLog.Info("AutoBrush", "battle presentation {0} -> {1}", Current, next);
            Current = next;
            AutoFightModel.Instance.SetCombatFreeze(freeze);
            if (freeze) FreezeActor();
        }

        private static void FreezeActor()
        {
            SceneCombat.Instance.SetClickTarget(0);
            MainRoleAgent.Current?.StopForPresentation();
        }
    }
}
