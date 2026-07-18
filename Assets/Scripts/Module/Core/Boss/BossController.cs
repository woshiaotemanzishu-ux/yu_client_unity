using System;
using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Relive;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Boss
{
    /// <summary>
    /// BOSS(节日大妖)控制器。大 BOSS 系统只提取"节日大妖"主界面图标 51,本服/跨服 BOSS 玩法协议
    /// (46xxx/47xxx:进副本/BOSS信息/掉落/复活/幻域/圣兽岭…)全部不做。
    ///
    /// 图标 51 的驱动 = 自定义活动列表 33101(FEASTBOSS,base_type=51)——老端 BossModel.FeastBossActivity
    /// 读该活动 condition 的时间窗,处于窗内则 addIcon("51", 结束时间戳)(带倒计时),当天有下一场则
    /// addIcon("51", "HH:MM开启")(预告),否则 deleteIcon("51")。老端也没有专属 BOSS 协议驱动该图标,
    /// 纯粹是自定义活动 + 客户端时间窗计算。
    ///
    /// ★不在此重注册 33101★:Unity NetManager 每协议仅一处理器(RegisterProtocal 覆盖式),33101 已由
    /// CustomActivityController 独占并解析(它进游戏 RequestActivityList() 发过、回包驱动全部自定义活动图标);
    /// 若此处再注册会把所有自定义活动图标逻辑覆盖掉。理想接线是订阅 CustomActivity 侧解析 FEASTBOSS(51)
    /// 时间窗后广播的事件(对标老端 AddVipServiceController 订阅首充 EVT_FIRST_RECHARGE_UPDATE 的做法),
    /// 但当前 CustomActivityController 既不广播活动更新事件、也不暴露 act info,暂无可订阅信号 →
    /// 图标默认隐藏(等价"当前无节日BOSS活动开启"),这是非破坏、与"无活动"一致的安全态。
    /// 接线口子已留:CustomActivity 侧(或每秒时间窗计时器)算出 FEASTBOSS 时间窗后调 NotifyFeastBossActivity 即可点亮。
    ///
    /// 等级变化(EVT_ROLE_INFO_UPDATE,去抖)复评图标以对齐模板;节日BOSS图标无等级门(靠活动时间窗),此处仅作复检钩子。
    /// 本期只做图标;节日BOSS玩法/面板(采集/掉落/结算/入口)未移植,待用户验收。
    /// </summary>
    public sealed class BossController : BaseController
    {
        public static readonly BossController Instance = new BossController();
        private BossController() { }

        public const string ICON_TYPE = BossModel.ICON_TYPE;

        /// <summary>跨服千幻蜃楼(holy)子类型,对标老端 BossModel.BossType.holy(1+cross_boss_base_index=1001)
        /// - BossModel.cross_boss_base_index(1000) = 1(BossModel.ts:118,149)。47000 请求体带的就是这个"去掉
        /// 千位跨服偏移后"的子类型,不是协议号本身;BossModel.cs 不在本包文件所有权内(spec §2 P2),常量落在
        /// 本控制器,不新增 Model 字段。</summary>
        private const int CROSS_BOSS_HOLY_TYPE = 1;

        // 复评图标的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时复评。
        private int _lastLevel = -1;

        protected override void Register()
        {
            // 无专属 BOSS 图标协议可注册:驱动源 33101 已由 CustomActivityController 独占(见类注释,不可重注册)。
            // 模板对齐:等级变化复评(节日BOSS图标无等级门,仅复检钩子)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnServerHourRefresh);

            RegisterBossFamily();
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnServerHourRefresh);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            BossModel.Instance.Reset();
            BossModel.Instance.Clear46000();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>整点刷新(对标老端 BossController.ts:168-180,hour==4 连发 7 个请求):46000×5
        /// (suit/abyss/field/field_infinite/fieldspecial)+ 47000(holy 跨服子类型)+ 61020(专属大妖
        /// Vip_Rerson_Boss,老端经 boss_model.Fire(BaseDungeonModel.SCMD_REQUEST,61020,DUN_TYPE.Vip_Rerson_Boss)
        /// 转发,本端借道 DungeonController.RequestState 公开 API 直发,不重复注册 61020 发送口)。
        /// hour 恒为4(ServerTimeModel.RefreshHourList=[4]),此判断是镜像老端的冗余判断。</summary>
        private void OnServerHourRefresh(int hour)
        {
            if (hour != 4) return;
            RequestBossList(BossModel.BossType.Suit);
            RequestBossList(BossModel.BossType.Abyss);
            RequestBossList(BossModel.BossType.Field);
            RequestBossList(BossModel.BossType.FieldInfinite);
            RequestBossList(BossModel.BossType.FieldSpecial);
            SendFmt(Proto.KFBOSS_EUDEMONS_LIST, "c", CROSS_BOSS_HOLY_TYPE); // 47000
            DungeonController.Instance.RequestState(DungeonModel.TYPE_VIP_PERSON_BOSS); // 61020
            GameLog.Info("Boss", "HOUR_REFRESH==4 批量复请求 46000×5(suit/abyss/field/field_infinite/fieldspecial) + 47000 + 61020");
        }

        /// <summary>
        /// 进游戏钩子(GameStartController.RequestStartupPackets 调用)。驱动协议 33101 已由
        /// CustomActivityController.RequestActivityList() 请求,此处无需发包,仅按当前状态复评一次图标
        /// (此刻多半无节日BOSS活动、复评为空;真正驱动在 NotifyFeastBossActivity 被接线后)。
        /// </summary>
        public void RequestStartup()
        {
            RefreshIcon("startup");
        }

        /// <summary>
        /// 节日BOSS图标驱动入口(对标老端 CustomActivityModel base_type==FEASTBOSS → BossModel.FeastBossActivity)。
        /// 由 CustomActivityController 在 33101 列表刷新/复评时调用:
        ///   · hasActivity=false(列表里没有 FEASTBOSS 活动)→ 清图标;
        ///   · hasActivity=true → 按活动 condition 的每日时间窗算三态(窗内倒计时 / 当天下一场预告 / 都无则删)。
        /// 注:未接每秒定时器,窗口边界的自动切换靠 33101 刷新或等级/任务复评时重算(icon-first 阶段够用);
        /// 严格边界即时切换(老端 StartFeastTimer 每秒轮询)待后续接全局定时器基建后补。
        /// </summary>
        public void EvaluateFeastBoss(bool hasActivity, string condition, int actStartTime, int actEndTime)
        {
            if (!hasActivity)
            {
                NotifyFeastBossActivity(false, 0, null);
                return;
            }
            (bool active, int endTime, string foreshadow) =
                BossModel.ComputeFeastWindow(condition, actStartTime, actEndTime, TimeUtil.NowSec());
            NotifyFeastBossActivity(active, endTime, foreshadow);
        }

        /// <summary>
        /// 节日BOSS图标接线入口(对标老端 BossModel.FeastBossActivity 的结果三态):
        ///   · active=true  → 处于活动时间窗内,endTime=活动结束时间戳(倒计时);
        ///   · active=false 且 foreshadow 非空 → 当天有下一场未开始,显示 "HH:MM开启" 预告;
        ///   · 都不满足(foreshadow 传 null) → 删除图标 51。
        /// </summary>
        public void NotifyFeastBossActivity(bool active, int endTime, string foreshadow = null)
        {
            BossModel.Instance.SetFeastBossActivity(active, endTime, foreshadow);
            RefreshIcon("feastBoss");
        }

        private void RefreshIcon(string from)
        {
            BossModel m = BossModel.Instance;
            if (m.FeastBossActive)
            {
                // 活动时间窗内:带倒计时(time=结束时间戳),对标老端 addIcon("51", left_time)。
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, m.FeastBossEndTime);
            }
            else if (!string.IsNullOrEmpty(m.FeastBossForeshadow))
            {
                // 当天有下一场未开始:预告文本,对标老端 addIcon("51", null, null, "HH:MM开启")。
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, 0, m.FeastBossForeshadow);
            }
            else
            {
                ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            }

            GameLog.Info("Boss", "{0} 节日大妖: active={1} endTime={2} foreshadow={3} open={4}",
                from, m.FeastBossActive, m.FeastBossEndTime, m.FeastBossForeshadow, m.GetEntranceOpenState());
        }

        // 模板对齐:主角等级变化(去抖)复评图标(节日BOSS图标无等级门,复检无害)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RefreshIcon("levelChange");
        }

        // ============================================================================================
        // Boss 家族一期·本服核心(自动循环 轮15a)。范围/裁决/死号见 BossModel.cs 顶部注释与 Proto.cs 各号注释。
        // ============================================================================================

        private static void ShowError(int errorCode) => TipsManager.Toast("错误(" + errorCode + ")"); // 错误码表未移植,显码降级

        private void RegisterBossFamily()
        {
            RegisterProtocal(Proto.BOSS_COLLECT_QUERY, On20025);
            RegisterProtocal(Proto.BOSS_COLLECT_INTERRUPT, On20026);

            RegisterProtocal(Proto.WAR_FREE_INFO, On20201);
            RegisterProtocal(Proto.WAR_FREE_USE, On20202);
            RegisterProtocal(Proto.WAR_FREE_END_TIME, On20203);
            RegisterProtocal(Proto.WAR_FREE_UPDATE, On20204);
            RegisterProtocal(Proto.WAR_FREE_END, On20205);

            RegisterProtocal(Proto.BOSS_LIST, On46000);
            RegisterProtocal(Proto.BOSS_KILL_LOG, On46001);
            RegisterProtocal(Proto.BOSS_DROP_LOG, On46002);
            RegisterProtocal(Proto.BOSS_ENTER, On46003);
            RegisterProtocal(Proto.BOSS_LEAVE, On46004);
            RegisterProtocal(Proto.BOSS_ANGER, On46005);
            RegisterProtocal(Proto.BOSS_ANGER_TIME, On46006);
            RegisterProtocal(Proto.BOSS_REMIND, On46007);
            RegisterProtocal(Proto.BOSS_REVIVE_REMIND, On46008); // 复用 On46016 解析(结构相同,老端同款复用)
            RegisterProtocal(Proto.BOSS_REBORN, On46009);
            RegisterProtocal(Proto.BOSS_COLLECT_TIMES, On46013); // 防御recv(修复轮订正,见 Proto.cs 注释)
            RegisterProtocal(Proto.BOSS_DAILY_RESET, On46014);   // 防御recv(每日重置 send_to_all 空包,同类一致)
            RegisterProtocal(Proto.BOSS_TIRED, On46011);
            RegisterProtocal(Proto.BOSS_SETTLE_REWARD, On46015);
            RegisterProtocal(Proto.BOSS_KILLED_NOTICE, On46016);
            RegisterProtocal(Proto.BOSS_DAMAGE_RANK_TOP3, On46019);
            RegisterProtocal(Proto.BOSS_DAMAGE_RANK_SELF, On46022);
            RegisterProtocal(Proto.BOSS_DKILL_NOTICE, On46024);
            RegisterProtocal(Proto.BOSS_ROLE_INFO, On46025);
            RegisterProtocal(Proto.BOSS_FEAST_HIDE_BOX, On46026);     // 防御recv,节日boss场景组
            RegisterProtocal(Proto.BOSS_FEAST_BOX_REFRESH, On46027);  // 防御recv,节日boss场景组
            RegisterProtocal(Proto.BOSS_FEAST_COLLECT_RESULT, On46028); // 防御recv,节日boss场景组
            RegisterProtocal(Proto.BOSS_FEAST_ALL_KILLED, On46029);   // 防御recv,节日boss场景组
            RegisterProtocal(Proto.BOSS_DOMAIN_BOX_OWNER, On46031);   // 防御recv(修复轮订正,见 Proto.cs 注释)
            RegisterProtocal(Proto.BOSS_FEAST_NEXT_WAVE, On46033);    // 防御recv,节日boss场景组
            RegisterProtocal(Proto.BOSS_DEATH_DEBUFF, On46034);
            RegisterProtocal(Proto.BOSS_DOMAIN_LAYER, On46035);
            RegisterProtocal(Proto.BOSS_REBORN_POS, On46036);
            RegisterProtocal(Proto.BOSS_HP_SHOW, On46040); // 防御recv+补发送,见 Proto.cs 注释
            RegisterProtocal(Proto.BOSS_REVIVE_CONSUME, On46041);
            RegisterProtocal(Proto.BOSS_REVIVE_NOTICE, On46042);
            RegisterProtocal(Proto.BOSS_VIT_ACK, On46043);
            RegisterProtocal(Proto.BOSS_VIT_DETAIL, On46044);
            RegisterProtocal(Proto.BOSS_VIT_RECOVER, On46045);

            // 死号严禁注册(逐号裁决见双报告§死码 + Proto.cs 46037/38/39/46 注释):
            // 发送侧死号(全仓库 zero SCMD_REQUEST):46001(已按 wire 权威补齐,见上,不算死);46032。
            // 接收侧遗弃且不可达(C2S 老端已弃用、且非自主推送,我方不发起请求则永不可达):
            //   46010/46012/46017/46018/46020/46021/46023/46032。
            // 服务端 write 调用点已被注释/从未真正 send:46030(lib_boss.erl:802 `%` 注释)。
            // 跨服 kf_great_demon 委托壳(pp_boss.erl handle 无条件转发 mod_great_demon_local,与 boss_type 取值无关):
            //   46037/46038/46039/46046。
            // 47000-47035/47101-47117/61900-61902 全跨服族:本轮完全不接(留15b)。
            // 修复轮订正:46013/46031 初审误判"非自主推送不可达",直接核实服务端 send_to_scene/send_to_uid
            // 均无条件真推送(Proto.cs 详注)——已改为防御 recv 登记,不再列入死号。46026-46029/46033(节日boss场景
            // 组)+46040(血条百分比)同样改为防御 recv/数据层登记,不再是静默缺口。
        }

        // ---------------------------------------------------------------------------------------
        // 采集(20025-26)
        // ---------------------------------------------------------------------------------------

        /// <summary>老端触发点:`BROADCAST_COLLECT_RESULT` flag==13 且 `IsHolyBossScene()`——Unity 无场景采集
        /// 钩子(BossSceneManager 等价物未接),对外暴露发送方法供未来场景系统调用,TODO 接线。</summary>
        public void NotifyCollectBroken(int monsterInsId, int monsterTypeId) =>
            SendFmt(Proto.BOSS_COLLECT_QUERY, "ii", monsterInsId, monsterTypeId);

        private void On20025(NetReader r)
        {
            int count = r.ReadU16();
            var roleIds = new List<long>(count);
            for (int i = 0; i < count; i++) roleIds.Add(r.ReadU64());
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_COLLECT_UPDATE, roleIds);
            GameLog.Info("Boss", "20025 采集怪当前采集对象 count={0}(场景消费钩子 TODO)", roleIds.Count);
        }

        private void On20026(NetReader r)
        {
            long role = r.ReadU64();
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_COLLECT_UPDATE, new List<long> { role });
            GameLog.Info("Boss", "20026 采集被打断 role={0}(场景消费钩子 TODO)", role);
        }

        // ---------------------------------------------------------------------------------------
        // 免战(20201-205)
        // ---------------------------------------------------------------------------------------

        public void RequestWarFreeInfo() => SendFmt(Proto.WAR_FREE_INFO);
        public void UseWarFreeProtect(int sceneType) => SendFmt(Proto.WAR_FREE_USE, "i", sceneType);
        public void RequestWarFreeEndTime() => SendFmt(Proto.WAR_FREE_END_TIME);
        public void EndWarFreeProtect(int sceneType) => SendFmt(Proto.WAR_FREE_END, "i", sceneType);

        private void On20201(NetReader r)
        {
            List<BossModel.WarFreeEntry> list = r.ReadArray(ReadWarFreeEntry);
            BossModel.Instance.ApplyWarFreeList(list);
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_WAR_FREE_UPDATE);
            GameLog.Info("Boss", "20201 免战保护信息 count={0}", list.Count);
        }

        private static BossModel.WarFreeEntry ReadWarFreeEntry(NetReader r) => new BossModel.WarFreeEntry
        {
            SceneType = r.ReadI32(), ProtectTime = r.ReadI32(), UseCount = r.ReadI32(),
        };

        private void On20202(NetReader r)
        {
            int errorCode = r.ReadI32();
            int sceneType = r.ReadI32();
            int protectTime = r.ReadI32();
            int useCount = r.ReadI32();
            if (errorCode == 1)
            {
                BossModel.Instance.UpsertWarFree(sceneType, protectTime, useCount);
                EventDispatcher.Emit(GlobalEvent.EVT_BOSS_WAR_FREE_UPDATE);
                RequestWarFreeEndTime();
            }
            else
            {
                ShowError(errorCode);
            }
            GameLog.Info("Boss", "20202 使用免战保护 errorCode={0} sceneType={1}", errorCode, sceneType);
        }

        private void On20203(NetReader r)
        {
            long endTime = r.ReadI32();
            BossModel.Instance.SetWarFreeEndTime(endTime, TimeUtil.NowSec());
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_WAR_FREE_UPDATE);
            GameLog.Info("Boss", "20203 免战保护结束时间 endTime={0} left={1}s", endTime, BossModel.Instance.WarFreeEndTimeLeft);
        }

        private void On20204(NetReader r)
        {
            int sceneType = r.ReadI32();
            int protectTime = r.ReadI32();
            int useCount = r.ReadI32();
            BossModel.Instance.UpsertWarFree(sceneType, protectTime, useCount);
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_WAR_FREE_UPDATE);
            GameLog.Info("Boss", "20204 免战保护时间更新推送 sceneType={0} protectTime={1} useCount={2}", sceneType, protectTime, useCount);
        }

        private void On20205(NetReader r)
        {
            int errorCode = r.ReadI32();
            int sceneType = r.ReadI32();
            if (errorCode == 1)
            {
                BossModel.Instance.SetWarFreeEndTime(0, TimeUtil.NowSec());
                EventDispatcher.Emit(GlobalEvent.EVT_BOSS_WAR_FREE_UPDATE);
                RequestWarFreeInfo();
                RequestWarFreeEndTime();
            }
            else
            {
                ShowError(errorCode);
            }
            GameLog.Info("Boss", "20205 结束免战保护 errorCode={0} sceneType={1}", errorCode, sceneType);
        }

        // ---------------------------------------------------------------------------------------
        // 本服 Boss 主链(46000 段)
        // ---------------------------------------------------------------------------------------

        public void RequestBossList(int bossType) => SendFmt(Proto.BOSS_LIST, "c", bossType);
        public void RequestKillLog(int bossType, int bossId) => SendFmt(Proto.BOSS_KILL_LOG, "ci", bossType, bossId);
        public void RequestDropLog() => SendFmt(Proto.BOSS_DROP_LOG);
        public void EnterBoss(int bossType, int bossId) => SendFmt(Proto.BOSS_ENTER, "ci", bossType, bossId);
        public void LeaveBoss(int bossType) => SendFmt(Proto.BOSS_LEAVE, "c", bossType);

        /// <summary>发 "cicc" boss_type,boss_id,remind,auto_state(默认 0,对标老端 SCMD_REQUEST switch)。</summary>
        public void SetBossRemind(int bossType, int bossId, bool remind, bool autoState = false) =>
            SendFmt(Proto.BOSS_REMIND, "cicc", bossType, bossId, remind ? 1 : 0, autoState ? 1 : 0);

        public void ConsumeBossRevive(int bossType, int bossId) => SendFmt(Proto.BOSS_REVIVE_CONSUME, "ci", bossType, bossId);
        public void RequestBossVitAck(int bossType) => SendFmt(Proto.BOSS_VIT_ACK, "c", bossType);
        public void RequestBossVitDetail(int bossType) => SendFmt(Proto.BOSS_VIT_DETAIL, "c", bossType);

        /// <summary>找回体力,发 "ch" boss_type,vit_back_num(u16)。</summary>
        public void RecoverBossVit(int bossType, int vitBackNum) => SendFmt(Proto.BOSS_VIT_RECOVER, "ch", bossType, vitBackNum);

        /// <summary>新野外boss死亡debuff状态查询,纯header无body(对标老端 default SendFmtToGame(cmd))。</summary>
        public void RequestBossDeathDebuff() => SendFmt(Proto.BOSS_DEATH_DEBUFF);

        private void On46000(NetReader r)
        {
            int bossType = r.ReadU8();
            int allCount = r.ReadU8();
            int count = r.ReadU8();
            int tired = r.ReadU16();
            int allTired = r.ReadU16();
            int vit = r.ReadU16();
            long lastVitTime = r.ReadU32();
            int collectTimes = r.ReadU8();
            int allCollectTimes = r.ReadU8();
            List<BossModel.BossEntry> list = r.ReadArray(ReadBossEntry);
            BossModel.Instance.ApplyBossList(bossType, allCount, count, tired, allTired, vit, lastVitTime,
                collectTimes, allCollectTimes, list);
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_LIST_UPDATE, bossType);
            GameLog.Info("Boss", "46000 boss列表 type={0} allCount={1} count={2} tired={3}/{4} vit={5} bossN={6}",
                bossType, allCount, count, tired, allTired, vit, list.Count);
        }

        private static BossModel.BossEntry ReadBossEntry(NetReader r) => new BossModel.BossEntry
        {
            BossId = r.ReadI32(), Num = r.ReadU8(), RebornTime = r.ReadU32(), IsRemind = r.ReadU8() != 0, AutoRemind = r.ReadU8() != 0,
        };

        /// <summary>46001 击杀日志,服务端 ?BOSS_LOG_LEN=100 硬顶(轮15a CliVerify 断言点)。</summary>
        private void On46001(NetReader r)
        {
            List<BossModel.KillLogEntry> list = r.ReadArray(ReadKillLogEntry);
            BossModel.Instance.ApplyKillLog(list);
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_KILL_LOG_UPDATE);
            GameLog.Info("Boss", "46001 击杀日志 count={0}", list.Count);
        }

        private static BossModel.KillLogEntry ReadKillLogEntry(NetReader r) => new BossModel.KillLogEntry
        {
            Time = r.ReadU32(), RoleId = r.ReadU64(), Name = r.ReadString(),
        };

        private void On46002(NetReader r)
        {
            List<BossModel.DropLogEntry> list = r.ReadArray(ReadDropLogEntry);
            BossModel.Instance.ApplyDropLog(list);
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_DROP_LOG_UPDATE);
            GameLog.Info("Boss", "46002 全局掉落日志 count={0}", list.Count);
        }

        private static BossModel.DropLogEntry ReadDropLogEntry(NetReader r)
        {
            var e = new BossModel.DropLogEntry
            {
                Time = r.ReadU32(), RoleId = r.ReadU64(), Name = r.ReadString(),
                BossType = r.ReadU8(), BossId = r.ReadI32(), GoodsId = r.ReadI32(), Num = r.ReadU32(), Rating = r.ReadU32(),
            };
            e.EquipExtraAttr = r.ReadArray(ReadEquipExtraAttr);
            e.IsTop = r.ReadU8() != 0;
            return e;
        }

        private static BossModel.EquipExtraAttrEntry ReadEquipExtraAttr(NetReader r) => new BossModel.EquipExtraAttrEntry
        {
            Color = r.ReadU8(), TypeId = r.ReadU8(), AttrId = r.ReadU16(), AttrVal = r.ReadU32(),
            PlusInterval = r.ReadU8(), PlusUnit = r.ReadU32(),
        };

        /// <summary>轮15b 补注(跨服壳复用确认):本 handler 同样服务 KfGreatDemon(boss_type=20,太古遗凶)
        /// 的进场景结果——服务端 `enter_check` 用同步 call 在本服完成完整 guard,通过后才 apply_cast 转发
        /// 跨服节点;**成功无任何 46003 包**(靠场景切换事件隐式确认),仅失败显式回此号,与本服其余 BossType
        /// 完全同一惯例,无需单独接线。</summary>
        private void On46003(NetReader r)
        {
            int code = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_ENTER_RESULT, true, code);
            if (code != 1) ShowError(code);
            GameLog.Info("Boss", "46003 进入Boss场景 code={0}", code);
        }

        private void On46004(NetReader r)
        {
            int code = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_ENTER_RESULT, false, code);
            if (code != 1) ShowError(code);
            GameLog.Info("Boss", "46004 离开Boss场景 code={0}", code);
        }

        /// <summary>46005 蛮荒禁地/跨服大妖怒气值(纯服务端推送,老端从未主动请求——本端同样只 recv)。</summary>
        private void On46005(NetReader r)
        {
            int anger = r.ReadU16();
            int maxAnger = r.ReadU16();
            GameLog.Info("Boss", "46005 怒气值 anger={0}/{1}", anger, maxAnger);
        }

        /// <summary>46006 蛮荒禁地退出倒计时(登记防御 recv,r15_server 直接核实服务端仍在推,老端 TS 已弃用)。</summary>
        private void On46006(NetReader r)
        {
            int type = r.ReadU8();
            int tickoutTime = r.ReadU8();
            GameLog.Info("Boss", "46006 退出倒计时(防御recv) type={0} tickoutTime={1}", type, tickoutTime);
        }

        private void On46007(NetReader r)
        {
            int code = r.ReadI32();
            int bossType = r.ReadU8();
            int bossId = r.ReadI32();
            int remind = r.ReadU8();
            int isAuto = r.ReadU8();
            if (code == 1)
            {
                BossModel.BossEntry e = BossModel.Instance.GetBossState(bossType)?.GetEntry(bossId);
                if (e != null) e.IsRemind = remind != 0;
                EventDispatcher.Emit(GlobalEvent.EVT_BOSS_REMIND_UPDATE, bossType, bossId);
                if (isAuto == 0) TipsManager.Toast(remind != 0 ? "已关注" : "已取消关注");
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Boss", "46007 关注操作 code={0} bossType={1} bossId={2} remind={3} isAuto={4}",
                code, bossType, bossId, remind, isAuto);
        }

        /// <summary>46008 单播复活提醒(与 46016 结构相同,老端 On46008 直接复用 46016 解析)。</summary>
        private void On46008(NetReader r) => On46016(r);

        /// <summary>46009 Boss 重生广播。**rule10 订正**:老端 `||` 恒真 bug 已修,KILL_BOSS 事件只对
        /// <see cref="BossModel.ShouldNotifyKillBoss"/> 命中的类型触发;数据落地(ApplyBossReborn)不受此门控制,
        /// 任意 boss_type 都要正确刷新列表。</summary>
        private void On46009(NetReader r)
        {
            int bossType = r.ReadU8();
            int bossId = r.ReadI32();
            long rebornTime = r.ReadU32();
            int num = r.ReadU8();
            BossModel.Instance.ApplyBossReborn(bossType, bossId, rebornTime, num);
            if (BossModel.ShouldNotifyKillBoss(bossType))
            {
                EventDispatcher.Emit(GlobalEvent.EVT_BOSS_REBORN, bossType, bossId);
            }
            GameLog.Info("Boss", "46009 Boss重生 bossType={0} bossId={1} rebornTime={2} num={3} notify={4}",
                bossType, bossId, rebornTime, num, BossModel.ShouldNotifyKillBoss(bossType));
        }

        /// <summary>46013 幻兽领(Eudaemon)采集次数广播(防御recv,修复轮订正:详见 Proto.cs 注释,服务端
        /// 无条件 send_to_scene,不是"不可达"死号)。</summary>
        private void On46013(NetReader r)
        {
            int bossType = r.ReadU8();
            int bossId = r.ReadI32();
            int num = r.ReadU8();
            GameLog.Info("Boss", "46013 采集次数广播(防御recv) bossType={0} bossId={1} num={2}", bossType, bossId, num);
        }

        /// <summary>46014 每日 boss 重置全服广播(防御recv,空包;老端零消费,收到仅记日志——将来接"每日重置
        /// 后列表刷新"时在此补逐类型重拉)。</summary>
        private void On46014(NetReader r)
        {
            GameLog.Info("Boss", "46014 每日boss重置广播(防御recv,空包)");
        }

        private void On46011(NetReader r)
        {
            int bossTired = r.ReadU8();
            BossModel.BossTypeState s = BossModel.Instance.GetBossState(BossModel.BossType.Field);
            if (s != null) s.Tired = bossTired;
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_LIST_UPDATE, BossModel.BossType.Field);
            // 对标老端注释"联动补发46044刷新完整体力信息"。
            RequestBossVitDetail(BossModel.BossType.Field);
            GameLog.Info("Boss", "46011 疲劳值广播 bossTired={0}", bossTired);
        }

        private void On46015(NetReader r)
        {
            int rewardType = r.ReadU8();
            int len = r.ReadU16();
            for (int i = 0; i < len; i++) { r.ReadU8(); r.ReadU32(); r.ReadU32(); r.ReadU64(); } // Type,GoodsTypeId,Num,Id
            GameLog.Info("Boss", "46015 结算奖励推送 rewardType={0} rewardCount={1}(无独立UI消费,数据落地留TODO)", rewardType, len);
        }

        /// <summary>46016 击杀/复活提醒(修复轮订正:对标老端 SetBossRebornTime(boss_type,boss_id,0),
        /// 复活提醒到达后把该 boss 条目的倒计时复位,否则残留旧 RebornTime 会被 UI 误判为仍在冷却)。</summary>
        private void On46016(NetReader r)
        {
            int bossType = r.ReadU8();
            int bossId = r.ReadI32();
            BossModel.BossEntry e = BossModel.Instance.GetBossState(bossType)?.GetEntry(bossId);
            if (e != null)
            {
                e.RebornTime = 0;
                EventDispatcher.Emit(GlobalEvent.EVT_BOSS_LIST_UPDATE, bossType);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_KILLED_NOTICE, bossType, bossId);
            GameLog.Info("Boss", "46016 击杀/复活提醒 bossType={0} bossId={1}", bossType, bossId);
        }

        /// <summary>46019 伤害榜前3防抖广播(轮15a 订正,详见 Proto.cs 注释:直接核实服务端仍在真发)。</summary>
        private void On46019(NetReader r)
        {
            List<BossModel.DamageRankEntry> list = r.ReadArray(ReadDamageRankEntry);
            BossModel.Instance.ApplyDamageRankTop3(list);
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_DAMAGE_RANK_UPDATE);
            GameLog.Info("Boss", "46019 伤害榜前3(500ms防抖广播) count={0}", list.Count);
        }

        private static BossModel.DamageRankEntry ReadDamageRankEntry(NetReader r) => new BossModel.DamageRankEntry
        {
            RoleName = r.ReadString(), Damage = r.ReadU32(),
        };

        /// <summary>46022 伤害榜-自己(非拉取,recv 纯被动落表——伤害发生后服务端自动回,不提供请求方法)。</summary>
        private void On46022(NetReader r)
        {
            int selfRank = r.ReadU8();
            long selfDamage = r.ReadU32();
            string selfName = r.ReadString();
            long distance = r.ReadU32();
            BossModel.Instance.ApplyDamageRankSelf(selfRank, selfDamage, selfName, distance);
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_DAMAGE_RANK_UPDATE);
            GameLog.Info("Boss", "46022 伤害榜-自己 rank={0} damage={1} name={2}", selfRank, selfDamage, selfName);
        }

        /// <summary>46024 连杀通知场景广播(轮15a 订正,详见 Proto.cs 注释:`lib_boss_mod:dkill_notice/2` 确被调用)。
        /// dkill&gt;2 且是自己连杀才带 index、他人连杀按5倍数才播报的节流逻辑属表现层,本轮只落数据,TODO。</summary>
        private void On46024(NetReader r)
        {
            long roleId = r.ReadU64();
            FigureProto figure = FigureProto.Read(r);
            int dkill = r.ReadU16();
            GameLog.Info("Boss", "46024 连杀通知 roleId={0} name={1} dkill={2}(播报节流留TODO)", roleId, figure?.name, dkill);
        }

        /// <summary>46025 世界boss广播role信息壳(老端 `switch(vo.key){}` 空 case,占位壳,本端同样只落原始
        /// key/val,不做业务分支)。</summary>
        private void On46025(NetReader r)
        {
            int len = r.ReadU16();
            for (int i = 0; i < len; i++) { r.ReadU8(); r.ReadU32(); } // Key,Val
            GameLog.Info("Boss", "46025 role信息壳 count={0}(占位,无业务分支)", len);
        }

        // ---------------------------------------------------------------------------------------
        // 节日boss场景组(46026-46029/46033,修复轮补注册防御 recv,详见 Proto.cs 注释——数据层,
        // 消费方随 FeastBoss 玩法/BossFightSceneView HUD 轮接入,TODO)。
        // ---------------------------------------------------------------------------------------

        private void On46026(NetReader r)
        {
            int len = r.ReadU16();
            for (int i = 0; i < len; i++) r.ReadI32(); // BoxId
            GameLog.Info("Boss", "46026 节日boss隐藏宝箱列表(防御recv) count={0}", len);
        }

        private void On46027(NetReader r)
        {
            int bossId = r.ReadI32();
            int bossX = r.ReadI32();
            int bossY = r.ReadI32();
            int len = r.ReadU16();
            for (int i = 0; i < len; i++)
            {
                r.ReadU16(); r.ReadU16();                                          // X,Y
                r.ReadI32(); r.ReadI32();                                          // AutoId,MonCfgId
                r.ReadU64(); r.ReadU64();                                          // Hp,HpLim
                r.ReadU16();                                                       // Lv
                r.ReadString();                                                    // Name
                r.ReadU16();                                                       // Sp
                r.ReadI32();                                                       // MonResource
                r.ReadString();                                                    // MonRes
                r.ReadI32(); r.ReadI32();                                          // ImagId,WeaponId
                r.ReadU8(); r.ReadU8(); r.ReadU8(); r.ReadU8(); r.ReadU8();        // AttType,Kind,Color,OnHook,Boss
                r.ReadU32();                                                       // CollectTime
                r.ReadU8(); r.ReadU8(); r.ReadU8(); r.ReadU8();                    // IsBeClicked,IsBeAtted,Hide,Ghost
                r.ReadU16();                                                       // MonGroup
                r.ReadU64();                                                       // GuildId
                r.ReadU16();                                                       // Angel
                r.ReadU8();                                                        // AttrType
                r.ReadI32();                                                       // Title
            }
            GameLog.Info("Boss", "46027 节日boss宝箱刷新广播(防御recv) bossId={0} pos=({1},{2}) count={3}",
                bossId, bossX, bossY, len);
        }

        private void On46028(NetReader r)
        {
            int code = r.ReadU8();
            int len = r.ReadU16();
            for (int i = 0; i < len; i++) { r.ReadU8(); r.ReadI32(); r.ReadI32(); } // Type,GoodsTypeId,Num(write_object_list)
            GameLog.Info("Boss", "46028 节日boss采集结算结果(防御recv) code={0} rewardCount={1}", code, len);
        }

        private void On46029(NetReader r)
        {
            GameLog.Info("Boss", "46029 节日boss全部击杀(防御recv,空包)");
        }

        /// <summary>46031 秘境宝箱归属信息广播(防御recv,修复轮订正:详见 Proto.cs 注释,服务端无条件
        /// send_to_scene/send_to_uid,不是"不可达"死号)。</summary>
        private void On46031(NetReader r)
        {
            int bossId = r.ReadI32();
            long roleId = r.ReadU64();
            string name = r.ReadString();
            int career = r.ReadU8();
            int lv = r.ReadU16();
            long combat = r.ReadU64();
            string picture = r.ReadString();
            long pictureVer = r.ReadU32();
            long time = r.ReadU32();
            int curTimes = r.ReadU16();
            int limitTimes = r.ReadU16();
            GameLog.Info("Boss", "46031 秘境宝箱归属(防御recv) bossId={0} roleId={1} name={2} lv={3} combat={4} curTimes={5}/{6}",
                bossId, roleId, name, lv, combat, curTimes, limitTimes);
        }

        private void On46033(NetReader r)
        {
            long nextWave = r.ReadU32();
            long time = r.ReadU32();
            GameLog.Info("Boss", "46033 节日boss下一波倒计时(防御recv) nextWave={0} time={1}", nextWave, time);
        }

        /// <summary>46034 新野外boss死亡debuff状态——转发 ReliveModel 死亡次数槽位(spec 明示接线点)。
        /// 复活窗精确路由留 TODO(ReliveController.OpenReliveWindow 既有说明)。</summary>
        private void On46034(NetReader r)
        {
            int dieTimes = r.ReadU16();
            long nextEnterTime = r.ReadU32();
            long debuffEndTime = r.ReadU32();
            long safeEndTime = r.ReadU32();
            ReliveModel.Instance.SetBossDieInfo(dieTimes, nextEnterTime, debuffEndTime, safeEndTime);
            GameLog.Info("Boss", "46034 死亡debuff dieTimes={0} nextEnterTime={1} debuffEnd={2} safeEnd={3}",
                dieTimes, nextEnterTime, debuffEndTime, safeEndTime);
        }

        private void On46035(NetReader r)
        {
            int bossType = r.ReadU8();
            int layer = r.ReadU8();
            GameLog.Info("Boss", "46035 秘境领域层数广播 bossType={0} layer={1}", bossType, layer);
        }

        /// <summary>46036 Boss/大妖复活坐标点位广播(通常与 46009 成对,本端只落日志,无独立坐标UI消费点)。</summary>
        private void On46036(NetReader r)
        {
            int bossId = r.ReadI32();
            int len = r.ReadU16();
            for (int i = 0; i < len; i++) { r.ReadU16(); r.ReadU16(); } // X,Y
            GameLog.Info("Boss", "46036 复活坐标点位 bossId={0} count={1}", bossId, len);
        }

        /// <summary>46040 boss血量百分比查询(请求驱动,C2S 空包,对标老端 StartUpdateBossHp 每5s 轮询)。</summary>
        public void RequestBossHpShow() => SendFmt(Proto.BOSS_HP_SHOW);

        /// <summary>46040 boss血量百分比(防御recv,数据层落地,战斗HUD消费方留TODO)。</summary>
        private void On46040(NetReader r)
        {
            int len = r.ReadU16();
            for (int i = 0; i < len; i++)
            {
                r.ReadI32();  // MonId
                r.ReadU64();  // AutoId
                r.ReadU64();  // Hp
                r.ReadU64();  // HpMax
            }
            GameLog.Info("Boss", "46040 boss血量百分比(防御recv) count={0}(战斗HUD消费留TODO)", len);
        }

        /// <summary>46041 消耗复活。修复轮订正:成功分支补 RequestBossList(bossType) 重拉列表(对标老端
        /// `SendFmtToGame(46000,"c",scmd.boss_type)`),否则场外/未订阅场景广播的模型会长期陈旧。</summary>
        private void On46041(NetReader r)
        {
            int errcode = r.ReadI32();
            int bossType = r.ReadU8();
            int bossId = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_REVIVE_RESULT, errcode == 1, bossType, bossId);
            if (errcode == 1) RequestBossList(bossType);
            else ShowError(errcode);
            GameLog.Info("Boss", "46041 消耗复活 errcode={0} bossType={1} bossId={2}", errcode, bossType, bossId);
        }

        /// <summary>46042 Boss进出/复活成功广播。修复轮订正:对标老端 else 分支补 RequestBossList(bossType)
        /// 重拉列表;boss_type==10(Eudaemon,跨服千幻蜃楼分支 SendFmtToGame(47000,"c",1))属47000跨服族,留15b。</summary>
        private void On46042(NetReader r)
        {
            int bossType = r.ReadU8();
            int bossId = r.ReadI32();
            if (bossType != BossModel.BossType.Eudaemon) RequestBossList(bossType);
            GameLog.Info("Boss", "46042 Boss进出/复活成功广播 bossType={0} bossId={1}", bossType, bossId);
        }

        private void On46043(NetReader r)
        {
            GameLog.Info("Boss", "46043 体力查询ack(空包,真实数据走46044)");
        }

        private void On46044(NetReader r)
        {
            int vit = r.ReadU16();
            int maxVit = r.ReadU16();
            int addVit = r.ReadU16();
            int backVit = r.ReadU16();
            long lastVitTime = r.ReadU32();
            // BossType 未随包下发,老端靠请求上下文关联——本端按当前唯一消费方 Field(NEW_OUTSIDE)落地,
            // SPECIAL 类型体力若未来接线需要独立跟踪,请求侧自行按返回顺序关联(TODO)。
            BossModel.Instance.ApplyVit(BossModel.BossType.Field, vit, maxVit, addVit, backVit, lastVitTime);
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_VIT_UPDATE, BossModel.BossType.Field);
            GameLog.Info("Boss", "46044 体力详情 vit={0}/{1} add={2} back={3}", vit, maxVit, addVit, backVit);
        }

        /// <summary>46045 找回体力。**rule9 订正**:S2C 字段名是 `code`,老端失败分支误读不存在的
        /// `scmd.errcode`(恒 undefined)——本端一律按 wire 真实字段 `code` 判定,不照抄老端笔误。</summary>
        private void On46045(NetReader r)
        {
            int code = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_BOSS_VIT_RECOVER_RESULT, code == 1);
            if (code == 1) RequestBossVitDetail(BossModel.BossType.Field);
            else ShowError(code);
            GameLog.Info("Boss", "46045 找回体力 code={0}", code);
        }
    }
}
