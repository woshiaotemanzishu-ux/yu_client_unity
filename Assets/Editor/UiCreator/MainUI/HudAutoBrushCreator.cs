using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 从 HudSkillCreator 拆出的斩妖挂机区域;root 收成实际占位(左下 90×136,x=26/bottom=262);
    /// 布局数值全部来自运行时快照,拆分未改动。
    ///
    /// 关键技术决定:
    ///   #2 _img_progress 需要左中锚点(0,0.5),因为 MainUIAutoBrushView.SetProgressWidth 直接改
    ///      sizeDelta.x,中心锚点会导致进度条从两侧往中间缩,左锚点才会保持左边不动、只缩右边。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   _img_bg(斩妖挂机主图)      -> AutoBrushIconBg
    //   _img_bg2                  -> TopBadgeBg
    //   _img_bg3                  -> LevelBadgeBg
    //   _lb_level                 -> LevelLabel
    //   _img_bg4                  -> ProgressBarBg
    //   _img_progress             -> ProgressBarFill
    //   _box_effect               -> CompletionEffectSlot
    //   _box_auto_level           -> AutoToggleButton
    //   _img_auto_level           -> AutoToggleBg
    //   _lb_auto_level            -> AutoToggleLabel
    //   _img_red(斩妖挂机主图红点) -> IconRedDot
    //   click_gp                  -> OpenAutoBrushButton
    //   _img_red2                 -> ToggleRedDot
    //   _box_challenge_effect     -> __DynamicResources/ChallengeEffectSlot
    public static class HudAutoBrushCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudAutoBrush.prefab";

        // ---------- 斩妖挂机(MainUIAutoBrushView) ----------
        private const string IMG_AB_BG = "resource/game/mainUI/texture/ui_zy_14.png";
        private const string IMG_AB_BG2 = "resource/game/mainUI/texture/ui_zy_15.png";
        private const string IMG_AB_BG3 = "resource/game/mainUI/texture/ui_zy_16.png";
        private const string IMG_AB_BG4 = "resource/game/mainUI/texture/ui_zy_17.png";
        private const string IMG_AB_PROGRESS = "resource/game/mainUI/texture/ui_zy_18.png"; // 老端 sizeGrid="2,2,3,2"(九宫格),本轮未动导入设置,见报告
        private const string IMG_AB_AUTO_LEVEL = "resource/game/mainUI/texture/ui_zy_19.png";
        private const string IMG_AB_RED = "resource/game/mainUI/texture/com_red_point.png";

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudAutoBrush(斩妖挂机)",
                Note = "左下斩妖挂机入口(关卡徽章+进度条+自动闯关钮),有界 root 90×136",
                Order = 41,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        /// <summary>
        /// 子根锚定:x=26 / bottom=262(720×1280 设计画布左下角起算),整体再叠加 scale 0.9。
        /// centerX = -DesignWidth/2 + x + w/2 = -360 + 26 + 45 = -289。
        /// centerY = -DesignHeight/2 + bottom + h/2 = -640 + 262 + 68 = -310。
        /// </summary>
        public static void Generate()
        {
            // 整棵树在 root 未激活时构建(对标 Login/RoleCreate 样板的安全建树习惯);建完再激活。
            RectTransform root = UiCreatorKit.NewRoot("HudAutoBrush");
            root.anchorMin = root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0f, 0f);
            root.sizeDelta = new Vector2(90f, 136f);
            root.anchoredPosition = new Vector2(26f, 262f);
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("MainUIAutoBrushView", root);
            UiCreatorKit.Stretch(viewRoot);
            viewRoot.localScale = new Vector3(0.9f, 0.9f, 1f);
            viewRoot.gameObject.AddComponent<CanvasGroup>();
            var view = viewRoot.gameObject.AddComponent<MainUIAutoBrushView>();

            Image bg = UiCreatorKit.NewImage("AutoBrushIconBg", viewRoot); // 老端: _img_bg
            UiCreatorKit.Place(bg.rectTransform, 8f, 9f, 90f, 90f);
            UiCreatorKit.TrySetSprite(bg, IMG_AB_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = bg;

            Image bg2 = UiCreatorKit.NewImage("TopBadgeBg", viewRoot); // 老端: _img_bg2
            UiCreatorKit.Place(bg2.rectTransform, -2f, 44f, 58f, 32f);
            UiCreatorKit.TrySetSprite(bg2, IMG_AB_BG2, UiCreatorKit.Palette.Panel);
            view._img_bg2 = bg2;

            Image bg3 = UiCreatorKit.NewImage("LevelBadgeBg", viewRoot); // 老端: _img_bg3
            UiCreatorKit.Place(bg3.rectTransform, 1f, -3f, 68f, 18f);
            UiCreatorKit.TrySetSprite(bg3, IMG_AB_BG3, UiCreatorKit.Palette.Panel);
            view._img_bg3 = bg3;

            TextMeshProUGUI lbLevel = UiCreatorKit.NewText("LevelLabel", bg3.transform, "第1关"); // 老端: _lb_level
            UiCreatorKit.Place(lbLevel.rectTransform, -2f, -1f, 48f, 16f);
            lbLevel.fontSize = 16f;
            lbLevel.color = new Color(0f, 250f / 255f, 100f / 255f); // 对标老端 #00fa64
            view._lb_level = lbLevel;

            Image bg4 = UiCreatorKit.NewImage("ProgressBarBg", viewRoot); // 老端: _img_bg4
            UiCreatorKit.Place(bg4.rectTransform, 1f, -17f, 68f, 8f);
            UiCreatorKit.TrySetSprite(bg4, IMG_AB_BG4, UiCreatorKit.Palette.Panel);
            view._img_bg4 = bg4;

            // #2 左中锚点(0,0.5):SetProgressWidth 直接改 sizeDelta.x,需要左边不动、只缩右边。
            Image progress = UiCreatorKit.NewImage("ProgressBarFill", bg4.transform); // 老端: _img_progress
            PlaceLeftCenter(progress.rectTransform, 0f, 0f, 68f, 8f);
            UiCreatorKit.TrySetSprite(progress, IMG_AB_PROGRESS, UiCreatorKit.Palette.BtnPrimary);
            view._img_progress = progress;

            RectTransform boxEffect = UiCreatorKit.NewNode("CompletionEffectSlot", viewRoot); // 老端: _box_effect
            UiCreatorKit.Place(boxEffect, 1f, 18f, 100f, 100f);
            view._box_effect = boxEffect;

            RectTransform boxAutoLevel = UiCreatorKit.NewNode("AutoToggleButton", viewRoot); // 老端: _box_auto_level
            UiCreatorKit.Place(boxAutoLevel, 1f, -40f, 102f, 36f);
            view._box_auto_level = boxAutoLevel;

            Image autoLevelImg = UiCreatorKit.NewImage("AutoToggleBg", boxAutoLevel); // 老端: _img_auto_level
            UiCreatorKit.Place(autoLevelImg.rectTransform, 0f, 0f, 102f, 36f);
            UiCreatorKit.TrySetSprite(autoLevelImg, IMG_AB_AUTO_LEVEL, UiCreatorKit.Palette.BtnSecond);
            view._img_auto_level = autoLevelImg;

            TextMeshProUGUI lbAutoLevel = UiCreatorKit.NewText("AutoToggleLabel", boxAutoLevel, "自动闯关"); // 老端: _lb_auto_level
            UiCreatorKit.Place(lbAutoLevel.rectTransform, -2f, 1f, 80f, 20f);
            lbAutoLevel.fontSize = 20f;
            lbAutoLevel.color = Color.white;
            view._lb_auto_level = lbAutoLevel;

            Image red = UiCreatorKit.NewImage("IconRedDot", viewRoot); // 老端: _img_red
            UiCreatorKit.Place(red.rectTransform, 26f, 54.5f, 26f, 27f);
            UiCreatorKit.TrySetSprite(red, IMG_AB_RED, UiCreatorKit.Palette.Mark);
            view._img_red = red;

            // OpenAutoBrushButton 是纯命中容器(无 skin),UIUtil.AddClick 对纯 RectTransform 容器会在运行时
            // 自动补透明命中 Image(见 Framework/UI/UIUtil.ResolveContainerRaycastTarget),建树期不用补图。
            RectTransform clickGp = UiCreatorKit.NewNode("OpenAutoBrushButton", viewRoot); // 老端: click_gp
            UiCreatorKit.Place(clickGp, 8f, 9f, 90f, 90f);
            view.click_gp = clickGp;

            Image red2 = UiCreatorKit.NewImage("ToggleRedDot", viewRoot); // 老端: _img_red2
            UiCreatorKit.Place(red2.rectTransform, 33f, -27.5f, 26f, 27f);
            UiCreatorKit.TrySetSprite(red2, IMG_AB_RED, UiCreatorKit.Palette.Mark);
            view._img_red2 = red2;

            RectTransform dynamicResources = UiCreatorKit.NewNode("__DynamicResources", viewRoot);
            UiCreatorKit.Stretch(dynamicResources);
            RectTransform challengeEffect = UiCreatorKit.NewNode("ChallengeEffectSlot", dynamicResources); // 老端: _box_challenge_effect
            UiCreatorKit.Place(challengeEffect, 2f, 39f, 128f, 128f);
            UIEffectSlot challengeSlot = challengeEffect.gameObject.AddComponent<UIEffectSlot>();
            challengeSlot.ConfigureEffect(
                MainUIAutoBrushView.CHALLENGE_EFFECT_SLOT_ID,
                "ui_mainDungeon",
                GameResPath.GetUIEffectPrefabPath("ui_mainDungeon"),
                "yu_client MainUIAutoBrushView.InitChallengeEffect",
                "斩妖进度完成特效;由 MainUIAutoBrushView 按 current_times == need_times 手动消费",
                Vector2.zero,
                Vector3.one,
                0f);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudAutoBrush.prefab 已生成: " + PrefabPath);
        }

        /// <summary>左中锚点+左中枢轴摆位,给需要"改宽度只缩右边"的进度条用(见 #2)。</summary>
        private static void PlaceLeftCenter(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        // ===================== 预览 =====================

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudAutoBrush",
                    "请先进入 Play 模式(主界面已加载、UI 层已初始化)再点预览。\n\n" +
                    "预览会把最新 HudAutoBrush.prefab 实例化到 Main 层,并调用 MainUIAutoBrushView.Show()," +
                    "仅用于看结构/试交互。",
                    "好");
                return;
            }

            if (_previewInstance != null)
            {
                Object.Destroy(_previewInstance);
                _previewInstance = null;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[UiCreator] 未找到 " + PrefabPath + ",请先点生成。");
                return;
            }

            Transform layer = ViewManager.GetLayer(UILayer.Main);
            _previewInstance = Object.Instantiate(prefab, layer);
            _previewInstance.name = "HudAutoBrush(Preview)";

            var view = _previewInstance.GetComponentInChildren<MainUIAutoBrushView>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] HudAutoBrush 预览实例缺少 MainUIAutoBrushView 组件");
                return;
            }
            view.Show();
        }
    }
}
