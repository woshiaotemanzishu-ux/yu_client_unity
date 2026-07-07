using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面「活动入口区」(重构版)纯代码建树生成器。
    ///
    /// 结构对标运行时快照(mainui_123123,188/219 节点)+ 老端场景 JSON:
    ///   MainUIActivityView(全屏挂业务类)
    ///     _gp_con   —— 左上活动图标网格容器;内含收/展太极(_img_turn)+ 活动图标模板(_tpl_ActivityIcon,
    ///                   对标 ActivityIcon.json,建成【模板子节点】而非独立按址 prefab——因为已写好的
    ///                   MainUIActivityView.CreateActivityIcon 用的是 `Instantiate(_tpl_ActivityIcon, _gp_con)`
    ///                   这种"字段直引用"克隆,不是 ResManager 按址加载;旧镜像 prefab 里这个字段其实只绑了
    ///                   一张空白 Image 占位(__Templates 下的桩节点),本生成器把它换成真正能用的图标子树)。
    ///     _box_rank —— 右上坐骑竞榜/循环冲榜卡片(默认隐藏,数据到达时 OnInit/OnTopPlayerMainData/OnCycleData 现身)。
    ///     _tpl_TopPlayerTipItem —— 头号玩家榜礼包提示气泡模板(当前业务只 SetActive(false),未接线实例化;
    ///                   按 Bind 契约先建好模板占位,便于后续接线)。
    ///
    /// 贴图已尽量对齐老端真实源图(TrySetSprite),动态图标模板贴一张代表图(运行时由数据决定实际图)。
    /// EquipmentItem(common 模块)、BaseIcon(无 Unity 类)按范围不做;ActivityIcon 内 _tpl_MainUIStrongerTalkBoard/
    /// _tpl_TalkBoard/_tpl_ArrowComponent 是变强/引导箭头等其他功能域的模板字段,同 _tpl_EquipmentItem 一样跨模块不做。
    /// 存 Assets/Prefabs/UI/MainUI/Regions/HudActivity.prefab,供人工核对后再并入 MainUIModule.prefab。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   _gp_con         -> IconGrid
    //   _img_turn       -> TurnDisk(收/展太极图标)
    //   _box_rank       -> RankPanel
    //   _img_bg         -> RankPanelBorder(RankPanel 内的边框图)
    //   effect          -> RankFullscreenEffectSlot
    //   icon_box        -> TopPlayerRewardSlot
    //   model_gp        -> CycleRankModelSlot
    //   icon            -> RankPreviewIcon
    //   bg1             -> RankNamePlateBg
    //   name            -> RankTitleLabel
    //   player_name     -> RankTopPlayerNameLabel
    //   time            -> RankCountdownLabel
    //   _img_red(RankPanel 内)      -> RankRedDot
    //   _box_effect(ActivityIcon 模板内)  -> CompletionEffectSlot
    //   _img_icon(ActivityIcon 模板内)    -> IconImage
    //   _box_arrow(ActivityIcon 模板内)   -> GuideArrowSlot
    //   _img_red(ActivityIcon 模板内)     -> IconRedDot
    //   _img_desc_bg    -> DescriptionBarBg
    //   _img_red_num    -> RedNumberBadgeBg
    //   _lb_num         -> RedNumberLabel
    //   _lb_desc        -> DescriptionLabel
    // 注:_box_effect2 不改名,原样保留——ActivityIcon.EnsureEffectBox 运行时按此字面量名兜底查找
    // (FindDeep(transform,"_box_effect2")),改名会让该兜底失效。
    public static class HudActivityCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudActivity.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)
        private const string IMG_TURN = "resource/game/mainUI/texture/uizjmv3_017b.png";     // 活动区收/展太极图标
        private const string IMG_RANK_BG = "resource/game/mainUI/texture/mainui_ui_37.png";  // 坐骑竞榜边框
        private const string IMG_EMPTY = "resource/game/common/texture/com_empty.png";       // icon/bg1 运行时换图前的老端默认底图
        private const string IMG_RED_DOT = "resource/game/mainUI/texture/com_red_point.png"; // 小红点
        private const string IMG_DESC_BG = "resource/game/mainUI/texture/mainui_ui_40.png";  // 图标下方文案条底图
        private const string IMG_RED_NUM_BG = "resource/game/mainUI/texture/ui_bq_03.png";   // 图标角标数字底图
        private const string IMG_TIP_BG = "resource/game/mainUI/texture/ui_First_16.png";    // TopPlayerTipItem 气泡底图
        private const string IMG_SAMPLE_ICON = "resource/game/icon/texture/151_1.png";       // 图标模板代表图(动态,运行时按配置换)

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudActivity(活动入口区)",
                Note = "左上活动图标网格(_gp_con + ActivityIcon 模板) + 右上坐骑竞榜(_box_rank)",
                Order = 20,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再统一激活(与 Login 系列 Creator 一致的安全写法)。
            RectTransform root = UiCreatorKit.NewRoot("HudActivity");
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("MainUIActivityView", root);
            UiCreatorKit.Stretch(viewRoot);
            var view = viewRoot.gameObject.AddComponent<MainUIActivityView>();

            // 隐藏模板挂载点:活动图标模板 / 头号玩家提示气泡模板都是纯克隆源,不应出现在可见的 IconGrid 容器里。
            RectTransform templates = NewTemplatesWrapper(viewRoot);

            // ================= IconGrid(老端 _gp_con):左上活动图标网格 =================
            RectTransform gpCon = UiCreatorKit.NewNode("IconGrid", viewRoot); // 老端: _gp_con
            PlaceTL(gpCon, 10f, 127f, 548f, 298f, UiCreatorKit.DesignWidth, UiCreatorKit.DesignHeight);
            view._gp_con = gpCon;

            Image imgTurn = UiCreatorKit.NewImage("TurnDisk", gpCon); // 老端: _img_turn
            PlaceTL(imgTurn.rectTransform, 35f, 322f, 78f, 78f, 548f, 298f);
            imgTurn.raycastTarget = true; // 收放箭头要能接收点击(对标 BindButtons)
            UiCreatorKit.TrySetSprite(imgTurn, IMG_TURN, UiCreatorKit.Palette.BtnNeutral);
            view._img_turn = imgTurn;

            // 活动图标模板:挂在隐藏的 __Templates 下,建完即禁用;CreateActivityIcon 用它 Instantiate 克隆再各自定位。
            // ⚠️ 模板根必须【真·左上锚】(anchor/pivot=(0,1)),不能用 PlaceTL(它只换算坐标、锚点仍是中心):
            // 克隆体继承模板锚点,运行时 RefreshIconPos→SetPosition 按「IconGrid 左上原点、anchoredPosition=(x,-y)」
            // 布局;中心锚会让全部图标整体漂移半个容器(2026-07-06 超值礼包悬空事故根因)。
            GameObject tplIcon = BuildActivityIconTemplate(templates);
            AnchorTopLeft((RectTransform)tplIcon.transform, 0f, 0f, 72f, 72f);
            tplIcon.SetActive(false);
            view._tpl_ActivityIcon = tplIcon;

            // ================= RankPanel(老端 _box_rank):右上坐骑竞榜 / 循环冲榜 =================
            RectTransform boxRank = UiCreatorKit.NewNode("RankPanel", viewRoot); // 老端: _box_rank
            PlaceTL(boxRank, 563f, 117f, 152f, 194f, UiCreatorKit.DesignWidth, UiCreatorKit.DesignHeight);
            view._box_rank = boxRank;

            Image imgBg = UiCreatorKit.NewImage("RankPanelBorder", boxRank); // 老端: _img_bg
            PlaceTL(imgBg.rectTransform, 0f, 0f, 154f, 157f, 152f, 194f);
            imgBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgBg, IMG_RANK_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = imgBg;

            // RankFullscreenEffectSlot(老端 effect):全屏特效挂点(老端相对 _box_rank 的坐标系把它顶到铺满全屏),OnInit 会强制 sizeDelta=720x1280。
            RectTransform effect = UiCreatorKit.NewNode("RankFullscreenEffectSlot", boxRank); // 老端: effect
            PlaceTL(effect, -284f, -565f, 720f, 1280f, 152f, 194f);
            effect.gameObject.SetActive(false);
            view.effect = effect;

            RectTransform iconBox = UiCreatorKit.NewNode("TopPlayerRewardSlot", boxRank); // 老端: icon_box
            PlaceTL(iconBox, 37f, 35f, 80f, 80f, 152f, 194f);
            iconBox.gameObject.SetActive(false); // 头号玩家奖励道具挂点,数据到达前不现身
            view.icon_box = iconBox;

            RectTransform modelGp = UiCreatorKit.NewNode("CycleRankModelSlot", boxRank); // 老端: model_gp
            PlaceTL(modelGp, -6f, 33f, 152f, 120f, 152f, 194f);
            view.model_gp = modelGp;

            Image icon = UiCreatorKit.NewImage("RankPreviewIcon", boxRank); // 老端: icon
            PlaceTL(icon.rectTransform, 33f, 31f, 86f, 86f, 152f, 194f);
            icon.raycastTarget = false;
            UiCreatorKit.TrySetSprite(icon, IMG_EMPTY, UiCreatorKit.Palette.Panel);
            view.icon = icon;

            Image bg1 = UiCreatorKit.NewImage("RankNamePlateBg", boxRank); // 老端: bg1
            PlaceTL(bg1.rectTransform, -2f, 139f, 157f, 40f, 152f, 194f);
            bg1.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg1, IMG_EMPTY, UiCreatorKit.Palette.Panel);
            view.bg1 = bg1;

            TextMeshProUGUI name = UiCreatorKit.NewText("RankTitleLabel", boxRank, "坐骑竞榜"); // 老端: name
            PlaceTL(name.rectTransform, 40f, 12f, 72f, 18f, 152f, 194f);
            name.fontSize = 18f;
            name.color = new Color(1f, 0.902f, 0.349f); // #ffe659
            view.name = name;

            TextMeshProUGUI playerName = UiCreatorKit.NewText("RankTopPlayerNameLabel", boxRank, "虚位以待"); // 老端: player_name
            PlaceTL(playerName.rectTransform, 36f, 140f, 80f, 20f, 152f, 194f);
            playerName.fontSize = 22f;
            view.player_name = playerName;

            TextMeshProUGUI time = UiCreatorKit.NewText("RankCountdownLabel", boxRank, "00:00:00"); // 老端: time
            PlaceTL(time.rectTransform, 49f, 163f, 54f, 18f, 152f, 194f);
            time.fontSize = 18f;
            time.color = new Color(0.533f, 1f, 0.263f); // #88ff43
            view.time = time;

            Image imgRedRank = UiCreatorKit.NewImage("RankRedDot", boxRank); // 老端: _img_red
            PlaceTL(imgRedRank.rectTransform, 120f, -5f, 28f, 29f, 152f, 194f);
            imgRedRank.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgRedRank, IMG_RED_DOT, UiCreatorKit.Palette.Mark);
            imgRedRank.gameObject.SetActive(false);
            view._img_red = imgRedRank;

            boxRank.gameObject.SetActive(false); // 对标 OnInit:头号玩家/竞榜数据到达前 _box_rank 整体不现身

            // ================= _tpl_TopPlayerTipItem(顶层):礼包提示气泡模板,当前业务未接线实例化,先建好占位 =================
            GameObject tplTip = BuildTopPlayerTipItemTemplate(templates);
            UiCreatorKit.Place((RectTransform)tplTip.transform, 0f, 0f, 276f, 66f);
            tplTip.SetActive(false);
            view._tpl_TopPlayerTipItem = tplTip;

            // 注:_tpl_EquipmentItem(common 模块道具格)不在本区范围,不建;
            // 已写好的 BuildTopPlayerReward 走 ResManager 按址加载 common/EquipmentItem,不依赖这个字段。

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudActivity.prefab 已生成: " + PrefabPath +
                      "(活动入口区,人工核对后再并入 MainUIModule.prefab)");
        }

        /// <summary>
        /// 建活动图标模板(对标 ActivityIcon.json + ActivityIconBind 字段),挂 ActivityIcon 业务类。
        /// 这是 MainUIActivityView.CreateActivityIcon 用 Instantiate(_tpl_ActivityIcon, _gp_con) 克隆的源对象,
        /// 本身建完即禁用,永不显示,只作为克隆模板。
        /// </summary>
        private static GameObject BuildActivityIconTemplate(Transform parent)
        {
            RectTransform item = UiCreatorKit.NewNode("ActivityIcon", parent);
            ActivityIcon icon = item.gameObject.AddComponent<ActivityIcon>();

            // CompletionEffectSlot(老端 _box_effect):图标完成特效挂点(纯布局盒,老端无 skin)
            RectTransform boxEffect = UiCreatorKit.NewNode("CompletionEffectSlot", item); // 老端: _box_effect
            PlaceTL(boxEffect, 0f, 0f, 72f, 85f, 72f, 72f);
            icon._box_effect = boxEffect;

            // IconImage(老端 _img_icon):图标本体(点击命中体),运行时按配置换图,先贴一张代表图
            Image imgIcon = UiCreatorKit.NewImage("IconImage", item); // 老端: _img_icon
            PlaceTL(imgIcon.rectTransform, 0f, 0f, 72f, 72f, 72f, 72f);
            imgIcon.raycastTarget = true;
            UiCreatorKit.TrySetSprite(imgIcon, IMG_SAMPLE_ICON, UiCreatorKit.Palette.BtnPrimary);
            icon._img_icon = imgIcon;

            // GuideArrowSlot(老端 _box_arrow):引导手指/箭头挂点(老端无 skin,纯命中/挂点透明层)
            Image boxArrow = UiCreatorKit.NewImage("GuideArrowSlot", item); // 老端: _box_arrow
            PlaceTL(boxArrow.rectTransform, 0f, 0f, 72f, 72f, 72f, 72f);
            boxArrow.color = new Color(1f, 1f, 1f, 0f);
            boxArrow.raycastTarget = false;
            icon._box_arrow = boxArrow;

            // IconRedDot(老端 _img_red):小红点(默认隐藏)
            Image imgRed = UiCreatorKit.NewImage("IconRedDot", item); // 老端: _img_red
            PlaceTL(imgRed.rectTransform, 50f, -5f, 28f, 29f, 72f, 72f);
            imgRed.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgRed, IMG_RED_DOT, UiCreatorKit.Palette.Mark);
            imgRed.gameObject.SetActive(false);
            icon._img_red = imgRed;

            // DescriptionBarBg(老端 _img_desc_bg):图标下方文案条底图(倒计时/描述,默认隐藏)
            Image imgDescBg = UiCreatorKit.NewImage("DescriptionBarBg", item); // 老端: _img_desc_bg
            PlaceTL(imgDescBg.rectTransform, 0f, 71f, 72f, 18f, 72f, 72f);
            imgDescBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgDescBg, IMG_DESC_BG, UiCreatorKit.Palette.Panel);
            imgDescBg.gameObject.SetActive(false);
            icon._img_desc_bg = imgDescBg;

            // RedNumberBadgeBg(老端 _img_red_num):角标数字红点底图(默认隐藏)
            Image imgRedNum = UiCreatorKit.NewImage("RedNumberBadgeBg", item); // 老端: _img_red_num
            PlaceTL(imgRedNum.rectTransform, 49f, -8f, 32f, 32f, 72f, 72f);
            imgRedNum.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgRedNum, IMG_RED_NUM_BG, UiCreatorKit.Palette.Mark);
            imgRedNum.gameObject.SetActive(false);
            icon._img_red_num = imgRedNum;

            // RedNumberLabel(老端 _lb_num):角标数字文本(默认隐藏)
            TextMeshProUGUI lbNum = UiCreatorKit.NewText("RedNumberLabel", item, "3"); // 老端: _lb_num
            PlaceTL(lbNum.rectTransform, 65f, -1f, 30f, 18f, 72f, 72f);
            lbNum.fontSize = 18f;
            lbNum.gameObject.SetActive(false);
            icon._lb_num = lbNum;

            // DescriptionLabel(老端 _lb_desc):图标下方描述/倒计时文本(默认隐藏)
            TextMeshProUGUI lbDesc = UiCreatorKit.NewText("DescriptionLabel", item, ""); // 老端: _lb_desc
            PlaceTL(lbDesc.rectTransform, 36f, 73f, 180f, 32f, 72f, 72f);
            lbDesc.fontSize = 16f;
            lbDesc.color = new Color(0.533f, 1f, 0.263f); // #88ff43
            lbDesc.gameObject.SetActive(false);
            icon._lb_desc = lbDesc;

            // _box_effect2:持久循环特效挂点(如超值礼包高亮环),纯布局盒。
            // 白名单不改名:ActivityIcon.EnsureEffectBox 运行时用 FindDeep(transform,"_box_effect2") 按此字面量名兜底查找
            // (烤制会裁掉 inactive 节点导致克隆体字段为 null),改名会让这条兜底路径失效、丢失环特效。
            RectTransform boxEffect2 = UiCreatorKit.NewNode("_box_effect2", item);
            PlaceTL(boxEffect2, -14f, -14f, 100f, 100f, 72f, 72f);
            icon._box_effect2 = boxEffect2;

            // _tpl_TopPlayerTipItem(图标内嵌):对标老端 icon_type=="191" 时挂到 display_obj 的礼包提示气泡,
            // 当前 ActivityIcon.cs 未接线实例化,按 Bind 契约先建好模板占位(位置对标老端 SetAnchoredPosition(50,-15))。
            GameObject tplTip = BuildTopPlayerTipItemTemplate(item);
            UiCreatorKit.Place((RectTransform)tplTip.transform, 50f, -15f, 276f, 66f);
            tplTip.SetActive(false);
            icon._tpl_TopPlayerTipItem = tplTip;

            // 注:_tpl_MainUIStrongerTalkBoard / _tpl_TalkBoard / _tpl_ArrowComponent 不建——
            // 分别是变强气泡(MainStronger 模块)、通用对话气泡、引导箭头(guide 系统)的模板字段,
            // 老端里都是按 icon_type=="158" 等条件动态 new 出来挂到具体图标上,不是 ActivityIcon.json 的静态子节点;
            // 跨模块、超出「活动入口区」范围,处理口径与 _tpl_EquipmentItem 一致,留给对应模块的 Creator 处理。

            return item.gameObject;
        }

        /// <summary>
        /// 建 TopPlayerTipItem 模板(对标 TopPlayerTipItem.json:276x66,bg 铺满 + des 文案),挂业务类。
        /// 建完由调用方摆位置、禁用。
        /// </summary>
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

        /// <summary>
        /// 建一个隐藏的模板挂载容器(__Templates):专收纯克隆源(ActivityIcon/TopPlayerTipItem 等模板),
        /// 不让它们裸露在可见的业务容器(IconGrid 等)里当"不可见的第一个子节点"。
        /// </summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>
        /// Laya 左上原点 (x,y,w,h) → Unity 中心锚 anchoredPosition 换算(对标建树约定):
        /// cx = x + w/2 - parentW/2,cy = -(y + h/2 - parentH/2)。
        /// </summary>
        private static void PlaceTL(RectTransform rt, float x, float y, float w, float h, float parentW, float parentH)
        {
            float cx = x + w * 0.5f - parentW * 0.5f;
            float cy = -(y + h * 0.5f - parentH * 0.5f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        /// <summary>真·左上锚:anchor/pivot=(0,1),anchoredPosition=(x,-y)。给【运行时按左上原点定位克隆体】
        /// 的模板根用(如 ActivityIcon:MainUIActivityView.RefreshIconPos 算的就是左上系坐标)。</summary>
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
                    "HudActivity 是并入 MainUIModule 的区域子视图,不走 ViewManager.Open<T>()" +
                    "(MainUIActivityViewBind 上没有 [UIView] 地址特性);预览直接把最新 prefab 实例化到 " +
                    "Window 层并手动调用 view.Show(),仅用于看结构。",
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
