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

        /// <summary>
        /// 寻怪/发起接近的最小间隔,毫秒(对标老端 Scene.ts:142 auto_find_and_attack_interval=0.4,
        /// 用于 Scene.ts:1744 `NowTime - last_find_target_time < 0.4` 节流)。AutoFightController 的 tick
        /// 从 500ms 提到 100ms 后(对齐老端 UpdateAutoFight 轮询频率),若不加这道闸,接近中的目标每 100ms
        /// 就会被 <see cref="ApproachThenRelease"/> 重新 MoveToNpc 一次,把 MainRoleAgent 的卡死/超时兜底计时
        /// (AutoStuckSeconds/AutoMoveTimeout)反复清零而不到点——变相软锁,比原先 500ms 版更容易触发。
        /// </summary>
        private const int FindAndAttackIntervalMs = 400;

        /// <summary>上次发起"寻怪后接近"的时间(Environment.TickCount 毫秒;对标老端 last_find_target_time)。</summary>
        private int _lastFindTargetTick;

        /// <summary>当前点击/锁定目标实例 id(对标老端 Scene.curr_click_target;0=无)。</summary>
        public int CurrentTargetId { get; private set; }

        /// <summary>对标老端 Scene.SetClickTarget:玩家点怪或自动寻敌后锁定目标。</summary>
        public void SetClickTarget(int monsterInstanceId) => CurrentTargetId = monsterInstanceId;

        public bool TrySetNearestMonsterByType(int monsterTypeId, int centerX, int centerY)
        {
            if (monsterTypeId <= 0) return false;
            MonsterVo target = FindNearestAttackableMonster(monsterTypeId, centerX, centerY);
            if (target == null) return false;

            SetClickTarget(target.InstanceId);
            GameLog.Info("Combat", "task target locked type={0} ins={1} pos=({2},{3})",
                monsterTypeId, target.InstanceId, target.X, target.Y);
            return true;
        }

        public bool TrySetNearestAttackableMonster(int centerX, int centerY)
        {
            MonsterVo target = FindNearestAttackableMonster(0, centerX, centerY);
            if (target == null) return false;

            SetClickTarget(target.InstanceId);
            GameLog.Info("Combat", "auto-brush target locked ins={0} type={1} pos=({2},{3})",
                target.InstanceId, target.TypeId, target.X, target.Y);
            return true;
        }

        /// <summary>Scene object click entry: lock a real monster instance and release a learned combat skill.</summary>
        public bool MainRoleAttackMonster(int monsterInstanceId, int skillId = 0, int attackType = SkillManager.ONLY_FIRE_ATTACK)
        {
            MonsterVo target = SceneManager.Instance.GetMonster(monsterInstanceId);
            if (target == null)
            {
                if (CurrentTargetId == monsterInstanceId) CurrentTargetId = 0;
                GameLog.Info("Combat", "click attack blocked: monster ins={0} no longer exists", monsterInstanceId);
                return false;
            }

            if (target.IsCollect)
            {
                SetClickTarget(target.InstanceId);
                GameLog.Warn("Combat", "click attack blocked: ins={0} type={1} is collect target", target.InstanceId, target.TypeId);
                return false;
            }

            if (target.CanAttack != 1 || target.Hp <= 0)
            {
                SetClickTarget(target.InstanceId);
                GameLog.Info("Combat", "click attack blocked: ins={0} type={1} canAttack={2} hp={3}",
                    target.InstanceId, target.TypeId, target.CanAttack, target.Hp);
                return false;
            }

            if (skillId <= 0)
            {
                SkillVo skill = SkillManager.Instance.GetNextCombatSkill();
                skillId = skill?.Id ?? 0;
            }

            if (skillId <= 0)
            {
                SetClickTarget(target.InstanceId);
                GameLog.Warn("Combat", "click attack blocked: no learned combat skill for target ins={0}", target.InstanceId);
                return false;
            }

            MainRoleAttackMonster(target, skillId, attackType);
            return true;
        }

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
                // 命中范围:朝向目标(对标 main_role.SetDirection + DoStand)→ 怪回头面向玩家 → 释放边界。
                FaceTarget(mon);
                MonsterRenderer.FaceMonster(mon.InstanceId, role.X, role.Y); // BOSS 面向玩家(对标老端攻击帧 SetDirection)
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
            return FindNearestAttackableMonster(0, role.X, role.Y);
        }

        private MonsterVo FindNearestAttackableMonster(int monsterTypeId, int centerX, int centerY)
        {
            MonsterVo best = null;
            float bestDist2 = float.MaxValue;
            foreach (MonsterVo vo in SceneManager.Instance.AllMonsters)
            {
                if (vo.IsCollect || vo.CanAttack != 1 || vo.Hp <= 0) continue;
                if (monsterTypeId > 0 && vo.TypeId != monsterTypeId) continue;
                float dx = vo.X - centerX;
                float dy = vo.Y - centerY;
                float d2 = dx * dx + dy * dy;
                if (d2 < bestDist2)
                {
                    bestDist2 = d2;
                    best = vo;
                }
            }
            return best;
        }

        /// <summary>
        /// 寻最近采集物(对标老端 Scene.GetNearestMonsterByTypeId,采集任务分支):IsCollect + 指定 type_id,
        /// 按中心点像素距离平方取最近。可采性(CanCollect 冷却 / 占用)由服务端在 20008 回包校验,这里不预过滤
        /// (与老端 GetNearestMonsterByTypeId 一致:先取怪,采集时服务端再裁决)。typeId&lt;=0 表示任意采集物。
        /// </summary>
        public MonsterVo FindNearestCollectMonster(int monsterTypeId, int centerX, int centerY)
        {
            MonsterVo best = null;
            float bestDist2 = float.MaxValue;
            foreach (MonsterVo vo in SceneManager.Instance.AllMonsters)
            {
                if (!vo.IsCollect) continue;
                if (monsterTypeId > 0 && vo.TypeId != monsterTypeId) continue;
                float dx = vo.X - centerX;
                float dy = vo.Y - centerY;
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

            // 节流(对标老端 Scene.ts:1744 `NowTime - last_find_target_time < auto_find_and_attack_interval` return):
            // 接近尚在进行时,不用每个 100ms tick 都重新 MoveToNpc 一次(MainRoleAgent 的移动本身按 Update() 逐帧推进,
            // 不需要这里反复驱动;重复调用只会清零卡死/超时计时器)。已到点才允许重发一次接近指令。
            int nowTick = Environment.TickCount;
            if (_lastFindTargetTick != 0 && nowTick - _lastFindTargetTick < FindAndAttackIntervalMs) return;
            _lastFindTargetTick = nowTick;

            // 站位修正(对标老端 StartTargetAction:停在玩家"接近方向"那一侧的攻击距离处,而非走到 BOSS 中心):
            // 原来 MoveToNpc(BOSS 中心, 半径=range/LogicRatioX) 是各向同性大圆,玩家从哪边进圆就停哪边 → 可能停到 BOSS 背后。
            // 改为:沿"玩家→BOSS"方向,停在距 BOSS 中心 stopDist 像素处的【BOSS 正前方站位点】,到达半径收紧。
            RoleModel role = RoleModel.Instance;
            float dx = mon.X - role.X;
            float dy = mon.Y - role.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            float stopDist = range * 0.85f;          // 留余量,停下后仍在攻击范围内(下轮 dist2<=range² 成立)
            if (stopDist < 60f) stopDist = 60f;      // 下限:防贴脸/穿模
            if (stopDist > range) stopDist = range;
            float approachX = mon.X, approachY = mon.Y;
            if (dist > 0.01f)
            {
                approachX = mon.X - dx / dist * stopDist; // BOSS 正前方(玩家这一侧)
                approachY = mon.Y - dy / dist * stopDist;
            }

            const float arriveLogic = 0.6f;          // 逻辑格,收紧到达半径(只判到没到站位点)
            GameLog.Info("Combat",
                "MainRoleAttackMonster skill={0} ins={1} dist={2:F0}px > range={3:F0}px → 接近 BOSS 正面站位点({4:F0},{5:F0}) stopDist={6:F0}",
                skillId, mon.InstanceId, dist, range, approachX, approachY, stopDist);

            agent.MoveToNpc(approachX, approachY, arriveLogic, () =>
            {
                MonsterVo cur = SceneManager.Instance.GetMonster(mon.InstanceId);
                if (cur == null || cur.Hp <= 0)
                {
                    if (CurrentTargetId == mon.InstanceId) CurrentTargetId = 0;
                    GameLog.Info("Combat", "接近完成但目标怪 ins={0} 已不在/已死 → 清目标,不释放", mon.InstanceId);
                    return;
                }
                FaceTarget(cur);
                MonsterRenderer.FaceMonster(cur.InstanceId, role.X, role.Y); // BOSS 面向玩家
                ReleaseMainSkill(skillId, cur, attackType);
            });
        }

        /// <summary>
        /// 技能释放边界:① 本地等价事件 EVT_RELEASE_MAIN_SKILL(skillId, targetInstanceId)(对标老端
        /// Fire(FightEvent.RELEASE_MAIN_SKILL, ..., monster.compress_id),供 UI/表现订阅,保留不动);
        /// ② 真实服务端攻击请求(单体 20001/进战斗态 20024,经 <see cref="FightController"/>,见 <see cref="SendRealAttackOrBlock"/>)。
        /// 老端真实 20001 在技能动作帧 skill_damage_time 由 fight-movie 触发;本端无动作帧系统,在释放边界即发(时序差异,非字段差异)。
        ///
        /// 僵直(对标老端 FightMovieInfo.ts:190-191 is_main_role_attack 分支 SetSkillRigidity(rigidity_time),
        /// rigidity_time=pre_swing+spell_time+back_swing):此处用 config_skill.time 同值(见 SkillConfigs.GetAnimTimeMs
        /// 注释的数据锚点)设僵直,AutoFightController 下一轮 tick 靠 SkillManager.IsInRigidity() 决定要不要再攻,
        /// 不再单纯依赖固定 tick 间隔——对齐老端"打得快慢由动作时长决定,不是由轮询间隔决定"。
        /// </summary>
        private static void ReleaseMainSkill(int skillId, MonsterVo mon, int attackType)
        {
            SkillManager.Instance.SetSkillRigidity(SkillConfigs.GetAnimTimeMs(skillId));
            // CD 起点(对标老端 FightMovieInfo.ts:191-207:预播/托管即 ResetSkill → startCD → START_SKILL_CD 遮罩)。
            SkillManager.Instance.ResetSkill(skillId);

            // 目标列表先算(与 20001 发包同一份,见 BuildAttackTargets),表现层据此把"受击者/落点"特效落到怪身上。
            List<int> monsterIds = BuildAttackTargets(skillId, mon, out bool aoeGeometryBlocked);
            MainRoleAgent.Current?.PlaySkill(skillId, monsterIds);
            EventDispatcher.Emit(GlobalEvent.EVT_RELEASE_MAIN_SKILL, skillId, mon.InstanceId);
            GameLog.Info("Combat",
                "RELEASE_MAIN_SKILL(本地) skill={0} target ins={1}(compress_id 等价) attackType={2}",
                skillId, mon.InstanceId, attackType);

            SendRealAttackOrBlock(skillId, mon, monsterIds, aoeGeometryBlocked);
        }

        /// <summary>
        /// 计算本次攻击的怪物目标列表(20001 发包与技能特效共用同一份,保证"打谁、特效落谁"一致):
        ///   · 单体(mod==1):[主目标];
        ///   · 圆形 AOE(aoe_mode==1):center=主目标坐标收集(<see cref="CollectCircleMonsters"/>);
        ///   · 直线/扇形 AOE(aoe_mode 2/3):几何收集链未移植 → 返回 [主目标] 供表现用,
        ///     <paramref name="aoeGeometryBlocked"/>=true(发包侧只记 blocker 不发,行为与第 9 轮一致)。
        /// </summary>
        private static List<int> BuildAttackTargets(int skillId, MonsterVo primary, out bool aoeGeometryBlocked)
        {
            aoeGeometryBlocked = false;
            if (!SkillConfigs.IsAoe(skillId))
            {
                return new List<int> { primary.InstanceId };
            }

            int aoeMode = SkillConfigs.GetAoeMode(skillId);
            if (aoeMode != 1)
            {
                aoeGeometryBlocked = true;
                return new List<int> { primary.InstanceId };
            }

            int level = SkillManager.Instance.GetSkill(skillId)?.Level ?? 0;
            int area = SkillConfigs.GetAreaForLevel(skillId, level);
            int maxMon = SkillConfigs.GetAttackNumForLevel(skillId, level)[1];
            if (maxMon <= 0) maxMon = 99; // 对标老端 att_num==0 → 99(不限)
            List<int> ids = CollectCircleMonsters(primary, area, maxMon);
            GameLog.Info("Combat",
                "圆形 AOE 收集: skill={0} center=主目标({1},{2}) 半径area={3} 上限num={4} → 命中 {5} 只: [{6}]",
                skillId, primary.X, primary.Y, area, maxMon, ids.Count, string.Join(",", ids));
            return ids;
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
        private static void SendRealAttackOrBlock(int skillId, MonsterVo primary, List<int> monsterIds, bool aoeGeometryBlocked)
        {
            if (aoeGeometryBlocked)
            {
                GameLog.Info("Combat",
                    "20001 未发(AOE blocker): skill={0} aoe_mode={1}(直线/扇形)需主角朝向+直线/扇形几何收集链(未移植),不猜范围。主目标 ins={2} 已锁定。",
                    skillId, SkillConfigs.GetAoeMode(skillId), primary.InstanceId);
                return;
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
                // combo 段自身的表现(对标老端 comboSkills 走同一 fight-movie 链:连段动作/特效逐段播)。
                // 无表现配置的连段 id 静默跳过,不刷 "skill movie missing"。
                if (SkillMovieConfigs.IsLoaded && SkillMovieConfigs.Has(comboSkillId))
                {
                    MainRoleAgent.Current?.PlaySkill(comboSkillId, alive);
                }
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
