using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.PetEquip.Views;

namespace Shenxiao.Editor.UiCreator.PetEquip
{
    /// <summary>
    /// 【侍魂装备】内容模块纯代码建树生成器。
    ///
    /// PetEquipModule 只承载 BaseWindowSkin 内容区，根下固定三个 720x992 子页：背包、强化、打造。
    /// 运行时 PetEquipFlow 负责共享窗框、页签与页面显隐；每页只保留真实可操作的穿戴槽、背包/材料
    /// 列表和操作按钮，不制作 3D、大师、帮助等首版未接功能。
    ///
    /// 动态槽位和物品行以 disabled 模板保存在对应布局容器内，运行时克隆后直接取组件及已序列化字段，
    /// 不依赖节点名查找。视觉优先复用 common 共享图，缺图由 UiCreatorKit 回退到不透明占位色。
    /// </summary>
    public static class PetEquipCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/PetEquip/PetEquipModule.prefab";

        private const string IMG_PAGE_BG = "resource/game/common4/other/bg_07.png";
        private const string IMG_TITLE_BG = "resource/game/common/texture/com_title_bg1.png";
        private const string IMG_ACTION_BTN = "resource/game/common/texture/com_rect_btn1.png";
        private const string IMG_SECOND_BTN = "resource/game/common/texture/ui_button_rect11.png";

        private static readonly Color HeadingColor = Hex("#ffe5aa");
        private static readonly Color BodyColor = Hex("#49331f");
        private static readonly Color MutedColor = Hex("#75624d");
        private static readonly Color AccentColor = Hex("#b84d22");
        private static readonly Color RowBackground = Hex("#e8d7b8");
        private static readonly Color SlotBackground = Hex("#d6c09a");

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "PetEquip",
                Name = "PetEquipModule(侍魂装备·背包/强化/打造)",
                Note = "BaseWindowSkin 内容区：四个穿戴槽 + 背包/材料列表 + 穿戴、强化、打造真实操作；动态行均由 disabled 模板克隆",
                Order = 10,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            RectTransform root = UiCreatorKit.NewRoot("PetEquipModule");
            root.gameObject.SetActive(false);

