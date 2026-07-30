using System.Threading.Tasks;
using Shenxiao.Module.Core.Login;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>公共顶栏与登录背景图片必须由 Prefab 直接持有，运行时不得加载或替换图片资源。</summary>
    public static class PrefabBackgroundOwnershipCase
    {
        private const string CommonPrefabPath = "Assets/Prefabs/UI/Common/BaseWindowSkin.prefab";
        private const string LoginPrefabPath = "Assets/Prefabs/UI/Login/LoginStage.prefab";
        private const string RemovedLoginCreatorPath = "Assets/Editor/UiCreator/Login/LoginStageCreator.cs";

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
            float spriteAspect = webImage != null && webImage.sprite != null && webImage.sprite.rect.height > 0f
                ? webImage.sprite.rect.width / webImage.sprite.rect.height
                : 0f;

            bool loginOk = login != null
                && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(login) == 0
                && webImage != null
                && webImage.sprite != null
                && !webImage.raycastTarget
                && fitter != null
                && fitter.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent
                && stage != null
                && stage.webBackground == webImage
                && stage.backgroundFitter == fitter
                && stage.viewport != null
                && Mathf.Approximately(fitter.aspectRatio, spriteAspect)
                && AssetDatabase.LoadAssetAtPath<MonoScript>(RemovedLoginCreatorPath) == null;

            bool pass = commonOk && loginOk;
            Debug.Log("CLIVERIFY prefab-backgrounds common=" + commonOk
                + " login=" + loginOk
                + " commonSprite=" + (topImage != null && topImage.sprite != null ? topImage.sprite.name : "null")
                + " loginSprite=" + (webImage != null && webImage.sprite != null ? webImage.sprite.name : "null")
                + " loginAspect=" + (fitter != null ? fitter.aspectRatio.ToString("0.####") : "null")
                + " spriteAspect=" + spriteAspect.ToString("0.####")
                + " pass=" + pass);
            return Task.FromResult(pass ? 0 : 3);
        }

        private static Transform FindDeep(Transform root, string nodeName)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == nodeName) return child;
            return null;
        }
    }
}
