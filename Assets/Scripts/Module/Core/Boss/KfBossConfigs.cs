using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Boss
{
    /// <summary>
    /// Boss 家族二期·跨服族(自动循环 轮15b)配置读取器——2 张地基表,均从
    /// yu_client cdn/resource/config/server/ 原样拷入(与 15a BossConfigs 同规格):
    ///   · config_eudemons_boss_cfg(48条)——千幻蜃楼/圣兽岭逐只配置(fixed_xy 固定坐标),
    ///     47000 SetBossInfo 用它算 open_lv/max_lv 等门槛(老端语义,本轮数据层不复算)。
    ///   · config_kf_great_demon(31条)——跨服"太古遗凶"专用配置,46000(type=20)/46037/46039 消费。
    /// config_decoration_boss/config_domain_kill_reward 15a 已导入(BossConfigs 持有),本类不重复。
    /// config_fairyland_open_cost 本轮未定位服务端专属 accessor(r15b 未解决项),不导入,按老端消费点决定,
    /// 留 TODO;config_decoration_boss_kv 属子视图(BossDomainBuyView 等)直读,本轮无 UI 消费方,不导入。
    /// </summary>
    public static class KfBossConfigs
    {
        private static JObject _eudemonsBossCfg;
        private static JObject _kfGreatDemon;

        public static bool IsLoaded => _eudemonsBossCfg != null;

        public static async Task EnsureLoaded()
        {
            if (_eudemonsBossCfg != null) return;
            _eudemonsBossCfg = await LoadServer("config_eudemons_boss_cfg");
            _kfGreatDemon = await LoadServer("config_kf_great_demon");
            GameLog.Info("Boss", "KfBossConfigs 加载: eudemonsBossCfg={0} kfGreatDemon={1}",
                _eudemonsBossCfg.Count, _kfGreatDemon.Count);
        }

        private static async Task<JObject> LoadServer(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Boss", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>config_eudemons_boss_cfg 单行(千幻蜃楼/圣兽岭 boss 实例)。</summary>
        public sealed class EudemonsBossCfgRow
        {
            public int BossId;
            public int Type;
            public int Scene;
            public string Condition = "[]"; // [["lv",N]] 等级门槛,原串留给调用方解析
        }

        public static EudemonsBossCfgRow GetEudemonsBossCfg(int bossId)
        {
            if (!(_eudemonsBossCfg?[bossId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new EudemonsBossCfgRow
            {
                BossId = bossId,
                Type = BossConfigs.ReadInt(row, "type"),
                Scene = BossConfigs.ReadInt(row, "scene"),
                Condition = BossConfigs.ReadRaw(row, "condition"),
            };
        }

        /// <summary>config_kf_great_demon 单行(跨服太古遗凶 boss 实例)。</summary>
        public sealed class KfGreatDemonCfgRow
        {
            public int BossId;
            public int MonType; // 0=普通怪 1=特殊大妖 2=普通宝箱 3=高级宝箱
            public int Scene;
        }

        public static KfGreatDemonCfgRow GetKfGreatDemonCfg(int bossId)
        {
            if (!(_kfGreatDemon?[bossId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new KfGreatDemonCfgRow
            {
                BossId = bossId,
                MonType = BossConfigs.ReadInt(row, "mon_type"),
                Scene = BossConfigs.ReadInt(row, "scene"),
            };
        }

        public static int EudemonsBossCfgCount => _eudemonsBossCfg?.Count ?? 0;
        public static int KfGreatDemonCount => _kfGreatDemon?.Count ?? 0;
    }
}
