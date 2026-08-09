using System;
using Shenxiao.Editor.LayaUI;
using Shenxiao.Generated.UI.Title;
using Shenxiao.Module.Core.Common;
using TMPro;
using Shenxiao.Module.Core.Medal;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Role
{
    /// <summary>
    /// 天境首次落袋入口。只允许首次从同账号运行时快照生成 TitleMainView；生成后立即清除
    /// 快照里的运行时列表影子、恢复共享组件身份，并把可调布局固化到 Prefab。
    /// </summary>
    public static class TitleMainViewPipeline
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Title/TitleMainView.prefab";
        private const string AwardPath = "Assets/Prefabs/UI/Common/BaseAwardItem.prefab";
        private const string FightingPath = "Assets/Prefabs/UI/Common/FightingShowSmallItem.prefab";

        public static void RunCli()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                string report = LayaSceneConverter.BakeModuleFromManifest(
                    "Tools/ModuleManifest/title.manifest.json",
                    "Tools/ModuleManifest/snapshots/title",
                    "Title",
                    "TitleMainView");
                Debug.Log(report);
                if (report.Contains("failed=1") || report.Contains("baked=0"))
                    throw new InvalidOperationException("TitleMainView 首次烤制失败: " + report);

                UpgradePrefab();
                VerifyPrefab();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("CLIVERIFY title-prefab PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void UpgradePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                TitleMainViewBind bind = root.GetComponent<TitleMainViewBind>();
                if (bind == null) throw new InvalidOperationException("TitleMainViewBind 未挂载");

                RectTransform rootRect = root.transform as RectTransform;
                rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0f, 1f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = new Vector2(720f, 992f);
                rootRect.localScale = Vector3.one;

                ReplaceShared(bind.item_gp, "BaseAwardItem", AwardPath, Vector2.zero);
                ReplaceShared(bind._gp_fight, "FightingShowSmallItem", FightingPath,
                    new Vector2(265f, 0f));

                if (bind.Content == null || bind.Content.content == null)
                    throw new InvalidOperationException("天境横向 Content 未绑定");
                ClearChildren(bind.Content.content);
                ConfigureHorizontalList(bind.Content);

                RectTransform attrContent = bind._Scroller1 != null && bind._Scroller1.content != null
                    ? bind._Scroller1.content
                    : bind.Content1;
                if (attrContent == null) throw new InvalidOperationException("天境属性 Content1 未绑定");
                bind.Content1 = attrContent;
                ClearChildren(attrContent);
                ConfigureVerticalList(bind._Scroller1, attrContent);
                ConfigureAttributeTemplate(bind._tpl_TitleAttrItem);

                DisableEmptyImages(bind.title_effect_gp);
                DisableEmptyImages(bind.success_effect_gp);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ReplaceShared(RectTransform parent, string childName, string prefabPath,
            Vector2 topLeft)
        {
            if (parent == null) throw new InvalidOperationException(childName + " 宿主未绑定");
            for (int i = parent.childCount - 1; i >= 0; i--)
                if (parent.GetChild(i).name == childName)
                    UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (source == null) throw new InvalidOperationException("共享 Prefab 缺失: " + prefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            instance.name = childName;
            RectTransform rect = instance.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
                rect.localScale = Vector3.one;
            }
            instance.SetActive(true);
        }

        private static void ClearChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void ConfigureHorizontalList(ScrollRect scroll)
        {
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            EnsureMask(scroll.viewport);
            RectTransform content = scroll.content;
            RemoveLayoutGroups(content);
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            // 老端 HBox 的 TitleItem 顶边与 viewport 顶边重合。MiddleLeft 会在转换后的
            // 100 高 Content 中把 182 高卡片上移 41px，标题因此落到 RectMask2D 外。
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 0f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private static void ConfigureVerticalList(ScrollRect scroll, RectTransform content)
        {
            if (scroll != null)
            {
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                EnsureMask(scroll.viewport);
            }
            RemoveLayoutGroups(content);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 0f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ConfigureAttributeTemplate(GameObject template)
        {
            if (template == null) throw new InvalidOperationException("TitleAttrItem 模板未绑定");
            TitleAttrItemBind bind = template.GetComponent<TitleAttrItemBind>();
            if (bind == null) throw new InvalidOperationException("TitleAttrItemBind 未挂载");
            ConfigureRuntimeHtmlText(bind.now_attr_lb, 177f);
            ConfigureRuntimeHtmlText(bind.next_attr_lb, 140f);
        }

        private static void ConfigureRuntimeHtmlText(TextMeshProUGUI text, float width)
        {
            if (text == null) throw new InvalidOperationException("天境属性文字未绑定");
            text.fontSize = 20f;
            text.richText = true;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            RectTransform rect = text.rectTransform;
            rect.sizeDelta = new Vector2(width, 20f);
        }

        private static void RemoveLayoutGroups(RectTransform content)
        {
            LayoutGroup[] groups = content.GetComponents<LayoutGroup>();
            for (int i = 0; i < groups.Length; i++) UnityEngine.Object.DestroyImmediate(groups[i]);
        }

        private static void EnsureMask(RectTransform viewport)
        {
            if (viewport != null && viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
        }

        private static void DisableEmptyImages(RectTransform host)
        {
            if (host == null) return;
            foreach (Image image in host.GetComponentsInChildren<Image>(true))
                if (image.sprite == null) image.enabled = false;
        }

        private static void VerifyPrefab()
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (root == null) throw new InvalidOperationException("天境 Prefab 未生成");
            TitleMainView view = root.GetComponent<TitleMainView>();
            if (view == null) throw new InvalidOperationException("天境业务 View 未挂载");
            if (view.Content == null || view.Content.content == null || view.Content.viewport == null
                || view.Content.viewport.GetComponent<RectMask2D>() == null)
                throw new InvalidOperationException("天境横向滚动结构不完整");
            if (view._Scroller1 == null || view._Scroller1.content == null
                || view._Scroller1.viewport == null
                || view._Scroller1.viewport.GetComponent<RectMask2D>() == null)
                throw new InvalidOperationException("天境属性滚动结构不完整");
            if (view.Content1 != view._Scroller1.content)
                throw new InvalidOperationException("天境 Content1 未指向属性 ScrollRect.content");
            if (view._tpl_TitleItem == null || view._tpl_TitleItem.GetComponent<TitleItem>() == null)
                throw new InvalidOperationException("TitleItem 模板/业务组件缺失");
            if (view._tpl_TitleAttrItem == null || view._tpl_TitleAttrItem.GetComponent<TitleAttrItem>() == null)
                throw new InvalidOperationException("TitleAttrItem 模板/业务组件缺失");
            TitleAttrItemBind attr = view._tpl_TitleAttrItem.GetComponent<TitleAttrItemBind>();
            if (attr.now_attr_lb == null || attr.next_attr_lb == null
                || attr.now_attr_lb.fontSize < 19.5f || attr.next_attr_lb.fontSize < 19.5f)
                throw new InvalidOperationException("TitleAttrItem 运行时 HTMLDiv 字号未恢复");
            if (view.item_gp == null || view.item_gp.GetComponentInChildren<BaseAwardItem>(true) == null)
                throw new InvalidOperationException("BaseAwardItem 共享实例缺失");
            if (view._gp_fight == null || view._gp_fight.GetComponentInChildren<FightingShowSmallItem>(true) == null)
                throw new InvalidOperationException("FightingShowSmallItem 共享实例缺失");
        }
    }
}
