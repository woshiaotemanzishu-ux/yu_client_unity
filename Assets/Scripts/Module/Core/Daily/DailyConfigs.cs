using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 日常中心配置读取器(自动循环 轮10):
    ///   · config_ac.json              —— "module@module_sub@ac_sub" 键控,活动定义(时间/等级/奖励/排名权重)
    ///   · config_activity_liveness.json —— "module@module_sub" 键控,活跃度上限(max)/活跃度值(live)
    ///   · config_to_be_strong.json    —— "id" 键控,我要变强推荐条目
    ///   · config_activity_reward.json —— "id" 键控,活跃度宝箱进度奖励(15705 领取展示用)
    ///   · config_liveness_active.json —— "id" 键控,活跃度可解锁形象(UseNewImage 自动换装用)
    /// 均从 yu_client cdn/resource/config/server/ 原样拷入 Assets/GameRes/resource/config/server/
    /// (与既有 config_dungeon.json 同规格——数字/复合键 JSON,字段值本身已是合法 JSON 字符串,不需 ErlangParser)。
    /// TODO(裁决见汇报偏差栏):config_ac_kv(专属大妖 VIP 加成次数)/config_res_act(找回奖励预览,老端该分支
    /// 本就是整段注释的死代码)/config_liveness_lv(活跃度升级红点)未导入——均为次要展示分支,不影响主排序/主链路。
    /// </summary>
    public static class DailyConfigs
    {
        private static JObject _ac;
        private static JObject _activityLiveness;
        private static JObject _toBeStrong;
        private static JObject _activityReward;
        private static JObject _livenessActive;
        private static Task _loading;

        public static bool IsLoaded => _ac != null
            && _activityLiveness != null
            && _toBeStrong != null
            && _activityReward != null
            && _livenessActive != null;

#if UNITY_EDITOR
        private static System.Func<string, Task<UnityEngine.TextAsset>> s_loadAssetOverride;
        private static System.Action<UnityEngine.TextAsset> s_releaseAssetOverride;
#endif

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAllAsync();
            return _loading;
        }

        private static async Task LoadAllAsync()
        {
            // 先落局部变量，五表全部成功后才替换公开快照；失败时保留旧完整快照。
            JObject ac = await Load("config_ac");
            JObject activityLiveness = await Load("config_activity_liveness");
            JObject toBeStrong = await Load("config_to_be_strong");
            JObject activityReward = await Load("config_activity_reward");
            JObject livenessActive = await Load("config_liveness_active");

            _ac = ac;
            _activityLiveness = activityLiveness;
            _toBeStrong = toBeStrong;
            _activityReward = activityReward;
            _livenessActive = livenessActive;
            GameLog.Info("Daily", "DailyConfigs 加载: ac={0} liveness={1} strong={2} reward={3} figure={4}",
                _ac.Count, _activityLiveness.Count, _toBeStrong.Count, _activityReward.Count, _livenessActive.Count);
        }

        private static async Task<JObject> Load(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset;
#if UNITY_EDITOR
            if (s_loadAssetOverride != null)
                asset = await s_loadAssetOverride(cfg);
            else
#endif
                asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Daily", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            try
            {
                return JObject.Parse(asset.text);
            }
            finally
            {
#if UNITY_EDITOR
                if (s_releaseAssetOverride != null) s_releaseAssetOverride(asset);
                else
#endif
                    ResManager.Release(asset);
            }
        }

        public static JObject GetAc(int module, int moduleSub, int acSub)
            => _ac?[module + "@" + moduleSub + "@" + acSub] as JObject;

        public static JObject GetActivityLiveness(int module, int moduleSub)
            => _activityLiveness?[module + "@" + moduleSub] as JObject;

        public static JObject GetToBeStrong(int id) => _toBeStrong?[id.ToString()] as JObject;

        /// <summary>全部我要变强条目 id(升序;对标老端 getItemConfig 遍历 config_to_be_strong 建索引的键源)。</summary>
        public static List<int> AllToBeStrongIds()
        {
            var list = new List<int>();
            if (_toBeStrong == null) return list;
            foreach (Newtonsoft.Json.Linq.JProperty prop in _toBeStrong.Properties())
                if (int.TryParse(prop.Name, out int id)) list.Add(id);
            list.Sort();
            return list;
        }

        // ---------- JSON 读取小工具(数字索引/字符串键混排容错,同 DungeonConfigs/PartnerConfigs 套路) ----------

        public static int ReadInt(JObject obj, string key)
        {
            if (obj == null) return 0;
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<double>();
            return int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        public static string ReadString(JObject obj, string key)
        {
            if (obj == null) return "";
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }

        // ---------- config_ac 时间/日期字段解析(week/month/time/open_day/merge_day/time_region) ----------

        /// <summary>纯数值数组(week/month,如 "[2,4,6]")。</summary>
        public static List<int> ParseIntArray(string json)
        {
            var list = new List<int>();
            if (string.IsNullOrEmpty(json)) return list;
            try
            {
                if (JToken.Parse(json) is JArray arr)
                    foreach (JToken t in arr) list.Add(t.Value<int>());
            }
            catch { /* 配置异常兜底为空,不阻断排序 */ }
            return list;
        }

        /// <summary>open_day/merge_day: [{"0":startDay,"1":endDay},...]。</summary>
        public static List<(int start, int end)> ParseDayRanges(string json)
        {
            var list = new List<(int, int)>();
            if (string.IsNullOrEmpty(json)) return list;
            try
            {
                if (JToken.Parse(json) is JArray arr)
                    foreach (JToken t in arr)
                        if (t is JObject o) list.Add((ReadInt(o, "0"), ReadInt(o, "1")));
            }
            catch { }
            return list;
        }

        /// <summary>time_region: [{"0":{"0":startH,"1":startM},"1":{"0":endH,"1":endM}},...]。</summary>
        public static List<(int startH, int startM, int endH, int endM)> ParseTimeRegion(string json)
        {
            var list = new List<(int, int, int, int)>();
            if (string.IsNullOrEmpty(json)) return list;
            try
            {
                if (JToken.Parse(json) is JArray arr)
                {
                    foreach (JToken t in arr)
                    {
                        if (!(t is JObject o)) continue;
                        JObject s = o["0"] as JObject;
                        JObject e = o["1"] as JObject;
                        list.Add((ReadInt(s, "0"), ReadInt(s, "1"), ReadInt(e, "0"), ReadInt(e, "1")));
                    }
                }
            }
            catch { }
            return list;
        }

        /// <summary>具体年月日(time 字段): [{"0":year,"1":month0based,"2":day},...](对标老端 getFullYear/getMonth/getDate)。</summary>
        public static List<(int year, int month0, int day)> ParseDateList(string json)
        {
            var list = new List<(int, int, int)>();
            if (string.IsNullOrEmpty(json)) return list;
            try
            {
                if (JToken.Parse(json) is JArray arr)
                    foreach (JToken t in arr)
                        if (t is JObject o) list.Add((ReadInt(o, "0"), ReadInt(o, "1"), ReadInt(o, "2")));
            }
            catch { }
            return list;
        }

        /// <summary>reward 分级表:[{"0":levelGate,"1":[{"0":style,"1":typeId,"2":count},...]},...]。
        /// 取玩家等级达标的最后一档(对标 GetActivityRewardList 的"逐档覆盖,能达标就更新"循环);
        /// 若一档都不达标则退回首档(对标 rewardList.length!=0 时的 list=rewardList[0][1] 兜底)。</summary>
        public static List<(int style, int typeId, long count)> GetActivityRewardList(JObject acCfg, int playerLv)
        {
            var result = new List<(int, int, long)>();
            string json = ReadString(acCfg, "reward");
            if (string.IsNullOrEmpty(json)) return result;
            JArray tiers;
            try { tiers = JArray.Parse(json); } catch { return result; }
            JArray chosen = null;
            foreach (JToken tier in tiers)
            {
                if (!(tier is JObject o)) continue;
                if (playerLv >= ReadInt(o, "0")) chosen = o["1"] as JArray;
            }
            if (chosen == null && tiers.Count > 0 && tiers[0] is JObject first) chosen = first["1"] as JArray;
            AppendRewardTuples(chosen, result);
            return result;
        }

        /// <summary>宝箱奖励(config_activity_reward[id].reward 首档,对标 GetBoxRewardListById——无等级门槛)。</summary>
        public static List<(int style, int typeId, long count)> GetBoxRewardListById(int id)
        {
            var result = new List<(int, int, long)>();
            JObject cfg = _activityReward?[id.ToString()] as JObject;
            string json = ReadString(cfg, "reward");
            if (string.IsNullOrEmpty(json)) return result;
            try
            {
                JArray tiers = JArray.Parse(json);
                if (tiers.Count > 0 && tiers[0] is JObject first) AppendRewardTuples(first["1"] as JArray, result);
            }
            catch { }
            return result;
        }

        private static void AppendRewardTuples(JArray arr, List<(int style, int typeId, long count)> outList)
        {
            if (arr == null) return;
            foreach (JToken t in arr)
                if (t is JObject o) outList.Add((ReadInt(o, "0"), ReadInt(o, "1"), ReadInt(o, "2")));
        }

        // ---------- 我要变强(config_to_be_strong) ----------

        public static string GetStrongName(int id) => ReadString(GetToBeStrong(id), "name");
        public static string GetStrongDesc(int id) => ReadString(GetToBeStrong(id), "desc");
        public static int GetStrongLv(int id) => ReadInt(GetToBeStrong(id), "lv");
        public static int GetStrongStar(int id) => ReadInt(GetToBeStrong(id), "star");
        public static int GetStrongDayLimit(int id) => ReadInt(GetToBeStrong(id), "day_limit");
        public static int GetStrongJumpId(int id) => ReadInt(GetToBeStrong(id), "jump_id");
        public static int GetStrongIconId(int id) => ReadInt(GetToBeStrong(id), "icon_id");
        public static int GetStrongType(int id) => ReadInt(GetToBeStrong(id), "type");

        // ---------- 活跃度形象(config_liveness_active,15710 成功后 UseNewImage 自动换装用) ----------

        /// <summary>找出 newLv 可解锁、且不同于当前形象的最高级形象 id(对标 UseNewImage 循环:按序覆盖,
        /// 最终落地最后一个满足条件的 id;15711 服务端 DEAD,发送无害,详见 Proto.DAILY_LIVENESS_CHANGE_FIGURE)。</summary>
        public static int FindNewFigureId(int newLv, int curFigureId)
        {
            if (_livenessActive == null) return 0;
            var keys = new List<int>();
            foreach (Newtonsoft.Json.Linq.JProperty prop in _livenessActive.Properties())
                if (int.TryParse(prop.Name, out int k)) keys.Add(k);
            keys.Sort();
            int result = 0;
            foreach (int k in keys)
            {
                JObject cfg = _livenessActive[k.ToString()] as JObject;
                int lv = ReadInt(cfg, "lv");
                int cfgId = ReadInt(cfg, "id");
                if (newLv >= lv && cfgId != curFigureId) result = cfgId;
            }
            return result;
        }
    }
}
