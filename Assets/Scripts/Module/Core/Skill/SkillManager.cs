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

        private readonly Dictionary<int, SkillVo> _mySkillList = new Dictionary<int, SkillVo>();

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
        }

        public SkillVo GetSkill(int skillId)
            => _mySkillList.TryGetValue(skillId, out SkillVo v) ? v : null;

        /// <summary>断线/登出清空(对标 ControllerHub.DisposeAll 链路)。</summary>
        public void Clear()
        {
            _mySkillList.Clear();
            ShortcutList = new List<SkillVo>();
            BarInfo = null;
            GameLog.Debug("Skill", "SkillManager cleared");
        }
    }
}
