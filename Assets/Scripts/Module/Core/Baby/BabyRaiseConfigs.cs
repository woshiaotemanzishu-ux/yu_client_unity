using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    public static class BabyRaiseConfigs
    {
        public sealed class BabyRaiseTaskCfg
        {
            public int TaskId;
            public string Desc = "";
            public int NumCon;
            public int RaiseExp;
            public int JumpId;
        }

        private static Dictionary<int, BabyRaiseTaskCfg> _byTaskId;
        private static Task _loading;

        public static bool IsLoaded => _byTaskId != null;

        public static Task EnsureLoaded()
        {
            if (_byTaskId != null) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        public static BabyRaiseTaskCfg Get(int taskId)
            => _byTaskId != null && _byTaskId.TryGetValue(taskId, out BabyRaiseTaskCfg cfg) ? cfg : null;

        private static async Task LoadAsync()
        {
            var byTaskId = new Dictionary<int, BabyRaiseTaskCfg>();
            string key = GameResPath.GetServerConfigPath("config_baby_raise");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("Baby", "missing baby raise config: {0}", key);
                _byTaskId = byTaskId;
                return;
            }

            try
            {
                JObject root = JObject.Parse(asset.text);
                foreach (KeyValuePair<string, JToken> pair in root)
                {
                    if (!(pair.Value is JObject row)) continue;
                    int taskId = ReadInt(row, "task_id");
                    if (taskId <= 0) continue;
                    byTaskId[taskId] = new BabyRaiseTaskCfg
                    {
                        TaskId = taskId,
                        Desc = row["desc"]?.ToString() ?? "",
                        NumCon = ReadInt(row, "num_con"),
                        RaiseExp = ReadInt(row, "raise_exp"),
                        JumpId = ReadInt(row, "jump_id"),
                    };
                }
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Baby", "parse baby raise config failed: {0}", e.Message);
            }
            finally
            {
                ResManager.Release(asset);
            }

            _byTaskId = byTaskId;
        }

        private static int ReadInt(JObject row, string key)
            => int.TryParse(row[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value : 0;
    }
}
