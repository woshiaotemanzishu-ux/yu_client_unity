using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 从 HudSkillCreator 拆出的摇杆区域;root=200×200 编辑器占位,运行时转盘会被代码挪到触点
    /// (root 只是坐标系)。布局数值全部来自运行时快照,拆分未改动。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   _gp_root                  -> JoystickDial
    //   _img_bg(摇杆底图)          -> DialBg
    //   _img_arrow                -> DirectionArrow
    //   _img_middle_circle        -> KnobImage
    public static class HudJoystickCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudJoystick.prefab";

        // ---------- 摇杆(UIJoyStick) ----------
        private const string IMG_JOY_BG = "resource/game/mainUI/texture/mainui_base_bg.png"; // 快照运行时真实贴图(设计态是占位 com_empty.png)
        private const string IMG_JOY_ARROW = "resource/game/mainUI/texture/mainui_joy_stick_arrow.png";
        private const string IMG_JOY_KNOB = "resource/game/mainUI/texture/mainui_touch_img.png";

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudJoystick(摇杆)",
                Note = "左下摇杆转盘;root=200×200 编辑器占位,运行时转盘会被代码挪到触点(root 只是坐标系)",
                Order = 42,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        /// <summary>
        /// 子根锚定:左下角,默认禁用(任务未给精确数值,按 200×200 尺寸留边距估一个,可手调)。
        /// _gp_root 的快照原始位置换算下来是 (-100,+100)(锚点在自身中心,x/y=0 时紧贴父节点左上角,
        /// 三象限探出父节点外)——但 UIJoyStick.OnInit 一开始就把它禁用,Update 里每次显示前都会先用
        /// SceneInput.StartScreen 重新赋值 anchoredPosition,建树期这个默认值永远不会被看到,
        /// 所以简化成与父节点同尺寸居中(0,0,200,200),不复刻这个视觉上无意义的偏移。
        /// </summary>
        public static void Generate()
        {
            // 整棵树在 root 未激活时构建(对标 Login/RoleCreate 样板的安全建树习惯);建完再激活。
            RectTransform root = UiCreatorKit.NewRoot("HudJoystick");
            root.anchorMin = root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0f, 0f);
            root.sizeDelta = new Vector2(200f, 200f);
            root.anchoredPosition = new Vector2(40f, 40f);
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("UIJoyStick", root);
            UiCreatorKit.Stretch(viewRoot);
            var view = viewRoot.gameObject.AddComponent<UIJoyStick>();

            RectTransform gpRoot = UiCreatorKit.NewNode("JoystickDial", viewRoot); // 老端: _gp_root
            UiCreatorKit.Place(gpRoot, 0f, 0f, 200f, 200f);
            view._gp_root = gpRoot;

            Image bg = UiCreatorKit.NewImage("DialBg", gpRoot); // 老端: _img_bg
            UiCreatorKit.Place(bg.rectTransform, 0f, 0f, 196f, 196f);
            UiCreatorKit.TrySetSprite(bg, IMG_JOY_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = bg;

            Image arrow = UiCreatorKit.NewImage("DirectionArrow", gpRoot); // 老端: _img_arrow
            UiCreatorKit.Place(arrow.rectTransform, 0f, 95f, 124f, 50f);
            UiCreatorKit.TrySetSprite(arrow, IMG_JOY_ARROW, UiCreatorKit.Palette.BtnSecond);
            arrow.gameObject.SetActive(false); // 对标快照/设计源 vis=false(方向箭头当前业务代码未使用)
            view._img_arrow = arrow;

            Image knob = UiCreatorKit.NewImage("KnobImage", gpRoot); // 老端: _img_middle_circle
            UiCreatorKit.Place(knob.rectTransform, 0.5f, 0f, 89f, 92f);
            UiCreatorKit.TrySetSprite(knob, IMG_JOY_KNOB, UiCreatorKit.Palette.BtnPrimary);
            view._img_middle_circle = knob;

            // 对标原 root.gameObject.SetActive(false):摇杆默认隐藏,按下场景空白处才显示。
            // 注意禁用的是这个视图子根(UIJoyStick),不是区域 root——区域 root 建完要正常激活存盘。
            viewRoot.gameObject.SetActive(false);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudJoystick.prefab 已生成: " + PrefabPath);
        }

        // ===================== 预览 =====================

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudJoystick",
                    "请先进入 Play 模式(主界面已加载、UI 层已初始化)再点预览。\n\n" +
                    "预览会把最新 HudJoystick.prefab 实例化到 Main 层,并调用 UIJoyStick.Show()," +
                    "但摇杆默认隐藏,预览 Show 后仍需按住屏幕才会现身。",
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
            _previewInstance.name = "HudJoystick(Preview)";

            var view = _previewInstance.GetComponentInChildren<UIJoyStick>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] HudJoystick 预览实例缺少 UIJoyStick 组件");
                return;
            }
            view.Show();
        }
    }
}
