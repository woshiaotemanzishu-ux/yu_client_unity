using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainStronger
{
    /// <summary>“我要变强”只读配置；缺配置时不生成兜底推荐。</summary>
    public static class MainStrongerConfigs
    {
        public sealed class Feature
        {
            public int Id { get; internal set; }
            public int Func { get; internal set; }
            public string Name { get; internal set; } = string.Empty;
            public string RedKey { get; internal set; } = string.Empty;
            public bool RedOnly { get; internal set; }
            public bool IsTop { get; internal set; }
            public int Order { get; internal set; }
            public string TopTips { get; internal set; } = string.Empty;
        }

        public sealed class SkillAwakeTask
        {
            public int TaskId { get; internal set; }
            public long Combat { get; internal set; }
        }

        public sealed class Snapshot
        {
            public IReadOnlyDictionary<int, Feature> Features { get; internal set; }
                = new Dictionary<int, Feature>();
            public IReadOnlyDictionary<int, SkillAwakeTask> SkillAwakeTasks { get; internal set; }
                = new Dictionary<int, SkillAwakeTask>();
        }

        private static readonly IReadOnlyDictionary<int, Feature> EmptyFeatures
            = new Dictionary<int, Feature>();
        private static readonly IReadOnlyDictionary<int, SkillAwakeTask> EmptySkillTasks
            = new Dictionary<int, SkillAwakeTask>();
        private static Snapshot _snapshot;
        private static Task _loading;

        public static bool IsLoaded => _snapshot != null && _snapshot.Features.Count > 0;
        public static IReadOnlyDictionary<int, Feature> Features
            => _snapshot?.Features ?? EmptyFeatures;
        public static IReadOnlyDictionary<int, SkillAwakeTask> SkillAwakeTasks
            => _snapshot?.SkillAwakeTasks ?? EmptySkillTasks;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static Feature GetFeature(int id)
            => Features.TryGetValue(id, out Feature feature) ? feature : null;

        /// <summary>隔离测试入口；不污染运行时缓存。</summary>
        public static Snapshot ParseForValidation(string json)
            => ParseSnapshot(JObject.Parse(json));

        /// <summary>置顶优先；置顶组内 order 越大越靠前；其余按 id 升序。</summary>
        public static int CompareFeature(Feature left, Feature right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            int top = right.IsTop.CompareTo(left.IsTop);
            if (top != 0) return top;
            if (left.IsTop || right.IsTop)
            {
                int order = right.Order.CompareTo(left.Order);
                if (order != 0) return order;
            }
            return left.Id.CompareTo(right.Id);
        }

        private static async Task LoadAsync()
        {
            TextAsset asset = await ResManager.LoadOptionalAsync<TextAsset>(
                GameResPath.GetClientConfigPath("ConfigFeatureBinding"));
            if (asset == null)
            {
                GameLog.Error("MainStronger",
                    "缺少 ConfigFeatureBinding；我要变强拒绝展示伪造推荐项");
                _loading = null;
                return;
            }
            try
            {
                Snapshot parsed = ParseSnapshot(JObject.Parse(asset.text));
                if (parsed.Features.Count == 0)
                {
                    GameLog.Error("MainStronger", "ConfigFeatureBinding 无有效 FeatureBinding 行");
                    return;
                }
                _snapshot = parsed;
                GameLog.Info("MainStronger", "ConfigFeatureBinding features={0} skillTasks={1}",
                    parsed.Features.Count, parsed.SkillAwakeTasks.Count);
            }
            catch (Exception e)
            {
                GameLog.Error("MainStronger", "ConfigFeatureBinding 解析失败: {0}", e);
            }
            finally
            {
                ResManager.Release(asset);
                if (!IsLoaded) _loading = null;
            }
        }

        private static Snapshot ParseSnapshot(JObject root)
        {
            var features = new SortedDictionary<int, Feature>();
            if (root["FeatureBinding"] is JObject featureRoot)
            {
                foreach (JProperty property in featureRoot.Properties())
                {
                    if (!(property.Value is JObject value)) continue;
                    int id = ReadInt(value, "id");
                    if (id <= 0) continue;
                    features[id] = new Feature
                    {
                        Id = id,
                        Func = Math.Max(0, ReadInt(value, "func")),
                        Name = ReadString(value, "name"),
                        RedKey = ReadString(value, "red_model_type"),
                        RedOnly = ReadBool(value, "is_redot_show", true),
                        IsTop = ReadBool(value, "is_top"),
                        Order = ReadInt(value, "order"),
                        TopTips = ReadString(value, "top_tips"),
                    };
                }
            }

            var tasks = new SortedDictionary<int, SkillAwakeTask>();
            if (root["SkillAwakeTask"] is JObject taskRoot)
            {
                foreach (JProperty property in taskRoot.Properties())
                {
                    if (!(property.Value is JObject value)) continue;
                    int taskId = ReadInt(value, "taskid");
                    long combat = Math.Max(0L, ReadLong(value, "combat"));
                    if (taskId <= 0) continue;
                    tasks[taskId] = new SkillAwakeTask { TaskId = taskId, Combat = combat };
                }
            }
            return new Snapshot { Features = features, SkillAwakeTasks = tasks };
        }

        private static string ReadString(JObject value, string key)
            => value[key]?.Type == JTokenType.Null ? string.Empty : value[key]?.ToString() ?? string.Empty;

        private static int ReadInt(JObject value, string key)
        {
            JToken token = value[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            return int.TryParse(token.ToString(), out int result) ? result : 0;
        }

        private static long ReadLong(JObject value, string key)
        {
            JToken token = value[key];
            if (token == null || token.Type == JTokenType.Null) return 0L;
            if (token.Type == JTokenType.Integer) return token.Value<long>();
            return long.TryParse(token.ToString(), out long result) ? result : 0L;
        }

        private static bool ReadBool(JObject value, string key, bool fallback = false)
        {
            JToken token = value[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            if (token.Type == JTokenType.Boolean) return token.Value<bool>();
            if (token.Type == JTokenType.Integer) return token.Value<int>() != 0;
            return bool.TryParse(token.ToString(), out bool result) ? result : fallback;
        }
    }
}
