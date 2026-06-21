using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Dialogue;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Scene.Vo;

namespace Shenxiao.Module.Core.Tasks
{
    public sealed class TaskModel
    {
        public const int MAIN_LINE = 1;
        public const int AWAKE_LINE = 2;
        public const int REINCARNATION = 3;
        public const int EXTENSION_LINE = 5;
        public const int GUILD = 6;
        public const int DAILY = 7;
        public const int NORMAL_DAILY = 8;
        public const int EUDAEMON_TASK = 9;
        public const int KFHOLYAREA_TASK = 10;

        // —— task_tips_type(服务端 pt_300 下发的提示类型;对标老端 TaskTipType,yu_client TaskModel.ts:52-243)——
        public const int TIP_TALK = 5;        // 与 NPC 对话(可选)
        public const int TIP_START_TALK = 6;  // 开始对话(不可选)
        public const int TIP_END_TALK = 7;    // 结束对话(不可选)
        public const int TIP_COIN = 80;       // 上交铜钱

        public static readonly TaskModel Instance = new TaskModel();

        private readonly Dictionary<int, List<TaskVo>> _hasReceiveTaskList = new Dictionary<int, List<TaskVo>>();
        private readonly Dictionary<int, List<TaskVo>> _canTaskList = new Dictionary<int, List<TaskVo>>();
        private readonly Dictionary<int, List<TaskVo>> _allTaskList = new Dictionary<int, List<TaskVo>>();

        private TaskModel() { }

        public int NowSelectTaskId { get; set; }
        public int NewestFinishTaskId { get; private set; }
        public TaskVo MainLineTaskVo { get; private set; }

        public IReadOnlyDictionary<int, List<TaskVo>> AllTaskList => _allTaskList;

        /// <summary>该任务当前是否"可接"(在 can 列表)。对话空文本自动接受分支用(对标 GetCanTaskList)。</summary>
        public bool IsCanTask(int taskId) => _canTaskList.ContainsKey(taskId);

        /// <summary>该任务当前是否"已接"(在 received 列表)。对话空文本自动完成分支用(对标 GetHasReceiveTaskList)。</summary>
        public bool IsReceivedTask(int taskId) => _hasReceiveTaskList.ContainsKey(taskId);

        public void ClearData()
        {
            _hasReceiveTaskList.Clear();
            _canTaskList.Clear();
            _allTaskList.Clear();
            MainLineTaskVo = null;
            NowSelectTaskId = 0;
        }

        public void SetNewestFinishTaskId(int taskId)
        {
            NewestFinishTaskId = taskId;
        }

        public void SetTaskLists(Dictionary<int, List<TaskVo>> canTaskList,
            Dictionary<int, List<TaskVo>> hasReceiveTaskList,
            Dictionary<int, List<TaskVo>> allTaskList)
        {
            Replace(_canTaskList, canTaskList);
            Replace(_hasReceiveTaskList, hasReceiveTaskList);
            Replace(_allTaskList, allTaskList);
            RefreshMainLineTaskVo();
        }

        public void UpdateTask(int taskId, List<TaskVo> tips)
        {
            if (taskId <= 0 || tips == null) return;
            _hasReceiveTaskList[taskId] = tips;
            _allTaskList[taskId] = tips;
            RefreshMainLineTaskVo();
        }

        public List<TaskEntry> GetTaskListForMainUI()
        {
            List<TaskEntry> result = new List<TaskEntry>();
            foreach (KeyValuePair<int, List<TaskVo>> kv in _allTaskList)
            {
                if (kv.Value == null || kv.Value.Count == 0) continue;
                if (IsAwakeTask(kv.Key) || IsEudaemonTask(kv.Key) || IsKKHolyAreaTask(kv.Key)) continue;
                if (IsMainTask(kv.Key) && MainLineTaskNeedShowArrow()) continue;

                (int sortIndex, int sortSubIndex, int sameTypeOrderIndex) = GetSortIndex(kv.Key);
                result.Add(new TaskEntry
                {
                    TaskId = kv.Key,
                    SortIndex = sortIndex,
                    SortSubIndex = sortSubIndex,
                    SameTypeOrderIndex = sameTypeOrderIndex,
                    TipsList = kv.Value,
                });
            }

            if (result.Count == 1 && MainLineTaskVo != null && !MainLineTaskNeedShowArrow())
            {
                result.Clear();
            }

            result.Sort(CompareTaskEntry);
            return result;
        }

