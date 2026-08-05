using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Audio;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene.Vo;
using Shenxiao.Module.Core.Skill;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 主角战斗驱动(对标老客户端 scene/Scene.ts 的 MainRoleAttackTarget / MainRoleAttackMonster /
    /// GetClickTarget / SetClickTarget)。目标 100% 来自真实 <see cref="SceneManager"/> 怪物表
    /// (12002/12007 下发的 <see cref="MonsterVo"/>)+ <see cref="RoleModel"/> 主角坐标,不造假怪/假伤/假 CD/假目标。
    ///
    /// 链路(对标老端 SkillManager.PressSkillHandler 的 else 分支 → Scene.GetInstance().MainRoleAttackTarget()):
    ///   1. <see cref="MainRoleAttackTarget"/>:手动技能优先取当前点击目标；无目标时按老端原地释放并只在技能
    ///      局部范围内预选。自动战斗入口才允许全场找最近可攻击怪并接近。
    ///   2. 有怪 → <see cref="MainRoleAttackMonster"/>:范围判定(dist²≤range²,对标老端 GetDistance 平方比较)。
    ///        · 命中范围:朝向目标(MainRoleAgent.FaceTowardPixel,对标 SetDirection+DoStand)→ 本地释放边界。
    ///        · 超范围:自动接近(MainRoleAgent.MoveToNpc,对标老端 oper_mgr.StartTargetAction)→ 到达后释放。
    ///   3. 手动无目标 → 以当前朝向原地释放；圆形技能先在施法者 area 内预选最近怪，仍无怪则把圆心放在
    ///      前方 distance 处。自动战斗无怪才真实阻塞。
    ///
    /// 真实服务端攻击请求 20001(老端 FightController.ts:800 WriteBegin(20001):
    ///   h+i×N 怪 + h+l×N 人 + ihhh skill/x/y/angle);另有 20024 "c"(1 进/2 出战斗态)。
    /// 第 9 轮:释放边界由 <see cref="FightController"/> 逐字段真实发送 20024/20001:
    ///   · 单体技能(config_skill.mod==1):怪列表=[主目标]。
    ///   · 圆形 AOE(mod!=1 且 aoe_mode==1,如首杀技能 御剑一式 area=350/num=[1,4]):center=主目标坐标,
    ///     收集半径 area 内可攻击怪取 num[1] 只(对标 Scene.FindMonsters 圆形分支 + target_hiter 置首)。
    ///   · 直线/扇形 AOE(aoe_mode 2/3):严格复刻老端前向矩形/角度点积几何和 100px 近身豁免。
    /// x/y=实际技能中心点、angle=0(见 FightController.ts:1170-1238/1351-1356)。
    /// 本地 <see cref="GlobalEvent.EVT_RELEASE_MAIN_SKILL"/> 边界保留(供 UI/表现订阅),与真实发包并存。
    /// </summary>
    public sealed class SceneCombat
    {
        public static readonly SceneCombat Instance = new SceneCombat();
        private SceneCombat() { }

        /// <summary>攻击范围下限(对标老端 SkillManager.GetCurrentAttackRange 的下限项)。
        /// <see cref="AttackRange"/> 对 mode1 使用 max(100,(distance+area)*0.8)，其他模式使用
        /// max(100,distance*0.8)；distance 缺省 50。</summary>
        private const float AttackRangeFloor = 100f;
        private const float CloseRangeGeometryBypass = 100f;
        private const int UnlimitedTargetCount = 99;

        /// <summary>
        /// 寻怪/发起接近的最小间隔,毫秒(对标老端 Scene.ts:142 auto_find_and_attack_interval=0.4,
        /// 用于 Scene.ts:1744 `NowTime - last_find_target_time < 0.4` 节流)。AutoFightController 的 tick
        /// 从 500ms 提到 100ms 后(对齐老端 UpdateAutoFight 轮询频率),若不加这道闸,接近中的目标每 100ms
        /// 就会被 <see cref="ApproachThenRelease"/> 重新 MoveToNpc 一次,把 MainRoleAgent 的卡死/超时兜底计时
        /// (AutoStuckSeconds/AutoMoveTimeout)反复清零而不到点——变相软锁,比原先 500ms 版更容易触发。
        /// </summary>
        private const int FindAndAttackIntervalMs = 400;

        /// <summary>上次发起"寻怪后接近"的时间(毫秒;对标老端 last_find_target_time)。
        /// 计时基准=Time.realtimeSinceStartupAsDouble(Environment.TickCount 在 WebGL 不可靠)。</summary>
        private double _lastFindTargetMs;

        /// <summary>当前点击/锁定目标实例 id(对标老端 Scene.curr_click_target;0=无)。</summary>
        public int CurrentTargetId { get; private set; }

        /// <summary>对标老端 Scene.SetClickTarget:玩家点怪或自动寻敌后锁定目标。</summary>
        public void SetClickTarget(int monsterInstanceId)
        {
            CurrentTargetId = monsterInstanceId;
            if (monsterInstanceId > 0) SceneTargetSelection.SelectMonster(monsterInstanceId);
            else SceneTargetSelection.Clear();
        }

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

        /// <summary>按实例 id 锁定仍存活且可攻击的 Boss；普通怪不会被大妖副本流程误选。</summary>
        public bool TrySetAttackableBoss(int monsterInstanceId)
        {
            MonsterVo target = SceneManager.Instance.GetMonster(monsterInstanceId);
            if (!IsAttackableBoss(target)) return false;

            SetClickTarget(target.InstanceId);
            GameLog.Info("Combat", "boss target locked exact ins={0} type={1} pos=({2},{3})",
                target.InstanceId, target.TypeId, target.X, target.Y);
            return true;
        }

        /// <summary>从当前场景选择距离指定点最近的可攻击 Boss；仅作为实例绑定失效时的兜底。</summary>
        public bool TrySetNearestBoss(int centerX, int centerY)
        {
            MonsterVo best = null;
            float bestDist2 = float.MaxValue;
            foreach (MonsterVo vo in SceneManager.Instance.AllMonsters)
            {
                if (!IsAttackableBoss(vo)) continue;
                float dx = vo.X - centerX;
                float dy = vo.Y - centerY;
                float dist2 = dx * dx + dy * dy;
                if (dist2 >= bestDist2) continue;
                bestDist2 = dist2;
                best = vo;
            }
            if (best == null) return false;

            SetClickTarget(best.InstanceId);
            GameLog.Info("Combat", "boss target locked nearest ins={0} type={1} pos=({2},{3})",
                best.InstanceId, best.TypeId, best.X, best.Y);
            return true;
        }

        private static bool IsAttackableBoss(MonsterVo vo)
        {
            return vo != null && vo.IsBoss && !vo.IsCollect && vo.CanAttack == 1 && vo.Hp > 0;
        }

        /// <summary>Scene object click entry: lock a real monster instance and release a learned combat skill.</summary>
        public bool MainRoleAttackMonster(int monsterInstanceId, int skillId = 0, int attackType = SkillManager.ONLY_FIRE_ATTACK)
        {
            MonsterVo target = SceneManager.Instance.GetMonster(monsterInstanceId);
            if (target == null)
            {
                if (CurrentTargetId == monsterInstanceId) SetClickTarget(0);
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
                SetClickTarget(0);
                return null;
            }
            return vo;
        }

        /// <summary>
        /// 对标老端 Scene.MainRoleAttackTarget:目标型技能(career!=52 且 obj!=1)点击后由 SkillController 调入。
        /// 手动入口无目标时原地释放；自动战斗入口可传 <paramref name="findNearestWhenNoTarget"/>，全场寻敌并接近。
        /// </summary>
        public void MainRoleAttackTarget(int skillId, int attackType, bool findNearestWhenNoTarget = true)
        {
            MonsterVo target = GetClickTarget();
            if (target == null && findNearestWhenNoTarget)
                target = FindNearestAttackableMonster();

            if (target == null)
            {
                if (!findNearestWhenNoTarget)
                {
                    GameLog.Info("Combat",
                        "MainRoleAttackTarget skill={0}: 手动无锁定目标 → 按当前朝向原地释放(对标老端空 target RELEASE_MAIN_SKILL)",
                        skillId);
                    ReleaseMainSkill(skillId, null, attackType);
                    return;
                }

                GameLog.Info("Combat",
                    "MainRoleAttackTarget skill={0}: 自动战斗无当前目标且 SceneManager 无可攻击怪(monsters={1}) → 阻塞",
                    skillId, SceneManager.Instance.MonsterCount);
                return;
            }
            MainRoleAttackMonster(target, skillId, attackType);
        }

        /// <summary>自我选择技能按当前朝向原地释放，不依赖怪物目标。</summary>
        public void MainRoleReleaseInPlace(int skillId, int attackType)
        {
            ReleaseMainSkill(skillId, null, attackType);
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
            double nowMs = UnityEngine.Time.realtimeSinceStartupAsDouble * 1000.0;
            if (_lastFindTargetMs != 0 && nowMs - _lastFindTargetMs < FindAndAttackIntervalMs) return;
            _lastFindTargetMs = nowMs;

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
                    if (CurrentTargetId == mon.InstanceId) SetClickTarget(0);
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
        /// ② 真实服务端攻击请求(20001/进战斗态 20024,经 <see cref="FightController"/>)。
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

            AttackPlan plan = BuildAttackPlan(skillId, mon);
            MainRoleAgent.Current?.PlaySkill(skillId, plan.MonsterIds);
            string sound = SkillMovieConfigs.GetSoundRes(skillId);
            if (!string.IsNullOrEmpty(sound)
                && (!SkillMovieConfigs.SoundRequiresHiter(skillId) || plan.MonsterIds.Count > 0))
            {
                _ = AudioManager.PlaySkill(sound, delaySeconds: SkillMovieConfigs.GetSoundStartTimeSeconds(skillId));
            }
            int targetInstanceId = plan.Primary?.InstanceId ?? 0;
            EventDispatcher.Emit(GlobalEvent.EVT_RELEASE_MAIN_SKILL, skillId, targetInstanceId);
            GameLog.Info("Combat",
                "RELEASE_MAIN_SKILL(本地) skill={0} target ins={1}(0=无锁定) center=({2:F0},{3:F0}) attackType={4} targets=[{5}]",
                skillId, targetInstanceId, plan.CenterX, plan.CenterY, attackType, string.Join(",", plan.MonsterIds));

            SendRealAttack(skillId, plan);
        }

        private sealed class AttackPlan
        {
            public MonsterVo Primary;
            public List<int> MonsterIds;
            public float CenterX;
            public float CenterY;
        }

        private readonly struct TargetCandidate
        {
            public readonly MonsterVo Monster;
            public readonly double DistanceSquared;
            public readonly int WireOrder;

            public TargetCandidate(MonsterVo monster, double distanceSquared, int wireOrder)
            {
                Monster = monster;
                DistanceSquared = distanceSquared;
                WireOrder = wireOrder;
            }
        }

        /// <summary>
        /// 计算技能中心与怪物目标。字段语义逐项对标老端 FightController.AttackRequest + Scene.FindMonsters：
        /// mode1 圆形、mode2 前向矩形、mode3 扇形；近身 100px 对直线/扇形免角度和长度判断。
        /// </summary>
        private static AttackPlan BuildAttackPlan(int skillId, MonsterVo selectedPrimary)
        {
            RoleModel role = RoleModel.Instance;
            int level = SkillManager.Instance.GetSkill(skillId)?.Level ?? 0;
            int area = SkillConfigs.GetAreaForLevel(skillId, level);
            int distance = SkillConfigs.GetDistanceForLevel(skillId, level);
            int maxMon = SkillConfigs.GetAttackNumForLevel(skillId, level)[1];
            if (maxMon <= 0) maxMon = UnlimitedTargetCount;

            MonsterVo primary = selectedPrimary;
            int selectType = SkillConfigs.GetSelectType(skillId);
            if (primary == null && selectType != 1)
            {
                // 老端无 target_compress_id 时先以施法者为中心、find_area=area 预选最近目标；
                // area==0 表示不限半径。这里只复刻当前已接的 PvE 怪物子集。
                primary = FindNearestAttackableMonsterInRadius(role.X, role.Y, area);
                if (primary != null) FaceTarget(primary);
            }

            Vector2 direction = ResolveAttackDirection(primary);
            float centerX = role.X;
            float centerY = role.Y;

            if (!SkillConfigs.IsAoe(skillId))
            {
                if (primary != null)
                {
                    centerX = primary.X;
                    centerY = primary.Y;
                }
                return new AttackPlan
                {
                    Primary = primary,
                    MonsterIds = primary != null ? new List<int> { primary.InstanceId } : new List<int>(),
                    CenterX = centerX,
                    CenterY = centerY,
                };
            }

            int aoeMode = SkillConfigs.GetAoeMode(skillId);
            List<TargetCandidate> candidates;
            switch (aoeMode)
            {
                case 1:
                    // 圆形:有目标时圆心=目标；无目标或 att_obj==1 时圆心=施法者前方 distance。
                    if (primary == null || SkillConfigs.GetAttObj(skillId) == 1)
                    {
                        float ellipseDistance = ChangeEllipseValue(direction, distance);
                        centerX += direction.x * ellipseDistance;
                        centerY += direction.y * ellipseDistance;
                    }
                    else
                    {
                        centerX = primary.X;
                        centerY = primary.Y;
                    }
                    candidates = CollectCircleMonsters(centerX, centerY, area);
                    break;
                case 2:
                    candidates = CollectLineMonsters(role.X, role.Y, direction, distance, area);
                    break;
                case 3:
                    candidates = CollectSectorMonsters(role.X, role.Y, direction, distance, area);
                    break;
                default:
                    // 老端 switch default 不设置方向几何，最终回落 FindMonsters 的圆形 area 分支（含 mode4）。
                    candidates = CollectCircleMonsters(role.X, role.Y, area);
                    break;
            }

            List<int> ids = BuildOrderedTargetIds(primary, candidates, maxMon);
            GameLog.Info("Combat",
                "AOE 收集: skill={0} mode={1} center=({2:F0},{3:F0}) distance={4} area={5} num={6} → [{7}]",
                skillId, aoeMode, centerX, centerY, distance, area, maxMon, string.Join(",", ids));
            return new AttackPlan
            {
                Primary = primary,
                MonsterIds = ids,
                CenterX = centerX,
                CenterY = centerY,
            };
        }

        /// <summary>
        /// 真实服务端攻击请求。怪列表与 center 均来自 <see cref="BuildAttackPlan"/>；
        /// 人列表仍为空（PvP FindRoles 链未接），angle 对标老端固定 0。
        /// </summary>
        private static void SendRealAttack(int skillId, AttackPlan plan)
        {
            // 进战斗态(每段战斗一次)→ 真实攻击请求(经 FightController/NetManager,逐字段对齐老端)。
            FightController.Instance.EnterFightingState();
            int centerX = (int)Math.Floor(plan.CenterX);
            int centerY = (int)Math.Floor(plan.CenterY);
            FightController.Instance.SendMainSkillAttack(skillId, plan.MonsterIds, Array.Empty<long>(), centerX, centerY, 0);

            // 第14轮真实伤害链闭合:普攻主技能(如 御剑一式 59100001)is_att=0/calc=0,服务端 mod_battle.erl 的
            // is_att=0 分支只回一个 damage=0 的"进战斗 engage 帧"(NoHurtDerList),真正扣血的是它的 combo 副技能
            // (如 59100002)is_att=1/calc=1。老端 fight-movie 在 comboSkills 时点对同目标补发副技能 20001(实测老端
            // 运行态 send 20001 后约 +300ms 再 send 20001);本端据 config_skill[skill].combo 链补发副技能(id/延迟全
            // 从配置读,不 hardcode),闭合真实伤害链。无 combo 链(真实主动技能/链尾)则不补发,行为不变。
            ScheduleComboFollowUp(skillId, plan.MonsterIds, centerX, centerY);
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
                if (delayMs > 0) await Shenxiao.Framework.Util.TimeUtil.Delay(delayMs); // ⚠ Task.Delay 在 WebGL 永不醒
                if (!NetManager.IsConnected)
                {
                    GameLog.Info("Combat", "combo 副技能补发跳过: 已断线 engage={0} combo={1}", engageSkillId, comboSkillId);
                    return;
                }

                // 发包前重过滤:只留仍在场且 hp>0 的怪(engage 帧 damage=0 不会杀怪,通常全保留;防移除/位移)。
                var alive = new List<int>(monsterIds.Count);
                foreach (int ins in monsterIds)
                {
                    MonsterVo m = SceneManager.Instance.GetMonster(ins);
                    if (m != null && m.Hp > 0) alive.Add(ins);
                }
                if (alive.Count == 0)
                {
                    GameLog.Info("Combat", "combo 副技能补发跳过: 目标全灭/离场 engage={0} combo={1}", engageSkillId, comboSkillId);
                    return;
                }

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

        private static MonsterVo FindNearestAttackableMonsterInRadius(int centerX, int centerY, int radius)
        {
            MonsterVo best = null;
            long bestDistanceSquared = long.MaxValue;
            long radiusSquared = (long)radius * radius;
            foreach (MonsterVo monster in SceneManager.Instance.AllMonsters)
            {
                if (!IsAttackableMonster(monster)) continue;

                long dx = monster.X - centerX;
                long dy = monster.Y - centerY;
                long distanceSquared = dx * dx + dy * dy;
                if (radius > 0 && distanceSquared > radiusSquared) continue;
                if (distanceSquared >= bestDistanceSquared) continue;

                best = monster;
                bestDistanceSquared = distanceSquared;
            }
            return best;
        }

        /// <summary>优先指向主目标；无目标时使用当前主角模型朝向，headless 场景回落舞台向下。</summary>
        private static Vector2 ResolveAttackDirection(MonsterVo primary)
        {
            RoleModel role = RoleModel.Instance;
            Vector2 direction;
            if (primary != null)
            {
                direction = new Vector2(primary.X - role.X, primary.Y - role.Y);
                if (direction.sqrMagnitude > 0.0001f) return direction.normalized;
            }

            MainRoleAgent agent = MainRoleAgent.Current;
            if (agent != null && agent.TryGetFacingPixelDirection(out direction) &&
                direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }

            return Vector2.up;
        }

        /// <summary>
        /// 对标老端 GameMath.ChangeEllipseValue(dir,value)：舞台横向保持原距离，纵向压到 0.5，
        /// 斜向按 |cos| 连续插值，用于无目标圆形技能的前方圆心。
        /// </summary>
        private static float ChangeEllipseValue(Vector2 direction, int value)
        {
            if (value <= 0 || direction.sqrMagnitude <= 0.0001f) return value;
            direction.Normalize();
            return value * (0.5f + Mathf.Abs(direction.x) * 0.5f);
        }

        /// <summary>
        /// 圆形 AOE：以技能中心和 area 半径筛选，按距离稳定升序；area&lt;=0 对标老端 null 半径（不限制）。
        /// </summary>
        private static List<TargetCandidate> CollectCircleMonsters(float centerX, float centerY, int area)
        {
            double areaSquared = (double)area * area;
            var candidates = new List<TargetCandidate>();
            int wireOrder = 0;
            foreach (MonsterVo monster in SceneManager.Instance.AllMonsters)
            {
                int currentOrder = wireOrder++;
                if (!IsAttackableMonster(monster)) continue;

                double dx = monster.X - centerX;
                double dy = monster.Y - centerY;
                double distanceSquared = dx * dx + dy * dy;
                if (area > 0 && distanceSquared > areaSquared) continue;
                candidates.Add(new TargetCandidate(monster, distanceSquared, currentOrder));
            }
            SortCandidates(candidates);
            return candidates;
        }

        /// <summary>
        /// 直线 AOE：前向长度 distance、总宽 area；100px 内目标对标老端直接命中，不受方向与长度限制。
        /// </summary>
        private static List<TargetCandidate> CollectLineMonsters(
            int originX, int originY, Vector2 direction, int distance, int area)
        {
            if (distance <= 0 || area <= 0)
                return CollectCircleMonsters(originX, originY, area);

            double distanceSquaredLimit = (double)distance * distance;
            double closeSquared = CloseRangeGeometryBypass * CloseRangeGeometryBypass;
            double halfWidthSquared = area * 0.5 * area * 0.5;
            var candidates = new List<TargetCandidate>();
            int wireOrder = 0;
            foreach (MonsterVo monster in SceneManager.Instance.AllMonsters)
            {
                int currentOrder = wireOrder++;
                if (!IsAttackableMonster(monster)) continue;

                double dx = monster.X - originX;
                double dy = monster.Y - originY;
                double distanceSquared = dx * dx + dy * dy;
                if (distanceSquared > closeSquared)
                {
                    if (distanceSquared > distanceSquaredLimit) continue;
                    double forward = dx * direction.x + dy * direction.y;
                    if (forward <= 0) continue;
                    double perpendicular = dx * direction.y - dy * direction.x;
                    if (perpendicular * perpendicular > halfWidthSquared) continue;
                }
                candidates.Add(new TargetCandidate(monster, distanceSquared, currentOrder));
            }
            SortCandidates(candidates);
            return candidates;
        }

        /// <summary>
        /// 扇形 AOE：前向长度 distance、总角度 area（度）；100px 内目标对标老端直接命中。
        /// </summary>
        private static List<TargetCandidate> CollectSectorMonsters(
            int originX, int originY, Vector2 direction, int distance, int area)
        {
            if (distance <= 0 || area == 0)
                return CollectCircleMonsters(originX, originY, area);

            double distanceSquaredLimit = (double)distance * distance;
            double closeSquared = CloseRangeGeometryBypass * CloseRangeGeometryBypass;
            double minimumDot = Math.Cos(area * Math.PI / 360.0);
            var candidates = new List<TargetCandidate>();
            int wireOrder = 0;
            foreach (MonsterVo monster in SceneManager.Instance.AllMonsters)
            {
                int currentOrder = wireOrder++;
                if (!IsAttackableMonster(monster)) continue;

                double dx = monster.X - originX;
                double dy = monster.Y - originY;
                double distanceSquared = dx * dx + dy * dy;
                if (distanceSquared > closeSquared)
                {
                    if (distanceSquared > distanceSquaredLimit) continue;
                    double length = Math.Sqrt(distanceSquared);
                    double dot = (dx * direction.x + dy * direction.y) / length;
                    if (dot < minimumDot) continue;
                }
                candidates.Add(new TargetCandidate(monster, distanceSquared, currentOrder));
            }
            SortCandidates(candidates);
            return candidates;
        }

        private static bool IsAttackableMonster(MonsterVo monster)
        {
            return monster != null && !monster.IsCollect && monster.CanAttack == 1 && monster.Hp > 0;
        }

        private static void SortCandidates(List<TargetCandidate> candidates)
        {
            candidates.Sort((left, right) =>
            {
                int byDistance = left.DistanceSquared.CompareTo(right.DistanceSquared);
                return byDistance != 0 ? byDistance : left.WireOrder.CompareTo(right.WireOrder);
            });
        }

        /// <summary>老端 target_hiter 强制置首，再从距离序列补齐到怪物数量上限。</summary>
        private static List<int> BuildOrderedTargetIds(
            MonsterVo primary, List<TargetCandidate> candidates, int maxMon)
        {
            if (maxMon <= 0) maxMon = UnlimitedTargetCount;
            var ids = new List<int>(Math.Min(maxMon, candidates.Count + (primary != null ? 1 : 0)));
            var seen = new HashSet<int>();
            if (primary != null && seen.Add(primary.InstanceId))
                ids.Add(primary.InstanceId);

            foreach (TargetCandidate candidate in candidates)
            {
                if (ids.Count >= maxMon) break;
                if (seen.Add(candidate.Monster.InstanceId))
                    ids.Add(candidate.Monster.InstanceId);
            }
            return ids;
        }

        /// <summary>
        /// 真实接敌/接近范围（对标老端 SkillManager.UpdateAttackDistance）：
        /// mode1 先把圆形 area 加到 distance，再统一乘 0.8；其他模式只取 distance；最终下限 100px。
        /// skill distance 缺省 50，level 取真实已学等级。
        /// </summary>
        private static float AttackRange(int skillId)
        {
            int level = SkillManager.Instance.GetSkill(skillId)?.Level ?? 0;
            int distance = SkillConfigs.GetDistanceForLevel(skillId, level);
            if (SkillConfigs.GetAoeMode(skillId) == 1)
                distance += SkillConfigs.GetAreaForLevel(skillId, level);
            return Math.Max(AttackRangeFloor, distance * 0.8f);
        }
    }
}
