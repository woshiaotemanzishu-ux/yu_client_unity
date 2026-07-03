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

        // 跨 domain reload 状态(首跑实证:EnterPlaymode 的 reload 会吞掉 playModeStateChanged 的静态订阅,
        // RuntimeUiCaptureTool 范式在本项目配置下不成立)→ 改用 SessionState(编辑器会话内跨 reload 保留)
        // 标志「冒烟进行中」,配合 [InitializeOnLoadMethod] 在新域自动重挂钩子。
        private const string SsRunning = "PlaySmoke.Running";
        private const string SsDeadline = "PlaySmoke.Deadline";
        private const string SsWatchUntil = "PlaySmoke.WatchUntil";   // 观察模式截止(0=未启用)

        // 观察模式:五门闩全中后不立即退出,继续观察主线推进(-smokeWatchMin N 分钟),
        // 统计 30001 finished 的最大任务 id 与最后一次 DoTask 的任务 id,期满输出后退出。
        private static double _watchUntil;
        private static int _maxFinishedTask;
        private static int _lastDoTask;

        /// <summary>每次域加载自动执行:若冒烟进行中(刚经历进 Play 的 reload),在新域重挂 OnLog/OnTick。</summary>
        [InitializeOnLoadMethod]
        private static void AutoRearm()
        {
            if (!SessionState.GetBool(SsRunning, false)) return;
            _running = true;
            _deadline = SessionState.GetFloat(SsDeadline, (float)(EditorApplication.timeSinceStartup + TimeoutSeconds));
            _watchUntil = SessionState.GetFloat(SsWatchUntil, 0f);
            _nextHeartbeatAt = EditorApplication.timeSinceStartup + HeartbeatIntervalSeconds;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            EditorApplication.update -= OnTick;
            EditorApplication.update += OnTick;
            Debug.Log("CLIVERIFY PLAYSMOKE rearmed after domain reload, watching log gates ...");
        }

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

            // -smokeWatchMin N:五门闩后继续观察主线推进 N 分钟(默认 0=命中即退)。
            double watchMin = 0;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-smokeWatchMin" && double.TryParse(args[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out double m))
                    watchMin = m;
            _watchUntil = watchMin > 0 ? EditorApplication.timeSinceStartup + watchMin * 60d : 0d;
            if (watchMin > 0) _deadline = _watchUntil + 120d;      // 观察模式下总超时=观察期+余量

            SessionState.SetBool(SsRunning, true);                 // 跨 reload:新域 AutoRearm 依此重挂
            SessionState.SetFloat(SsDeadline, (float)_deadline);   // timeSinceStartup 跨 reload 连续,可存绝对值
            SessionState.SetFloat(SsWatchUntil, (float)_watchUntil);

            EditorApplication.playModeStateChanged -= OnState;
            EditorApplication.playModeStateChanged += OnState;     // 双保险(若某配置下订阅存活,亦幂等)

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

            // 观察模式:统计主线推进深度(30001 finished 的任务 id / 最近 DoTask 的任务 id)。
            if (_watchUntil > 0)
            {
                int idx = condition.IndexOf("30001 task=", StringComparison.Ordinal);
                if (idx >= 0 && condition.Contains("finished"))
                {
                    if (int.TryParse(TakeDigits(condition, idx + 11), out int tid) && tid > _maxFinishedTask)
                    {
                        _maxFinishedTask = tid;
                        Debug.Log("CLIVERIFY PLAYSMOKE mainline finished task=" + tid);
                    }
                }
                idx = condition.IndexOf("DoTask: id=", StringComparison.Ordinal);
                if (idx >= 0 && int.TryParse(TakeDigits(condition, idx + 11), out int did)) _lastDoTask = did;
            }
        }

        private static string TakeDigits(string s, int start)
        {
            int end = start;
            while (end < s.Length && char.IsDigit(s[end])) end++;
            return s.Substring(start, end - start);
        }

        private static void OnTick()
        {
            if (!_running)
            {
                return;
            }

            if (_loginOk && _enterOk && _gameStartOk && _sceneOk && _taskOk)
            {
                if (_watchUntil <= 0) { Finish(0); return; }                       // 原行为:命中即退
                if (EditorApplication.timeSinceStartup >= _watchUntil)             // 观察期满
                {
                    Debug.Log("CLIVERIFY PLAYSMOKE watch done maxFinishedTask=" + _maxFinishedTask + " lastDoTask=" + _lastDoTask);
                    Finish(0);
                    return;
                }
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
            SessionState.EraseBool(SsRunning);
            SessionState.EraseFloat(SsDeadline);
            SessionState.EraseFloat(SsWatchUntil);
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
