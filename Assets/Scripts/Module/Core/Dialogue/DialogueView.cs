using System.Collections.Generic;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Dialogue
{
    /// <summary>
    /// NPC 对话视图 —— 临时原生 uGUI 壳(TEMP SHELL)。
    ///
    /// 说明:老端 dialogue/DialogueView.ts 是 Laya UI,Unity 端尚无对应转换产物(无 DialogueViewBind/prefab),
    /// 故按本轮任务包许可做"最小可交互原生 Unity UI",但 ★数据与入口全为真★:
    ///   · NPC 名 = config_npc.name;对话文字 = config_talk(12101 默认对话 / 12102 任务对话);
    ///   · 任务项 = 12101 真实 task_list;点"接取/提交/对话"= 真发 30003/30004/30007。
    /// 待 LayaUI 转换流水线产出 DialogueView 后,用生成 Bind/prefab 替换本壳(见交付报告"下一轮")。
    ///
    /// 字体:复用场景中已打开 MainUI 文本的 TMP 字体(含中文字形),避免裸建视图时字体资源缺失导致豆腐块。
    /// </summary>
    public sealed class DialogueView
    {
        private GameObject _root;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _contentText;
        private RectTransform _btnContainer;
        private readonly List<GameObject> _buttons = new List<GameObject>();

        private NpcDialogVo _vo;
        private int _dialogIndex;        // 当前内容块(1 基,对标 dialog_index)

        private static TMP_FontAsset _font;
        private static Material _fontMat;

        // ===================== 开/关 =====================

        public void Open(NpcDialogVo vo)
        {
            if (vo == null) return;
            _vo = vo;
            _dialogIndex = 1;
            EnsureBuilt();
            if (_root == null) return;

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            DialogueModel.Instance.DialogIsOpen = true;
            DialogueModel.Instance.CurrentNpcId = vo.NpcId;
            Render();
            GameLog.Info("Dialogue", "对话视图打开: npcId={0} talkId={1} 任务数={2} default={3}",
                vo.NpcId, vo.TalkId, vo.TaskList.Count, vo.IsDefaultTalk());
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
            DialogueModel.Instance.DialogIsOpen = false;
            DialogueModel.Instance.CurrentNpcId = 0;
            GameLog.Info("Dialogue", "对话视图关闭");
        }

        // 继续(对标 NextDialog:推进内容块,越界即关)。
        private void NextDialog()
        {
            _dialogIndex++;
            if (_vo == null || _dialogIndex > _vo.GetTalkContentCount()) { Close(); return; }
            Render();
        }

        // ===================== 渲染 =====================

        private void Render()
        {
            if (_vo == null) return;
            ClearButtons();

            _nameText.text = DialogueModel.Instance.GetNpcName(_vo.NpcId);

            // 当前内容块的 NPC/角色文字(多条拼行展示)。
            TalkConfigs.TalkContentBlock block = _vo.GetContent(_dialogIndex);
            var lines = new List<string>();
            var actionNodes = new List<DialogueNodeVo>();
            if (block != null)
            {
                foreach (DialogueNodeVo n in block.Nodes)
                {
                    if (DialogueTypeConst.IsActionNode(n.Type)) actionNodes.Add(n);
                    else if (!string.IsNullOrEmpty(n.Text)) lines.Add(n.Text);
                }
            }
            _contentText.text = lines.Count > 0 ? string.Join("\n", lines)
                : (_vo.TalkCfg == null ? "(talk_id=" + _vo.TalkId + " 无对话配置)" : "");

            // 1) 12101 默认对话的任务菜单:每个关联任务一个按钮 → SelectTask(发 12102)。
            foreach (DialogueTaskEntry t in _vo.TaskList)
            {
                DialogueTaskEntry task = t; // 闭包捕获
                string label = TaskStateText(task.TaskState) + " " + (string.IsNullOrEmpty(task.TaskName) ? task.TaskId.ToString() : task.TaskName);
                AddButton(label, () => DialogueController.Instance.SelectTask(_vo.NpcId, task.TaskId, task.TaskState));
            }

            // 2) 任务对话的动作节点:接取/提交/对话事件 → ClickAnswerHandler(发 30003/30004/30007),完后关闭。
            foreach (DialogueNodeVo n in actionNodes)
            {
                DialogueNodeVo node = n;
                string label = string.IsNullOrEmpty(node.Text) ? ActionDefaultText(node.Type) : node.Text;
                AddButton(label, () =>
                {
                    DialogueController.Instance.ClickAnswerHandler(_vo, node);
                    Close();
                });
            }

            // 3) 继续 / 关闭:还有后续内容块则"继续",否则"关闭"。
            bool hasMore = _dialogIndex < _vo.GetTalkContentCount();
            if (hasMore) AddButton("继续", NextDialog);
            AddButton("关闭", Close);
        }

        private static string TaskStateText(int state)
        {
            switch (state)
            {
                case 1: return "[接取]";
                case 2: return "[进行中]";
                case 3: return "[提交]";
                case 4: return "[对话]";
                default: return "[任务]";
            }
        }

        private static string ActionDefaultText(int type)
        {
            switch (type)
            {
                case DialogueTypeConst.TRIGGER: return "接取任务";
                case DialogueTypeConst.FINISH:
                case DialogueTypeConst.FINISH_AND_TRIGGER: return "完成任务";
                case DialogueTypeConst.TALK_EVENT: return "继续";
                default: return "确定";
            }
        }

        // ===================== 构建(代码建 uGUI)=====================

        private void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Popup);
            if (parent == null)
            {
                GameLog.Error("Dialogue", "对话视图无法构建:UI Popup 层未就绪(ViewManager 未 Init)");
                return;
            }

            _root = NewRect("DialogueView(TempShell)", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 半透明背景(挡住后面点击)。
            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.55f);
            bgImg.raycastTarget = true;

            // 底部对话面板。
            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
            RectTransform panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.sizeDelta = new Vector2(-80f, 380f);   // 左右各留 40,高 380
            panelRt.anchoredPosition = new Vector2(0f, 30f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.96f);

            _nameText = NewText("Name", panel.transform, 30, TextAlignmentOptions.TopLeft);
            RectTransform nameRt = _nameText.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 1f); nameRt.anchorMax = new Vector2(1f, 1f); nameRt.pivot = new Vector2(0f, 1f);
            nameRt.anchoredPosition = new Vector2(28f, -18f); nameRt.sizeDelta = new Vector2(-56f, 44f);
            _nameText.color = new Color(1f, 0.86f, 0.45f);
            _nameText.fontStyle = FontStyles.Bold;

            _contentText = NewText("Content", panel.transform, 26, TextAlignmentOptions.TopLeft);
            RectTransform contRt = _contentText.rectTransform;
            contRt.anchorMin = new Vector2(0f, 1f); contRt.anchorMax = new Vector2(1f, 1f); contRt.pivot = new Vector2(0f, 1f);
            contRt.anchoredPosition = new Vector2(28f, -72f); contRt.sizeDelta = new Vector2(-56f, 150f);
            _contentText.textWrappingMode = TextWrappingModes.Normal;
            _contentText.color = Color.white;

            // 按钮容器(纵向排列,底部对齐)。
            GameObject btnBox = NewRect("Buttons", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
            _btnContainer = (RectTransform)btnBox.transform;
            _btnContainer.pivot = new Vector2(0.5f, 0f);
            _btnContainer.sizeDelta = new Vector2(-56f, 150f);
            _btnContainer.anchoredPosition = new Vector2(0f, 16f);
            var layout = btnBox.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void AddButton(string label, System.Action onClick)
        {
            if (_btnContainer == null) return;
            GameObject go = NewRect("Btn", _btnContainer, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 220f; le.preferredHeight = 64f;
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.20f, 0.30f, 0.48f, 1f);

            TextMeshProUGUI t = NewText("Label", go.transform, 26, TextAlignmentOptions.Center);
            RectTransform trt = t.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            t.text = label;
            t.color = Color.white;

            UIUtil.AddClick(img, onClick);
            _buttons.Add(go);
        }

        private void ClearButtons()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] != null) Object.Destroy(_buttons[i]);
            }
            _buttons.Clear();
        }

        // ---- uGUI 构建小工具 ----

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

        // 复用场景里已有 TMP 文本的字体(MainUI 已打开,字体含中文)。一次性缓存。
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
