using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoBrush;
using Shenxiao.Module.Core.Scene.Vo;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 大妖副本 Boss 入场演出入口。老端在 boss>=3 的怪物真正加入场景后才打开
    /// DungeonFightSceneMaskView；本端只在主线大妖占位怪 7001 上接管，避免所有副本 Boss
    /// 都误播。静态布局与时序参数全部在 BossBornIntro.prefab 中维护。
    /// </summary>
    public static class BossBornEffectFlow
    {
        private const string PrefabModule = "scene";
        private const string PrefabName = "BossBornIntro";
        private const float CompletionWatchdogMarginSeconds = 0.75f;

        private static readonly HashSet<int> Shown = new HashSet<int>();
        private static GameObject _activeRoot;
        private static bool _loading;
        private static int _epoch;
        private static bool _installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (_installed) return;
            _installed = true;
            EventDispatcher.On(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED, Reset);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, Reset);
        }

        public static void NotifyMonsterAdded(MonsterVo vo)
        {
            if (vo == null || !vo.IsBoss) return;
            if (vo.TypeId != AutoBrushModel.AutoBrushMonsterId) return;
            AutoBrushBattleFlow.BindBoss(vo.InstanceId);
            if (!Shown.Add(vo.InstanceId)) return;
            _ = PlayAsync(vo);
        }

        public static void Reset()
        {
            Shown.Clear();
            _epoch++;
            _loading = false;
            ReleaseActive();
        }

        private static async Task PlayAsync(MonsterVo vo)
        {
            if (_loading || _activeRoot != null) return;
            if (!(ViewManager.GetLayer(UILayer.Top) is RectTransform topLayer))
            {
                GameLog.Warn("Scene", "大妖来袭:Top 层不可用,跳过 ins={0}", vo.InstanceId);
                AutoBrushBattleFlow.OnBossIntroUnavailable();
                return;
            }

            int epoch = ++_epoch;
            _loading = true;
            AutoBrushBattleFlow.OnBossIntroStarted();

            string key = GameResPath.GetUIPrefab(PrefabModule, PrefabName);
            GameObject root = null;
            try
            {
                root = await ResManager.InstantiateAsync(key, topLayer);
            }
            catch (Exception e)
            {
                GameLog.Warn("Scene", "大妖来袭 prefab 加载异常:key={0} error={1}", key, e.Message);
            }
            if (epoch != _epoch)
            {
                if (root != null) ResManager.ReleaseInstance(root);
                return;
            }

            _loading = false;
            if (root == null)
            {
                GameLog.Warn("Scene", "大妖来袭 prefab 加载失败:key={0};直接进入战斗,不锁死流程", key);
                AutoBrushBattleFlow.OnBossIntroUnavailable();
                return;
            }

            BossBornEffectPlayer player = root.GetComponent<BossBornEffectPlayer>();
            if (player == null)
            {
                GameLog.Error("Scene", "大妖来袭 prefab 缺少 BossBornEffectPlayer:key={0}", key);
                ResManager.ReleaseInstance(root);
                AutoBrushBattleFlow.OnBossIntroUnavailable();
                return;
            }

            _activeRoot = root;
            root.name = PrefabName;
            player.Begin(() => OnPlayerFinished(epoch));
            _ = WatchCompletionAsync(epoch, vo.InstanceId,
                Mathf.Max(1f, player.MaxPlaybackSeconds + CompletionWatchdogMarginSeconds));
            GameLog.Info("Scene", "大妖来袭:play ins={0} type={1} name=\"{2}\"", vo.InstanceId, vo.TypeId, vo.Name);
        }

        private static void OnPlayerFinished(int epoch)
        {
            if (epoch != _epoch) return;
            ReleaseActive();
            AutoBrushBattleFlow.OnBossIntroFinished();
        }

        private static async Task WatchCompletionAsync(int epoch, int instanceId, float timeoutSeconds)
        {
            try
            {
                double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
                // ReferenceEquals 可识别 Unity 已销毁但仍留在字段里的 fake-null；这种异常销毁同样
                // 必须等到超时后走 fail-open，不能被当成“正常完成”而继续冻结战斗。
                while (epoch == _epoch && !ReferenceEquals(_activeRoot, null)
                    && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    await Task.Yield();
                }

                if (epoch != _epoch || ReferenceEquals(_activeRoot, null)) return;
                GameLog.Warn("Scene",
                    "大妖来袭:播放完成回调超时,强制解锁战斗 ins={0} timeout={1:F2}s",
                    instanceId, timeoutSeconds);
                ReleaseActive();
                AutoBrushBattleFlow.OnBossIntroUnavailable();
            }
            catch (Exception e)
            {
                GameLog.Warn("Scene", "大妖来袭:完成看门狗异常 ins={0} error={1}", instanceId, e.Message);
                if (epoch != _epoch || ReferenceEquals(_activeRoot, null)) return;
                ReleaseActive();
                AutoBrushBattleFlow.OnBossIntroUnavailable();
            }
        }

        private static void ReleaseActive()
        {
            GameObject root = _activeRoot;
            _activeRoot = null;
            if (root != null) ResManager.ReleaseInstance(root);
        }
    }

}
