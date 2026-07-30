using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Common
{
    /// <summary>
    /// BaseWindowSkin 公共窗框的幂等 Prefab 精修器。
    ///
    /// 老端 _img_title 不写 width/height，运行时换图使用图片原始尺寸；centerX=-66 则固定的是
    /// 标题中心。旧转换产物把锻造占位图的 132x40 当成了所有标题的固定 Rect，导致 72x44 的
    /// “背包”等标题横向拉伸。这里直接修整已落袋的公共 Prefab，不重新批量转换任何业务面板。
    /// </summary>
    public static class BaseWindowSkinRefiner
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Common/BaseWindowSkin.prefab";
        private static readonly Vector2 TitleAnchor = new Vector2(0.5f, 1f);
        private static readonly Vector2 TitlePivot = new Vector2(0.5f, 1f);
        private static readonly Vector2 TitlePosition = new Vector2(-66f, -11f);
        private static readonly Vector2 PlaceholderSize = new Vector2(132f, 40f);

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Common",
                Name = "BaseWindowSkin(公共窗框标题精修)",
                Note = "精修现有公共 Prefab：标题按原图尺寸显示并固定中心；不运行 Laya 批量转换",
                Order = 5,
                Generate = () => Generate(),
                PrefabPath = PrefabPath,
            });
        }

        public static bool Generate()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + PrefabPath);
                return false;
            }

            try
            {
                Transform titleNode = FindDeep(root.transform, "_img_title");
                if (titleNode == null || !(titleNode is RectTransform titleRect))
                {
                    Debug.LogError("[UiCreator] BaseWindowSkin 缺 _img_title RectTransform");
                    return false;
                }

                Image titleImage = titleNode.GetComponent<Image>();
                if (titleImage == null)
                {
                    Debug.LogError("[UiCreator] BaseWindowSkin/_img_title 缺 Image");
                    return false;
                }

                titleRect.anchorMin = TitleAnchor;
                titleRect.anchorMax = TitleAnchor;
                titleRect.pivot = TitlePivot;
                titleRect.anchoredPosition = TitlePosition;
                titleRect.sizeDelta = PlaceholderSize;
                titleImage.preserveAspect = true;
                titleImage.raycastTarget = false;

                // Unity HorizontalLayoutGroup 会在 SetNativeSize 后重新固定标题左边缘，造成窄标题
                // 向左漂移；老端这里固定的是 centerX，因此禁用这层单子项布局器并由 RectTransform
                // 保存最终视觉参数。
                HorizontalLayoutGroup layout = titleNode.parent != null
                    ? titleNode.parent.GetComponent<HorizontalLayoutGroup>()
                    : null;
                if (layout != null) layout.enabled = false;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            bool ok = Verify();
            Debug.Log("[UiCreator] BaseWindowSkinRefiner " + (ok ? "OK " : "FAILED ") + PrefabPath);
            if (ok)
            {
                GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Selection.activeObject = saved;
                EditorGUIUtility.PingObject(saved);
            }
            return ok;
        }

        private static bool Verify()
        {
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Transform titleNode = saved != null ? FindDeep(saved.transform, "_img_title") : null;
            RectTransform titleRect = titleNode as RectTransform;
            Image titleImage = titleNode != null ? titleNode.GetComponent<Image>() : null;
            HorizontalLayoutGroup layout = titleNode != null && titleNode.parent != null
                ? titleNode.parent.GetComponent<HorizontalLayoutGroup>()
                : null;

            return titleRect != null
                   && titleImage != null
                   && Nearly(titleRect.anchorMin, TitleAnchor)
                   && Nearly(titleRect.anchorMax, TitleAnchor)
                   && Nearly(titleRect.pivot, TitlePivot)
                   && Nearly(titleRect.anchoredPosition, TitlePosition)
                   && Nearly(titleRect.sizeDelta, PlaceholderSize)
                   && titleImage.preserveAspect
                   && !titleImage.raycastTarget
                   && (layout == null || !layout.enabled);
        }

        private static bool Nearly(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.001f && Mathf.Abs(a.y - b.y) < 0.001f;
        }

        private static Transform FindDeep(Transform root, string nodeName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == nodeName) return child;
            return null;
        }

        /// <summary>供 CI / 本地 batchmode 验证公共 Prefab 精修结果。</summary>
        public static void GenerateBatch()
        {
            try
            {
                EditorApplication.Exit(Generate() ? 0 : 1);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[UiCreator] BaseWindowSkinRefiner.GenerateBatch 异常: " + exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
