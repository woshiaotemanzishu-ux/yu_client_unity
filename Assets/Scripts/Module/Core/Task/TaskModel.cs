using System.Collections.Generic;
using Shenxiao.Framework.Util;

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

        public static readonly TaskModel Instance = new TaskModel();

        private readonly Dictionary<int, List<TaskVo>> _hasReceiveTaskList = new Dictionary<int, List<TaskVo>>();
        private readonly Dictionary<int, List<TaskVo>> _canTaskList = new Dictionary<int, List<TaskVo>>();
        private readonly Dictionary<int, List<TaskVo>> _allTaskList = new Dictionary<int, List<TaskVo>>();

        private TaskModel() { }

        public int NowSelectTaskId { get; set; }
        public int NewestFinishTaskId { get; private set; }
        public TaskVo MainLineTaskVo { get; private set; }

        public IReadOnlyDictionary<int, List<TaskVo>> AllTaskList => _allTaskList;

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
            return taskTipsType == 0;
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
