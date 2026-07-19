using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.StarEquip
{
    /// <summary>
    /// 星宿家族(星宿核心 pp_constellation_equip + 星宿锻造 pp_constellation_forge)配表读取器——
    /// 17 张表全进(config_constellation_* 16 张服务端表 + 客户端 ConfigConstellation 1 张),口径同
    /// MarriageConfigs/KfBossConfigs(JObject 原样读,数字/复合键扁平 JSON)。
    ///
    /// **所有权铁律**(主控裁决4):本类由 PK1(星宿核心)维护;PK2(星宿锻造 StarForgeController/Model)
    /// 配置读取合用本类(§2 锻造表 + 通用 JSON 小工具),但**只读不改本文件**——PK2 若需要新增读取方法,
    /// 写在自己的 StarForgeConfigs.cs 里,不得往这里加代码。
    ///
    /// 表清单(轮23 侦察 r23_starequip.md §四 实测行数,均已核对与 yu_client
    /// cdn/resource/config/server 法定同步源一致):
    ///   equip(180)/page(5)/compose(20)/decompose(2000)/pos(10)/kv(6,具名键) —— PK1(星宿核心)直接消费。
    ///   evolution(222)/evolution_pool(30)/evolution_rate(**0,空表存疑**)/enchantment(930)/
    ///   enchantment_master(55)/strength(876)/strength_buff(15)/strength_master(23)/spirit(30)/
    ///   forge_kv(10) —— 星宿锻造(PK2)专用,本类只负责加载+原始行读取,不解读业务含义。
    ///   ConfigConstellation.star_point_cfg(**12**,⚠订正:侦察稿 r23_starequip.md 误记为8条,直接实测
    ///   cdn/resource/config/client/ConfigConstellation.json 为准)—— 客户端专属星图坐标,纯 UI 用
    ///   (星宿总览页面画点位),本轮数据层 only 不消费,仅登记加载与计数。
    /// </summary>
    public static class StarEquipConfigs
    {
        // ---------------- §1 星宿核心(PK1 直接消费) ----------------

        public sealed class EquipRow
        {
            public int GoodsId;
            public long ComposeInfo;      // JSON 里是数字字符串("300"),按数值用
            public string ExtraAttr = "[]";
            public string ExtraList = "[]";
            public int Page;
            public int IsSuit;
            public long DecomposeExp;
            public string ExtraBaseAttr = "[]";
        }

        public sealed class PageRow
        {
            public int Page;
            public string Condition = "[]";
            public string NormalName = "";
            public string NormalSuitAttr = "[]";
            public string SpecialName = "";
            public string SpecialSuitAttr = "[]";
        }

        public sealed class ComposeRow
        {
            public int Id;
            public string Name = "";
            public string Condition = "[]";
            public string RegularMat = "[]";
            public string IrregularMat = "[]";
            public string Cost = "[]";
            public string Goods = "[]";
            public string FailGoods = "[]";
            public int RatioType;
            public string Ratio = "[]";
            public int BindType;
            public int TvType;
        }

        public sealed class DecomposeRow
        {
            public int Lv;
            public long Exp;
            public string Attr = "[]";
        }

        public sealed class PosRow
        {
            public int Pos;
            public string Name = "";
            public int Type;
            public string TypeName = "";
        }

        /// <summary>config_constellation_kv 具名键行(decompose_color_status/decompose_star_status/
        /// goods_for_gm/max_bag_num/open_day_limit/open_lv)。老端 h5/src 全仓未直接消费本表
        /// (仅服务端 handle/3 门槛判定 + 23208 校验用),本类仍原样登记供以后校验复刻用。</summary>
        public sealed class KvRow { public string Key = ""; public string Value = ""; public string Desc = ""; }

        // ---------------- §2 星宿锻造(PK2 消费,本类只加载+原始行,合用铁律见类注释) ----------------

        public sealed class StrengthRow { public int EquipType; public int Pos; public int Lv; public string Cost = "[]"; public string Attr = "[]"; public string SpecialAttr = "[]"; }
        public sealed class StrengthBuffRow { public int EquipType; public int Lv; public string SatisfyStatus = "[]"; public string Attr = "[]"; }
        public sealed class StrengthMasterRow { public int EquipType; public int Lv; public string SatisfyStatus = "[]"; public string Attr = "[]"; }
        public sealed class EnchantmentRow { public int EquipType; public int Pos; public int Lv; public string Cost = "[]"; public string Attr = "[]"; }
        public sealed class EnchantmentMasterRow { public int EquipType; public int Lv; public string SatisfyStatus = "[]"; public string Attr = "[]"; }
        public sealed class EvolutionRow { public int EquipType; public int Pos; public int Lv; public long EvPoint; public long Rate; public string Cost = "[]"; public string Attr = "[]"; }
        public sealed class EvolutionPoolRow { public int EquipType; public int Pos; public string AttrPool = "[]"; }
        public sealed class SpiritRow { public int EquipType; public int Pos; public string Cost = "[]"; public string Attr = "[]"; }
        /// <summary>config_constellation_forge_kv(id1-5=四子系统+进化卓越属性条数门槛,id6-9=四子系统类型码
        /// 1强化/2进化/3附魔/4启灵,id10=附魔大师特殊属性加成 attr id 列表)。</summary>
        public sealed class ForgeKvRow { public int Id; public string Value = ""; public string Desc = ""; }

        private static JObject _equip, _page, _compose, _decompose, _pos, _kv;
        private static JObject _strength, _strengthBuff, _strengthMaster, _enchantment, _enchantmentMaster,
            _evolution, _evolutionPool, _evolutionRate, _spirit, _forgeKv;
        private static JObject _starPointCfgRoot; // 客户端 ConfigConstellation.json 整表({"star_point_cfg":[...]})

        public static bool IsLoaded => _equip != null;

        public static async Task EnsureLoaded()
        {
            if (_equip != null) return;
            _equip = await LoadServer("config_constellation_equip");
            _page = await LoadServer("config_constellation_page");
            _compose = await LoadServer("config_constellation_compose");
            _decompose = await LoadServer("config_constellation_decompose");
            _pos = await LoadServer("config_constellation_pos");
            _kv = await LoadServer("config_constellation_kv");

            _strength = await LoadServer("config_constellation_strength");
            _strengthBuff = await LoadServer("config_constellation_strength_buff");
            _strengthMaster = await LoadServer("config_constellation_strength_master");
            _enchantment = await LoadServer("config_constellation_enchantment");
            _enchantmentMaster = await LoadServer("config_constellation_enchantment_master");
            _evolution = await LoadServer("config_constellation_evolution");
            _evolutionPool = await LoadServer("config_constellation_evolution_pool");
            _evolutionRate = await LoadServer("config_constellation_evolution_rate"); // ⚠源表实测 0 条,见类注释
            _spirit = await LoadServer("config_constellation_spirit");
            _forgeKv = await LoadServer("config_constellation_forge_kv");

            _starPointCfgRoot = await LoadClient("ConfigConstellation");

            GameLog.Info("StarEquip", "StarEquipConfigs 加载: equip={0} page={1} compose={2} decompose={3} pos={4} kv={5} " +
                "strength={6} strengthBuff={7} strengthMaster={8} enchantment={9} enchantmentMaster={10} " +
                "evolution={11} evolutionPool={12} evolutionRate={13} spirit={14} forgeKv={15} starPointCfg={16}",
                _equip.Count, _page.Count, _compose.Count, _decompose.Count, _pos.Count, _kv.Count,
                _strength.Count, _strengthBuff.Count, _strengthMaster.Count, _enchantment.Count, _enchantmentMaster.Count,
                _evolution.Count, _evolutionPool.Count, _evolutionRate.Count, _spirit.Count, _forgeKv.Count, StarPointCfgCount);
        }

        private static async Task<JObject> LoadServer(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("StarEquip", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        private static async Task<JObject> LoadClient(string cfg)
        {
            string key = GameResPath.GetClientConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("StarEquip", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        // ---------------- §1 访问器 ----------------

        public static EquipRow GetEquipInfo(int goodsId)
        {
            if (!(_equip?[goodsId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new EquipRow
            {
                GoodsId = goodsId,
                ComposeInfo = ReadLong(row, "compose_info"),
                ExtraAttr = ReadRaw(row, "extra_attr"),
                ExtraList = ReadRaw(row, "extra_list"),
                Page = ReadInt(row, "page"),
                IsSuit = ReadInt(row, "is_suit"),
                DecomposeExp = ReadLong(row, "decompose_exp"),
                ExtraBaseAttr = ReadRaw(row, "extra_base_attr"),
            };
        }

        public static PageRow GetPageInfo(int page)
        {
            if (!(_page?[page.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new PageRow
            {
                Page = page,
                Condition = ReadRaw(row, "condition"),
                NormalName = ReadString(row, "normal_name"),
                NormalSuitAttr = ReadRaw(row, "normal_suit_attr"),
                SpecialName = ReadString(row, "special_name"),
                SpecialSuitAttr = ReadRaw(row, "special_suit_attr"),
            };
        }

        public static ComposeRow GetComposeInfo(int composeId)
        {
            if (!(_compose?[composeId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new ComposeRow
            {
                Id = composeId,
                Name = ReadString(row, "name"),
                Condition = ReadRaw(row, "condition"),
                RegularMat = ReadRaw(row, "regular_mat"),
                IrregularMat = ReadRaw(row, "irregular_mat"),
                Cost = ReadRaw(row, "cost"),
                Goods = ReadRaw(row, "goods"),
                FailGoods = ReadRaw(row, "fail_goods"),
                RatioType = ReadInt(row, "ratio_type"),
                Ratio = ReadRaw(row, "ratio"),
                BindType = ReadInt(row, "bind_type"),
                TvType = ReadInt(row, "tv_type"),
            };
        }

        public static DecomposeRow GetDecomposeInfo(int lv)
        {
            if (!(_decompose?[lv.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new DecomposeRow { Lv = lv, Exp = ReadLong(row, "exp"), Attr = ReadRaw(row, "attr") };
        }

        public static PosRow GetPos(int pos)
        {
            if (!(_pos?[pos.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new PosRow { Pos = pos, Name = ReadString(row, "name"), Type = ReadInt(row, "type"), TypeName = ReadString(row, "type_name") };
        }

        /// <summary>config_constellation_kv 具名键取值(如 "open_lv"/"decompose_color_status")。</summary>
        public static KvRow GetKv(string key)
        {
            if (string.IsNullOrEmpty(key) || !(_kv?[key] is JObject row)) return null;
            return new KvRow { Key = key, Value = ReadRaw(row, "value"), Desc = ReadString(row, "desc") };
        }

        // ---------------- §2 星宿锻造(PK2 消费)访问器 ----------------

        public static StrengthRow GetStrength(int equipType, int pos, int lv)
        {
            string key = equipType.ToString(CultureInfo.InvariantCulture) + "@" + pos.ToString(CultureInfo.InvariantCulture) + "@" + lv.ToString(CultureInfo.InvariantCulture);
            if (!(_strength?[key] is JObject row)) return null;
            return new StrengthRow { EquipType = equipType, Pos = pos, Lv = lv, Cost = ReadRaw(row, "cost"), Attr = ReadRaw(row, "attr"), SpecialAttr = ReadRaw(row, "special_attr") };
        }

        public static StrengthBuffRow GetStrengthBuff(int equipType, int lv)
        {
            string key = equipType.ToString(CultureInfo.InvariantCulture) + "@" + lv.ToString(CultureInfo.InvariantCulture);
            if (!(_strengthBuff?[key] is JObject row)) return null;
            return new StrengthBuffRow { EquipType = equipType, Lv = lv, SatisfyStatus = ReadRaw(row, "satisfy_status"), Attr = ReadRaw(row, "attr") };
        }

        public static StrengthMasterRow GetStrengthMaster(int equipType, int lv)
        {
            string key = equipType.ToString(CultureInfo.InvariantCulture) + "@" + lv.ToString(CultureInfo.InvariantCulture);
            if (!(_strengthMaster?[key] is JObject row)) return null;
            return new StrengthMasterRow { EquipType = equipType, Lv = lv, SatisfyStatus = ReadRaw(row, "satisfy_status"), Attr = ReadRaw(row, "attr") };
        }

        public static EnchantmentRow GetEnchantment(int equipType, int pos, int lv)
        {
            string key = equipType.ToString(CultureInfo.InvariantCulture) + "@" + pos.ToString(CultureInfo.InvariantCulture) + "@" + lv.ToString(CultureInfo.InvariantCulture);
            if (!(_enchantment?[key] is JObject row)) return null;
            return new EnchantmentRow { EquipType = equipType, Pos = pos, Lv = lv, Cost = ReadRaw(row, "cost"), Attr = ReadRaw(row, "attr") };
        }

        public static EnchantmentMasterRow GetEnchantmentMaster(int equipType, int lv)
        {
            string key = equipType.ToString(CultureInfo.InvariantCulture) + "@" + lv.ToString(CultureInfo.InvariantCulture);
            if (!(_enchantmentMaster?[key] is JObject row)) return null;
            return new EnchantmentMasterRow { EquipType = equipType, Lv = lv, SatisfyStatus = ReadRaw(row, "satisfy_status"), Attr = ReadRaw(row, "attr") };
        }

        public static EvolutionRow GetEvolution(int equipType, int pos, int lv)
        {
            string key = equipType.ToString(CultureInfo.InvariantCulture) + "@" + pos.ToString(CultureInfo.InvariantCulture) + "@" + lv.ToString(CultureInfo.InvariantCulture);
            if (!(_evolution?[key] is JObject row)) return null;
            return new EvolutionRow { EquipType = equipType, Pos = pos, Lv = lv, EvPoint = ReadLong(row, "ev_point"), Rate = ReadLong(row, "rate"), Cost = ReadRaw(row, "cost"), Attr = ReadRaw(row, "attr") };
        }

        public static EvolutionPoolRow GetEvolutionPool(int equipType, int pos)
        {
            string key = equipType.ToString(CultureInfo.InvariantCulture) + "@" + pos.ToString(CultureInfo.InvariantCulture);
            if (!(_evolutionPool?[key] is JObject row)) return null;
            return new EvolutionPoolRow { EquipType = equipType, Pos = pos, AttrPool = ReadRaw(row, "attr_pool") };
        }

        /// <summary>⚠config_constellation_evolution_rate 源表实测 0 条(见 r23_starequip.md 存疑项3——
        /// 不确定是该维度数值已内联进 evolution/evolution_pool 未拆出,还是老端已弃用),本方法恒返回 null,
        /// 占位供 PK2 需要时对照 lib_constellation_forge.erl cal_addition_rate/4 的真实取值来源再实现。</summary>
        public static JObject GetEvolutionRateRaw() => _evolutionRate;

        public static SpiritRow GetSpirit(int equipType, int pos)
        {
            string key = equipType.ToString(CultureInfo.InvariantCulture) + "@" + pos.ToString(CultureInfo.InvariantCulture);
            if (!(_spirit?[key] is JObject row)) return null;
            return new SpiritRow { EquipType = equipType, Pos = pos, Cost = ReadRaw(row, "cost"), Attr = ReadRaw(row, "attr") };
        }

        public static ForgeKvRow GetForgeKv(int id)
        {
            if (!(_forgeKv?[id.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new ForgeKvRow { Id = id, Value = ReadRaw(row, "value"), Desc = ReadString(row, "desc") };
        }

        // ---------------- 计数(CliVerify 断言用) ----------------

        public static int EquipCount => _equip?.Count ?? 0;
        public static int PageCount => _page?.Count ?? 0;
        public static int ComposeCount => _compose?.Count ?? 0;
        public static int DecomposeCount => _decompose?.Count ?? 0;
        public static int PosCount => _pos?.Count ?? 0;
        public static int KvCount => _kv?.Count ?? 0;
        public static int StrengthCount => _strength?.Count ?? 0;
        public static int StrengthBuffCount => _strengthBuff?.Count ?? 0;
        public static int StrengthMasterCount => _strengthMaster?.Count ?? 0;
        public static int EnchantmentCount => _enchantment?.Count ?? 0;
        public static int EnchantmentMasterCount => _enchantmentMaster?.Count ?? 0;
        public static int EvolutionCount => _evolution?.Count ?? 0;
        public static int EvolutionPoolCount => _evolutionPool?.Count ?? 0;
        public static int EvolutionRateCount => _evolutionRate?.Count ?? 0; // 恒为 0,见类注释存疑项
        public static int SpiritCount => _spirit?.Count ?? 0;
        public static int ForgeKvCount => _forgeKv?.Count ?? 0;

        /// <summary>ConfigConstellation.star_point_cfg 数组长度(UI 专用星图坐标,本轮数据层不消费,仅计数)。</summary>
        public static int StarPointCfgCount => _starPointCfgRoot?["star_point_cfg"] is JArray arr ? arr.Count : 0;

        // ---------- JSON 读取小工具(同 MarriageConfigs/BossConfigs 套路,自成一份不跨模块耦合) ----------

        private static int ReadInt(JObject obj, string key)
        {
            if (obj == null) return 0;
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<double>();
            return int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static long ReadLong(JObject obj, string key)
        {
            if (obj == null) return 0;
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<long>();
            if (token.Type == JTokenType.Float) return (long)token.Value<double>();
            return long.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out long v) ? v : 0;
        }

        private static string ReadString(JObject obj, string key)
        {
            if (obj == null) return "";
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }

        private static string ReadRaw(JObject obj, string key) => ReadString(obj, key);
    }
}
