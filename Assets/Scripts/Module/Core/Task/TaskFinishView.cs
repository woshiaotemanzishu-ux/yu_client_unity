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
using UnityEngine.UI;

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
        private Graphic _moduleClickSurface;
        private Graphic _viewClickSurface;
        private readonly List<GameObject> _rewardCells = new List<GameObject>();
        private CancellationTokenSource _timerCts;
        private TaskVo _task;
        private int _openEpoch;
        private int _rewardEpoch;
        private int _closeTime;
        private bool _submitSent;

        public void Open(TaskVo task)
        {
            if (task == null) return;

            // 幂等重开:同一任务且弹层已可见 → 不重开(自动任务 tick 会周期性重进 DoTask→DoFinishTask,
            // 每次重开都会 Render 重建奖励格并 StartTime 重置 10s 倒计时 → 自动提交永远到不了点,
            // 弹层反复闪。实测日志:任务 100060 循环重开数十分钟无一次 30004。已开着就让倒计时跑完。
            if (_task != null && _task.TaskId == task.TaskId && IsShowing())
            {
                return;
            }

            _submitSent = false;
            _task = task;
            _ = OpenAsync(++_openEpoch);
        }

        /// <summary>弹层当前真实可见(引用活着且激活;Unity 销毁对象比较 == null 为 true,天然覆盖被外因销毁的情况)。</summary>
        private bool IsShowing()
        {
            return _moduleRoot != null && _bind != null
                && _moduleRoot.activeSelf && _bind.gameObject.activeSelf;
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
            // fire-and-forget 入口必须自兜异常:此前 SetActive/Render 抛错被静默吞掉,表现为"完成弹层
            // 再也不弹、无任何报错"(实测 100060 卡死循环),必须落日志暴露根因。
            try
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
            catch (System.Exception ex)
            {
                GameLog.Error("Task", "TaskFinishView open failed: task={0} err={1}\n{2}",
                    _task?.TaskId ?? 0, ex.Message, ex.StackTrace);
            }
        }

        private async Task<bool> EnsureLoaded()
        {
            if (_bind != null && _moduleRoot != null) return true;

            // 加载过但引用已失效(场景切换等外因把 TaskModule 销毁,Unity fake-null)→ 丢弃缓存结果重载;
            // 加载失败的缓存(false)同样借此路径重试。仅在旧任务已完结时重置,在途加载照常等待复用。
            if (_loadTask != null && _loadTask.IsCompleted)
            {
                _loadTask = null;
                _moduleRoot = null;
                _bind = null;
                ClearRewardCells();
            }

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
                ResManager.ReleaseInstance(_moduleRoot);
                _moduleRoot = null;
                return false;
            }

            if (_bind._tpl_EquipmentItem != null) _bind._tpl_EquipmentItem.SetActive(false);
            ConfigureUniversalSubmitSurface();
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

            GameLog.Info("Task", "TaskFinishView reward: task={0} count={1} {2}",
                _task.TaskId, rewards.Count, TaskReward.ToText(rewards, " / "));
        }

        /// <summary>
        /// 完成弹层的任何点击都代表领取/提交。Module 根点击面覆盖屏外遮罩，View 根点击面覆盖面板；
        /// 子 Graphic 关闭射线，保证关闭图标、文字、奖励区不会把语义点击截走。
        /// </summary>
        private void ConfigureUniversalSubmitSurface()
        {
            if (_moduleRoot == null || _bind == null) return;

            _moduleClickSurface = EnsureRootClickSurface(_moduleRoot);
            UIUtil.AddClick(_moduleClickSurface, OnSubmit);
            _viewClickSurface = EnsureRootClickSurface(_bind.gameObject);
            UIUtil.AddClick(_viewClickSurface, OnSubmit);

            foreach (Graphic graphic in _moduleRoot.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != null && graphic != _moduleClickSurface && graphic != _viewClickSurface)
                    graphic.raycastTarget = false;
            }
        }

        private static Graphic EnsureRootClickSurface(GameObject root)
        {
            Graphic graphic = root != null ? root.GetComponent<Graphic>() : null;
            if (graphic != null) return graphic;

            Image image = root.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            return image;
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
                foreach (Graphic graphic in cellGo.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
                _rewardCells.Add(cellGo);

                await Task.Yield();
                if (epoch != _rewardEpoch) return;
            }
        }

        private void OnSubmit()
        {
            if (_submitSent) return;
            if (_task == null)
            {
                CancelTimer();
                Close();
                return;
            }

            // 老端 TaskFinishView 点击提交前会再次检查 IsAllStepFinish。弹层打开后若 30001
            // 已把任务推进/替换，旧弹层不能继续提交一个已失效任务号。
            if (!TaskModel.Instance.IsAllStepFinish(_task.TaskId)) return;

            CancelTimer();
            _submitSent = true;
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
                    try { await Shenxiao.Framework.Util.TimeUtil.Delay(1000, token); }
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
