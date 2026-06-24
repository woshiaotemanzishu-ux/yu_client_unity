using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    public sealed class MainUITaskItem : MainUITaskItemBind
    {
        private const string TASK_FINISH_EFFECT_SLOT = "main_ui_task_finish_frame";
        private static readonly Regex FontColorStart = new Regex("<font\\s+color=['\"]?(#[0-9a-fA-F]{6})['\"]?>", RegexOptions.IgnoreCase);

        private readonly List<TextMeshProUGUI> _tipLabels = new List<TextMeshProUGUI>();
        private TaskModel.TaskEntry _entry;
        private UIEffectStage.Handle _finishEffect;
        private bool _finishEffectLoading;
        private int _finishEffectVersion;

        protected override void OnInit()
        {
            _img_done.gameObject.SetActive(false);
            _img_select.gameObject.SetActive(false);
            _box_finger_con.gameObject.SetActive(false);
            _box_effect.gameObject.SetActive(false);
            lblTaskTitle2.text = "";
            UIUtil.AddClick(_img_bg, OnClick);
        }

        protected override void OnHide()
        {
            HideMainLineArrow();
        }

        protected override void OnDispose()
        {
            HideMainLineArrow();
            ClearFinishEffect();
        }

        private void OnClick()
        {
            if (_entry == null) return;
            TaskVo task = TaskModel.Instance.FindUnFinishTask(_entry.TipsList);
            if (task == null) return;
            HideMainLineArrow();
            TaskModel.Instance.DoTask(task);
        }

        public void SetData(TaskModel.TaskEntry entry)
        {
            _entry = entry;
            ClearTipLabels();
            if (_entry == null)
            {
                HideMainLineArrow();
                lblTaskTitle.text = "";
                lblTaskTitle2.text = "";
                _img_done.gameObject.SetActive(false);
                _img_select.gameObject.SetActive(false);
                if (_box_effect != null) _box_effect.gameObject.SetActive(false);
                ClearFinishEffect();
                return;
            }

            TaskVo task = TaskModel.Instance.FindUnFinishTask(_entry.TipsList);
            if (task == null) return;

            SetTitle(task);
            SetTips(task);
            bool finish = TaskModel.Instance.IsAllStepFinish(task.TaskId);
            _img_done.gameObject.SetActive(finish);
            _img_select.gameObject.SetActive(task.TaskId == TaskModel.Instance.NowSelectTaskId);
            UpdateFinishEffect(finish);
        }

        public void ShowMainLineArrow()
        {
            if (!TryBuildGuideData(out ArrowData data))
            {
                HideMainLineArrow();
                return;
            }

            _box_finger_con.gameObject.SetActive(true);
            MainUIGuideManager.Instance.ShowMainUiFinger(this, _box_finger_con, data, OnClick, isTaskItem: true);
        }

        public void HideMainLineArrow()
        {
            if (_box_finger_con != null) _box_finger_con.gameObject.SetActive(false);
            MainUIGuideManager.Instance.HideMainUiFinger(this);
        }

        private bool TryBuildGuideData(out ArrowData data)
        {
            data = null;
            if (_entry == null || _box_finger_con == null) return false;

            TaskVo task = TaskModel.Instance.FindUnFinishTask(_entry.TipsList);
            if (task == null) return false;
            if (TaskModel.Instance.MainLineTaskVo == null || task.TaskId != TaskModel.Instance.MainLineTaskVo.TaskId) return false;
            if (!TaskModel.Instance.MainLineTaskNeedShowArrow()) return false;
            TaskModel.TaskGuideStep step = TaskModel.Instance.GetNowGuideCfg(true, task);
            if (step == null) return false;
            if (step.NotShowInTaskItem) return false;

            data = new ArrowData
            {
                Content = step.Text,
                Direction = step.Direction,
                CloseTime = step.CloseTime,
                AutoCountdown = step.AutoCountdown,
                NotEffect = step.NotEffect,
                SelectEffectScale = new Vector3(step.EffectScaleX, step.EffectScaleY, step.EffectScaleZ),
                FingerEffectOffset = new Vector2(step.FingerOffsetX, step.FingerOffsetY),
                Offset = new Vector2(step.OffsetX, step.OffsetY),
                Target = _box_finger_con,
            };
            return true;
        }

        private void UpdateFinishEffect(bool finish)
        {
            if (_box_effect == null) return;
            _finishEffectVersion++;
            _box_effect.gameObject.SetActive(finish);
            if (!finish)
            {
                ClearFinishEffect();
                return;
            }
            if (_finishEffect != null || _finishEffectLoading) return;
            _ = LoadFinishEffectAsync(_finishEffectVersion);
        }

        private async Task LoadFinishEffectAsync(int version)
        {
            if (_box_effect == null || _finishEffectLoading || _finishEffect != null) return;
            _finishEffectLoading = true;

            float boxHeight = Mathf.Max(1f, _box_effect.rect.height);
            float itemHeight = _img_bg != null ? Mathf.Max(1f, _img_bg.rectTransform.rect.height) : 54f;
            float scale = 1280f / boxHeight * 0.96f;
            float yScale = scale * itemHeight / 54f;
            UIEffectSlot slot = FindFinishEffectSlot();
            if (slot == null)
            {
                _finishEffectLoading = false;
                GameLog.Warn("MainUI", "Task finish effect slot missing: {0}", TASK_FINISH_EFFECT_SLOT);
                return;
            }

            Vector3 slotScale = slot.Scale;
            UIEffectStage.Handle effect = await UIEffectStage.AddByKeyAsync(slot.EffectName, slot.AddressKey,
                _box_effect, slot.Position, new Vector3(slotScale.x * scale, slotScale.y * yScale, slotScale.z * scale),
                slot.RotationY);
            _finishEffectLoading = false;
            if (version != _finishEffectVersion || _box_effect == null || !_box_effect.gameObject.activeSelf)
            {
                effect?.Dispose();
                return;
            }
            _finishEffect = effect;
        }

        private void ClearFinishEffect()
        {
            _finishEffectVersion++;
            if (_finishEffect != null)
            {
                _finishEffect.Dispose();
                _finishEffect = null;
            }
            _finishEffectLoading = false;
        }

        private UIEffectSlot FindFinishEffectSlot()
        {
            if (_box_effect == null) return null;
            UIEffectSlot[] slots = _box_effect.GetComponentsInChildren<UIEffectSlot>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].SlotId == TASK_FINISH_EFFECT_SLOT) return slots[i];
            }
            return null;
        }

        private void SetTitle(TaskVo task)
        {
            string tagName = TaskModel.Instance.GetTaskTagName(task.TaskType);
            string title = string.IsNullOrEmpty(task.TaskName) ? task.TaskId.ToString() : task.TaskName;
            lblTaskTitle.text = "[" + tagName + "] " + title;
            if (ColorUtility.TryParseHtmlString(TaskModel.Instance.GetTaskColor(task.TaskType), out Color color))
            {
                lblTaskTitle.color = color;
            }

            string subTitle = "";
            if (task.TaskType == TaskModel.MAIN_LINE && task.TaskId <= 101410 && task.MainLineOrder > 0)
            {
                subTitle = task.MainLineOrder.ToString();
            }
            lblTaskTitle2.text = subTitle;
        }

        private void SetTips(TaskVo task)
        {
            TextMeshProUGUI tips = CreateTipLabel();
            tips.text = ToTmpRichText(TaskModel.Instance.BuildMainUITips(task));
            tips.gameObject.SetActive(true);

            RectTransform rt = tips.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(6f, -32f);
            rt.sizeDelta = new Vector2(210f, 44f);
        }

        private TextMeshProUGUI CreateTipLabel()
        {
            var go = new GameObject("MainUITaskItemTaskDesc__");
            go.transform.SetParent(transform, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = lblTaskTitle.font;
            text.fontSharedMaterial = lblTaskTitle.fontSharedMaterial;
            text.fontSize = 18f;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.richText = true;
            _tipLabels.Add(text);
            return text;
        }

        private void ClearTipLabels()
        {
            for (int i = 0; i < _tipLabels.Count; i++)
            {
                if (_tipLabels[i] != null) Destroy(_tipLabels[i].gameObject);
            }
            _tipLabels.Clear();
        }

        private static string ToTmpRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string result = FontColorStart.Replace(text, "<color=$1>");
            result = result.Replace("</font>", "</color>");
            return result;
        }
    }
}
