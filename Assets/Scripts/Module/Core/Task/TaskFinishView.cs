using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Tasks
{
    /// <summary>
    /// Temporary runtime shell for task completion. Static UI should be replaced by
    /// the converted task/TaskFinishView prefab when the task module is generated.
    /// </summary>
    public sealed class TaskFinishView
    {
        private const int AutoSeconds = 10;

        private GameObject _root;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _targetText;
        private TextMeshProUGUI _rewardText;
        private TextMeshProUGUI _submitLabel;
        private TextMeshProUGUI _countdownText;
        private RectTransform _rewardRow;
        private GameObject _submitBtn;
        private readonly List<GameObject> _rewardCells = new List<GameObject>();
        private int _rewardEpoch;
        private CancellationTokenSource _timerCts;
        private int _closeTime;
        private TaskVo _task;

        private static TMP_FontAsset _font;
        private static Material _fontMat;

        public void Open(TaskVo task)
        {
            if (task == null) return;
            _task = task;
            EnsureBuilt();
            if (_root == null) return;

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Render();
            StartTime();
            GameLog.Info("Task", "TaskFinishView opened: task={0} name={1}", task.TaskId, task.TaskName);
        }

        public void Close()
        {
            CancelTimer();
            if (_root != null) _root.SetActive(false);
            _rewardEpoch++;
            for (int i = 0; i < _rewardCells.Count; i++)
            {
                if (_rewardCells[i] != null) ResManager.ReleaseInstance(_rewardCells[i]);
            }
            _rewardCells.Clear();
            GameLog.Info("Task", "TaskFinishView closed");
        }

        private void Render()
        {
            if (_task == null) return;

            _titleText.text = string.IsNullOrEmpty(_task.TaskName) ? "任务完成" : _task.TaskName;

            string target = string.IsNullOrEmpty(_task.TaskTipsMsg) ? _task.Tips : _task.TaskTipsMsg;
            string desc = string.IsNullOrEmpty(_task.Desc) ? "" : "\n" + _task.Desc;
            _targetText.text = (target + " <color=#0a953e>(完成)</color>").Trim() + desc;

            List<TaskReward.Entry> rewards = TaskReward.Build(_task.SpecialGoodsList, _task.AwardList, RoleModel.Instance.Career);
            bool hasReward = rewards.Count > 0;
            _ = BuildRewardCells(rewards);
            _submitLabel.text = hasReward ? "领取奖励" : "提交任务";

            GameLog.Info("Task", "TaskFinishView reward: task={0} count={1} {2}",
                _task.TaskId, rewards.Count, TaskReward.ToText(rewards, " / "));
        }

        private async Task BuildRewardCells(List<TaskReward.Entry> rewards)
        {
            int epoch = ++_rewardEpoch;
            for (int i = 0; i < _rewardCells.Count; i++)
            {
                if (_rewardCells[i] != null) ResManager.ReleaseInstance(_rewardCells[i]);
            }
            _rewardCells.Clear();
            if (_rewardRow == null) return;

            var goods = new List<TaskReward.Entry>();
            for (int i = 0; i < rewards.Count; i++)
            {
                if (!string.IsNullOrEmpty(GoodsModel.GetGoodsIcon(rewards[i].TypeId)))
                    goods.Add(rewards[i]);
            }

            _rewardText.text = rewards.Count > 0 ? "奖励:" + TaskReward.ToText(rewards, "   ") : "";
            if (goods.Count == 0) return;

            const float baseSize = 130f;
            const float gap = 12f;
            const float rowW = 600f;
            float rawTotal = goods.Count * baseSize + (goods.Count - 1) * gap;
            float scale = rawTotal > rowW ? rowW / rawTotal : 1f;
            float cellW = baseSize * scale;
            float gapS = gap * scale;
            float totalW = goods.Count * cellW + (goods.Count - 1) * gapS;
            float leftStart = -totalW / 2f;

            for (int i = 0; i < goods.Count; i++)
            {
                GameObject cellGo = await ResManager.InstantiateAsync(
                    GameResPath.GetUIPrefab("common", "BaseAwardItem"), _rewardRow);
                if (epoch != _rewardEpoch)
                {
                    if (cellGo != null) ResManager.ReleaseInstance(cellGo);
                    return;
                }
                if (cellGo == null)
                {
                    GameLog.Warn("Task", "TaskFinishView BaseAwardItem instantiate failed: typeId={0}", goods[i].TypeId);
                    continue;
                }

                BaseAwardItem cell = cellGo.GetComponent<BaseAwardItem>();
                if (cell == null)
                {
                    GameLog.Warn("Task", "BaseAwardItem prefab missing BaseAwardItem component. Run common bind backfill.");
                    ResManager.ReleaseInstance(cellGo);
                    continue;
                }

                cellGo.SetActive(true);
                RectTransform rt = (RectTransform)cellGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.localScale = Vector3.one * scale;
                rt.anchoredPosition = new Vector2(leftStart + i * (cellW + gapS), cellW / 2f);
                cell.SetData(goods[i].TypeId, goods[i].Count);
                _rewardCells.Add(cellGo);
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
                    if (_countdownText != null) _countdownText.text = _closeTime + "s后自动提交任务";
                    try { await Task.Delay(1000, token); }
                    catch (TaskCanceledException) { return; }
                    continue;
                }

                if (_countdownText != null) _countdownText.text = "";
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
            if (_countdownText != null) _countdownText.text = "";
        }

        private void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Popup);
            if (parent == null)
            {
                GameLog.Error("Task", "TaskFinishView cannot build: Popup layer missing");
                return;
            }

            _root = NewRect("TaskFinishView(TempShell)", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.55f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(640f, 560f);
            panelRt.anchoredPosition = Vector2.zero;
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.96f);

            _titleText = NewText("Title", panel.transform, 34, TextAlignmentOptions.Top);
            RectTransform titleRt = _titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -24f);
            titleRt.sizeDelta = new Vector2(-48f, 48f);
            _titleText.color = new Color(1f, 0.86f, 0.45f);
            _titleText.fontStyle = FontStyles.Bold;

            _targetText = NewText("Target", panel.transform, 24, TextAlignmentOptions.TopLeft);
            RectTransform tgtRt = _targetText.rectTransform;
            tgtRt.anchorMin = new Vector2(0f, 1f);
            tgtRt.anchorMax = new Vector2(1f, 1f);
            tgtRt.pivot = new Vector2(0f, 1f);
            tgtRt.anchoredPosition = new Vector2(28f, -92f);
            tgtRt.sizeDelta = new Vector2(-56f, 180f);
            _targetText.textWrappingMode = TextWrappingModes.Normal;
            _targetText.color = Color.white;

            GameObject rowGo = NewRect("RewardRow", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _rewardRow = (RectTransform)rowGo.transform;
            _rewardRow.pivot = new Vector2(0.5f, 0.5f);
            _rewardRow.sizeDelta = new Vector2(600f, 156f);
            _rewardRow.anchoredPosition = new Vector2(0f, -40f);

            _rewardText = NewText("Reward", panel.transform, 24, TextAlignmentOptions.TopLeft);
            RectTransform rwRt = _rewardText.rectTransform;
            rwRt.anchorMin = new Vector2(0f, 0f);
            rwRt.anchorMax = new Vector2(1f, 0f);
            rwRt.pivot = new Vector2(0f, 0f);
            rwRt.anchoredPosition = new Vector2(28f, 130f);
            rwRt.sizeDelta = new Vector2(-56f, 200f);
            _rewardText.textWrappingMode = TextWrappingModes.Normal;
            _rewardText.color = new Color(0.9f, 0.95f, 1f);

            _submitBtn = NewRect("Submit", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            RectTransform subRt = (RectTransform)_submitBtn.transform;
            subRt.pivot = new Vector2(0.5f, 0f);
            subRt.sizeDelta = new Vector2(260f, 72f);
            subRt.anchoredPosition = new Vector2(0f, 30f);
            Image subImg = _submitBtn.AddComponent<Image>();
            subImg.color = new Color(0.20f, 0.30f, 0.48f, 1f);
            _submitLabel = NewText("Label", _submitBtn.transform, 28, TextAlignmentOptions.Center);
            RectTransform slRt = _submitLabel.rectTransform;
            slRt.anchorMin = Vector2.zero;
            slRt.anchorMax = Vector2.one;
            slRt.offsetMin = Vector2.zero;
            slRt.offsetMax = Vector2.zero;
            _submitLabel.color = Color.white;
            UIUtil.AddClick(subImg, OnSubmit);

            _countdownText = NewText("Countdown", panel.transform, 20, TextAlignmentOptions.Center);
            RectTransform ctRt = _countdownText.rectTransform;
            ctRt.anchorMin = new Vector2(0.5f, 0f);
            ctRt.anchorMax = new Vector2(0.5f, 0f);
            ctRt.pivot = new Vector2(0.5f, 0f);
            ctRt.sizeDelta = new Vector2(360f, 30f);
            ctRt.anchoredPosition = new Vector2(0f, 108f);
            _countdownText.color = new Color(0.04f, 0.58f, 0.24f);

            GameObject close = NewRect("Close", panel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            RectTransform closeRt = (RectTransform)close.transform;
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.sizeDelta = new Vector2(56f, 56f);
            closeRt.anchoredPosition = new Vector2(-8f, -8f);
            Image closeImg = close.AddComponent<Image>();
            closeImg.color = new Color(0.5f, 0.18f, 0.18f, 1f);
            TextMeshProUGUI closeLbl = NewText("X", close.transform, 30, TextAlignmentOptions.Center);
            RectTransform clRt = closeLbl.rectTransform;
            clRt.anchorMin = Vector2.zero;
            clRt.anchorMax = Vector2.one;
            clRt.offsetMin = Vector2.zero;
            clRt.offsetMax = Vector2.zero;
            closeLbl.text = "X";
            closeLbl.color = Color.white;
            UIUtil.AddClick(closeImg, Close);
        }

        private static GameObject NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
            return go;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = align;
            t.richText = true;
            ApplyFont(t);
            return t;
        }

        private static void ApplyFont(TextMeshProUGUI t)
        {
            if (_font == null)
            {
                TextMeshProUGUI src = UnityEngine.Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (src != null)
                {
                    _font = src.font;
                    _fontMat = src.fontSharedMaterial;
                }
            }
            if (_font != null) t.font = _font;
            if (_fontMat != null) t.fontSharedMaterial = _fontMat;
        }
    }
}
