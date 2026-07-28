using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Dungeon
{
    /// <summary>
    /// 通用副本进出结算控制器(对标老端 commonController/BaseDungeonController.ts;服务端 pt_610)。
    /// 御魂本(config_dungeon.type=12,dun_id 12001~)最小闭环:61001 进入 → 61003 结算推送 → 61002 退出
    /// (61002 已在 <see cref="Shenxiao.Module.Core.AutoBrush.AutoBrushController"/> 注册,复用其常量
    /// Proto.DUNGEON_EXIT,本控制器不重复注册/不再发起独立处理,Exit() 仅 SendFmt 转发)。
    ///
    /// ⚠61003 vs 61013 抉择(侦察实证,见工单交付报告):老端结算入口是 <b>61003</b>「通用结算界面」
    /// (BaseDungeonController.ts:767 起注册,御魂本 dun_type=Rune 走第 911/976 行分支,统一走
    /// SetDungeonResultInfo + OPEN_DENGEON_RESULT_VIEW/TryAddResultDrop 收尾);61013 在老端源码整棵
    /// h5/src 树里 <b>零处注册处理器</b>,proto610.d.ts 里的类型声明写明 desc="结算界面加好友,邀请加入
    /// 公会,积分展示"——是结算面板上的社交附加协议(好友/公会邀请),配合 61011/61012 服务"助战类"副本,
    /// 并非御魂本会收到的结算本体。本控制器按侦察结论只接 61003;61013 常量保留(Proto.DUNGEON_SETTLE)
    /// 但不注册,如后续实测服务端确有下发再补。
    ///
    /// 轮9 副本家族补全一期(逐号裁决见 r9 三份侦察 + 规格§0):
    ///   接:61004 副本信息(双路:loading 白名单服务端主动推/其余 61001 成功补发)+ 61005·61030 波次 +
    ///      61007·61019 坐标事件状态机 + 61009 剧情推送→事件 + 61011 助战次数 + 61018 退出倒计时 +
    ///      61021 购买(全组共享 vip_count 分支+6100043 婚姻本专文案)+ 61022 扫荡 + 61023 时间评分 +
    ///      61025·61026 鼓舞 + 61044 经验本面板推送 + 61045 冷却时间 + 61120·61121 资源本一键与次数 +
    ///      50801·50802 周本(独立 PolarModel 数据线)。
    ///   发送封装:61010 剧情事件("iic",老端 StoryController 直发序,勿抄 BaseDungeonController 死分支 ilc)。
    ///   跳过:61006/61014/61015/61016/61017/61024/61027(老端 h5/src 全树零引用 UNUSED)、
    ///      61028(被 61120+61121 取代的死协议)、61012/61029/61057/61060/61099/61119(服务端 DEAD)、
    ///      61031-41(守卫公会本,归公会包)、61112-16(灵魄本奖励系统,记灵魄包)、61118(限时爬塔二期)、
    ///      50805(周本专属结算推送,DungeonPolarBalance 面板未移植,TODO 周本二期)。
    ///   连锁:61001 成功→按类型乐观计数/Equip·Dragon 补发 61020/非 loading 类型补发 61004;
    ///      61003·61020 →补发 61121(对标老端 RequestDungeonNum);GAME_START/等级变化/任务推进→
    ///      500ms 防抖对 InitStateDunTypes 白名单批量 61020(对标 CheckAllDunInitState);
    ///      进副本场景(EVT_SCENE_MAP_READY 且 DunId≠0)→固定重发 61004/61018/61030 三连+61019 对账。
    ///
    /// 轮22 族错误出口批补 61000(家族统一错误壳)+ 61047(回应邀请进入副本)+ 61092(异兽入侵领取阶段奖励,
    /// 老端成功分支已注释,纯错误出口)。
    /// 轮231 补 61046 邀请/取消请求与发送者原始消息；轮232 补 61048 双方完整原始状态快照。
    /// 轮241 补 61058 神纹跳关奖励 S2C-only 原始快照，不触发 UI 或本地发奖。
    /// 轮242 补 61059 高级经验副本波数面板 S2C-only 原始快照，不公开服务端空查询。
    /// 轮244 补 61061 高级经验副本跳关进入 S2C-only 原始快照，与 61059 独立。
    /// 轮245 补 61062 副本开关设置显式查询与按 dun_id 原始快照，不派生 UI/鼓舞状态。
    /// 轮246 补 61063 副本开关设置更新；成功只重查 61062，失败只显码，不乐观改模型。
    /// 轮248 补 61065 入场自动鼓舞 S2C 权威计数，复用 61026 被动状态语义。
    /// 轮250 补 61088 周本特殊信息显式查询与按 dun_id 原始整包快照，不解析或合并 term。
    /// </summary>
    public sealed class DungeonController : BaseController
    {
        public static readonly DungeonController Instance = new DungeonController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_cooldownOutboundIntercept = null;
        private static Func<byte[], bool> s_inviteOutboundIntercept = null;
        private static Func<byte[], bool> s_dragonBestRecordOutboundIntercept = null;
        private static Func<byte[], bool> s_dragonStageRewardOutboundIntercept = null;
        private static Func<byte[], bool> s_dragonQuickInfoOutboundIntercept = null;
        private static Func<byte[], bool> s_dragonSkillInfoOutboundIntercept = null;
        private static Func<byte[], bool> s_dungeonSettingInfoOutboundIntercept = null;
        private static Func<byte[], bool> s_dungeonSettingUpdateOutboundIntercept = null;
        private static Func<byte[], bool> s_polarSpecialInfoOutboundIntercept = null;
#endif

        private DungeonController() { }

        // CheckAllDunInitState 防抖(对标老端 setTimeout 500ms;epoch 自增使旧批次失效)。
        private int _checkEpoch;
        // 等级变化门(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,只在等级真变时重查——同 BaseDungeonController 塔图标去抖)。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.DUNGEON_ERROR, On61000);
            RegisterProtocal(Proto.DUNGEON_ENTER, On61001);
            RegisterProtocal(Proto.DUNGEON_SETTLE_UI, On61003);
            RegisterProtocal(Proto.DUNGEON_STATE, On61020);
            RegisterProtocal(Proto.DUNGEON_INVITE, On61046);
            RegisterProtocal(Proto.DUNGEON_INVITE_RESPOND, On61047);
            RegisterProtocal(Proto.DUNGEON_INVITE_STATE, On61048);
            RegisterProtocal(Proto.DUNGEON_DRAGON_BEST_RECORD, On61050);
            RegisterProtocal(Proto.DUNGEON_DRAGON_STAGE_REWARD, On61051);
            RegisterProtocal(Proto.DUNGEON_DRAGON_QUICK_INFO, On61053);
            RegisterProtocal(Proto.DUNGEON_DRAGON_SKILL_INFO, On61055);
            RegisterProtocal(Proto.DUNGEON_DRAGON_JUMP_REWARD, On61058);
            RegisterProtocal(Proto.DUNGEON_ADVANCED_EXP_INFO, On61059);
            RegisterProtocal(Proto.DUNGEON_ADVANCED_EXP_JUMP_INFO, On61061);
            RegisterProtocal(Proto.DUNGEON_SETTING_INFO, On61062);
            RegisterProtocal(Proto.DUNGEON_SETTING_UPDATE, On61063);
            RegisterProtocal(Proto.DUNGEON_INSPIRIT_ENTRY_STATE, On61065);
            RegisterProtocal(Proto.DUNGEON_POLAR_SPECIAL_INFO, On61088);
            RegisterProtocal(Proto.DUNGEON_MONSTER_INVASION_REWARD, On61092);
            // 61002(DUNGEON_EXIT)已由 AutoBrushController 注册,红线不可重复注册;Exit() 只发不接。
            RegisterProtocal(Proto.DUNGEON_INFO, On61004);
            RegisterProtocal(Proto.DUNGEON_WAVE_PUSH, On61005);
            RegisterProtocal(Proto.DUNGEON_POS_EVENT, On61007);
            RegisterProtocal(Proto.DUNGEON_STORY_PUSH, On61009);
            RegisterProtocal(Proto.DUNGEON_HELP_COUNT, On61011);
            RegisterProtocal(Proto.DUNGEON_EXIT_TIME, On61018);
            RegisterProtocal(Proto.DUNGEON_POS_EVENT_LIST, On61019);
            RegisterProtocal(Proto.DUNGEON_BUY_COUNT, On61021);
            RegisterProtocal(Proto.DUNGEON_SWEEP, On61022);
            RegisterProtocal(Proto.DUNGEON_SCORE_STATE, On61023);
            RegisterProtocal(Proto.DUNGEON_INSPIRIT, On61025);
            RegisterProtocal(Proto.DUNGEON_INSPIRIT_STATE, On61026);
            RegisterProtocal(Proto.DUNGEON_NEXT_WAVE_TIME, On61030);
            RegisterProtocal(Proto.DUNGEON_EXP_PANEL, On61044);
            RegisterProtocal(Proto.DUNGEON_COOLDOWN, On61045);
            RegisterProtocal(Proto.DUNGEON_RESOURCE_ONEKEY, On61120);
            RegisterProtocal(Proto.DUNGEON_RESOURCE_COUNT, On61121);
            RegisterProtocal(Proto.POLAR_WEEK_INFO, On50801);
            RegisterProtocal(Proto.POLAR_RANK, On50802);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdated);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_MAP_READY, OnSceneMapReady);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.On<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnServerHourRefresh);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_SCENE_MAP_READY, OnSceneMapReady);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnServerHourRefresh);
            ++_checkEpoch;   // 使在途防抖批次失效
            _lastLevel = -1;
            DungeonModel.Instance.Clear();
            PolarModel.Instance.Clear();
            base.Dispose();
        }

        // =====================================================================================
        // 61020 触发时机(对标老端 Init() 的 CheckAllDunInitState:GAME_START/等级变化/任务推进 →
        // 500ms 防抖 → 对白名单里未 init 的类型批量发 61020。DAY_CHANGE/HOUR_REFRESH==4 的重置路
        // 见下方 OnServerDayChange/OnServerHourRefresh,轮20 已接入 ServerClock 事件源)。
        // =====================================================================================

        private void OnGameStart()
        {
            // 对标老端 GAME_START:ResetData(全类型待重拉)后批量补请求。
            DungeonModel.Instance.ResetDunInitState();
            CheckAllDunInitState();
        }

        /// <summary>跨天(对标老端 BaseDungeonController.ts:230-240 DAY_CHANGE):无条件 ResetDunInitState +
        /// CheckAllDunInitState(老端另有 setTimeout(fn,3) 才调用 CheckAllDunInitState,CheckAllDunInitState
        /// 自身又是 500ms 防抖,3ms 在这条链路里可忽略——本端直接复用既有 500ms 防抖链,见 spec_serverclock_round20.md
        /// §2 P2)。同一老端处理器内还调用 local_BaseDungeonModel_Instance.RequestLimitTowerData()(爬塔请求,
        /// BaseDungeonController.ts:237),但那部分数据/发送不归本文件所有——已挂在 BaseDungeon/BaseDungeonController.cs
        /// (P4 包,独立订阅同一个 EVT_SERVER_DAY_CHANGE),本控制器不重复实现。</summary>
        private void OnServerDayChange()
        {
            DungeonModel.Instance.ResetDunInitState();
            CheckAllDunInitState();
        }

        /// <summary>整点刷新(对标老端 BaseDungeonController.ts:242-253 HOUR_REFRESH):无条件
        /// ResetDunInitState;仅 hour==4 才补触发 CheckAllDunInitState(RefreshHourList=[4],hour 恒为4,
        /// 此判断是镜像老端的冗余判断,非本端引入)。</summary>
        private void OnServerHourRefresh(int hour)
        {
            DungeonModel.Instance.ResetDunInitState();
            if (hour == 4) CheckAllDunInitState();
        }

        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            CheckAllDunInitState();   // 对标老端 CHANGE_LEVEL → CheckAllDunInitState
        }

        private void OnTaskListUpdated()
        {
            CheckAllDunInitState();   // 对标老端 UPDATE_NEWEST_TASK_ID → CheckAllDunInitState
        }

        /// <summary>500ms 防抖批量补 61020(对标 BaseDungeonController.ts:200-212)。
        /// ⚠老端 CheckDunInitState 还有 CheckDunOpenState(功能开放)门控——功能开放门控数据未接线,
        /// 本端不拦(多发的 61020 服务端安全回空表);老端 InitDunData 里 Rune 额外发 61113/61115(灵魄
        /// 奖励包,规格§0 归灵魄包跳过)、Heart 额外发 61101(技能列表,pp_dungeon_sec),均记 TODO 不发。</summary>
        private void CheckAllDunInitState()
        {
            _ = CheckAllDunInitStateAsync(++_checkEpoch);
        }

        private async Task CheckAllDunInitStateAsync(int epoch)
        {
            await Shenxiao.Framework.Util.TimeUtil.Delay(500);
            if (epoch != _checkEpoch || !IsInitialized) return;
            DungeonModel model = DungeonModel.Instance;
            int sent = 0;
            foreach (int dunType in DungeonModel.InitStateDunTypes)
            {
                if (model.GetDunInitState(dunType)) continue;
                model.SetDunInitState(dunType);
                RequestState(dunType);
                sent++;
            }
            if (sent > 0) GameLog.Info("Dungeon", "CheckAllDunInitState 批量补 61020 ×{0}", sent);
        }

        /// <summary>进副本场景固定重发三连+坐标对账(对标老端 DungeonFightSceneView.LoadCustomLogic:337-339
        /// 发 61004/61018/61030,BaseDungeonController.EnterSceneHandle:2096 发 61019)。离开副本回野外时
        /// 清副本内临时状态(坐标事件/波次/倒计时,对标老端 ClearDungeonInfo)。</summary>
        private void OnSceneMapReady()
        {
            RoleModel role = RoleModel.Instance;
            if (role.DunId != 0)
            {
                SendFmt(Proto.DUNGEON_INFO);
                SendFmt(Proto.DUNGEON_EXIT_TIME);
                SendFmt(Proto.DUNGEON_NEXT_WAVE_TIME);
                SendFmt(Proto.DUNGEON_POS_EVENT_LIST, "i", role.SceneId);
                GameLog.Info("Dungeon", "进副本场景重发三连 61004/61018/61030 + 61019(scene={0})", role.SceneId);
            }
            else
            {
                DungeonModel.Instance.ClearDungeonInfo();
            }
        }

        /// <summary>进入副本(对标老端 675 行 61001 请求;发 "i" dun_id)。</summary>
        public void Enter(int dunId)
        {
            if (dunId <= 0) return;
            SendFmt(Proto.DUNGEON_ENTER, "i", dunId);
            GameLog.Info("Dungeon", "enter 61001 dun_id={0}", dunId);
        }

        /// <summary>退出副本(复用 AutoBrushController 已注册的 61002 回包处理,本控制器只发不接,无参)。</summary>
        public void Exit()
        {
            SendFmt(Proto.DUNGEON_EXIT);
            GameLog.Info("Dungeon", "exit 61002(回包由 AutoBrushController.On61002 处理)");
        }

        /// <summary>请求副本状态/次数(对标 pt_610 read(61020):请求体仅 dun_type:c 一个字段)。</summary>
        public void RequestState(int dunType)
        {
            SendFmt(Proto.DUNGEON_STATE, "c", dunType);
            GameLog.Info("Dungeon", "request 61020 dun_type={0}", dunType);
        }

        /// <summary>请求副本信息 61004(空参;loading 白名单类型服务端主动推,通常不需要手动调)。</summary>
        public void RequestDungeonInfo() => SendFmt(Proto.DUNGEON_INFO);

        /// <summary>请求退出倒计时 61018(裸发,无参)。</summary>
        public void RequestExitTime() => SendFmt(Proto.DUNGEON_EXIT_TIME);

        /// <summary>请求下一波怪物时间 61030(裸发,无参)。</summary>
        public void RequestNextWaveTime() => SendFmt(Proto.DUNGEON_NEXT_WAVE_TIME);

        /// <summary>查询指定副本的绝对冷却结束时间。0/最大 u32 均按 wire 原样发送。</summary>
        public void RequestCooldown(uint dunId)
        {
#if UNITY_EDITOR
            if (s_cooldownOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.DUNGEON_COOLDOWN, "i", dunId);
                if (s_cooldownOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.DUNGEON_COOLDOWN, "i", dunId);
        }

        /// <summary>邀请(type=1)/取消邀请(type=2)。不等待 ACK；邀请状态另由 61048 承载。</summary>
        public void RequestInvite(byte type, uint dunId, ulong otherId)
        {
            if (type != 1 && type != 2) return;
            long wireOtherId = unchecked((long)otherId);
#if UNITY_EDITOR
            if (s_inviteOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.DUNGEON_INVITE, "cil",
                    new object[] { type, dunId, wireOtherId });
                if (s_inviteOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.DUNGEON_INVITE, "cil", type, dunId, wireOtherId);
        }

        /// <summary>显式查询神纹副本指定波次的个人/最佳记录；不由生命周期或 UI 自动触发。</summary>
        public void RequestDragonBestRecord(uint dunId, byte wave)
        {
#if UNITY_EDITOR
            if (s_dragonBestRecordOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.DUNGEON_DRAGON_BEST_RECORD, "ic",
                    new object[] { dunId, wave });
                if (s_dragonBestRecordOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.DUNGEON_DRAGON_BEST_RECORD, "ic", dunId, wave);
        }

        /// <summary>显式查询神纹副本阶段奖励领取情况；不由 GAME_START 或入口自动触发。</summary>
        public void RequestDragonStageRewardInfo(uint dunId)
        {
#if UNITY_EDITOR
            if (s_dragonStageRewardOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.DUNGEON_DRAGON_STAGE_REWARD, "i", dunId);
                if (s_dragonStageRewardOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.DUNGEON_DRAGON_STAGE_REWARD, "i", dunId);
        }

        /// <summary>显式查询神纹副本快速出怪信息；严格空包，不由生命周期或 UI 自动触发。</summary>
        public void RequestDragonQuickInfo()
        {
#if UNITY_EDITOR
            if (s_dragonQuickInfoOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.DUNGEON_DRAGON_QUICK_INFO, null, null);
                if (s_dragonQuickInfoOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.DUNGEON_DRAGON_QUICK_INFO);
        }

        /// <summary>显式查询神纹副本临时技能数量；严格空包，不由生命周期或 UI 自动触发。</summary>
        public void RequestDragonSkillInfo()
        {
#if UNITY_EDITOR
            if (s_dragonSkillInfoOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.DUNGEON_DRAGON_SKILL_INFO, null, null);
                if (s_dragonSkillInfoOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.DUNGEON_DRAGON_SKILL_INFO);
        }

        /// <summary>显式查询指定副本的开关设置；不由生命周期或 UI 自动触发。</summary>
        public void RequestDungeonSettingInfo(uint dunId)
        {
#if UNITY_EDITOR
            if (s_dungeonSettingInfoOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.DUNGEON_SETTING_INFO, "i", dunId);
                if (s_dungeonSettingInfoOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.DUNGEON_SETTING_INFO, "i", dunId);
        }

        /// <summary>显式更新指定副本开关；服务端成功后由 61063 回包触发 61062 权威重查。</summary>
        public void RequestDungeonSetting(uint dunId, byte type, byte selectType, byte isOpen, byte count)
        {
#if UNITY_EDITOR
            if (s_dungeonSettingUpdateOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.DUNGEON_SETTING_UPDATE, "icccc",
                    new object[] { dunId, type, selectType, isOpen, count });
                if (s_dungeonSettingUpdateOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.DUNGEON_SETTING_UPDATE, "icccc", dunId, type, selectType, isOpen, count);
        }

        /// <summary>请求坐标触发情况表 61019(发 "i" scene_id;进副本场景对账用)。</summary>
        public void RequestPosEventList(int sceneId) => SendFmt(Proto.DUNGEON_POS_EVENT_LIST, "i", sceneId);

        /// <summary>请求助战剩余次数 61011(发 "i" dun_id;神纹/装备本入口用)。</summary>
        public void RequestHelpCount(int dunId) => SendFmt(Proto.DUNGEON_HELP_COUNT, "i", dunId);

        /// <summary>购买副本次数 61021(发 "ih" dun_id,1——老端 UI 恒传 count=1,无批量购买入口)。
        /// ⚠老端 DungeonBuyTimeView 有 can_buy_ 预校验(VIP 特权额度算出,不足直接跳 VIP 购买页不发协议)
        /// ——VIP 特权表未移植,本端直接发,由服务端 check_buy_count 校验(err610_buy_* 系错误码)。</summary>
        public void BuyCount(int dunId)
        {
            SendFmt(Proto.DUNGEON_BUY_COUNT, "ih", dunId, 1);
            GameLog.Info("Dungeon", "request 61021 buy dun_id={0} count=1", dunId);
        }

        /// <summary>扫荡 61022(发 "ih" dun_id, auto_num)。老端触发点带 SWEEPING_GOODS_ID(38040001)
        /// 扫荡券库存预检(不足弹 DungeonMaterialAlertView 购买)——该弹窗未移植,预检留给调用方 UI(TODO)。</summary>
        public void Sweep(int dunId, int autoNum)
        {
            if (autoNum <= 0) return;
            SendFmt(Proto.DUNGEON_SWEEP, "ih", dunId, autoNum);
            GameLog.Info("Dungeon", "request 61022 sweep dun_id={0} auto_num={1}", dunId, autoNum);
        }

        /// <summary>请求当前时间评分状态 61023(⚠裸发无参——老端调用方传的 dun_id 会被 switch default
        /// 分支静默丢弃,r9 侦察坑位,不要以为要带 dun_id)。</summary>
        public void RequestScoreState() => SendFmt(Proto.DUNGEON_SCORE_STATE);

        /// <summary>鼓舞 61025(发 "c" cost_type;1=铜币,2=元宝。经验副本"鼓舞"面板用)。</summary>
        public void Inspirit(int costType)
        {
            if (costType != 1 && costType != 2) return;   // 服务端 guard COIN/GOLD
            SendFmt(Proto.DUNGEON_INSPIRIT, "c", costType);
        }

        /// <summary>请求鼓舞状态 61026(裸发;进经验本战斗界面/打开鼓舞面板各查一次)。</summary>
        public void RequestInspiritState() => SendFmt(Proto.DUNGEON_INSPIRIT_STATE);

        /// <summary>资源副本一键操作 61120(发 "c" oper_type;1=一键挑战(无消耗),2=一键扫荡(有消耗)。
        /// 对标老端 RequestDungeonChallenge;服务端要求周卡激活(err610_no_active_weekly_card)。</summary>
        public void RequestResourceOneKey(int operType)
        {
            if (operType != 1 && operType != 2) return;
            SendFmt(Proto.DUNGEON_RESOURCE_ONEKEY, "c", operType);
            GameLog.Info("Dungeon", "request 61120 oper_type={0}", operType);
        }

        /// <summary>资源副本次数查询 61121(发 "c" dun_type;0=全部资源副本类型,对标老端 RequestDungeonNum:
        /// dun_type==0 时先清本地表再全量重建;非资源类型服务端静默 skip,老端也是无条件发,行为保留)。</summary>
        public void RequestDungeonNum(int dunType)
        {
            if (dunType == 0) DungeonModel.Instance.ClearResourceCounts();
            SendFmt(Proto.DUNGEON_RESOURCE_COUNT, "c", dunType);
        }

        /// <summary>剧情事件 61010 发送封装(发 "iic" story_id, sub_story_id, is_end;is_end 0/1,服务端 guard)。
        /// ⚠字段序以老端 StoryController.ts:600 直发为准(r9 侦察实证)——BaseDungeonController.ts:347 的
        /// "ilc"分支是全仓库零触发的死代码,勿抄。服务端无回包(纯 ack),不注册接收。剧情播放系统未移植,
        /// 本封装供后续 Story/Dialogue 通道调用。</summary>
        public void SendStoryEvent(int storyId, int subStoryId, int isEnd)
        {
            if (isEnd != 0 && isEnd != 1) return;   // 服务端 guard IsEnd==0 orelse IsEnd==1
            SendFmt(Proto.DUNGEON_STORY_EVENT, "iic", storyId, subStoryId, isEnd);
            GameLog.Info("Dungeon", "request 61010 story={0} sub={1} isEnd={2}", storyId, subStoryId, isEnd);
        }

        /// <summary>主角移动检查(对标老端 onMainRoleMoveHandler:进入 trigger_state==1 事件的范围 → 置 2 并发
        /// 61007,坐标用**事件目标点**而非主角位置——老端 TriggerFlushMonster(tx,ty))。
        /// TODO:MainRoleAgent 尚无"主角移动"事件广播(老端 MAINROLE_MOVE_EVENT),场景层接线后在移动回调里调本方法;
        /// 坐标事件表本身也待场景元素配置解析接入(DungeonModel.AddPosEvent)。</summary>
        public void OnMainRoleMoved(int x, int y)
        {
            DungeonModel.PosEventVo hit = DungeonModel.Instance.TryEnterPosEvent(RoleModel.Instance.SceneId, x, y);
            if (hit == null) return;
            SendFmt(Proto.DUNGEON_POS_EVENT, "hh", hit.PosX, hit.PosY);
            GameLog.Info("Dungeon", "request 61007 坐标事件触发 pos=({0},{1})", hit.PosX, hit.PosY);
        }

        /// <summary>周本信息 50801(裸发;周本大厅加载时查一次)。</summary>
        public void RequestPolarInfo() => SendFmt(Proto.POLAR_WEEK_INFO);

        /// <summary>周本榜单 50802(发 "icc" team_dun_id, 1, RANK_MAX——老端固定查 1~10 名;
        /// 服务端 guard Rank1&lt;Rank2 且 Rank1&gt;0 且 Rank2≤30,越界静默无响应)。</summary>
        public void RequestPolarRank(int teamDunId) =>
            SendFmt(Proto.POLAR_RANK, "icc", teamDunId, 1, PolarModel.RANK_MAX);

        /// <summary>显式查询当前周本副本特殊信息；不自动挂载场景加载、HP 或生命周期事件。</summary>
        public void RequestPolarSpecialInfo()
        {
#if UNITY_EDITOR
            if (s_polarSpecialInfoOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.DUNGEON_POLAR_SPECIAL_INFO, null, null);
                if (s_polarSpecialInfoOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.DUNGEON_POLAR_SPECIAL_INFO);
        }

        /// <summary>61000 通用副本(pt_610)家族统一错误出口(对标老端 BaseDungeonController.ts:668-673
        /// "通用错误返回",无条件 ErrorCodeShow(error_code)。服务端 send_dungeon_msg/2(lib_dungeon.erl:1341-1345)
        /// 是副本大量失败分支共享的错误壳,回包恒为错误码,老端忽略 error_code_args,本端同样只消费不透出)。
        /// 错误码表未移植,显码降级。</summary>
        private void On61000(NetReader r)
        {
            int code = (int)r.ReadU32();
            string args = r.ReadString();
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("Dungeon", "61000 家族错误壳 code={0} args={1}", code, args);
        }

        /// <summary>61001 进入回包:dun_id:i, scene_id:i, error_code:i, error_code_args:s。
        /// error_code==1 成功(对标老端 BaseDungeonController.ts:675~681,与 61002 的"1=成功"同一套约定)。</summary>
        private void On61001(NetReader r)
        {
            int dunId = (int)r.ReadU32();
            int sceneId = (int)r.ReadU32();
            int errorCode = (int)r.ReadU32();
            string errorCodeArgs = r.ReadString();

            if (errorCode != 1)
            {
                TipsManager.Toast("进入副本失败(" + errorCode + (string.IsNullOrEmpty(errorCodeArgs) ? "" : "," + errorCodeArgs) + ")");
                GameLog.Warn("Dungeon", "61001 enter fail dun_id={0} errorCode={1} args={2}", dunId, errorCode, errorCodeArgs);
                return;
            }

            DungeonModel.Instance.Apply61001(dunId);
            TipsManager.Toast("进入副本:" + DungeonConfigs.GetName(dunId));
            GameLog.Info("Dungeon", "61001 enter ok dun_id={0} scene_id={1}", dunId, sceneId);

            // —— 连锁行为(对标老端 61001 成功分支,BaseDungeonController.ts:694-747)——
            int dunType = DungeonConfigs.GetType(dunId);
            ApplyEnterSideEffects(dunType, dunId);
            // loading 白名单类型服务端进副本后主动推 61004;其余类型客户端显式补发空参(:745-747)。
            if (dunType != 0 && !DungeonModel.IsLoadingDunType(dunType))
            {
                SendFmt(Proto.DUNGEON_INFO);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_UPDATE);
        }

        /// <summary>61001 成功的按类型副作用(对标老端 switch dun_type):
        /// Vip_Rerson_Boss →全条目 daily_count+1、命中条目 is_sweep=1(本地乐观更新);
        /// Exp/AdvancedExp →非帮派经验本那条 daily_count+1(老端合服经验卡 GetMergeExpCount 批量档未移植,按 +1);
        /// Equip/Dragon →主动补发 61020 刷新状态。SingleRank/SentientAct 的子 Model 转发未移植(TODO 天境/诸天包)。</summary>
        private void ApplyEnterSideEffects(int dunType, int dunId)
        {
            DungeonModel model = DungeonModel.Instance;
            if (!model.DunStatesByType.TryGetValue(dunType, out List<DungeonModel.DunState> list) || list == null)
            {
                if (dunType == DungeonModel.TYPE_EQUIP || dunType == DungeonModel.TYPE_DRAGON) RequestState(dunType);
                return;
            }
            switch (dunType)
            {
                case DungeonModel.TYPE_VIP_PERSON_BOSS:
                    foreach (DungeonModel.DunState vo in list)
                    {
                        if (vo.DunId == dunId) vo.IsSweep = true;
                        vo.DailyCount += 1;
                    }
                    break;
                case DungeonModel.TYPE_EXP:
                case DungeonModel.TYPE_ADVANCED_EXP:
                    foreach (DungeonModel.DunState vo in list)
                    {
                        if (vo.DunId != DungeonModel.GUILD_EXP_ID) { vo.DailyCount += 1; break; }
                    }
                    break;
                case DungeonModel.TYPE_EQUIP:
                case DungeonModel.TYPE_DRAGON:
                    RequestState(dunType);
                    break;
            }
        }

        /// <summary>61003 结算推送(对标老端"通用结算界面";字段序照 ClientProtocol.json "61003" 逐个读完):
        /// result:c, result_subtype:c, dun_id:i, grade:c, scene_id:i,
        /// reward_list[u16×{style:c,typeId:i,count:l,goods_id:l}],
        /// other_reward[u16×{reward_type:c, other_reward_list[u16×{style1:c,type_id1:i,count1:l,goods_id1:l}]}],
        /// ex_data[u16×{key:h,val:i}], count:c。result==1 成功(与 61001/61002 同一套"1=成功"约定)。</summary>
        private void On61003(NetReader r)
        {
            int result = r.ReadU8();
            int resultSubtype = r.ReadU8();
            int dunId = (int)r.ReadU32();
            int grade = r.ReadU8();
            int sceneId = (int)r.ReadU32();

            var rewards = new List<(int typeId, long num)>();
            // 结算界面展示用:经 GoodsModel.GetMappingTypeId 还原真实 goods_id(style 即 GetMappingTypeId 的 type)。
            var displayRewards = new List<(int goodsId, long count)>();
            List<(int style, int typeId, long count, long goodsId)> rewardList = r.ReadArray(ReadRewardItem);
            foreach ((int style, int typeId, long count, long goodsId) item in rewardList)
            {
                rewards.Add((item.typeId, item.count));
                (int mappedId, int _) = GoodsModel.GetMappingTypeId(item.style, item.typeId);
                displayRewards.Add((mappedId, item.count));
            }

            List<(int rewardType, List<(int style1, int typeId1, long count1, long goodsId1)> list)> otherReward =
                r.ReadArray(ReadOtherReward);
            foreach ((int rewardType, List<(int style1, int typeId1, long count1, long goodsId1)> list) group in otherReward)
            {
                if (group.list == null) continue;
                foreach ((int style1, int typeId1, long count1, long goodsId1) item in group.list)
                {
                    rewards.Add((item.typeId1, item.count1));
                    (int mappedId, int _) = GoodsModel.GetMappingTypeId(item.style1, item.typeId1);
                    displayRewards.Add((mappedId, item.count1));
                }
            }

            r.ReadArray(ReadExData);   // ex_data(附加键值,本轮只按序读完,不用于展示)
            int count = r.ReadU8();

            DungeonModel.Instance.ApplySettle(result, rewards);

            // 结算界面(对标老端 OPEN_DENGEON_RESULT_VIEW → DungeonVictoryView/DungeonFailureView;
            // 加载失败时 DungeonResultView 内部自回退 Toast,不落静默)。
            DungeonResultView.Instance.Show(result == 1, grade, displayRewards);

            // 连锁:结算后补发 61121 刷新资源本次数(对标老端 61003 handler 尾部 RequestDungeonNum(dun_type),
            // BaseDungeonController.ts:1115;非资源类型服务端静默 skip,老端也是无条件发)。
            int dunType = DungeonConfigs.GetType(dunId);
            if (dunType != 0) RequestDungeonNum(dunType);

            GameLog.Info("Dungeon", "61003 settle dun_id={0} result={1} subtype={2} grade={3} scene={4} rewards={5} other={6} count={7}",
                dunId, result, resultSubtype, grade, sceneId, rewardList.Count, otherReward.Count, count);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_UPDATE);
        }

        /// <summary>61020 副本状态回包:dun_type:c + dun_list[u16×{dun_id:i, daily_count:h, weekly_count:h,
        /// permanent_count:h, reset_count:h, vip_count:h, add_count:h, is_sweep:c, rec_data[u16×{key:h,val:i}]}]。</summary>
        private void On61020(NetReader r)
        {
            int dunType = r.ReadU8();
            List<DungeonModel.DunState> list = r.ReadArray(ReadDunState);
            DungeonModel.Instance.Apply61020(dunType, list);
            // 连锁:补发 61121(对标老端 61020 handler 尾部 RequestDungeonNum(dun_type),
            // BaseDungeonController.ts:1243;非资源类型服务端静默 skip)。老端还有 SetDunBaseInfo
            // (config_dungeon+config_dungeon_ui_content 联查补 name/recommend_power 等展示字段)——
            // 展示字段消费方(副本大厅)未移植,联查留 TODO,先只落协议原始字段。
            RequestDungeonNum(dunType);
            GameLog.Info("Dungeon", "61020 state dun_type={0} count={1}", dunType, list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_UPDATE);
        }

        // =====================================================================================
        // 轮9 新增接收侧
        // =====================================================================================

        /// <summary>61004 副本信息:start_time:i, start_time_ms:l, end_time:i, level:h, level_end_time:i,
        /// owner_id:l, wave_num:i(对标老端 SetDungeonSceneMsg → Fire(UPDATE_DUNGEON_INFO))。
        /// 老端"刚进副本场景"分支(隐藏主 UI 10 号/关活动状态/STARTAUTOFIGHT)依赖场景前后对比与主 UI 分区句柄,
        /// 未移植,TODO 待副本战斗场景 UI(DungeonFightSceneView)接线时补。</summary>
        private void On61004(NetReader r)
        {
            var vo = new DungeonModel.SceneInfoVo
            {
                StartTime = (int)r.ReadU32(),
                StartTimeMs = (long)r.ReadU64(),
                EndTime = (int)r.ReadU32(),
                Level = r.ReadU16(),
                LevelEndTime = (int)r.ReadU32(),
                OwnerId = (long)r.ReadU64(),
                WaveNum = (int)r.ReadU32(),
            };
            DungeonModel.Instance.SetSceneInfo(vo);
            GameLog.Info("Dungeon", "61004 info start={0} end={1} level={2} wave={3}", vo.StartTime, vo.EndTime, vo.Level, vo.WaveNum);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_INFO_UPDATE);
        }

        /// <summary>61005 波次/事件推送(S2C):dun_id:i, scene_id:i, type:h, time:i, wave_num:i。
        /// 老端据此 RefreshMonster(刷怪表现)+寻路——Unity 刷怪由场景协议(12007/12012)下发实体承担,
        /// 副本内自动寻路未移植(TODO),本端只落波次数据。</summary>
        private void On61005(NetReader r)
        {
            int dunId = (int)r.ReadU32();
            int sceneId = (int)r.ReadU32();
            int type = r.ReadU16();
            int time = (int)r.ReadU32();
            int waveNum = (int)r.ReadU32();
            DungeonModel.Instance.SetWaveInfo(type, waveNum);
            GameLog.Info("Dungeon", "61005 wave push dun_id={0} scene={1} type={2} time={3} wave={4}", dunId, sceneId, type, time, waveNum);
        }

        /// <summary>61007 坐标事件回执(原样回显 x:h,y:h):驱动 role_pos_event_list 状态机
        /// (命中范围内置 3 完成,曾触发中(2)未命中回退 1——对标老端 SuccessTriggerRolePos)。</summary>
        private void On61007(NetReader r)
        {
            int x = r.ReadU16();
            int y = r.ReadU16();
            DungeonModel.Instance.SuccessTriggerRolePos(x, y);
            GameLog.Info("Dungeon", "61007 坐标事件回执 pos=({0},{1})", x, y);
        }

        /// <summary>61009 剧情触发推送:story_id:i, sub_sotry_id:i → 事件(对标老端 STORY_PLAY_TRIGGER;
        /// 剧情播放系统未移植,消费方后续 Story 通道接)。</summary>
        private void On61009(NetReader r)
        {
            int storyId = (int)r.ReadU32();
            int subStoryId = (int)r.ReadU32();
            GameLog.Info("Dungeon", "61009 story trigger story={0} sub={1}", storyId, subStoryId);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_STORY_TRIGGER, storyId, subStoryId);
        }

        /// <summary>61011 助战剩余次数:dun_id:i, left_help_count:c(对标老端 SetDungeonHelpData)。</summary>
        private void On61011(NetReader r)
        {
            int dunId = (int)r.ReadU32();
            int leftHelpCount = r.ReadU8();
            DungeonModel.Instance.SetHelpCount(dunId, leftHelpCount);
            GameLog.Info("Dungeon", "61011 help count dun_id={0} left={1}", dunId, leftHelpCount);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_HELP_COUNT, dunId);
        }

        /// <summary>61018 退出倒计时:type:c, end_time:i——仅 type==1 才落值发事件
        /// (type==0 表示该副本无倒计时配置,老端不处理,原样保留)。</summary>
        private void On61018(NetReader r)
        {
            int type = r.ReadU8();
            int endTime = (int)r.ReadU32();
            if (type != 1)
            {
                GameLog.Info("Dungeon", "61018 exit time type={0}(无倒计时配置,忽略)", type);
                return;
            }
            DungeonModel.Instance.SetExitEndTime(endTime);
            GameLog.Info("Dungeon", "61018 exit time end={0}", endTime);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_END_TIME, endTime);
        }

        /// <summary>61019 坐标触发情况表:xy_list[u16×{x:i,y:i}](⚠32 位,与 61007 的 16 位不同)——
        /// 服务端已记录的触发点逐一对账置 trigger_state=3(对标老端 ResetPosEventList)。</summary>
        private void On61019(NetReader r)
        {
            List<(int x, int y)> list = r.ReadArray(rr => ((int)rr.ReadU32(), (int)rr.ReadU32()));
            DungeonModel.Instance.ResetPosEventList(list);
            GameLog.Info("Dungeon", "61019 触发情况表 count={0}", list.Count);
        }

        /// <summary>61021 购买次数:error_code:i, dun_id:i, buy_count:h。成功按 dun_type 分支
        /// (NEW_*/Material_*/Unreal/Soul/AdvancedExp 全组共享 vip_count 广播,其余单条,对标老端
        /// BaseDungeonController.ts:1263-1290);失败姻缘本 6100043 走专文案(:1308-1309)。
        /// 老端"vip_count 达 total_buy_count 自动关购买弹窗"依赖 VIP 特权表(未移植),由 DungeonBuyTimeView
        /// 订阅事件自行刷新,TODO 特权额度接入后补自动关。</summary>
        private void On61021(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int dunId = (int)r.ReadU32();
            int buyCount = r.ReadU16();

            if (errorCode != 1)
            {
                if (dunId == DungeonModel.MARRIAGE_DUN_ID && errorCode == 6100043)
                    TipsManager.Toast("购买次数已达上限");   // 姻缘本专文案(对标老端)
                else
                    TipsManager.Toast("购买失败(" + errorCode + ")");   // 错误码表未移植,显码降级
                GameLog.Warn("Dungeon", "61021 buy fail dun_id={0} code={1}", dunId, errorCode);
                return;
            }

            TipsManager.Toast("购买成功");
            int dunType = DungeonConfigs.GetType(dunId);
            if (DungeonModel.Instance.DunStatesByType.TryGetValue(dunType, out List<DungeonModel.DunState> list) && list != null)
            {
                if (DungeonModel.IsSharedVipCountType(dunType))
                {
                    foreach (DungeonModel.DunState vo in list) vo.VipCount = buyCount;   // 全组共享一个 vip_count
                }
                else
                {
                    foreach (DungeonModel.DunState vo in list)
                    {
                        if (vo.DunId == dunId) { vo.VipCount = buyCount; break; }
                    }
                }
            }
            GameLog.Info("Dungeon", "61021 buy ok dun_id={0} type={1} buy_count={2}", dunId, dunType, buyCount);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_BUY_SUCCESS, dunId, dunType);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_UPDATE);
        }

        /// <summary>61022 扫荡:error_code:i, dun_id:i, grade:c, left_count:h, auto_num:h,
        /// sweep_list[u16×{reward_list[u16×{style:c,typeId:i,count:i,goods_id:l}],
        /// other_reward[u16×{reward_type:c,other_reward_list[...同款 count:i...]}]}]
        /// (⚠count 是 32 位,与 61003 的 64 位不同——双端交叉核对 pt_610.erl:506-529+ClientProtocol)。
        /// 成功:拼装奖励开结算(老端新资源本走 DungeonMaterialNewResultView,未移植→统一走既有
        /// DungeonResultView 通道,TODO);本地乐观计数:新资源组全条目 +auto_num,其余命中条目 +1
        /// (老端 default 分支有个 return 早退 bug 导致 +1 永不执行——r9 侦察裁决按语义实现,不复刻)。
        /// 扫荡后的次数同步服务端会自动推 61121(pp_dungeon.erl:253),无需客户端补发。</summary>
        private void On61022(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int dunId = (int)r.ReadU32();
            int grade = r.ReadU8();
            int leftCount = r.ReadU16();
            int autoNum = r.ReadU16();
            List<(int typeId, long num)> rewards;
            List<(int goodsId, long count)> displayRewards;
            ReadSweepRewardList(r, out rewards, out displayRewards);

            if (errorCode != 1)
            {
                TipsManager.Toast("扫荡失败(" + errorCode + ")");   // err610_sweep_* 系错误码,显码降级
                GameLog.Warn("Dungeon", "61022 sweep fail dun_id={0} code={1}", dunId, errorCode);
                return;
            }

            int dunType = DungeonConfigs.GetType(dunId);
            if (DungeonModel.Instance.DunStatesByType.TryGetValue(dunType, out List<DungeonModel.DunState> list) && list != null)
            {
                if (DungeonModel.IsSweepGroupCountType(dunType))
                {
                    foreach (DungeonModel.DunState vo in list) vo.DailyCount += autoNum;
                }
                else
                {
                    foreach (DungeonModel.DunState vo in list)
                    {
                        if (vo.DunId == dunId) { vo.DailyCount += 1; break; }
                    }
                }
            }

            // 奖励展示走既有结算通道(result_type=Sweeping 的分型布局未移植,victory 面板兜底展示)。
            DungeonResultView.Instance.Show(true, grade, displayRewards);
            GameLog.Info("Dungeon", "61022 sweep ok dun_id={0} type={1} grade={2} left={3} auto={4} rewards={5}",
                dunId, dunType, grade, leftCount, autoNum, displayRewards.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_UPDATE);
        }

        /// <summary>61023 时间评分状态:cur_score:i, next_score:i, change_time:i(对标老端 NOW_TIME_SCORE_STATE)。</summary>
        private void On61023(NetReader r)
        {
            int curScore = (int)r.ReadU32();
            int nextScore = (int)r.ReadU32();
            int changeTime = (int)r.ReadU32();
            DungeonModel.Instance.SetScoreState(curScore, nextScore, changeTime);
            GameLog.Info("Dungeon", "61023 score cur={0} next={1} change={2}", curScore, nextScore, changeTime);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_SCORE_STATE);
        }

        /// <summary>61025 鼓舞:error_code:i, coin_count:c, gold_count:c。成功→伤害加成 toast
        /// (帮派经验本 ×5%/其余 ×10%,对标老端;老端逐档连弹 total 条,本端只弹最终加成一条,简化)。</summary>
        private void On61025(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int coinCount = r.ReadU8();
            int goldCount = r.ReadU8();
            if (errorCode != 1)
            {
                TipsManager.Toast("鼓舞失败(" + errorCode + ")");
                GameLog.Warn("Dungeon", "61025 inspirit fail code={0}", errorCode);
                return;
            }
            DungeonModel model = DungeonModel.Instance;
            model.SetInspiritInfo(coinCount, goldCount);
            int bonus = model.GetInspiritBonusPercent(RoleModel.Instance.DunId);
            TipsManager.Toast("鼓舞成功,当前伤害加成为 " + bonus + "%");
            GameLog.Info("Dungeon", "61025 inspirit ok coin={0} gold={1} bonus={2}%", coinCount, goldCount, bonus);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_INSPIRIT_UPDATE, true);
        }

        /// <summary>61026 鼓舞状态:coin_count:c, gold_count:c(被动查询,不弹 toast——区别于 61025 成功那条)。</summary>
        private void On61026(NetReader r)
        {
            int coinCount = r.ReadU8();
            int goldCount = r.ReadU8();
            DungeonModel.Instance.SetInspiritInfo(coinCount, goldCount);
            GameLog.Info("Dungeon", "61026 inspirit state coin={0} gold={1}", coinCount, goldCount);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_INSPIRIT_UPDATE, false);
        }

        /// <summary>61030 下一波怪物生成时间:wave_num:i, time:i。</summary>
        private void On61030(NetReader r)
        {
            int waveNum = (int)r.ReadU32();
            int time = (int)r.ReadU32();
            DungeonModel.Instance.SetNextWaveTime(waveNum, time);
            GameLog.Info("Dungeon", "61030 next wave num={0} time={1}", waveNum, time);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_NEXT_WAVE, waveNum, time);
        }

        /// <summary>61044 经验副本面板主动推送:kill_num:h,exp:l。</summary>
        private void On61044(NetReader r)
        {
            DungeonModel.Instance.ApplyExpDungeonInfo(r.ReadU16(), unchecked((ulong)r.ReadU64()));
        }

        /// <summary>61045 副本冷却时间:dun_id:i,next_time:i；每个 id 独立覆盖。</summary>
        private void On61045(NetReader r)
        {
            uint dunId = r.ReadU32();
            uint nextTime = r.ReadU32();
            DungeonModel.Instance.ApplyCooldown(dunId, nextTime);
            GameLog.Info("Dungeon", "61045 cooldown dun_id={0} next_time={1}", dunId, nextTime);
        }

        /// <summary>61120 资源副本一键操作:code:i, oper_type:c, sweep_list(与 61022 同款 reward 形状,32 位 count)。
        /// 成功→老端开 DungeonMaterialNewResultView(未移植)→统一走既有 DungeonResultView 通道(TODO);
        /// 次数同步由服务端一键收尾时自动推 61121(lib_dungeon_resource.erl:290),无需客户端补发。
        /// 失败常见 err610_no_active_weekly_card(需周卡),错误码表未移植显码降级。</summary>
        private void On61120(NetReader r)
        {
            int code = (int)r.ReadU32();
            int operType = r.ReadU8();
            List<(int typeId, long num)> rewards;
            List<(int goodsId, long count)> displayRewards;
            ReadSweepRewardList(r, out rewards, out displayRewards);

            if (code != 1)
            {
                TipsManager.Toast((operType == 1 ? "一键挑战" : "一键扫荡") + "失败(" + code + ")");
                GameLog.Warn("Dungeon", "61120 onekey fail oper={0} code={1}", operType, code);
                return;
            }
            DungeonResultView.Instance.Show(true, 0, displayRewards);
            GameLog.Info("Dungeon", "61120 onekey ok oper={0} rewards={1}", operType, displayRewards.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_UPDATE);
        }

        /// <summary>61121 资源副本次数:count_list[u16×{dun_type:c, sweep_count:h, challenge_count:h}]
        /// (对标老端 SaveDungeonNumData → dungeon_num_data_)。</summary>
        private void On61121(NetReader r)
        {
            List<(int dunType, int sweep, int challenge)> list =
                r.ReadArray(rr => ((int)rr.ReadU8(), (int)rr.ReadU16(), (int)rr.ReadU16()));
            foreach ((int dunType, int sweep, int challenge) item in list)
                DungeonModel.Instance.SetResourceCount(item.dunType, item.sweep, item.challenge);
            GameLog.Info("Dungeon", "61121 resource count entries={0}", list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_RESOURCE_COUNT, list.Count == 1 ? list[0].dunType : 0);
        }

        /// <summary>50801 周本信息:dun_list[u16×{week_dun_id:i, dun_score:h, single_succ:c, team_succ:c,
        /// help_times:h, boss_reward[u16×{boss_id:i, reward_st:c}]}] → PolarModel(独立数据线)。</summary>
        private void On50801(NetReader r)
        {
            List<PolarModel.WeekInfoVo> list = r.ReadArray(ReadPolarWeekInfo);
            PolarModel.Instance.SetWeekInfos(list);
            GameLog.Info("Dungeon", "50801 polar info count={0}", list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_POLAR_DATA);
        }

        private static PolarModel.WeekInfoVo ReadPolarWeekInfo(NetReader r)
        {
            var vo = new PolarModel.WeekInfoVo
            {
                WeekDunId = (int)r.ReadU32(),
                DunScore = r.ReadU16(),
                SingleSucc = r.ReadU8(),
                TeamSucc = r.ReadU8(),
                HelpTimes = r.ReadU16(),
            };
            vo.BossReward = r.ReadArray(rr => new PolarModel.BossRewardVo
            {
                BossId = (int)rr.ReadU32(),
                RewardSt = rr.ReadU8(),
            });
            return vo;
        }

        /// <summary>50802 周本榜单:team_dun_id:i, self_rank:c, self_pass_time:h,
        /// rank_list[u16×{pass_time:h, time:i, rank:c, role_list[u16×{role_id:l, role_name:s,
        /// server_id:h, server_num:h}]}] → PolarModel(role_list 支持同排名多个组队成员)。</summary>
        private void On50802(NetReader r)
        {
            var vo = new PolarModel.RankVo
            {
                TeamDunId = (int)r.ReadU32(),
                SelfRank = r.ReadU8(),
                SelfPassTime = r.ReadU16(),
            };
            vo.Entries = r.ReadArray(ReadPolarRankEntry);
            PolarModel.Instance.SetRank(vo);
            GameLog.Info("Dungeon", "50802 polar rank dun={0} selfRank={1} entries={2}", vo.TeamDunId, vo.SelfRank, vo.Entries.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_POLAR_RANK_DATA, vo.TeamDunId);
        }

        private static PolarModel.RankEntryVo ReadPolarRankEntry(NetReader r)
        {
            var e = new PolarModel.RankEntryVo
            {
                PassTime = r.ReadU16(),
                Time = (int)r.ReadU32(),
                Rank = r.ReadU8(),
            };
            e.Roles = r.ReadArray(rr => new PolarModel.RankRoleVo
            {
                RoleId = (long)rr.ReadU64(),
                RoleName = rr.ReadString(),
                ServerId = rr.ReadU16(),
                ServerNum = rr.ReadU16(),
            });
            return e;
        }

        /// <summary>61046 邀请发送者原始消息；不解释为空串/成功，也不等待该包作为 ACK。</summary>
        private void On61046(NetReader r)
        {
            DungeonModel.Instance.ApplyInviteResponse(r.ReadString());
        }

        /// <summary>61047 回应邀请进入副本(对标老端 BaseDungeonController.ts:1593-1601 内联 handler:
        /// code==1 空分支/否则 ErrorCodeShow(code),无其它副作用)。answer 字段老端未读,本端只消费对齐游标。
        /// 错误码表未移植,显码降级。</summary>
        private void On61047(NetReader r)
        {
            int code = (int)r.ReadU32();
            r.ReadU8();   // answer(老端未消费,仅对齐游标)
            if (code != 1)
            {
                TipsManager.Toast("操作失败(" + code + ")");
                GameLog.Warn("Dungeon", "61047 回应邀请进入副本失败 code={0}", code);
            }
        }

        /// <summary>61048 双方邀请状态完整原始快照；不解释 code，不触发 UI/事件或自动回应。</summary>
        private void On61048(NetReader r)
        {
            uint code = r.ReadU32();
            List<DungeonModel.InviteStateEntry> list = r.ReadArray(rr => new DungeonModel.InviteStateEntry
            {
                Type = rr.ReadU8(),
                RoleId = unchecked((ulong)rr.ReadU64()),
                Figure = FigureProto.Read(rr),
            });
            uint dunId = r.ReadU32();
            DungeonModel.Instance.ApplyInviteState(code, list, dunId);
        }

        /// <summary>61050 神纹副本个人/最佳记录完整原始快照；不派事件或做展示层变换。</summary>
        private void On61050(NetReader r)
        {
            uint dunId = r.ReadU32();
            byte wave = r.ReadU8();
            uint myTime = r.ReadU32();
            uint bestTime = r.ReadU32();
            List<DungeonModel.DragonBestRecordRole> roles = r.ReadArray(rr => new DungeonModel.DragonBestRecordRole
            {
                RoleId = unchecked((ulong)rr.ReadU64()),
                Name = rr.ReadString(),
                Power = rr.ReadU32(),
                ServerNum = rr.ReadU32(),
                ServerId = rr.ReadU32(),
            });
            DungeonModel.Instance.ApplyDragonBestRecord(dunId, wave, myTime, bestTime, roles);
        }

        /// <summary>61051 神纹副本阶段奖励领取情况完整原始快照；不派生领取状态或红点。</summary>
        private void On61051(NetReader r)
        {
            uint dunId = r.ReadU32();
            byte historyWave = r.ReadU8();
            List<byte> claimedWaves = r.ReadArray(rr => rr.ReadU8());
            DungeonModel.Instance.ApplyDragonStageReward(dunId, historyWave, claimedWaves);
        }

        private void On61053(NetReader r)
        {
            DungeonModel.Instance.ApplyDragonQuickInfo(r.ReadU16(), r.ReadU16(), r.ReadU32());
        }

        private void On61055(NetReader r)
        {
            List<DungeonModel.DragonSkillInfoEntry> skills = r.ReadArray(rr =>
                new DungeonModel.DragonSkillInfoEntry
                {
                    SkillId = rr.ReadU32(),
                    Num = rr.ReadU16(),
                });
            DungeonModel.Instance.ApplyDragonSkillInfo(skills);
        }

        /// <summary>61058 神纹跳关奖励主动通知；仅保留服务端实际下发的有序原始快照。</summary>
        private void On61058(NetReader r)
        {
            uint wave = r.ReadU32();
            List<DungeonModel.DragonJumpRewardEntry> rewards = r.ReadArray(rr =>
                new DungeonModel.DragonJumpRewardEntry
                {
                    Type = rr.ReadU8(),
                    TypeId = rr.ReadU32(),
                    Num = rr.ReadU32(),
                });
            DungeonModel.Instance.ApplyDragonJumpReward(wave, rewards);
        }

        /// <summary>61059 高级经验副本波数面板主动推送；只保存五字段 wire 原值。</summary>
        private void On61059(NetReader r)
        {
            DungeonModel.Instance.ApplyAdvancedExpInfo(
                r.ReadU32(),
                r.ReadU32(),
                r.ReadU32(),
                r.ReadU32(),
                unchecked((ulong)r.ReadU64()));
        }

        /// <summary>61061 高级经验副本跳关进入通知；不合并或覆盖 61059 面板快照。</summary>
        private void On61061(NetReader r)
        {
            DungeonModel.Instance.ApplyAdvancedExpJumpInfo(
                r.ReadU32(),
                r.ReadU32(),
                unchecked((ulong)r.ReadU64()));
        }

        /// <summary>61062 副本开关设置完整原始快照；不按 type 归一化或派生鼓舞状态。</summary>
        private void On61062(NetReader r)
        {
            uint dunId = r.ReadU32();
            List<DungeonModel.DungeonSettingInfoEntry> settings = r.ReadArray(rr =>
                new DungeonModel.DungeonSettingInfoEntry
                {
                    Type = rr.ReadU8(),
                    SelectType = rr.ReadU8(),
                    IsOpen = rr.ReadU8(),
                    Count = rr.ReadU8(),
                });
            DungeonModel.Instance.ApplyDungeonSettingInfo(dunId, settings);
        }

        /// <summary>61063 设置结果；成功仅重查 61062，失败仅显码，回显字段只消费不落模型。</summary>
        private void On61063(NetReader r)
        {
            uint errorCode = r.ReadU32();
            uint dunId = r.ReadU32();
            r.ReadU8(); // type
            r.ReadU8(); // select_type
            r.ReadU8(); // is_open
            r.ReadU8(); // count
            if (errorCode == 1)
                RequestDungeonSettingInfo(dunId);
            else
                TipsManager.Toast("操作失败(" + errorCode + ")");
        }

        /// <summary>61065 入场自动鼓舞权威计数；与 61026 同为被动状态，不补请求或派生提示。</summary>
        private void On61065(NetReader r)
        {
            int coinCount = r.ReadU8();
            int goldCount = r.ReadU8();
            DungeonModel.Instance.SetInspiritInfo(coinCount, goldCount);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_INSPIRIT_UPDATE, false);
        }

        /// <summary>61088 周本特殊信息原始包；push_type=2 可能仅含局部 term，按包整体覆盖且不解析。</summary>
        private void On61088(NetReader r)
        {
            uint dunId = r.ReadU32();
            byte dunType = r.ReadU8();
            byte pushType = r.ReadU8();
            string content = r.ReadString();
            PolarModel.Instance.ApplySpecialInfo(dunId, dunType, pushType, content);
        }

        /// <summary>61092 异兽入侵 领取阶段奖励(对标老端 BaseDungeonController.ts:1848-1857 内联 handler:
        /// error_code==1 分支 setMonsterInvasionReward 调用**已被老端注释**——纯死代码,运行时无副作用,
        /// 否则 ErrorCodeShow(error_code)。本端如实镜像"成功也不做事",不臆造奖励消费,仅读完 reward_list
        /// 保证游标对齐)。错误码表未移植,显码降级。</summary>
        private void On61092(NetReader r)
        {
            r.ReadU32();     // dun_id(老端未读)
            int code = (int)r.ReadU32();
            r.ReadU8();      // reward_status(老端未读)
            r.ReadArray(rr => { rr.ReadU8(); rr.ReadU32(); rr.ReadU32(); return 0; });   // reward_list(老端成功分支已注释,仅对齐游标)
            if (code != 1)
            {
                TipsManager.Toast("操作失败(" + code + ")");
                GameLog.Warn("Dungeon", "61092 异兽入侵领取阶段奖励失败 code={0}", code);
            }
        }

        /// <summary>61022/61120 共用的 sweep_list 读取+奖励拼平(r9 侦察建议的公共工具,免逐协议复制粘贴):
        /// sweep_list[u16×{reward_list[u16×{style:c,typeId:i,count:i,goods_id:l}],
        /// other_reward[u16×{reward_type:c,other_reward_list[u16×{style1:c,typeId1:i,count1:i,goods_id1:l}]}]}]。
        /// ⚠count 是 32 位(pt_610/pt_611 item Count:32),与 61003 的 64 位不同。displayRewards 经
        /// GoodsModel.GetMappingTypeId 还原真实 goods_id 供结算面板;按 max_overlap 拆堆叠的展示细则未移植(TODO)。</summary>
        private static void ReadSweepRewardList(NetReader r,
            out List<(int typeId, long num)> rewards, out List<(int goodsId, long count)> displayRewards)
        {
            var rw = new List<(int typeId, long num)>();
            var dr = new List<(int goodsId, long count)>();
            int sweepCount = r.ReadU16();
            for (int i = 0; i < sweepCount; i++)
            {
                int rewardCount = r.ReadU16();
                for (int j = 0; j < rewardCount; j++)
                {
                    int style = r.ReadU8();
                    int typeId = (int)r.ReadU32();
                    long count = r.ReadU32();      // ⚠32 位(勿抄 61003 的 u64)
                    r.ReadU64();                   // goods_id(展示走映射后 id,不用实例 id)
                    rw.Add((typeId, count));
                    (int mappedId, int _) = GoodsModel.GetMappingTypeId(style, typeId);
                    dr.Add((mappedId, count));
                }
                int otherCount = r.ReadU16();
                for (int j = 0; j < otherCount; j++)
                {
                    r.ReadU8();                    // reward_type(结算分栏展示未移植,读掉对齐)
                    int itemCount = r.ReadU16();
                    for (int k = 0; k < itemCount; k++)
                    {
                        int style1 = r.ReadU8();
                        int typeId1 = (int)r.ReadU32();
                        long count1 = r.ReadU32();
                        r.ReadU64();               // goods_id1
                        rw.Add((typeId1, count1));
                        (int mappedId, int _) = GoodsModel.GetMappingTypeId(style1, typeId1);
                        dr.Add((mappedId, count1));
                    }
                }
            }
            rewards = rw;
            displayRewards = dr;
        }

        private static (int style, int typeId, long count, long goodsId) ReadRewardItem(NetReader r)
        {
            return (r.ReadU8(), (int)r.ReadU32(), (long)r.ReadU64(), (long)r.ReadU64());
        }

        private static (int rewardType, List<(int style1, int typeId1, long count1, long goodsId1)> list) ReadOtherReward(NetReader r)
        {
            int rewardType = r.ReadU8();
            List<(int style1, int typeId1, long count1, long goodsId1)> list = r.ReadArray(ReadOtherRewardItem);
            return (rewardType, list);
        }

        private static (int style1, int typeId1, long count1, long goodsId1) ReadOtherRewardItem(NetReader r)
        {
            return (r.ReadU8(), (int)r.ReadU32(), (long)r.ReadU64(), (long)r.ReadU64());
        }

        private static (int key, int val) ReadExData(NetReader r)
        {
            return (r.ReadU16(), r.ReadI32());
        }

        private static DungeonModel.DunState ReadDunState(NetReader r)
        {
            var s = new DungeonModel.DunState
            {
                DunId = (int)r.ReadU32(),
                DailyCount = r.ReadU16(),
                WeeklyCount = r.ReadU16(),
                PermanentCount = r.ReadU16(),
                ResetCount = r.ReadU16(),
                VipCount = r.ReadU16(),
                AddCount = r.ReadU16(),
            };
            s.IsSweep = r.ReadU8() != 0;
            r.ReadArray(ReadExData);   // rec_data(附加键值,本轮只按序读完)
            return s;
        }
    }
}
