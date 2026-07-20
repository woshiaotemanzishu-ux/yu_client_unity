using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.OutWard
{
    /// <summary>
    /// 幻化外观数据层(对标老端 commonModel/OutWardModel.ts,协议段 pt_160)。坐骑/同修/翼影/圣器/神兵统一走本框架,
    /// 按 type_id 参数化(1=坐骑 2=剑魄同修 3/4/5=翼影/圣器/神兵)。主线链序:
    ///   100330(ctype23 id=1 need=2):坐骑(type_id=1) Stage&gt;1 或 (Stage==1 且 Star&gt;=2)
    ///   100521(ctype90 id=2 need=2):同修(type_id=2)系统B等级&gt;=2
    ///   100901(ctype90 id=1 need=2):坐骑(type_id=1)系统B等级&gt;=2
    /// 同一 type_id 身上两套并存的养成线(系统A阶星/系统B等级),字段全落一个 OutWardVo。
    /// </summary>
    public sealed class OutWardModel
    {
        public static readonly OutWardModel Instance = new OutWardModel();
        private OutWardModel() { }

        /// <summary>外观对象实例(系统A 阶/星/祝福 + 系统B 等级/经验/战力,字段名对照 Proto.cs OUTWARD_* 注释)。</summary>
        public sealed class OutWardVo
        {
            public int TypeId;

            // ---- 系统A:阶/星(16002/16023) ----
            public int Stage;
            public int Star;
            public long Blessing;
            public int FigureStage;
            public long Combat;
            public long Etime;
            public int AutoBuy;
            public List<(int attrId, long val)> Attrs;   // 16002 attr_list
            public List<int> Skills;                      // 16002 skill_list(skill_id)

            // ---- 系统B:等级/经验(16028/16029) ----
            public bool HasLv;
            public int Level;
            public long CurExp;
            public long LvCombat;
            public List<(int attrId, long val)> LvAttrs;              // 16028 attr_list
            public List<(int skillId, int skillLevel)> LvSkills;      // 16028 skill_list
        }

        private readonly Dictionary<int, OutWardVo> _map = new Dictionary<int, OutWardVo>();

        public OutWardVo Get(int typeId)
        {
            return _map.TryGetValue(typeId, out OutWardVo vo) ? vo : null;
        }

        private OutWardVo GetOrCreate(int typeId)
        {
            if (!_map.TryGetValue(typeId, out OutWardVo vo))
            {
                vo = new OutWardVo { TypeId = typeId };
                _map[typeId] = vo;
            }
            return vo;
        }

        /// <summary>16002 回包套值(系统A阶星全字段 + 附属 attr/skill)。</summary>
        public void Apply16002(int typeId, int stage, int star, long blessing, int figureStage, long combat,
            long etime, int autoBuy, List<(int attrId, long val)> attrs, List<int> skills)
        {
            OutWardVo vo = GetOrCreate(typeId);
            vo.Stage = stage;
            vo.Star = star;
            vo.Blessing = blessing;
            vo.FigureStage = figureStage;
            vo.Combat = combat;
            vo.Etime = etime;
            vo.AutoBuy = autoBuy;
            vo.Attrs = attrs;
            vo.Skills = skills;
        }

        /// <summary>16023 升星成功套值(errcode==1 时调用;blessing_plus/ratio_list 由控制器读完,本层只落 stage/star/blessing)。</summary>
        public void Apply16023(int typeId, int stage, int star, long blessing, long etime, int autoBuy)
        {
            OutWardVo vo = GetOrCreate(typeId);
            vo.Stage = stage;
            vo.Star = star;
            vo.Blessing = blessing;
            vo.Etime = etime;
            vo.AutoBuy = autoBuy;
        }

        /// <summary>16005 通用升星成功套值(≈Apply16023 少 etime/auto_buy 两字段;3翼影/4圣器/5神兵无系统B等级线)。
        /// 薄增量六件套第20轮。</summary>
        public void Apply16005(int typeId, int stage, int star, long blessing)
        {
            OutWardVo vo = GetOrCreate(typeId);
            vo.Stage = stage;
            vo.Star = star;
            vo.Blessing = blessing;
        }

        /// <summary>16028 面板回包套值(系统B全字段)。</summary>
        public void Apply16028(int typeId, int level, long curExp, long combat,
            List<(int attrId, long val)> attrs, List<(int skillId, int skillLevel)> skills)
        {
            OutWardVo vo = GetOrCreate(typeId);
            vo.HasLv = true;
            vo.Level = level;
            vo.CurExp = curExp;
            vo.LvCombat = combat;
            vo.LvAttrs = attrs;
            vo.LvSkills = skills;
        }

        /// <summary>16029 升级成功套值(errcode==1 时调用;add_exp/ratio_list 由控制器读完,本层只落 level/cur_exp/combat)。</summary>
        public void Apply16029(int typeId, int level, long curExp, long combat, List<(int skillId, int skillLevel)> skills)
        {
            OutWardVo vo = GetOrCreate(typeId);
            vo.HasLv = true;
            vo.Level = level;
            vo.CurExp = curExp;
            vo.LvCombat = combat;
            vo.LvSkills = skills;
        }

        /// <summary>16030 系统B技能升级成功套值(errcode==1 时调用;在 LvSkills 里原地更新对应 skill_id 的等级,
        /// 找不到则忽略——对标老端 On16030 的 for-in 就地改写惯例)。第21轮补齐。</summary>
        public void Apply16030(int typeId, int skillId, int level)
        {
            OutWardVo vo = GetOrCreate(typeId);
            if (vo.LvSkills == null) return;
            for (int i = 0; i < vo.LvSkills.Count; i++)
            {
                if (vo.LvSkills[i].skillId != skillId) continue;
                vo.LvSkills[i] = (skillId, level);
                break;
            }
        }

        /// <summary>16024 自动购买开关套值(对标老端 On16024:仅在 vo 已存在时套值,不 GetOrCreate;
        /// 老端不判 errcode 直接套,因为服务端 change_auto_buy 恒发 ?SUCCESS——照抄不加判断)。</summary>
        public void Apply16024(int typeId, int autoBuy)
        {
            OutWardVo vo = Get(typeId);
            if (vo != null) vo.AutoBuy = autoBuy;
        }

        /// <summary>16003 幻化穿戴/取消成功套值(type==1 基础形象:回填系统A的 FigureStage,清空幻化穿戴 id;
        /// type==2 幻化形象:系统A的 FigureStage 清 0,幻化穿戴 id=args。对标老端
        /// OutWardBaseModel.UpdateOutWardFigure:297-319)。</summary>
        public void ApplyIllusionWear(int typeId, int type, long args)
        {
            OutWardVo vo = Get(typeId);
            IllusionListVo illu = _illusionMap.TryGetValue(typeId, out IllusionListVo v) ? v : null;
            if (type == 1)
            {
                if (vo != null) vo.FigureStage = (int)args;
                if (illu != null) illu.IllusionId = 0;
            }
            else if (type == 2)
            {
                if (vo != null) vo.FigureStage = 0;
                if (illu != null) illu.IllusionId = (int)args;
            }
        }

        public void Clear()
        {
            _map.Clear();
            _illusionMap.Clear();
            _figureDetailMap.Clear();
            _crystalMap.Clear();
            LastFightPreview = default;
            LastStarFightPreview = default;
        }

        // =================================================================================
        // 幻化(Illusion,轮24 PI 增量):16006 形象列表 / 16007 形象详情缓存 / 16011 魔晶次数 /
        // 16020 升星原地 patch / 16012 到期删除 / 16022+16027 瞬时战力预览。
        // 对标老端 OutWardBaseModel.ts 的 outward_illu_data_list / outward_figure_list / active_illu_list。
        // =================================================================================

        /// <summary>幻化形象简报(16006 figure_list 条目;16020 升星成功后原地 patch 这里的 Star)。</summary>
        public sealed class FigureBriefVo
        {
            public int Id;
            public int Stage;
            public int Star;
            public long EndTime;
        }

        /// <summary>幻化形象列表(16006 回包整体)。IllusionId=当前穿戴的 figure id,0=未穿戴/仅基础形象。</summary>
        public sealed class IllusionListVo
        {
            public int TypeId;
            public int IllusionId;
            public int ColorId;
            public List<FigureBriefVo> FigureList;
        }

        /// <summary>幻化形象详情缓存(16007 回包全字段,对标老端 outward_figure_list[type_id][id])。</summary>
        public sealed class FigureDetailVo
        {
            public int TypeId;
            public int Id;
            public int Stage;
            public int Star;
            public long Blessing;
            public long Combat;
            public long StarCombat;
            public long EndTime;
            public List<(int attrId, long val)> Attrs;
            public List<int> Skills;                                  // 16007 skill_list 仅 id,无 level
            public List<(int colorId, long colorLv)> ColorList;
            public long NextStarPower;
        }

        /// <summary>幻化战力预览瞬时值(16022/16027 回包无 errcode 包装,老端不落任何列表只经事件传参;
        /// 本层保留最近一次供事件消费方按需读取,对标老端 REAL_FIGHT/UPDATE_STAR_FIGHT 事件参数)。</summary>
        public struct FightPreviewVo
        {
            public int TypeId;
            public int FigureId;
            public long Power;
            public long StarCombat;       // 16027 无此字段,固定 0
            public long NextStarPower;
        }

        private readonly Dictionary<int, IllusionListVo> _illusionMap = new Dictionary<int, IllusionListVo>();
        private readonly Dictionary<int, Dictionary<int, FigureDetailVo>> _figureDetailMap = new Dictionary<int, Dictionary<int, FigureDetailVo>>();
        private readonly Dictionary<int, List<(int goodsId, int times, int timesLim)>> _crystalMap = new Dictionary<int, List<(int, int, int)>>();

        public FightPreviewVo LastFightPreview;
        public FightPreviewVo LastStarFightPreview;

        public IllusionListVo GetIllusionList(int typeId)
        {
            return _illusionMap.TryGetValue(typeId, out IllusionListVo vo) ? vo : null;
        }

        public FigureDetailVo GetFigureDetail(int typeId, int figureId)
        {
            return _figureDetailMap.TryGetValue(typeId, out Dictionary<int, FigureDetailVo> m) && m.TryGetValue(figureId, out FigureDetailVo d) ? d : null;
        }

        public IReadOnlyList<(int goodsId, int times, int timesLim)> GetCrystalCounters(int typeId)
        {
            return _crystalMap.TryGetValue(typeId, out List<(int, int, int)> list) ? list : null;
        }

        /// <summary>16006 回包套值(整表替换,对标老端 outward_illu_data_list[type_id] = data)。</summary>
        public void Apply16006(int typeId, int illusionId, int colorId, List<FigureBriefVo> figureList)
        {
            _illusionMap[typeId] = new IllusionListVo { TypeId = typeId, IllusionId = illusionId, ColorId = colorId, FigureList = figureList };
        }

        /// <summary>16007 回包套值(按 type_id+id 二级索引缓存,对标老端 outward_figure_list[type_id][id] = data)。</summary>
        public void Apply16007(FigureDetailVo detail)
        {
            if (!_figureDetailMap.TryGetValue(detail.TypeId, out Dictionary<int, FigureDetailVo> m))
            {
                m = new Dictionary<int, FigureDetailVo>();
                _figureDetailMap[detail.TypeId] = m;
            }
            m[detail.Id] = detail;
        }

        /// <summary>16011 回包套值(整表替换,对标老端 outward_crystal_data_[type_id] = counter_list)。</summary>
        public void Apply16011(int typeId, List<(int goodsId, int times, int timesLim)> counters)
        {
            _crystalMap[typeId] = counters;
        }

        /// <summary>16020 升星成功原地 patch 缓存 figure_list 里对应 id 的 Star(对标老端 On16020 的 for 就地
        /// 改写,不整表重建;找不到该 id 静默忽略——调用方随后还会再发 16006/16007 全量兜底刷新)。</summary>
        public void PatchIllusionStar(int typeId, int figureId, int star)
        {
            if (!_illusionMap.TryGetValue(typeId, out IllusionListVo vo) || vo.FigureList == null) return;
            foreach (FigureBriefVo f in vo.FigureList)
            {
                if (f.Id != figureId) continue;
                f.Star = star;
                break;
            }
        }

        /// <summary>16012 到期删除套值(从 figure_list 与详情缓存里一并摘除,对标老端 On16012 双删)。</summary>
        public void Apply16012(int typeId, int figureId)
        {
            if (_illusionMap.TryGetValue(typeId, out IllusionListVo vo) && vo.FigureList != null)
            {
                vo.FigureList.RemoveAll(f => f.Id == figureId);
            }
            if (_figureDetailMap.TryGetValue(typeId, out Dictionary<int, FigureDetailVo> m))
            {
                m.Remove(figureId);
            }
        }

        /// <summary>16022 战力预览套值(瞬时,不做二级索引缓存)。</summary>
        public void ApplyFightPreview(int typeId, int figureId, long power, long starCombat, long nextStarPower)
        {
            LastFightPreview = new FightPreviewVo { TypeId = typeId, FigureId = figureId, Power = power, StarCombat = starCombat, NextStarPower = nextStarPower };
        }

        /// <summary>16027 升星战力预览套值(瞬时,不做二级索引缓存;无 StarCombat 字段,固定 0)。</summary>
        public void ApplyStarFightPreview(int typeId, int figureId, long power, long nextStarPower)
        {
            LastStarFightPreview = new FightPreviewVo { TypeId = typeId, FigureId = figureId, Power = power, StarCombat = 0, NextStarPower = nextStarPower };
        }
    }

    /// <summary>
    /// config_mount_star 读取器(⚠数字键,列序 config_table_default:0=type_id/1=stage/2=star/3=max_blessing/
    /// 4=attr/5=combat/6=clear_status;主键 "{type_id}@{stage}@{star}")。表经 ClientConfigSync 从 yu_client cdn 同步。
    /// 本轮最小闭环只需 max_blessing(显示"祝福 X/Y")。
    /// </summary>
    public static class OutWardConfigs
    {
        private static JObject _mountStar;
        private static JObject _mountStage;
        private static JObject _mountGoods;
        private static JObject _mountFigure;        // 轮24 PI:幻化"可激活形象"列表(主键 "type_id@id@career")
        private static JObject _mountFigureStage;   // 轮24 PI:幻化升阶(主键 "type_id@id@stage")
        private static JObject _mountFigureStar;    // 轮24 PI:幻化升星(主键 "type_id@id@star",老端 upStarCfg)
        private static JObject _mountSkill;         // 轮24 PI:幻化技能(主键 "type_id@skill_id")
        private static readonly Dictionary<int, List<int>> _trainGoodsByType = new Dictionary<int, List<int>>();

        public static bool IsLoaded => _mountStar != null;
        /// <summary>幻化 4 张专属表是否已加载(独立于养成线 3 张,供 CliVerify/未来 UI 单独判定)。</summary>
        public static bool IsIllusionConfigLoaded => _mountFigure != null;

        public static async Task EnsureLoaded()
        {
            if (_mountStar == null)
            {
                string key = GameResPath.GetServerConfigPath("config_mount_star");
                UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
                if (asset == null)
                {
                    GameLog.Error("OutWard", "missing config_mount_star: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                    _mountStar = new JObject();
                }
                else
                {
                    _mountStar = JObject.Parse(asset.text);
                    ResManager.Release(asset);
                    GameLog.Info("OutWard", "config_mount_star={0}", _mountStar.Count);
                }
            }
            if (_mountStage == null)
            {
                string key = GameResPath.GetServerConfigPath("config_mount_stage");
                UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
                if (asset == null)
                {
                    GameLog.Error("OutWard", "missing config_mount_stage: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                    _mountStage = new JObject();
                }
                else
                {
                    _mountStage = JObject.Parse(asset.text);
                    ResManager.Release(asset);
                    GameLog.Info("OutWard", "config_mount_stage={0}", _mountStage.Count);
                }
            }
            if (_mountGoods == null)
            {
                string key = GameResPath.GetServerConfigPath("config_mount_goods");
                UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
                if (asset == null)
                {
                    GameLog.Error("OutWard", "missing config_mount_goods: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                    _mountGoods = new JObject();
                }
                else
                {
                    _mountGoods = JObject.Parse(asset.text);
                    ResManager.Release(asset);
                    GameLog.Info("OutWard", "config_mount_goods={0}", _mountGoods.Count);
                }
                _trainGoodsByType.Clear();
            }
            if (_mountFigure == null)
            {
                _mountFigure = await LoadServerConfig("config_mount_figure");
            }
            if (_mountFigureStage == null)
            {
                _mountFigureStage = await LoadServerConfig("config_mount_figure_stage");
            }
            if (_mountFigureStar == null)
            {
                _mountFigureStar = await LoadServerConfig("config_mount_figure_star");
            }
            if (_mountSkill == null)
            {
                _mountSkill = await LoadServerConfig("config_mount_skill");
            }
        }

        /// <summary>server 配表通用加载(JObject,具名键;缺表落空表+报错,不抛异常阻断流程)。</summary>
        private static async Task<JObject> LoadServerConfig(string name)
        {
            string key = GameResPath.GetServerConfigPath(name);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("OutWard", "missing {0}: {1}(未同步?跑 神霄/配表/同步客户端配置)", name, key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("OutWard", "{0}={1}", name, obj.Count);
            return obj;
        }

        /// <summary>某培养对象的培养材料物品 id 列表(config_mount_goods 键 "type_id@goods_id",按 goods_id 升序;缺表=空)。</summary>
        public static IReadOnlyList<int> GetTrainGoodsIds(int typeId)
        {
            if (_trainGoodsByType.TryGetValue(typeId, out List<int> cached)) return cached;
            var list = new List<int>();
            if (_mountGoods != null)
            {
                string prefix = typeId + "@";
                foreach (KeyValuePair<string, JToken> kv in _mountGoods)
                {
                    if (!kv.Key.StartsWith(prefix)) continue;
                    if (int.TryParse(kv.Key.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int goodsId))
                    {
                        list.Add(goodsId);
                    }
                }
                list.Sort();
            }
            _trainGoodsByType[typeId] = list;
            return list;
        }

        /// <summary>阶名(config_mount_stage["type@stage@career"].name;缺表/缺项降级 "")。</summary>
        public static string GetStageName(int typeId, int stage, int career)
        {
            JObject obj = GetStageObj(typeId, stage, career);
            return obj?.Value<string>("name") ?? "";
        }

        /// <summary>本阶满星数(config_mount_stage["type@stage@career"].max_star;缺表/缺项降级 0)。</summary>
        public static int GetMaxStar(int typeId, int stage, int career)
        {
            JObject obj = GetStageObj(typeId, stage, career);
            return obj == null ? 0 : (int)ReadLong(obj, "max_star");
        }

        private static JObject GetStageObj(int typeId, int stage, int career)
        {
            if (_mountStage == null) return null;
            // 表按职业细分;缺当前职业条目回退职业 1(同名不同 figure,显示用名字/星数一致)。
            return _mountStage[typeId + "@" + stage + "@" + career] as JObject
                ?? _mountStage[typeId + "@" + stage + "@1"] as JObject;
        }

        /// <summary>祝福上限(config_mount_star["type@stage@star"]["3"]);缺表/缺项降级 0(标出而非臆造)。</summary>
        public static long GetMaxBlessing(int typeId, int stage, int star)
        {
            if (_mountStar == null) return 0;
            string key = typeId + "@" + stage + "@" + star;
            if (!(_mountStar[key] is JObject obj)) return 0;
            return ReadLong(obj, "3");
        }

        private static long ReadLong(JObject obj, string key)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            return token.Type == JTokenType.Integer ? token.Value<long>()
                : long.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out long v) ? v : 0;
        }

        // =================================================================================
        // 幻化专属 4 表访问器(轮24 PI;对标老端 outward_illusion_cfg/outward_figure_stage_cfg/
        // upStarCfg/outward_skill_cfg,OutWardBaseModel.ts:483-490 GetConfig)。
        // =================================================================================

        /// <summary>可激活形象基础行(config_mount_figure["type_id@id@career"];缺当前职业回退职业1,
        /// 同 GetStageObj 惯例——同一 figure 不同职业仅换名字/资源,数值字段一致)。</summary>
        public static JObject GetFigureRow(int typeId, int id, int career)
        {
            if (_mountFigure == null) return null;
            return _mountFigure[typeId + "@" + id + "@" + career] as JObject
                ?? _mountFigure[typeId + "@" + id + "@1"] as JObject;
        }

        /// <summary>形象名(缺表/缺项降级 "")。</summary>
        public static string GetFigureName(int typeId, int id, int career)
        {
            return GetFigureRow(typeId, id, career)?.Value<string>("name") ?? "";
        }

        /// <summary>激活消耗物品 id/数量(缺表/缺项降级 0,goods_num==0 表示无消耗)。</summary>
        public static void GetFigureActivateCost(int typeId, int id, int career, out long goodsId, out long goodsNum)
        {
            JObject row = GetFigureRow(typeId, id, career);
            goodsId = row == null ? 0 : ReadLong(row, "goods_id");
            goodsNum = row == null ? 0 : ReadLong(row, "goods_num");
        }

        /// <summary>升阶行(config_mount_figure_stage["type_id@id@stage"])。</summary>
        public static JObject GetFigureStageRow(int typeId, int id, int stage)
        {
            return _mountFigureStage?[typeId + "@" + id + "@" + stage] as JObject;
        }

        /// <summary>本阶祝福上限(max_blessing;缺表/缺项降级 0)。</summary>
        public static long GetFigureStageMaxBlessing(int typeId, int id, int stage)
        {
            JObject row = GetFigureStageRow(typeId, id, stage);
            return row == null ? 0 : ReadLong(row, "max_blessing");
        }

        /// <summary>升星行(config_mount_figure_star["type_id@id@star"],老端 upStarCfg)。</summary>
        public static JObject GetFigureStarRow(int typeId, int id, int star)
        {
            return _mountFigureStar?[typeId + "@" + id + "@" + star] as JObject;
        }

        /// <summary>技能行(config_mount_skill["type_id@skill_id"])。</summary>
        public static JObject GetSkillRow(int typeId, int skillId)
        {
            return _mountSkill?[typeId + "@" + skillId] as JObject;
        }

        /// <summary>解出行内字符串化二级 JSON 数组字段(cost/attr/skill_list/condition_list/exp_goods 等;
        /// 全仓已知既有序列化风格——部分数组元素被序列化成"数字字符串键对象"而非真数组,如
        /// "[{"0":0,"1":16030109,"2":20}]",按下标字符串键(如 obj["1"])正常取值即可,非本表特有坑。
        /// 缺表/缺项/解析失败一律降级空数组,不抛异常。</summary>
        public static JArray ParseJsonArrayField(JObject row, string field)
        {
            string s = row?.Value<string>(field);
            if (string.IsNullOrEmpty(s)) return new JArray();
            try { return JArray.Parse(s); }
            catch (System.Exception e)
            {
                GameLog.Error("OutWard", "ParseJsonArrayField({0}) 解析失败: {1}", field, e.Message);
                return new JArray();
            }
        }
    }
}
