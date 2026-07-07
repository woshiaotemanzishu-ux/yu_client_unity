using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Skill
{
    /// <summary>
    /// 技能数据模型(对标老端 skill/SkillManager.ts 的最小子集)。
    /// 持有 mySkillList(21002 全表)+ shortcutList(首屏 4 槽)+ skill_bar_info(13007 快捷栏)。
    /// 单例 + 全局事件(对标 AutoBrushModel 范式),视图监听 EVT_SKILL_LIST_UPDATED 读 ShortcutList 刷新。
    ///
    /// shortcutList 规则严格对齐老端 UpdateShortCutList:
    ///   ConfigSkillUI.carrerSkillList[career] 去掉 common(普攻),取 mySkillList 对应 SkillVo,按 id 升序。
    /// 缺 ConfigSkillUI 时回落:用 21002 全表去普攻,按 id 升序(仍是真实服务端数据,不硬编码)。
    /// </summary>
    public sealed class SkillManager
    {
        public static readonly SkillManager Instance = new SkillManager();
        private SkillManager() { }

        /// <summary>技能攻击类型 ONLY_FIRE_ATTACK(对标 SkillManager.SKILL_ATTACK_TYPE.ONLY_FIRE_ATTACK=1)。</summary>
        public const int ONLY_FIRE_ATTACK = 1;

        /// <summary>13007 快捷栏一项。</summary>
        public sealed class SkillBarInfo
        {
            public int Pos;
            public int Type;
            public int SkillId;
            public int IsAuto;
        }

        /// <summary>
        /// 普攻均值僵直兜底(毫秒)。对标老端 career1 普攻链 59100001/3/5/8 的 config_skill.time=567/433/1133/1267
        /// 均值≈850ms(见 SkillConfigs.GetAnimTimeMs 注释的数据锚点)。仅当 config_skill 缺 time 字段
        /// (理论上不会发生,真实表都有)时兜底,不凭空造更快的数字。
        /// </summary>
        private const int DefaultRigidityMs = 850;

        private readonly Dictionary<int, SkillVo> _mySkillList = new Dictionary<int, SkillVo>();
        private int _autoFightShortcutIndex;

        /// <summary>技能僵直结束时间(Environment.TickCount 毫秒,对标老端 SkillManager.skill_rigidity)。</summary>
        private int _rigidityEndTick;

        /// <summary>首屏技能槽(去普攻、按 id 升序)。视图据此铺 4 槽。</summary>
        public List<SkillVo> ShortcutList { get; private set; } = new List<SkillVo>();

        /// <summary>13007 快捷栏配置(可空,type==2 项可覆盖默认顺序)。</summary>
        public List<SkillBarInfo> BarInfo { get; private set; }

        public int MySkillCount => _mySkillList.Count;

        // ===================== 21002:技能总表 → CreateSkillList =====================

        /// <summary>对标 On21002 → CreateSkillList:读 len:h + {skill_id:i, skill_lv:h}×len,建 mySkillList,刷 shortcutList。</summary>
        public void CreateSkillList(NetReader r)
        {
            _mySkillList.Clear();
            int len = r.ReadU16();
            for (int i = 0; i < len; i++)
            {
                int skillId = (int)r.ReadU32();
                int skillLv = r.ReadU16();
                // 对标 getSkillFromConfig != null 才入表;config_skill 未载时不过滤(直接用真实 21002 数据)。
                if (SkillConfigs.IsLoaded && !SkillConfigs.Has(skillId)) continue;
                _mySkillList[skillId] = new SkillVo(skillId) { Level = skillLv };
            }

            UpdateShortcutList();
            EventDispatcher.Emit(GlobalEvent.EVT_SKILL_LIST_UPDATED);
        }

        // ===================== 13007:快捷栏 =====================

        /// <summary>对标 on13007:按 pos 升序存 skill_bar_info,刷 shortcutList,发 EVT_SKILL_BAR_UPDATED。</summary>
        public void SetBarInfo(List<SkillBarInfo> info)
        {
            info?.Sort((a, b) => a.Pos - b.Pos);
            BarInfo = info;
            UpdateShortcutList();
            EventDispatcher.Emit(GlobalEvent.EVT_SKILL_BAR_UPDATED);
        }

        // ===================== shortcutList(对标 UpdateShortCutList)=====================

        public void UpdateShortcutList()
        {
            var list = new List<SkillVo>();
            int career = RoleModel.Instance.Career;

            List<SkillUIConfigs.CareerSkill> careerSkills =
                SkillUIConfigs.IsLoaded ? SkillUIConfigs.GetCareerSkills(career) : null;

            if (careerSkills != null && careerSkills.Count > 0)
            {
                // 主路径:ConfigSkillUI.carrerSkillList[career] 去 common(普攻),只取真实在 21002 mySkillList 里的技能
                // (对标老端 push mySkillList[skill_id]:未学/未下发的不入槽,不伪造)。普通职业的常规技能服务端会下发;
                // 神明等特殊技能(如 career1 的 59370006 神明威压)未解锁则不在 mySkillList,自然不占槽 → 常规为 4 槽。
                foreach (SkillUIConfigs.CareerSkill cs in careerSkills)
                {
                    if (cs.Common || cs.SkillId <= 0) continue;
                    if (_mySkillList.TryGetValue(cs.SkillId, out SkillVo v)) list.Add(v);
                }
            }
            else
            {
                // 回落:无 ConfigSkillUI → 21002 全表去普攻(仍是真实数据,记差异)。
                foreach (SkillVo vo in _mySkillList.Values)
                {
                    if (SkillConfigs.IsLoaded && SkillConfigs.IsNormal(vo.Id)) continue;
                    list.Add(vo);
                }
            }

            list.Sort((a, b) => a.Id - b.Id); // 升序(对标 sort a.id < b.id)
            ShortcutList = list;
            if (_autoFightShortcutIndex >= ShortcutList.Count) _autoFightShortcutIndex = 0;
        }

        public SkillVo GetSkill(int skillId)
            => _mySkillList.TryGetValue(skillId, out SkillVo v) ? v : null;

        /// <summary>
        /// 释放技能后设置僵直(对标老端 SkillManager.SetSkillRigidity,调用点对标 FightMovieInfo.ts:191
        /// local_SkillManager.SetSkillRigidity(rigidity_time),rigidity_time=pre_swing+spell_time+back_swing)。
        /// 本端无逐帧动作演出系统,在释放边界(SceneCombat.ReleaseMainSkill)直接用 config_skill.time 作僵直时长
        /// (同一数值来源,见 SkillConfigs.GetAnimTimeMs)。取更长值不缩短(对标老端"僵直只能变长不能被新请求缩短")。
        /// </summary>
        public void SetSkillRigidity(int durationMs)
        {
            if (durationMs <= 0) durationMs = DefaultRigidityMs;
            int end = System.Environment.TickCount + durationMs;
            if (_rigidityEndTick != 0 && end < _rigidityEndTick) return; // 对标老端: rigidity_end_time < this.skill_rigidity 则不覆盖
            _rigidityEndTick = end;
        }

        /// <summary>是否仍在攻击僵直中(对标老端 SkillManager.IsInRigidity)。AutoFightController 用它替代固定 tick 间隔节流攻击。</summary>
        public bool IsInRigidity() => _rigidityEndTick != 0 && Environment_TickDiff(_rigidityEndTick) > 0;

        // ── 技能 CD(对标老端 SkillManager.ResetSkill → SkillVo.startCD + Fire(START_SKILL_CD)) ──────────
        // 数据源 = config_skill lv_data[level-1].cd(毫秒,SkillConfigs.GetCdMsForLevel);状态 = 释放时刻 tick。
        // 消费方:MainUISkillItem 每帧轮询画时钟遮罩+倒计时(对标 CirCleCdView 帧驱动);
        //         GetNextCombatSkill/GetNextAutoFightSkill 跳过 CD 中技能(对标老端自动战斗按 GetLeftCD()==0 选技)。
        private readonly Dictionary<int, (int startTick, int cdMs)> _skillCd = new Dictionary<int, (int, int)>();

        /// <summary>技能释放 → 进 CD(对标老端 ResetSkill;调用点 SceneCombat.ReleaseMainSkill,自动/手动同路)。cd=0 无记录。</summary>
        public void ResetSkill(int skillId)
        {
            int level = GetSkill(skillId)?.Level ?? 0;
            int cdMs = SkillConfigs.IsLoaded ? SkillConfigs.GetCdMsForLevel(skillId, level) : 0;
            if (cdMs <= 0) { _skillCd.Remove(skillId); return; }
            _skillCd[skillId] = (System.Environment.TickCount, cdMs);
        }

        /// <summary>剩余 CD 毫秒(对标老端 SkillVo.GetLeftCD);0=可用。TickCount 差值防回绕(同僵直约定)。</summary>
        public int GetCdLeftMs(int skillId)
        {
            if (!_skillCd.TryGetValue(skillId, out (int startTick, int cdMs) cd)) return 0;
            int elapsed = System.Environment.TickCount - cd.startTick;
            if (elapsed < 0 || elapsed >= cd.cdMs)
            {
                _skillCd.Remove(skillId); // 到点即清(防表膨胀;elapsed<0=回绕,按已过处理)
                return 0;
            }
            return cd.cdMs - elapsed;
        }

        /// <summary>本次 CD 总时长毫秒(遮罩分母);无在途 CD 返回 0。</summary>
        public int GetCdTotalMs(int skillId)
            => _skillCd.TryGetValue(skillId, out (int startTick, int cdMs) cd) ? cd.cdMs : 0;

        // TickCount 会在约 24.9 天回绕;用差值判断而非直接比较,避免回绕瞬间误判(老端 Status.NowTime 是秒级浮点无此问题,
        // 这里补一个防回绕的差值封装,仍是"未到点=true"的同一语义)。
        private static int Environment_TickDiff(int endTick) => endTick - System.Environment.TickCount;

        public SkillVo GetNextAutoFightSkill()
        {
            if (ShortcutList == null || ShortcutList.Count == 0) return null;

            int count = ShortcutList.Count;
            for (int i = 0; i < count; i++)
            {
                int index = (_autoFightShortcutIndex + i) % count;
                SkillVo skill = ShortcutList[index];
                if (skill == null || skill.Locked) continue;
                if (GetCdLeftMs(skill.Id) > 0) continue; // CD 中跳过(对标老端自动战斗按 GetLeftCD()==0 选技)

                _autoFightShortcutIndex = (index + 1) % count;
                return skill;
            }

            return null;
        }

        /// <summary>Combat skill selection: shortcut skill first, then a learned normal/active target skill.</summary>
        public SkillVo GetNextCombatSkill()
        {
            int shortcutCount = ShortcutList?.Count ?? 0;
            for (int i = 0; i < shortcutCount; i++)
            {
                int index = (_autoFightShortcutIndex + i) % shortcutCount;
                SkillVo skill = ShortcutList[index];
                if (!IsCombatSkill(skill)) continue;
                if (GetCdLeftMs(skill.Id) > 0) continue; // CD 中跳过(普攻 cd=0 恒可用,循环不会空转)

                _autoFightShortcutIndex = (index + 1) % shortcutCount;
                return skill;
            }

            SkillVo bestNormal = null;
            SkillVo bestActive = null;
            foreach (SkillVo vo in _mySkillList.Values)
            {
                if (!IsCombatSkill(vo)) continue;
                if (GetCdLeftMs(vo.Id) > 0) continue; // CD 中跳过
                if (!SkillConfigs.IsLoaded)
                {
                    if (bestActive == null || vo.Id < bestActive.Id) bestActive = vo;
                    continue;
                }

                bool normal = SkillConfigs.IsNormal(vo.Id);
                bool targetSkill = SkillConfigs.GetSelectType(vo.Id) == 2 || SkillConfigs.GetAttObj(vo.Id) == 2;
                bool activeSkill = SkillConfigs.GetSkillType(vo.Id) == 1;
                if (normal)
                {
                    if (bestNormal == null || vo.Id < bestNormal.Id) bestNormal = vo;
                }
                else if (targetSkill && activeSkill)
                {
                    if (bestActive == null || vo.Id < bestActive.Id) bestActive = vo;
                }
            }

            return bestNormal ?? bestActive;
        }

        private static bool IsCombatSkill(SkillVo vo)
        {
            if (vo == null || vo.Locked) return false;
            if (!SkillConfigs.IsLoaded) return true;
            if (SkillConfigs.GetCareer(vo.Id) == 52) return false;
            if (SkillConfigs.IsNormal(vo.Id)) return true;
            return SkillConfigs.GetSkillType(vo.Id) == 1
                && (SkillConfigs.GetSelectType(vo.Id) == 2 || SkillConfigs.GetAttObj(vo.Id) == 2);
        }

        /// <summary>断线/登出清空(对标 ControllerHub.DisposeAll 链路)。</summary>
        public void Clear()
        {
            _mySkillList.Clear();
            ShortcutList = new List<SkillVo>();
            BarInfo = null;
            _autoFightShortcutIndex = 0;
            _skillCd.Clear();
            _rigidityEndTick = 0;
            GameLog.Debug("Skill", "SkillManager cleared");
        }
    }
}
