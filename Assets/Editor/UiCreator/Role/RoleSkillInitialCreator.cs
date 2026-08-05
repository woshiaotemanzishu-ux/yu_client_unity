using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Role
{
    /// <summary>
    /// One-time promotion of the converted passive-skill view plus idempotent scroll-layout repair.
    /// This creator is explicit-only and never participates in automatic prefab rebuilding.
    /// </summary>
    public static class RoleSkillInitialCreator
    {
        private const string RoleModulePath = "Assets/Prefabs/UI/Role/RoleModule.prefab";
        private const string ViewName = "SkillPassiveSubItem";
        private const string ItemTemplateName = "SkillPassiveItemTemplate";

        [MenuItem("Tools/UiCreator/Role/SkillPassiveSubItem Initial Creator")]
        public static void Generate()
        {
            GenerateInternal();
        }

        private static bool GenerateInternal()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(RoleModulePath);
            if (asset == null)
            {
                Debug.LogError("[UiCreator] Missing " + RoleModulePath);
                return false;
            }

            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(RoleModulePath);
                Transform root = contents.transform;
                Transform view = root.Find(ViewName);
                if (view == null)
                {
                    view = PromoteView(root);
                    if (view == null)
                        return false;
                }
                else
                {
                    Debug.Log("[UiCreator] Existing top-level " + ViewName + "; applying idempotent repair.");
                }

                if (!RepairLayout(view))
                    return false;

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, RoleModulePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = saved;
                EditorGUIUtility.PingObject(saved);
                Debug.Log("[UiCreator] Saved " + RoleModulePath + " with passive-skill list layout.");
                return saved != null;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] RoleSkillInitialCreator.Generate failed: " + e);
                return false;
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>CLI entry: exits 0 on a valid saved layout, otherwise exits 3.</summary>
        public static void GenerateCli()
        {
            try
            {
                bool generated = GenerateInternal();
                GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(RoleModulePath);
                Transform view = saved != null ? saved.transform.Find(ViewName) : null;
                bool ok = generated && view != null && IsLayoutValid(view);
                Debug.Log("[UiCreator] RoleSkillInitialCreator.GenerateCli "
                    + (ok ? "OK " : "FAILED ") + RoleModulePath);
                EditorApplication.Exit(ok ? 0 : 3);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] RoleSkillInitialCreator.GenerateCli failed: " + e);
                EditorApplication.Exit(3);
            }
        }

        private static Transform PromoteView(Transform root)
        {
            Transform passiveItem = root.Find("SkillPassiveItem");
            Transform source = passiveItem != null
                ? passiveItem.Find("__Templates/" + ViewName)
                : null;
            if (source == null && passiveItem != null)
            {
                // Compatibility with the historical converter output.
                source = passiveItem.Find(ViewName);
            }

            if (passiveItem == null || source == null)
            {
                Debug.LogError("[UiCreator] Missing SkillPassiveItem/__Templates/" + ViewName
                    + " or its compatible direct child.");
                return null;
            }

            source.SetParent(root, false);
            source.name = ViewName;
            SetCentered((RectTransform)source, 720f, 997f, false);

            Transform content = FindContent(source);
            if (content == null)
            {
                Debug.LogError("[UiCreator] Missing " + ViewName
                    + "/_Scroller1/Viewport/Content; save aborted.");
                return null;
            }

            passiveItem.name = ItemTemplateName;
            passiveItem.SetParent(content, false);
            SetTopLeft((RectTransform)passiveItem, 0f, 0f, 148f, 173f);
            passiveItem.gameObject.SetActive(false);
            source.gameObject.SetActive(false);
            return source;
        }

        private static bool RepairLayout(Transform view)
        {
            RectTransform viewRt = view as RectTransform;
            if (viewRt == null)
            {
                Debug.LogError("[UiCreator] " + ViewName + " is missing RectTransform.");
                return false;
            }

            SetCentered(viewRt, 720f, 997f, false);
            view.gameObject.SetActive(false);

            Transform scroller = view.Find("_Scroller1");
            Transform viewport = scroller != null ? scroller.Find("Viewport") : null;
            Transform content = viewport != null ? viewport.Find("Content") : null;
            RectTransform viewportRt = viewport as RectTransform;
            RectTransform contentRt = content as RectTransform;
            if (scroller == null || viewportRt == null || contentRt == null)
            {
                Debug.LogError("[UiCreator] " + ViewName
                    + " is missing _Scroller1/Viewport/Content; save aborted.");
                return false;
            }

            Transform template = content.Find(ItemTemplateName);
            if (template != null)
            {
                SetTopLeft((RectTransform)template, 0f, 0f, 148f, 173f);
                template.gameObject.SetActive(false);
            }

            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(148f, 173f);
            grid.spacing = new Vector2(0f, 0f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectMask2D mask = viewport.GetComponent<RectMask2D>();
            if (mask == null)
                viewport.gameObject.AddComponent<RectMask2D>();

            ScrollRect scroll = scroller.GetComponent<ScrollRect>();
            if (scroll == null)
                scroll = scroller.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            return true;
        }

        private static Transform FindContent(Transform view)
        {
            Transform scroller = view.Find("_Scroller1");
            if (scroller == null)
                return null;
            Transform viewport = scroller.Find("Viewport");
            return viewport != null ? viewport.Find("Content") : null;
        }

        private static bool IsLayoutValid(Transform view)
        {
            Transform scroller = view.Find("_Scroller1");
            Transform viewport = scroller != null ? scroller.Find("Viewport") : null;
            Transform content = viewport != null ? viewport.Find("Content") : null;
            ScrollRect scroll = scroller != null ? scroller.GetComponent<ScrollRect>() : null;
            RectTransform viewportRt = viewport as RectTransform;
            RectTransform contentRt = content as RectTransform;
            RectMask2D mask = viewport != null ? viewport.GetComponent<RectMask2D>() : null;
            GridLayoutGroup grid = content != null ? content.GetComponent<GridLayoutGroup>() : null;
            ContentSizeFitter fitter = content != null ? content.GetComponent<ContentSizeFitter>() : null;
            RectTransform viewRt = view as RectTransform;

            return viewRt != null && viewRt.sizeDelta == new Vector2(720f, 997f)
                && viewportRt != null && mask != null && contentRt != null
                && grid != null
                && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                && grid.constraintCount == 4
                && grid.cellSize == new Vector2(148f, 173f)
                && grid.spacing == new Vector2(0f, 0f)
                && fitter != null
                && fitter.verticalFit == ContentSizeFitter.FitMode.PreferredSize
                && scroll != null && scroll.viewport == viewportRt && scroll.content == contentRt
                && !scroll.horizontal && scroll.vertical;
        }

        private static void SetCentered(
            RectTransform rt,
            float width,
            float height,
            bool active)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, height);
            rt.gameObject.SetActive(active);
        }

        private static void SetTopLeft(
            RectTransform rt,
            float x,
            float y,
            float width,
            float height)
        {
            rt.anchorMin = Vector2.up;
            rt.anchorMax = Vector2.up;
            rt.pivot = Vector2.up;
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}
