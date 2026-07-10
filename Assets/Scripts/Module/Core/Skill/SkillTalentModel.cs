using System.Collections.Generic;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Skill
{
    /// <summary>
    /// 天赋数据 + 职业技能buff(12093) + 模块加成杂项(18401)(对标老端 commonModel/SkillUIModel.ts 里
    /// 对应字段的最小子集:innate_skill_info_/career_skill_buff_list,及 SkillController.on18401 就地处理的两个 key)。
    /// 单例;数据落 SkillController.On21010/On12093/On18401,Dispose 挂 SkillController.Dispose 清空。
    /// 天赋 UI 壳本轮不做(留 3b),这里只建数据层 + 21011 前置校验公用方法,供 3b 直接消费。
    /// </summary>
    public sealed class SkillTalentModel
    {
        public static readonly SkillTalentModel Instance = new SkillTalentModel();
        private SkillTalentModel() { }

        // ===================== 21010:天赋技能面板 =====================

        /// <summary>天赋分支(对标 pt_210.erl item_to_bin_3:SkillType/Point/Skills)。SkillType 真实取值来自服务端
        /// 回包(实测 5攻击/6防守/7通用,不在此臆造枚举名做强绑定,按值透传)。</summary>
        public sealed class TalentGroup
        {
            public int SkillType;
            public int Point;
            public readonly Dictionary<int, int> SkillLevels = new Dictionary<int, int>(); // skillId -> skillLv
        }

        public int LessPoint { get; private set; }
        public bool HasTalentInfo { get; private set; }
        private readonly Dictionary<int, TalentGroup> _groups = new Dictionary<int, TalentGroup>();

        /// <summary>21010 回包落地(对标 SkillUIModel.SetInnateInfo):整表替换。</summary>
        public void SetTalentInfo(int lessPoint, List<TalentGroup> groups)
        {
            LessPoint = lessPoint;
            _groups.Clear();
            if (groups != null)
            {
                foreach (TalentGroup g in groups) _groups[g.SkillType] = g;
            }
            HasTalentInfo = true;
        }

        public TalentGroup GetGroup(int skillType) => _groups.TryGetValue(skillType, out TalentGroup g) ? g : null;

        /// <summary>21011 成功回包直接落地(对标规格"成功→更新模型"要求):写 LessPoint + 该 skillId 的新等级,
        /// 随后 SkillController 仍会补发 21010 做权威全量刷新(本方法只是让 UI 不必等一次网络往返)。</summary>
        public void ApplyLearnResult(int skillId, int skillLv, int lessPoint)
        {
            LessPoint = lessPoint;
            foreach (TalentGroup g in _groups.Values)
            {
                if (g.SkillLevels.ContainsKey(skillId)) { g.SkillLevels[skillId] = skillLv; return; }
            }
            // 21010 尚未建过该 skill 的归属分支(理论不会发生)→ 落一个 skillType=0 的兜底分支,
            // 不新建假的真实分支(GetGroup(realType).Point 语义不受影响),等 21010 全量覆盖纠正。
            if (!_groups.TryGetValue(0, out TalentGroup misc))
            {
                misc = new TalentGroup { SkillType = 0 };
                _groups[0] = misc;
            }
            misc.SkillLevels[skillId] = skillLv;
        }

        /// <summary>某天赋技能当前等级(未学 → 0)。</summary>
        public int GetTalentLevel(int skillId)
        {
            foreach (TalentGroup g in _groups.Values)
            {
                if (g.SkillLevels.TryGetValue(skillId, out int lv)) return lv;
            }
            return 0;
        }

        /// <summary>
        /// 21011 发送前置校验(对标 innateSkill/InnateUpInfoItem.ts:62-130 + 服务端 lib_skill.erl:1240-1276
        /// check_talent_skill_learn_cond 权威语义):满级 / 剩余天赋点&lt;=0 / point 分支点数不足 / pre_skill(2) 前置
        /// 技能等级不足 → 拦截并给出文案(逐字对标老端三条 Message)。turn/career 两类条件不臆造判定(服务端兜底,
        /// 老端点击处理器本身对 turn 也只是 break 不校验、未处理 career)。
        /// </summary>
        public bool CanLearn(int skillId, out string failReason)
        {
            failReason = null;

            int curLevel = GetTalentLevel(skillId);
            int maxLevel = SkillConfigs.GetMaxLevel(skillId);
            if (maxLevel > 0 && curLevel >= maxLevel)
            {
                failReason = "技能已满级";
                return false;
            }
            if (LessPoint <= 0)
            {
                failReason = "天赋点不足";
                return false;
            }

            ErlangTerm cond = SkillConfigs.GetConditionTerm(skillId, curLevel + 1);
            if (cond?.Items == null) return true;

            foreach (ErlangTerm tuple in cond.Items)
            {
                if (!tuple.IsCollection || tuple.Items == null || tuple.Items.Count == 0) continue;
                string kind = tuple.Items[0].As<string>();

                if (kind == "point" && tuple.Items.Count >= 3)
                {
                    int needType = tuple.Items[1].As<int>();
                    int needPoint = tuple.Items[2].As<int>();
                    int have = GetGroup(needType)?.Point ?? 0;
                    if (have < needPoint) { failReason = "条件不足"; return false; }
                }
                else if (kind == "pre_skill" && tuple.Items.Count >= 3)
                {
                    int preId = tuple.Items[1].As<int>();
                    int preLv = tuple.Items[2].As<int>();
                    if (GetTalentLevel(preId) < preLv) { failReason = "条件不足"; return false; }
                }
                else if (kind == "pre_skill2" && tuple.Items.Count >= 2 && tuple.Items[1].IsCollection)
                {
                    foreach (ErlangTerm sub in tuple.Items[1].Items)
                    {
                        if (!sub.IsCollection || sub.Items == null || sub.Items.Count < 2) continue;
                        int preId = sub.Items[0].As<int>();
                        int preLv = sub.Items[1].As<int>();
                        if (GetTalentLevel(preId) < preLv) { failReason = "条件不足"; return false; }
                    }
                }
                // turn/career:服务端兜底,不在此判定(对标老端同样不在客户端拦截这两类)。
            }

            return true;
        }

        // ===================== 12093:职业技能给予的 buff =====================

        /// <summary>对标老端 SkillUIModel.career_skill_buff_list(纯被动推送,列表项即技能id+等级)。</summary>
        public readonly List<(int skillId, int skillLv)> CareerSkillBuffList = new List<(int skillId, int skillLv)>();

        public void SetCareerSkillBuffList(List<(int skillId, int skillLv)> list)
        {
            CareerSkillBuffList.Clear();
            if (list != null) CareerSkillBuffList.AddRange(list);
        }

        // ===================== 18401:模块加成效果列表 =====================

        private readonly Dictionary<int, string> _moduleBuffRaw = new Dictionary<int, string>();

        /// <summary>key==2 解出的挂机时长上限(对标老端 OutLineModel.max_outline_time = 20*3600+onhook_time,秒)。</summary>
        public long OnhookMaxTimeSec { get; private set; }

        /// <summary>key==6 裸数字(对标老端 CompositeModel.lifeSkillAdd,生活技能加成)。</summary>
        public double LifeSkillAdd { get; private set; }

        /// <summary>18401 回包落地:全量存 dict + 就地解析 key==2(挂机时长)/key==6(生活技能加成)。</summary>
        public void SetModuleBuffList(List<(int key, string values)> list)
        {
            _moduleBuffRaw.Clear();
            if (list == null) return;

            foreach ((int key, string values) it in list)
            {
                _moduleBuffRaw[it.key] = it.values;
                if (it.key == 2) ParseOnhookTime(it.values);
                else if (it.key == 6) ParseLifeSkillAdd(it.values);
            }
        }

        private void ParseOnhookTime(string values)
        {
            ErlangTerm term;
            try { term = ErlangParser.Parse(values); }
            catch (System.Exception ex) { GameLog.Warn("Skill", "18401 key=2 解析失败: {0}", ex.Message); return; }
            if (term?.Items == null) return;

            foreach (ErlangTerm tuple in term.Items)
            {
                if (!tuple.IsCollection || tuple.Items == null || tuple.Items.Count < 2) continue;
                if (tuple.Items[0].As<string>() != "onhook_time") continue;
                long onhookTime = tuple.Items[1].As<long>();
                OnhookMaxTimeSec = 20 * 3600 + onhookTime; // 对标老端 max_outline_time
                Shenxiao.Module.Core.OnHook.OnHookController.SetMaxOnlineTimeSec(OnhookMaxTimeSec);
                break;
            }
        }

        private void ParseLifeSkillAdd(string values)
        {
            if (double.TryParse(values, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v))
            {
                LifeSkillAdd = v;
            }
        }

        public string GetModuleBuffRaw(int key) => _moduleBuffRaw.TryGetValue(key, out string v) ? v : null;

        /// <summary>断线/登出清空(挂 SkillController.Dispose)。</summary>
        public void Clear()
        {
            LessPoint = 0;
            HasTalentInfo = false;
            _groups.Clear();
            CareerSkillBuffList.Clear();
            _moduleBuffRaw.Clear();
            OnhookMaxTimeSec = 0;
            LifeSkillAdd = 0;
        }
    }
}
