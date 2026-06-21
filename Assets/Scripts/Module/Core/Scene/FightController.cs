using System;
using System.Collections.Generic;
using System.Text;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 战斗发包层(对标老端 scene/fight/FightController.ts 的真实发包子集)。本轮只实现
    /// **主角单体目标技能** 的真实攻击请求 20001 + 进/出战斗态 20024,全部字段来自真实
    /// <see cref="SceneManager"/> 目标 + <see cref="Role.RoleModel"/> 坐标 + config_skill,不猜不造。
    ///
    /// 20001(C2S,逐字段对标 FightController.ts:800-814 onRoleRequestToFightHandler):
    ///   WriteBegin(20001) → h 怪数 + i×怪数(实例id) + h 人数 + l×人数(roleId) + ihhh(skill,x,y,angle)
    ///   · 单体目标技能:怪列表=[目标实例id](对标 FightController.ts:1351-1356 unshift target.id),人列表=[];
    ///   · x/y = center_pos = 目标坐标(非 AOE 分支 FightController.ts:1208-1212 center_pos=pre_tar.x/y),已 floor & clamp≥0;
    ///   · angle = 0(FightController.ts:1238/1253 硬编码 attack_angle=0)。
    ///   · 老端发送时机 = 技能动作帧 skill_damage_time(由 fight-movie 触发);本端无动作帧/预播系统,
    ///     在 <see cref="SceneCombat"/> 释放边界即发(时序差异,非字段差异,见 MainQuest-CombatFinish 报告)。
    ///
    /// 20024(C2S,FightController.ts:889-892):进战斗态发 "c" 1、出战斗态发 "c" 2。
    ///   老端由 EnterFightingState→CHANGE_FIGHTING_STATE 驱动(受 ConfigClientScene.fighting_state_invalidate 限制);
    ///   本端在首次单体攻击进入战斗态时发一次 "c" 1(fighting_state_invalidate[sceneId] 未接入 → 见报告 blocker)。
    ///
    /// S2C 20001(攻击结果广播:攻击者信息 + 防御者列表 + 伤害,FightVo)格式复杂,本期只记录到达 + 原始长度取证;
    /// 真正扣血/死亡走既有 12009/12006 链(SceneController → SceneManager → MonsterRenderer 血条/销毁),不在此伪造。
    /// </summary>
    public sealed class FightController : BaseController
    {
        public static readonly FightController Instance = new FightController();
        private FightController() { }

        /// <summary>本段战斗是否已发过 20024 "c" 1(对标老端 is_fighting_state;每段战斗只发一次进战斗态)。</summary>
        private bool _fighting;

        protected override void Register()
        {
            // 同号 S2C 20001 攻击结果广播:仅取证记录(完整 FightVo 解析 = P4 深水区)。
            RegisterProtocal(Proto.CS_FIGHT_ATTACK, On20001Broadcast);
            _fighting = false;
        }

        public override void Dispose()
        {
            _fighting = false; // 断线/登出:战斗态复位(重连后重新进战斗态再发 20024 "c" 1)
            base.Dispose();
        }

        /// <summary>
        /// 进战斗态(对标 FightController.ts:889 onChangeFightingState → 20024 "c" 1)。每段战斗只发一次。
        /// 老端受 fighting_state_invalidate[sceneId] 限制(该场景禁战斗态则不发);本端未接该客户端配置,
        /// 在主线打怪场景(有可击杀怪)按常态发送,差异记录于报告。
        /// </summary>
        public void EnterFightingState()
        {
            if (_fighting) return;
            _fighting = true;
            SendFmt(Proto.CS_FIGHTING_STATE, "c", 1);
            GameLog.Info("Fight", "send 20024 \"c\" 1 进战斗态(对标 FightController.ts:889;fighting_state_invalidate 未校验,见报告)");
        }

        /// <summary>出战斗态(对标 20024 "c" 2)。老端按战斗态超时触发;本端在断线/切场景最小复位时发。</summary>
        public void ExitFightingState()
        {
            if (!_fighting) return;
            _fighting = false;
            SendFmt(Proto.CS_FIGHTING_STATE, "c", 2);
            GameLog.Info("Fight", "send 20024 \"c\" 2 出战斗态");
        }

        /// <summary>
        /// 真实主角技能攻击请求 20001(逐字段对标 FightController.ts:800-814)。
        /// 动态格式串 = h + i×怪数 + h + l×人数 + ihhh,与老端 WriteBegin/WriteFMT 顺序逐字节一致。
        /// 只消费真实数据(目标实例id、目标坐标、技能id);x/y 已 clamp≥0(对标老端 attack_x/y<0?0)。
        /// </summary>
        /// <param name="skillId">技能 id(真实 config_skill / 21002 已学技能)。</param>
        /// <param name="monsterIds">怪物目标实例 id 列表(单体=[目标],对标 request_fight_mon_list)。</param>
        /// <param name="roleIds">玩家目标 roleId 列表(攻击怪时为空)。</param>
        /// <param name="x">attack_x = center_pos.x = 目标像素 X(非 AOE)。</param>
        /// <param name="y">attack_y = center_pos.y = 目标像素 Y(非 AOE)。</param>
        /// <param name="angle">attack_angle = 0(老端硬编码)。</param>
        public void SendMainSkillAttack(int skillId, IReadOnlyList<int> monsterIds, IReadOnlyList<long> roleIds, int x, int y, int angle)
        {
            if (monsterIds == null) monsterIds = Array.Empty<int>();
            if (roleIds == null) roleIds = Array.Empty<long>();

            int cx = x < 0 ? 0 : x;
            int cy = y < 0 ? 0 : y;
            int ca = angle < 0 ? 0 : angle;

            // 动态格式串:h(怪数) + i×怪数 + h(人数) + l×人数 + ihhh(skill,x,y,angle)。
            var fmt = new StringBuilder(8 + monsterIds.Count + roleIds.Count);
            var args = new List<object>(4 + monsterIds.Count + roleIds.Count);

            fmt.Append('h'); args.Add(monsterIds.Count);
            for (int i = 0; i < monsterIds.Count; i++) { fmt.Append('i'); args.Add(monsterIds[i]); }
            fmt.Append('h'); args.Add(roleIds.Count);
            for (int i = 0; i < roleIds.Count; i++) { fmt.Append('l'); args.Add(roleIds[i]); }
            fmt.Append("ihhh");
            args.Add(skillId);
            args.Add(cx);
            args.Add(cy);
            args.Add(ca);

            SendFmt(Proto.CS_FIGHT_ATTACK, fmt.ToString(), args.ToArray());
            GameLog.Info("Fight",
                "send 20001 攻击请求: skill={0} 怪列表=[{1}] 人列表=[{2}] x={3} y={4} angle={5} fmt=\"{6}\"",
                skillId, string.Join(",", monsterIds), string.Join(",", roleIds), cx, cy, ca, fmt);
        }

        // S2C 20001:攻击结果广播(攻击者信息 + 防御者列表 + 伤害,FightVo)。
        // 完整解析(逐防御者 hp/damage/死亡/buff)= P4 深水区,本期只记录到达取证;
        // 怪血条扣减/死亡移除以服务端 12009/12006 为准(已接 MonsterRenderer),此处不解析、不伪造。
        private void On20001Broadcast(NetReader reader)
        {
            GameLog.Info("Fight",
                "recv 20001 攻击结果广播(S2C): payload={0}B —— 完整 FightVo(攻击者+防御者列表+伤害)解析=P4;扣血/死亡以 12009/12006 为准",
                reader.Remaining);
        }
    }
}
