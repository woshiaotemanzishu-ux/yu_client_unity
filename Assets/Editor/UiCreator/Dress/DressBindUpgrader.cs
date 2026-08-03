using Shenxiao.Editor.LayaUI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Dress;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Dress
{
    /// <summary>
    /// 在现有 DressModule.prefab 上增量升级业务脚本和列表布局；不重转、不覆盖转换后的视觉树。
    /// </summary>
    public static class DressBindUpgrader
    {
        private const string ModulePath = "Assets/Prefabs/UI/Dress/DressModule.prefab";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Dress",
                Name = "DressModule(装扮 Bind 升级)",
                Note = "增量升级 6 个业务组件，并把列表布局与战力文字直接保存到 Prefab。",
                Order = 98,
                Generate = () => Generate(),
                PrefabPath = ModulePath,
            });
        }

        public static bool Generate()
        {
            if (!LayaBindFiller.FillPrefab(ModulePath))
            {
                Debug.LogError("[UiCreator] DressModule Bind 升级失败");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(ModulePath);
            try
            {
                DressView view = root.GetComponentInChildren<DressView>(true);
                DressSubView sub = root.GetComponentInChildren<DressSubView>(true);
                if (view == null || sub == null)
                {
                    Debug.LogError("[UiCreator] DressModule 缺 DressView/DressSubView 业务组件");
                    return false;
                }

                EnsureHorizontal(view._Scroller1 != null ? view._Scroller1.content : null, 0f);
                EnsureGrid(sub.scroll != null ? sub.scroll.content : null);
                EnsureFitter(sub.Content11, ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize);
                EnsureFitter(sub.Content1, ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.Unconstrained);
                EnsurePowerLabel(sub._gp_fight);
                EnsureItemClickSurface(root.GetComponentInChildren<DressItem>(true));

                PrefabUtility.SaveAsPrefabAsset(root, ModulePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            return Verify() && DressAssetPreflight.EnsureAddressables();
        }

        private static void EnsureGrid(RectTransform content)
        {
            if (content == null) return;
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(8, 8, 0, 0);
            grid.cellSize = new Vector2(170f, 205f);
            grid.spacing = new Vector2(5f, 0f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            EnsureFitter(content, ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize);
        }

        private static void EnsureHorizontal(RectTransform content, float spacing)
        {
            if (content == null) return;
            HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(135, 135, 0, 0);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            EnsureFitter(content, ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.Unconstrained);
        }

        private static void EnsureFitter(RectTransform target, ContentSizeFitter.FitMode horizontal, ContentSizeFitter.FitMode vertical)
        {
            if (target == null) return;
            ContentSizeFitter fitter = target.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = target.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = horizontal;
            fitter.verticalFit = vertical;
        }

        private static void EnsurePowerLabel(RectTransform parent)
        {
            if (parent == null) return;
            Transform existed = parent.Find("dress_power_label");
            TextMeshProUGUI label;
            if (existed == null)
            {
                var go = new GameObject("dress_power_label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                RectTransform rect = (RectTransform)go.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                label = go.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                label = existed.GetComponent<TextMeshProUGUI>();
                if (label == null) label = existed.gameObject.AddComponent<TextMeshProUGUI>();
            }
            label.text = "战力--";
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 28f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(255, 229, 92, 255);
            label.raycastTarget = false;
        }

        private static void EnsureItemClickSurface(DressItem item)
        {
            if (item == null) return;
            foreach (Graphic graphic in item.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            if (item.click_bg != null)
            {
                Image obsolete = item.click_bg.GetComponent<Image>();
                if (obsolete != null) Object.DestroyImmediate(obsolete, true);
            }
            if (item.bg != null) item.bg.raycastTarget = true;
        }

        private static bool Verify()
        {
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
            bool ok = saved != null
                && saved.GetComponentInChildren<DressView>(true) != null
                && saved.GetComponentInChildren<DressSubView>(true) != null
                && saved.GetComponentInChildren<DressItem>(true) != null
                && saved.GetComponentInChildren<DressTab>(true) != null
                && saved.GetComponentInChildren<DressProItem>(true) != null
                && saved.GetComponentInChildren<DressSkillItem>(true) != null;
            if (ok)
            {
                DressView view = saved.GetComponentInChildren<DressView>(true);
                DressSubView sub = saved.GetComponentInChildren<DressSubView>(true);
                DressItem item = saved.GetComponentInChildren<DressItem>(true);
                ok = view._Scroller1.content.GetComponent<HorizontalLayoutGroup>() != null
                    && sub.scroll.content.GetComponent<GridLayoutGroup>() != null
                    && sub._gp_fight.Find("dress_power_label") != null
                    && item != null && item.bg != null && item.bg.raycastTarget;
            }
            Debug.Log("[UiCreator] DressBindUpgrader " + (ok ? "OK" : "FAILED") + " " + ModulePath);
            return ok;
        }

        public static void GenerateBatch()
        {
            try { EditorApplication.Exit(Generate() ? 0 : 1); }
            catch (System.Exception exception)
            {
                Debug.LogError("[UiCreator] DressBindUpgrader 异常: " + exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
