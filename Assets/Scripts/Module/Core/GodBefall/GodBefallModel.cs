using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 谪仙临凡(GodBefall,自动循环 轮18 便宜活批 PK1)配置读取器——8 张地基表,均从
    /// yu_client cdn/resource/config/server/ 原样拷入(与 MarriageConfigs/KfBossConfigs 同规格,P0 已搬运
    /// +登记 ClientConfigSync 白名单):
    ///   · config_god(10条,数字键=god_id 3001起)——神格基础表(name/skill/color/base_attr/talent),
    ///     对标老端 GodBefallModel.GetGodData(id)。
    ///   · config_god_equip(128条,数字键=goods_type_id)——神装配置(limit/pos/color/attr/composite_rule/
    ///     next_equip/decompose_exp),对标 GetEquipData(type_id)。
    ///   · config_god_lv(1010条,复合键"id@lv")——升级经验表,对标 GetLvData(id,lv)。
    ///   · config_god_stage(110条,复合键"id@stage")——升阶配置,对标 GetStageData(id,stage)。
    ///   · config_god_star(59条,复合键"id@star")——升星配置,对标 GetStarData(id,star)。
    ///   · config_god_kv(6条,字符串键,如 open_lv/update_lv_goods/forbid_scene_type)——杂项KV,对标
    ///     GetKVData(key)。⚠GodBefallDefine.OPEN_LV(=400)老端硬编码、不读此表,与本表 open_lv=400
    ///     数值巧合一致(见 GodBefallController.OPEN_LV 注释),本轮仍原样搬运该表供后续尾包按需读取。
    ///   · config_god_star_up_limit(5条,数字键=star 0-4)——升星装备品阶上限表,对标 GetStarLimitData(star)。
    ///   · config_god_stren(2404条,复合键"god_type@stren_lv")——神格强化(44017/44018)配置,对标
    ///     god_stren_item(god_type∈{3,4,5,6})。
    /// 全部字段按 MarriageConfigs 套路薄封装:JObject 原样缓存(重表如 stren/lv 不预解析成 List),
    /// attr/skill/talent/condition 等 JSON 数组字段原样透出字符串,调用方按需二次解析,数据层不预解析。
    /// </summary>
    public static class GodBefallConfigs
    {
        private static JObject _god;
        private static JObject _equip;
        private static JObject _lv;
        private static JObject _stage;
        private static JObject _star;
        private static JObject _kv;
        private static JObject _starUpLimit;
        private static JObject _stren;

        public static bool IsLoaded => _god != null;

        public static async Task EnsureLoaded()
        {
            if (_god != null) return;
            _god = await LoadServer("config_god");
            _equip = await LoadServer("config_god_equip");
            _lv = await LoadServer("config_god_lv");
            _stage = await LoadServer("config_god_stage");
            _star = await LoadServer("config_god_star");
            _kv = await LoadServer("config_god_kv");
            _starUpLimit = await LoadServer("config_god_star_up_limit");
            _stren = await LoadServer("config_god_stren");
            GameLog.Info("GodBefall", "GodBefallConfigs 加载: god={0} equip={1} lv={2} stage={3} star={4} kv={5} starUpLimit={6} stren={7}",
                _god.Count, _equip.Count, _lv.Count, _stage.Count, _star.Count, _kv.Count, _starUpLimit.Count, _stren.Count);
        }

        private static async Task<JObject> LoadServer(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("GodBefall", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>config_god 单行(数字键=god_id)。</summary>
        public sealed class GodRow
        {
            public int Id;
            public string Name = "";
            public string Skill = "[]";
            public int Color;
            public string BaseAttr = "[]";
            public string Talent = "[]";
        }

        public static GodRow GetGod(int id)
        {
            if (!(_god?[id.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new GodRow
            {
                Id = id, Name = ReadString(row, "name"), Skill = ReadRaw(row, "skill"),
                Color = ReadInt(row, "color"), BaseAttr = ReadRaw(row, "base_attr"), Talent = ReadRaw(row, "talent"),
            };
        }

        /// <summary>config_god_equip 单行(数字键=goods_type_id)。Limit/NextEquip 原表即字符串类型(如"3"/"0")。</summary>
        public sealed class EquipRow
        {
            public int Id;
            public string Limit = "";
            public int Pos;
            public int Color;
            public string Attr = "[]";
            public int CompositeRule;
            public string NextEquip = "0";
            public int DecomposeExp;
        }

        public static EquipRow GetEquip(int id)
        {
            if (!(_equip?[id.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new EquipRow
            {
                Id = id, Limit = ReadString(row, "limit"), Pos = ReadInt(row, "pos"), Color = ReadInt(row, "color"),
                Attr = ReadRaw(row, "attr"), CompositeRule = ReadInt(row, "composite_rule"),
                NextEquip = ReadString(row, "next_equip"), DecomposeExp = ReadInt(row, "decompose_exp"),
            };
        }

        /// <summary>config_god_lv 单行(复合键"id@lv")。</summary>
        public sealed class LvRow
        {
            public int Id;
            public int Lv;
            public long Exp;
            public string Attr = "[]";
            public string Doc = "";
        }

        public static LvRow GetLv(int id, int lv)
        {
            string key = id.ToString(CultureInfo.InvariantCulture) + "@" + lv.ToString(CultureInfo.InvariantCulture);
            if (!(_lv?[key] is JObject row)) return null;
            return new LvRow { Id = id, Lv = lv, Exp = ReadInt(row, "exp"), Attr = ReadRaw(row, "attr"), Doc = ReadString(row, "doc") };
        }

        /// <summary>config_god_stage 单行(复合键"id@stage")。</summary>
        public sealed class StageRow
        {
            public int Id;
            public int Stage;
            public string Condition = "[]";
            public string Attr = "[]";
            public string Doc = "";
        }

        public static StageRow GetStage(int id, int stage)
        {
            string key = id.ToString(CultureInfo.InvariantCulture) + "@" + stage.ToString(CultureInfo.InvariantCulture);
            if (!(_stage?[key] is JObject row)) return null;
            return new StageRow { Id = id, Stage = stage, Condition = ReadRaw(row, "condition"), Attr = ReadRaw(row, "attr"), Doc = ReadString(row, "doc") };
        }

        /// <summary>config_god_star 单行(复合键"id@star")。</summary>
        public sealed class StarRow
        {
            public int Id;
            public int Star;
            public string Condition = "[]";
            public string Attr = "[]";
            public string Doc = "";
        }

        public static StarRow GetStar(int id, int star)
        {
            string key = id.ToString(CultureInfo.InvariantCulture) + "@" + star.ToString(CultureInfo.InvariantCulture);
            if (!(_star?[key] is JObject row)) return null;
            return new StarRow { Id = id, Star = star, Condition = ReadRaw(row, "condition"), Attr = ReadRaw(row, "attr"), Doc = ReadString(row, "doc") };
        }

        /// <summary>config_god_kv 单行(字符串键)。对标老端 GetKVData(key)。</summary>
        public sealed class KvRow
        {
            public string Key = "";
            public string Values = "[]";
        }

        public static KvRow GetKv(string key)
        {
            if (!(_kv?[key] is JObject row)) return null;
            return new KvRow { Key = key, Values = ReadRaw(row, "values") };
        }

        /// <summary>config_god_star_up_limit 单行(数字键=star 0-4)。</summary>
        public sealed class StarUpLimitRow
        {
            public int Star;
            public int Color;
            public int EquipStar;
        }

        public static StarUpLimitRow GetStarUpLimit(int star)
        {
            if (!(_starUpLimit?[star.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new StarUpLimitRow { Star = star, Color = ReadInt(row, "color"), EquipStar = ReadInt(row, "equip_star") };
        }

        /// <summary>config_god_stren 单行(复合键"god_type@stren_lv",god_type∈{3,4,5,6})。</summary>
        public sealed class StrenRow
        {
            public int GodType;
            public int StrenLv;
            public long LvUpNeedExp;
            public string AttrAdd = "[]";
        }

        public static StrenRow GetStren(int godType, int strenLv)
        {
            string key = godType.ToString(CultureInfo.InvariantCulture) + "@" + strenLv.ToString(CultureInfo.InvariantCulture);
            if (!(_stren?[key] is JObject row)) return null;
            return new StrenRow { GodType = godType, StrenLv = strenLv, LvUpNeedExp = ReadInt(row, "lv_up_need_exp"), AttrAdd = ReadRaw(row, "attr_add") };
        }

        public static int GodCount => _god?.Count ?? 0;
        public static int EquipCount => _equip?.Count ?? 0;
        public static int LvCount => _lv?.Count ?? 0;
        public static int StageCount => _stage?.Count ?? 0;
        public static int StarCount => _star?.Count ?? 0;
        public static int KvCount => _kv?.Count ?? 0;
        public static int StarUpLimitCount => _starUpLimit?.Count ?? 0;
        public static int StrenCount => _stren?.Count ?? 0;

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

        private static string ReadString(JObject obj, string key)
        {
            if (obj == null) return "";
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }

        private static string ReadRaw(JObject obj, string key) => ReadString(obj, key);
    }

    /// <summary>
    /// 谪仙临凡(GodBefall)数据落地(自动循环 轮18 便宜活批 PK1,pt_440.erl 16 号全活)。纯数据层:
    /// GodBefallController 收到 44000/44001/44002-44006/44010-44018 解析落地后写这里,UI 尾包按需读取
    /// (同 15a/15b Boss、轮16 Marriage 先例,本轮不接 View)。
    /// </summary>
    public sealed class GodBefallModel
    {
        public static readonly GodBefallModel Instance = new GodBefallModel();
        private GodBefallModel() { }

        public sealed class EquipSlot
        {
            public int Pos;
            public long GoodsId;
        }

        /// <summary>单只神格(44000 GodList 元素 / 44001 单只推送,11 字段同构)。</summary>
        public sealed class GodEntry
        {
            public int IsBattle;
            public long GodId;
            public int Lv;
            public long Exp;
            public int Grade;
            public long Star; // pt_440.erl:Star 恒 32 位(item_to_bin_0/44005 均同),勿误当16位
            public long Power;
            public long NextLvPower;
            public long NextGradePower;
            public long NextStarPower;
            public readonly List<EquipSlot> EquipList = new List<EquipSlot>();
        }

        public sealed class SwitchCdInfo
        {
            public long SwitchCd;
            public long EndTime;
        }

        /// <summary>44017/44018 神格强化(god_type 维度,3~6,与 GodId 不同 id 空间)。</summary>
        public sealed class StrongGodEntry
        {
            public int GodType;
            public int CurrentLv;
            public long CurrentExp;
        }

        public sealed class QuickSynthesisResult
        {
            public int Code;
            public long RuleId;
            public long GoodsId;
        }

        public sealed class SmartSynthesisReward
        {
            public int GoodsType;
            public long GoodsTypeId;
            public int GoodsNum;
        }

        public sealed class PowerPreview
        {
            public long GodId;
            public long Power;
        }

        public sealed class TypeStrengthenResult
        {
            public int Code;
            public string Args = "";
            public int GodType;
            public int CurrentLv;
            public long CurrentExp;
            public int IsDivide;
        }

        private readonly Dictionary<long, GodEntry> _godDic = new Dictionary<long, GodEntry>();
        private readonly List<GodEntry> _godList = new List<GodEntry>();
        private readonly Dictionary<int, StrongGodEntry> _strongGodDic = new Dictionary<int, StrongGodEntry>();

        public IReadOnlyList<GodEntry> GodList => _godList;
        public bool HasGodList { get; private set; }
        /// <summary>当前出战/变身神格 id(44000扫描 is_battle==1 得出 / 44006成功 / 44002 老端"quirk"直写,
        /// 见 GodBefallController.On44002 注释——老端激活成功即无条件置位,不判断是否真为首只上阵神格)。
        /// B1订正:44011 全仓不写该字段(老端 on44011 成功只补发44010+释放技能提示,不碰 _cur_battle_id),
        /// 此前误当"44002·44011 双 quirk 写点"是错的,quirk 写点只有 44002 一处。</summary>
        public long CurrentBattleId { get; private set; }
        public SwitchCdInfo SwitchCd { get; private set; }
        public QuickSynthesisResult LastQuickSynthesis { get; private set; }
        public List<SmartSynthesisReward> LastSmartSynthesisRewards { get; } = new List<SmartSynthesisReward>();
        public PowerPreview LastPowerPreview { get; private set; }
        public TypeStrengthenResult LastTypeStrengthen { get; private set; }

        public GodEntry GetGod(long godId) => _godDic.TryGetValue(godId, out GodEntry e) ? e : null;
        public StrongGodEntry GetStrongGod(int godType) => _strongGodDic.TryGetValue(godType, out StrongGodEntry e) ? e : null;

        /// <summary>44000 全量落地(替换整表)。CurrentBattleId 按 is_battle==1 扫描重算(对标老端 SetGodInfo)。</summary>
        public void SetGodList(List<GodEntry> list)
        {
            _godDic.Clear();
            _godList.Clear();
            long battleId = 0;
            if (list != null)
            {
                foreach (GodEntry e in list)
                {
                    _godDic[e.GodId] = e;
                    _godList.Add(e);
                    if (e.IsBattle == 1) battleId = e.GodId;
                }
            }
            CurrentBattleId = battleId;
            HasGodList = true;
        }

        /// <summary>44001 单只推送落地:已存在则原地整条替换(对标老端 SetGodVoInDic 全字段覆盖),不存在则插入。
        /// 若该条 IsBattle==1,以服务端权威值刷新 CurrentBattleId(纠正 44002 quirk 写入的本地态;B1订正:
        /// 44011 不再写该字段,不存在需要纠正的场景)。</summary>
        public void UpsertGod(GodEntry e)
        {
            if (e == null) return;
            if (_godDic.ContainsKey(e.GodId))
            {
                int idx = _godList.FindIndex(x => x.GodId == e.GodId);
                if (idx >= 0) _godList[idx] = e; else _godList.Add(e);
            }
            else
            {
                _godList.Add(e);
            }
            _godDic[e.GodId] = e;
            if (e.IsBattle == 1) CurrentBattleId = e.GodId;
        }

        /// <summary>已存在则取原条目,不存在则新建插入(44003/44004/44005 局部字段更新兜底,正常流程下
        /// 该god早经44000/44001落地,此兜底对标老端"vo不存在"边界,理论不可达)。</summary>
        private GodEntry GetOrCreate(long godId)
        {
            if (_godDic.TryGetValue(godId, out GodEntry e)) return e;
            e = new GodEntry { GodId = godId };
            _godDic[godId] = e;
            _godList.Add(e);
            return e;
        }

        /// <summary>44003 升级成功局部更新(Lv/Exp/Power/Next*Power,不动 Grade/Star/EquipList/IsBattle)。</summary>
        public void ApplyLevelUp(long godId, int lv, long exp, long power, long nextLvPower, long nextGradePower, long nextStarPower)
        {
            GodEntry e = GetOrCreate(godId);
            e.Lv = lv; e.Exp = exp; e.Power = power;
            e.NextLvPower = nextLvPower; e.NextGradePower = nextGradePower; e.NextStarPower = nextStarPower;
        }

        /// <summary>44004 升阶成功局部更新(Grade/Power/Next*Power)。</summary>
        public void ApplyGradeUp(long godId, int grade, long power, long nextLvPower, long nextGradePower, long nextStarPower)
        {
            GodEntry e = GetOrCreate(godId);
            e.Grade = grade; e.Power = power;
            e.NextLvPower = nextLvPower; e.NextGradePower = nextGradePower; e.NextStarPower = nextStarPower;
        }

        /// <summary>44005 升星成功局部更新(Star/Power/Next*Power)。</summary>
        public void ApplyStarUp(long godId, long star, long power, long nextLvPower, long nextGradePower, long nextStarPower)
        {
            GodEntry e = GetOrCreate(godId);
            e.Star = star; e.Power = power;
            e.NextLvPower = nextLvPower; e.NextGradePower = nextGradePower; e.NextStarPower = nextStarPower;
        }

        /// <summary>44006 出战成功:整表清 IsBattle 后仅置目标 god,并同步 CurrentBattleId(对标老端 on44006
        /// 遍历 god_list 置1/清0)。</summary>
        public void SetBattle(long godId)
        {
            foreach (GodEntry e in _godList) e.IsBattle = e.GodId == godId ? 1 : 0;
            CurrentBattleId = godId;
        }

        public void SetSwitchCd(long switchCd, long endTime) => SwitchCd = new SwitchCdInfo { SwitchCd = switchCd, EndTime = endTime };

        /// <summary>44002 成功后老端直接置位当前变身/出战 id(quirk,不做首只判断),见
        /// GodBefallController.On44002 注释。B1订正:44011 不调用本方法(老端 on44011 成功不写
        /// _cur_battle_id,只补发44010+释放技能提示),仅 44002 这一处调用。</summary>
        public void SetCurrentBattleId(long godId) => CurrentBattleId = godId;

        public void SetStrongGod(int godType, int currentLv, long currentExp) =>
            _strongGodDic[godType] = new StrongGodEntry { GodType = godType, CurrentLv = currentLv, CurrentExp = currentExp };

        /// <summary>44014 恒记录(成功/失败都覆盖,"最近一次合成结果"日志语义,非持久神格状态)。</summary>
        public void SetQuickSynthesisResult(int code, long ruleId, long goodsId) =>
            LastQuickSynthesis = new QuickSynthesisResult { Code = code, RuleId = ruleId, GoodsId = goodsId };

        /// <summary>44016 仅成功时覆盖(失败沿用上次数据,对标老端失败分支不消费 goods_list)。</summary>
        public void SetSmartSynthesisRewards(List<SmartSynthesisReward> rewards)
        {
            LastSmartSynthesisRewards.Clear();
            if (rewards != null) LastSmartSynthesisRewards.AddRange(rewards);
        }

        public void SetPowerPreview(long godId, long power) => LastPowerPreview = new PowerPreview { GodId = godId, Power = power };

        /// <summary>44018 恒记录(成功/失败都覆盖,含 Args 错误详情;GodType/CurrentLv/CurrentExp 供比对,
        /// StrongGodDic 是否联动更新由调用方按 Code==1 另行决定)。</summary>
        public void SetTypeStrengthenResult(int code, string args, int godType, int currentLv, long currentExp, int isDivide) =>
            LastTypeStrengthen = new TypeStrengthenResult { Code = code, Args = args ?? "", GodType = godType, CurrentLv = currentLv, CurrentExp = currentExp, IsDivide = isDivide };

        /// <summary>断线/登出清空(ControllerHub.DisposeAll 联动,对标老端 GodBefallModel.Reset()+CleanGodInfo())。
        /// GodBefallConfigs 是进程级配表缓存,不随本方法清空(同 MarriageConfigs 惯例)。</summary>
        public void Reset()
        {
            _godDic.Clear();
            _godList.Clear();
            _strongGodDic.Clear();
            HasGodList = false;
            CurrentBattleId = 0;
            SwitchCd = null;
            LastQuickSynthesis = null;
            LastSmartSynthesisRewards.Clear();
            LastPowerPreview = null;
            LastTypeStrengthen = null;
        }
    }
}
