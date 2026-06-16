using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On(GlobalEvent.EVT_GAME_ENTERED, OnGameEntered);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
            EventDispatcher.On<string>(GlobalEvent.EVT_GAME_START_FLAG_READY, OnGameStartFlagReady);
        }

        private static void OnGameEntered()
        {
            ResetGameStartGate();
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

            if (_receivedStartFlags.Count < RequiredStartFlags.Length) return;

            _waitingGameStart = false;
            GameLog.Info("Game", "GAME_START ready: startup protocol gate complete");
            EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
        }

        private static void ResetGameStartGate()
        {
            _receivedStartFlags.Clear();
            _waitingGameStart = true;
        }

        private static void OnDisconnected()
        {
            _waitingGameStart = false;
            _receivedStartFlags.Clear();
            if (!ControllerHub.Initialized) return;

            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfo);
            ControllerHub.DisposeAll();
            RoleModel.Instance.Reset();
        }
    }
}
