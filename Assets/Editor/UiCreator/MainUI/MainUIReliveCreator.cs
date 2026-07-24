using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 复活倒计时窗(MainUIReliveView)独立 prefab 生成器。
    ///
    /// 几何、贴图和 Bind 只维护在 <see cref="HudOverlayCombatCreator.BuildRelive"/> 这一棵建树函数；
    /// 本 Creator 直接调用并把 MainUIReliveView 本体保存为独立 prefab，不依赖已经退役的
    /// HudOverlayCombat bundle。MainUIFlow.ShowReliveAsync 按精确地址单独加载该 prefab。
    /// </summary>
    public static class MainUIReliveCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/MainUIReliveView.prefab";

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "MainUIReliveView(复活倒计时窗·独立)",
                Note = "直接生成独立 MainUIReliveView；不依赖 HudOverlayCombat bundle",
                Order = 73,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            RectTransform buildRoot = UiCreatorKit.NewRoot("__MainUIReliveBuildRoot");
            buildRoot.gameObject.SetActive(false);
            RectTransform root = HudOverlayCombatCreator.BuildRelive(buildRoot);
            root.SetParent(null, false);
            root.gameObject.SetActive(true);
            Object.DestroyImmediate(buildRoot.gameObject);

            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] MainUIReliveView.prefab 已生成: " + PrefabPath +
                      "(独立建树,不依赖 HudOverlayCombat;MainUIFlow.ShowReliveAsync 可直接加载;" +
                      "真机包前记得跑一次 Addressable 自动分组)");
        }

        /// <summary>
        /// 批处理入口(供 -executeMethod 调用,不依赖 [MenuItem]/交互面板):
        ///   Unity.exe -batchmode -projectPath . -executeMethod
        ///     Shenxiao.Editor.UiCreator.MainUI.MainUIReliveCreator.GenerateBatch -logFile Temp/mainui_relive_creator.log
        /// 生成成功且 prefab 资产确实落盘 → Exit(0);否则 Exit(1)。
        /// </summary>
        public static void GenerateBatch()
        {
            try
            {
                Generate();
                bool ok = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
                Debug.Log("[UiCreator] MainUIReliveCreator.GenerateBatch " + (ok ? "OK " : "FAILED ") + PrefabPath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] MainUIReliveCreator.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 MainUIReliveView",
                    "请先进入 Play 模式(游戏已起、mainUI addressable 可用)再点预览。\n\n" +
                    "预览走 MainUIFlow.ShowReliveAsync 同一条加载路径(GetUIPrefab(\"mainUI\",\"MainUIReliveView\") + " +
                    "InstantiateAsync + Show()),仅用于看结构/倒计时表现,不发真实复活协议。",
                    "好");
                return;
            }
            _ = PreviewAsync();
        }

        private static async Task PreviewAsync()
        {
            if (_previewInstance != null)
            {
                Object.Destroy(_previewInstance);
                _previewInstance = null;
            }

            string key = GameResPath.GetUIPrefab("mainUI", "MainUIReliveView");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            if (go == null)
            {
                Debug.LogWarning("[UiCreator] MainUIReliveView 预览加载失败: " + key + "(检查 addressable/是否已生成)");
                return;
            }
            MainUIReliveView view = go.GetComponent<MainUIReliveView>();
            if (view == null)
            {
                Debug.LogWarning("[UiCreator] MainUIReliveView 预览缺组件(重跑生成)");
                Object.Destroy(go);
                return;
            }
            _previewInstance = go;
            view.Show(); // 无参:走本地默认倒计时(_defaultReliveSeconds=5,对标老端 GetReliveDuration()=5)
        }
    }
}