        public TaskVo FindUnFinishTask(List<TaskVo> tipsList)
        {
            if (tipsList == null || tipsList.Count == 0) return null;
            TaskVo task = tipsList[0];
            for (int i = 0; i < tipsList.Count; i++)
            {
                if (tipsList[i].HasFinish != 1)
                {
                    task = tipsList[i];
                    break;
                }
            }
            return task;
        }

        public bool IsAllStepFinish(int taskId)
        {
            if (!_allTaskList.TryGetValue(taskId, out List<TaskVo> tips) || tips == null || tips.Count == 0) return false;
            for (int i = 0; i < tips.Count; i++)
            {
                if (tips[i].HasFinish != 1) return false;
            }
            return true;
        }

        public string GetTaskTagName(int taskType)
        {
            if (taskType == MAIN_LINE) return "主";
            if (taskType == DAILY || taskType == NORMAL_DAILY || taskType == EUDAEMON_TASK) return "日";
            if (taskType == GUILD) return "结";
            if (taskType == REINCARNATION) return "转";
            if (taskType == AWAKE_LINE) return "唤";
            return "支";
        }

        public string GetTaskColor(int taskType)
        {
            if (taskType == MAIN_LINE) return "#ff9015";
            if (taskType == REINCARNATION) return "#a376ff";
            return "#60aeff";
        }

        public string BuildMainUITips(TaskVo task)
        {
            if (task == null) return "";

            string tips = string.IsNullOrEmpty(task.TaskTipsMsg) ? task.Tips : task.TaskTipsMsg;
            bool finish = IsAllStepFinish(task.TaskId);
            if (finish)
            {
                if (!IsFindNpcTask(task.TaskTipsType)) tips += "(完成)";
            }
            else if (task.ShowNum > 0)
            {
                tips += " (0/" + task.ShowNum + ")";
            }
            else
            {
                tips += " (" + task.NowNum + "/" + task.NeedNum + ")";
            }
            return tips;
        }

        public bool IsMainTask(int taskId)
        {
            return TaskConfigs.Get(taskId)?.Type == MAIN_LINE;
        }

        public bool IsAwakeTask(int taskId)
        {
            return TaskConfigs.Get(taskId)?.Type == AWAKE_LINE;
        }

        public bool IsEudaemonTask(int taskId)
        {
            return TaskConfigs.Get(taskId)?.Type == EUDAEMON_TASK;
        }

        public bool IsKKHolyAreaTask(int taskId)
        {
            return TaskConfigs.Get(taskId)?.Type == KFHOLYAREA_TASK;
        }

        public bool MainLineTaskNeedShowArrow()
        {
            return MainLineTaskVo != null && MainLineTaskVo.NeedGuide != 0;
        }

        public (int sortIndex, int sortSubIndex, int sameTypeOrderIndex) GetSortIndex(int taskId)
        {
            TaskConfigs.TaskCfg cfg = TaskConfigs.Get(taskId);
            if (cfg == null) return (99, 99, 0);

            bool finish = IsAllStepFinish(taskId);
            int index;
            int subIndex;
            if (finish)
            {
                index = 3;
                subIndex = GetTypeSortOrder(cfg.Type);
            }
            else
            {
                index = GetTypeSortOrder(cfg.Type);
                subIndex = index;
            }
            return (index == 0 ? 99 : index, subIndex == 0 ? 99 : subIndex, cfg.Sep);
        }

        private void RefreshMainLineTaskVo()
        {
            MainLineTaskVo = null;
            foreach (KeyValuePair<int, List<TaskVo>> kv in _allTaskList)
            {
                if (!IsMainTask(kv.Key)) continue;
                MainLineTaskVo = FindUnFinishTask(kv.Value);
                return;
            }
        }

