using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Tasks
{
    /// <summary>
    /// Accessor for old-client server config_task. The source table uses numeric
    /// JSON keys, so this mirrors the MainUI JObject facade until ConfigGenerator
    /// supports indexed legacy tables.
    /// </summary>
    public static class TaskConfigs
    {
        public sealed class TaskCfg
        {
            public int Id;
            public string Name;
            public string Desc;
            public string Tips;
            public int Type;
            public int Level;
            public int Sep;
            public int MainLineOrder;
            public int ShowFinish;
            public string SpecialGoodsList; // [23] 职业专属奖励(Erlang 文本 {career,type_id,count})
            public string AwardList;        // [24] 任务奖励(Erlang 文本 {a,b,type_id,count})
        }

        private static JObject _task;
        private static readonly Dictionary<int, TaskCfg> _cache = new Dictionary<int, TaskCfg>();

        public static bool IsLoaded => _task != null;

        public static async Task EnsureLoaded()
        {
            if (_task != null) return;

            string key = GameResPath.GetServerConfigPath("config_task");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Task", "missing config_task: {0}", key);
                _task = new JObject();
                return;
            }

            _task = JObject.Parse(asset.text);
            _cache.Clear();
            ResManager.Release(asset);
        }

        public static TaskCfg Get(int taskId)
        {
            if (taskId <= 0 || _task == null) return null;
            if (_cache.TryGetValue(taskId, out TaskCfg cached)) return cached;
            if (!(_task[taskId.ToString()] is JObject obj)) return null;

            TaskCfg cfg = new TaskCfg
            {
                Id = ReadInt(obj, "0"),
                Name = ReadString(obj, "1"),
                Desc = ReadString(obj, "2"),
                Tips = ReadString(obj, "3"),
                Type = ReadInt(obj, "4"),
                // local_config.config_table_default.config_task:
                // 0 id, 1 name, 2 desc, 3 tips, 4 type, ..., 23 special_goods_list,
                // 24 award_list, ..., 27 sep, 29 main_line_order, 30 show_finish.
                Level = ReadInt(obj, "7"),
                SpecialGoodsList = ReadString(obj, "23"),
                AwardList = ReadString(obj, "24"),
                Sep = ReadInt(obj, "27"),
                MainLineOrder = ReadInt(obj, "29"),
                ShowFinish = ReadInt(obj, "30"),
            };
            _cache[taskId] = cfg;
            return cfg;
        }

        private static int ReadInt(JObject obj, string key)
        {
            JToken token = obj[key];
            if (token == null) return 0;
            return token.Type == JTokenType.Integer ? token.Value<int>() : int.TryParse(token.ToString(), out int v) ? v : 0;
        }

        private static string ReadString(JObject obj, string key)
        {
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }
    }
}
