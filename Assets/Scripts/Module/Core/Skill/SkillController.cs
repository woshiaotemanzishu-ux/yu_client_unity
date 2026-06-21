using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoFight;

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
    ///   本轮只记录到事件边界(真实释放 Scene.MainRoleAttackTarget 未移植),不硬造攻击 —— 下一轮战斗链路接。
    ///
    /// 老端 GAME_START 还批量请求 21101/21010/18401(远古奥术/天赋/模块加成),属深水区(P4 只记录),本轮不请求不解析。
    /// </summary>
    public sealed class SkillController : BaseController
    {
        public static readonly SkillController Instance = new SkillController();
        private SkillController() { }

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
            AutoFightModel.Instance.Reset();

            // 对标 SkillController GAME_START 延迟批量请求;Unity 这里 await 配置即天然延迟,连接已就绪后再发。
            SendFmt(Proto.SKILL_LIST);         // 21002 技能总表
            SendFmt(Proto.SKILL_SHORTCUT_BAR); // 13007 快捷栏
            GameLog.Info("Skill", "request 21002/13007 (config_skill={0} ConfigSkillUI={1})",
                SkillConfigs.IsLoaded, SkillUIConfigs.IsLoaded);
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
            // 对标老端 PressSkillHandler:CanAttack → setCurrentSkillId → Scene.MainRoleAttackTarget / RELEASE_MAIN_SKILL / PartnerUpdateFight。
            // 这条战斗释放链路(寻敌/朝向/命中/特效)本轮未移植,只到事件边界,不硬造攻击。下一轮打怪链路接。
            GameLog.Info("Skill", "SKILL_SHORTCUT_CLICK skill={0} type={1} → 真实释放(Scene.MainRoleAttackTarget)未移植,下一轮战斗链路接", skillId, attackType);
        }
    }
}
