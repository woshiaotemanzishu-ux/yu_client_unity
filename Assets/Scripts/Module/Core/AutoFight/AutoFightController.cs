using System;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Scene.Vo;
using Shenxiao.Module.Core.Skill;
using Shenxiao.Module.Core.Tasks;

namespace Shenxiao.Module.Core.AutoFight
{
    /// <summary>
    /// Minimal field auto-fight loop.
    /// Old client starts this from task arrival / hang-up state; real target
    /// selection and 20001 attack packets stay in SceneCombat.
    /// </summary>
    public sealed class AutoFightController : BaseController
    {
        public static readonly AutoFightController Instance = new AutoFightController();

        private const int LOOP_DELAY_MS = 500;

        private CancellationTokenSource _loopCts;
        private bool _warnedNoSkill;

        private AutoFightController() { }

        protected override void Register()
        {
            EventDispatcher.On<bool>(GlobalEvent.EVT_AUTO_FIGHT_STATE, OnAutoFightState);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_SNAPSHOT_READY, OnSceneSnapshotReady);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED, OnSceneObjectsCleared);
        }

        public override void Dispose()
        {
            EventDispatcher.Off<bool>(GlobalEvent.EVT_AUTO_FIGHT_STATE, OnAutoFightState);
            EventDispatcher.Off(GlobalEvent.EVT_SCENE_SNAPSHOT_READY, OnSceneSnapshotReady);
            EventDispatcher.Off(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED, OnSceneObjectsCleared);
            StopLoop();
            base.Dispose();
        }

        private void OnAutoFightState(bool on)
        {
            if (on) StartLoop();
            else StopLoop();
        }

        private void OnSceneSnapshotReady()
        {
            if (AutoFightModel.Instance.AutoFightState) StartLoop();
        }

        private void OnSceneObjectsCleared()
        {
            SceneCombat.Instance.SetClickTarget(0);
        }

        private void StartLoop()
        {
            if (_loopCts != null) return;
            _warnedNoSkill = false;
            _loopCts = new CancellationTokenSource();
            _ = RunLoopAsync(_loopCts);
            GameLog.Info("AutoFight", "auto-fight loop started weight={0}", AutoFightModel.Instance.AutoFightWeight);
        }

        private void StopLoop()
        {
            if (_loopCts == null) return;
            // 先取局部 + 先置 null,再 Cancel/Dispose:Cancel() 会**同步**唤起 RunLoopAsync 的 finally,
            // 那里 `if (_loopCts == cts) _loopCts = null`——若不先置 null,finally 会把字段清空,回到这里
            // _loopCts.Dispose() 就对 null 触发 NRE(实测 30001 → SetAutoFightWeight → OnAutoFightState 路径崩)。
            CancellationTokenSource cts = _loopCts;
            _loopCts = null;
            cts.Cancel();
            cts.Dispose();
            SceneCombat.Instance.SetClickTarget(0);
            GameLog.Info("AutoFight", "auto-fight loop stopped");
        }

        private async Task RunLoopAsync(CancellationTokenSource cts)
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    TryAutoAttack();
                    await Task.Delay(LOOP_DELAY_MS, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal state change / scene clear shutdown.
            }
            catch (Exception e)
            {
                GameLog.Warn("AutoFight", "auto-fight loop exception: {0}", e.Message);
            }
            finally
            {
                if (_loopCts == cts)
                {
                    _loopCts = null;
                }
            }
        }

        private void TryAutoAttack()
        {
            AutoFightModel model = AutoFightModel.Instance;
            if (!model.AutoFightState || model.TempMode || !NetManager.IsConnected) return;

            SkillVo skill = SkillManager.Instance.GetNextCombatSkill();
            if (skill == null)
            {
                if (!_warnedNoSkill)
                {
                    _warnedNoSkill = true;
                    GameLog.Warn("AutoFight", "auto-fight blocked: no learned combat skill");
                }
                return;
            }

            _warnedNoSkill = false;
            if (!EnsureTaskTarget()) return;
            SceneCombat.Instance.MainRoleAttackTarget(skill.Id, SkillManager.ONLY_FIRE_ATTACK);
        }

        private static bool EnsureTaskTarget()
        {
            if (AutoFightModel.Instance.AutoFightWeight != AutoFightModel.AUTO_WEIGHT_TASK) return true;

            TaskVo task = TaskModel.Instance.MainLineTaskVo;
            if (task == null) return true;

            if (task.TaskTipsType == TaskModel.TIP_PASS_MAIN_DUNGEON)
            {
                MonsterVo autoBrushTarget = SceneCombat.Instance.GetClickTarget();
                if (autoBrushTarget != null && autoBrushTarget.CanAttack == 1 && autoBrushTarget.Hp > 0) return true;

                if (task.Id > 0 && SceneCombat.Instance.TrySetNearestMonsterByType(task.Id, task.SceneX, task.SceneY)) return true;
                return SceneCombat.Instance.TrySetNearestAttackableMonster(task.SceneX, task.SceneY);
            }

            if (task.TaskTipsType != TaskModel.TIP_KILL && task.TaskTipsType != TaskModel.TIP_ITEM) return true;

            MonsterVo current = SceneCombat.Instance.GetClickTarget();
            if (current != null && current.TypeId == task.Id) return true;

            return SceneCombat.Instance.TrySetNearestMonsterByType(task.Id, task.SceneX, task.SceneY);
        }
    }
}
