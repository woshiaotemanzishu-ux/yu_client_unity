using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Scene;

namespace Shenxiao.Module.Core.Skill
{
    /// <summary>
    /// 技能协议层(对标老端 skill/SkillController.ts)。
    ///
    /// 链路:进游戏(EVT_GAME_START)→ 预载 config_skill + ConfigSkillUI → 请求 21002(技能总表)+ 13007(快捷栏)。
    ///   · On21002 → SkillManager.CreateSkillList(建 mySkillList + shortcutList)→ 发 EVT_SKILL_LIST_UPDATED。
    ///   · On13007 → 解析 {pos,type,skill_id,is_auto} → SkillManager.SetBarInfo → 发 EVT_SKILL_BAR_UPDATED。
    /// 技能列表/快捷栏/图标 100% 来自真实协议 21002/13007 + config_skill/ConfigSkillUI,无硬编码技能 id。
    ///
    /// 点击技能槽 → MainUISkillItem 发 EVT_SKILL_SHORTCUT_CLICK → 本控制器 PressSkillHandler:
    ///   CanAttack 子集闸 + career/obj 三分支;目标型技能进 SceneCombat.MainRoleAttackTarget(真实 SceneManager 怪物寻敌 →
    ///   范围/朝向/接近 → 本地 RELEASE_MAIN_SKILL 边界)。真实 20001 攻击请求(fight-movie/AOE 链)= 下一轮 blocker。
    ///
    /// 老端 GAME_START 还批量请求 21101/21010/18401(远古奥术/天赋/模块加成),属深水区(P4 只记录),本轮不请求不解析。
    /// </summary>
    public sealed class SkillController : BaseController
    {
        public static readonly SkillController Instance = new SkillController();
        private SkillController() { }

        /// <summary>伙伴技能职业号(对标老端 PressSkillHandler 的 carrer==52 分支)。</summary>
        private const int CAREER_PARTNER = 52;

        protected override void Register()
        {
            RegisterProtocal(Proto.SKILL_LIST, On21002);
            RegisterProtocal(Proto.SKILL_SHORTCUT_BAR, On13007);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_SKILL_SHORTCUT_CLICK, PressSkillHandler);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_SKILL_SHORTCUT_CLICK, PressSkillHandler);
            SkillManager.Instance.Clear();
            AutoFightModel.Instance.Reset();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            // 技能名/等级图标来自 config_skill;快捷栏顺序来自 ConfigSkillUI(EnsureLoaded 幂等,对标 BagController/TaskController)。
            await SkillConfigs.EnsureLoaded();
            await SkillUIConfigs.EnsureLoaded();
            await SkillMovieConfigs.EnsureLoaded();
            await OtherFightConfigs.EnsureLoaded();
            AutoFightModel.Instance.Reset();

            // 对标 SkillController GAME_START 延迟批量请求;Unity 这里 await 配置即天然延迟,连接已就绪后再发。
            SendFmt(Proto.SKILL_LIST);         // 21002 技能总表
            SendFmt(Proto.SKILL_SHORTCUT_BAR); // 13007 快捷栏
            GameLog.Info("Skill", "request 21002/13007 (config_skill={0} ConfigSkillUI={1} SkillMovies={2} ConfigOtherFightInfo={3})",
                SkillConfigs.IsLoaded, SkillUIConfigs.IsLoaded, SkillMovieConfigs.IsLoaded, OtherFightConfigs.IsLoaded);
        }

        // ===================== 21002:技能总表 =====================

        private void On21002(NetReader r)
        {
            SkillManager.Instance.CreateSkillList(r);
            GameLog.Info("Skill", "recv 21002: mySkills={0} shortcut={1}",
                SkillManager.Instance.MySkillCount, SkillManager.Instance.ShortcutList.Count);
        }

        // ===================== 13007:快捷栏 =====================

        private void On13007(NetReader r)
        {
            int len = r.ReadU16();
            var list = new List<SkillManager.SkillBarInfo>(len);
            for (int i = 0; i < len; i++)
            {
                list.Add(new SkillManager.SkillBarInfo
                {
                    Pos = r.ReadU8(),
                    Type = r.ReadU8(),
                    SkillId = (int)r.ReadU32(),
                    IsAuto = r.ReadU8(),
                });
            }
            SkillManager.Instance.SetBarInfo(list);
            GameLog.Info("Skill", "recv 13007: barInfo={0}", len);
        }

        // ===================== 点击技能槽(对标 SkillManager.PressSkillHandler 边界)=====================

        private void PressSkillHandler(int skillId, int attackType)
        {
            // 对标老端 SkillManager.PressSkillHandler:CanAttack → setCurrentSkillId → 按职业/选取模式分三支。
            // 第一步 CanAttack(skill_id, true):老端真链含 pose(跳/被击/死)/眩晕/幽灵/僵直/CD 等(主角+场景+战斗系统),
            // 本轮未移植 → 只做可支持子集:技能必须真实在 21002 mySkillList 且已学(level>0)。其余阻塞记差异,不假判可攻。
            SkillVo vo = SkillManager.Instance.GetSkill(skillId);
            if (vo == null)
            {
                GameLog.Info("Skill", "PressSkill skill={0}:不在 21002 mySkillList → 不释放(对标 CanAttack『取不到技能信息』)", skillId);
                return;
            }
            if (vo.Locked)
            {
                GameLog.Info("Skill", "PressSkill skill={0}:未学 level=0 → 不释放(对标 UpdateLockState/CanAttack)", skillId);
                return;
            }

            // 对标老端 setCurrentSkillId 后按 career / GetSelectType(obj) 分支(真实读 config_skill,不硬编码):
            int career = vo.Career;
            int selectType = vo.SelectType; // obj:1自己 2最近敌方 3最近队友
            if (career == CAREER_PARTNER)
            {
                GameLog.Info("Skill", "PressSkill skill={0} 伙伴技能(career=52)→ 老端 Scene.PartnerUpdateFight 边界(伙伴战斗系统未移植,差异记录)", skillId);
            }
            else if (selectType == 1)
            {
                GameLog.Info("Skill", "PressSkill skill={0} 自我释放(obj=1)→ 老端 Fire(RELEASE_MAIN_SKILL, type={1}) 边界(技能释放/特效链路未移植,差异记录)", skillId, attackType);
            }
            else
            {
                // 主线最常见:职业输出技能(obj=2 最近敌方 / obj=3 最近队友),对应老端 else 分支 Scene.GetInstance().MainRoleAttackTarget()。
                GameLog.Info("Skill", "PressSkill skill={0} 目标技能(obj={1})→ SceneCombat.MainRoleAttackTarget(真实 SceneManager 怪物寻敌)", skillId, selectType);
                SceneCombat.Instance.MainRoleAttackTarget(skillId, attackType);
            }
        }
    }
}
