using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.LookOver.Views;

namespace Shenxiao.Editor.UiCreator.LookOver
{
    /// <summary>
    /// 【他人资料卡】LookOverCardView 独立 prefab 纯代码建树生成器(轮21 §2 PL,module 1 基本装备)。
    ///
    /// 无死树可嫁接(Unity 全仓此前无 PlayerMessage/LookOver 任何资产,见侦察 r21_lookover.md §5/§6),
    /// 走"简单 Creator"打法(同 TransferJobCreator/Login 家族):直接 AddComponent
    /// <see cref="LookOverCardView"/> 具体类并在建树期精确赋字段,不依赖 LayaBindFiller/Manifest。
    ///
    /// 贴图复用已导入的 common 共享图(未逐一核对与老端 playerMessage 原图是否一致,按用户新标准
    /// "能点能用即可,不求像素级"从简):bg(common/other/bg_03.png)/ 标题条(common/texture/com_title_bg1.png)/
    /// 关闭按钮(common/texture/com_close.png)。贴不到则 TrySetSprite 自动占位色,不留白。
    ///
    /// 产物:Assets/Prefabs/UI/LookOver/LookOverCardView.prefab(根挂 LookOverCardView 组件,独立弹窗,
    /// 对标 TransferJobCardView.prefab 惯例)。<see cref="Shenxiao.Module.Core.LookOver.LookOverFlow"/>
    /// 用 GetUIPrefab("LookOver","LookOverCardView") 加载。入口:「神霄/重构UI 生成器」面板 + GenerateBatch。
    /// 真机包前记得跑「神霄/资源/Addressable 自动分组」。
    /// </summary>
    public static class LookOverCardCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/LookOver/LookOverCardView.prefab";

        private const string IMG_PANEL_BG = "resource/game/common/other/bg_03.png";
        private const string IMG_TITLE_BG = "resource/game/common/texture/com_title_bg1.png";
        private const string IMG_CLOSE_BTN = "resource/game/common/texture/com_close.png";

        private static readonly Color TitleColor = Hex("#ffe4b0");
        // 面板底(common/other/bg_03.png)实测是浅色/近白底(见 CliVerify 截图),下面几项文字必须走深色,
        // 否则浅字浅底不可读——"能点能用"里"能用"包含起码的可读性,不是只求不崩。
        private static readonly Color NameColor = Hex("#332a1a");
        private static readonly Color GrayColor = Hex("#6b6558");
        private static readonly Color OrangeColor = Hex("#b5590a");
        private static readonly Color RowColor = Hex("#4a4536");

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "LookOver",
                Name = "LookOverCardView(他人资料卡·module1)",
                Note = "640x900 独立弹窗:标题/关闭 + 加载态 + 姓名/服务器/角色ID/战力/成就阶 + 装备/法阵/仙灵纯文本列表;" +
                       "无死树,纯代码建树。数据源 19502(FriendModel.PlayerCard/EVT_PLAYER_CARD)",
                Order = 10,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            const float W = 640f, H = 900f;

            RectTransform root = UiCreatorKit.NewNode("LookOverCardView", null);
            root.gameObject.SetActive(false);
            UiCreatorKit.Place(root, 0f, 0f, W, H);
            var view = root.gameObject.AddComponent<LookOverCardView>();

            Img(root, "Bg", IMG_PANEL_BG, 0f, 0f, W, H, W, H, sliced: true);

            const float titleW = 360f;
            Img(root, "TitleBg", IMG_TITLE_BG, (W - titleW) / 2f, 20f, titleW, 46f, W, H, sliced: true);
            view.lblTitle = Lbl(root, "lblTitle", "角色资料卡", (W - titleW) / 2f, 20f, titleW, 46f, 26,
                TitleColor, W, H, FontStyles.Bold, align: TextAlignmentOptions.Center);

            RectTransform spClose = UiCreatorKit.NewNode("spClose", root);
            PlaceLaya(spClose, W - 64f, 16f, 44f, 44f, W, H);
            Image closeImg = spClose.gameObject.AddComponent<Image>();
            closeImg.raycastTarget = true;
            UiCreatorKit.TrySetSprite(closeImg, IMG_CLOSE_BTN, UiCreatorKit.Palette.BtnNeutral);
            view.spClose = closeImg;

            view.lblLoading = Lbl(root, "lblLoading", "加载中...", 0f, H / 2f - 20f, W, 40f, 24,
                NameColor, W, H, align: TextAlignmentOptions.Center);

            RectTransform infoGroup = UiCreatorKit.NewNode("infoGroup", root);
            PlaceLaya(infoGroup, 0f, 96f, W, 220f, W, H);
            view.infoGroup = infoGroup.gameObject;

            const float infoW = W;
            view.lblName = Lbl(infoGroup, "lblName", "", 24f, 0f, infoW - 48f, 40f, 28, NameColor, infoW, 220f,
                FontStyles.Bold, align: TextAlignmentOptions.Left);
            view.lblServer = Lbl(infoGroup, "lblServer", "", 24f, 46f, infoW - 48f, 32f, 20, GrayColor, infoW, 220f,
                align: TextAlignmentOptions.Left);
            view.lblRoleId = Lbl(infoGroup, "lblRoleId", "", 24f, 82f, infoW - 48f, 32f, 20, GrayColor, infoW, 220f,
                align: TextAlignmentOptions.Left);
            view.lblCombat = Lbl(infoGroup, "lblCombat", "", 24f, 118f, infoW - 48f, 32f, 20, OrangeColor, infoW, 220f,
                align: TextAlignmentOptions.Left);
            view.lblAchv = Lbl(infoGroup, "lblAchv", "", 24f, 154f, infoW - 48f, 32f, 20, OrangeColor, infoW, 220f,
                align: TextAlignmentOptions.Left);

