using System;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Skill;

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
            _loopCts.Cancel();
            _loopCts.Dispose();
            _loopCts = null;
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

            SkillVo skill = SkillManager.Instance.GetNextAutoFightSkill();
            if (skill == null)
            {
                if (!_warnedNoSkill)
                {
                    _warnedNoSkill = true;
                    GameLog.Warn("AutoFight", "auto-fight blocked: no usable shortcut skill");
                }
                return;
            }

            _warnedNoSkill = false;
            SceneCombat.Instance.MainRoleAttackTarget(skill.Id, SkillManager.ONLY_FIRE_ATTACK);
        }
    }
}
