using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Task;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Tasks
{
    /// <summary>
    /// 任务总览页编排（老端 TaskView → TaskBarItem → TaskContentSubView）。静态布局与模板
    /// 来自 TaskModule.prefab；本类只负责模块生命周期、真实任务数据、展开态、奖励与点击语义。
    /// MainUI 的“任务页签二次点击”入口由 MainUI 岛接入 <see cref="Toggle"/>。
    /// </summary>
    public static class TaskFlow
    {
        private const string ModuleName = "task";
        private const string PrefabName = "TaskModule";
        private static GameObject _moduleRoot;
        private static TaskViewRuntime _view;
        private static bool _loading;

        public static void Toggle()
        {
            if (_view != null && _view.IsShown)
            {
                Close();
                return;
            }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            _view?.Hide();
        }

        private static async Task OpenAsync()
        {
            if (_view != null && _view.IsAlive)
            {
                _view.Show();
                return;
            }
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
                _moduleRoot = null;
            }
            if (_loading) return;
            _loading = true;

            GameObject root = null;
            try
            {
                string key = GameResPath.GetUIPrefab(ModuleName, PrefabName);
                root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
                if (root == null)
                {
                    GameLog.Error("Task", "TaskModule prefab load failed: {0}", key);
                    return;
                }

                root.name = PrefabName;
                foreach (Transform child in root.transform) child.gameObject.SetActive(false);
                TaskViewBind bind = root.GetComponentInChildren<TaskViewBind>(true);
                if (bind == null)
                {
                    GameLog.Error("Task", "TaskModule missing TaskViewBind");
                    ResManager.ReleaseInstance(root);
                    return;
                }

                _moduleRoot = root;
                _view = new TaskViewRuntime(bind, root);
                _view.Show();
                GameLog.Info("Task", "任务总览页打开: {0}", key);
            }
            catch (Exception e)
            {
                if (root != null && root != _moduleRoot) ResManager.ReleaseInstance(root);
                GameLog.Error("Task", "TaskView open exception: {0}", e.Message);
            }
            finally
            {
                _loading = false;
            }
        }

        internal static void Reset()
        {
            _view?.Dispose();
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _view = null;
            _loading = false;
        }

        private sealed class TaskViewRuntime
        {
            private static readonly Regex FontColorStart =
                new Regex("<font\\s+color=['\"]?(#[0-9a-fA-F]{6})['\"]?>", RegexOptions.IgnoreCase);

            private readonly TaskViewBind _bind;
            private readonly List<GameObject> _rows = new List<GameObject>();
            private bool _bound;
            private bool _subscribed;
            private int _expandedTaskId;
            private GameObject _equipmentTemplate;

            public bool IsAlive => _bind != null;
            public bool IsShown => _bind != null && _bind.IsShown;

            public TaskViewRuntime(TaskViewBind bind, GameObject moduleRoot)
            {
                _bind = bind;
                TaskFinishViewBind finish = moduleRoot != null
                    ? moduleRoot.GetComponentInChildren<TaskFinishViewBind>(true)
                    : null;
                _equipmentTemplate = finish != null ? finish._tpl_EquipmentItem : null;
            }

            public void Show()
            {
                if (_bind == null) return;
                _bind.Show();
                BindOnce();
                Subscribe();
                Refresh();
            }

            public void Hide()
            {
                Unsubscribe();
                _bind?.Hide();
            }

            public void Dispose()
            {
                Unsubscribe();
                ClearRows();
            }

            private void BindOnce()
            {
                if (_bound || _bind == null) return;
                _bound = true;
                if (_bind.closeBtn != null) UIUtil.AddClick(_bind.closeBtn, Close);
                if (_bind._tpl_TaskBarItem != null) _bind._tpl_TaskBarItem.SetActive(false);
                if (_bind._tpl_TaskContentSubView != null) _bind._tpl_TaskContentSubView.SetActive(false);

                if (_equipmentTemplate != null) _equipmentTemplate.SetActive(false);
                HideScrollbars(_bind.scroll);
            }

            private void Subscribe()
            {
                if (_subscribed) return;
                EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, Refresh);
                EventDispatcher.On<int>(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskUpdated);
                _subscribed = true;
            }

            private void Unsubscribe()
            {
                if (!_subscribed) return;
                EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, Refresh);
                EventDispatcher.Off<int>(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskUpdated);
                _subscribed = false;
            }

            private void OnTaskUpdated(int taskId) => Refresh();

            private void Refresh()
            {
                if (_bind == null || !_bind.IsShown) return;
                ClearRows();
                if (_bind._tpl_TaskBarItem == null || _bind.Content == null) return;

                List<TaskModel.TaskEntry> entries = TaskModel.Instance.GetTaskListForTaskView();
                if (entries.Count == 0)
                {
                    _expandedTaskId = 0;
                    return;
                }
                bool expandedStillExists = false;
                for (int i = 0; i < entries.Count; i++)
                    if (entries[i].TaskId == _expandedTaskId) expandedStillExists = true;
                if (!expandedStillExists) _expandedTaskId = entries[0].TaskId;

                for (int i = 0; i < entries.Count; i++) BuildRow(entries[i]);
                if (_bind.scroll != null && _bind.scroll.content != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_bind.scroll.content);
            }

            private void BuildRow(TaskModel.TaskEntry entry)
            {
                GameObject rowGo = UnityEngine.Object.Instantiate(_bind._tpl_TaskBarItem, _bind.Content);
                rowGo.SetActive(true);
                TaskBarItemBind row = rowGo.GetComponent<TaskBarItemBind>();
                if (row == null)
                {
                    GameLog.Warn("Task", "TaskBarItem template missing TaskBarItemBind");
                    UnityEngine.Object.Destroy(rowGo);
                    return;
                }
                row.Show();
                TaskVo task = TaskModel.Instance.FindUnFinishTask(entry.TipsList);
                if (task == null)
                {
                    UnityEngine.Object.Destroy(rowGo);
                    return;
                }

                if (row.tab_txt != null) row.tab_txt.text = BuildTaskTitle(task);
                bool expanded = entry.TaskId == _expandedTaskId;
                if (row.arrow != null) row.arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, expanded ? 0f : 180f);
                if (row.red_dot != null) row.red_dot.gameObject.SetActive(false);
                if (row.subCon != null) row.subCon.gameObject.SetActive(expanded);
                if (row.tab != null)
                {
                    int taskId = entry.TaskId;
                    UIUtil.AddClick(row.tab, () => ToggleRow(taskId));
                }

                if (expanded && row.subCon != null) BuildContent(row.subCon, task);
                _rows.Add(rowGo);
            }

            private void ToggleRow(int taskId)
            {
                _expandedTaskId = _expandedTaskId == taskId ? 0 : taskId;
                Refresh();
            }

            private void BuildContent(RectTransform parent, TaskVo task)
            {
                if (_bind._tpl_TaskContentSubView == null) return;
                GameObject contentGo = UnityEngine.Object.Instantiate(_bind._tpl_TaskContentSubView, parent);
                contentGo.SetActive(true);
                TaskContentSubViewBind content = contentGo.GetComponent<TaskContentSubViewBind>();
                if (content == null)
                {
                    GameLog.Warn("Task", "TaskContentSubView template missing TaskContentSubViewBind");
                    UnityEngine.Object.Destroy(contentGo);
                    return;
                }
                content.Show();
                if (content.task_name != null) content.task_name.text = task.TaskName ?? string.Empty;
                if (content.content != null) content.content.text = (task.Desc ?? string.Empty).Trim();

                bool finish = TaskModel.Instance.IsAllStepFinish(task.TaskId);
                string target = TaskModel.Instance.BuildMainUITips(task);
                if (content.task_target2 != null)
                {
                    content.task_target2.richText = true;
                    content.task_target2.text = ToTmpRichText(target.Trim());
                }
                if (content.task_target != null) content.task_target.gameObject.SetActive(false);
                if (content.finishBtn != null)
                {
                    content.finishBtn.gameObject.SetActive(finish);
                    UIUtil.AddClick(content.finishBtn, () => SubmitOrGo(task));
                }
                if (content.goBtn != null)
                {
                    content.goBtn.gameObject.SetActive(!finish);
                    UIUtil.AddClick(content.goBtn, () => Go(task));
                }
                HideScrollbars(content._Scroller1);
                BuildRewards(content, task);
            }

            private void BuildRewards(TaskContentSubViewBind content, TaskVo task)
            {
                List<TaskReward.Entry> rewards = TaskReward.Build(task.SpecialGoodsList, task.AwardList, RoleModel.Instance.Career);
                if (content.reward_con != null) content.reward_con.gameObject.SetActive(rewards.Count > 0);
                if (content.Content == null || _equipmentTemplate == null) return;

                for (int i = 0; i < rewards.Count; i++)
                {
                    GameObject cellGo = UnityEngine.Object.Instantiate(_equipmentTemplate, content.Content);
                    cellGo.SetActive(true);
                    EquipmentItem cell = cellGo.GetComponent<EquipmentItem>();
                    if (cell == null)
                    {
                        UnityEngine.Object.Destroy(cellGo);
                        continue;
                    }
                    cell.Show();
                    cell.SetScale(0.6f);
                    cell.SetData(rewards[i].TypeId, rewards[i].Count);
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(content.Content);
            }

            private static void SubmitOrGo(TaskVo task)
            {
                if (task == null || !TaskModel.Instance.IsAllStepFinish(task.TaskId)) return;
                TaskModel.Instance.NowSelectTaskId = task.TaskId;
                EventDispatcher.Emit(GlobalEvent.EVT_TASK_SELECT_CHANGED, task.TaskId);
                if (task.TaskType == TaskModel.REINCARNATION || TaskModel.IsFindNpcTaskType(task.TaskTipsType))
                {
                    TaskModel.Instance.DoTask(task);
                    Close();
                    return;
                }
                TaskController.Instance.SubmitFinish(task.TaskId);
            }

            private static void Go(TaskVo task)
            {
                if (task == null) return;
                TaskModel.Instance.DoTask(task);
                Close();
            }

            private void ClearRows()
            {
                for (int i = 0; i < _rows.Count; i++)
                    if (_rows[i] != null) UnityEngine.Object.Destroy(_rows[i]);
                _rows.Clear();
            }

            private static string BuildTaskTitle(TaskVo task)
            {
                string title = "[" + TaskModel.Instance.GetTaskTagName(task.TaskType) + "]" + (task.TaskName ?? string.Empty);
                if (task.TaskType == TaskModel.MAIN_LINE && task.TaskId <= 100640 && task.MainLineOrder > 0)
                    title += task.MainLineOrder;
                return title;
            }

            private static void HideScrollbars(ScrollRect scroll)
            {
                if (scroll == null) return;
                if (scroll.horizontalScrollbar != null) scroll.horizontalScrollbar.gameObject.SetActive(false);
                if (scroll.verticalScrollbar != null) scroll.verticalScrollbar.gameObject.SetActive(false);
            }

            private static string ToTmpRichText(string text)
            {
                if (string.IsNullOrEmpty(text)) return string.Empty;
                return FontColorStart.Replace(text, "<color=$1>").Replace("</font>", "</color>");
            }
        }
    }
}
