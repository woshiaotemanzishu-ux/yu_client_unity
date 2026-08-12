using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Skill;

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

        public enum EffectLifecyclePhase
        {
            Empty,
            Loading,
            Attached,
            FirstFrameReady,
            Failed,
            Released,
        }

        /// <summary>
        /// 模型常驻特效的纯生命周期/动态帧投影。它不依赖 GameObject，可由 View 在装载、附着、RT 采样与释放时推进，
        /// 也可由 CliVerify 直接构造验证；RT 存在本身不会把状态推进到 FirstFrameReady。
        /// </summary>
        public sealed class EffectLifecycleState
        {
            public int Epoch { get; private set; }
            public string ResourceKey { get; private set; } = string.Empty;
            public EffectLifecyclePhase Phase { get; private set; }
            public int SampleCount { get; private set; }
            public int VisiblePixels { get; private set; }
            public int LastFingerprint { get; private set; }
            public bool HasDynamicFrameChange { get; private set; }
            public string Failure { get; private set; } = string.Empty;

            public void Begin(int epoch, string resourceKey)
            {
                Epoch = epoch;
                ResourceKey = resourceKey ?? string.Empty;
                Phase = EffectLifecyclePhase.Loading;
                SampleCount = 0;
                VisiblePixels = 0;
                LastFingerprint = 0;
                HasDynamicFrameChange = false;
                Failure = string.Empty;
            }

            public bool MarkAttached(int epoch)
            {
                if (!Matches(epoch) || Phase != EffectLifecyclePhase.Loading) return false;
                Phase = EffectLifecyclePhase.Attached;
                return true;
            }

            public bool ObserveFrame(int epoch, int visiblePixels, int fingerprint)
            {
                if (!Matches(epoch) || (Phase != EffectLifecyclePhase.Attached && Phase != EffectLifecyclePhase.FirstFrameReady))
                    return false;
                if (SampleCount > 0 && VisiblePixels > 0 && visiblePixels > 0 && fingerprint != LastFingerprint)
                    HasDynamicFrameChange = true;
                SampleCount++;
                VisiblePixels = Math.Max(0, visiblePixels);
                LastFingerprint = fingerprint;
                if (VisiblePixels >= 8) Phase = EffectLifecyclePhase.FirstFrameReady;
                return true;
            }

            public void Fail(int epoch, string reason)
            {
                if (!Matches(epoch)) return;
                Phase = EffectLifecyclePhase.Failed;
                Failure = reason ?? string.Empty;
            }

            public void Release(int epoch)
            {
                if (epoch < Epoch) return;
                Epoch = epoch;
                Phase = EffectLifecyclePhase.Released;
                VisiblePixels = 0;
            }

            private bool Matches(int epoch) => epoch == Epoch;
        }

        public enum OneKeyAvailability
        {
            Loading,
            Ready,
            Insufficient,
            MaxStage,
        }

        public sealed class OneKeyMaterialState
        {
            public int GoodsId;
            public int Type;
            public long Exp;
            public long Owned;
            public long ProvidedExp;
            public bool Available;
        }

        /// <summary>16005 一键提升所需的材料、QuickBuy、满阶与红点纯状态。</summary>
        public sealed class OneKeyState
        {
            public int TypeId;
            public int Stage;
            public int Star;
            public int MaxStar;
            public bool HasNextStage;
            public long NeedBlessing;
            public long ProvidedExp;
            public OneKeyAvailability Availability;
            public bool CanSubmit;
            public bool ShouldOpenQuickBuy;
            public bool ShowRedDot;
            public readonly List<OneKeyMaterialState> Materials = new List<OneKeyMaterialState>();
        }

        public sealed class IllusionRedState
        {
            public int TypeId;
            public bool ShowRedDot;
            public readonly List<IllusionFigureState> Figures = new List<IllusionFigureState>();
        }

        /// <summary>幻化属性行纯 ViewModel；Current/NextDelta 分开，未激活按 0 → 首阶投影。</summary>
        public sealed class IllusionAttributeRowState
        {
            public int AttrId;
            public string Name = string.Empty;
            public long CurrentValue;
            public long NextDelta;
            public string CurrentText = string.Empty;
            public string NextText = string.Empty;
            public bool IsStageAdd;
        }

        /// <summary>幻化技能行纯 ViewModel；配置决定解锁阶、名称与图标，16007 决定当前形象技能集合。</summary>
        public sealed class IllusionSkillRowState
        {
            public int SkillId;
            public int RequiredStage;
            public int SelectedStage;
            public string Name = string.Empty;
            public string Icon = string.Empty;
            public bool Locked;
        }

        public sealed class LevelSkillRowState
        {
            public int Index;
            public int TypeId;
            public int SkillId;
            public int SkillLevel;
            public int NextSkillLevel;
            public int RequiredOutwardLevel;
            public int CurrentOutwardLevel;
            public string Name = string.Empty;
            public string Icon = string.Empty;
            public bool IsLocked;
            public bool HasNextLevel;
            public bool CanUpgrade;
            public string BlockReason = string.Empty;
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

        public OneKeyState GetOneKeyState(int typeId, int career, Func<int, long> goodsCounter)
        {
            OutWardVo vo = Get(typeId);
            var state = new OneKeyState { TypeId = typeId, Availability = OneKeyAvailability.Loading };
            if (vo == null) return state;

            return ProjectOneKeyState(vo,
                OutWardConfigs.GetMaxStar(typeId, vo.Stage, career),
                OutWardConfigs.HasStage(typeId, vo.Stage + 1, career),
                OutWardConfigs.GetMaxBlessing(typeId, vo.Stage, vo.Star),
                OutWardConfigs.GetTrainGoods(typeId), goodsCounter);
        }

        /// <summary>
        /// 老端 OutWardBaseModel.CanLvUp 的纯投影：先算 remainingExp=max_blessing-blessing，
        /// 再仅累计 config_mount_prop 中 type=1 的 owned*exp；累计覆盖 remainingExp 才可提交/亮红点。
        /// 材料不足仍允许进入 QuickBuy，但绝不标成 Ready。
        /// </summary>
        public static OneKeyState ProjectOneKeyState(OutWardVo vo, int maxStar, bool hasNextStage,
            long maxBlessing, IReadOnlyList<OutWardConfigs.TrainGoodsConfig> goods,
            Func<int, long> goodsCounter)
        {
            var state = new OneKeyState
            {
                TypeId = vo?.TypeId ?? 0,
                Stage = vo?.Stage ?? 0,
                Star = vo?.Star ?? 0,
                MaxStar = maxStar,
                HasNextStage = hasNextStage,
                Availability = vo == null ? OneKeyAvailability.Loading : OneKeyAvailability.Insufficient,
            };
            if (vo == null) return state;

            bool maxed = maxStar > 0 && vo.Star >= maxStar && !hasNextStage;
            state.NeedBlessing = Math.Max(0, maxBlessing - vo.Blessing);
            if (goods != null)
            {
                for (int i = 0; i < goods.Count; i++)
                {
                    OutWardConfigs.TrainGoodsConfig row = goods[i];
                    if (row == null || row.Type != 1 || row.GoodsId <= 0 || row.Exp <= 0) continue;
                    long owned = goodsCounter != null ? Math.Max(0, goodsCounter(row.GoodsId)) : 0;
                    long provided = owned > long.MaxValue / row.Exp ? long.MaxValue : owned * row.Exp;
                    state.ProvidedExp = state.ProvidedExp > long.MaxValue - provided
                        ? long.MaxValue : state.ProvidedExp + provided;
                    state.Materials.Add(new OneKeyMaterialState
                    {
                        GoodsId = row.GoodsId,
                        Type = row.Type,
                        Exp = row.Exp,
                        Owned = owned,
                        ProvidedExp = provided,
                        Available = owned > 0,
                    });
                }
            }

            bool enough = state.NeedBlessing <= 0 || state.ProvidedExp >= state.NeedBlessing;
            state.Availability = maxed ? OneKeyAvailability.MaxStage
                : enough ? OneKeyAvailability.Ready : OneKeyAvailability.Insufficient;
            state.CanSubmit = state.Availability == OneKeyAvailability.Ready;
            state.ShouldOpenQuickBuy = state.Availability == OneKeyAvailability.Insufficient;
            state.ShowRedDot = state.CanSubmit;
            return state;
        }

        /// <summary>系统 B 的固定三技能行纯 ViewModel；生产节点缺失不影响状态与资格测试。</summary>
        public IReadOnlyList<LevelSkillRowState> GetLevelSkillRows(int typeId, int maxRows = 3)
        {
            var result = new List<LevelSkillRowState>();
            if (maxRows <= 0) return result;
            OutWardVo vo = Get(typeId);
            IReadOnlyList<int> configured = OutWardConfigs.GetLevelSkillIds(typeId);
            for (int i = 0; i < configured.Count && result.Count < maxRows; i++)
            {
                int skillId = configured[i];
                int level = 0;
                if (vo?.LvSkills != null)
                    for (int j = 0; j < vo.LvSkills.Count; j++)
                        if (vo.LvSkills[j].skillId == skillId) { level = vo.LvSkills[j].skillLevel; break; }
                int nextLevel = level + 1;
                int requiredLevel = GetMountUpgradeLevel(SkillConfigs.GetConditionTerm(skillId, nextLevel));
                bool hasNext = nextLevel <= SkillConfigs.GetMaxLevel(skillId);
                bool hasPanel = vo != null && vo.HasLv;
                bool eligible = hasPanel && hasNext && (requiredLevel <= 0 || vo.Level >= requiredLevel);
                result.Add(new LevelSkillRowState
                {
                    Index = result.Count,
                    TypeId = typeId,
                    SkillId = skillId,
                    SkillLevel = level,
                    NextSkillLevel = nextLevel,
                    RequiredOutwardLevel = requiredLevel,
                    CurrentOutwardLevel = vo?.Level ?? 0,
                    Name = SkillConfigs.GetName(skillId),
                    Icon = SkillConfigs.GetIconForLevel(skillId, level > 0 ? level : 1),
                    IsLocked = level <= 0,
                    HasNextLevel = hasNext,
                    CanUpgrade = eligible,
                    BlockReason = !hasPanel ? "panel-not-ready" : !hasNext ? "max-skill-level"
                        : requiredLevel > vo.Level ? "requires-outward-level-" + requiredLevel : string.Empty,
                });
            }
            return result;
        }

        public bool CanUpgradeLevelSkill(int typeId, int skillId, out LevelSkillRowState row, out string reason)
        {
            row = null;
            IReadOnlyList<LevelSkillRowState> rows = GetLevelSkillRows(typeId);
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].SkillId == skillId) { row = rows[i]; break; }
            reason = row == null ? "skill-not-in-level-system" : row.BlockReason;
            return row != null && row.CanUpgrade;
        }

        private static int GetMountUpgradeLevel(ErlangTerm condition)
        {
            if (condition?.Items == null) return 0;
            foreach (ErlangTerm tuple in condition.Items)
            {
                if (!tuple.IsCollection || tuple.Items == null || tuple.Items.Count < 2) continue;
                if (tuple.Items[0].As<string>() == "mount_upgrade_lv") return tuple.Items[1].As<int>();
            }
            return 0;
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

        /// <summary>
        /// 幻化生产 View 的只读状态投影。页面尚缺生产 Prefab 时，协议/Model 仍通过这个稳定接口
        /// 暴露锁定、已激活、当前使用、激活材料、升阶/升星可达性，避免未来 View 再复制一套判定。
        /// </summary>
        public sealed class IllusionFigureState
        {
            public int TypeId;
            public int FigureId;
            public bool Configured;
            public bool Activated;
            public bool Current;
            public int Stage;
            public int Star;
            public long EndTime;
            public int Career;
            public string Name;
            public int ModelRes;
            public long ActivateGoodsId;
            public long ActivateGoodsNum;
            public long ActivateOwnedNum;
            public bool ConditionsMet;
            public string ConditionBlockReason;
            public bool CanActivate;
            public bool HasNextStage;
            public long StageGoodsId;
            public long StageGoodsNum;
            public long StageOwnedNum;
            public bool CanStageUp;
            public bool HasStarSystem;
            public bool HasNextStar;
            public long StarGoodsId;
            public long StarGoodsNum;
            public long StarOwnedNum;
            public bool CanStarUp;
            public FigureDetailVo Detail;
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

        public FigureBriefVo GetFigureBrief(int typeId, int figureId)
        {
            IllusionListVo list = GetIllusionList(typeId);
            if (list?.FigureList == null) return null;
            for (int i = 0; i < list.FigureList.Count; i++)
                if (list.FigureList[i].Id == figureId) return list.FigureList[i];
            return null;
        }

        public bool IsFigureActivated(int typeId, int figureId) => GetFigureBrief(typeId, figureId) != null;

        public bool IsFigureCurrent(int typeId, int figureId)
            => GetIllusionList(typeId)?.IllusionId == figureId && figureId > 0;

        public IllusionFigureState GetIllusionFigureState(int typeId, int figureId, int career,
            System.Func<int, long> goodsCounter)
            => GetIllusionFigureState(typeId, figureId, career, int.MaxValue, goodsCounter);

        public IllusionFigureState GetIllusionFigureState(int typeId, int figureId, int career,
            int roleTurn, System.Func<int, long> goodsCounter)
        {
            JObject row = OutWardConfigs.GetFigureRow(typeId, figureId, career);
            FigureBriefVo brief = GetFigureBrief(typeId, figureId);
            FigureDetailVo detail = GetFigureDetail(typeId, figureId);
            int stage = detail?.Stage ?? brief?.Stage ?? 0;
            int star = detail?.Star ?? brief?.Star ?? 0;
            var state = new IllusionFigureState
            {
                TypeId = typeId,
                FigureId = figureId,
                Career = career,
                Configured = row != null,
                Activated = brief != null,
                Current = IsFigureCurrent(typeId, figureId),
                Stage = stage,
                Star = star,
                EndTime = detail?.EndTime ?? brief?.EndTime ?? 0,
                Name = row?.Value<string>("name") ?? string.Empty,
                ModelRes = row?.Value<int?>("ride_figure") ?? 0,
                ConditionsMet = true,
                ConditionBlockReason = string.Empty,
                Detail = detail,
            };

            OutWardConfigs.GetFigureActivateCost(typeId, figureId, career,
                out state.ActivateGoodsId, out state.ActivateGoodsNum);
            state.ConditionsMet = AreFigureConditionsMet(typeId, row, roleTurn, out string conditionBlock);
            state.ConditionBlockReason = conditionBlock;
            state.ActivateOwnedNum = CountGoods(goodsCounter, state.ActivateGoodsId);
            state.CanActivate = state.Configured && !state.Activated
                && state.ConditionsMet && state.ActivateOwnedNum >= state.ActivateGoodsNum;

            JObject nextStage = OutWardConfigs.GetFigureStageRow(typeId, figureId, MathfMax(1, stage + 1));
            state.HasNextStage = state.Activated && nextStage != null;
            ReadFirstCost(nextStage, out state.StageGoodsId, out state.StageGoodsNum);
            state.StageOwnedNum = CountGoods(goodsCounter, state.StageGoodsId);
            state.CanStageUp = state.HasNextStage && state.StageOwnedNum >= state.StageGoodsNum;

            state.HasStarSystem = OutWardConfigs.HasFigureStarSystem(typeId, figureId);
            JObject nextStar = OutWardConfigs.GetFigureStarRow(typeId, figureId, star);
            state.HasNextStar = state.Activated && nextStar != null;
            ReadFirstCost(nextStar, out state.StarGoodsId, out state.StarGoodsNum);
            state.StarOwnedNum = CountGoods(goodsCounter, state.StarGoodsId);
            state.CanStarUp = state.HasNextStar && state.StarOwnedNum >= state.StarGoodsNum;
            return state;
        }

        public IllusionRedState GetIllusionRedState(int typeId, int career, int roleTurn,
            Func<int, long> goodsCounter)
        {
            var state = new IllusionRedState { TypeId = typeId };
            IReadOnlyList<int> ids = OutWardConfigs.GetFigureIds(typeId, career);
            for (int i = 0; i < ids.Count; i++)
            {
                IllusionFigureState figure = GetIllusionFigureState(typeId, ids[i], career, roleTurn, goodsCounter);
                state.Figures.Add(figure);
                state.ShowRedDot |= figure.CanActivate || figure.CanStageUp || figure.CanStarUp;
            }
            return state;
        }

        /// <summary>
        /// 对标老端 IllusionBaseView.SetPropData：当前阶 attr/add 与下一阶作差；未激活没有当前行，
        /// 因而按 0 → stage=1 的完整值显示。16007 attr_list 在已激活时覆盖当前配置值。
        /// </summary>
        public IReadOnlyList<IllusionAttributeRowState> GetIllusionAttributeRows(
            int typeId, int figureId, int selectedStage, FigureDetailVo detail = null)
        {
            var result = new List<IllusionAttributeRowState>();
            JObject currentRow = selectedStage > 0
                ? OutWardConfigs.GetFigureStageRow(typeId, figureId, selectedStage) : null;
            JObject nextRow = OutWardConfigs.GetFigureStageRow(typeId, figureId, Math.Max(1, selectedStage + 1));
            Dictionary<int, long> currentAttrs = ReadAttrMap(currentRow, "attr");
            Dictionary<int, long> nextAttrs = ReadAttrMap(nextRow, "attr");
            if (selectedStage > 0 && detail?.Attrs != null)
                for (int i = 0; i < detail.Attrs.Count; i++)
                    currentAttrs[detail.Attrs[i].attrId] = detail.Attrs[i].val;

            var attrIds = new List<int>(currentAttrs.Keys);
            foreach (int id in nextAttrs.Keys) if (!attrIds.Contains(id)) attrIds.Add(id);
            attrIds.Sort();
            for (int i = 0; i < attrIds.Count; i++)
            {
                int attrId = attrIds[i];
                long current = currentAttrs.TryGetValue(attrId, out long cur) ? cur : 0;
                long next = nextAttrs.TryGetValue(attrId, out long nxt) ? nxt : current;
                string name = GoodsModel.GetAttrName(attrId);
                result.Add(new IllusionAttributeRowState
                {
                    AttrId = attrId,
                    Name = string.IsNullOrEmpty(name) ? "属性" + attrId : name,
                    CurrentValue = current,
                    NextDelta = next - current,
                    CurrentText = GoodsModel.FormatAttrValue(attrId, current),
                    NextText = next == current ? string.Empty : "+" + GoodsModel.FormatAttrValue(attrId, next - current),
                });
            }

            AddStageAddRows(result, typeId, currentRow, nextRow);
            return result;
        }

        /// <summary>
        /// 对标老端 SetSkillData：技能集合取形象配置 skill_list，已激活时以16007 skill_list补充；
        /// config_mount_skill.type=1 才属于幻化阶技能，按 stage/skillId 排序并投影锁定与解锁阶。
        /// </summary>
        public IReadOnlyList<IllusionSkillRowState> GetIllusionSkillRows(
            int typeId, int figureId, int career, int selectedStage, FigureDetailVo detail = null)
        {
            var ids = new List<int>();
            JArray configured = OutWardConfigs.ParseJsonArrayField(
                OutWardConfigs.GetFigureRow(typeId, figureId, career), "skill_list");
            for (int i = 0; i < configured.Count; i++)
            {
                int id = ReadIndexedInt(configured[i], -1);
                if (id <= 0 && int.TryParse(configured[i]?.ToString(), out int scalar)) id = scalar;
                if (id > 0 && !ids.Contains(id)) ids.Add(id);
            }
            if (detail?.Skills != null)
                for (int i = 0; i < detail.Skills.Count; i++)
                    if (detail.Skills[i] > 0 && !ids.Contains(detail.Skills[i])) ids.Add(detail.Skills[i]);
            // 翼影/圣器/神兵/背饰的 figure.skill_list 在当前表为空，老端仍由
            // config_mount_skill(type=1) 展示固定阶技能；统一回落到同一权威表。
            if (ids.Count == 0)
                foreach (int id in OutWardConfigs.GetDefaultSkillIds(typeId)) ids.Add(id);

            var result = new List<IllusionSkillRowState>();
            for (int i = 0; i < ids.Count; i++)
            {
                int id = ids[i];
                JObject row = OutWardConfigs.GetSkillRow(typeId, id);
                if (row == null || ReadConfigInt(row, "type") != 1 || SkillConfigs.GetSkillType(id) == 1) continue;
                int requiredStage = ReadConfigInt(row, "stage");
                result.Add(new IllusionSkillRowState
                {
                    SkillId = id,
                    RequiredStage = requiredStage,
                    SelectedStage = selectedStage,
                    Name = SkillConfigs.GetName(id),
                    Icon = SkillConfigs.GetIconForLevel(id, 1),
                    Locked = requiredStage > selectedStage,
                });
            }
            result.Sort((a, b) => a.RequiredStage != b.RequiredStage
                ? a.RequiredStage.CompareTo(b.RequiredStage) : a.SkillId.CompareTo(b.SkillId));
            return result;
        }

        private static Dictionary<int, long> ReadAttrMap(JObject row, string field)
        {
            var result = new Dictionary<int, long>();
            JArray data = OutWardConfigs.ParseJsonArrayField(row, field);
            for (int i = 0; i < data.Count; i++)
            {
                int id = ReadIndexedInt(data[i], 0);
                long value = ReadIndexedLong(data[i], 1);
                if (id > 0) result[id] = value;
            }
            return result;
        }

        private static void AddStageAddRows(List<IllusionAttributeRowState> result, int typeId,
            JObject currentRow, JObject nextRow)
        {
            Dictionary<int, long> current = ReadAttrMap(currentRow, "add");
            Dictionary<int, long> next = ReadAttrMap(nextRow, "add");
            var ids = new List<int>(current.Keys);
            foreach (int id in next.Keys) if (!ids.Contains(id)) ids.Add(id);
            ids.Sort();
            for (int i = 0; i < ids.Count; i++)
            {
                int attrId = ids[i];
                long cur = current.TryGetValue(attrId, out long c) ? c : 0;
                long nxt = next.TryGetValue(attrId, out long n) ? n : cur;
                string name = GoodsModel.GetAttrName(attrId);
                result.Add(new IllusionAttributeRowState
                {
                    AttrId = attrId,
                    Name = GetTypeWord(typeId) + (string.IsNullOrEmpty(name) ? "属性" + attrId : name),
                    CurrentValue = cur,
                    NextDelta = nxt - cur,
                    CurrentText = FormatStageAdd(cur),
                    NextText = nxt == cur ? string.Empty : "+" + FormatStageAdd(nxt - cur),
                    IsStageAdd = true,
                });
            }
        }

        private static int ReadConfigInt(JObject row, string field)
            => row?[field]?.Value<int?>() ?? (int.TryParse(row?[field]?.ToString(), out int value) ? value : 0);

        private static int ReadIndexedInt(JToken token, int index)
            => (int)ReadIndexedLong(token, index);

        private static long ReadIndexedLong(JToken token, int index)
        {
            JToken value = token is JArray array && index >= 0 && index < array.Count ? array[index]
                : token is JObject obj ? obj[index.ToString()] : null;
            if (value == null) return 0;
            return value.Type == JTokenType.Integer ? value.Value<long>()
                : long.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out long parsed) ? parsed : 0;
        }

        private static string FormatStageAdd(long raw)
            => (raw / 100d).ToString("0.##", CultureInfo.InvariantCulture) + "%";

        private static string GetTypeWord(int typeId)
        {
            switch (typeId)
            {
                case 1: return "坐骑";
                case 2: return "同修";
                case 3: return "翼影";
                case 4: return "圣器";
                case 5: return "神兵";
                case 12: return "背饰";
                default: return "外观";
            }
        }

        private bool AreFigureConditionsMet(int typeId, JObject row, int roleTurn, out string reason)
        {
            reason = string.Empty;
            JArray conditions = OutWardConfigs.ParseJsonArrayField(row, "condition_list");
            for (int i = 0; i < conditions.Count; i++)
            {
                JToken token = conditions[i];
                string kind = ReadConditionValue(token, 0);
                if (kind == "turn")
                {
                    int needTurn = ReadConditionInt(token, 1);
                    if (roleTurn < needTurn) { reason = "requires-turn-" + needTurn; return false; }
                }
                else if (kind == "active_id")
                {
                    JToken value = token is JArray arr && arr.Count > 1 ? arr[1]
                        : token is JObject obj ? obj["1"] : null;
                    int needId = value?["id"]?.Value<int?>() ?? 0;
                    int needStage = value?["lv"]?.Value<int?>() ?? 0;
                    FigureBriefVo brief = GetFigureBrief(typeId, needId);
                    if (brief == null || brief.Stage < needStage)
                    {
                        reason = "requires-figure-" + needId + "-stage-" + needStage;
                        return false;
                    }
                }
            }
            return true;
        }

        private static string ReadConditionValue(JToken token, int index)
        {
            if (token is JArray arr && index >= 0 && index < arr.Count) return arr[index]?.ToString() ?? string.Empty;
            if (token is JObject obj) return obj[index.ToString()]?.ToString() ?? string.Empty;
            return string.Empty;
        }

        private static int ReadConditionInt(JToken token, int index)
            => int.TryParse(ReadConditionValue(token, index), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;

        private static long CountGoods(System.Func<int, long> goodsCounter, long goodsId)
            => goodsCounter != null && goodsId > 0 && goodsId <= int.MaxValue ? goodsCounter((int)goodsId) : 0;

        private static int MathfMax(int a, int b) => a > b ? a : b;

        private static void ReadFirstCost(JObject row, out long goodsId, out long goodsNum)
        {
            goodsId = 0;
            goodsNum = 0;
            JArray costs = OutWardConfigs.ParseJsonArrayField(row, "cost");
            if (costs.Count <= 0 || !(costs[0] is JObject first)) return;
            goodsId = ReadCostValue(first, "1");
            goodsNum = ReadCostValue(first, "2");
        }

        private static long ReadCostValue(JObject obj, string key)
        {
            JToken token = obj?[key];
            if (token == null) return 0;
            return token.Type == JTokenType.Integer ? token.Value<long>()
                : long.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out long value) ? value : 0;
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

    public readonly struct OutWardTransactionResult
    {
        public readonly int Command;
        public readonly int TypeId;
        public readonly int EntityId;
        public readonly int Code;
        public bool Success => Code == 1;

        public OutWardTransactionResult(int command, int typeId, int entityId, int code)
        {
            Command = command;
            TypeId = typeId;
            EntityId = entityId;
            Code = code;
        }
    }

    /// <summary>垂神翼影所有写操作的成功/失败/父页权威刷新矩阵；51302 明确为 send-only 红点确认。</summary>
    public sealed class OutWardTransactionRefreshPolicy
    {
        public int Command;
        public bool HasAcknowledgement;
        public string SuccessEvent;
        public string FailureEvent;
        public int[] ParentRefreshCommands;
        public string AuthorityMutation;
        public bool UiBlocked;
    }

    public static class OutWardTransactionRefreshPolicies
    {
        public static readonly OutWardTransactionRefreshPolicy[] All =
        {
            Ack(Proto.OUTWARD_ILLUSION_WEAR, GlobalEvent.EVT_OUTWARD_ILLUSION_WEAR, "ApplyIllusionWear"),
            Ack(Proto.OUTWARD_STAR_UP_GENERIC, GlobalEvent.EVT_OUTWARD_UPDATE, "Apply16005", Proto.OUTWARD_INFO),
            Ack(Proto.OUTWARD_STAR_UP, GlobalEvent.EVT_OUTWARD_UPDATE, "Apply16023", Proto.OUTWARD_INFO),
            Ack(Proto.OUTWARD_FIGURE_ACTIVATE, GlobalEvent.EVT_OUTWARD_FIGURE_ACTIVATED, "list-authority", true, Proto.OUTWARD_ILLUSION_LIST),
            Ack(Proto.OUTWARD_FIGURE_STAGE_UP, GlobalEvent.EVT_OUTWARD_FIGURE_STAGE_UP, "list-authority", true, Proto.OUTWARD_ILLUSION_LIST),
            Ack(Proto.OUTWARD_CRYSTAL_USE, GlobalEvent.EVT_OUTWARD_CRYSTAL_UPDATE, "counter+info-authority", Proto.OUTWARD_CRYSTAL_COUNTER, Proto.OUTWARD_INFO),
            Ack(Proto.OUTWARD_FIGURE_STAR_UP, GlobalEvent.EVT_OUTWARD_FIGURE_STAR_UP, "PatchIllusionStar", true, Proto.OUTWARD_ILLUSION_LIST, Proto.OUTWARD_FIGURE_DETAIL),
            Ack(Proto.OUTWARD_LV_UP, GlobalEvent.EVT_OUTWARD_UPDATE, "Apply16029", Proto.OUTWARD_INFO),
            Ack(Proto.OUTWARD_LV_SKILL_UP, GlobalEvent.EVT_OUTWARD_UPDATE, "Apply16030", true, Proto.OUTWARD_INFO),
            new OutWardTransactionRefreshPolicy
            {
                Command = Proto.FAIRYWISH_BUY,
                HasAcknowledgement = false,
                SuccessEvent = GlobalEvent.EVT_FAIRYWISH_UPDATE,
                FailureEvent = string.Empty,
                ParentRefreshCommands = new[] { Proto.FAIRYWISH_INFO },
                AuthorityMutation = "entry-red-local-2-or-3;51300-separate-query",
                UiBlocked = true,
            },
            new OutWardTransactionRefreshPolicy
            {
                Command = Proto.SHOP_QUICK_BUY,
                HasAcknowledgement = true,
                SuccessEvent = GlobalEvent.EVT_SHOP_BUY_SUCCESS,
                FailureEvent = GlobalEvent.EVT_SHOP_QUICK_BUY_RESULT,
                ParentRefreshCommands = Array.Empty<int>(),
                AuthorityMutation = "bag-authority-then-outward-projection",
                UiBlocked = false,
            },
        };

        public static OutWardTransactionRefreshPolicy Get(int command)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Command == command) return All[i];
            return null;
        }

        private static OutWardTransactionRefreshPolicy Ack(int command, string successEvent,
            string mutation, params int[] refreshCommands)
            => Ack(command, successEvent, mutation, false, refreshCommands);

        private static OutWardTransactionRefreshPolicy Ack(int command, string successEvent,
            string mutation, bool uiBlocked, params int[] refreshCommands)
            => new OutWardTransactionRefreshPolicy
            {
                Command = command,
                HasAcknowledgement = true,
                SuccessEvent = successEvent,
                FailureEvent = GlobalEvent.EVT_OUTWARD_TRANSACTION_RESULT,
                ParentRefreshCommands = refreshCommands ?? Array.Empty<int>(),
                AuthorityMutation = mutation,
                UiBlocked = uiBlocked,
            };
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
        private static JObject _mountProp;
        private static JObject _mountGoods;
        private static JObject _mountFigure;        // 轮24 PI:幻化"可激活形象"列表(主键 "type_id@id@career")
        private static JObject _mountFigureStage;   // 轮24 PI:幻化升阶(主键 "type_id@id@stage")
        private static JObject _mountFigureStar;    // 轮24 PI:幻化升星(主键 "type_id@id@star",老端 upStarCfg)
        private static JObject _mountSkill;         // 轮24 PI:幻化技能(主键 "type_id@skill_id")
        private static JObject _mountLevel;         // 系统B等级经验(主键 "type_id@level")
        private static readonly Dictionary<int, List<int>> _trainGoodsByType = new Dictionary<int, List<int>>();
        private static readonly Dictionary<int, List<TrainGoodsConfig>> _trainGoodsConfigByType = new Dictionary<int, List<TrainGoodsConfig>>();
        private static readonly Dictionary<int, List<int>> _crystalGoodsByType = new Dictionary<int, List<int>>();
        private static readonly Dictionary<int, List<int>> _defaultSkillsByType = new Dictionary<int, List<int>>();
        private static readonly Dictionary<int, List<int>> _levelSkillsByType = new Dictionary<int, List<int>>();
        private static readonly Dictionary<string, List<int>> _figureIdsByTypeCareer = new Dictionary<string, List<int>>();

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
                _crystalGoodsByType.Clear();
            }
            if (_mountProp == null)
            {
                _mountProp = await LoadServerConfig("config_mount_prop");
                _trainGoodsByType.Clear();
                _trainGoodsConfigByType.Clear();
            }
            if (_mountFigure == null)
            {
                _mountFigure = await LoadServerConfig("config_mount_figure");
                _figureIdsByTypeCareer.Clear();
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
                _defaultSkillsByType.Clear();
                _levelSkillsByType.Clear();
            }
            if (_mountLevel == null)
            {
                _mountLevel = await LoadServerConfig("config_mount_level");
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

        /// <summary>
        /// 某培养对象的系统 A 培养材料。老端 GetExpItemList(type, 1) 来自
        /// config_mount_prop 的 type_id/type=1；config_mount_goods 是魔晶表，不能混用。
        /// </summary>
        public static IReadOnlyList<int> GetTrainGoodsIds(int typeId)
        {
            if (_trainGoodsByType.TryGetValue(typeId, out List<int> cached)) return cached;
            var list = new List<int>();
            IReadOnlyList<TrainGoodsConfig> rows = GetTrainGoods(typeId);
            for (int i = 0; i < rows.Count; i++)
            {
                int goodsId = rows[i].GoodsId;
                if (goodsId > 0 && !list.Contains(goodsId)) list.Add(goodsId);
            }
            list.Sort();
            _trainGoodsByType[typeId] = list;
            return list;
        }

        public sealed class TrainGoodsConfig
        {
            public int GoodsId;
            public int Type;
            public long Exp;
        }

        /// <summary>config_mount_prop 培养材料投影；保留 goodsId/exp/type，调用方必须只消费 type=1。</summary>
        public static IReadOnlyList<TrainGoodsConfig> GetTrainGoods(int typeId)
        {
            if (_trainGoodsConfigByType.TryGetValue(typeId, out List<TrainGoodsConfig> cached)) return cached;
            var list = new List<TrainGoodsConfig>();
            if (_mountProp != null)
            {
                foreach (KeyValuePair<string, JToken> kv in _mountProp)
                {
                    if (!(kv.Value is JObject obj) || ReadLong(obj, "type_id") != typeId) continue;
                    int goodsId = (int)ReadLong(obj, "goods_id");
                    int type = (int)ReadLong(obj, "type");
                    long exp = ReadLong(obj, "exp");
                    if (goodsId > 0) list.Add(new TrainGoodsConfig { GoodsId = goodsId, Type = type, Exp = exp });
                }
                list.Sort((a, b) => a.GoodsId.CompareTo(b.GoodsId));
            }
            _trainGoodsConfigByType[typeId] = list;
            return list;
        }

        /// <summary>某培养对象的三枚魔晶物品 id(config_mount_goods)。</summary>
        public static IReadOnlyList<int> GetCrystalGoodsIds(int typeId)
        {
            if (_crystalGoodsByType.TryGetValue(typeId, out List<int> cached)) return cached;
            var list = new List<int>();
            if (_mountGoods != null)
            {
                foreach (KeyValuePair<string, JToken> kv in _mountGoods)
                {
                    if (!(kv.Value is JObject obj) || ReadLong(obj, "type_id") != typeId) continue;
                    int goodsId = (int)ReadLong(obj, "goods_id");
                    if (goodsId > 0 && !list.Contains(goodsId)) list.Add(goodsId);
                }
                list.Sort();
            }
            _crystalGoodsByType[typeId] = list;
            return list;
        }

        /// <summary>
        /// 培养页常驻技能球。对标老端 OutWardBaseModel.GetDefaultSkillList：只取
        /// config_mount_skill 中 type=1 的条目，并按解锁 stage 升序排列；是否过滤主动技能
        /// 由已加载的 config_skill 在 View 层裁决，避免 OutWard 配置层反向依赖 Skill 模块。
        /// </summary>
        public static IReadOnlyList<int> GetDefaultSkillIds(int typeId)
        {
            if (_defaultSkillsByType.TryGetValue(typeId, out List<int> cached)) return cached;
            var rows = new List<(int stage, int skillId)>();
            if (_mountSkill != null)
            {
                foreach (KeyValuePair<string, JToken> kv in _mountSkill)
                {
                    if (!(kv.Value is JObject obj)) continue;
                    if (ReadLong(obj, "type_id") != typeId || ReadLong(obj, "type") != 1) continue;
                    int skillId = (int)ReadLong(obj, "skill_id");
                    if (skillId <= 0) continue;
                    bool duplicate = false;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        if (rows[i].skillId != skillId) continue;
                        duplicate = true;
                        break;
                    }
                    if (!duplicate) rows.Add(((int)ReadLong(obj, "stage"), skillId));
                }
            }
            rows.Sort((a, b) => a.stage != b.stage
                ? a.stage.CompareTo(b.stage)
                : a.skillId.CompareTo(b.skillId));
            var list = new List<int>(rows.Count);
            for (int i = 0; i < rows.Count; i++) list.Add(rows[i].skillId);
            _defaultSkillsByType[typeId] = list;
            return list;
        }

        /// <summary>系统 B 固定技能行(config_mount_skill type=4)，按 skill_id 排序；翼影当前正好三行。</summary>
        public static IReadOnlyList<int> GetLevelSkillIds(int typeId)
        {
            if (_levelSkillsByType.TryGetValue(typeId, out List<int> cached)) return cached;
            var list = new List<int>();
            if (_mountSkill != null)
            {
                foreach (KeyValuePair<string, JToken> kv in _mountSkill)
                {
                    if (!(kv.Value is JObject obj)) continue;
                    if (ReadLong(obj, "type_id") != typeId || ReadLong(obj, "type") != 4) continue;
                    int skillId = (int)ReadLong(obj, "skill_id");
                    if (skillId > 0 && !list.Contains(skillId)) list.Add(skillId);
                }
                list.Sort();
            }
            _levelSkillsByType[typeId] = list;
            return list;
        }

        /// <summary>阶名(config_mount_stage["type@stage@career"].name;缺表/缺项降级 "")。</summary>
        public static string GetStageName(int typeId, int stage, int career)
        {
            JObject obj = GetStageObj(typeId, stage, career);
            return obj?.Value<string>("name") ?? "";
        }

        /// <summary>当前阶对应的基础外观资源 ride_figure；缺表返回 0。</summary>
        public static int GetStageModelRes(int typeId, int stage, int career)
        {
            JObject obj = GetStageObj(typeId, stage, career);
            return obj == null ? 0 : (int)ReadLong(obj, "ride_figure");
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

        public static bool HasStage(int typeId, int stage, int career) => GetStageObj(typeId, stage, career) != null;

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

        /// <summary>按当前职业去重枚举全部 Figure 配置 id；缺职业行时沿 GetFigureRow 规则回退职业 1。</summary>
        public static IReadOnlyList<int> GetFigureIds(int typeId, int career)
        {
            string cacheKey = typeId + "@" + career;
            if (_figureIdsByTypeCareer.TryGetValue(cacheKey, out List<int> cached)) return cached;
            var exact = new List<int>();
            var fallback = new List<int>();
            if (_mountFigure != null)
            {
                foreach (KeyValuePair<string, JToken> kv in _mountFigure)
                {
                    if (!(kv.Value is JObject row) || ReadLong(row, "type_id") != typeId) continue;
                    int id = (int)ReadLong(row, "id");
                    int rowCareer = (int)ReadLong(row, "career");
                    if (id <= 0) continue;
                    if (rowCareer == career && !exact.Contains(id)) exact.Add(id);
                    if (rowCareer == 1 && !fallback.Contains(id)) fallback.Add(id);
                }
            }
            List<int> list = exact.Count > 0 ? exact : fallback;
            list.Sort();
            _figureIdsByTypeCareer[cacheKey] = list;
            return list;
        }

        public static long GetLevelNeedExp(int typeId, int level)
        {
            JObject row = _mountLevel?[typeId + "@" + level] as JObject;
            return row == null ? 0 : ReadLong(row, "need_exp");
        }

        /// <summary>系统B是否存在指定等级行；用于生产页区分可升级与已满级，禁止用 need_exp=0 猜满级。</summary>
        public static bool HasLevel(int typeId, int level)
        {
            return level > 0 && _mountLevel?[typeId + "@" + level] is JObject;
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

        /// <summary>该类型/形象是否存在任一升星配置；翼影当前配表为 false，View 应隐藏升星入口。</summary>
        public static bool HasFigureStarSystem(int typeId, int id)
        {
            if (_mountFigureStar == null) return false;
            string prefix = typeId + "@" + id + "@";
            foreach (JProperty property in _mountFigureStar.Properties())
                if (property.Name.StartsWith(prefix, System.StringComparison.Ordinal)) return true;
            return false;
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
