using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
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

            // ----- 技能成长线(自动循环 轮3) -----
            RegisterProtocal(Proto.SKILL_UPGRADE, On21001);
            RegisterProtocal(Proto.TALENT_INFO, On21010);
            RegisterProtocal(Proto.TALENT_LEARN, On21011);
            RegisterProtocal(Proto.TALENT_RESET, On21012);
            RegisterProtocal(Proto.QUICKBAR_SAVE, On13008);
            RegisterProtocal(Proto.QUICKBAR_SWAP, On13010);
            RegisterProtocal(Proto.CAREER_SKILL_BUFF, On12093);
            RegisterProtocal(Proto.MODULE_BUFF_LIST, On18401);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_SKILL_SHORTCUT_CLICK, PressSkillHandler);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_SKILL_SHORTCUT_CLICK, PressSkillHandler);
            SkillManager.Instance.Clear();
            SkillTalentModel.Instance.Clear();
            AutoFightModel.Instance.Reset();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            // Reset 必须在任何 await 之前同步执行:压在 4 个 Addressables await 后面时,与登录点火链
            // (TaskController→FindNextAutoFightTask→SetAutoFightWeight(TASK))存在竞态——Reset 落在首次
            // 点火之后会把状态灭掉,后续同值 SetAutoFightWeight 早退不发事件,攻击环再无人拉起(死循环成因)。
            AutoFightModel.Instance.Reset();
            // 21002/13007 不依赖本地配置,先发不压在配置加载后面(配置挂起曾导致技能表永远不来 → 永久 no-skill)。
            SendFmt(Proto.SKILL_LIST);         // 21002 技能总表
            SendFmt(Proto.SKILL_SHORTCUT_BAR); // 13007 快捷栏

            // 技能名/等级图标来自 config_skill;快捷栏顺序来自 ConfigSkillUI(EnsureLoaded 幂等,对标 BagController/TaskController)。
            await SkillConfigs.EnsureLoaded();
            await SkillUIConfigs.EnsureLoaded();
            await SkillMovieConfigs.EnsureLoaded();
            await OtherFightConfigs.EnsureLoaded();
            // 对标老端 GAME_START 延迟2帧追加请求(21010/18401 无条件发,服务端按 turn 静默 skip,非错误码)。
            SendFmt(Proto.TALENT_INFO);        // 21010 天赋面板
            SendFmt(Proto.MODULE_BUFF_LIST);   // 18401 模块加成
            GameLog.Info("Skill", "request 21002/13007/21010/18401 (config_skill={0} ConfigSkillUI={1} SkillMovies={2} ConfigOtherFightInfo={3})",
                SkillConfigs.IsLoaded, SkillUIConfigs.IsLoaded, SkillMovieConfigs.IsLoaded, OtherFightConfigs.IsLoaded);
        }

        /// <summary>重拉技能总表(对标老端转职成功后 SkillManager.Fire(REQUEST_CCMD_EVENT,21002))。
        /// 供 <see cref="Shenxiao.Module.Core.TransferJob.TransferJobController"/> 转职成功级联调用。</summary>
        public void RequestSkillList() => SendFmt(Proto.SKILL_LIST);

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

        // ===================== 21001:职业(被动)技能升级 =====================

        /// <summary>请求升级技能(对标 SkillPassiveSubItem.ts:75-100 _gp_level_up 点击):发送前材料预校验——
        /// config_skill 该级 condition 里的 {goods,TypeId,Count} 项与背包持有数比对,不足则 toast 拦截不发包
        /// (老端该处 next_vo.condition.goods 缺 ErlangParser 解析步骤是死代码,本端用 SkillConfigs.TryGetGoodsCost
        /// 的正确解析实现同等意图,见 Proto.SKILL_UPGRADE 注释)。</summary>
        public void UpgradeSkill(int skillId)
        {
            SkillVo vo = SkillManager.Instance.GetSkill(skillId);
            if (vo == null)
            {
                GameLog.Info("Skill", "UpgradeSkill skill={0}:不在 mySkillList,拒绝发送", skillId);
                return;
            }
            if (vo.IsMaxLevel)
            {
                TipsManager.Toast("技能已满级");
                return;
            }
            if (vo.TryGetNextLevelGoodsCost(out int typeId, out int need))
            {
                long have = BagModel.Instance.GetTypeGoodsNum(typeId);
                if (have < need)
                {
                    GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
                    TipsManager.Toast("材料不足" + (basic != null ? "(" + basic.Name + ")" : ""));
                    GameLog.Info("Skill", "UpgradeSkill skill={0} 材料不足: typeId={1} have={2} need={3}", skillId, typeId, have, need);
                    return;
                }
            }

            SendFmt(Proto.SKILL_UPGRADE, "i", skillId);
            GameLog.Info("Skill", "send 21001 升级技能 skill={0}", skillId);
        }

        /// <summary>21001 回包(对标 SkillController.On21001):errcode!=1 → 显码降级 toast;==1 → Emit
        /// EVT_SKILL_LEVEL_UP(服务端会自动补推 21002 刷新列表,不在此手动重拉)。</summary>
        private void On21001(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int skillId = (int)r.ReadU32();
            GameLog.Info("Skill", "recv 21001 errcode={0} skill={1}", errcode, skillId);
            if (errcode != 1)
            {
                TipsManager.Toast("升级失败(" + errcode + ")");
                return;
            }
            EventDispatcher.Emit(GlobalEvent.EVT_SKILL_LEVEL_UP, skillId);
        }

        // ===================== 21010/21011/21012:天赋技能 =====================

        /// <summary>21010 天赋面板全量(对标老端 On21010 → SkillUIModel.SetInnateInfo):
        /// LessPoint:16, Len:16, {SkillType:8,Point:16,Len:16,{SkillId:32,SkillLv:16}×N}×N。</summary>
        private void On21010(NetReader r)
        {
            int lessPoint = r.ReadU16();
            int groupLen = r.ReadU16();
            var groups = new List<SkillTalentModel.TalentGroup>(groupLen);
            for (int i = 0; i < groupLen; i++)
            {
                var g = new SkillTalentModel.TalentGroup
                {
                    SkillType = r.ReadU8(),
                    Point = r.ReadU16(),
                };
                int skillLen = r.ReadU16();
                for (int s = 0; s < skillLen; s++)
                {
                    int skillId = (int)r.ReadU32();
                    int skillLv = r.ReadU16();
                    g.SkillLevels[skillId] = skillLv;
                }
                groups.Add(g);
            }
            SkillTalentModel.Instance.SetTalentInfo(lessPoint, groups);
            GameLog.Info("Skill", "recv 21010 天赋面板: lessPoint={0} groups={1}", lessPoint, groupLen);
            EventDispatcher.Emit(GlobalEvent.EVT_TALENT_INFO);
        }

        /// <summary>请求学习/加点天赋技能(对标 InnateUpInfoItem.ts:126):发送前走 SkillTalentModel.CanLearn
        /// 前置校验(满级/点数不足/point分支/pre_skill(2)前置),不满足则 toast 拦截不发包。</summary>
        public void LearnTalent(int skillId)
        {
            if (!SkillTalentModel.Instance.CanLearn(skillId, out string failReason))
            {
                TipsManager.Toast(failReason ?? "条件不足");
                GameLog.Info("Skill", "LearnTalent skill={0} 前置校验未过: {1}", skillId, failReason);
                return;
            }
            SendFmt(Proto.TALENT_LEARN, "i", skillId);
            GameLog.Info("Skill", "send 21011 学习天赋 skill={0}", skillId);
        }

        /// <summary>21011 回包(对标老端 On21011):errcode!=1 → 显码降级 toast;成功 → 补发 21010 刷全量
        /// (对标老端反查请求21010)+ Emit EVT_TALENT_LEARNED。</summary>
        private void On21011(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int skillId = (int)r.ReadU32();
            int skillLv = r.ReadU16();
            int lessPoint = r.ReadU16();
            GameLog.Info("Skill", "recv 21011 errcode={0} skill={1} lv={2} lessPoint={3}", errcode, skillId, skillLv, lessPoint);
            if (errcode != 1)
            {
                TipsManager.Toast("学习失败(" + errcode + ")");
                return;
            }
            SkillTalentModel.Instance.ApplyLearnResult(skillId, skillLv, lessPoint); // 对标规格"成功→更新模型"
            SendFmt(Proto.TALENT_INFO); // 对标老端成功后反查 21010 刷新全量(权威纠正)
            EventDispatcher.Emit(GlobalEvent.EVT_TALENT_LEARNED, skillId, skillLv);
        }

        /// <summary>请求重置天赋技能(对标 InnateSkillView.ts:114-135):**不做客户端拦截**,道具/货币够不够都发,
        /// 服务端 errcode 兜底(与 21011 的"发包前拦截"策略刻意不同,勿混)。</summary>
        public void ResetTalent()
        {
            SendFmt(Proto.TALENT_RESET);
            GameLog.Info("Skill", "send 21012 重置天赋");
        }

        /// <summary>21012 回包(对标老端 On21012):errcode!=1 → 显码降级 toast;==1 → toast「天赋重置成功」+
        /// Emit EVT_TALENT_RESET(服务端会主动重放 21010,本端无需手动重拉)。</summary>
        private void On21012(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int allPoint = r.ReadU16();
            GameLog.Info("Skill", "recv 21012 errcode={0} allPoint={1}", errcode, allPoint);
            if (errcode != 1)
            {
                TipsManager.Toast("重置失败(" + errcode + ")");
                return;
            }
            TipsManager.Toast("天赋重置成功");
            EventDispatcher.Emit(GlobalEvent.EVT_TALENT_RESET);
        }

        // ===================== 13008/13010:快捷栏保存/替换 =====================

        /// <summary>保存快捷栏(对标老端 SendFmtToGame(13008,"ccic",pos,type,skill_id,is_auto))。
        /// 老端/权威协议表均无 UI 触发源(role/*.ts 全仓库零调用),本轮只提供协议 API,无 UI 触发对标老端现状。</summary>
        public void SaveQuickbar(int pos, int type, int skillId, int isAuto)
        {
            SendFmt(Proto.QUICKBAR_SAVE, "ccic", pos, type, skillId, isAuto);
            GameLog.Info("Skill", "send 13008 保存快捷栏 pos={0} type={1} skill={2} isAuto={3}", pos, type, skillId, isAuto);
        }

        /// <summary>13008 回包:State:8(1成功/0失败,非errcode)。==1 → toast「保存成功」+ 重拉 13007。</summary>
        private void On13008(NetReader r)
        {
            int state = r.ReadU8();
            GameLog.Info("Skill", "recv 13008 state={0}", state);
            if (state == 1)
            {
                TipsManager.Toast("保存成功");
                SendFmt(Proto.SKILL_SHORTCUT_BAR);
                EventDispatcher.Emit(GlobalEvent.EVT_QUICKBAR_SAVED, false);
            }
            else
            {
                TipsManager.Toast("保存失败");
            }
        }

        /// <summary>交换快捷栏两个槽位(对标老端 SendFmtToGame(13010,"cc",pos1,pos2))。同上无 UI 触发源。</summary>
        public void SwapQuickbar(int pos1, int pos2)
        {
            SendFmt(Proto.QUICKBAR_SWAP, "cc", pos1, pos2);
            GameLog.Info("Skill", "send 13010 替换快捷栏 pos1={0} pos2={1}", pos1, pos2);
        }

        /// <summary>13010 回包:State:8(1成功/0失败,非errcode)。==1 → toast「替换成功」+ 重拉 13007。</summary>
        private void On13010(NetReader r)
        {
            int state = r.ReadU8();
            GameLog.Info("Skill", "recv 13010 state={0}", state);
            if (state == 1)
            {
                TipsManager.Toast("替换成功");
                SendFmt(Proto.SKILL_SHORTCUT_BAR);
                EventDispatcher.Emit(GlobalEvent.EVT_QUICKBAR_SAVED, true);
            }
            else
            {
                TipsManager.Toast("替换失败");
            }
        }

        // ===================== 12093:职业技能给予的 buff(纯被动推送) =====================

        /// <summary>12093 回包(对标老端 on12093):Len:16,{SkillId:32,SkillLv:16}×N。存 SkillTalentModel + Emit
        /// EVT_CAREER_SKILL_BUFF。HUD buff 图标行现成通道 MainUIBuffView.RefreshBuffList 需要 buff_cfgs 等价配置表
        /// (未加载)且挂载点 MainUIFlow.cs 当前基线脏(并行会话在改,不碰),本轮只落数据 + log,不接 HUD(见汇报)。</summary>
        private void On12093(NetReader r)
        {
            List<(int skillId, int skillLv)> list = r.ReadArray(rr => ((int)rr.ReadU32(), (int)rr.ReadU16()));
            SkillTalentModel.Instance.SetCareerSkillBuffList(list);
            GameLog.Info("Skill", "recv 12093 职业技能buff count={0}(HUD buff行未接:见 Proto.CAREER_SKILL_BUFF 注释)", list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_CAREER_SKILL_BUFF);
        }

        // ===================== 18401:模块加成效果列表 =====================

        /// <summary>18401 回包(对标老端 on18401):Len:16,{Key:32,ValuesStr}×N。全量存
        /// SkillTalentModel(内部就地解析 key==2 挂机时长/key==6 生活技能加成)+ Emit EVT_MODULE_BUFF_LIST。</summary>
        private void On18401(NetReader r)
        {
            List<(int key, string values)> list = r.ReadArray(rr => ((int)rr.ReadU32(), rr.ReadString()));
            SkillTalentModel.Instance.SetModuleBuffList(list);
            GameLog.Info("Skill", "recv 18401 模块加成 count={0} onhookMaxSec={1} lifeSkillAdd={2}",
                list.Count, SkillTalentModel.Instance.OnhookMaxTimeSec, SkillTalentModel.Instance.LifeSkillAdd);
            EventDispatcher.Emit(GlobalEvent.EVT_MODULE_BUFF_LIST);
        }
    }
}
