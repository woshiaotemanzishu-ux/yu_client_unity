using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dialogue;

namespace Shenxiao.Module.Core.Tasks
{
    public sealed class TaskController : BaseController
    {
        public static readonly TaskController Instance = new TaskController();

        private bool _taskFinishPendingAuto;

        private TaskController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.TASK_LIST, On30000);
            RegisterProtocal(Proto.TASK_UPDATE_ONE, On30001);
            RegisterProtocal(Proto.CC_TASK_ACCEPT, On30003);
            RegisterProtocal(Proto.CC_TASK_FINISH, On30004);
            RegisterProtocal(Proto.TASK_LATEST_FINISHED, On30005);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            base.Dispose();
        }

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
            await TaskConfigs.EnsureLoaded();
            await TaskGuideConfigs.EnsureLoaded();
            // 奖励真实物品名/图标需 config_goods(对话奖励摘要、完成弹层、BaseAwardItem 共用 GoodsModel)。
            await GoodsModel.EnsureLoaded();
            // 任务条"与<NPC名>交谈"文案需 config_npc(对标老端 GetTaskTipsMsgByMainUITaskItem 取 config_npc.name)。
            await NpcConfigs.EnsureLoaded();
            _taskFinishPendingAuto = false;
            TaskModel.Instance.ClearData();
            SendFmt(Proto.TASK_LIST);
            GameLog.Info("Task", "request task list proto={0}", Proto.TASK_LIST);
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
        }

        private void On30001(NetReader r)
        {
            ReadTaskVo(r, out int taskId, out List<TaskVo> tips, true);
            TaskModel.Instance.UpdateTask(taskId, tips);
            EventDispatcher.Emit(GlobalEvent.EVT_TASK_ONE_UPDATED, taskId);
            EventDispatcher.Emit(GlobalEvent.EVT_TASK_LIST_UPDATED);
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
