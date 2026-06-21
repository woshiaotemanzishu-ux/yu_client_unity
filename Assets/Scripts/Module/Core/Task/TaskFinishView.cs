using System.Collections.Generic;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Tasks
{
    /// <summary>
    /// 任务完成弹层 —— 临时原生 uGUI 壳(TEMP SHELL),对标老端 task/TaskFinishView.ts。
    ///
    /// 链路:任务全步完成且非"找 NPC 对话"类 → <see cref="TaskModel.DoFinishTask"/> 打开本弹层 →
    /// 玩家点"领取奖励/提交任务"→ <see cref="TaskController.SubmitFinish"/> 发 30004 → 服务端回推 30001/30000
    /// 刷新任务栏。对标 TaskFinishView.ts:75-79 的 Fire(REQUEST_CCMD_EVENT, 30004, task_id) → Close。
    ///
    /// ★数据与入口全为真★:任务名/描述来自 config_task(TaskVo),奖励来自 config_task 的
    /// special_goods_list/award_list 经 <see cref="TaskReward"/> 真实解析(按职业过滤),提交是真发 30004。
    /// 老端 Laya UI(TaskFinishView.lh)尚无 Unity 转换产物(无 Bind/prefab),故按任务包许可做最小原生壳;
    /// 奖励物品已显示【真实名称】(GoodsModel/config_goods,经 TaskReward.ToText 替换裸 type_id);真实图标需 goodsIcon
    /// png 导入(未导入则名称降级,精确 blocker 见 BaseAwardItem.RefreshIcon)。待 Bind/prefab + 图标导入后换图标格。
    /// 字体复用场景中已打开文本的 TMP 字体(含中文字形),避免裸建视图豆腐块(同 DialogueView)。
    /// </summary>
    public sealed class TaskFinishView
    {
        private GameObject _root;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _targetText;
        private TextMeshProUGUI _rewardText;
        private TextMeshProUGUI _submitLabel;
        private GameObject _submitBtn;

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
            GameLog.Info("Task", "TaskFinishView 打开: 任务 {0} '{1}'", task.TaskId, task.TaskName);
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
            GameLog.Info("Task", "TaskFinishView 关闭");
        }

        private void Render()
        {
            if (_task == null) return;

            _titleText.text = string.IsNullOrEmpty(_task.TaskName) ? "任务完成" : _task.TaskName;

            string target = string.IsNullOrEmpty(_task.TaskTipsMsg) ? _task.Tips : _task.TaskTipsMsg;
            string desc = string.IsNullOrEmpty(_task.Desc) ? "" : "\n" + _task.Desc;
            _targetText.text = (target + " <color=#0a953e>(完成)</color>").Trim() + desc;

            // 奖励:config_task 真实数据,按当前职业过滤(对标老端 special_goods_list + award_list 装配)。
            List<TaskReward.Entry> rewards = TaskReward.Build(_task.SpecialGoodsList, _task.AwardList, RoleModel.Instance.Career);
            bool hasReward = rewards.Count > 0;
            _rewardText.text = hasReward ? "奖励\n" + TaskReward.ToText(rewards) : "";
            // 对标 TaskFinishView.ts:189-193:有奖励则"领取奖励",否则"提交任务"。
            _submitLabel.text = hasReward ? "领取奖励" : "提交任务";

            GameLog.Info("Task", "TaskFinishView 任务 {0} 奖励 {1} 项: {2}",
                _task.TaskId, rewards.Count, TaskReward.ToText(rewards, " / "));
        }

        private void OnSubmit()
        {
            if (_task == null) { Close(); return; }
            // 对标 TaskFinishView.ts:77 Fire(REQUEST_CCMD_EVENT, 30004, task_id):真发提交,关弹层,等服务端刷新任务栏。
            TaskController.Instance.SubmitFinish(_task.TaskId);
            Close();
        }

        // ===================== 构建(代码建 uGUI,居中弹层)=====================

        private void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Popup);
            if (parent == null)
            {
                GameLog.Error("Task", "TaskFinishView 无法构建:UI Popup 层未就绪(ViewManager 未 Init)");
                return;
            }

            _root = NewRect("TaskFinishView(TempShell)", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 半透明背景(挡住后面点击;点背景关闭,对标 click_bg_toClose)。
            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.55f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            // 居中面板(对标 is_center=true)。
            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(640f, 560f);
            panelRt.anchoredPosition = Vector2.zero;
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.96f);

            _titleText = NewText("Title", panel.transform, 34, TextAlignmentOptions.Top);
            RectTransform titleRt = _titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f); titleRt.anchorMax = new Vector2(1f, 1f); titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -24f); titleRt.sizeDelta = new Vector2(-48f, 48f);
            _titleText.color = new Color(1f, 0.86f, 0.45f);
            _titleText.fontStyle = FontStyles.Bold;

            _targetText = NewText("Target", panel.transform, 24, TextAlignmentOptions.TopLeft);
            RectTransform tgtRt = _targetText.rectTransform;
            tgtRt.anchorMin = new Vector2(0f, 1f); tgtRt.anchorMax = new Vector2(1f, 1f); tgtRt.pivot = new Vector2(0f, 1f);
            tgtRt.anchoredPosition = new Vector2(28f, -92f); tgtRt.sizeDelta = new Vector2(-56f, 180f);
            _targetText.textWrappingMode = TextWrappingModes.Normal;
            _targetText.color = Color.white;

            _rewardText = NewText("Reward", panel.transform, 24, TextAlignmentOptions.TopLeft);
            RectTransform rwRt = _rewardText.rectTransform;
            rwRt.anchorMin = new Vector2(0f, 0f); rwRt.anchorMax = new Vector2(1f, 0f); rwRt.pivot = new Vector2(0f, 0f);
            rwRt.anchoredPosition = new Vector2(28f, 130f); rwRt.sizeDelta = new Vector2(-56f, 200f);
            _rewardText.textWrappingMode = TextWrappingModes.Normal;
            _rewardText.color = new Color(0.9f, 0.95f, 1f);

            // 提交/领取按钮(底部居中)。
            _submitBtn = NewRect("Submit", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            RectTransform subRt = (RectTransform)_submitBtn.transform;
            subRt.pivot = new Vector2(0.5f, 0f);
            subRt.sizeDelta = new Vector2(260f, 72f);
            subRt.anchoredPosition = new Vector2(0f, 30f);
            Image subImg = _submitBtn.AddComponent<Image>();
            subImg.color = new Color(0.20f, 0.30f, 0.48f, 1f);
            _submitLabel = NewText("Label", _submitBtn.transform, 28, TextAlignmentOptions.Center);
            RectTransform slRt = _submitLabel.rectTransform;
            slRt.anchorMin = Vector2.zero; slRt.anchorMax = Vector2.one; slRt.offsetMin = Vector2.zero; slRt.offsetMax = Vector2.zero;
            _submitLabel.color = Color.white;
            UIUtil.AddClick(subImg, OnSubmit);

            // 关闭 X(右上角)。
            GameObject close = NewRect("Close", panel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            RectTransform closeRt = (RectTransform)close.transform;
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.sizeDelta = new Vector2(56f, 56f);
            closeRt.anchoredPosition = new Vector2(-8f, -8f);
            Image closeImg = close.AddComponent<Image>();
            closeImg.color = new Color(0.5f, 0.18f, 0.18f, 1f);
            TextMeshProUGUI closeLbl = NewText("X", close.transform, 30, TextAlignmentOptions.Center);
            RectTransform clRt = closeLbl.rectTransform;
            clRt.anchorMin = Vector2.zero; clRt.anchorMax = Vector2.one; clRt.offsetMin = Vector2.zero; clRt.offsetMax = Vector2.zero;
            closeLbl.text = "×";
            closeLbl.color = Color.white;
            UIUtil.AddClick(closeImg, Close);
        }

        // ---- uGUI 构建小工具(同 DialogueView 的 TEMP 壳约定)----

        private static GameObject NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
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
                TextMeshProUGUI src = Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (src != null) { _font = src.font; _fontMat = src.fontSharedMaterial; }
            }
            if (_font != null) t.font = _font;
            if (_fontMat != null) t.fontSharedMaterial = _fontMat;
        }
    }
}
