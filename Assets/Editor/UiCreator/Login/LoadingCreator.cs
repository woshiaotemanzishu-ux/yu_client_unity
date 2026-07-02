using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Login;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 资源加载页(重构版)纯代码建树生成器。
    ///
    /// 结构对标老 LoginLoadingViewSkin.scene + 截图:
    ///   全屏龙纹立绘背景(load_bg)/ 左上 version 文本 /
    ///   底部进度条(条底图 _img_back + Filled 横向填充前景 _img_front + 进度端标 _img_progress_end)/
    ///   进度文案 "loading……N%" / 首次加载提示。
    /// 挂 LoadingView 并回填全部 public 引用,存 Assets/Prefabs/UI/Login/LoadingView.prefab。
    /// 元素已贴老端真实源图,贴不到回退占位色。尺寸/位置为 720×1280 起步值,自行在编辑器调。
    /// 入口在「神霄/重构UI 生成器」面板(Module=Login)。
    /// </summary>
    public static class LoadingCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Login/LoadingView.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)
        // 背景:老端运行时按 _preload_bg_id 加载 load_bgN.jpg,这里建树期贴一张做可见底图(运行时可由 SetImage 覆盖)。
        private const string IMG_BG = "resource/game/login/other/full_screen_bg.jpg";
        private const string IMG_BAR_BACK = "resource/game/login/other/uidl_015.png";   // 进度条底图(Skin scene compId6)
        private const string IMG_BAR_FRONT = "resource/game/login/other/uidl_016.png";  // 进度条前景填充(Skin scene _front_img)
        private const string IMG_PROGRESS_END = "resource/game/login/texture/ui_progress_end.png"; // 进度端标

        // 进度条满宽(对标老端 front_img_width=635)
        private const float BarFullWidth = 635f;
        private const float BarHeight = 28f;
        private const float BarBottomY = 150f;   // 进度条距底部高度(起步值)

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Login",
                Name = "LoadingView(资源加载页)",
                Note = "全屏立绘 + 底部进度条(Filled) + 进度/版本文案",
                Order = 10,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再激活(与 LoginPanelCreator 同范式)。
            RectTransform root = UiCreatorKit.NewRoot("LoadingView");
            root.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<LoadingView>();

            // ---------- 全屏背景立绘 ----------
            Image bg = UiCreatorKit.NewImage("Bg", root);
            UiCreatorKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_BG, UiCreatorKit.Palette.Bg);
            view.bg = bg;

            // ---------- 左上 version 文本 ----------
            TextMeshProUGUI version = UiCreatorKit.NewText("VersionLabel", root, "version 1.0.0");
            RectTransform vrt = version.rectTransform;
            vrt.anchorMin = vrt.anchorMax = vrt.pivot = new Vector2(0f, 1f); // 左上角锚
            vrt.sizeDelta = new Vector2(320f, 40f);
            vrt.anchoredPosition = new Vector2(20f, -20f);
            version.alignment = TextAlignmentOptions.TopLeft;
            version.fontSize = 24f;
            view.versionLabel = version;

            // ---------- 底部进度区:首次加载提示 + 进度条 + 进度文案 ----------
            // 用一个底部对齐的容器承载,便于整体上移/排版。
            RectTransform conBox = UiCreatorKit.NewNode("ConBox", root);
            conBox.anchorMin = new Vector2(0.5f, 0f);
            conBox.anchorMax = new Vector2(0.5f, 0f);
            conBox.pivot = new Vector2(0.5f, 0f);
            conBox.sizeDelta = new Vector2(UiCreatorKit.DesignWidth, 220f);
            conBox.anchoredPosition = new Vector2(0f, BarBottomY - 60f);

            // 首次加载提示(静态文案,放进度条上方)
            TextMeshProUGUI firstLoad = UiCreatorKit.NewText(
                "FirstLoadLabel", conBox, "首次加载可能较慢,请耐心等待。");
            UiCreatorKit.Place(firstLoad.rectTransform, 0f, 150f, 680f, 30f);
            firstLoad.alignment = TextAlignmentOptions.Center;
            firstLoad.fontSize = 24f;
            view.firstLoadLabel = firstLoad;

            // 进度条底图(BarBack)——居中,作为进度条的视觉底。
            Image barBack = UiCreatorKit.NewImage("BarBack", conBox);
            UiCreatorKit.Place(barBack.rectTransform, 0f, 90f, BarFullWidth, BarHeight);
            barBack.raycastTarget = false;
            UiCreatorKit.TrySetSprite(barBack, IMG_BAR_BACK, UiCreatorKit.Palette.Panel);
            view.progressBack = barBack;

            // 进度条前景(BarFront)——铺满底图,Image type=Filled / Horizontal / fillOrigin=Left。
            // 用左对齐锚(pivot.x=0)使 fillAmount 从左向右增长在视觉上与端标对齐。
            Image barFront = UiCreatorKit.NewImage("BarFront", barBack.transform);
            RectTransform frontRt = barFront.rectTransform;
            frontRt.anchorMin = new Vector2(0f, 0.5f);
            frontRt.anchorMax = new Vector2(0f, 0.5f);
            frontRt.pivot = new Vector2(0f, 0.5f);
            frontRt.sizeDelta = new Vector2(BarFullWidth, BarHeight);
            frontRt.anchoredPosition = new Vector2(-BarFullWidth * 0.5f, 0f); // 左边缘对齐底图左边缘
            barFront.raycastTarget = false;
            barFront.type = Image.Type.Filled;
            barFront.fillMethod = Image.FillMethod.Horizontal;
            barFront.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFront.fillAmount = 0f;
            UiCreatorKit.TrySetSprite(barFront, IMG_BAR_FRONT, UiCreatorKit.Palette.BtnPrimary);
            view.progressFront = barFront;

            // 进度端标(ProgressEnd)——挂在 BarFront(左原点)下,View 用 maskWidth-55 定位,与老端一致。
            Image progressEnd = UiCreatorKit.NewImage("ProgressEnd", barFront.transform);
            RectTransform endRt = progressEnd.rectTransform;
            endRt.anchorMin = new Vector2(0f, 0.5f);
            endRt.anchorMax = new Vector2(0f, 0.5f);
            endRt.pivot = new Vector2(0.5f, 0.5f);
            endRt.sizeDelta = new Vector2(40f, 40f);
            endRt.anchoredPosition = new Vector2(PROGRESS_END_NEAR_X, 0f);
            progressEnd.raycastTarget = false;
            UiCreatorKit.TrySetSprite(progressEnd, IMG_PROGRESS_END, UiCreatorKit.Palette.Mark);
            view.progressEnd = progressEnd;

            // 进度文案(放进度条下方)
            TextMeshProUGUI progressLabel = UiCreatorKit.NewText("ProgressLabel", conBox, "loading……0%");
            UiCreatorKit.Place(progressLabel.rectTransform, 0f, 40f, 680f, 30f);
            progressLabel.alignment = TextAlignmentOptions.Center;
            progressLabel.fontSize = 24f;
            view.progressLabel = progressLabel;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] LoadingView.prefab 已生成: " + PrefabPath +
                      "(可经 ViewManager.Open<LoadingView>() 加载;真机包前记得跑 Addressable 自动分组)");
        }

        // 与 LoadingView 内常量保持一致(进度端标贴左修正起步 x)。
        private const float PROGRESS_END_NEAR_X = -20f;

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 LoadingView",
                    "请先进入 Play 模式(登录场景已起、UI 层已初始化)再点预览。\n\n" +
                    "预览经 ViewManager.Open<LoadingView>() 加载新 prefab,仅用于看结构/试交互。",
                    "好");
                return;
            }
            // 先释放上次预览缓存的实例,确保每次都从【最新】prefab 重新实例化。
            ViewManager.Dispose<LoadingView>();
            _ = ViewManager.Open<LoadingView>();
        }
    }
}
