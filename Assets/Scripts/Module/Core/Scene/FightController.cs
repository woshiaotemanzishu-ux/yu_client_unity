using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
    }
}
