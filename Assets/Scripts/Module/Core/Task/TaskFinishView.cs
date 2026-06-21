using System.Collections.Generic;
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
    /// 任务完成弹层 —— 临时原生 uGUI 壳(TEMP SHELL),对标老端 task/TaskFinishView.ts。
    ///
    /// 链路:任务全步完成且非"找 NPC 对话"类 → <see cref="TaskModel.DoFinishTask"/> 打开本弹层 →
    /// 玩家点"领取奖励/提交任务"→ <see cref="TaskController.SubmitFinish"/> 发 30004 → 服务端回推 30001/30000
    /// 刷新任务栏。对标 TaskFinishView.ts:75-79 的 Fire(REQUEST_CCMD_EVENT, 30004, task_id) → Close。
    ///
    /// ★数据与入口全为真★:任务名/描述来自 config_task(TaskVo),奖励来自 config_task 的
    /// special_goods_list/award_list 经 <see cref="TaskReward"/> 真实解析(按职业过滤),提交是真发 30004。
    /// 老端 Laya UI(TaskFinishView.lh)尚无 Unity 转换产物(无 Bind/prefab),故按任务包许可做最小原生壳;
    /// 奖励物品行显示【真实图标 + 品质底板 + 数量 + 名称】(GoodsModel/config_goods + GameResPath + ResManager,
    /// 与 <see cref="BaseAwardItem"/> 同一数据/加载路径;图标缺图自 yu_client/cdn 兜底导入,真缺才降级 + 精确 blocker);
    /// 货币/经验(type_id==0)无 goods 记录,走底部文本。复用 BaseAwardItem.prefab 受其缺 Bind 组件阻断(见第 6 轮包),
    /// 故本壳按既有原生风格自建图标格;待 prefab 回填组件后可换成 BaseAwardItem 实例。
    /// 字体复用场景中已打开文本的 TMP 字体(含中文字形),避免裸建视图豆腐块(同 DialogueView)。
    /// </summary>
    public sealed class TaskFinishView
    {
        private GameObject _root;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _targetText;
        private RectTransform _rewardRow;
        private readonly List<GameObject> _rewardCells = new List<GameObject>();
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
            BuildRewardCells(rewards); // 物品 → 图标格(图标+底板+数量+名称);货币/经验 → _rewardText
            // 对标 TaskFinishView.ts:189-193:有奖励则"领取奖励",否则"提交任务"。
            _submitLabel.text = hasReward ? "领取奖励" : "提交任务";

            GameLog.Info("Task", "TaskFinishView 任务 {0} 奖励 {1} 项: {2}",
                _task.TaskId, rewards.Count, TaskReward.ToText(rewards, " / "));
        }

        // ===================== 奖励图标格(对标老端 BaseAwardItem 行)=====================

        /// <summary>物品奖励 → 图标格(底板+图标+数量+名称),走 GoodsModel + ResManager;货币/经验 → _rewardText。</summary>
        private void BuildRewardCells(List<TaskReward.Entry> rewards)
        {
            for (int i = 0; i < _rewardCells.Count; i++)
                if (_rewardCells[i] != null) Object.Destroy(_rewardCells[i]);
            _rewardCells.Clear();
            if (_rewardRow == null) return;

            var goods = new List<TaskReward.Entry>();
            var currency = new List<TaskReward.Entry>();
            for (int i = 0; i < rewards.Count; i++)
                (rewards[i].IsCurrency ? currency : goods).Add(rewards[i]);

            const float cellW = 116f, cellH = 150f, gap = 16f;
            int n = goods.Count;
            float totalW = n > 0 ? n * cellW + (n - 1) * gap : 0f;
            float startX = -totalW / 2f + cellW / 2f;
            for (int i = 0; i < n; i++)
                _rewardCells.Add(CreateRewardCell(goods[i], startX + i * (cellW + gap), cellW, cellH));

            // 货币/经验(type_id==0):暂保留文本(真名待 ConfigNotNormalGoods,见第 6 轮包)。
            _rewardText.text = currency.Count > 0 ? "其它奖励:" + TaskReward.ToText(currency, "   ") : "";
        }

        private GameObject CreateRewardCell(TaskReward.Entry e, float x, float w, float h)
        {
            GameObject cell = NewRect("RewardCell", _rewardRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform crt = (RectTransform)cell.transform;
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(w, h);
            crt.anchoredPosition = new Vector2(x, 0f);

            // 品质底板(对标 item_bg = com_goods_plate_{color},common 图集)。
            GameObject plateGo = NewRect("Plate", cell.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            RectTransform plateRt = (RectTransform)plateGo.transform;
            plateRt.pivot = new Vector2(0.5f, 1f);
            plateRt.sizeDelta = new Vector2(96f, 96f);
            plateRt.anchoredPosition = Vector2.zero;
            Image plateImg = plateGo.AddComponent<Image>();
            plateImg.raycastTarget = false;
            _ = ResManager.SetImageAsync(plateImg,
                GameResPath.GetIcon("common", "com_goods_plate_" + GoodsModel.GetDisplayColor(e.TypeId)), false, false);

            // 物品图标(叠底板上)。
            GameObject iconGo = NewRect("Icon", plateGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform iconRt = (RectTransform)iconGo.transform;
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(84f, 84f);
            iconRt.anchoredPosition = Vector2.zero;
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;
            _ = LoadCellIcon(iconImg, GameResPath.GetGoodsIconPath(GoodsModel.GetGoodsIcon(e.TypeId)), e.TypeId);

            // 数量(>1 才显,底板右下,对标 ChangeCountVisible)。
            if (e.Count > 1)
            {
                TextMeshProUGUI cnt = NewText("Count", plateGo.transform, 22, TextAlignmentOptions.BottomRight);
                RectTransform cntRt = cnt.rectTransform;
                cntRt.anchorMin = Vector2.zero; cntRt.anchorMax = Vector2.one;
                cntRt.offsetMin = new Vector2(2f, 2f); cntRt.offsetMax = new Vector2(-4f, -4f);
                cnt.text = "×" + e.Count;
                cnt.fontStyle = FontStyles.Bold;
                cnt.color = Color.white;
                cnt.raycastTarget = false;
            }

            // 名称(底板下方)。
            TextMeshProUGUI nameT = NewText("Name", cell.transform, 18, TextAlignmentOptions.Top);
            RectTransform nameRt = nameT.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0f); nameRt.anchorMax = new Vector2(1f, 0f); nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.anchoredPosition = new Vector2(0f, 2f); nameRt.sizeDelta = new Vector2(0f, 46f);
            string nm = GoodsModel.GetGoodsName(e.TypeId);
            nameT.text = string.IsNullOrEmpty(nm) ? ("物品 " + e.TypeId) : nm;
            nameT.color = new Color(0.95f, 0.96f, 1f);
            nameT.textWrappingMode = TextWrappingModes.Normal;
            nameT.raycastTarget = false;

            return cell;
        }

        private static async Task LoadCellIcon(Image img, string key, int typeId)
        {
            bool ok = await ResManager.SetImageAsync(img, key, false, false);
            if (img == null) return;
            if (!ok)
            {
                img.enabled = false;
                GameLog.Warn("Task", "TaskFinishView 奖励图标缺失(blocker): typeId={0} key={1}", typeId, key);
            }
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

            // 奖励图标行(物品格:品质底板 + 图标 + 数量 + 名称),居中带状容器;货币/经验走下方 _rewardText。
            GameObject rowGo = NewRect("RewardRow", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _rewardRow = (RectTransform)rowGo.transform;
            _rewardRow.pivot = new Vector2(0.5f, 0.5f);
            _rewardRow.sizeDelta = new Vector2(600f, 156f);
            _rewardRow.anchoredPosition = new Vector2(0f, -40f);

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
