using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面「活动入口区」纯代码建树生成器(Unity 布局组容器化版)。
    ///
    /// 设计原则(用户定):布局归 prefab、代码不碰布局。老端 Laya 用 RefreshIconPos 在代码里算坐标——那是 Laya 的搞法;
    /// Unity 里改成【子容器 + Unity 布局组件】,运行时代码只把图标 Instantiate 进对应容器、按 pos_index 排 sibling 顺序,
    /// 绝不在 C# 里写位置/间距/尺寸/行偏移。所有布局值(格子大小、间距、缩进、分组竖排)都落在 prefab 组件上,随时可在
    /// 预制体里手调。
    ///
    /// 结构:
    ///   HudActivity(root)                 —— 有界:左上锚,定位(10,127),548×380,不再全屏
    ///     MainUIActivityView(view)         —— Stretch 填满 root,挂业务类
    ///       __Templates / ActivityIcon      —— 活动图标克隆模板(建完禁用)
    ///       IconGrid(_gp_con)              —— VerticalLayoutGroup:自动竖排下面 4 个分组(空组由运行时收起)
    ///         Group_ActivityOne  (loc1)     —— GridLayoutGroup(7列/72格/间距5·20):顶排
    ///         Group_ActivityTwo  (loc2)     —— GridLayoutGroup:主排(头号玩家 331@10@0 排这组第2格)
    ///         Group_ActivityOther(loc3)     —— GridLayoutGroup
    ///         Group_ActivityFourth(loc4/10) —— GridLayoutGroup(padding.left=77 缩进,给太极让位):底排
    ///       TurnDisk(_img_turn)            —— 收/展太极折叠钮,固定左下(独立于布局组,折叠时不被收起)
    ///
    /// 右上「竞榜/头号玩家榜」卡片(老端 _box_rank)已拆到独立区域 HudRank.prefab(见 HudRankCreator)。
    /// location_type=9(RightMiddle)新老配置都 0 条(死配置),GroupFor 里直接忽略,不建对应容器。
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
                Name = "HudActivity(活动入口区)",
                Note = "有界左上区 + 4 个 GridLayoutGroup 分组容器(loc1/2/3/4)+ 太极;布局全在 prefab 可调",
                Order = 20,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            RectTransform root = UiCreatorKit.NewRoot("HudActivity");
            // root 收成活动网格的实际占位(左上区),不再全屏 Stretch;并入总装时锚点非全屏 → offset 归零自动跳过。
            AnchorTopLeft(root, RegionOrigin.x, RegionOrigin.y, RegionSize.x, RegionSize.y);
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("MainUIActivityView", root);
            UiCreatorKit.Stretch(viewRoot); // 填满有界 root
            var view = viewRoot.gameObject.AddComponent<MainUIActivityView>();

            // 隐藏模板挂载点:活动图标模板是纯克隆源,不应出现在可见容器里。
            RectTransform templates = NewTemplatesWrapper(viewRoot);
            GameObject tplIcon = BuildActivityIconTemplate(templates);
            AnchorTopLeft((RectTransform)tplIcon.transform, 0f, 0f, GridCellW, GridCellH);
            tplIcon.SetActive(false);
            view._tpl_ActivityIcon = tplIcon;

            // ================= IconGrid(老端 _gp_con):【槽位容器】,子节点=空槽位,运行时按顺序填图标 =================
            RectTransform gpCon = UiCreatorKit.NewNode("IconGrid", viewRoot); // 老端: _gp_con
            UiCreatorKit.Stretch(gpCon); // 填满 view(槽位相对它左上定位)
            view._gp_con = gpCon;

            // 【槽位基线】默认摆 SlotRows×SlotCols 个空槽位(位置全在 prefab,随便拖/加/删/换行)。每个槽带一个样例图标便于
            // 编辑器可视,运行时 MainUIActivityView 清样例、把真图标【按顺序】填进各槽(填满一个进下一个,自然溢流)。
            // 想改布局:直接在 prefab 的 IconGrid 下拖这些 Slot_* 节点即可 —— 代码绝不算坐标。
            // 老端活动网格 4 行封顶(每行 ≤7,共 28 个上限,超出往下挤/隐);对齐之 → 4×7=28 个槽,杜绝往下溢出压到场景/角色。
            const int SlotRows = 4, SlotCols = 7;
            int idx = 0;
            for (int r = 0; r < SlotRows; r++)
                for (int c = 0; c < SlotCols; c++)
                    BuildSlot(gpCon, idx++, c * (GridCellW + GridHGap), r * (GridCellH + GridVGap));

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudActivity.prefab 已生成: " + PrefabPath +
                      "(槽位式:IconGrid 下 " + (SlotRows * SlotCols) + " 个空槽,位置全在 prefab 可拖;人工核对后再并入 MainUIModule.prefab)");
        }

        /// <summary>建一个空槽位:左上锚定的 72×72 RectTransform + 一个样例图标子节点(仅设计期可视,运行时被 MainUIActivityView 清掉)。</summary>
        private static void BuildSlot(RectTransform parent, int index, float x, float y)
        {
            RectTransform slot = UiCreatorKit.NewNode("Slot_" + index, parent);
            AnchorTopLeft(slot, x, y, GridCellW, GridCellH);
            Image sample = UiCreatorKit.NewImage("Sample", slot);
            AnchorTopLeft(sample.rectTransform, 0f, 0f, GridCellW, GridCellH);
            UiCreatorKit.TrySetSprite(sample, IMG_SAMPLE_ICON, UiCreatorKit.Palette.BtnNeutral);
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