            BuildPage(root, PetEquipPageMode.Bag, "PetEquipBagPage", "侍魂装备背包", "选择背包装备后可穿戴或替换", "穿戴", "背包暂无可穿戴装备");
            BuildPage(root, PetEquipPageMode.Strengthen, "PetEquipStrengthenPage", "侍魂装备强化", "选择穿戴中的目标与消耗装备", "强化", "暂无可用于强化的装备");
            BuildPage(root, PetEquipPageMode.Polish, "PetEquipPolishPage", "侍魂装备打造", "选择穿戴中的目标与一件打造材料", "打造", "暂无可用于打造的材料");

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] PetEquipModule.prefab 已生成: " + PrefabPath +
                      "(三个内容页；真机包前由资源工具统一跑 Addressable 自动分组)");
        }

        private static void BuildPage(Transform moduleRoot, PetEquipPageMode mode, string name,
            string heading, string summary, string action, string empty)
        {
            const float W = 720f;
            const float H = 992f;

            RectTransform pageRoot = UiCreatorKit.NewNode(name, moduleRoot);
            UiCreatorKit.Place(pageRoot, 0f, 0f, W, H);
            PetEquipPageView view = pageRoot.gameObject.AddComponent<PetEquipPageView>();
            view.mode = mode;

            Image pageBg = UiCreatorKit.NewImage("PageBackground", pageRoot);
            UiCreatorKit.Stretch(pageBg.rectTransform);
            pageBg.raycastTarget = true;
            UiCreatorKit.TrySetSprite(pageBg, IMG_PAGE_BG, UiCreatorKit.Palette.Panel);
            pageBg.type = Image.Type.Sliced;

            Image titleBg = UiCreatorKit.NewImage("HeadingBackground", pageRoot);
            PlaceTopLeft(titleBg.rectTransform, 180f, 18f, 360f, 48f, W, H);
            titleBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(titleBg, IMG_TITLE_BG, UiCreatorKit.Palette.BtnNeutral);
            titleBg.type = Image.Type.Sliced;

            view.lblHeading = Label(pageRoot, "lblHeading", heading, 180f, 18f, 360f, 48f, 28f,
                HeadingColor, W, H, TextAlignmentOptions.Center, FontStyles.Bold);
            view.lblCombat = Label(pageRoot, "lblCombat", "战力 0", 28f, 76f, 250f, 38f, 24f,
                AccentColor, W, H, TextAlignmentOptions.Left, FontStyles.Bold);
            view.lblSummary = Label(pageRoot, "lblSummary", summary, 278f, 76f, 414f, 38f, 20f,
                MutedColor, W, H, TextAlignmentOptions.Right);

            Label(pageRoot, "lblWornTitle", "已穿戴", 28f, 122f, 180f, 34f, 22f,
                BodyColor, W, H, TextAlignmentOptions.Left, FontStyles.Bold);
            view.wornContent = BuildWornContent(pageRoot, W, H, out PetEquipSlotRowView slotTemplate);
            view.slotTemplate = slotTemplate;

            Label(pageRoot, "lblGoodsTitle", mode == PetEquipPageMode.Bag ? "装备背包" : "可用材料",
                28f, 336f, 180f, 34f, 22f, BodyColor, W, H, TextAlignmentOptions.Left, FontStyles.Bold);
            BuildGoodsScroll(pageRoot, W, H, out RectTransform goodsContent, out PetEquipGoodsRowView goodsTemplate);
            view.goodsContent = goodsContent;
            view.goodsTemplate = goodsTemplate;

            view.lblEmpty = Label(pageRoot, "lblEmpty", empty, 80f, 555f, 560f, 48f, 22f,
                MutedColor, W, H, TextAlignmentOptions.Center);

            UiCreatorKit.ButtonParts selectAll = UiCreatorKit.NewButton("btnSelectAll", pageRoot, "全选材料");
            PlaceTopLeft(selectAll.root, 92f, 894f, 190f, 64f, W, H);
            UiCreatorKit.TrySetSprite(selectAll.bg, IMG_SECOND_BTN, UiCreatorKit.Palette.BtnSecond);
            selectAll.bg.type = Image.Type.Sliced;
            selectAll.bg.raycastTarget = true;
            selectAll.label.fontSize = 24f;
            selectAll.label.fontStyle = FontStyles.Bold;
            view.btnSelectAll = selectAll.bg;
            view.lblSelectAll = selectAll.label;
            selectAll.root.gameObject.SetActive(mode == PetEquipPageMode.Strengthen);

            UiCreatorKit.ButtonParts actionButton = UiCreatorKit.NewButton("btnAction", pageRoot, action);
            PlaceTopLeft(actionButton.root, mode == PetEquipPageMode.Strengthen ? 364f : 235f, 894f, 250f, 64f, W, H);
            UiCreatorKit.TrySetSprite(actionButton.bg, IMG_ACTION_BTN, UiCreatorKit.Palette.BtnPrimary);
            actionButton.bg.type = Image.Type.Sliced;
            actionButton.bg.raycastTarget = true;
            actionButton.label.fontSize = 26f;
            actionButton.label.fontStyle = FontStyles.Bold;
            view.btnAction = actionButton.bg;
            view.lblAction = actionButton.label;

            pageRoot.gameObject.SetActive(false);
        }

        private static RectTransform BuildWornContent(Transform parent, float parentW, float parentH,
            out PetEquipSlotRowView slotTemplate)
        {
            RectTransform content = UiCreatorKit.NewNode("wornContent", parent);
            PlaceTopLeft(content, 28f, 160f, 664f, 158f, parentW, parentH);

            HorizontalLayoutGroup layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            slotTemplate = BuildSlotTemplate(content);
            return content;
        }

        private static PetEquipSlotRowView BuildSlotTemplate(Transform parent)
        {
            const float W = 156f;
            const float H = 150f;

            RectTransform root = NewTopLeftNode("SlotTemplate", parent, W, H);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = W;
            layout.preferredHeight = H;

            PetEquipSlotRowView item = root.gameObject.AddComponent<PetEquipSlotRowView>();

            Image background = UiCreatorKit.NewImage("Background", root);
            UiCreatorKit.Stretch(background.rectTransform);
            background.color = SlotBackground;
            background.raycastTarget = false;
            item.background = background;

            Image click = UiCreatorKit.NewImage("Click", root);
            UiCreatorKit.Stretch(click.rectTransform);
            click.color = new Color(1f, 1f, 1f, 0.001f);
            click.raycastTarget = true;
            item.click = click;

            Image selected = UiCreatorKit.NewImage("SelectedMark", root);
            PlaceTopLeft(selected.rectTransform, W - 30f, 6f, 24f, 24f, W, H);
            selected.color = UiCreatorKit.Palette.Mark;
            selected.raycastTarget = false;
            selected.gameObject.SetActive(false);
            item.selectedMark = selected;

            item.lblPosition = Label(root, "lblPosition", "装备位", 8f, 12f, W - 16f, 36f, 22f,
                BodyColor, W, H, TextAlignmentOptions.Center, FontStyles.Bold);
            item.lblDetail = Label(root, "lblDetail", "未穿戴", 8f, 55f, W - 16f, 78f, 18f,
                MutedColor, W, H, TextAlignmentOptions.Center);
            item.lblDetail.textWrappingMode = TextWrappingModes.Normal;

            root.gameObject.SetActive(false);
            return item;
        }

        private static void BuildGoodsScroll(Transform parent, float parentW, float parentH,
            out RectTransform goodsContent, out PetEquipGoodsRowView goodsTemplate)
        {
            const float scrollW = 664f;
            const float scrollH = 520f;

            RectTransform scrollRoot = UiCreatorKit.NewNode("GoodsScroll", parent);
            PlaceTopLeft(scrollRoot, 28f, 374f, scrollW, scrollH, parentW, parentH);
            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();

            Image scrollBackground = scrollRoot.gameObject.AddComponent<Image>();
            scrollBackground.color = new Color(0.12f, 0.09f, 0.05f, 0.14f);
            scrollBackground.raycastTarget = true;

            RectTransform viewport = UiCreatorKit.NewNode("Viewport", scrollRoot);
            UiCreatorKit.StretchPadding(viewport, 8f, 8f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportHit = viewport.gameObject.AddComponent<Image>();
            viewportHit.color = new Color(1f, 1f, 1f, 0.001f);
            viewportHit.raycastTarget = true;

            goodsContent = UiCreatorKit.NewNode("goodsContent", viewport);
            goodsContent.anchorMin = new Vector2(0f, 1f);
            goodsContent.anchorMax = new Vector2(1f, 1f);
            goodsContent.pivot = new Vector2(0.5f, 1f);
            goodsContent.anchoredPosition = Vector2.zero;
            goodsContent.sizeDelta = new Vector2(0f, scrollH - 16f);

            VerticalLayoutGroup layout = goodsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = goodsContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = goodsContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            goodsTemplate = BuildGoodsTemplate(goodsContent);
        }

        private static PetEquipGoodsRowView BuildGoodsTemplate(Transform parent)
        {
            const float W = 632f;
            const float H = 90f;

            RectTransform root = NewTopLeftNode("GoodsTemplate", parent, W, H);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = H;

            PetEquipGoodsRowView item = root.gameObject.AddComponent<PetEquipGoodsRowView>();

            Image background = UiCreatorKit.NewImage("Background", root);
            UiCreatorKit.Stretch(background.rectTransform);
            background.color = RowBackground;
            background.raycastTarget = false;
            item.background = background;

            Image click = UiCreatorKit.NewImage("Click", root);
            UiCreatorKit.Stretch(click.rectTransform);
            click.color = new Color(1f, 1f, 1f, 0.001f);
            click.raycastTarget = true;
            item.click = click;

            Image selected = UiCreatorKit.NewImage("SelectedMark", root);
            PlaceTopLeft(selected.rectTransform, 12f, 25f, 40f, 40f, W, H);
            selected.color = UiCreatorKit.Palette.Mark;
            selected.raycastTarget = false;
            selected.gameObject.SetActive(false);
            item.selectedMark = selected;

            item.lblName = Label(root, "lblName", "装备名称", 68f, 10f, 330f, 34f, 22f,
                BodyColor, W, H, TextAlignmentOptions.Left, FontStyles.Bold);
            item.lblDetail = Label(root, "lblDetail", "部位 / 评分 / 阶星", 68f, 45f, 540f, 30f, 18f,
                MutedColor, W, H, TextAlignmentOptions.Left);

            root.gameObject.SetActive(false);
            return item;
        }

        private static RectTransform NewTopLeftNode(string name, Transform parent, float width, float height)
        {
            RectTransform rt = UiCreatorKit.NewNode(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private static void PlaceTopLeft(RectTransform rt, float x, float y, float width, float height,
            float parentWidth, float parentHeight)
        {
            float centerX = x + width * 0.5f - parentWidth * 0.5f;
            float centerY = -(y + height * 0.5f - parentHeight * 0.5f);
            UiCreatorKit.Place(rt, centerX, centerY, width, height);
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text,
            float x, float y, float width, float height, float fontSize, Color color,
            float parentWidth, float parentHeight, TextAlignmentOptions alignment,
            FontStyles fontStyle = FontStyles.Normal)
        {
            TextMeshProUGUI label = UiCreatorKit.NewText(name, parent, text);
            PlaceTopLeft(label.rectTransform, x, y, width, height, parentWidth, parentHeight);
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = fontStyle;
            return label;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }

        /// <summary>Unity batchmode 入口。</summary>
        public static void GenerateBatch()
        {
            try
            {
                Generate();
                bool ok = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
                Debug.Log("[UiCreator] PetEquipCreator.GenerateBatch " + (ok ? "OK " : "FAILED ") + PrefabPath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] PetEquipCreator.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 PetEquipModule",
                    "请先进入 Play 模式(UI 层已初始化)再点预览。\n\n" +
                    "预览仅展示背包内容页的静态结构，不创建假装备数据；真实入口由 PetEquipFlow 装配共享窗框并刷新协议数据。",
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
                Debug.LogError("[UiCreator] PetEquipModule 预览失败，找不到 " + PrefabPath + "（请先生成）");
                return;
            }

            _previewInstance = Object.Instantiate(prefab, ViewManager.GetLayer(UILayer.Window));
            _previewInstance.name = "PetEquipModule(Preview)";
            _previewInstance.SetActive(true);

            PetEquipPageView[] pages = _previewInstance.GetComponentsInChildren<PetEquipPageView>(true);
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].gameObject.SetActive(pages[i].mode == PetEquipPageMode.Bag);
            }
            Selection.activeObject = _previewInstance;
        }
    }
}
