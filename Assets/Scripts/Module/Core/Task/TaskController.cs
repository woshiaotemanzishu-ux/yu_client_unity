using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dialogue;
using Shenxiao.Module.Core.Scene;

namespace Shenxiao.Module.Core.Tasks
{
    public sealed class TaskController : BaseController
    {
        public static readonly TaskController Instance = new TaskController();

        private bool _taskFinishPendingAuto;
        private int _taskOneAutoEpoch;

        private TaskController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.TASK_LIST, On30000);
            RegisterProtocal(Proto.TASK_UPDATE_ONE, On30001);
            RegisterProtocal(Proto.CC_TASK_ACCEPT, On30003);
            RegisterProtocal(Proto.CC_TASK_FINISH, On30004);
            RegisterProtocal(Proto.TASK_LATEST_FINISHED, On30005);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_COLLECT_ENDED, OnCollectEnded);
            TaskSystemAutoPilot.Init(); // 测试专用代行器(-shenxiaoPlaySmoke 门控),见该类头注释
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_COLLECT_ENDED, OnCollectEnded);
            TaskSystemAutoPilot.Shutdown();
            base.Dispose();
        }

        // 采集非成功终止 → 让任务模型延时重试当前采集任务(对标老端 FindNextOne)。
        private void OnCollectEnded() => TaskModel.Instance.OnCollectEnded();

        /// <summary>
        /// 提交完成任务(发 30004)。对标老端 TaskFinishView 的 Fire(REQUEST_CCMD_EVENT, 30004, task_id):
        /// 非对话的"完成弹层"确认/领奖后由此真实提交,服务端回推 30001/30000 刷新任务栏。
        /// (对话内的接/交走 DialogueController.AcceptTask/FinishTask,带 dialogue auto 流程;此处是弹层独立提交。)
        /// </summary>
        public void SubmitFinish(int taskId)
        {
            if (taskId <= 0) return;
            SendFmt(Proto.CC_TASK_FINISH, "i", taskId);
            GameLog.Info("Task", "send 30004 finish task={0}(TaskFinishView 提交)", taskId);
        }

        private async void OnGameStart()
        {
            _startupTaskListRequested = false;   // 配置加载期间到达的 30000 不点火(见 TryKickoffAutoTaskOnLogin)
            await TaskConfigs.EnsureLoaded();
            await TaskGuideConfigs.EnsureLoaded();
            // 奖励真实物品名/图标需 config_goods(对话奖励摘要、完成弹层、BaseAwardItem 共用 GoodsModel)。
            await GoodsModel.EnsureLoaded();
            // 任务条"与<NPC名>交谈"文案需 config_npc(对标老端 GetTaskTipsMsgByMainUITaskItem 取 config_npc.name)。
            await NpcConfigs.EnsureLoaded();
            _taskFinishPendingAuto = false;
            _taskOneAutoEpoch++;
            _loginKickoffDone = false;
            _startupTaskListRequested = true;   // 从这个请求之后的首个 30000 才允许点火
            TaskModel.Instance.ClearData();
            SendFmt(Proto.TASK_LIST);
            GameLog.Info("Task", "request task list proto={0}", Proto.TASK_LIST);
        }

        // 冷启动点火(活服实证修复):登录后 30000 全量到达时,若开着自动任务且当前主线任务半途(如上次
        // 会话卡在副本/杀怪),没有任何 30001 增量来「点火」→ FindNextAutoFightTask 永不启动
        // (既有两个续跑入口都依赖任务完成事件)。对标老端登录 loading 关闭后的自动任务恢复;
        // 每次进游戏后的首个 30000 只触发一次。
        // 门禁 _startupTaskListRequested:服务端在登录早期(10004 前)就会推一版 30000,那时配置未加载、
        // OnGameStart 也未跑,点火只会空跑;且旧逻辑里这个早推包消耗掉 _loginKickoffDone 后又被
        // OnGameStart 重置,造成一次登录点火两遍(test.log 831/1003 行双 kickoff 实证)。
        private bool _loginKickoffDone;
        private bool _startupTaskListRequested;

        private void TryKickoffAutoTaskOnLogin()
        {
            if (!_startupTaskListRequested || _loginKickoffDone) return;
            _loginKickoffDone = true;
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;
            _ = KickoffWhenMainRoleReadyAsync();
        }

        /// <summary>点火须等主角渲染就绪(活服实证:30000 到达早于 MainRoleAgent 创建,过早点火会在
        /// DoPassMainDungeonTask 等分支「no MainRoleAgent」早退且不再重试)。轮询最多 60s。</summary>
        private async Task KickoffWhenMainRoleReadyAsync()
        {
            for (int i = 0; i < 120; i++)
            {
                if (MainRoleAgent.Current != null)
                {
                    GameLog.Info("Task", "login kickoff: auto task resume (main role ready after {0}ms)", i * 500);
                    TaskModel.Instance.FindNextAutoFightTask();
                    return;
                }
                await Task.Delay(500);
            }
            GameLog.Warn("Task", "login kickoff abandoned: MainRoleAgent not ready in 60s");
        }

        private void On30000(NetReader r)
        {
            Dictionary<int, List<TaskVo>> allTaskList = new Dictionary<int, List<TaskVo>>();
            Dictionary<int, List<TaskVo>> canTaskList = new Dictionary<int, List<TaskVo>>();
            Dictionary<int, List<TaskVo>> hasReceiveTaskList = new Dictionary<int, List<TaskVo>>();

            int canCount = r.ReadU16();
            for (int i = 0; i < canCount; i++)
            {
                ReadTaskVo(r, out int taskId, out List<TaskVo> tips);
                canTaskList[taskId] = tips;
                allTaskList[taskId] = tips;
            }

            int receiveCount = r.ReadU16();
            for (int i = 0; i < receiveCount; i++)
            {
                ReadTaskVo(r, out int taskId, out List<TaskVo> tips);
                hasReceiveTaskList[taskId] = tips;
                allTaskList[taskId] = tips;
            }

            TaskModel.Instance.SetTaskLists(canTaskList, hasReceiveTaskList, allTaskList);
            GameLog.Info("Task", "30000 tasks can={0} receive={1} all={2}", canCount, receiveCount, allTaskList.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_TASK_LIST_UPDATED);
            TryContinueAutoTaskAfterList();
            TryKickoffAutoTaskOnLogin();
        }

        private void On30001(NetReader r)
        {
            ReadTaskVo(r, out int taskId, out List<TaskVo> tips, true);
            TaskModel.Instance.UpdateTask(taskId, tips);
            EventDispatcher.Emit(GlobalEvent.EVT_TASK_ONE_UPDATED, taskId);
            EventDispatcher.Emit(GlobalEvent.EVT_TASK_LIST_UPDATED);
            TryContinueAutoTaskAfterOne(taskId);
        }

        private void On30005(NetReader r)
        {
            int taskId = (int)r.ReadU32();
            TaskModel.Instance.SetNewestFinishTaskId(taskId);
            GameLog.Info("Task", "30005 newest finish task id={0}", taskId);
            EventDispatcher.Emit(GlobalEvent.EVT_TASK_LIST_UPDATED);
            EventDispatcher.Emit(GlobalEvent.EVT_GAME_START_FLAG_READY, "30005");
        }

        private void On30003(NetReader r)
        {
            if (r.Remaining < 6)
            {
                GameLog.Warn("Task", "30003 accept reply too short remaining={0}", r.Remaining);
                return;
            }

            int taskId = (int)r.ReadU32();
            int code = r.ReadU16();
            GameLog.Info("Task", "30003 accept reply task={0} code={1}", taskId, code);
        }

        private void On30004(NetReader r)
        {
            if (r.Remaining < 8)
            {
                GameLog.Warn("Task", "30004 finish reply too short remaining={0}", r.Remaining);
                return;
            }

            int taskId = (int)r.ReadU32();
            int code = (int)r.ReadU32();
            GameLog.Info("Task", "30004 finish reply task={0} code={1}", taskId, code);
            if (code != 1) return;

            TaskConfigs.TaskCfg cfg = TaskConfigs.Get(taskId);
            int taskType = cfg?.Type ?? 0;
            if (taskType == TaskModel.AWAKE_LINE || taskType == TaskModel.NORMAL_DAILY) return;
            _taskFinishPendingAuto = true;
        }

        private void TryContinueAutoTaskAfterList()
        {
            if (!_taskFinishPendingAuto) return;
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;

            _taskFinishPendingAuto = false;
            TaskModel.Instance.FindNextAutoFightTask();
        }

        private void TryContinueAutoTaskAfterOne(int taskId)
        {
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;

            TaskVo task = TaskModel.Instance.MainLineTaskVo;
            if (task == null || task.TaskId != taskId) return;
            if (!TaskModel.Instance.IsAllStepFinish(taskId))
            {
                TryResumeAutoFightAfterProgress(taskId, task);
                return;
            }

            if (AutoFightModel.Instance.AutoFightWeight == AutoFightModel.AUTO_WEIGHT_TASK)
            {
                AutoFightModel.Instance.SetAutoFightWeight(AutoFightModel.AUTO_WEIGHT_CLOSE);
                SceneCombat.Instance.SetClickTarget(0);
            }

            int epoch = ++_taskOneAutoEpoch;
            _ = ContinueAutoTaskAfterOneAsync(taskId, epoch);
        }

        private void TryResumeAutoFightAfterProgress(int taskId, TaskVo task)
        {
            if (task == null
                || (task.TaskTipsType != TaskModel.TIP_KILL
                    && task.TaskTipsType != TaskModel.TIP_ITEM
                    && task.TaskTipsType != TaskModel.TIP_COLLECT
                    && task.TaskTipsType != TaskModel.TIP_PASS_MAIN_DUNGEON)) return;

            int epoch = ++_taskOneAutoEpoch;
            _ = ResumeAutoFightAfterProgressAsync(taskId, epoch);
        }

        private async Task ContinueAutoTaskAfterOneAsync(int taskId, int epoch)
        {
            // 老端主线任务 30001 后 DoTask 是立即执行(TaskModel.ts:2226-2234,仅帮派/日常 setTimeout 700ms);
            // 这里只留一小拍去重窗(同一完成常连发多条 30001,epoch 取最后一条),不再人为停 350ms——
            // 那是"任务推进后角色愣一下才动"的直接来源之一(用户实感,以老端节奏为准)。
            await Task.Delay(100);
            if (epoch != _taskOneAutoEpoch) return;
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;

            TaskVo task = TaskModel.Instance.MainLineTaskVo;
            if (task == null || task.TaskId != taskId) return;
            if (!TaskModel.Instance.IsAllStepFinish(taskId)) return;

            GameLog.Info("Task", "30001 task={0} finished -> continue auto task (open finish countdown)", taskId);
            TaskModel.Instance.FindNextAutoFightTask();
        }

        private async Task ResumeAutoFightAfterProgressAsync(int taskId, int epoch)
        {
            await Task.Delay(100); // 同上:老端进度更新即续跑,只留去重窗(原 250ms 是可感知的停顿)
            if (epoch != _taskOneAutoEpoch) return;
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;

            TaskVo task = TaskModel.Instance.MainLineTaskVo;
            if (task == null || task.TaskId != taskId) return;
            if (TaskModel.Instance.IsAllStepFinish(taskId)) return;

            bool resumed = TaskModel.Instance.ResumeCurrentTaskAutoFight();
            GameLog.Info("Task", "30001 task={0} progress -> resume task auto fight resumed={1}", taskId, resumed);
        }

        private static void ReadTaskVo(NetReader r, out int taskId, out List<TaskVo> tips, bool setNewFinishFlag = false)
        {
            taskId = (int)r.ReadU32();
            int tipCount = r.ReadU16();
            tips = new List<TaskVo>(tipCount);

            TaskConfigs.TaskCfg cfg = TaskConfigs.Get(taskId);
            for (int i = 0; i < tipCount; i++)
            {
                TaskVo vo = new TaskVo(
                    taskId,
                    r.ReadU8(),
                    r.ReadString(),
                    r.ReadU8(),
                    (int)r.ReadU32(),
                    (int)r.ReadU32(),
                    (int)r.ReadU32(),
                    (int)r.ReadU32(),
                    (int)r.ReadU32(),
                    r.ReadU16(),
                    r.ReadU16(),
                    r.ReadU8());

                NormalizeTaskStep(vo);
                if (setNewFinishFlag && vo.HasFinish == 1) vo.NewFinishFlag = true;
                vo.ApplyConfig(cfg);
                tips.Add(vo);
            }
        }

        private static void NormalizeTaskStep(TaskVo vo)
        {
            if (vo == null) return;
            if (vo.TaskTipsType == 0 || vo.TaskTipsType == 23 || vo.TaskTipsType == 25)
            {
                vo.NowNum = vo.HasFinish != 0 ? 1 : 0;
                vo.NeedNum = 1;
            }
            else if (vo.NeedNum > 0 && vo.NowNum > vo.NeedNum)
            {
                vo.NowNum = vo.NeedNum;
            }
        }
    }
}
