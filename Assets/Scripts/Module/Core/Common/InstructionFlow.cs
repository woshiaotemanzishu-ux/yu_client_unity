using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>复用 CommonModule.prefab/InstructionView 的配置驱动说明弹层。</summary>
    public static class InstructionFlow
    {
        private const float ItemWidth = 558f;
        private const float ContentWidth = 552f;
        private const float TitleOffset = 43f;
        private const float SectionSpacing = 15f;
        private const float LineHeight = 18f;
        private const float WrappedLineStep = 26f;
        private const float LineSpacing = 10f;

        private static readonly Color32 SectionTitleColor = new Color32(0x76, 0x33, 0x20, 0xff);
        private static readonly Color32 SectionTitleOutlineColor = new Color32(0xdf, 0xd1, 0xcd, 0xff);
        private static readonly Color32 DescriptionColor = new Color32(0x66, 0x39, 0x15, 0xff);
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
                ConfigureScroll();
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

        private static void ConfigureScroll()
        {
            if (_view == null || _view._panel_item == null || _view._vbox_con == null) return;

            RectTransform content = _view._vbox_con;
            content.anchorMin = content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ContentWidth);

            // 动态高度必须落到 ScrollRect 的真实 content；不能只改内部 VBox。
            _view._panel_item.content = content;
            _view._panel_item.horizontal = false;
            _view._panel_item.vertical = true;
            Image hitArea = _view._panel_item.GetComponent<Image>();
            if (hitArea == null) hitArea = _view._panel_item.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = SectionSpacing;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
        }

        private static void Render(InstructionConfigs.Entry entry)
        {
            foreach (GameObject item in RuntimeItems)
            {
                if (item == null) continue;
                item.SetActive(false);
                UnityEngine.Object.Destroy(item);
            }
            RuntimeItems.Clear();

            if (_view._lb_title != null) _view._lb_title.text = entry.Title;
            if (_view._lb_ins != null) _view._lb_ins.gameObject.SetActive(false);
            if (_view._tpl_InstructionItem == null || _view._vbox_con == null) return;

            ConfigureScroll();
            float totalHeight = 0f;
            int renderedSections = 0;
            foreach (InstructionConfigs.Section section in entry.Sections)
            {
                GameObject itemGo = UnityEngine.Object.Instantiate(
                    _view._tpl_InstructionItem, _view._vbox_con, false);
                RuntimeItems.Add(itemGo);
                itemGo.SetActive(true);
                InstructionItemBind item = itemGo.GetComponent<InstructionItemBind>();
                if (item == null) continue;
                item.Show();

                bool showTitle = !string.IsNullOrEmpty(section.Title);
                if (item._box_title != null) item._box_title.gameObject.SetActive(showTitle);
                if (item._html_title != null)
                {
                    item._html_title.text = section.Title;
                    item._html_title.richText = true;
                    item._html_title.fontSize = 20f;
                    item._html_title.fontStyle = FontStyles.Bold;
                    item._html_title.color = SectionTitleColor;
                    item._html_title.outlineColor = SectionTitleOutlineColor;
                    item._html_title.outlineWidth = 0.1f;
                    item._html_title.textWrappingMode = TextWrappingModes.NoWrap;
                    item._html_title.rectTransform.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Horizontal,
                        Mathf.Max(1f, item._html_title.GetPreferredValues(section.Title).x));
                    item._html_title.rectTransform.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical, 20f);
                }
                if (item._tpl_InstructionSmallItem != null)
                    item._tpl_InstructionSmallItem.SetActive(false);
                if (item._tpl_InstructionSmallItem == null || item._vbox_con == null)
                    continue;

                RectTransform inner = item._vbox_con;
                inner.anchorMin = inner.anchorMax = new Vector2(0f, 1f);
                inner.pivot = new Vector2(0f, 1f);
                inner.anchoredPosition = new Vector2(4f, showTitle ? -TitleOffset : 0f);
                inner.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ContentWidth);
                VerticalLayoutGroup innerLayout = inner.GetComponent<VerticalLayoutGroup>();
                if (innerLayout != null)
                {
                    innerLayout.spacing = LineSpacing;
                    innerLayout.childControlWidth = false;
                    innerLayout.childControlHeight = false;
                    innerLayout.childForceExpandWidth = false;
                    innerLayout.childForceExpandHeight = false;
                }

                float innerHeight = 0f;
                int renderedLines = 0;
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
                    line._lb_desc.fontSize = 18f;
                    line._lb_desc.fontStyle = FontStyles.Normal;
                    line._lb_desc.color = DescriptionColor;
                    line._lb_desc.textWrappingMode = TextWrappingModes.Normal;
                    RectTransform textRect = line._lb_desc.rectTransform;
                    textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
                    textRect.pivot = new Vector2(0f, 1f);
                    textRect.anchoredPosition = Vector2.zero;
                    textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ContentWidth);
                    // TMP 在只有一行高的 Rect 内更新网格时，textInfo 可能只报告首行，
                    // 但后续渲染仍会把换行内容画出来，造成相邻说明互相覆盖。
                    // 先给足测量高度，再按真实换行数收回 Rect，保持单行旧尺寸不变。
                    textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 10000f);
                    line._lb_desc.ForceMeshUpdate();
                    int visualLineCount = Mathf.Max(1, line._lb_desc.textInfo.lineCount);
                    float height = LineHeight + (visualLineCount - 1) * WrappedLineStep;
                    textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                    line._lb_desc.ForceMeshUpdate();

                    RectTransform lineRect = (RectTransform)lineGo.transform;
                    lineRect.anchorMin = lineRect.anchorMax = new Vector2(0f, 1f);
                    lineRect.pivot = new Vector2(0f, 1f);
                    lineRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ContentWidth);
                    lineRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                    if (renderedLines > 0) innerHeight += LineSpacing;
                    innerHeight += height;
                    renderedLines++;
                }

                inner.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical, Mathf.Max(innerHeight, 0f));
                LayoutRebuilder.ForceRebuildLayoutImmediate(inner);

                float itemHeight = (showTitle ? TitleOffset : 0f) + innerHeight;
                RectTransform itemRect = (RectTransform)itemGo.transform;
                itemRect.anchorMin = itemRect.anchorMax = new Vector2(0f, 1f);
                itemRect.pivot = new Vector2(0f, 1f);
                itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ItemWidth);
                itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
                if (renderedSections > 0) totalHeight += SectionSpacing;
                totalHeight += itemHeight;
                renderedSections++;
            }

            _view._vbox_con.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, Mathf.Max(totalHeight, 1f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(_view._vbox_con);
            if (_view._panel_item != null)
            {
                _view._panel_item.StopMovement();
                _view._panel_item.verticalNormalizedPosition = 1f;
            }
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
