using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene.Vo;

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
    /// S2C 20001(攻击结果广播:攻击者头 + 防御者列表 + 伤害,FightVo):第10轮起**真实解析**(见
    /// <see cref="FightVo"/>,逐字节对标老端 FightVo.ReadFromProtocal)。<see cref="On20001Broadcast"/> 解析后把
    /// 每个防御者的服务端**新绝对 hp / 死亡**喂给既有 <see cref="SceneManager"/> 链(hp&gt;0→ApplyHp 刷血条;
    /// hp==0→NotifyKilled(死亡动作)+DeleteSceneObj,对标老端 RefreshObjVo:1527 + DoDead),与 12009/12006
    /// 同一渲染出口,不开新假血条路径、不伪造伤害。
    ///
    /// 战斗表现消费(本轮接入,全部只吃协议真实字段):
    ///   · damage/damage_flag → <see cref="DamageFontRenderer"/> 伤害飘字(仅主角攻击/主角被击,对标老端
    ///     FightDamageManager.ShowFont 前置门槛);
    ///   · defender 受击动作:damage&gt;0 或 闪避 才播 behit(对标老端 executeHitedAnimation:506 门槛
    ///     force_hited||damage&gt;0||flag==ShanBi;engage 帧 damage=0 不再空播);
    ///   · 主角自身 hp(攻击头 hp / defender hp)→ RoleModel.BattleAttr.Hp + EVT_ROLE_INFO_UPDATE
    ///     (MainUITopView 既有血条链,对标老端 RefreshObjVo 更新 attacker/defender vo.hp)。
    /// </summary>
    public sealed class FightController : BaseController
    {
        public static readonly FightController Instance = new FightController();
        private FightController() { }

        /// <summary>本段战斗是否已发过 20024 "c" 1(对标老端 is_fighting_state;每段战斗只发一次进战斗态)。</summary>
        private bool _fighting;

        // ── 20001 发送节流(对齐老端 fight-movie 动作帧门控)──────────────────────────────────────
        // 老端真实 20001 由 FightMovieInfo 在动画 skill_damage_time 帧触发(FightMovieInfo.ts:490 +
        // past_time>=skill_damage_time),连招走同一动画/buff 系统,发包率被动画播放天然串行限速,物理上发不快。
        // 本端无动作帧系统:engage 在释放边界即发 + 连招 fire-and-forget Task.Delay 补发(SceneCombat)。
        // 主线程一卡顿(boss 渲染/特效),堆积的 Task.Delay 续体会在 resume 时同帧一次性 flush 出突发 20001,
        // 被服务端判为异常并 force-close 连接(实测断线签名 "remote closed without close handshake",见 reconnect 排查)。
        // 这里在唯一 20001 出口做 FIFO 节流:相邻两包间隔不低于 MIN_ATTACK_SEND_INTERVAL_SEC(对齐服务端 ~200ms
        // 处理节流线),承载真实伤害的连招不丢、只把突发摊匀。稳态 AutoFight 节奏(500ms 循环,engage→combo 间隔
        // ≥200ms)本就不触发节流;只有续体堆积时才生效。队列上限兜底极端卡顿,溢出丢最早请求并告警。
        private const float MIN_ATTACK_SEND_INTERVAL_SEC = 0.2f;
        private const int MAX_PENDING_ATTACK_SENDS = 16;
        private readonly Queue<Action> _attackSendQueue = new Queue<Action>();
        private float _nextAttackSendAllowedAt;
        private bool _drainingAttackQueue;

        // 老端 SceneBaseType(SceneConfig.ts:31)中本端 20001 defense_list 需路由的子集(协议枚举,非业务魔数)。
        private const int OBJ_MONSTER = 1;   // 怪 / 采集物
        private const int OBJ_ROLE = 2;      // 玩家
        private const int OBJ_FAKE_ROLE = 5; // 假人

        // damage_flag 受击门槛用值(全表见 FightVo/DamageFontRenderer;老端 FightDamageFlag.ShanBi=1)。
        private const int DAMAGE_FLAG_DODGE = 1;

        protected override void Register()
        {
            // 同号 S2C 20001 攻击结果广播:真实解析 FightVo,把服务端新 hp/死亡喂既有血量链(第10轮)。
            RegisterProtocal(Proto.CS_FIGHT_ATTACK, On20001Broadcast);

            // ----- Fight 扩容(自动循环 队列#2 轮2;各号 wire 格式/权威源见 Proto.cs 对应常量注释) -----
            RegisterProtocal(Proto.CS_FIGHTING_STATE, On20024);
            RegisterProtocal(Proto.CS_FIGHT_ATTACK_FAIL, On20005);
            RegisterProtocal(Proto.CS_BUFF_CLEAR, On20007);
            RegisterProtocal(Proto.CS_PICK_MONSTER, On20010);
            RegisterProtocal(Proto.CS_KILLER_INFO, On20013);
            RegisterProtocal(Proto.CS_KILL_INFO, On20014);
            RegisterProtocal(Proto.CS_PK_VALUE, On20015);
            RegisterProtocal(Proto.CS_SKILL_CD_CLEAR, On20018);
            RegisterProtocal(Proto.CS_SNATCH_OWNERSHIP, On20020);
            RegisterProtocal(Proto.CS_CHECK_OWNERSHIP, On20021);
            RegisterProtocal(Proto.CS_SIMULATE_FIGHT, On20022);
            RegisterProtocal(Proto.CS_FIGHT_ENERGY, On20023);
            RegisterProtocal(Proto.CS_SKILL_CD_END, On20027);
            RegisterProtocal(Proto.CS_TRIGGER_SKILLS, On20028);

            _fighting = false;
        }

        public override void Dispose()
        {
            _fighting = false; // 断线/登出:战斗态复位(重连后重新进战斗态再发 20024 "c" 1)
            _attackSendQueue.Clear(); // 丢弃未发的 20001(断线后不再补发,drainer 见空队列自然退出)
            _nextAttackSendAllowedAt = 0f;
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

            // 编码所需的值已快照进 fmtStr/argArr;日志字段同步快照,避免闭包持有可变列表引用。
            string fmtStr = fmt.ToString();
            object[] argArr = args.ToArray();
            string monStr = string.Join(",", monsterIds);
            string roleStr = string.Join(",", roleIds);

            // 不直接发:经 20001 节流出口(对齐老端动作帧门控,防主线程卡顿后续体堆积同帧 flush 触发服务端 force-close)。
            EnqueueAttackSend(() =>
            {
                SendFmt(Proto.CS_FIGHT_ATTACK, fmtStr, argArr);
                GameLog.Info("Fight",
                    "send 20001 攻击请求: skill={0} 怪列表=[{1}] 人列表=[{2}] x={3} y={4} angle={5} fmt=\"{6}\"",
                    skillId, monStr, roleStr, cx, cy, ca, fmtStr);
            });
        }

        /// <summary>
        /// 把一个 20001 发送动作压入节流队列并确保 drainer 在跑。全程主线程(SceneCombat 战斗逻辑 / 连招
        /// Task.Delay 续体都在 UnitySynchronizationContext),无需加锁。队列满则丢最早请求兜底极端卡顿。
        /// </summary>
        private void EnqueueAttackSend(Action send)
        {
            if (send == null) return;
            if (_attackSendQueue.Count >= MAX_PENDING_ATTACK_SENDS)
            {
                _attackSendQueue.Dequeue();
                GameLog.Warn("Fight",
                    "20001 发送队列已满({0}),丢弃最早请求防积压(主线程严重卡顿?)", MAX_PENDING_ATTACK_SENDS);
            }
            _attackSendQueue.Enqueue(send);
            if (!_drainingAttackQueue)
            {
                _drainingAttackQueue = true;
                _ = DrainAttackQueueAsync();
            }
        }

        /// <summary>
        /// 单 drainer 按 MIN_ATTACK_SEND_INTERVAL_SEC 节流出队发送。续体堆积时每次 await 后重判时间(continue),
        /// 自纠正为逐个间隔发送,绝不同帧一次性 flush。稳态(包间隔本就 ≥ 阈值)队列为空、立即放行、无漂移。
        /// </summary>
        private async Task DrainAttackQueueAsync()
        {
            try
            {
                while (_attackSendQueue.Count > 0)
                {
                    float now = UnityEngine.Time.realtimeSinceStartup;
                    if (now < _nextAttackSendAllowedAt)
                    {
                        int waitMs = (int)Math.Ceiling((_nextAttackSendAllowedAt - now) * 1000f);
                        if (waitMs > 0) await Task.Delay(waitMs);
                        continue; // await 后重判:多个续体同帧醒来也只放行到时的那个,其余继续等
                    }
                    Action send = _attackSendQueue.Dequeue();
                    _nextAttackSendAllowedAt = UnityEngine.Time.realtimeSinceStartup + MIN_ATTACK_SEND_INTERVAL_SEC;
                    try { send(); }
                    catch (Exception e) { GameLog.Warn("Fight", "20001 节流发送异常: {0}", e.Message); }
                }
            }
            finally
            {
                _drainingAttackQueue = false;
            }
        }

        // S2C 20001:攻击结果广播(攻击者头 + 防御者列表 + 伤害,FightVo)。第10轮起真实解析:
        // 逐字节按老端 FightVo.ReadFromProtocal 读出 attacker + defense_list,再把每个防御者的服务端
        // **新绝对 hp / 死亡** 喂既有 SceneManager 链(hp>0 刷血条、hp==0 移除可见怪)。不伪造伤害/死亡。
        private void On20001Broadcast(NetReader reader)
        {
            int payloadLen = reader.Remaining;
            var vo = new FightVo();
            try
            {
                vo.ReadFromProtocal(reader);
            }
            catch (Exception e)
            {
                GameLog.Error("Fight",
                    "20001 S2C 解析错位(字段顺序与老端 FightVo 不一致?): len={0}B err={1}", payloadLen, e.Message);
                return;
            }

            GameLog.Info("Fight",
                "recv 20001 攻击结果广播(S2C): len={0}B attacker type={1} role={2} hp={3} skill={4} lv={5} pos=({6},{7}) atkPos=({8},{9}) atkBuff={10} trigger={11} defenders={12} remaining={13}B",
                payloadLen, vo.Attack.AttackerType, vo.Attack.RoleId, vo.Attack.Hp, vo.Attack.SkillId, vo.Attack.SkillLevel,
                vo.Attack.PosX, vo.Attack.PosY, vo.Attack.AttackPosX, vo.Attack.AttackPosY,
                vo.Attack.Buffs.Count, vo.AttackTriggerSkills.Count, vo.DefenseList.Count, reader.Remaining);

            for (int i = 0; i < vo.DefenseList.Count; i++)
            {
                FightVo.DefenseInfo d = vo.DefenseList[i];
                GameLog.Info("Fight",
                    "  defender[{0}] type_flag={1} id={2} hp={3} damage={4} flag={5} pos=({6},{7})",
                    i, d.TypeFlag, d.RoleId, d.Hp, d.Damage, d.DamageFlag, d.PosX, d.PosY);
            }

            ApplyDefenseListToScene(vo, vo.Attack.SkillId);
        }

        /// <summary>
        /// 把 20001 S2C 的 defense_list 真实 hp/死亡喂既有 <see cref="SceneManager"/> 链(对标老端 RefreshObjVo:1527):
        ///   hp&gt;0  → <see cref="SceneManager.ApplyHp"/>(服务端新绝对 hp,保留既有 HpLim)→ MonsterHpChanged/RoleHpChanged → 血条刷新;
        ///   hp==0 → <see cref="SceneManager.DeleteSceneObj"/>(移除可见怪/玩家)→ MonsterRemoved → 模型/名牌/血条销毁(对标 ForceDoDead)。
        /// 与 12009/12006 同一渲染出口,不开新假血条路径。找不到对应场景对象只记 warning,绝不造假对象;
        /// damage/damage_flag(飘字)本轮不消费。
        /// 第18轮:回包后通知 SceneController 检查是否继续击杀。
        /// </summary>
        private void ApplyDefenseListToScene(FightVo vo, int skillId)
        {
            ApplyMonsterFightVisuals(vo);
            ApplyMoveAnimToMainRole(vo);
            ApplyDamageFontsAndMainRoleHp(vo); // 在血量应用/移除之前:飘字取怪的在场坐标(死怪即将被移除)

            SceneManager mgr = SceneManager.Instance;
            int firstMonsterId = 0;
            bool firstMonsterAlive = false;

            foreach (FightVo.DefenseInfo d in vo.DefenseList)
            {
                if (d.TypeFlag == OBJ_MONSTER)
                {
                    if (d.RoleId < 0 || d.RoleId > int.MaxValue)
                    {
                        GameLog.Warn("Fight", "20001 defender 怪实例 id 越界: {0}", d.RoleId);
                        continue;
                    }
                    int ins = (int)d.RoleId;
                    MonsterVo m = mgr.GetMonster(ins);
                    if (m == null)
                    {
                        GameLog.Warn("Fight", "20001 defender 怪 {0} 不在 SceneManager(未在视野/已移除),只记录不造假", ins);
                        continue;
                    }
                    if (firstMonsterId == 0) firstMonsterId = ins;
                    if (d.Hp == 0)
                    {
                        GameLog.Info("Fight", "怪 {0} 服务端判定死亡(hp=0 damage={1}),播死亡动作后移除(对标老端 DoDead)", ins, d.Damage);
                        MonsterRenderer.NotifyKilled(ins); // 视图走 death + 尸体停留;数据层照常立即移除
                        mgr.DeleteSceneObj(ins);
                    }
                    else
                    {
                        if (firstMonsterId == ins) firstMonsterAlive = true;
                        GameLog.Info("Fight", "怪 {0} 服务端新 hp={1}/{2}(damage={3} flag={4}),刷新血条", ins, d.Hp, m.HpLim, d.Damage, d.DamageFlag);
                        mgr.ApplyHp(ins, d.Hp, m.HpLim);
                    }
                }
                else if (d.TypeFlag == OBJ_ROLE || d.TypeFlag == OBJ_FAKE_ROLE)
                {
                    RoleVo r = mgr.GetRole(d.RoleId);
                    if (r == null)
                    {
                        // 主角自身不在 _roles(hp 在 RoleModel,已由 ApplyDamageFontsAndMainRoleHp 同步 HUD);
                        // 其余未在视野玩家只记录不造假。
                        if (d.RoleId != RoleModel.Instance.RoleId)
                        {
                            GameLog.Warn("Fight", "20001 defender 玩家 {0} 不在 SceneManager(未在视野),只记录不造假", d.RoleId);
                        }
                        continue;
                    }
                    if (d.Hp == 0)
                    {
                        GameLog.Info("Fight", "玩家 {0} 服务端判定死亡(hp=0),移除", d.RoleId);
                        mgr.DeleteSceneObj(d.RoleId);
                    }
                    else
                    {
                        mgr.ApplyHp(d.RoleId, d.Hp, r.HpLim);
                    }
                }
                else
                {
                    GameLog.Warn("Fight", "20001 defender 未路由 type_flag={0} id={1}(本轮只接怪/玩家/假人)", d.TypeFlag, d.RoleId);
                }
            }

            // 第18轮:combo 回包后通知 SceneController 继续击杀
            if (firstMonsterId > 0)
            {
                SceneController.Instance.OnRound18FightResult(skillId, firstMonsterId, firstMonsterAlive);
            }
        }

        private static void ApplyMonsterFightVisuals(FightVo vo)
        {
            // 进场演出(大妖来袭横幅)期间冻结"怪的攻击动作/受击表现"(对标老端:全屏演出视图打开时
            // fight-movie 跳过播放,观感=怪也等演出结束才开打;数据层 hp/死亡照常应用,不吞协议)。
            // 演出期怪打主角本就是 damage=0 的空挥(实测 flag=7),只砍表现零风险。
            if (AutoFight.AutoFightModel.Instance.CombatFreeze) return;

            if (vo.Attack.AttackerType == OBJ_MONSTER)
            {
                if (vo.Attack.RoleId >= 0 && vo.Attack.RoleId <= int.MaxValue)
                {
                    MonsterRenderer.PlaySkill((int)vo.Attack.RoleId, vo.Attack.SkillId);
                    GameLog.Info("Fight", "apply monster attack visual ins={0} skill={1}",
                        vo.Attack.RoleId, vo.Attack.SkillId);
                }
                else
                {
                    GameLog.Warn("Fight", "20001 attacker monster id out of int range: {0}", vo.Attack.RoleId);
                }
            }

            for (int i = 0; i < vo.DefenseList.Count; i++)
            {
                FightVo.DefenseInfo d = vo.DefenseList[i];
                if (d.TypeFlag != OBJ_MONSTER || d.Hp == 0) continue;
                if (d.RoleId < 0 || d.RoleId > int.MaxValue) continue;
                // 受击门槛(对标老端 FightMovieInfo.executeHitedAnimation:506 force_hited||damage>0||flag==ShanBi):
                // engage 帧(damage=0 flag=0)不播受击,免得普攻进战斗帧也让怪抽搐一下。
                if (d.Damage <= 0 && d.DamageFlag != DAMAGE_FLAG_DODGE) continue;
                MonsterRenderer.PlayBeHit((int)d.RoleId);
            }
        }

        /// <summary>
        /// 伤害飘字 + 主角自身 hp 同步(对标老端 FightDamageManager.ShowFont + RefreshObjVo 的 hp 写入):
        ///   · 飘字门槛 = 攻击者是主角 或 受击者是主角(其余玩家/怪互殴不飘,对标 ShowFont:426-440);
        ///   · 飘字位置 = 受击者头顶:怪取场景 vo 实时坐标(不在场退回协议坐标),主角取 RoleModel;
        ///   · 主角 hp:攻击头 hp(主角出手)/ defender hp(主角被击)都是服务端新绝对值 → BattleAttr.Hp
        ///     + EVT_ROLE_INFO_UPDATE,走 MainUITopView 既有血条/血字链,不开新路径。
        /// </summary>
        private static void ApplyDamageFontsAndMainRoleHp(FightVo vo)
        {
            long mainRoleId = RoleModel.Instance.RoleId;
            if (mainRoleId <= 0) return;

            bool attackerIsMainRole = vo.Attack.AttackerType == OBJ_ROLE && vo.Attack.RoleId == mainRoleId;
            if (attackerIsMainRole) ApplyMainRoleHp(vo.Attack.Hp);

            SceneManager mgr = SceneManager.Instance;
            for (int i = 0; i < vo.DefenseList.Count; i++)
            {
                FightVo.DefenseInfo d = vo.DefenseList[i];
                bool defenderIsMainRole = (d.TypeFlag == OBJ_ROLE || d.TypeFlag == OBJ_FAKE_ROLE) && d.RoleId == mainRoleId;
                if (defenderIsMainRole) ApplyMainRoleHp(d.Hp);

                if (!attackerIsMainRole && !defenderIsMainRole) continue;

                int wx, wy;
                if (defenderIsMainRole)
                {
                    wx = RoleModel.Instance.X;
                    wy = RoleModel.Instance.Y;
                }
                else if (d.TypeFlag == OBJ_MONSTER && d.RoleId >= 0 && d.RoleId <= int.MaxValue
                         && mgr.GetMonster((int)d.RoleId) != null)
                {
                    MonsterVo m = mgr.GetMonster((int)d.RoleId);
                    wx = m.X;
                    wy = m.Y;
                }
                else
                {
                    wx = d.PosX; // 不在场(已出九宫格等):退回协议里的受击坐标,不猜
                    wy = d.PosY;
                }
                DamageFontRenderer.ShowDamage(wx, wy, d.Damage, d.DamageFlag, defenderIsMainRole);
            }
        }

        /// <summary>主角服务端新绝对 hp → RoleModel.BattleAttr + 既有 EVT_ROLE_INFO_UPDATE 刷 HUD(值没变不发,防事件风暴)。</summary>
        private static void ApplyMainRoleHp(long newHp)
        {
            Shenxiao.Common.Proto.BattleAttrProto attr = RoleModel.Instance.BattleAttr;
            if (attr == null || newHp < 0 || attr.Hp == newHp) return;
            attr.Hp = newHp;
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
        }

        private static void ApplyMoveAnimToMainRole(FightVo vo)
        {
            if (AutoFight.AutoFightModel.Instance.CombatFreeze) return; // 演出期间不播位移表现(同上)
            MainRoleAgent agent = MainRoleAgent.Current;
            if (agent == null) return;

            long mainRoleId = RoleModel.Instance.RoleId;
            if (mainRoleId <= 0) return;

            if (vo.Attack.MoveAnim > 0
                && vo.Attack.AttackerType == OBJ_ROLE
                && vo.Attack.RoleId == mainRoleId)
            {
                agent.PlayMoveAnim(vo.Attack.MoveAnim, vo.Attack.PosX, vo.Attack.PosY);
                GameLog.Info("Fight", "apply main role attack move_anim={0} pos=({1},{2})",
                    vo.Attack.MoveAnim, vo.Attack.PosX, vo.Attack.PosY);
            }

            for (int i = 0; i < vo.DefenseList.Count; i++)
            {
                FightVo.DefenseInfo d = vo.DefenseList[i];
                if (d.MoveAnim <= 0) continue;
                if ((d.TypeFlag == OBJ_ROLE || d.TypeFlag == OBJ_FAKE_ROLE) && d.RoleId == mainRoleId)
                {
                    agent.PlayMoveAnim(d.MoveAnim, d.PosX, d.PosY);
                    GameLog.Info("Fight", "apply main role defender move_anim={0} pos=({1},{2})",
                        d.MoveAnim, d.PosX, d.PosY);
                }
            }
        }

        // ===================================================================================
        // Fight 扩容(自动循环 队列#2 轮2):20005/20007/20010/20013/20014/20015/20018/20020/20021/
        // 20022/20023/20024补recv/20027/20028。字段序权威源见 Proto.cs 对应常量注释(逐条核对
        // yu_server pt_200.erl 原文;与老端 ReadFmt 冲突处已按服务端 write 为准并在 Proto.cs 标注)。
        // ===================================================================================

        /// <summary>20024 战斗态服务端回显(对标规格"补 recv":echo type,与本地 _fighting 对齐)。</summary>
        private void On20024(NetReader r)
        {
            int type = r.ReadU8();
            bool fighting = type == 1;
            if (_fighting != fighting)
            {
                GameLog.Info("Fight", "recv 20024 战斗态服务端回显 type={0}(本地 {1}→{2},对齐)", type, _fighting, fighting);
                _fighting = fighting;
            }
            else
            {
                GameLog.Info("Fight", "recv 20024 战斗态服务端回显 type={0}(与本地一致)", type);
            }
        }

        /// <summary>20005 攻击失败返回(仅打日志,老端处理体整段死代码;字段序按服务端权威 pt_200.erl:106-110
        /// 写序,与老端 ReadFmt 冲突——详见 Proto.CS_FIGHT_ATTACK_FAIL 注释)。error_flag 语义(老端注释):
        /// 1=对方没血 2=出手太快 3=自己没血 4=距离太远 5=技能cd未到。</summary>
        private void On20005(NetReader r)
        {
            int errCode = r.ReadU8();
            int sign1 = r.ReadU8();
            long user1 = r.ReadU64();
            long hp1 = r.ReadU64();
            int x1 = r.ReadU16();
            int y1 = r.ReadU16();
            int sign2 = r.ReadU8();
            long user2 = r.ReadU64();
            long hp2 = r.ReadU64();
            int x2 = r.ReadU16();
            int y2 = r.ReadU16();
            List<long> inexistenceList = r.ReadArray(rr => (long)rr.ReadU32());
            GameLog.Warn("Fight",
                "recv 20005 攻击失败(log-only): errCode={0} attacker(sign={1} id={2} hp={3} pos=({4},{5})) defender(sign={6} id={7} hp={8} pos=({9},{10})) inexistence={11}",
                errCode, sign1, user1, hp1, x1, y1, sign2, user2, hp2, x2, y2, inexistenceList.Count);
        }

        /// <summary>20007 buff 技能清理广播(纯转发事件,消费方 TODO——buff UI 未移植)。</summary>
        private void On20007(NetReader r)
        {
            int typeFlag = r.ReadU8();
            long roleId = r.ReadU64();
            List<(int buffType, int buffSkillId)> list = r.ReadArray(rr => ((int)rr.ReadU16(), (int)rr.ReadU32()));
            GameLog.Info("Fight", "recv 20007 buff清理: type_flag={0} role_id={1} count={2}", typeFlag, roleId, list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_BUFF_CLEARED, typeFlag, roleId, list);
        }

        /// <summary>发送拾取怪物请求(对标 FightController.ts:896 onCollideMonsterHandler:动态 fmt "h"+n×"i")。</summary>
        public void PickMonsters(IReadOnlyList<int> instanceIds)
        {
            if (instanceIds == null || instanceIds.Count == 0) return;
            var fmt = new StringBuilder("h");
            var args = new List<object>(1 + instanceIds.Count) { instanceIds.Count };
            foreach (int id in instanceIds) { fmt.Append('i'); args.Add(id); }
            SendFmt(Proto.CS_PICK_MONSTER, fmt.ToString(), args.ToArray());
            GameLog.Info("Fight", "send 20010 拾取怪物请求 count={0}", instanceIds.Count);
        }

        /// <summary>20010 拾取怪物结果(对标 FightController.ts:661-682):error_code==1→toast「拾取成功」;
        /// 一律发事件清待拾取标记(老端失败提示分支已死代码化,仅保留成功提示,对标实测行为)。</summary>
        private void On20010(NetReader r)
        {
            List<(int errCode, int monId)> list = r.ReadArray(rr => ((int)rr.ReadU8(), (int)rr.ReadU32()));
            foreach ((int errCode, int monId) it in list)
            {
                if (it.errCode == 1) TipsManager.Toast("拾取成功");
            }
            GameLog.Info("Fight", "recv 20010 拾取结果 count={0}", list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_PICK_MON_RESULT, list);
        }

        /// <summary>查询登录死亡恢复信息(对标 20013 空包查询,pp_battle.erl:208-220 仅 hp&lt;=0 且有 LastBeKill
        /// 记录才回,其余静默 skip)。老端 GAME_START 不发它(纯登录死亡恢复用场景),本端亦不自动发,
        /// 只留 API 供未来登录流程按需调用。</summary>
        public void QueryKillerInfo()
        {
            SendFmt(Proto.CS_KILLER_INFO);
            GameLog.Info("Fight", "send 20013 查询死亡/击杀信息(本端未接自动触发,手动调用)");
        }

        /// <summary>20013 被杀信息(死亡→复活弹窗的唯一触发信号,对标 FightController.ts:506-541)。
        /// 中间 4 值(罪恶值/扣除元宝/玩家等级/几转)老端读后即弃,照读弃。killerName 3 级 fallback
        /// (老端"根因修复"逻辑,完整保留):①SceneManager 怪 vo 的 type_id 查 config_mon 名;
        /// ②Unity 侧无 BossSceneManager.select_boss_id 等价物(Boss域系统未移植)→ 此级跳过,TODO;
        /// ③killerId 直接当模板 id 查(老端兜底同款)。</summary>
        private void On20013(NetReader r)
        {
            int killerType = r.ReadU8();
            string killerName = r.ReadString();
            r.ReadU16(); // 现在的罪恶值,读弃
            r.ReadU8();  // 扣除的元宝,读弃(服务端恒传0)
            r.ReadU16(); // 玩家等级,读弃
            r.ReadU8();  // 几转,读弃
            long killerId = r.ReadU64();

            if (killerType == OBJ_MONSTER && killerId > 0)
            {
                int templateId = 0;
                if (killerId <= int.MaxValue)
                {
                    MonsterVo mv = SceneManager.Instance.GetMonster((int)killerId);
                    templateId = mv?.TypeId ?? 0;
                }
                // TODO: 老端第二级 fallback 是 BossSceneManager.select_boss_id(Boss域场景当前选中的 boss 模板id),
                // Unity 侧 Boss域系统未移植、无等价物可退,此级如实跳过,直接落到第三级兜底。
                if (templateId == 0 && killerId <= int.MaxValue) templateId = (int)killerId;

                MonsterConfigs.MonCfg cfg = templateId > 0 ? MonsterConfigs.Get(templateId) : null;
                if (cfg != null && !string.IsNullOrEmpty(cfg.Name)) killerName = cfg.Name;
            }

            Relive.ReliveModel.Instance.SetKiller(killerType, killerId, killerName);
            GameLog.Info("Fight", "recv 20013 被杀信息: killerType={0} killerName={1} killerId={2}", killerType, killerName, killerId);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_DEAD);
        }

        /// <summary>20014 击杀信息推送(老端无对应 recv 实现,按服务端权威 pt_200.erl:155-157 写序解析)。</summary>
        private void On20014(NetReader r)
        {
            string name = r.ReadString();
            int isShowPkV = r.ReadU8();
            int pkValue = r.ReadU16();
            GameLog.Info("Fight", "recv 20014 击杀信息推送(老端无 recv 实现,按服务端 write 序解析): name={0} isShowPkV={1} pkValue={2}",
                name, isShowPkV, pkValue);
            EventDispatcher.Emit(GlobalEvent.EVT_KILL_INFO, name, isShowPkV, pkValue);
        }

        /// <summary>20015 广播 PK 值(老端无对应 recv 实现,按服务端权威 pt_200.erl:160-161 写序解析:
        /// RoleId:l, PkValue:h——与规格草案假设的 "l,i" 冲突,以服务端源码为准,见汇报偏差项)。</summary>
        private void On20015(NetReader r)
        {
            long roleId = r.ReadU64();
            int pkValue = r.ReadU16();
            GameLog.Info("Fight", "recv 20015 pk值广播: role_id={0} pk_value={1}", roleId, pkValue);
            EventDispatcher.Emit(GlobalEvent.EVT_PK_VALUE_UPDATE, roleId, pkValue);
        }

        /// <summary>20018 清理刚放技能CD(老端无对应 recv 实现,按服务端权威 pt_200.erl:168-169 写序解析)。</summary>
        private void On20018(NetReader r)
        {
            int skillId = (int)r.ReadU32();
            GameLog.Info("Fight", "recv 20018 清理刚放技能CD skill_id={0}", skillId);
            EventDispatcher.Emit(GlobalEvent.EVT_SKILL_CD_CLEAR, skillId);
        }

        /// <summary>发送抢夺归属请求(对标 FightController.ts:875 SendFmtToGame(20020,"i",instance_id))。
        /// ⚠老端触发源(SNATCHING_OWNERSHIP 事件)全仓库无 UI Fire,发送侧当前孤立,交互点未来另补。</summary>
        public void SnatchOwnership(int insId)
        {
            if (insId <= 0) return;
            SendFmt(Proto.CS_SNATCH_OWNERSHIP, "i", insId);
            GameLog.Info("Fight", "send 20020 抢夺归属 ins={0}", insId);
        }

        /// <summary>20020 抢夺归属结果(对标 FightController.ts:542-551):error_code==1→toast「抢夺成功」+
        /// 归属事件(归属改为主角);否则错误码(Util.ErrorCodeShow 表未移植,显码降级)。</summary>
        private void On20020(NetReader r)
        {
            int errCode = (int)r.ReadU32();
            int monId = (int)r.ReadU32();
            GameLog.Info("Fight", "recv 20020 抢夺归属结果 errCode={0} monId={1}", errCode, monId);
            if (errCode == 1)
            {
                TipsManager.Toast("抢夺成功");
                EventDispatcher.Emit(GlobalEvent.EVT_MON_OWNER_UPDATE, (long)monId, RoleModel.Instance.RoleId);
            }
            else
            {
                TipsManager.Toast("抢夺失败(" + errCode + ")");
            }
        }

        /// <summary>发送查看归属请求(对标 FightController.ts:879 SendFmtToGame(20021,"i",instance_id))。
        /// ⚠老端触发源(CHECK_OWNERSHIP 事件)同样全仓库无 UI Fire,发送侧孤立,交互点未来另补。</summary>
        public void CheckOwnership(int insId)
        {
            if (insId <= 0) return;
            SendFmt(Proto.CS_CHECK_OWNERSHIP, "i", insId);
            GameLog.Info("Fight", "send 20021 查看归属 ins={0}", insId);
        }

        /// <summary>20021 查看归属结果(对标 FightController.ts:552-555)。</summary>
        private void On20021(NetReader r)
        {
            int monId = (int)r.ReadU32();
            long firstId = r.ReadU64();
            GameLog.Info("Fight", "recv 20021 查看归属 monId={0} firstAttackRoleId={1}", monId, firstId);
            EventDispatcher.Emit(GlobalEvent.EVT_MON_OWNER_UPDATE, (long)monId, firstId);
        }

        /// <summary>20022 模拟战斗结果/强制死亡广播(对标 FightController.ts:556-569)。died_id==主角:
        /// 同 20013 路径记录 killer,但不 Emit EVT_ROLE_DEAD(不开复活窗,对标老端 SetKillerInfo 不 Fire
        /// SHOWRELIVEWINDOW,弹窗信号只认 20013)。协议本身不带 killerName/killerType,故不触发 3 级 fallback。
        /// died_id 是他人:同步 hp/死亡到 SceneManager(参照 On20001 defense_list 的写法)。</summary>
        private void On20022(NetReader r)
        {
            long killerId = r.ReadU64();
            long diedId = r.ReadU64();
            long hp = r.ReadU64();
            long hpLim = r.ReadU64();

            long mainRoleId = RoleModel.Instance.RoleId;
            if (mainRoleId > 0 && diedId == mainRoleId)
            {
                Relive.ReliveModel.Instance.SetKiller(OBJ_ROLE, killerId, "");
                GameLog.Info("Fight", "recv 20022 主角死亡(模拟战斗):killer={0}(不开复活窗,对标老端)", killerId);
            }
            else
            {
                SceneManager mgr = SceneManager.Instance;
                RoleVo role = mgr.GetRole(diedId);
                if (role != null)
                {
                    if (hp <= 0) mgr.DeleteSceneObj(diedId);
                    else mgr.ApplyHp(diedId, hp, hpLim > 0 ? hpLim : role.HpLim);
                }
                else if (diedId >= 0 && diedId <= int.MaxValue && mgr.GetMonster((int)diedId) != null)
                {
                    int ins = (int)diedId;
                    if (hp <= 0)
                    {
                        MonsterRenderer.NotifyKilled(ins);
                        mgr.DeleteSceneObj(ins);
                    }
                    else
                    {
                        MonsterVo m = mgr.GetMonster(ins);
                        mgr.ApplyHp(ins, hp, hpLim > 0 ? hpLim : m.HpLim);
                    }
                }
                else
                {
                    GameLog.Warn("Fight", "recv 20022 died_id={0} 不在 SceneManager(未在视野),只记录不造假", diedId);
                }
                GameLog.Info("Fight", "recv 20022 模拟战斗死亡: killer={0} died={1} hp={2}/{3}", killerId, diedId, hp, hpLim);
            }

            EventDispatcher.Emit(GlobalEvent.EVT_SIMULATE_FIGHT, killerId, diedId);
        }

        /// <summary>发送战斗能量查询(对标 FightController.ts:570-573,空包)。</summary>
        public void QueryEnergy()
        {
            SendFmt(Proto.CS_FIGHT_ENERGY);
            GameLog.Info("Fight", "send 20023 查询战斗能量");
        }

        /// <summary>20023 战斗能量更新。</summary>
        private void On20023(NetReader r)
        {
            int energy = r.ReadU16();
            GameLog.Info("Fight", "recv 20023 能量值 energy={0}", energy);
            EventDispatcher.Emit(GlobalEvent.EVT_FIGHT_ENERGY, energy);
        }

        /// <summary>20027 技能CD结束时间通知(对标 FightController.ts:683-690)。⚠老端读取是**单条**
        /// (变量名 skill_list 但无 count 前缀/无循环,只 push 一个元素),不要脑补成数组循环。</summary>
        private void On20027(NetReader r)
        {
            int skillId = (int)r.ReadU32();
            long endTime = r.ReadU64();
            GameLog.Info("Fight", "recv 20027 技能CD结束 skill_id={0} end_time={1}", skillId, endTime);
            EventDispatcher.Emit(GlobalEvent.EVT_SKILL_CD_END, skillId, endTime);
        }

        /// <summary>20028 触发技能列表(对标 FightController.ts:692-703,伙伴/联携技能表现)。</summary>
        private void On20028(NetReader r)
        {
            List<int> skillIds = r.ReadArray(rr => (int)rr.ReadU32());
            GameLog.Info("Fight", "recv 20028 触发技能列表 count={0}", skillIds.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_TRIGGER_SKILLS, skillIds);
        }
    }
}
