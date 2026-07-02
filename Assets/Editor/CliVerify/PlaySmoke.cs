using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// PlaySmoke:批处理模式真正 EnterPlaymode,连活服跑一遍登录链(HTTP登录→选服→WebSocket→10000→
    /// [0 角色时自动创角]→10004 进游戏→GAME_START 网关→12002 场景快照→30000 任务列表),全部命中即判过。
    ///
    /// domain reload 存活范式(照抄 RuntimeCapture/RuntimeUiCaptureTool.cs:55-162):
    ///   ① 所有状态是静态字段,不用闭包捕获局部变量;
    ///   ② 所有回调是静态方法,靠方法组订阅(方法组引用在 domain reload 后依然指向同一静态方法,
    ///      不依赖任何实例);
    ///   ③ EnterPlaymode 前先订阅 EditorApplication.playModeStateChanged(这行发生在 reload 之前,
    ///      订阅本身在 reload 后失效,所以 EnteredPlayMode 后要重新挂 EditorApplication.update);
    ///   ④ 真正的 tick 驱动(EditorApplication.update)只在 EnteredPlayMode 之后挂,避免 reload 期间
    ///      空转或状态错乱。
    /// PlaySmoke 命令行入口用法:-executeMethod Shenxiao.EditorTools.PlaySmoke.Run -shenxiaoPlaySmoke
    /// (-shenxiaoPlaySmoke 由 LoginBootstrap 读取,驱动登录链自动跑;Run 本身只负责进 Play + 判定 + 退出)。
    /// </summary>
    public static class PlaySmoke
    {
        private const string LaunchScenePath = "Assets/_App/Scenes/Launch.unity";
        private const double TimeoutSeconds = 600d;
        private const double HeartbeatIntervalSeconds = 30d;

        private static double _deadline;
        private static double _nextHeartbeatAt;
        private static bool _running;

        // ----- 命中标记(全部静态字段,domain reload 后由 OnLog 继续累积)-----
        private static bool _loginOk;
        private static bool _gameStartOk;
        private static bool _sceneOk;
        private static bool _enterOk;
        private static bool _taskOk;

        /// <summary>命令行入口:-executeMethod Shenxiao.EditorTools.PlaySmoke.Run</summary>
        public static void Run()
        {
            SceneAsset launchScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LaunchScenePath);
            if (launchScene == null)
            {
                Debug.LogError("CLIVERIFY PLAYSMOKE EXIT 1 reason=launch-scene-missing path=" + LaunchScenePath);
                EditorApplication.Exit(1);
                return;
            }
            EditorSceneManager.playModeStartScene = launchScene;

            ResetHits();
            _deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            _nextHeartbeatAt = EditorApplication.timeSinceStartup + HeartbeatIntervalSeconds;
            _running = true;

            EditorApplication.playModeStateChanged -= OnState;
            EditorApplication.playModeStateChanged += OnState;

            Debug.Log("CLIVERIFY PLAYSMOKE ENTER launchScene=" + LaunchScenePath + " timeoutSec=" + TimeoutSeconds.ToString("0", CultureInfo.InvariantCulture));
            EditorApplication.isPlaying = true;
        }

        private static void ResetHits()
        {
            _loginOk = false;
            _gameStartOk = false;
            _sceneOk = false;
            _enterOk = false;
            _taskOk = false;
        }

        private static void OnState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnState;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            EditorApplication.update -= OnTick;
            EditorApplication.update += OnTick;
            Debug.Log("CLIVERIFY PLAYSMOKE EnteredPlayMode, watching log gates ...");
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return;
            }

            if (!_loginOk && condition.Contains("登录链全通"))
            {
                _loginOk = true;
                Debug.Log("CLIVERIFY PLAYSMOKE gate login=OK");
            }
            if (!_gameStartOk && condition.Contains("GAME_START ready"))
            {
                _gameStartOk = true;
                Debug.Log("CLIVERIFY PLAYSMOKE gate gameStart=OK");
            }
            if (!_sceneOk && condition.Contains("12002 快照"))
            {
                _sceneOk = true;
                Debug.Log("CLIVERIFY PLAYSMOKE gate scene=OK");
            }
            if (!_enterOk && condition.Contains("进入游戏成功"))
            {
                _enterOk = true;
                Debug.Log("CLIVERIFY PLAYSMOKE gate enter=OK");
            }
            if (!_taskOk && condition.Contains("30000 tasks"))
            {
                _taskOk = true;
                Debug.Log("CLIVERIFY PLAYSMOKE gate task=OK");
            }
        }

        private static void OnTick()
        {
            if (!_running)
            {
                return;
            }

            if (_loginOk && _enterOk && _gameStartOk && _sceneOk && _taskOk)
            {
                Finish(0);
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now >= _deadline)
            {
                Debug.LogError("CLIVERIFY PLAYSMOKE TIMEOUT login=" + _loginOk + " enter=" + _enterOk +
                    " gameStart=" + _gameStartOk + " scene=" + _sceneOk + " task=" + _taskOk);
                Finish(2);
                return;
            }

            if (now >= _nextHeartbeatAt)
            {
                _nextHeartbeatAt = now + HeartbeatIntervalSeconds;
                double remaining = _deadline - now;
                Debug.Log("CLIVERIFY PLAYSMOKE progress login=" + _loginOk + " enter=" + _enterOk +
                    " gameStart=" + _gameStartOk + " scene=" + _sceneOk + " task=" + _taskOk +
                    " remainingSec=" + remaining.ToString("0", CultureInfo.InvariantCulture));
            }
        }

        private static void Finish(int code)
        {
            _running = false;
            EditorApplication.update -= OnTick;
            Application.logMessageReceived -= OnLog;
            EditorApplication.playModeStateChanged -= OnState;

            Debug.Log("CLIVERIFY PLAYSMOKE EXIT " + code +
                " login=" + _loginOk + " enter=" + _enterOk + " gameStart=" + _gameStartOk +
                " scene=" + _sceneOk + " task=" + _taskOk);

            // Play 中直接退出进程:不先退 Play,规避 ExitPlaymode 触发的二次 domain reload
            // 与随之而来的竞态(EditorApplication.Exit 会连 Play 一起终止,不需要先手动停 Play)。
            EditorApplication.Exit(code);
        }
    }
}
