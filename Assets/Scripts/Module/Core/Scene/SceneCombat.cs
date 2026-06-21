using System;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene.Vo;

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
    ///   h+i×N 怪 + h+l×N 人 + ihhh skill/x/y/angle)由 fight-movie + AOE 碰撞收集链构建;另有 20024 "c"(1 进/2 出战斗态)。
    /// 本轮不移植 fight-movie/AOE、不猜协议格式,释放只到本地 <see cref="GlobalEvent.EVT_RELEASE_MAIN_SKILL"/> 边界,
    /// 真实 20001/20024 发送列为下一轮 blocker。
    /// </summary>
    public sealed class SceneCombat
    {
        public static readonly SceneCombat Instance = new SceneCombat();
        private SceneCombat() { }

        /// <summary>攻击范围下限(对标老端 SkillManager.GetCurrentAttackRange = Math.max(100, skill_distance*0.8))。
        /// 精确 range 需 config_skill 攻击距离字段 + 主角 attack_range(未接,下一轮),本轮用真实下限 100 像素,不臆造倍率。</summary>
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
            float range = AttackRangeFloor;

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
        /// 本地技能释放边界(对标老端 Fire(FightEvent.RELEASE_MAIN_SKILL, null, null, monster.compress_id, force))。
        /// 发本地等价事件 EVT_RELEASE_MAIN_SKILL(skillId, targetInstanceId);真实 20001 攻击请求(fight-movie/AOE 链)
        /// 本轮不发、不猜格式,只记录 blocker。
        /// </summary>
        private static void ReleaseMainSkill(int skillId, MonsterVo mon, int attackType)
        {
            EventDispatcher.Emit(GlobalEvent.EVT_RELEASE_MAIN_SKILL, skillId, mon.InstanceId);
            GameLog.Info("Combat",
                "RELEASE_MAIN_SKILL(本地) skill={0} target ins={1}(compress_id 等价) attackType={2};真实 20001 发送(h+i×N+h+l×N+ihhh,经 fight-movie/AOE 链)= 下一轮 blocker",
                skillId, mon.InstanceId, attackType);
        }
    }
}
