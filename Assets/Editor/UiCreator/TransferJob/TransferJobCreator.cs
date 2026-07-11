using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.TransferJob;

namespace Shenxiao.Editor.UiCreator.TransferJob
{
    /// <summary>
    /// 【转职卡】TransferJobCardView 独立 prefab 纯代码建树生成器(轮5 新增;无死树可嫁接,全新烤制)。
    ///
    /// 几何源 = 老端 <c>cdn/resource/game/transferJob/{TransferJobCardView,TransferJobCardItem}.json</c>
    /// 设计值(无运行时快照可用,本模块从未跑过转换/烤制流水线):数值均取自这两份 json 的
    /// props.x/y/width/height,Laya 左上原点(默认 anchorX=anchorY=0)经 <see cref="PlaceLaya"/> 换算成
    /// Unity 中心锚,公式同 SettingCreator/PetCreator 的 PlaceLaya(本类未接 <c>LayaRectMath</c> 的
    /// centerX/left/right 等复杂分支——两份 json 里的节点全部是纯 x/y,无需那些分支)。
    ///
    /// 贴图:resource/game/transferJob/texture/ 下的老端源图(bg_54.png/com_bg_02.png/com_title_bg_1.png/
    /// close_btn.png/career_{c}_{s}.png ×4/award_light_bg2.png)本仓库 Assets/GameRes 尚未导入(自查确认,
    /// 部分如 award_light_bg2.png 连老端自己的资源目录都没有);TrySetSprite 缺图自动回退占位色,不留白。
    /// 待美术批量导入 transferJob 贴图后,重跑本 Creator 即可自动贴上真图(不用改代码)。
    ///
    /// 直接 AddComponent 具体 View 类(TransferJobCardView/TransferJobCardItem 已存在且编译通过)并在建树期
    /// 赋字段引用,同 PetCreator/OutWardBaseView 手法——不依赖 LayaBindFiller/LayaUIManifest(那条链路
    /// 面向"从 .scene 转换生成的 Bind 类"批量回填,本 Creator 直接精确赋值,幂等、无 manifest 依赖风险)。
    ///
    /// 产物:Assets/Prefabs/UI/TransferJob/TransferJobCardView.prefab(根挂 TransferJobCardView 组件,
    /// 对标 MainUIReliveView.prefab"根即 View"的独立弹窗惯例;<see cref="TransferJobFlow"/>
    /// 用 GetUIPrefab("TransferJob","TransferJobCardView") 加载)。
    /// 入口:「神霄/重构UI 生成器」面板 + GenerateBatch(-executeMethod)。真机包前记得跑「神霄/资源/Addressable 自动分组」。
    /// </summary>
    public static class TransferJobCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/TransferJob/TransferJobCardView.prefab";

        // ---- 老端源图(resource/game/transferJob/texture,尚未导入本仓库;TrySetSprite 缺图占位色+告警) ----
        private const string IMG_PANEL_BG = "resource/game/common/texture/bg_03.png";                     // 面板底(共享 common 图,已导入)
        private const string IMG_AWARD_LIGHT = "resource/game/transferJob/texture/award_light_bg2.png";    // 装饰光(老端源目录亦缺此文件,恒占位)
        private const string IMG_TITLE_BG = "resource/game/transferJob/texture/com_title_bg_1.png";
        private const string IMG_CLOSE_BTN = "resource/game/transferJob/texture/close_btn.png";
        private const string IMG_ITEM_BG = "resource/game/transferJob/texture/bg_54.png";
        private const string IMG_ITEM_BG_INNER = "resource/game/transferJob/texture/com_bg_02.png";
        private const string IMG_BTN_RECT11 = "resource/game/common/texture/ui_button_rect11.png";         // 转职按钮底(共享 common 图)

        private static readonly Color TitleOrange = Hex("#b94a00");
        private static readonly Color DescRed = Hex("#fe1a1a");
        private static readonly Color ItemDescBrown = Hex("#663915");
        private static readonly Color ItemTypeRed = Hex("#cf2b2c");
        private static readonly Color SureWhite = Hex("#ffffff");

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "TransferJob",
                Name = "TransferJobCardView(转职卡)",
                Note = "转职卡列表窗 626x538 + 转职卡项 558x120(职业图/说明/类型/转职按钮);无死树,纯代码建树(几何=老端 json 设计值)",
                Order = 90,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            const float W = 626f, H = 538f;

            RectTransform root = UiCreatorKit.NewNode("TransferJobCardView", null);
            root.gameObject.SetActive(false);
            UiCreatorKit.Place(root, 0f, 0f, W, H);
            var view = root.gameObject.AddComponent<TransferJobCardView>();

            Img(root, "Image_4", IMG_PANEL_BG, 0f, 15f, 603f, 523f, W, H, sliced: true);
            Img(root, "Image_7", IMG_AWARD_LIGHT, 24f, 467f, 64f, 64f, W, H);
            Img(root, "Image_3", IMG_TITLE_BG, 121f, 30f, 363f, 36f, W, H, sliced: true);

            view.lblTitle = Lbl(root, "lblTitle", "转职卡", 190f, 30f, 246f, 36f, 22, TitleOrange, W, H,
                FontStyles.Bold, align: TextAlignmentOptions.Center);

            view.lblDesc = Lbl(root, "lblDesc",
                "专职后穿戴中装备及套装石及相应职业内容均跟随变换，背包和仓库 中装备及套装石不跟随变化",
                47f, 462f, 539f, 60f, 18, DescRed, W, H, FontStyles.Bold, wrap: true);

            view.listTransfer = BuildScrollList(root, "listTransfer", 24f, 78f, 558f, 368f, W, H);

