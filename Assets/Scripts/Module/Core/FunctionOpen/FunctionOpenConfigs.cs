using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.FunctionOpen
{
    /// <summary>
    /// config_module_open(对标老端 Config.PRELOAD_SERVER_CONFIG.config_module_open),键=功能 id。
    /// 功能预告(FunctionOpenIcon 轮播 + FunctionOpenListView 列表)的全部静态数据源:
    /// 名字 / 解锁条件 / 图标 / 3D模型(model+model_type+scale) / 文案 / 奖励 / 排序 / is_hide / jump_id。
    /// json 已拷进 Assets/GameRes/resource/config/server/config_module_open.json(打真机包前跑 Addressable 自动分组)。
    /// </summary>
    public static class FunctionOpenConfigs
    {
        public sealed class RewardVo { public int Type; public int Id; public long Num; }
        public sealed class CondVo { public string Type = ""; public long Val; } // type: task/lv/open_day/turn

        public sealed class Cfg
        {
            public int Id;
            public int SortNo;
            public string Name = "";
            public readonly List<CondVo> Conditions = new List<CondVo>();
            public string Icon = "";
            public string Model = "0";       // "0"=无3D模型(用2D图标)
            public int ModelType;            // UI_MODEL_TYPE:2坐骑/3翅膀/4武器/5法宝/7灵/9宝宝/10降神/11妖灵/20背饰
            public float ModelScale;
            public string IconText = "";     // "305级开启" 之类
            public string IconOpenText = "";
            public string Desc = "";
            public string SimpleDesc = "";
            public readonly List<RewardVo> Rewards = new List<RewardVo>();
            public int ShowLv;
            public int JumpId;
            public int IsHide;
            public int IsShow;

            public bool HasModel => !string.IsNullOrEmpty(Model) && Model != "0";
            /// <summary>解锁等级(condition 里 lv 的值);无 lv 条件返回 0。</summary>
            public long LvCond()
            {
                for (int i = 0; i < Conditions.Count; i++) if (Conditions[i].Type == "lv") return Conditions[i].Val;
                return 0;
            }
        }

        private static Dictionary<int, Cfg> _byId;
        private static Task _loading;
        public static bool Loaded => _byId != null;
        public static IReadOnlyDictionary<int, Cfg> All => _byId;

        public static Task EnsureLoaded()
        {
            if (_byId != null) return Task.CompletedTask;
            // config_module_open 走 Addressables,启动初期目录/远程未就绪时无重试的 LoadAsync 会拿 null 甚至一直不返回
            //(configfunctionicon 同款坑,见 MainUIConfigs.LoadFunctionIconWithRetryAsync)——后果是功能预告框整局
            // 静默无数据、不显示。改为:单飞 + 重试到配置真就绪。
            if (_loading == null || _loading.IsCompleted) _loading = LoadWithRetryAsync();
            return _loading;
        }

        private static async Task LoadWithRetryAsync()
        {
            TextAsset asset = null;
            for (int attempt = 0; attempt < 600 && asset == null; attempt++)
            {
                asset = await ResManager.LoadOptionalAsync<TextAsset>(GameResPath.GetServerConfigPath("config_module_open"));
                if (asset == null) await Task.Yield();
            }
            var map = new Dictionary<int, Cfg>();
            if (asset == null)
            {
                _byId = map;
                GameLog.Error("FuncOpen", "config_module_open 重试 600 帧仍加载失败(Addressables 目录不就绪或未导入),功能预告无数据");
                return;
            }
            JObject jo = JObject.Parse(asset.text);
            ResManager.Release(asset);

            foreach (KeyValuePair<string, JToken> kv in jo)
            {
                if (!(kv.Value is JObject o)) continue;
                var cfg = new Cfg
                {
                    Id = ReadInt(o, "id"),
                    SortNo = ReadInt(o, "sort_no"),
                    Name = (string)o["name"] ?? "",
                    Icon = o["icon"]?.ToString() ?? "",
                    Model = o["model"]?.ToString() ?? "0",
                    ModelType = ReadInt(o, "model_type"),
                    ModelScale = ReadFloat(o, "model_scale"),
                    IconText = (string)o["icon_text"] ?? "",
                    IconOpenText = (string)o["icon_open_text"] ?? "",
                    Desc = (string)o["desc"] ?? "",
                    SimpleDesc = (string)o["simple_desc"] ?? "",
                    ShowLv = ReadInt(o, "show_lv"),
                    JumpId = ReadInt(o, "jump_id"),
                    IsHide = ReadInt(o, "is_hide"),
                    IsShow = ReadInt(o, "is_show"),
                };
                ParsePairs(o["condition"], (a, b) => cfg.Conditions.Add(new CondVo { Type = a, Val = ToLong(b) }));
                ParseRewards(o["reward"], cfg.Rewards);
                map[cfg.Id] = cfg;
            }
            _byId = map; // 全部解析完才发布,避免并发调用拿到空/半成品 dict(那次会把图标误判成无数据)
        }

        public static Cfg Get(int id) => _byId != null && _byId.TryGetValue(id, out Cfg v) ? v : null;

        // condition: "[{\"0\":\"lv\",\"1\":150}]"(JSON 字符串)→ 回调(type, valToken)
        private static void ParsePairs(JToken raw, System.Action<string, JToken> cb)
        {
            string s = raw?.ToString();
            if (string.IsNullOrEmpty(s) || s == "[]") return;
            try
            {
                JArray arr = JArray.Parse(s);
                foreach (JToken t in arr)
                    if (t is JObject co) cb(co["0"]?.ToString() ?? "", co["1"]);
            }
            catch { }
        }

        // reward: "[{\"0\":2,\"1\":0,\"2\":10}]"(type, typeId, num)
        private static void ParseRewards(JToken raw, List<RewardVo> outList)
        {
            string s = raw?.ToString();
            if (string.IsNullOrEmpty(s) || s == "[]") return;
            try
            {
                JArray arr = JArray.Parse(s);
                foreach (JToken t in arr)
                    if (t is JObject ro)
                        outList.Add(new RewardVo { Type = (int)ToLong(ro["0"]), Id = (int)ToLong(ro["1"]), Num = ToLong(ro["2"]) });
            }
            catch { }
        }

        private static long ToLong(JToken t) => t != null && long.TryParse(t.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : 0;
        private static int ReadInt(JObject o, string k) => int.TryParse(o[k]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        private static float ReadFloat(JObject o, string k) => float.TryParse(o[k]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
    }
}
