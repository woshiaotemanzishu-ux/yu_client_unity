using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Module.Core.Login;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>公共顶栏与登录背景必须由 Prefab 直接持有，不能退回运行时或页面 Creator 管理。</summary>
    public static class PrefabBackgroundOwnershipCase
    {
        private const string CommonPrefabPath = "Assets/Prefabs/UI/Common/BaseWindowSkin.prefab";
        private const string LoginPrefabPath = "Assets/Prefabs/UI/Login/LoginStage.prefab";
        private static readonly string[] LoginPagePrefabPaths =
        {
            "Assets/Prefabs/UI/Login/LoginPanel.prefab",
            "Assets/Prefabs/UI/Login/RoleCreateView.prefab",
            "Assets/Prefabs/UI/Login/RoleSelectView.prefab",
            "Assets/Prefabs/UI/Login/ServerSelectView.prefab",
        };

        private static readonly string[] RemovedLoginCreatorPaths =
        {
            "Assets/Editor/UiCreator/Login/LoginStageCreator.cs",
            "Assets/Editor/UiCreator/Login/LoginPanelCreator.cs",
            "Assets/Editor/UiCreator/Login/RoleCreateCreator.cs",
            "Assets/Editor/UiCreator/Login/RoleSelectCreator.cs",
            "Assets/Editor/UiCreator/Login/ServerSelectCreator.cs",
        };

        public static Task<int> Run()
        {
            GameObject common = AssetDatabase.LoadAssetAtPath<GameObject>(CommonPrefabPath);
            Transform top = FindDeep(common != null ? common.transform : null, "TopBackground");
            Transform titleArt = FindDeep(common != null ? common.transform : null, "_img_title_bg");
            Image topImage = top != null ? top.GetComponent<Image>() : null;
            bool commonOk = common != null
                && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(common) == 0
                && topImage != null
                && topImage.sprite != null
                && !topImage.raycastTarget
                && titleArt != null
                && top.parent == titleArt.parent
                && top.GetSiblingIndex() < titleArt.GetSiblingIndex();

            GameObject login = AssetDatabase.LoadAssetAtPath<GameObject>(LoginPrefabPath);
            Transform web = FindDeep(login != null ? login.transform : null, "WebBackground");
            Image webImage = web != null ? web.GetComponent<Image>() : null;
            AspectRatioFitter fitter = web != null ? web.GetComponent<AspectRatioFitter>() : null;
            LoginStage stage = login != null ? login.GetComponent<LoginStage>() : null;
            bool hasRuntimeBackgroundField = false;
            foreach (FieldInfo field in typeof(LoginStage).GetFields(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                hasRuntimeBackgroundField |= typeof(Image).IsAssignableFrom(field.FieldType)
                    || typeof(AspectRatioFitter).IsAssignableFrom(field.FieldType);
            }

            bool loginStageOk = login != null
                && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(login) == 0
                && webImage != null
                && webImage.sprite != null
                && !webImage.raycastTarget
                && fitter != null
                && fitter.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent
                && stage != null
                && stage.viewport != null
                && !hasRuntimeBackgroundField;

            bool loginPagesOk = true;
            foreach (string prefabPath in LoginPagePrefabPaths)
                loginPagesOk &= HasSerializedFullScreenBackground(prefabPath);

            bool creatorsRemoved = true;
            foreach (string creatorPath in RemovedLoginCreatorPaths)
                creatorsRemoved &= AssetDatabase.LoadAssetAtPath<MonoScript>(creatorPath) == null;

            bool pass = commonOk && loginStageOk && loginPagesOk && creatorsRemoved;
            Debug.Log("CLIVERIFY prefab-backgrounds common=" + commonOk
                + " loginStage=" + loginStageOk
                + " loginPages=" + loginPagesOk
                + " creatorsRemoved=" + creatorsRemoved
                + " commonSprite=" + (topImage != null && topImage.sprite != null ? topImage.sprite.name : "null")
                + " loginSprite=" + (webImage != null && webImage.sprite != null ? webImage.sprite.name : "null")
                + " runtimeBackgroundField=" + hasRuntimeBackgroundField
                + " pass=" + pass);
            return Task.FromResult(pass ? 0 : 3);
        }

        private static bool HasSerializedFullScreenBackground(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) != 0)
                return false;

            foreach (Image image in prefab.GetComponentsInChildren<Image>(true))
            {
                RectTransform rect = image.rectTransform;
                if (image.name == "Bg"
                    && image.sprite != null
                    && !image.raycastTarget
                    && Near(rect.anchorMin, Vector2.zero)
                    && Near(rect.anchorMax, Vector2.one))
                    return true;
            }
            return false;
        }

        private static Transform FindDeep(Transform root, string nodeName)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == nodeName) return child;
            return null;
        }

        private static bool Near(Vector2 a, Vector2 b) =>
            Mathf.Abs(a.x - b.x) < 0.001f && Mathf.Abs(a.y - b.y) < 0.001f;
    }
}
