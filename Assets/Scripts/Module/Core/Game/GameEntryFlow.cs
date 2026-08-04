using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Loading;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Preload;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Game
{
    /// <summary>
    /// Coordinates the enter-game handoff after 10004 succeeds.
    /// </summary>
    public static class GameEntryFlow
    {
        private static readonly string[] RequiredStartFlags =
        {
            "13001",
            "10201",
            "30005",
            "13088@300@1",
            "10202@3",
        };

        private static readonly HashSet<string> _requiredStartFlags = new HashSet<string>(RequiredStartFlags);
        private static readonly HashSet<string> _receivedStartFlags = new HashSet<string>();
        private static bool _waitingGameStart;
        private static bool _installed;
        private static int _gameStartSerial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (_installed) return;
            _installed = true;
            EventDispatcher.On(GlobalEvent.EVT_GAME_ENTERED, OnGameEntered);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
            EventDispatcher.On<string>(GlobalEvent.EVT_GAME_START_FLAG_READY, OnGameStartFlagReady);
        }

        /// <summary>
        /// Editor RunCommand harness 调用入口：[RuntimeInitializeOnLoadMethod] 在编辑器非 Play 态不自动触发，
        /// harness 需在 Pump 开始前显式调用此方法完成事件订阅。Play 态由 Install() 幂等保护，不重复订阅。
        /// </summary>
        public static void EnsureInstalled() => Install();

        private static void OnGameEntered()
        {
            ResetGameStartGate();
            LoadingManager.Show("同步角色数据");
            LoadingManager.SetProgress(0.03f, "同步角色数据 (0/5)");
            GameLog.Info("Game", "enter-game ack: init in-game controllers and wait for startup packets");
            RoleModel.Instance.Reset();
            ControllerHub.InitAll();
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfo);
            GameStartController.Instance.RequestStartupPackets();
        }

        private static void OnRoleInfo()
        {
            if (!RoleModel.Instance.HasBaseInfo) return;
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfo);

            RoleModel m = RoleModel.Instance;
            GameLog.Info("Game", "role ready: {0} Lv.{1} power={2} coin={3} gold={4} scene={5}({6},{7})",
                m.Name, m.Level, m.CombatPower, m.Coin, m.Gold, m.SceneId, m.X, m.Y);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_READY);
            EventDispatcher.Emit(GlobalEvent.EVT_GAME_START_FLAG_READY, "13001");
        }

        private static void OnGameStartFlagReady(string flag)
        {
            if (!_waitingGameStart || !_requiredStartFlags.Contains(flag)) return;
            if (!_receivedStartFlags.Add(flag)) return;

            GameLog.Info("Game", "startup flag ready: {0} ({1}/{2})",
                flag, _receivedStartFlags.Count, RequiredStartFlags.Length);
            float protocolProgress = 0.03f + 0.22f * _receivedStartFlags.Count / RequiredStartFlags.Length;
            LoadingManager.SetProgress(protocolProgress,
                $"同步角色数据 ({_receivedStartFlags.Count}/{RequiredStartFlags.Length})");

            if (_receivedStartFlags.Count < RequiredStartFlags.Length) return;

            _waitingGameStart = false;
            int serial = ++_gameStartSerial;
            _ = CompleteGameStartAsync(serial);
        }

        private static void ResetGameStartGate()
        {
            _receivedStartFlags.Clear();
            _waitingGameStart = true;
        }

        private static async Task CompleteGameStartAsync(int serial)
        {
            GameLog.Info("Game", "GAME_START gate complete: preload game resources");
            LoadingManager.SetProgress(0.25f, "游戏资源 · 检查资源");
            await LegacyPreloadService.PreloadGameStartAsync((p, hint) =>
                LoadingManager.SetProgress(0.25f + p * 0.55f, "游戏资源 · " + hint));
            if (serial != _gameStartSerial) return;

            LoadingManager.SetProgress(0.80f, "准备场景");
            GameLog.Info("Game", "GAME_START ready: startup protocol gate complete");
            EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
        }

        private static void OnDisconnected()
        {
            ++_gameStartSerial;
            _waitingGameStart = false;
            _receivedStartFlags.Clear();
            LoadingManager.Hide();
            if (!ControllerHub.Initialized) return;

            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfo);
            ControllerHub.DisposeAll();
            RoleModel.Instance.Reset();
        }
    }
}
