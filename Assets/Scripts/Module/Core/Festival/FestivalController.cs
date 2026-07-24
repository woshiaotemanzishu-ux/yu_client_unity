using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Festival
{
    /// <summary>
    /// 祭典(宝录)控制器(对标老客户端 FestivalController,模块 194)。进游戏请求 19401;
    /// 回包据 uid 增删主界面图标 223(GetEntranceOpenState:uid>0 显示)。点击图标经 MainUIRouter
    /// 路由 "223" 打开宝录面板(<see cref="FestivalFlow"/>/<see cref="FestivalBootstrap"/> 自动循环 轮18 PK3
    /// 已接:容器级开关窗,子面板绑定留尾包)。等级变化(EVT_ROLE_INFO_UPDATE)时复请求
    /// 19401(对标老端 CHANGE_LEVEL→发 19401),让升到 120 级开启宝录后图标及时出现。
    /// 自动循环 轮18 便宜活批 PK3 扩展:补 19400(纯推送错误码)/19402(领等级奖)/19403(任务列表,
    /// 二层嵌套,收到 19401 且 GetEntranceOpenState() 为真时自动 type=0 发起,老端 Controller:134-141
    /// 镜像;关闭态不发,B6修复)/19404(领任务经验)/19405
    /// (购买高阶,pt_194.erl 无 write 子句,**无回执**,成功与否只能等下一次 19401 刷新,发送侧禁止阻塞
    /// 等待本号 ack)。数据层落地,面板 UI 待用户验收。既有 19401 图标增删逻辑(GetEntranceOpenState
    /// 判定 AddIconAsync/DeleteIcon)一行未删。
    /// </summary>
    public sealed class FestivalController : BaseController
    {
        public static readonly FestivalController Instance = new FestivalController();
        private FestivalController() { }

        public const string ICON_TYPE = FestivalModel.ICON_TYPE;

        // 复请求 19401 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        private static void ShowError(int code) => TipsManager.Toast("错误(" + code + ")"); // 错误码表未移植,显码降级

        protected override void Register()
        {
            RegisterProtocal(Proto.FESTIVAL_INFO, On19401);
            // PK3 补全:19400(纯推送)/19402-19404(双向);19405 pt_194.erl 无 write 子句,不注册 recv。
            RegisterProtocal(Proto.FESTIVAL_ERROR, On19400);
            RegisterProtocal(Proto.FESTIVAL_LEVEL_AWARD_CLAIM, On19402);
            RegisterProtocal(Proto.FESTIVAL_TASK_LIST, On19403);
            RegisterProtocal(Proto.FESTIVAL_TASK_EXP_CLAIM, On19404);
            // 对标老端 CHANGE_LEVEL→发 19401:等级变化时复请求(120级开启宝录)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            ActivityIconManager.Instance.SetIconRedDot(ICON_TYPE, false);
            FestivalModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 19401)。</summary>
        public void RequestStartup()
        {
            SendFmt(Proto.FESTIVAL_INFO);
        }

        // 19401: uid:h, act_id:c, type:c, lv:h, exp:i, expired_time:i, reward_list[u16×{lv:h, status1:c, status2:c}]
        private void On19401(NetReader r)
        {
            int uid = r.ReadU16();
            int actId = r.ReadU8();
            int type = r.ReadU8();
            int lv = r.ReadU16();
            int exp = (int)r.ReadU32();
            int expiredTime = (int)r.ReadU32();
            int rewardCount = r.ReadU16();
            var rewardList = new List<FestivalModel.LevelRewardState>(rewardCount);
            for (int i = 0; i < rewardCount; i++)
            {
                rewardList.Add(new FestivalModel.LevelRewardState
                {
                    Lv = r.ReadU16(),
                    Status1 = r.ReadU8(), // 普通档领取态
                    Status2 = r.ReadU8(), // 高阶档领取态
                });
            }

            FestivalModel m = FestivalModel.Instance;
            m.SetBasicInfo(uid, actId, type, lv, exp, expiredTime, rewardList);

            // B6修复:老端 Controller:122-144 的 19403 自动请求在 GetEntranceOpenState() 为真的分支内
            // (ts:134-141),关闭态(else 分支)只删图标,不发 19403。
            if (m.GetEntranceOpenState())
            {
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
                RequestTaskList(0); // 老端 Controller:140 镜像:开启态自动发 19403(type=0)求全部三类任务列表。
            }
            else
            {
                ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            }
            RefreshEntranceRedDot();

            GameLog.Info("Festival", "19401 宝录: uid={0} act_id={1} type={2} lv={3} open={4}",
                uid, actId, type, lv, m.GetEntranceOpenState());
        }

        // 对标老端:主角等级变化复请求 19401(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        // ---- PK3 补全:19400/19402-19405 发送封装 ----

        /// <summary>19402 领取等级奖励(AwardListItem:181/LevelAwardView:120,lv=0 代表全部)。</summary>
        public void RequestLevelAward(int lv) => SendFmt(Proto.FESTIVAL_LEVEL_AWARD_CLAIM, "h", lv);

        /// <summary>19403 任务列表(type=0 代表三类全部,1日/2周/3赛季)。</summary>
        public void RequestTaskList(int type) => SendFmt(Proto.FESTIVAL_TASK_LIST, "c", type);

        /// <summary>19404 领取任务经验(TaskView:235/TaskListItem:100,taskId=0 代表全部)。</summary>
        public void ClaimTaskExp(int type, int taskId) => SendFmt(Proto.FESTIVAL_TASK_EXP_CLAIM, "ch", type, taskId);

        /// <summary>19405 购买高阶宝录(1=豪华/2=至尊)。**无回执**,pt_194.erl 无对应 write 子句,
        /// 发送侧禁止阻塞等待本号 ack,成功与否只能等 <see cref="On19401"/> 下一次刷新。</summary>
        public void RequestPurchase(int type) => SendFmt(Proto.FESTIVAL_PURCHASE, "c", type);

        // ---- 19400: 通用返回码(纯推送,无 read 子句)。Code:32, Args:string ----
        // m6修复:老端 On19400(ts:113-119)`if(scmd.code==1){} else { Util.ErrorCodeShow(scmd.code) }`,
        // code==1 时什么都不做,非1才显码——本端补齐失败显码分支。
        private void On19400(NetReader r)
        {
            int code = (int)r.ReadU32();
            string args = r.ReadString();
            if (code != 1) ShowError(code);
            GameLog.Info("Festival", "19400 错误推送: code={0} args={1}", code, args);
            EventDispatcher.Emit(GlobalEvent.EVT_FESTIVAL_UPDATE, Proto.FESTIVAL_ERROR);
        }

        // ---- 19402: RewardList(ObjectList,无独立 Code——非空即成功,对齐老端读法) ----
        private void On19402(NetReader r)
        {
            int count = r.ReadU16();
            var rewardList = new List<FestivalModel.RewardObj>(count);
            for (int i = 0; i < count; i++)
            {
                rewardList.Add(new FestivalModel.RewardObj
                {
                    Type = r.ReadU8(),
                    ObjectTypeId = (int)r.ReadU32(),
                    Num = (int)r.ReadU32(),
                });
            }

            FestivalModel.Instance.SetLevelAwardResult(rewardList);
            GameLog.Info("Festival", "19402 领取等级奖励: rewardN={0} success={1}", count, FestivalModel.Instance.LastLevelAwardSuccess);
            EventDispatcher.Emit(GlobalEvent.EVT_FESTIVAL_UPDATE, Proto.FESTIVAL_LEVEL_AWARD_CLAIM);
        }

        // ---- 19403: TypeList[u16×item_to_bin_1(3字段:Type:8, TaskList[u16×item_to_bin_2{TaskId:16,FinishTimes:8,CurNum:32,Status:8}], RefreshTime:32)]。二层嵌套 ----
        private void On19403(NetReader r)
        {
            int groupCount = r.ReadU16();
            for (int i = 0; i < groupCount; i++)
            {
                int type = r.ReadU8();
                int taskCount = r.ReadU16();
                var tasks = new List<FestivalModel.TaskEntry>(taskCount);
                for (int j = 0; j < taskCount; j++)
                {
                    tasks.Add(new FestivalModel.TaskEntry
                    {
                        TaskId = r.ReadU16(),
                        FinishTimes = r.ReadU8(),
                        CurNum = (int)r.ReadU32(),
                        Status = r.ReadU8(),
                    });
                }
                int refreshTime = (int)r.ReadU32();
                FestivalModel.Instance.SetTaskGroup(type, tasks, refreshTime);
            }

            GameLog.Info("Festival", "19403 任务列表: groups={0}", groupCount);
            RefreshEntranceRedDot();
            EventDispatcher.Emit(GlobalEvent.EVT_FESTIVAL_UPDATE, Proto.FESTIVAL_TASK_LIST);
        }

        private static void RefreshEntranceRedDot()
        {
            FestivalModel m = FestivalModel.Instance;
            ActivityIconManager.Instance.SetIconRedDot(
                ICON_TYPE, m.GetEntranceOpenState() && m.GetEntranceRedDot());
        }

        // ---- 19404: Exp:32(无Code,捎带随后 19401+19403 刷新,由各自 handler 落地) ----
        private void On19404(NetReader r)
        {
            int exp = (int)r.ReadU32();
            FestivalModel.Instance.LastTaskExpClaimed = exp;
            GameLog.Info("Festival", "19404 任务经验领取: exp={0}", exp);
            EventDispatcher.Emit(GlobalEvent.EVT_FESTIVAL_UPDATE, Proto.FESTIVAL_TASK_EXP_CLAIM);
        }
    }
}
