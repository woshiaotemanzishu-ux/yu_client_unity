using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
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
        private static readonly Regex FontColorStart = new Regex("<font\\s+color=['\"]?(#[0-9a-fA-F]{6})['\"]?>", RegexOptions.IgnoreCase);

        private readonly List<TextMeshProUGUI> _tipLabels = new List<TextMeshProUGUI>();
        private TaskModel.TaskEntry _entry;
        private ArrowComponent _guideArrow;
        private bool _guideLoading;

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

        private void OnClick()
        {
            if (_entry == null) return;
            HideMainLineArrow();
            TaskVo task = TaskModel.Instance.FindUnFinishTask(_entry.TipsList);
            if (task == null) return;
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
                return;
            }

            TaskVo task = TaskModel.Instance.FindUnFinishTask(_entry.TipsList);
            if (task == null) return;

            SetTitle(task);
            SetTips(task);
            _img_done.gameObject.SetActive(TaskModel.Instance.IsAllStepFinish(task.TaskId));
            _img_select.gameObject.SetActive(task.TaskId == TaskModel.Instance.NowSelectTaskId);
        }

        public void ShowMainLineArrow()
        {
            if (_entry == null || _box_finger_con == null) return;
            _box_finger_con.gameObject.SetActive(true);
            _ = ShowMainLineArrowAsync();
        }

        public void HideMainLineArrow()
        {
            if (_box_finger_con != null) _box_finger_con.gameObject.SetActive(false);
            if (_guideArrow != null) _guideArrow.Hide();
        }

        private async Task ShowMainLineArrowAsync()
        {
            if (_guideArrow == null)
            {
                if (_guideLoading) return;
                _guideLoading = true;
                GameObject go = await ResManager.InstantiateAsync(
                    GameResPath.GetUIPrefab("mainUI", "ArrowComponent"), _box_finger_con);
                _guideLoading = false;
                if (go == null) return;

                _guideArrow = go.GetComponent<ArrowComponent>();
                if (_guideArrow == null)
                {
                    GameLog.Warn("MainUI", "ArrowComponent prefab missing business component. Run mainUI convert + bind backfill.");
                    ResManager.ReleaseInstance(go);
                    return;
                }
            }

            if (_entry == null || _box_finger_con == null) return;
            TaskVo task = TaskModel.Instance.FindUnFinishTask(_entry.TipsList);
            _guideArrow.Show();
            _guideArrow.SetData(new ArrowData
            {
                Content = BuildGuideText(task),
                Direction = ArrowComponent.DIR_LEFT,
                CloseTime = 10,
                Target = _box_finger_con,
            }, OnClick);
        }

        private static string BuildGuideText(TaskVo task)
        {
            if (task == null) return "点击此处完成任务吧";
            if (TaskModel.Instance.IsAllStepFinish(task.TaskId)) return "点击此处完成任务吧";
            if (task.TaskId == TaskModel.FIRST_TASK_ID) return "点击此处完成任务吧";
            return "继续推进<color=#0a9f42>主线</color>";
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
