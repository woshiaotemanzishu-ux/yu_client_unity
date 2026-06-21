using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Tasks;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Task/team HUD view. The static initial state mirrors old
    /// MainUITaskTeamView.LoadSuccess + SwitchView(Task); task rows are driven
    /// by the real TaskController 30000 chain.
    /// </summary>
    public sealed class MainUITaskTeamView : MainUITaskTeamViewBind
    {
        private static readonly Color TaskTabLightColor = ParseColor("#FFF7D6");
        private static readonly Color TaskTabDarkColor = ParseColor("#6CFFD3");

        private readonly List<MainUITaskItem> _taskItems = new List<MainUITaskItem>();

        protected override void OnInit()
        {
            _box_task.gameObject.SetActive(true);
            _box_team.gameObject.SetActive(false);
            _box_non_team.gameObject.SetActive(false);
            _img_team_red.gameObject.SetActive(false);
            _img_team_role_count_bg.gameObject.SetActive(false);
            _box_main_line.gameObject.SetActive(false);
            _panel_task.gameObject.SetActive(false);
            _lb_task_desc.text = "任务";
            _lb_team_desc.text = "队伍";
            _lb_task_desc_en.gameObject.SetActive(false);
            _lb_team_desc_en.gameObject.SetActive(false);
            _img_awaken_red.gameObject.SetActive(false);
            _box_awaken_effect.gameObject.SetActive(false);
            if (_tpl_MainUITaskItem != null) _tpl_MainUITaskItem.SetActive(false);
            if (_tpl_TeamMainRoleItem != null) _tpl_TeamMainRoleItem.SetActive(false);
            ApplyTaskTabState(true);
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, RefreshTaskItems);
            EventDispatcher.On<int>(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskOneUpdated);
            EventDispatcher.On<int>(GlobalEvent.EVT_TASK_SELECT_CHANGED, OnTaskSelectChanged);
            RefreshTaskItems();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, RefreshTaskItems);
            EventDispatcher.Off<int>(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskOneUpdated);
            EventDispatcher.Off<int>(GlobalEvent.EVT_TASK_SELECT_CHANGED, OnTaskSelectChanged);
        }

        private void OnTaskOneUpdated(int taskId)
        {
            RefreshTaskItems();
        }

        // 选中任务变化(点任务项后 DoTask 广播)→ 重刷任务栏,各项据 NowSelectTaskId 更新 _img_select 选中态。
        private void OnTaskSelectChanged(int taskId)
        {
            RefreshTaskItems();
        }

        private void RefreshTaskItems()
        {
            List<TaskModel.TaskEntry> list = TaskModel.Instance.GetTaskListForMainUI();
            _panel_task.gameObject.SetActive(list.Count > 0);
            _box_main_line.gameObject.SetActive(TaskModel.Instance.MainLineTaskNeedShowArrow());

            Transform parent = _panel_task.content != null ? _panel_task.content : _panel_task.transform;
            for (int i = 0; i < list.Count; i++)
            {
                MainUITaskItem item = GetOrCreateItem(i, parent);
                if (item == null) return;

                item.gameObject.SetActive(true);
                item.Show();
                item.SetData(list[i]);

                RectTransform rt = (RectTransform)item.transform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(0f, -i * 78f);
            }

            for (int i = list.Count; i < _taskItems.Count; i++)
            {
                if (_taskItems[i] == null) continue;
                _taskItems[i].SetData(null);
                _taskItems[i].gameObject.SetActive(false);
            }
        }

        private MainUITaskItem GetOrCreateItem(int index, Transform parent)
        {
            if (index < _taskItems.Count && _taskItems[index] != null) return _taskItems[index];
            if (_tpl_MainUITaskItem == null)
            {
                GameLog.Error("MainUI", "MainUITaskItem template missing");
                return null;
            }

            GameObject go = Instantiate(_tpl_MainUITaskItem, parent);
            go.SetActive(true);
            MainUITaskItem item = go.GetComponent<MainUITaskItem>();
            if (item == null)
            {
                GameLog.Error("MainUI", "MainUITaskItem template is not rebound to business script");
                Destroy(go);
                return null;
            }

            while (_taskItems.Count <= index) _taskItems.Add(null);
            _taskItems[index] = item;
            return item;
        }

        private void ApplyTaskTabState(bool taskSelected)
        {
            _ = ResManager.SetImageAsync(_img_task_bg,
                GameResPath.GetIcon("mainUI", taskSelected ? "mainui_taskbtn_light" : "mainui_taskbtn_normal"),
                nativeSize: false);
            _ = ResManager.SetImageAsync(_img_team_bg,
                GameResPath.GetIcon("mainUI", taskSelected ? "mainui_taskbtn_normal" : "mainui_taskbtn_light"),
                nativeSize: false);

            _lb_task_desc.color = taskSelected ? TaskTabLightColor : TaskTabDarkColor;
            _lb_team_desc.color = taskSelected ? TaskTabDarkColor : TaskTabLightColor;
        }

        private static Color ParseColor(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
        }
    }
}
