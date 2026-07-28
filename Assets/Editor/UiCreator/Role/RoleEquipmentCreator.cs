using Shenxiao.Editor.UiCreator;
using Shenxiao.Module.Core.Role;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Role
{
    /// <summary>
    /// 人物主面板的可重复装配器。转换产物保留老端坐标，本装配器只补 Unity 布局组件与默认显隐，
    /// 让运行时只刷新数据/显隐，不再按设备尺寸二次写 anchoredPosition。
    /// </summary>
    public static class RoleEquipmentCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Role/RoleModule.prefab";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Role",
                Name = "RoleModule(人物主面板布局)",
                Note = "补齐人物属性两列网格、战力居中与默认显隐；不重转原始 Laya 坐标。",
                Order = 9,
                Generate = Generate,
                PrefabPath = PrefabPath,
            });
        }

        [MenuItem("神霄/重构UI/生成/Role/人物主面板布局")]
        public static void Generate()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + PrefabPath);
                return;
            }

            try
            {
                EquipmentView view = root.GetComponentInChildren<EquipmentView>(true);
                if (view == null)
                {
                    Debug.LogError("[UiCreator] RoleModule 缺 EquipmentView");
                    return;
                }

                ConfigureAttributeGrid(view);
                ConfigureFightLayout(view);
                if (view._img_title_base != null) view._img_title_base.gameObject.SetActive(true);
                if (view._img_title_best != null) view._img_title_best.gameObject.SetActive(false);
                if (view.worldGp != null) view.worldGp.gameObject.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[UiCreator] RoleModule 人物主面板布局已更新: " + PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureAttributeGrid(EquipmentView view)
        {
            if (view._Scroller1 == null || view._Scroller1.content == null) return;
            RectTransform content = view._Scroller1.content;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(600f, content.sizeDelta.y);

            GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(content.gameObject);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.cellSize = new Vector2(300f, 38f);
            grid.spacing = new Vector2(0f, 5f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ConfigureFightLayout(EquipmentView view)
        {
            if (view._gp_fight == null) return;
            HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(view._gp_fight.gameObject);
            layout.padding = new RectOffset();
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T value = go.GetComponent<T>();
            return value != null ? value : go.AddComponent<T>();
        }
    }
}
