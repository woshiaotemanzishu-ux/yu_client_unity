using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene.Vo;
using Shenxiao.Module.Core.Skill;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 主角战斗驱动(对标老客户端 scene/Scene.ts 的 MainRoleAttackTarget / MainRoleAttackMonster /
    /// GetClickTarget / SetClickTarget)。目标 100% 来自真实 <see cref="SceneManager"/> 怪物表
    /// (12002/12007 下发的 <see cref="MonsterVo"/>)+ <see cref="RoleModel"/> 主角坐标,不造假怪/假伤/假 CD/假目标。
    ///
    /// 链路(对标老端 SkillManager.PressSkillHandler 的 else 分支 → Scene.GetInstance().MainRoleAttackTarget()):
    ///   1. <see cref="MainRoleAttackTarget"/>:取当前点击目标(<see cref="GetClickTarget"/>),无则按老端
    ///      FindTargets(attack_type_all, 1, ., 10000) 取最近可攻击怪(<see cref="FindNearestAttackableMonster"/>)。
    ///   2. 有怪 → <see cref="MainRoleAttackMonster"/>:范围判定(dist²≤range²,对标老端 GetDistance 平方比较)。
    ///        · 命中范围:朝向目标(MainRoleAgent.FaceTowardPixel,对标 SetDirection+DoStand)→ 本地释放边界。
    ///        · 超范围:自动接近(MainRoleAgent.MoveToNpc,对标老端 oper_mgr.StartTargetAction)→ 到达后释放。
    ///   3. 无任何可攻击怪 → 只记录真实阻塞(对标老端无 click_target 时 Fire(RELEASE_MAIN_SKILL) 空放;
    ///      空放/AOE 命中需 fight 系统,本轮不假放、不假伤)。
    ///
    /// 真实服务端攻击请求 20001(老端 FightController.ts:800 WriteBegin(20001):
    ///   h+i×N 怪 + h+l×N 人 + ihhh skill/x/y/angle);另有 20024 "c"(1 进/2 出战斗态)。
    /// 第 9 轮:释放边界由 <see cref="FightController"/> 逐字段真实发送 20024/20001:
    ///   · 单体技能(config_skill.mod==1):怪列表=[主目标]。
    ///   · 圆形 AOE(mod!=1 且 aoe_mode==1,如首杀技能 御剑一式 area=350/num=[1,4]):center=主目标坐标,
    ///     收集半径 area 内可攻击怪取 num[1] 只(对标 Scene.FindMonsters 圆形分支 + target_hiter 置首)。
    ///   · 直线/扇形 AOE(aoe_mode 2/3):需朝向几何收集链(未移植)→ 只记 blocker 不发(不猜范围)。
    /// x/y=主目标坐标、angle=0(见 FightController.ts:1187/1238/1351-1356)。
    /// 本地 <see cref="GlobalEvent.EVT_RELEASE_MAIN_SKILL"/> 边界保留(供 UI/表现订阅),与真实发包并存。
    /// </summary>
    public sealed class SceneCombat
    {
        public static readonly SceneCombat Instance = new SceneCombat();
        private SceneCombat() { }

        /// <summary>攻击范围下限(对标老端 SkillManager.GetCurrentAttackRange = Math.max(100, skill_distance*0.8) 的下限项)。
        /// 第 9 轮已接 config_skill 真实攻击距离(<see cref="AttackRange"/>):range = max(100, distance*0.8);
        /// distance 取自 config_skill lv_data[level-1].distance(缺省 50,对标 SkillVo.GetDistance)。</summary>
        private const float AttackRangeFloor = 100f;

        /// <summary>当前点击/锁定目标实例 id(对标老端 Scene.curr_click_target;0=无)。</summary>
        public int CurrentTargetId { get; private set; }

        /// <summary>对标老端 Scene.SetClickTarget:玩家点怪或自动寻敌后锁定目标。</summary>
        public void SetClickTarget(int monsterInstanceId) => CurrentTargetId = monsterInstanceId;

        /// <summary>对标老端 Scene.GetClickTarget:返回当前目标 VO;目标已死(hp≤0)或已被移除则清空返回 null。</summary>
        public MonsterVo GetClickTarget()
        {
            if (CurrentTargetId == 0) return null;
            MonsterVo vo = SceneManager.Instance.GetMonster(CurrentTargetId);
            if (vo == null || vo.Hp <= 0)
            {
                CurrentTargetId = 0;
                return null;
            }
            return vo;
        }

        /// <summary>
        /// 对标老端 Scene.MainRoleAttackTarget:目标型技能(career!=52 且 obj!=1)点击后由 SkillController 调入。
        /// 取当前点击目标 → 无则寻最近可攻击怪 → 有怪走 MainRoleAttackMonster;无怪只记真实阻塞。
        /// </summary>
        public void MainRoleAttackTarget(int skillId, int attackType)
        {
            MonsterVo target = GetClickTarget() ?? FindNearestAttackableMonster();
            if (target == null)
            {
                GameLog.Info("Combat",
                    "MainRoleAttackTarget skill={0}: 无当前点击目标且 SceneManager 无可攻击怪(monsters={1}) → 真实阻塞,不假放(对标老端无 click_target 分支)",
                    skillId, SceneManager.Instance.MonsterCount);
                return;
            }
            MainRoleAttackMonster(target, skillId, attackType);
        }

        /// <summary>
        /// 对标老端 Scene.MainRoleAttackMonster:命中范围(像素距离平方比较)→ 朝向 + 释放边界;
        /// 超范围 → 自动接近后释放。范围内/外都先锁定目标(对标 SetClickTarget)。
        /// </summary>
        private void MainRoleAttackMonster(MonsterVo mon, int skillId, int attackType)
        {
            if (mon == null || mon.Hp <= 0) return;

            SetClickTarget(mon.InstanceId);

            RoleModel role = RoleModel.Instance;
            float dx = mon.X - role.X;
            float dy = mon.Y - role.Y;
            float dist2 = dx * dx + dy * dy;
            float range = AttackRange(skillId);

            if (dist2 <= range * range)
            {
                // 命中范围:朝向目标(对标 main_role.SetDirection + DoStand)→ 释放边界。
                FaceTarget(mon);
                GameLog.Info("Combat",
                    "MainRoleAttackMonster skill={0} 目标怪 ins={1} type_id={2} 距离={3:F0}px ≤ range={4:F0}px → 朝向 + 释放边界",
                    skillId, mon.InstanceId, mon.TypeId, Math.Sqrt(dist2), range);
                ReleaseMainSkill(skillId, mon, attackType);
            }
            else
            {
                // 超范围:自动接近后释放(对标老端 oper_mgr.StartTargetAction(compress_id, attack_range, action_func))。
                ApproachThenRelease(mon, skillId, attackType, range, dist2);
            }
        }

        /// <summary>
        /// 寻最近可攻击怪(对标老端 Scene.FindTargets(attack_type_all, 1, ., 10000) 取 monster_list[0])。
        /// 过滤:非采集物(采集走 20008,不走技能释放)+ 服务端 can_attack==1 + hp&gt;0;按主角像素距离平方取最近。
        /// </summary>
        private MonsterVo FindNearestAttackableMonster()
        {
            RoleModel role = RoleModel.Instance;
            MonsterVo best = null;
            float bestDist2 = float.MaxValue;
            foreach (MonsterVo vo in SceneManager.Instance.AllMonsters)
            {
                if (vo.IsCollect || vo.CanAttack != 1 || vo.Hp <= 0) continue;
                float dx = vo.X - role.X;
                float dy = vo.Y - role.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 < bestDist2)
                {
                    bestDist2 = d2;
                    best = vo;
                }
            }
            return best;
        }

        /// <summary>朝向目标怪(对标老端 main_role.SetDirection)。无 3D 主角(headless/未装配)则跳过,不报错。</summary>
        private static void FaceTarget(MonsterVo mon)
        {
            MainRoleAgent agent = MainRoleAgent.Current;
            agent?.FaceTowardPixel(mon.X, mon.Y);
        }

        /// <summary>
        /// 超攻击范围:自动接近到攻击范围内后朝向 + 释放(对标老端 main_role.CanMove() → StartTargetAction)。
        /// 复用 MainRoleAgent.MoveToNpc(直线接近 + 撞墙滑行 + 卡死/超时兜底,绝不软锁)。
        /// 无 3D 主角时只记录真实边界,不假位移。
        /// </summary>
        private void ApproachThenRelease(MonsterVo mon, int skillId, int attackType, float range, float dist2)
        {
            MainRoleAgent agent = MainRoleAgent.Current;
            if (agent == null)
            {
                GameLog.Info("Combat",
                    "MainRoleAttackMonster skill={0} 目标怪 ins={1} 距离={2:F0}px > range={3:F0}px,需接近;无 MainRoleAgent(主角未装配)→ 记录边界(对标老端 StartTargetAction),不假位移",
                    skillId, mon.InstanceId, Math.Sqrt(dist2), range);
                return;
            }

            // 攻击范围(像素)换算到 MoveToNpc 的逻辑格到达半径(近似:像素/逻辑比 X)。MoveToNpc 自带卡死/超时兜底。
            float arriveLogic = range / SceneMapData.LogicRatioX;
            GameLog.Info("Combat",
                "MainRoleAttackMonster skill={0} 目标怪 ins={1} 距离={2:F0}px > range={3:F0}px → 自动接近(MoveToNpc)到攻击范围后释放(对标 StartTargetAction)",
                skillId, mon.InstanceId, Math.Sqrt(dist2), range);

            agent.MoveToNpc(mon.X, mon.Y, arriveLogic, () =>
            {
                MonsterVo cur = SceneManager.Instance.GetMonster(mon.InstanceId);
                if (cur == null || cur.Hp <= 0)
                {
                    if (CurrentTargetId == mon.InstanceId) CurrentTargetId = 0;
                    GameLog.Info("Combat", "接近完成但目标怪 ins={0} 已不在/已死 → 清目标,不释放", mon.InstanceId);
                    return;
                }
                FaceTarget(cur);
                ReleaseMainSkill(skillId, cur, attackType);
            });
        }

        /// <summary>
        /// 技能释放边界:① 本地等价事件 EVT_RELEASE_MAIN_SKILL(skillId, targetInstanceId)(对标老端
        /// Fire(FightEvent.RELEASE_MAIN_SKILL, ..., monster.compress_id),供 UI/表现订阅,保留不动);
        /// ② 真实服务端攻击请求(单体 20001/进战斗态 20024,经 <see cref="FightController"/>,见 <see cref="SendRealAttackOrBlock"/>)。
        /// 老端真实 20001 在技能动作帧 skill_damage_time 由 fight-movie 触发;本端无动作帧系统,在释放边界即发(时序差异,非字段差异)。
        /// </summary>
        private static void ReleaseMainSkill(int skillId, MonsterVo mon, int attackType)
        {
            EventDispatcher.Emit(GlobalEvent.EVT_RELEASE_MAIN_SKILL, skillId, mon.InstanceId);
            GameLog.Info("Combat",
                "RELEASE_MAIN_SKILL(本地) skill={0} target ins={1}(compress_id 等价) attackType={2}",
                skillId, mon.InstanceId, attackType);

            SendRealAttackOrBlock(skillId, mon);
        }

        /// <summary>
        /// 真实服务端攻击请求(对标老端 AttackRequest → onRoleRequestToFightHandler → 20001)。字段全部来自真实数据,不补假字段:
        ///   · 单体技能(config_skill.mod==1):怪列表=[主目标](对标 FightController.ts:1351-1356)。
        ///   · 圆形 AOE(mod!=1 且 aoe_mode==1,如首杀技能 御剑一式 area=350/num=[1,4]):center=主目标坐标
        ///     (FightController.ts:1187),收集 center 半径=config_skill area 内可攻击怪、按距离升序取 num[1] 只
        ///     (对标 Scene.FindMonsters 圆形分支 3348-3351 + target_hiter 置首 3351-1356)。
        ///   · 直线/扇形 AOE(aoe_mode 2/3):需主角朝向 + 直线/扇形几何(Scene.FindMonsters 3313-3347),未移植 → 只记 blocker。
        /// x/y=center=主目标坐标;angle=0(FightController.ts:1238);人列表本期空(PvE 首杀无敌方玩家;PvP FindRoles 链下一轮)。
        /// </summary>
        private static void SendRealAttackOrBlock(int skillId, MonsterVo primary)
        {
            int level = SkillManager.Instance.GetSkill(skillId)?.Level ?? 0;

            List<int> monsterIds;
            if (!SkillConfigs.IsAoe(skillId))
            {
                monsterIds = new List<int> { primary.InstanceId }; // 单体:仅主目标
            }
            else
            {
                int aoeMode = SkillConfigs.GetAoeMode(skillId);
                if (aoeMode != 1)
                {
                    GameLog.Info("Combat",
                        "20001 未发(AOE blocker): skill={0} aoe_mode={1}(直线/扇形)需主角朝向+直线/扇形几何收集链(未移植),不猜范围。主目标 ins={2} 已锁定。",
                        skillId, aoeMode, primary.InstanceId);
                    return;
                }

                int area = SkillConfigs.GetAreaForLevel(skillId, level);
                int maxMon = SkillConfigs.GetAttackNumForLevel(skillId, level)[1];
                if (maxMon <= 0) maxMon = 99; // 对标老端 att_num==0 → 99(不限)
                monsterIds = CollectCircleMonsters(primary, area, maxMon);
                GameLog.Info("Combat",
                    "圆形 AOE 收集: skill={0} center=主目标({1},{2}) 半径area={3} 上限num={4} → 命中 {5} 只: [{6}]",
                    skillId, primary.X, primary.Y, area, maxMon, monsterIds.Count, string.Join(",", monsterIds));
            }

            // 进战斗态(每段战斗一次)→ 真实攻击请求(经 FightController/NetManager,逐字段对齐老端)。
            FightController.Instance.EnterFightingState();
            FightController.Instance.SendMainSkillAttack(skillId, monsterIds, Array.Empty<long>(), primary.X, primary.Y, 0);

            // 第14轮真实伤害链闭合:普攻主技能(如 御剑一式 59100001)is_att=0/calc=0,服务端 mod_battle.erl 的
            // is_att=0 分支只回一个 damage=0 的"进战斗 engage 帧"(NoHurtDerList),真正扣血的是它的 combo 副技能
            // (如 59100002)is_att=1/calc=1。老端 fight-movie 在 comboSkills 时点对同目标补发副技能 20001(实测老端
            // 运行态 send 20001 后约 +300ms 再 send 20001);本端据 config_skill[skill].combo 链补发副技能(id/延迟全
            // 从配置读,不 hardcode),闭合真实伤害链。无 combo 链(真实主动技能/链尾)则不补发,行为不变。
            ScheduleComboFollowUp(skillId, monsterIds, primary.X, primary.Y);
        }

        /// <summary>
        /// engage 技能后按 config_skill combo 链补发"承载真实伤害的副技能"20001。对标老端 fight-movie comboSkills:
        /// 主技能(普攻 御剑一式 is_att=0/calc=0)只是进战斗 engage 帧(服务端恒 damage=0),combo 延迟后对同一目标
        /// 补发副技能(is_att=1/calc=1)才真实扣血。副技能 id / 延迟全部来自 <see cref="SkillConfigs.GetComboNext"/>
        /// (config_skill[skill].combo),不 hardcode。无 combo 链(真实主动技能 / 链尾)则不补发,行为不变。
        /// </summary>
        private static void ScheduleComboFollowUp(int engageSkillId, List<int> monsterIds, int x, int y)
        {
            (int comboSkillId, int delayMs) = SkillConfigs.GetComboNext(engageSkillId);
            if (comboSkillId <= 0) return; // 非 combo 普攻 / 已是链尾 → 不补发(行为与第13轮一致)
            _ = SendComboAfterDelayAsync(engageSkillId, comboSkillId, new List<int>(monsterIds), x, y, delayMs);
        }

        /// <summary>
        /// combo 延迟后对同一(仍存活)目标补发副技能 20001(承载真实伤害)。延迟对标服务端 combo next_time / 老端
        /// fight-movie comboSkills;发包前按 hp&gt;0 重过滤(对标老端 onRoleRequestToFightHandler 发包前过滤死亡怪)。
        /// fire-and-forget(非 async void);异常只记录不吞链路。
        /// </summary>
        private static async Task SendComboAfterDelayAsync(int engageSkillId, int comboSkillId, List<int> monsterIds, int x, int y, int delayMs)
        {
            try
            {
                if (delayMs > 0) await Task.Delay(delayMs);
                if (!NetManager.IsConnected) return; // 已断线则不补发(短会话窗口兜底)

                // 发包前重过滤:只留仍在场且 hp>0 的怪(engage 帧 damage=0 不会杀怪,通常全保留;防移除/位移)。
                var alive = new List<int>(monsterIds.Count);
                foreach (int ins in monsterIds)
                {
                    MonsterVo m = SceneManager.Instance.GetMonster(ins);
                    if (m != null && m.Hp > 0) alive.Add(ins);
                }
                if (alive.Count == 0) return;

                GameLog.Info("Combat",
                    "combo 副技能补发: engage={0} → combo={1} 延迟={2}ms 目标=[{3}](承载真实伤害,对标老端 fight-movie comboSkills 第二次 20001)",
                    engageSkillId, comboSkillId, delayMs, string.Join(",", alive));
                FightController.Instance.SendMainSkillAttack(comboSkillId, alive, Array.Empty<long>(), x, y, 0);
            }
            catch (Exception e)
            {
                GameLog.Warn("Combat", "combo 副技能补发异常 engage={0} combo={1}: {2}", engageSkillId, comboSkillId, e.Message);
            }
        }

        /// <summary>
        /// 圆形 AOE 怪物收集(对标 Scene.FindMonsters 圆形分支 3348-3351 + AttackRequest target_hiter 置首 3351-1356):
        /// 从真实 <see cref="SceneManager.AllMonsters"/> 取以 center(主目标坐标)为圆心、area 为半径内的可攻击怪
        /// (非采集 + can_attack==1 + hp&gt;0),按到 center 像素距离平方升序;主目标恒置首(距圆心 0,本就最近),
        /// 再补到 maxMon 上限。area&lt;=0 视为不限半径(对标老端 area_pw==null)。
        /// </summary>
        private static List<int> CollectCircleMonsters(MonsterVo primary, int area, int maxMon)
        {
            long areaPw = (long)area * area;
            var cands = new List<KeyValuePair<int, long>>();
            foreach (MonsterVo m in SceneManager.Instance.AllMonsters)
            {
                if (m.IsCollect || m.CanAttack != 1 || m.Hp <= 0) continue;
                long dx = m.X - primary.X, dy = m.Y - primary.Y;
                long d2 = dx * dx + dy * dy;
                if (area <= 0 || d2 <= areaPw) cands.Add(new KeyValuePair<int, long>(m.InstanceId, d2));
            }
            cands.Sort((a, b) => a.Value.CompareTo(b.Value));

            var ids = new List<int> { primary.InstanceId }; // target_hiter 置首(对标 FightController.ts:1351)
            foreach (KeyValuePair<int, long> c in cands)
            {
                if (ids.Count >= maxMon) break;
                if (c.Key == primary.InstanceId) continue;
                ids.Add(c.Key);
            }
            return ids;
        }

        /// <summary>
        /// 真实攻击范围(对标老端 SkillManager.GetCurrentAttackRange = max(100, skill_distance*0.8))。
        /// skill_distance 取 config_skill lv_data[level-1].distance(缺省 50);level 取真实已学等级。
        /// </summary>
        private static float AttackRange(int skillId)
        {
            int level = SkillManager.Instance.GetSkill(skillId)?.Level ?? 0;
            int distance = SkillConfigs.GetDistanceForLevel(skillId, level);
            return Math.Max(AttackRangeFloor, distance * 0.8f);
        }
    }
}