        private static int GetTypeSortOrder(int taskType)
        {
            if (taskType == MAIN_LINE) return 2;
            if (taskType == REINCARNATION) return 4;
            if (taskType == DAILY) return 5;
            if (taskType == GUILD) return 6;
            if (taskType == EXTENSION_LINE) return 7;
            if (taskType == NORMAL_DAILY) return 8;
            if (taskType == EUDAEMON_TASK) return 9;
            return 99;
        }

        private static bool IsFindNpcTask(int taskTipsType)
        {
            // 对标 TaskModel.ts:2966-2972 IsFindNpcTask:Talk/StartTalk/EndTalk 三类为"找 NPC 对话"任务。
            return taskTipsType == TIP_TALK || taskTipsType == TIP_START_TALK || taskTipsType == TIP_END_TALK;
        }

        /// <summary>
        /// 任务点击主入口(最小等价,对标老端 TaskModel.DoTask,yu_client TaskModel.ts:744-784 + 797 switch)。
        /// 先置选中态(NowSelectTaskId)并广播 EVT_TASK_SELECT_CHANGED(对标老端 CLICK_DO_TASK),再按
        /// task_tips_type 进入正确分支:找 NPC 对话(Talk/StartTalk/EndTalk)→ 定位 NPC;完成且非对话 → 完成弹层;
        /// 带场景坐标 → 寻路/切场景。未移植的子系统(对话/完成弹层/寻路)给精确 blocker,不臆造、不假装完成。
        /// </summary>
        public void DoTask(TaskVo task)
        {
            if (task == null) task = MainLineTaskVo;
            if (task == null) { GameLog.Warn("Task", "DoTask: 无可执行任务(传入 null 且无主线任务)"); return; }

            // 选中态(对标 now_select_task_id):点任务即设选中并广播,任务栏据此刷新 _img_select。
            NowSelectTaskId = task.TaskId;
            EventDispatcher.Emit(GlobalEvent.EVT_TASK_SELECT_CHANGED, task.TaskId);
            GameLog.Info("Task", "DoTask: id={0} tipsType={1} finish={2} npcId={3} scene=({4},{5},{6})",
                task.TaskId, task.TaskTipsType, task.HasFinish, task.Id, task.SceneId, task.SceneX, task.SceneY);

            // 1) 找 NPC 对话任务(对标 ts:783 finish 早退排除 find-npc + ts:1767 Talk/StartTalk/EndTalk case)。
            if (IsFindNpcTask(task.TaskTipsType)) { DoFindNpcTask(task); return; }

            // 2) 全部完成且非对话 → 打开完成弹层真实入口(对标 ts:2385 TASK_OPEN_VIEW 'TaskFinishView')。
            if (IsAllStepFinish(task.TaskId)) { DoFinishTask(task); return; }

            // 3) 带场景坐标 → 寻路/切场景(对标 Kill/Collect/Item 等 case 的 pathfind / USE_FLY_SHOE)。
            if (task.SceneId > 0 && (task.SceneX > 0 || task.SceneY > 0)) { DoGotoSceneTask(task); return; }

            GameLog.Warn("Task", "DoTask blocker: tipsType={0} 其余 case 未移植(对标 TaskModel.ts:797 switch 的 60+ case)。" +
                "当前最小入口只覆盖 对话/完成/场景坐标 三类;其余(开背包/锻造/进副本等)按需逐 case 补。", task.TaskTipsType);
        }

