using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>老端 ConfigInstruction 的只读解析器。</summary>
    public static class InstructionConfigs
    {
        public sealed class Section
        {
            public string Title = string.Empty;
            public readonly List<string> Lines = new List<string>();
        }

        public sealed class Entry
        {
            public string Title = string.Empty;
            public readonly List<Section> Sections = new List<Section>();
        }

        private static JObject _config;
        private static Task _loading;

        public static Task EnsureLoaded()
        {
            if (_config != null) return Task.CompletedTask;
            return _loading ?? (_loading = LoadAsync());
        }

        public static Entry Get(int id)
        {
            if (_config == null || !(_config[id.ToString()] is JArray rows) || rows.Count == 0
                || !(rows[0] is JObject row))
            {
                return null;
            }

            var result = new Entry { Title = Read(row, "view_title") };
            if (!(row["content"] is JObject content)
                || !(content["item_List"] is JArray items))
            {
                return result;
            }

            foreach (JToken token in items)
            {
                if (!(token is JObject item)) continue;
                var section = new Section { Title = Read(item, "item_title") };
                if (item["item_content"] is JArray lines)
                {
                    foreach (JToken line in lines)
                        section.Lines.Add(line?.ToString() ?? string.Empty);
                }
                result.Sections.Add(section);
            }
            return result;
        }

        private static async Task LoadAsync()
        {
            string key = GameResPath.GetClientConfigPath("ConfigInstruction");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Instruction", "说明配置缺失: {0}", key);
                _loading = null;
                return;
            }
            _config = JObject.Parse(asset.text);
            ResManager.Release(asset);
        }

        private static string Read(JObject row, string key)
            => row?[key]?.ToString() ?? string.Empty;
    }
}
