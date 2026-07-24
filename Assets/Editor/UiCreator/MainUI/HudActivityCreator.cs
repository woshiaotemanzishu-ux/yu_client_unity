using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面「活动入口区」纯代码建树生成器(可编辑槽位版)。
    ///
    /// 设计原则(用户定):布局归 prefab、代码不碰布局。老端 Laya 用 RefreshIconPos 在代码里算坐标——那是 Laya 的搞法;
    /// Unity 里改成【分组手摆槽位】,运行时代码只把图标按配置 location_type/pos_index 填进对应组。绝不在运行时代码里计算坐标、间距、尺寸和行偏移;
    /// 所有布局都落在 prefab；HudActivity 只保留一份完整 ActivityIcon 模板，运行时按需克隆。
    ///
    /// 结构:
    ///   HudActivity(root)                 —— 有界:左上锚,定位(10,127),548×380,不再全屏
    ///     MainUIActivityView(view)         —— Stretch 填满 root,挂业务类
    ///       __Templates / ActivityIcon      —— 活动图标克隆模板(建完禁用)
    ///       IconGrid(_gp_con)
    ///         Group_ActivityOne / Slot_One_00..06
    ///         Group_ActivityTwo / Slot_Two_00..06
    ///         Group_ActivityOther / Slot_Other_00..06
    ///         Group_ActivityFourth / Slot_Fourth_00..05
    ///                                      —— 27 个分组手摆空槽，运行时克隆唯一模板填入
    ///
    /// 右上「竞榜/头号玩家榜」卡片(老端 _box_rank)已拆到独立区域 HudRank.prefab(见 HudRankCreator)。
    /// location_type=6/7/9 不再各建屏幕模块，运行时统一并入 Group_ActivityOther；location_type=10 进入第四组。
    /// 存 Assets/Prefabs/UI/MainUI/Regions/HudActivity.prefab,供人工核对后再并入 MainUIModule.prefab。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   _gp_con         -> IconGrid
    //   _img_turn       -> TurnDisk(收/展太极图标)
    //   _box_effect(ActivityIcon 模板内)  -> CompletionEffectSlot
    //   _img_icon(ActivityIcon 模板内)    -> IconImage
    //   _box_arrow(ActivityIcon 模板内)   -> GuideArrowSlot
    //   _img_red(ActivityIcon 模板内)     -> IconRedDot
    //   _img_desc_bg    -> DescriptionBarBg
    //   _img_red_num    -> RedNumberBadgeBg
    //   _lb_num         -> RedNumberLabel
    //   _lb_desc        -> DescriptionLabel
    // 注:_box_effect2 不改名,原样保留——ActivityIcon.EnsureEffectBox 运行时按此字面量名兜底查找。
    public static class HudActivityCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudActivity.prefab";
        private const string LeftPrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudActivityLeft.prefab";
        private const string RightPrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudActivityRight.prefab";
        private const string IconPrefabPath = "Assets/Prefabs/UI/MainUI/Components/ActivityIcon.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)。太极图(IMG_TURN)已移到 MainUIModuleCreator。
        private const string IMG_RED_DOT = "resource/game/mainUI/texture/com_red_point.png"; // 小红点
        private const string IMG_DESC_BG = "resource/game/mainUI/texture/mainui_ui_40.png";  // 图标下方文案条底图
        private const string IMG_RED_NUM_BG = "resource/game/mainUI/texture/ui_bq_03.png";   // 图标角标数字底图
        private const string IMG_TIP_BG = "resource/game/mainUI/texture/ui_First_16.png";    // TopPlayerTipItem 气泡底图
        private const string IMG_SAMPLE_ICON = "resource/game/icon/texture/151_1.png";       // 图标模板代表图(动态,运行时按配置换)

        // ---- 布局起步值(全部会落进 prefab 组件、供预制体里手调;代码里只在这里出现一次,建完不再有布局计算)----
        private const float GridCellW = 72f;     // ActivityIcon.WIDTH
        private const float GridCellH = 72f;     // ActivityIcon.HEIGHT
        private const float GridHGap = 5f;       // 老端 hgap
        private const float GridVGap = 20f;      // 老端 vgap(组内行距 + 组间竖距)

        // 区域外框(左上锚定在屏幕 (10,127) = 老端 _gp_con 原点;图标从这里起,与快照一像素不差)
        private static readonly Vector2 RegionOrigin = new Vector2(10f, 127f);
        private static readonly Vector2 RegionSize = new Vector2(548f, 380f);

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "ActivityIcon(活动入口模板)",
                Note = "唯一活动图标模板;图片、大小、描述条、红点、数字角标和特效都在这里精调",
                Order = 15,
                Generate = GenerateActivityIconPrefab,
                PrefabPath = IconPrefabPath,
            });

            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudActivity(活动入口区)",
                Note = "有界左上区 + 4 个逻辑组/27 个空槽；与左右活动位共用同一个 MainUIActivityView/模板",
                Order = 20,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });

            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudActivityLeft(左侧活动位)",
                Note = "location_type=4 的有界空槽区域；无独立逻辑、无模板，由 HudActivity 统一填充",
                Order = 21,
                Generate = GenerateLeft,
                PrefabPath = LeftPrefabPath,
            });

            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudActivityRight(右侧活动位)",
                Note = "location_type=5 的有界横条槽区域；无独立逻辑、无模板，由 HudActivity 统一填充",
                Order = 22,
                Generate = GenerateRight,
                PrefabPath = RightPrefabPath,
            });
        }

        public static void Generate()
        {
            GameObject iconPrefab = EnsureActivityIconPrefab();
            if (iconPrefab == null)
            {
                Debug.LogError("[UiCreator] HudActivity 生成中止:无法生成或加载 " + IconPrefabPath);
                return;
            }

            RectTransform root = UiCreatorKit.NewRoot("HudActivity");
            // root 收成活动网格的实际占位(左上区),不再全屏 Stretch;并入总装时锚点非全屏 → offset 归零自动跳过。
            AnchorTopLeft(root, RegionOrigin.x, RegionOrigin.y, RegionSize.x, RegionSize.y);
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("MainUIActivityView", root);
            UiCreatorKit.Stretch(viewRoot); // 填满有界 root
            var view = viewRoot.gameObject.AddComponent<MainUIActivityView>();

            // 隐藏模板挂载点:活动图标模板是纯克隆源,不应出现在可见容器里。
            RectTransform templates = NewTemplatesWrapper(viewRoot);
            GameObject tplIcon = InstantiateActivityIcon(iconPrefab, templates, "ActivityIcon");
            AnchorTopLeft((RectTransform)tplIcon.transform, 0f, 0f, GridCellW, GridCellH);
            tplIcon.SetActive(false);
            view._tpl_ActivityIcon = tplIcon;

            // ================= IconGrid(老端 _gp_con):【分组槽位容器】,运行时按 location_type/pos_index 填图标 =================
            RectTransform gpCon = UiCreatorKit.NewNode("IconGrid", viewRoot); // 老端: _gp_con
            UiCreatorKit.Stretch(gpCon); // 填满 view(槽位相对它左上定位)
            view._gp_con = gpCon;

            // 【槽位基线】恢复老端 FormatIconList 的四个逻辑组，但四组都只是 HudActivity 下的普通子层级，
            // 不再拆 HudNotice/HudSecondary。这样服务端动态增删图标后仍按配置组换行，同时位置仍可在 prefab 精调。
            RectTransform groupOne = NewSlotGroup("Group_ActivityOne", gpCon);
            RectTransform groupTwo = NewSlotGroup("Group_ActivityTwo", gpCon);
            RectTransform groupOther = NewSlotGroup("Group_ActivityOther", gpCon);
            RectTransform groupFourth = NewSlotGroup("Group_ActivityFourth", gpCon);

            const int SlotCols = 7;
            for (int c = 0; c < SlotCols; c++)
                BuildSlot(groupOne, "One", c, c * (GridCellW + GridHGap), 0f);
            for (int c = 0; c < SlotCols; c++)
                BuildSlot(groupTwo, "Two", c, c * (GridCellW + GridHGap), GridCellH + GridVGap);
            for (int c = 0; c < SlotCols; c++)
                BuildSlot(groupOther, "Other", c, c * (GridCellW + GridHGap), 2f * (GridCellH + GridVGap));

            // 第四组整体右移给左下角的太极折叠钮 TurnDisk 让位,并因此少一格:
            // 太极在总装根层占屏幕 x∈[6,84]、y∈[396.3,474.3](见 MainUIModuleCreator.TurnLocal/TurnSize),
            // 而底排槽屏幕 y∈[403,475] 与它完全重叠 —— 不缩进的话首槽(屏幕 x∈[10,82])会被太极正面压住。
            // 缩进 80.6 后首槽屏幕 x∈[90.6,162.6],距太极右缘留 6.6px 净空;
            // 末槽右缘 = 80.6 + 5*77 + 72 = 537.6,仍在区域宽 548 内,再加第 7 格右缘 614.6 会溢出 66px,
            // 故底排只排 6 格。数值取自编辑器内的手工调整(存档提交 65393a5ea),
            // 按「改 UI 一律改 Creator」铁律回写,避免下次重跑生成时被覆盖抹掉。
            // 注:第四活动组(location_type=10)需要缩进给太极让位；
            // 是后来改成均匀 4×7 槽位循环时把这条缩进丢了,这里把它补回来(实测值 80.6,比原设计的 77 多 3.6px 净空)。
            const float LastRowIndent = 80.6f;
            for (int c = 0; c < SlotCols - 1; c++)
                BuildSlot(groupFourth, "Fourth", c,
                    LastRowIndent + c * (GridCellW + GridHGap), 3f * (GridCellH + GridVGap));
            const int slotCount = SlotCols * 3 + (SlotCols - 1); // 27 = 前三组 7 格 + 第四组 6 格

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudActivity.prefab 已生成: " + PrefabPath +
                      "(分组槽位式:IconGrid 下 4 组/" + slotCount + " 个空槽,第四组缩进给太极让位;位置全在 prefab 可拖;人工核对后再并入 MainUIModule.prefab)");
        }

        public static void GenerateLeft()
        {
            RectTransform root = UiCreatorKit.NewRoot("HudActivityLeft");
            // 旧 HudSecondary.LeftIconSlot 的运行时终态：(7,498) 起，303×72。
            AnchorTopLeft(root, 7f, 498f, 303f, 72f);
            root.gameObject.SetActive(false);
            for (int i = 0; i < 4; i++)
                BuildSlot(root, "Left", i, i * (GridCellW + GridHGap), 0f);
            root.gameObject.SetActive(true);
            SaveRegion(root, LeftPrefabPath, "HudActivityLeft");
        }

        public static void GenerateRight()
        {
            RectTransform root = UiCreatorKit.NewRoot("HudActivityRight");
            // 旧 HudSecondary.RightIconSlot 的运行时终态：右缘锚、屏幕中线下移 250，2 列×6 行区域。
            root.anchorMin = root.anchorMax = new Vector2(1f, 0.5f);
            root.pivot = new Vector2(1f, 0f);
            root.sizeDelta = new Vector2(228f, 384f);
            root.anchoredPosition = new Vector2(0f, -250f);
            root.gameObject.SetActive(false);

            for (int i = 0; i < 8; i++)
            {
                RectTransform slot = UiCreatorKit.NewNode("Slot_Right_" + i.ToString("00"), root);
                slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(1f, 0f);
                slot.sizeDelta = new Vector2(114f, 64f);
                slot.anchoredPosition = new Vector2(-(Mathf.Floor(i / 6f) * 114f), (i % 6) * 64f);
            }

            root.gameObject.SetActive(true);
            SaveRegion(root, RightPrefabPath, "HudActivityRight");
        }

        private static void SaveRegion(RectTransform root, string path, string label)
        {
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, path);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] " + label + ".prefab 已生成：" + path + "（只含可编辑空槽，模板与逻辑统一归 HudActivity）");
        }

        /// <summary>只建空槽位；活动图标统一由 MainUIActivityView 克隆唯一的 ActivityIcon 模板填入。</summary>
        private static void BuildSlot(RectTransform parent, string groupKey, int groupIndex, float x, float y)
        {
            RectTransform slot = UiCreatorKit.NewNode("Slot_" + groupKey + "_" + groupIndex.ToString("00"), parent);
            AnchorTopLeft(slot, x, y, GridCellW, GridCellH);
        }

        private static RectTransform NewSlotGroup(string name, Transform parent)
        {
            RectTransform group = UiCreatorKit.NewNode(name, parent);
            UiCreatorKit.Stretch(group);
            return group;
        }

        private static GameObject InstantiateActivityIcon(GameObject prefab, Transform parent, string name)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.SetActive(true);
            return instance;
        }

        private static GameObject EnsureActivityIconPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(IconPrefabPath);
            if (prefab != null) return prefab;
            GenerateActivityIconPrefab();
            return AssetDatabase.LoadAssetAtPath<GameObject>(IconPrefabPath);
        }

        /// <summary>独立保存通用图标模板。HudActivity 重建时若它已存在就保留人工精调,不会连带覆盖。</summary>
        public static void GenerateActivityIconPrefab()
        {
            GameObject icon = BuildActivityIconTemplate(null);
            RectTransform rt = (RectTransform)icon.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(GridCellW, GridCellH);
            icon.SetActive(true);

            GameObject saved = UiCreatorKit.SavePrefab(icon, IconPrefabPath);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] ActivityIcon.prefab 已生成: " + IconPrefabPath +
                      "(HudActivity 仅保留这一份克隆模板;图片/大小/描述条/红点/特效都在此精调)");
        }

        /// <summary>
        /// 建活动图标模板(对标 ActivityIcon.json + ActivityIconBind 字段),挂 ActivityIcon 业务类。
        /// MainUIActivityView 用 Instantiate 克隆后 PlaceIconInSlot 撑满所在槽——槽多大图多大;
        /// 内部件锚边角/撑满:模板根被槽拉伸时,图撑满、角标钉角、文字条贴底(槽位式的尺寸传导链)。
        /// 本体建完即禁用,只作克隆源。
        /// </summary>
        private static GameObject BuildActivityIconTemplate(Transform parent)
        {
            RectTransform item = UiCreatorKit.NewNode("ActivityIcon", parent);
            ActivityIcon icon = item.gameObject.AddComponent<ActivityIcon>();

            // CompletionEffectSlot(老端 _box_effect):图标完成特效挂点(纯布局盒,老端无 skin)
            RectTransform boxEffect = UiCreatorKit.NewNode("CompletionEffectSlot", item); // 老端: _box_effect
            PlaceTL(boxEffect, 0f, 0f, 72f, 85f, 72f, 72f);
            icon._box_effect = boxEffect;

            // 内部件锚边角/撑满:模板根被槽拉伸时,图撑满、角标钉角、文字条贴底(槽位式的尺寸传导链)。
            // IconImage(老端 _img_icon):图标本体(点击命中体),运行时按配置换图,先贴一张代表图;Stretch 撑满模板根
            Image imgIcon = UiCreatorKit.NewImage("IconImage", item); // 老端: _img_icon
            UiCreatorKit.Stretch(imgIcon.rectTransform);
            imgIcon.raycastTarget = true;
            UiCreatorKit.TrySetSprite(imgIcon, IMG_SAMPLE_ICON, UiCreatorKit.Palette.BtnPrimary);
            icon._img_icon = imgIcon;

            // GuideArrowSlot(老端 _box_arrow):引导手指/箭头挂点(老端无 skin,纯命中/挂点透明层);Stretch 撑满
            Image boxArrow = UiCreatorKit.NewImage("GuideArrowSlot", item); // 老端: _box_arrow
            UiCreatorKit.Stretch(boxArrow.rectTransform);
            boxArrow.color = new Color(1f, 1f, 1f, 0f);
            boxArrow.raycastTarget = false;
            icon._box_arrow = boxArrow;

            // IconRedDot(老端 _img_red):小红点(默认隐藏);钉右上角,x=原左上锚 50−72=−22,y 不变
            Image imgRed = UiCreatorKit.NewImage("IconRedDot", item); // 老端: _img_red
            PinTopRight(imgRed.rectTransform, -22f, 5f, 28f, 29f);
            imgRed.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgRed, IMG_RED_DOT, UiCreatorKit.Palette.Mark);
            imgRed.gameObject.SetActive(false);
            icon._img_red = imgRed;

            // DescriptionBarBg(老端 _img_desc_bg):图标下方文案条底图(倒计时/描述,默认隐藏);
            // 底部横铺:宽随模板根,条顶钉在根底上方 1(原 TL(0,71,72,18) 于 72 高根 → 72−71=1)
            Image imgDescBg = UiCreatorKit.NewImage("DescriptionBarBg", item); // 老端: _img_desc_bg
            imgDescBg.rectTransform.anchorMin = new Vector2(0f, 0f);
            imgDescBg.rectTransform.anchorMax = new Vector2(1f, 0f);
            imgDescBg.rectTransform.pivot = new Vector2(0.5f, 1f);
            imgDescBg.rectTransform.sizeDelta = new Vector2(0f, 18f);
            imgDescBg.rectTransform.anchoredPosition = new Vector2(0f, 1f);
            imgDescBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgDescBg, IMG_DESC_BG, UiCreatorKit.Palette.Panel);
            imgDescBg.gameObject.SetActive(false);
            icon._img_desc_bg = imgDescBg;

            // RedNumberBadgeBg(老端 _img_red_num):角标数字红点底图(默认隐藏);钉右上角 49−72=−23
            Image imgRedNum = UiCreatorKit.NewImage("RedNumberBadgeBg", item); // 老端: _img_red_num
            PinTopRight(imgRedNum.rectTransform, -23f, 8f, 32f, 32f);
            imgRedNum.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgRedNum, IMG_RED_NUM_BG, UiCreatorKit.Palette.Mark);
            imgRedNum.gameObject.SetActive(false);
            icon._img_red_num = imgRedNum;

            // RedNumberLabel(老端 _lb_num):角标数字文本(默认隐藏);钉右上角 65−72=−7
            TextMeshProUGUI lbNum = UiCreatorKit.NewText("RedNumberLabel", item, "3"); // 老端: _lb_num
            PinTopRight(lbNum.rectTransform, -7f, 1f, 30f, 18f);
            lbNum.fontSize = 18f;
            lbNum.gameObject.SetActive(false);
            icon._lb_num = lbNum;

            // DescriptionLabel(老端 _lb_desc):图标下方描述/倒计时文本(默认隐藏);锚底中(0.5,0)。
            // x=0(水平居中):老端 x=36 是【中心枢轴】坐标(=72 宽根的正中),转换器产物 ActivityIcon.prefab
            // 可证;此前 PlaceTL 把 36 误当左上角换出 +90 的右偏,是移植笔误,按居中修正。
            TextMeshProUGUI lbDesc = UiCreatorKit.NewText("DescriptionLabel", item, ""); // 老端: _lb_desc
            lbDesc.rectTransform.anchorMin = lbDesc.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            lbDesc.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            lbDesc.rectTransform.sizeDelta = new Vector2(180f, 32f);
            lbDesc.rectTransform.anchoredPosition = new Vector2(0f, -17f);
            lbDesc.fontSize = 16f;
            lbDesc.color = new Color(0.533f, 1f, 0.263f); // #88ff43
            lbDesc.gameObject.SetActive(false);
            icon._lb_desc = lbDesc;

            // _box_effect2:持久循环特效挂点(如超值礼包高亮环),纯布局盒。白名单不改名(EnsureEffectBox 按字面量兜底找)。
            RectTransform boxEffect2 = UiCreatorKit.NewNode("_box_effect2", item);
            PlaceTL(boxEffect2, -14f, -14f, 100f, 100f, 72f, 72f);
            icon._box_effect2 = boxEffect2;

            // _tpl_TopPlayerTipItem(图标内嵌):对标老端 icon_type=="191" 的礼包提示气泡,当前未接线,先建好占位。
            GameObject tplTip = BuildTopPlayerTipItemTemplate(item);
            UiCreatorKit.Place((RectTransform)tplTip.transform, 50f, -15f, 276f, 66f);
            tplTip.SetActive(false);
            icon._tpl_TopPlayerTipItem = tplTip;

            return item.gameObject;
        }

        /// <summary>建 TopPlayerTipItem 模板(对标 TopPlayerTipItem.json:276x66),挂业务类。ActivityIcon 内嵌用。</summary>
        private static GameObject BuildTopPlayerTipItemTemplate(Transform parent)
        {
            RectTransform item = UiCreatorKit.NewNode("TopPlayerTipItem", parent);
            UiCreatorKit.Place(item, 0f, 0f, 276f, 66f);
            TopPlayerTipItem tip = item.gameObject.AddComponent<TopPlayerTipItem>();

            Image bg = UiCreatorKit.NewImage("bg", item);
            UiCreatorKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_TIP_BG, UiCreatorKit.Palette.Panel);
            tip.bg = bg;

            TextMeshProUGUI des = UiCreatorKit.NewText("des", item, "提示文案");
            PlaceTL(des.rectTransform, 32f, 22f, 200f, 32f, 276f, 66f);
            des.alignment = TextAlignmentOptions.Left;
            des.fontSize = 20f;
            des.color = new Color(0.4f, 0.224f, 0.082f); // #663915
            tip.des = des;

            return item.gameObject;
        }

        /// <summary>隐藏的模板挂载容器(__Templates):收纯克隆源,不裸露在可见容器里。</summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>Laya 左上原点 (x,y,w,h) → Unity 中心锚 anchoredPosition 换算(仅模板内部子节点用)。</summary>
        private static void PlaceTL(RectTransform rt, float x, float y, float w, float h, float parentW, float parentH)
        {
            float cx = x + w * 0.5f - parentW * 0.5f;
            float cy = -(y + h * 0.5f - parentH * 0.5f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        /// <summary>钉右上角(anchor(1,1)/pivot(0,1)):x=原左上锚 x−根宽,y 不变(取正上)。角标类内部件用,
        /// 模板根被槽拉伸时角标始终贴右上角(与 ActivityIcon.prefab 的同款重锚一致)。</summary>
        private static void PinTopRight(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>真·左上锚:anchor/pivot=(0,1),anchoredPosition=(x,-y)。给有界区域根/太极等左上定位用。</summary>
        private static void AnchorTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudActivity",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "HudActivity 是并入 MainUIModule 的区域子视图,不走 ViewManager.Open<T>();预览直接把最新 prefab " +
                    "实例化到 Window 层并手动调用 view.Show(),仅用于看结构。",
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
                Debug.LogError("[UiCreator] 找不到 " + PrefabPath + ",请先点生成");
                return;
            }

            Transform parent = ViewManager.GetLayer(UILayer.Window);
            _previewInstance = Object.Instantiate(prefab, parent);
            var view = _previewInstance.GetComponentInChildren<MainUIActivityView>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] HudActivity 预览实例缺少 MainUIActivityView 组件");
                return;
            }
            view.Show();
        }
    }
}
