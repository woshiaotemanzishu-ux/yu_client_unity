using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    public sealed class MainUITaskItem : MainUITaskItemBind
    {
        private static readonly Regex FontColorStart = new Regex("<font\\s+color=['\"]?(#[0-9a-fA-F]{6})['\"]?>", RegexOptions.IgnoreCase);

        private readonly List<TextMeshProUGUI> _tipLabels = new List<TextMeshProUGUI>();
        private TaskModel.TaskEntry _entry;

        protected override void OnInit()
        {
            _img_done.gameObject.SetActive(false);
            _img_select.gameObject.SetActive(false);
            _box_finger_con.gameObject.SetActive(false);
            _box_effect.gameObject.SetActive(false);
            lblTaskTitle2.text = "";
        }

        public void SetData(TaskModel.TaskEntry entry)
        {
            _entry = entry;
            ClearTipLabels();
            if (_entry == null)
            {
                lblTaskTitle.text = "";
                lblTaskTitle2.text = "";
                _img_done.gameObject.SetActive(false);
                _img_select.gameObject.SetActive(false);
                return;
            }

            TaskVo task = TaskModel.Instance.FindUnFinishTask(_entry.TipsList);
            if (task == null) return;

            SetTitle(task);
            SetTips(task);
            _img_done.gameObject.SetActive(TaskModel.Instance.IsAllStepFinish(task.TaskId));
            _img_select.gameObject.SetActive(task.TaskId == TaskModel.Instance.NowSelectTaskId);
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
