using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>复用 CommonModule.prefab/InstructionView 的配置驱动说明弹层。</summary>
    public static class InstructionFlow
    {
        private static readonly List<GameObject> RuntimeItems = new List<GameObject>();
        private static readonly Regex FontOpen = new Regex(
            "<font\\s+color=['\"]([^'\"]+)['\"]>", RegexOptions.IgnoreCase);

        private static GameObject _moduleRoot;
        private static InstructionViewBind _view;
        private static Image _mask;
        private static bool _loading;
        private static int _pendingId;

        public static void Show(int instructionId)
        {
            if (instructionId <= 0) return;
            _pendingId = instructionId;
            _ = ShowAsync();
        }

        public static void Close()
        {
            if (_view != null && _view.IsShown) _view.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
        }

        private static async Task ShowAsync()
        {
            await InstructionConfigs.EnsureLoaded();
            if (!await EnsureViewAsync()) return;
            InstructionConfigs.Entry entry = InstructionConfigs.Get(_pendingId);
            if (entry == null)
            {
                GameLog.Warn("Instruction", "找不到说明配置 id={0}", _pendingId);
                return;
            }

            Render(entry);
            _moduleRoot.SetActive(true);
            if (_mask != null)
            {
                _mask.gameObject.SetActive(true);
                _mask.transform.SetAsFirstSibling();
            }
            _view.Show(_pendingId);
            _view.transform.SetAsLastSibling();
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_moduleRoot != null && _view != null) return true;
            if (_loading) return false;
            _loading = true;
            try
            {
                string key = GameResPath.GetUIPrefab("common", "CommonModule");
                _moduleRoot = await ResManager.InstantiateAsync(
                    key, ViewManager.GetLayer(UILayer.Popup));
                if (_moduleRoot == null)
                {
                    GameLog.Error("Instruction", "CommonModule 加载失败: {0}", key);
                    return false;
                }

                _moduleRoot.name = "CommonModule(Instruction)";
                foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true))
                    view.gameObject.SetActive(false);

                _view = _moduleRoot.GetComponentInChildren<InstructionViewBind>(true);
                if (_view == null)
                {
                    GameLog.Error("Instruction", "CommonModule 缺 InstructionViewBind");
                    ResManager.ReleaseInstance(_moduleRoot);
                    _moduleRoot = null;
                    return false;
                }

                if (_view._img_close != null)
                {
                    _view._img_close.raycastTarget = true;
                    UIUtil.AddClick(_view._img_close, Close);
                }
                if (_view._tpl_InstructionItem != null)
                    _view._tpl_InstructionItem.SetActive(false);
                EnsureMask();
                _moduleRoot.SetActive(false);
                return true;
            }
            finally
            {
                _loading = false;
            }
        }

        private static void EnsureMask()
        {
            if (_mask != null || _moduleRoot == null) return;
            var go = new GameObject("__InstructionMask", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(_moduleRoot.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _mask = go.GetComponent<Image>();
            _mask.color = new Color(0f, 0f, 0f, 0.62f);
            _mask.raycastTarget = true;
            UIUtil.AddClick(_mask, Close);
        }

        private static void Render(InstructionConfigs.Entry entry)
        {
            foreach (GameObject item in RuntimeItems)
                if (item != null) UnityEngine.Object.Destroy(item);
            RuntimeItems.Clear();

            if (_view._lb_title != null) _view._lb_title.text = entry.Title;
            if (_view._lb_ins != null) _view._lb_ins.gameObject.SetActive(false);
            if (_view._tpl_InstructionItem == null || _view._vbox_con == null) return;

            float y = 0f;
            const float width = 520f;
            foreach (InstructionConfigs.Section section in entry.Sections)
            {
                GameObject itemGo = UnityEngine.Object.Instantiate(
                    _view._tpl_InstructionItem, _view._vbox_con, false);
                RuntimeItems.Add(itemGo);
                itemGo.SetActive(true);
                InstructionItemBind item = itemGo.GetComponent<InstructionItemBind>();
                if (item == null) continue;
                item.Show();

                if (item._html_title != null)
                {
                    item._html_title.text = section.Title;
                    item._html_title.richText = true;
                }
                if (item._tpl_InstructionSmallItem != null)
                    item._tpl_InstructionSmallItem.SetActive(false);
                if (item._tpl_InstructionSmallItem == null || item._vbox_con == null)
                    continue;

                float innerY = 0f;
                foreach (string raw in section.Lines)
                {
                    GameObject lineGo = UnityEngine.Object.Instantiate(
                        item._tpl_InstructionSmallItem, item._vbox_con, false);
                    lineGo.SetActive(true);
                    InstructionSmallItemBind line = lineGo.GetComponent<InstructionSmallItemBind>();
                    if (line == null || line._lb_desc == null) continue;
                    line.Show();
                    line._lb_desc.richText = true;
                    line._lb_desc.text = ToTmpRichText(raw);
                    line._lb_desc.textWrappingMode = TMPro.TextWrappingModes.Normal;
                    line._lb_desc.rectTransform.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Horizontal, width - 28f);
                    float height = Mathf.Max(30f, line._lb_desc.preferredHeight + 8f);
                    RectTransform lineRect = (RectTransform)lineGo.transform;
                    lineRect.anchorMin = lineRect.anchorMax = new Vector2(0f, 1f);
                    lineRect.pivot = new Vector2(0f, 1f);
                    lineRect.anchoredPosition = new Vector2(14f, -innerY);
                    lineRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 28f);
                    lineRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                    innerY += height;
                }

                float itemHeight = 52f + innerY + 12f;
                RectTransform itemRect = (RectTransform)itemGo.transform;
                itemRect.anchorMin = itemRect.anchorMax = new Vector2(0f, 1f);
                itemRect.pivot = new Vector2(0f, 1f);
                itemRect.anchoredPosition = new Vector2(0f, -y);
                itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
                y += itemHeight + 8f;
            }

            _view._vbox_con.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, Mathf.Max(y, 1f));
            if (_view._panel_item != null)
                _view._panel_item.verticalNormalizedPosition = 1f;
        }

        private static string ToTmpRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string converted = FontOpen.Replace(text, "<color=$1>");
            return converted.Replace("</font>", "</color>");
        }

        internal static void Reset()
        {
            Close();
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _view = null;
            _mask = null;
            _loading = false;
            _pendingId = 0;
            RuntimeItems.Clear();
        }
    }
}