        /// <summary>
        /// 找 NPC 对话(对标 ts:1767-1835 Talk/StartTalk/EndTalk)。定位 NPC → 打开真实对话(12101/12102)。
        /// 老端是"主角走到 NPC(MainRoleToNpc)→ 到达后 SHOW_TASK → DialogueController";本轮 P1 先在点击时
        /// 直接打开对话入口(ShowTask),把"走到 NPC 再触发"留给 P2(MainRoleToNpc 到达回调里调 ShowTask)。
        /// 去重(对话已开不重复)由 DialogueController/DialogueModel.DialogIsOpen 负责。
        /// </summary>
        private void DoFindNpcTask(TaskVo task)
        {
            if (task.Id == 0) { GameLog.Info("Task", "DoTask: 自言自语任务(npcId=0),无目标 NPC,跳过"); return; }

            NpcVo npc = SceneManager.Instance.GetNpc(task.Id);
            if (npc == null)
            {
                GameLog.Warn("Task",
                    "DoTask 找 NPC blocker: 目标 NPC {0} 不在当前场景(任务场景={1})→ 需切到任务场景再交互;" +
                    "跨场景切换(老端 USE_FLY_SHOE/飞鞋协议)未移植 → blocker。", task.Id, task.SceneId);
                return;
            }

            // NPC 在当前场景:打开对话入口(发 12101)。P2 将在此前插入"走到 NPC"动作。
            GameLog.Info("Task", "DoTask 找 NPC: NPC {0} 在场景 pos=({1},{2}) → 打开对话(12101)", task.Id, npc.X, npc.Y);
            DialogueController.Instance.ShowTask(task.Id);
        }

        /// <summary>完成提交(对标 ts:2385:TaskFinishView/TaskCircleFinishView + 协议 30004)。</summary>
        private void DoFinishTask(TaskVo task)
        {
            // 真实入口 = TaskFinishView(完成后展示奖励并发 30004 提交)。该 View 在 Unity 端未生成/未移植 → blocker。
            // 不直接发 30004:老端要求经完成弹层确认再提交,跳过弹层直接提交不忠实。
            GameLog.Warn("Task",
                "DoTask 完成 blocker: 任务 {0} 全步完成,应开 TaskFinishView(展示奖励 + 发 30004 提交)。" +
                "该完成弹层未移植 → blocker。移植后这里 Emit TASK_OPEN_VIEW 打开它。", task.TaskId);
        }

        /// <summary>带场景坐标(对标 Kill/Collect/Item case:同场景寻路到点,跨场景飞鞋)。</summary>
        private void DoGotoSceneTask(TaskVo task)
        {
            int curScene = RoleModel.Instance.SceneId;
            if (task.SceneId == curScene || task.SceneId == 0)
            {
                GameLog.Warn("Task",
                    "DoTask 寻路 blocker: 任务 {0} 目标在当前场景 pos=({1},{2}),但自动寻路到点未移植" +
                    "(MainRoleAgent 仅摇杆驱动,无 A* 寻路)→ blocker。目标坐标已就绪,可手动摇杆走到。",
                    task.TaskId, task.SceneX, task.SceneY);
            }
            else
            {
                GameLog.Warn("Task",
                    "DoTask 切场景 blocker: 任务 {0} 目标在场景 {1}(当前 {2}),跨场景切换(老端 USE_FLY_SHOE/飞鞋)" +
                    "未移植 → blocker。目标坐标 ({3},{4}) 已就绪。", task.TaskId, task.SceneId, curScene, task.SceneX, task.SceneY);
            }
        }

        private static int CompareTaskEntry(TaskEntry a, TaskEntry b)
        {
            int v = a.SortIndex.CompareTo(b.SortIndex);
            if (v != 0) return v;
            v = a.SortSubIndex.CompareTo(b.SortSubIndex);
            if (v != 0) return v;
            v = a.SameTypeOrderIndex.CompareTo(b.SameTypeOrderIndex);
            if (v != 0) return v;
            return a.TaskId.CompareTo(b.TaskId);
        }

        private static void Replace(Dictionary<int, List<TaskVo>> dst, Dictionary<int, List<TaskVo>> src)
        {
            dst.Clear();
            if (src == null) return;
            foreach (KeyValuePair<int, List<TaskVo>> kv in src)
            {
                dst[kv.Key] = kv.Value;
            }
        }

        public sealed class TaskEntry
        {
            public int TaskId;
            public int SortIndex;
            public int SortSubIndex;
            public int SameTypeOrderIndex;
            public List<TaskVo> TipsList;
        }
    }
}
