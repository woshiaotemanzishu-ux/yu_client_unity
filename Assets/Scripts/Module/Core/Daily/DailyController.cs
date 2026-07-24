using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 日常中心网络层(自动循环 轮10;对标老端 commonController/DailyController.ts)。
    /// 注册 15700(跨系统共享错误码壳)+ 15701/03/05/06/09/10/11/12/14/15/16/17/18/19/20/21 + 41900/03/04 +
    /// 61801(15722 fire-and-forget 无 recv)。⚠轮10交叉验收 blocker 订正:15700 此前"不重复注册"的裁决前提
    /// 不成立——全仓 grep 无任何人注册它,而 15705/15710/15715/15716/15719 等失败分支服务端只走 15700 回包
    /// (老端就是 DailyController 唯一注册方),故本端在此补注册。跳过号见 Proto.cs 对应常量注释与规格 §0。
    /// </summary>
    public sealed class DailyController : BaseController
    {
        public static readonly DailyController Instance = new DailyController();
        private DailyController() { }

        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.DAILY_ERROR, On15700);
            RegisterProtocal(Proto.DAILY_ACTIVITY_LIST, On15701);
            RegisterProtocal(Proto.DAILY_LIVENESS_REWARD, On15703);
            RegisterProtocal(Proto.DAILY_LIVENESS_REWARD_GET, On15705);
            RegisterProtocal(Proto.DAILY_ACTIVITY_STATE_PUSH, On15706);
            RegisterProtocal(Proto.DAILY_LIVENESS_INFO, On15709);
            RegisterProtocal(Proto.DAILY_LIVENESS_LEVEL_UP, On15710);
            RegisterProtocal(Proto.DAILY_LIVENESS_CHANGE_FIGURE, On15711);
            RegisterProtocal(Proto.DAILY_LIVENESS_FIGURE_PUSH, On15712);
            RegisterProtocal(Proto.DAILY_ONHOOK_TIME_PUSH, On15714);
            RegisterProtocal(Proto.DAILY_LIVENESS_FIND_INFO, On15715);
            RegisterProtocal(Proto.DAILY_LIVENESS_FIND, On15716);
            RegisterProtocal(Proto.DAILY_TASK_LIVENESS_CLAIM, On15717);
            RegisterProtocal(Proto.DAILY_SIGNUP_LIST, On15718);
            RegisterProtocal(Proto.DAILY_SIGNUP, On15719);
            RegisterProtocal(Proto.DAILY_SIGNUP_REWARD, On15720);
            RegisterProtocal(Proto.DAILY_ACT_REMIND, On15721);
            // 15722(DAILY_ACT_REMIND_SET):服务端无 write 子句,fire-and-forget,不注册。
            RegisterProtocal(Proto.DAILY_RES_FIND_INFO, On41900);
            RegisterProtocal(Proto.DAILY_RES_FIND, On41903);
            RegisterProtocal(Proto.DAILY_RES_FIND_ONEKEY, On41904);
            RegisterProtocal(Proto.DAILY_STRONGER_LIST, On61801);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            _lastLevel = -1;
            DailyModel.Instance.Clear();
            ActivityIconManager.Instance.SetIconRedDot("157", false);
            base.Dispose();
        }

        private static void EmitRedDot()
        {
            bool on = DailyModel.Instance.ComputeRedDot();
            EventDispatcher.Emit(GlobalEvent.EVT_DAILY_RED_DOT, on);
            ActivityIconManager.Instance.SetIconRedDot("157", on);
        }

        // =====================================================================================
        // 触发时机(对标老端 GAME_START 一次性批量请求 + CHANGE_LEVEL 补查 15721)
        // =====================================================================================

        private async void OnGameStart()
        {
            DailyModel.Instance.Clear();
            // ⚠轮10交叉验收 blocker 订正:此前 fire-and-forget 后立刻发包,回包若先于配置到达会被
            // SetDailyData/SetResTable 因 acCfg==null 误判摘空(同「Config load timing gotcha」记忆条目);
            // 老端 config_ac 走 PRELOAD_SERVER_CONFIG 同步预载无此窗口,这里改为先 await 配置就绪再发包。
            await DailyConfigs.EnsureLoaded();
            SendFmt(Proto.DAILY_ACTIVITY_LIST, "c", DailyModel.ACT_UNLIMIT);
            SendFmt(Proto.DAILY_ACTIVITY_LIST, "c", DailyModel.ACT_LIMIT);
            SendFmt(Proto.DAILY_LIVENESS_REWARD);
            SendFmt(Proto.DAILY_RES_FIND_INFO);
            SendFmt(Proto.DAILY_LIVENESS_INFO);
            SendFmt(Proto.DAILY_ACT_REMIND);
            if (RoleModel.Instance.Level >= DailyModel.LIVENESS_FIND_OPEN_LEVEL) SendFmt(Proto.DAILY_LIVENESS_FIND_INFO);
            SendFmt(Proto.DAILY_SIGNUP_LIST);
            _lastLevel = RoleModel.Instance.Level;
            GameLog.Info("Daily", "GAME_START 批量请求 15701×2/15703/41900/15709/15721/15718{0}",
                RoleModel.Instance.Level >= DailyModel.LIVENESS_FIND_OPEN_LEVEL ? "/15715" : "");
        }

        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            SendFmt(Proto.DAILY_ACT_REMIND); // 对标老端 CHANGE_LEVEL → Fire(request_proto,15721)
        }

        // =====================================================================================
        // 15700:跨协议共享错误码壳
        // =====================================================================================

        /// <summary>15700 通用错误码(对标老端 Handler15700→Util.ErrorCodeShow):15705/15710/15715/15716/
        /// 15717/15719 等失败分支的服务端 guard 全部经此号回包,而非各自协议号——Daily 是老端唯一注册方,
        /// 无跨系统双注册风险(GapMap 风险#5 结论)。错误码表(Util.ErrorCodeShow)未移植 → 显码降级。</summary>
        private void On15700(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            TipsManager.Toast("操作失败(" + errcode + ")");
            GameLog.Warn("Daily", "15700 通用错误码 code={0}", errcode);
        }

        // =====================================================================================
        // 每日任务/限时活动(15701/15703/15705/15706/15717)
        // =====================================================================================

        /// <summary>开页再拉一次(对标 DailyTaskView/DailyLimitActivityView open 各自再发)。</summary>
        public void RequestActivityList(int actType) => SendFmt(Proto.DAILY_ACTIVITY_LIST, "c", actType);

        private void On15701(NetReader r)
        {
            int actType = r.ReadU8();
            long onHookTime = (long)r.ReadU64();
            List<DailyModel.ActivityVo> list = r.ReadArray(ReadActivityVo);
            DailyModel.Instance.SetDailyData(actType, onHookTime, list);
            GameLog.Info("Daily", "15701 act_type={0} count={1} onhook={2}", actType, list.Count, onHookTime);
            EventDispatcher.Emit(actType == DailyModel.ACT_UNLIMIT ? GlobalEvent.EVT_DAILY_TASK_UPDATE : GlobalEvent.EVT_DAILY_LIMIT_UPDATE);
            EmitRedDot();
        }

        private static DailyModel.ActivityVo ReadActivityVo(NetReader r)
        {
            return new DailyModel.ActivityVo
            {
                Module = (int)r.ReadU32(),
                ModuleSub = (int)r.ReadU32(),
                AcSub = (int)r.ReadU32(),
                Num = (int)r.ReadU32(),
                MaxNum = (int)r.ReadU32(),
                Live = (int)r.ReadU32(),
                MaxLive = (int)r.ReadU32(),
                CanGetLive = (int)r.ReadU32(),
                State = r.ReadU8(),
            };
        }

        public void RequestLivenessReward() => SendFmt(Proto.DAILY_LIVENESS_REWARD);

        private void On15703(NetReader r)
        {
            int live = (int)r.ReadU32();
            int liveMax = (int)r.ReadU32();
            List<DailyModel.LivenessRewardVo> list = r.ReadArray(rr => new DailyModel.LivenessRewardVo { Id = (int)rr.ReadU32(), State = rr.ReadU8() });
            DailyModel.Instance.SetLivenessReward(live, liveMax, list);
            GameLog.Info("Daily", "15703 live={0}/{1} rewards={2}", live, liveMax, list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_DAILY_LIVENESS_REWARD_UPDATE);
            EmitRedDot();
        }

        /// <summary>领取活跃度宝箱(DailyBottomView 4 个宝箱格,index 取 state==1 的项才发)。</summary>
        public void ClaimLivenessBox(int id) => SendFmt(Proto.DAILY_LIVENESS_REWARD_GET, "i", id);

        private void On15705(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int id = (int)r.ReadU32();
            if (errcode == 1)
            {
                List<(int style, int typeId, long count)> rewards = DailyConfigs.GetBoxRewardListById(id);
                // 周卡翻倍假条目(WeekCardModel.is_activity==1 追加展示)未接线,TODO;奖励展示走既有降级通道(同 MailController 先例)。
                TipsManager.Toast(rewards.Count > 0 ? "领取奖励成功," + FormatRewardSummary(rewards) : "领取奖励成功");
                SendFmt(Proto.DAILY_LIVENESS_REWARD);
                GameLog.Info("Daily", "15705 claim box ok id={0} rewards={1}", id, rewards.Count);
            }
            else
            {
                TipsManager.Toast("领取失败(" + errcode + ")");
                GameLog.Warn("Daily", "15705 claim box fail id={0} code={1}", id, errcode);
            }
        }

        /// <summary>每日任务单条 item 领活跃度。发 "ih"(module, module_sub)。⚠r10_server 静态证据 read/handle
        /// 字段数疑似不匹配(大概率不可达),按规格 §0 与老端行为原样实现。</summary>
        public void ClaimTaskLiveness(int module, int moduleSub) => SendFmt(Proto.DAILY_TASK_LIVENESS_CLAIM, "ih", module, moduleSub);

        private void On15717(NetReader r)
        {
            int actId = (int)r.ReadU32();
            int actSub = r.ReadU16();
            int addLive = (int)r.ReadU32();
            TipsManager.Toast("领取成功");
            SendFmt(Proto.DAILY_ACTIVITY_LIST, "c", DailyModel.ACT_UNLIMIT);
            SendFmt(Proto.DAILY_LIVENESS_REWARD);
            GameLog.Info("Daily", "15717 领活跃度成功 act={0}@{1} add_live={2}(联动重拉15701+15703)", actId, actSub, addLive);
        }

        private void On15706(NetReader r)
        {
            int module = (int)r.ReadU32();
            int moduleSub = (int)r.ReadU32();
            int actType = r.ReadU8();
            int status = r.ReadU8();
            bool changed = DailyModel.Instance.UpdateActivityState(actType, module, moduleSub, status);
            GameLog.Info("Daily", "15706 push module={0}@{1} act_type={2} status={3} changed={4}", module, moduleSub, actType, status, changed);
            // 650@1 CSPVP 联动查询——老端该分支已注释(@@@),不抄。
            // DailyActTipView 若正开着顺手刷 15721——该弹窗现仅 Bind 无具体类(无壳),恒不成立,TODO。
            EventDispatcher.Emit(actType == DailyModel.ACT_UNLIMIT ? GlobalEvent.EVT_DAILY_TASK_UPDATE : GlobalEvent.EVT_DAILY_LIMIT_UPDATE);
        }

        // =====================================================================================
        // 活跃度形象(15709/15710/15711/15712)
        // =====================================================================================

        public void RequestLivenessImage() => SendFmt(Proto.DAILY_LIVENESS_INFO);
        public void UpgradeLiveness() => SendFmt(Proto.DAILY_LIVENESS_LEVEL_UP);

        private void On15709(NetReader r)
        {
            int lv = (int)r.ReadU32();
            int liveness = (int)r.ReadU32();
            int id = (int)r.ReadU32();
            int display = r.ReadU8();
            DailyModel.Instance.SetLivenessImage(lv, liveness, id, display);
            GameLog.Info("Daily", "15709 liveness image lv={0} liveness={1} id={2} display={3}", lv, liveness, id, display);
            EventDispatcher.Emit(GlobalEvent.EVT_DAILY_LIVENESS_IMAGE_UPDATE);
            EmitRedDot();
        }

        private void On15710(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int lv = (int)r.ReadU32();
            int liveness = (int)r.ReadU32();
            if (errcode == 1)
            {
                TipsManager.Toast("升级成功");
                int curFigureId = DailyModel.Instance.LivenessImgId;
                SendFmt(Proto.DAILY_LIVENESS_INFO);
                // 15711 不自动发:老端 UseNewImage 全仓零调用点(仅定义),运行时从不发 15711;
                // 服务端 handle 已注释,发送只会落 pp_activitycalen catch-all 回 {error}。
                // FindNewFigureId 与 On15711 防御性 recv 保留,待服务端复活该号再接。
                int newFigureId = DailyConfigs.FindNewFigureId(lv, curFigureId);
                if (newFigureId > 0)
                {
                    GameLog.Info("Daily", "15710 检出新形象 newFigureId={0}(15711 双端死路,不发)", newFigureId);
                }
                GameLog.Info("Daily", "15710 升级成功 lv={0} liveness={1}", lv, liveness);
            }
            else
            {
                TipsManager.Toast("升级失败(" + errcode + ")");
                GameLog.Warn("Daily", "15710 升级失败 code={0}", errcode);
            }
        }

        /// <summary>15711 换形象回包:⚠r10_server 实证服务端 handle 已注释,恒不可达;仅注册防御(同 CHAT_BANNED_NOTICE 先例)。</summary>
        private void On15711(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int id = (int)r.ReadU32();
            if (errcode == 1)
            {
                TipsManager.Toast("幻化成功");
                SendFmt(Proto.DAILY_LIVENESS_INFO);
            }
            else
            {
                GameLog.Warn("Daily", "15711 换形象失败 id={0} code={1}", id, errcode);
            }
        }

        private void On15712(NetReader r)
        {
            long roleId = (long)r.ReadU64();
            int figureId = (int)r.ReadU32();
            GameLog.Info("Daily", "15712 他人形象变更 role={0} figure={1}(场景角色同步消费方未接线,TODO)", roleId, figureId);
            EventDispatcher.Emit(GlobalEvent.EVT_DAILY_FIGURE_PUSH, roleId, figureId);
        }

        // =====================================================================================
        // 离线挂机(15714)/活跃度找回(15715/15716,功能已下线仅存数据)
        // =====================================================================================

        private void On15714(NetReader r)
        {
            long time = (long)r.ReadU64();
            DailyModel.Instance.SetOutlineTime(time);
            GameLog.Info("Daily", "15714 挂机时间={0}", time);
            EventDispatcher.Emit(GlobalEvent.EVT_DAILY_OUTLINE_TIME, time);
            EmitRedDot();
        }

        /// <summary>15715 活跃度找回信息:⚠老端 LivenessCanFind() 已硬编码 return false=功能下线;
        /// 协议接收保留但不建 UI(规格§0)。</summary>
        private void On15715(NetReader r)
        {
            List<DailyModel.HuoYueDuVo> list = r.ReadArray(rr => new DailyModel.HuoYueDuVo
            {
                ActId = (int)rr.ReadU32(),
                ActSub = rr.ReadU16(),
                Lefttimes = rr.ReadU16(),
                BackTimes = rr.ReadU16(),
            });
            DailyModel.Instance.SetHuoYueDuData(list);
            GameLog.Info("Daily", "15715 活跃度找回信息 count={0}(功能已下线,无 UI)", list.Count);
        }

        /// <summary>15716 活跃度找回:⚠老端 h5/src 全仓库无发送调用点(功能随 15715 一并下线),
        /// 仅按协议声明防御性注册 recv,不提供 UI/公开发送 API(无法确认真实字段宽度,规格§0)。</summary>
        private void On15716(NetReader r)
        {
            int actId = (int)r.ReadU32();
            int actSub = r.ReadU16();
            int lefttimes = r.ReadU16();
            TipsManager.Toast("找回成功");
            SendFmt(Proto.DAILY_LIVENESS_FIND_INFO);
            GameLog.Info("Daily", "15716 找回成功 act={0}@{1} left={2}", actId, actSub, lefttimes);
        }

        // =====================================================================================
        // 限时活动预约三件套(15718/15719/15720)
        // =====================================================================================

        public void RequestSignUpList() => SendFmt(Proto.DAILY_SIGNUP_LIST);

        private void On15718(NetReader r)
        {
            List<(int module, int moduleSub, int acSub, int status, int join)> list = r.ReadArray(rr =>
                ((int)rr.ReadU32(), (int)rr.ReadU32(), (int)rr.ReadU32(), (int)rr.ReadU8(), (int)rr.ReadU8()));
            DailyModel.Instance.SetResTable(list);
            GameLog.Info("Daily", "15718 报名情况 count={0} red={1}", list.Count, DailyModel.Instance.DailyResRed);
            EventDispatcher.Emit(GlobalEvent.EVT_DAILY_SIGNUP_UPDATE);
            EmitRedDot();
        }

        /// <summary>报名/预约。⚠老端夹带微信小游戏订阅检查(SettingModel.CheckSubsOpen 等),Unity 非微信渠道整体跳过。</summary>
        public void SignUp(int module, int moduleSub, int acSub) => SendFmt(Proto.DAILY_SIGNUP, "iii", module, moduleSub, acSub);

        private void On15719(NetReader r)
        {
            int code = (int)r.ReadU32();
            int module = (int)r.ReadU32();
            int moduleSub = (int)r.ReadU32();
            int acSub = (int)r.ReadU32();
            int status = r.ReadU8();
            int join = r.ReadU8();
            if (code == 1)
            {
                DailyModel.Instance.SetResSingle(module, moduleSub, acSub, status, join);
                EventDispatcher.Emit(GlobalEvent.EVT_DAILY_SIGNUP_SUCCESS, module, moduleSub, acSub);
                if (status != 2)
                {
                    TipsManager.Toast("预约成功");
                    DailyFlow.OpenSub("DailyReservationView"); // 现仅 Bind 无具体类(无壳),内部自动降级为日志
                }
                GameLog.Info("Daily", "15719 报名成功 module={0}@{1}@{2} status={3} join={4}", module, moduleSub, acSub, status, join);
            }
            else
            {
                TipsManager.Toast("报名失败(" + code + ")");
                GameLog.Warn("Daily", "15719 报名失败 code={0}", code);
            }
            EmitRedDot();
        }

        public void ClaimSignUpReward(int module, int moduleSub, int acSub) => SendFmt(Proto.DAILY_SIGNUP_REWARD, "iii", module, moduleSub, acSub);

        private void On15720(NetReader r)
        {
            int code = (int)r.ReadU32();
            int module = (int)r.ReadU32();
            int moduleSub = (int)r.ReadU32();
            int acSub = (int)r.ReadU32();
            if (code == 1)
            {
                // 奖励展示走既有通道降级为纯 toast(config_ac.sign_up_reward 明细预览未接线,同 MailController 先例)。
                TipsManager.Toast("领取成功");
                DailyModel.Instance.SetReservationState(module, moduleSub, acSub, 2);
                // ⚠轮10交叉验收 blocker 订正:不再在此单独扣红点——服务端领奖成功会同时广播一条
                // 15719(status=2)(lib_act_sign_up.erl:190-191),红点的那一次 -1 由 On15719→SetResSingle
                // 完成;老端 Handler15720 本身也不碰 dailyResRed,此前额外调用会造成每次领奖双扣。
                EventDispatcher.Emit(GlobalEvent.EVT_DAILY_SIGNUP_UPDATE);
                GameLog.Info("Daily", "15720 领取报名奖励成功 module={0}@{1}@{2}", module, moduleSub, acSub);
            }
            else
            {
                TipsManager.Toast("领取失败(" + code + ")");
                GameLog.Warn("Daily", "15720 领取报名奖励失败 code={0}", code);
            }
            EmitRedDot();
        }

        // =====================================================================================
        // 限时活动开启提醒(15721/15722)
        // =====================================================================================

        public void RequestActRemind() => SendFmt(Proto.DAILY_ACT_REMIND);

        /// <summary>设置"今日不再提醒"(15722,fire-and-forget,无 recv)。</summary>
        public void SetActRemind(bool open) => SendFmt(Proto.DAILY_ACT_REMIND_SET, "c", open ? 1 : 0);

        private void On15721(NetReader r)
        {
            int isRemind = r.ReadU8();
            List<DailyModel.ActRemindVo> list = r.ReadArray(rr => new DailyModel.ActRemindVo
            {
                Module = (int)rr.ReadU32(),
                ModuleSub = (int)rr.ReadU32(),
                AcSub = (int)rr.ReadU32(),
                State = rr.ReadU8(),
                Time = (int)rr.ReadU32(),
                SignState = rr.ReadU8(),
            });
            (bool hasNew, int newModule) = DailyModel.Instance.HasNewAct(list);
            // ⚠DailyActTipView(15721 弹窗簇)现仅 Bind 无具体类(r10_unity 结论)——"有壳接壳,无壳 toast 降级+TODO"。
            // ⚠轮10交叉验收 blocker 订正:老端 Handler15721(非"界面已打开"分支,Unity 无 ViewPopLevelModel
            // 等价物,恒按此分支处理)只在"弹窗"这一条路径调 SetDailyActData 存表;is_remind==0 分支与
            // "isRemind==1 但无新增/空表"分支老端都不存表(保留旧表供下次 HasNewAct 比对基准不被提前冲掉)。
            // 此前 Unity 三分支无条件存表,会导致"旧表非空且新增了活动"下次仍判不出新增。
            if (isRemind == 1 && list.Count > 0 && hasNew)
            {
                DailyModel.Instance.SetDailyActData(true, list);
                TipsManager.Toast("有新的限时活动开启");
                DailyFlow.OpenSub("DailyActTipView"); // 无壳,内部自动降级为日志
            }
            else if (isRemind == 0)
            {
                GameLog.Info("Daily", "15721 is_remind=0 → 活动预告分支(ActivityForeshowManager 未移植,TODO;不存表)");
            }
            GameLog.Info("Daily", "15721 活动提醒 is_remind={0} count={1} hasNew={2} newModule={3}", isRemind, list.Count, hasNew, newModule);
            EventDispatcher.Emit(GlobalEvent.EVT_DAILY_ACT_REMIND);
        }

        // =====================================================================================
        // 资源找回(41900/41903/41904)
        // =====================================================================================

        public void RequestResFindInfo() => SendFmt(Proto.DAILY_RES_FIND_INFO);

        private void On41900(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            List<DailyModel.ResFindVo> list = r.ReadArray(ReadResFindVo);
            if (errcode == 1)
            {
                DailyModel.Instance.SetResFindData(list);
                GameLog.Info("Daily", "41900 资源找回信息 count={0}", list.Count);
                EventDispatcher.Emit(GlobalEvent.EVT_DAILY_RES_FIND_UPDATE);
                EmitRedDot();
            }
            else
            {
                GameLog.Warn("Daily", "41900 资源找回信息失败 code={0}", errcode);
            }
        }

        private static DailyModel.ResFindVo ReadResFindVo(NetReader r)
        {
            return new DailyModel.ResFindVo
            {
                ActId = (int)r.ReadU32(),
                ActSub = r.ReadU16(),
                Lefttimes = r.ReadU16(),
                LefttimesVip = r.ReadU16(),
                RewardLv = (int)r.ReadU32(),
            };
        }

        /// <summary>单条找回。UI 简化:滑杆简化为"全额找回"一键(规格§UI 裁决),times/timesOthers 由调用方
        /// 按 DailyModel.ResFindVo 的 Lefttimes/LefttimesVip 全额传入。type:1=绑钻(付费) 2=金币/免费。
        /// ⚠轮10交叉验收 blocker:调用方默认必须传 2——老端 resFindCheck 默认 [false,true] → money_type
        /// 默认 2(DailyResFindView.ts:48/76-91),type=1 是付费路径且老端必带二次确认 Alert,本端 config_res_act
        /// 未导入无法判定金币/绑钻分支前,禁止默认走 1(会变成"一点即扣绑钻,无任何确认")。</summary>
        public void ResFind(int actId, int actSub, int type, int times, int timesOthers)
            => SendFmt(Proto.DAILY_RES_FIND, "ihchh", actId, actSub, type, times, timesOthers);

        private void On41903(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int type = r.ReadU8();
            int actId = (int)r.ReadU32();
            int actSub = r.ReadU16();
            int lefttimes = r.ReadU16();
            int lefttimesVip = r.ReadU16();
            int rewardLv = (int)r.ReadU32();
            if (errcode == 1)
            {
                TipsManager.Toast("找回成功");
                DailyModel.Instance.UpdateResFind(actId, actSub, rewardLv, lefttimes, lefttimesVip);
                EventDispatcher.Emit(GlobalEvent.EVT_DAILY_RES_FIND_UPDATE);
                GameLog.Info("Daily", "41903 找回成功 act={0}@{1} lv={2} left={3}/{4}", actId, actSub, rewardLv, lefttimes, lefttimesVip);
            }
            else if (errcode == 4190001)
            {
                SendFmt(Proto.DAILY_RES_FIND_INFO); // 次数不同步兜底重拉
                GameLog.Warn("Daily", "41903 次数不同步(4190001)→ 重拉41900");
            }
            else
            {
                TipsManager.Toast("找回失败(" + errcode + ")");
                GameLog.Warn("Daily", "41903 找回失败 code={0}", errcode);
            }
        }

        public void ResFindOneKey(int type) => SendFmt(Proto.DAILY_RES_FIND_ONEKEY, "c", type);

        private void On41904(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int type = r.ReadU8();
            List<DailyModel.ResFindVo> list = r.ReadArray(ReadResFindVo);
            if (errcode == 1)
            {
                // ⚠轮10交叉验收 minor 订正:不再拿本包整体覆盖主表——老端 SetAllResFindData 只存独立的
                // merge_find_list_(仅用于奖励展示),不碰 res_find_data;服务端本包 SendList 也只含"成功
                // 找回"的命中项,若在此覆盖主表会把未命中/不可找回的行短暂丢失。真正的全量刷新交给紧随其后的
                // 41900(下面已发)。
                TipsManager.Toast("一键找回成功");
                SendFmt(Proto.DAILY_RES_FIND_INFO); // 兜底重拉41900(对标老端 Handler41904 尾部,全量覆盖主表)
                GameLog.Info("Daily", "41904 一键找回成功 type={0} hitCount={1}(等待41900全量刷新主表)", type, list.Count);
            }
            else
            {
                TipsManager.Toast("一键找回失败(" + errcode + ")");
                GameLog.Warn("Daily", "41904 一键找回失败 code={0}", errcode);
            }
        }

        // =====================================================================================
        // 我要变强(61801)
        // =====================================================================================

        public void RequestStrongerList() => SendFmt(Proto.DAILY_STRONGER_LIST);

        private void On61801(NetReader r)
        {
            List<DailyModel.StrongStateVo> list = r.ReadArray(rr => new DailyModel.StrongStateVo
            {
                Id = (int)rr.ReadU32(),
                State = rr.ReadU8(),
                Time = (long)rr.ReadU64(),
            });
            DailyModel.Instance.SetStrongerData(list);
            GameLog.Info("Daily", "61801 我要变强状态表 count={0}", list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_DAILY_STRONGER_UPDATE);
        }

        // ---- 小工具 ----

        /// <summary>奖励摘要文案(降级 toast 用,对标老端 CongratulationObtainView 的物品名列表;同 MailController 先例)。</summary>
        private static string FormatRewardSummary(List<(int style, int typeId, long count)> rewards)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rewards.Count; i++)
            {
                if (i > 0) sb.Append('、');
                (int goodsId, int _) = GoodsModel.GetMappingTypeId(rewards[i].style, rewards[i].typeId);
                string name = GoodsModel.GetGoodsName(goodsId);
                if (string.IsNullOrEmpty(name)) name = "物品" + goodsId;
                sb.Append(name).Append('x').Append(rewards[i].count);
            }
            return sb.ToString();
        }
    }
}