            RectTransform spClose = UiCreatorKit.NewNode("spClose", root);
            PlaceLaya(spClose, 574f, 0f, 44f, 44f, W, H);
            Image closeImg = spClose.gameObject.AddComponent<Image>();
            closeImg.raycastTarget = true;
            UiCreatorKit.TrySetSprite(closeImg, IMG_CLOSE_BTN, UiCreatorKit.Palette.BtnNeutral);
            view.spClose = spClose;

            BuildTemplates(root, view);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] TransferJobCardView.prefab 已生成: " + PrefabPath +
                "(几何=老端 json 设计值,贴图缺失已占位色告警;真机包前记得跑 Addressable 自动分组)");
        }

        // ---------------------------------------------------------------- 模板(__Templates)

        private static void BuildTemplates(RectTransform root, TransferJobCardView view)
        {
            RectTransform templates = UiCreatorKit.NewNode("__Templates", root);
            UiCreatorKit.Place(templates, 0f, 0f, 100f, 100f);
            templates.gameObject.SetActive(false);
            view._tpl_TransferJobCardItem = BuildItemTemplate(templates);
        }

        private static GameObject BuildItemTemplate(Transform parent)
        {
            const float IW = 558f, IH = 120f;
            RectTransform root = NewTopLeftNode("TransferJobCardItem", parent, IW, IH);
            var item = root.gameObject.AddComponent<TransferJobCardItem>();

            Img(root, "Image_10", IMG_ITEM_BG, 0f, 0f, IW, IH, IW, IH, sliced: true);
            // com_bg_02(职业图外框衬底):json 未给显式宽高,按 imgJob(62x96,x37,y11)外扩估算,贴真图后可再调。
            Img(root, "Image_11", IMG_ITEM_BG_INNER, 11f, 8f, 84f, 104f, IW, IH);

            RectTransform btnSureRt = UiCreatorKit.NewNode("btnSure", root);
            // json 未给 btnSure 显式宽高,按 ui_button_rect11.png 常规按钮尺寸估(116x46),贴真图后可再调。
            PlaceLaya(btnSureRt, 434f, 38f, 116f, 46f, IW, IH);
            Image btnSureImg = btnSureRt.gameObject.AddComponent<Image>();
            btnSureImg.raycastTarget = true;
            UiCreatorKit.TrySetSprite(btnSureImg, IMG_BTN_RECT11, UiCreatorKit.Palette.BtnPrimary);
            item.btnSure = btnSureImg;
            item.lblTransfer = Lbl(btnSureRt, "lblTransfer", "转职", 26f, 10f, 70f, 30f, 24, SureWhite, 116f, 46f, FontStyles.Bold);

            // imgJob:运行时按 career_{career}_{sex} 动态贴(TransferJobCardItem.SetData),建树期占位色。
            item.imgJob = Img(root, "imgJob", null, 37f, 11f, 62f, 96f, IW, IH);
            item.lblDesc = Lbl(root, "lblDesc", "正义必将降临 失败无需畏惧", 136f, 70f, 400f, 30f, 20, ItemDescBrown, IW, IH, FontStyles.Bold);
            item.lblType = Lbl(root, "lblType", "高攻·肉盾", 138f, 29f, 200f, 30f, 20, ItemTypeRed, IW, IH, FontStyles.Bold);

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ---------------------------------------------------------------- 列表(无 LayoutGroup,运行时手摆——同 SettingCreator.NewScrollPage)

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
            vpImg.color = new Color(1f, 1f, 1f, 0.001f); // 近乎透明,仅用于承接遮罩(常规 UGUI ScrollRect 做法)

            RectTransform content = UiCreatorKit.NewNode("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(w, h);

            sr.viewport = viewport;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 30f;
            return sr;
        }

        // ---------------------------------------------------------------- 布局/建元素 helper(同 SettingCreator/PetCreator 约定)

        private static RectTransform NewTopLeftNode(string name, Transform parent, float w, float h)
        {
            RectTransform rt = UiCreatorKit.NewNode(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        /// <summary>Laya 左上原点(默认 anchorX=anchorY=0)→ Unity 中心锚,公式同 SettingCreator/PetCreator.PlaceLaya。</summary>
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
        ///   Shenxiao.Editor.UiCreator.TransferJob.TransferJobCreator.GenerateBatch -logFile Temp/transferjob_creator.log</summary>
        public static void GenerateBatch()
        {
            try
            {
                Generate();
                bool ok = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
                Debug.Log("[UiCreator] TransferJobCreator.GenerateBatch " + (ok ? "OK " : "FAILED ") + PrefabPath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] TransferJobCreator.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 TransferJobCardView",
                    "请先进入 Play 模式(游戏已起、transferjob addressable 可用)再点预览。\n\n" +
                    "预览走 TransferJobFlow.Show 同一条加载路径,列表数据需要 config_career/ClientTransfer\n" +
                    "同步进 Assets/GameRes 才会非空(未同步时列表为空,这不是布局问题)。",
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

            string key = GameResPath.GetUIPrefab("TransferJob", "TransferJobCardView");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            if (go == null)
            {
                Debug.LogWarning("[UiCreator] TransferJobCardView 预览加载失败: " + key + "(检查 addressable/是否已生成)");
                return;
            }
            var view = go.GetComponent<TransferJobCardView>();
            if (view == null)
            {
                Debug.LogWarning("[UiCreator] TransferJobCardView 预览缺组件(重跑生成)");
                Object.Destroy(go);
                return;
            }
            _previewInstance = go;
            view.Show();
        }
    }
}