            view.listDetail = BuildScrollList(root, "listDetail", 24f, 336f, W - 48f, H - 366f, W, H);

            BuildTemplates(root, view);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] LookOverCardView.prefab 已生成: " + PrefabPath +
                "(贴图为 common 共享图,不求与老端 playerMessage 原图逐一对应;真机包前记得跑 Addressable 自动分组)");
        }

        // ---------------------------------------------------------------- 模板(__Templates)

        private static void BuildTemplates(RectTransform root, LookOverCardView view)
        {
            RectTransform templates = UiCreatorKit.NewNode("__Templates", root);
            UiCreatorKit.Place(templates, 0f, 0f, 100f, 100f);
            templates.gameObject.SetActive(false);
            view.rowTemplate = BuildRowTemplate(templates);
        }

        private static GameObject BuildRowTemplate(Transform parent)
        {
            const float RW = 560f, RH = 40f;
            RectTransform root = NewTopLeftNode("DetailRow", parent, RW, RH);

            TextMeshProUGUI lbl = UiCreatorKit.NewText("lbl", root, "");
            UiCreatorKit.StretchPadding(lbl.rectTransform, 4f, 2f);
            lbl.fontSize = 20;
            lbl.color = RowColor;
            lbl.alignment = TextAlignmentOptions.Left;

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ---------------------------------------------------------------- 列表(Content 挂 VerticalLayoutGroup+ContentSizeFitter,
        // 对标 Docs/UI分辨率适配手册.md 铁律6:布局归 prefab,不手摆 anchoredPosition)

        private static ScrollRect BuildScrollList(Transform parent, string name, float x, float y, float w, float h,
            float parentW, float parentH)
        {
            RectTransform root = UiCreatorKit.NewNode(name, parent);
            PlaceLaya(root, x, y, w, h, parentW, parentH);
            ScrollRect sr = root.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = UiCreatorKit.NewNode("Viewport", root);
            UiCreatorKit.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.001f); // 近乎透明,仅用于承接遮罩

            RectTransform content = UiCreatorKit.NewNode("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, h);

            VerticalLayoutGroup vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 4f;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = viewport;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 30f;
            return sr;
        }

        // ---------------------------------------------------------------- 布局/建元素 helper(同 TransferJobCreator 约定)

        private static RectTransform NewTopLeftNode(string name, Transform parent, float w, float h)
        {
            RectTransform rt = UiCreatorKit.NewNode(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        /// <summary>Laya 左上原点(默认 anchorX=anchorY=0)→ Unity 中心锚,公式同 TransferJobCreator.PlaceLaya。</summary>
        private static void PlaceLaya(RectTransform rt, float x, float y, float w, float h, float parentW, float parentH)
        {
            float cx = x + w / 2f - parentW / 2f;
            float cy = -(y + h / 2f - parentH / 2f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        private static Image Img(Transform parent, string name, string skin, float x, float y, float w, float h,
            float parentW, float parentH, bool sliced = false)
        {
            Image img = UiCreatorKit.NewImage(name, parent);
            PlaceLaya(img.rectTransform, x, y, w, h, parentW, parentH);
            img.raycastTarget = false;
            bool got = !string.IsNullOrEmpty(skin) && UiCreatorKit.TrySetSprite(img, skin, UiCreatorKit.Palette.Panel);
            if (!got) img.color = UiCreatorKit.Palette.Panel;
            if (got && sliced) img.type = Image.Type.Sliced;
            return img;
        }

        private static TextMeshProUGUI Lbl(Transform parent, string name, string text, float x, float y, float w, float h,
            float fontSize, Color color, float parentW, float parentH, FontStyles style = FontStyles.Normal,
            bool wrap = false, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            TextMeshProUGUI t = UiCreatorKit.NewText(name, parent, text);
            PlaceLaya(t.rectTransform, x, y, w, h, parentW, parentH);
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            if (wrap) t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;

        // ---------------------------------------------------------------- 批处理 / 预览

        /// <summary>批处理入口(-executeMethod,不依赖 [MenuItem]):
        /// Unity.exe -batchmode -projectPath . -executeMethod
        ///   Shenxiao.Editor.UiCreator.LookOver.LookOverCardCreator.GenerateBatch -logFile Temp/lookover_creator.log</summary>
        public static void GenerateBatch()
        {
            try
            {
                Generate();
                bool ok = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
                Debug.Log("[UiCreator] LookOverCardCreator.GenerateBatch " + (ok ? "OK " : "FAILED ") + PrefabPath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] LookOverCardCreator.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 LookOverCardView",
                    "请先进入 Play 模式(游戏已起、lookover addressable 可用)再点预览。\n\n" +
                    "预览走 LookOverFlow.Show 同一条加载路径,需要真实点一次「点头像」入口才会实际发 19501/收 19502,\n" +
                    "本预览只加载空面板(加载中态),不会主动造假数据。",
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

            string key = GameResPath.GetUIPrefab("LookOver", "LookOverCardView");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            if (go == null)
            {
                Debug.LogWarning("[UiCreator] LookOverCardView 预览加载失败: " + key + "(检查 addressable/是否已生成)");
                return;
            }
            var view = go.GetComponent<LookOverCardView>();
            if (view == null)
            {
                Debug.LogWarning("[UiCreator] LookOverCardView 预览缺组件(重跑生成)");
                Object.Destroy(go);
                return;
            }
            _previewInstance = go;
            view.Show(0L);
        }
    }
}
