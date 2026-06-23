using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Task;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Tasks
{
    /// <summary>
    /// Runtime controller for converted task/TaskFinishView.
    /// Static layout and skins come from TaskModule.prefab; this class only fills task data,
    /// reward cells, countdown, and the submit click.
    /// </summary>
    public sealed class TaskFinishView
    {
        private const int AutoSeconds = 10;
        private static readonly Regex FontColorStart =
            new Regex("<font\\s+color=['\"]?(#[0-9a-fA-F]{6})['\"]?>", RegexOptions.IgnoreCase);

        private GameObject _moduleRoot;
        private TaskFinishViewBind _bind;
        private Task<bool> _loadTask;
        private readonly List<GameObject> _rewardCells = new List<GameObject>();
        private CancellationTokenSource _timerCts;
        private TaskVo _task;
        private int _openEpoch;
        private int _rewardEpoch;
        private int _closeTime;

        public void Open(TaskVo task)
        {
            if (task == null) return;
            _task = task;
            _ = OpenAsync(++_openEpoch);
        }

        public void Close()
        {
            ++_openEpoch;
            CancelTimer();
            ClearRewardCells();
            if (_bind != null) _bind.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
            GameLog.Info("Task", "TaskFinishView closed");
        }

        private async Task OpenAsync(int epoch)
        {
            if (!await EnsureLoaded()) return;
            if (epoch != _openEpoch || _task == null) return;

            _moduleRoot.SetActive(true);
            _bind.Show();
            _bind.transform.SetAsLastSibling();
            Render();
            StartTime();
            GameLog.Info("Task", "TaskFinishView opened: task={0} name={1}", _task.TaskId, _task.TaskName);
        }

        private async Task<bool> EnsureLoaded()
        {
            if (_bind != null && _moduleRoot != null) return true;
            if (_loadTask == null) _loadTask = LoadPrefab();
            return await _loadTask;
        }

        private async Task<bool> LoadPrefab()
        {
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("Task", "TaskFinishView cannot load: Window layer missing");
                return false;
            }

            string key = GameResPath.GetUIPrefab("task", "TaskModule");
            _moduleRoot = await ResManager.InstantiateAsync(key, parent);
            if (_moduleRoot == null)
            {
                GameLog.Error("Task", "TaskModule prefab load failed: {0}", key);
                return false;
            }

            _moduleRoot.name = "TaskModule";
            BaseView[] views = _moduleRoot.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views) v.gameObject.SetActive(false);

            _bind = _moduleRoot.GetComponentInChildren<TaskFinishViewBind>(true);
            if (_bind == null)
            {
                GameLog.Error("Task", "TaskModule missing TaskFinishViewBind. Run task LayaUI convert + bind backfill.");
                return false;
            }

            if (_bind._tpl_EquipmentItem != null) _bind._tpl_EquipmentItem.SetActive(false);
            if (_bind._panel_reward != null)
            {
                if (_bind._panel_reward.verticalScrollbar != null)
                    _bind._panel_reward.verticalScrollbar.gameObject.SetActive(false);
                if (_bind._panel_reward.horizontalScrollbar != null)
                    _bind._panel_reward.horizontalScrollbar.gameObject.SetActive(false);
            }

            _moduleRoot.SetActive(false);
            return true;
        }

        private void Render()
        {
            if (_task == null || _bind == null) return;

            bool finish = TaskModel.Instance.IsAllStepFinish(_task.TaskId);
            if (_bind._box_finish != null) _bind._box_finish.gameObject.SetActive(finish);
            if (_bind._lb_title != null) _bind._lb_title.text = string.IsNullOrEmpty(_task.TaskName) ? "任务完成" : _task.TaskName;

            string target = string.IsNullOrEmpty(_task.TaskTipsMsg) ? _task.Tips : _task.TaskTipsMsg;
            string desc = string.IsNullOrEmpty(_task.Desc) ? "" : _task.Desc.Trim();
            string targetText = finish
                ? target + "<font color='#0a953e'>(完成)</font>"
                : target;
            if (_bind._html_task_target != null)
            {
                _bind._html_task_target.richText = true;
                _bind._html_task_target.text = ToTmpRichText(targetText.Trim());
            }
            if (_bind._lb_content != null) _bind._lb_content.text = desc;

            List<TaskReward.Entry> rewards = TaskReward.Build(_task.SpecialGoodsList, _task.AwardList, RoleModel.Instance.Career);
            bool hasReward = rewards.Count > 0;
            if (_bind._box_reward != null) _bind._box_reward.gameObject.SetActive(hasReward);
            if (_bind._lb_finish != null) _bind._lb_finish.text = hasReward ? "领取奖励" : "提交任务";
            _ = BuildRewardCells(rewards);

            BindClicks();
            GameLog.Info("Task", "TaskFinishView reward: task={0} count={1} {2}",
                _task.TaskId, rewards.Count, TaskReward.ToText(rewards, " / "));
        }

        private void BindClicks()
        {
            if (_bind?._img_finish != null)
            {
                UIUtil.ClearClicks(_bind._img_finish);
                UIUtil.AddClick(_bind._img_finish, OnSubmit);
            }
            if (_bind?._img_close != null)
            {
                UIUtil.ClearClicks(_bind._img_close);
                UIUtil.AddClick(_bind._img_close, Close);
            }
        }

        private async Task BuildRewardCells(IReadOnlyList<TaskReward.Entry> rewards)
        {
            int epoch = ++_rewardEpoch;
            ClearRewardCells(false);
            if (_bind == null || _bind._hbox_reward == null || _bind._tpl_EquipmentItem == null) return;
            if (rewards == null || rewards.Count == 0) return;

            for (int i = 0; i < rewards.Count; i++)
            {
                GameObject cellGo = Object.Instantiate(_bind._tpl_EquipmentItem, _bind._hbox_reward);
                if (epoch != _rewardEpoch)
                {
                    Object.Destroy(cellGo);
                    return;
                }

                cellGo.SetActive(true);
                EquipmentItem cell = cellGo.GetComponent<EquipmentItem>();
                if (cell == null)
                {
                    GameLog.Warn("Task", "EquipmentItem template missing EquipmentItem component. Run task/common bind backfill.");
                    Object.Destroy(cellGo);
                    continue;
                }

                cell.Show();
                RectTransform rt = (RectTransform)cellGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(i * 80f, 0f);
                cell.SetScale(0.6f);
                cell.SetData(rewards[i].TypeId, rewards[i].Count);
                _rewardCells.Add(cellGo);

                await Task.Yield();
                if (epoch != _rewardEpoch) return;
            }
        }

        private void OnSubmit()
        {
            CancelTimer();
            if (_task == null)
            {
                Close();
                return;
            }

            TaskController.Instance.SubmitFinish(_task.TaskId);
            Close();
        }

        private void StartTime()
        {
            CancelTimer();
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;

            _closeTime = AutoSeconds;
            _timerCts = new CancellationTokenSource();
            _ = CountdownLoop(_timerCts.Token);
        }

        private async Task CountdownLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _closeTime--;
                if (_closeTime > 0)
                {
                    if (_bind?._lb_count_down != null) _bind._lb_count_down.text = _closeTime + "s后自动提交任务";
                    try { await Task.Delay(1000, token); }
                    catch (TaskCanceledException) { return; }
                    continue;
                }

                if (_bind?._lb_count_down != null) _bind._lb_count_down.text = "";
                OnSubmit();
                return;
            }
        }

        private void CancelTimer()
        {
            if (_timerCts != null)
            {
                _timerCts.Cancel();
                _timerCts.Dispose();
                _timerCts = null;
            }
            if (_bind?._lb_count_down != null) _bind._lb_count_down.text = "";
        }

        private void ClearRewardCells(bool bumpEpoch = true)
        {
            if (bumpEpoch) _rewardEpoch++;
            for (int i = 0; i < _rewardCells.Count; i++)
            {
                if (_rewardCells[i] != null) Object.Destroy(_rewardCells[i]);
            }
            _rewardCells.Clear();
        }

        private static string ToTmpRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string result = FontColorStart.Replace(text, "<color=$1>");
            return result.Replace("</font>", "</color>");
        }
    }
}
