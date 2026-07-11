using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面「竞榜 / 头号玩家榜」卡片区(_box_rank)纯代码建树生成器 —— 所见即所得版。
    ///
    /// 这一块原本挂在 HudActivity 的 MainUIActivityView 上,现拆成独立区域(对标用户要求:
    /// HudActivity 只留左上活动图标网格,右上这张竞榜卡单独成区)。
    ///
    /// 设计原则(用户定):布局归 prefab、代码不碰布局。root 收成竞榜卡的实际占位(右上 152×194),
    /// 不再全屏;边框图 mainui_ui_37、名牌底图 mainui_ui_38、红点位置、effect 挂点尺寸全部烤进 prefab,
    /// MainUIRankView 运行时不再有任何换图/改尺寸/改位置逻辑 —— 想调视觉直接改 HudRank.prefab。
    ///
    ///   HudRank(root)              —— 有界:右上锚,右缘内缩 5、顶 117,152×194(= 老端 _box_rank 563,117)
    ///     MainUIRankView(view)      —— Stretch 填满 root,挂业务类
    ///       RankPanel(_box_rank)    —— Stretch 填满(默认隐藏,数据到达时 OnTopPlayerMainData/OnCycleData 现身)
    ///
    /// TopPlayerTipItem 气泡模板业务从未接线,已连同 __Templates 挂载点一起移除(Bind 字段同步删);
    /// 将来要用时按址加载独立的 TopPlayerTipItem.prefab(同 EquipmentItem 套路),不在本卡里藏克隆源。
    ///
    /// 存 Assets/Prefabs/UI/MainUI/Regions/HudRank.prefab,供人工核对后再并入 MainUIModule.prefab。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
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
    //   _img_red        -> RankRedDot
    public static class HudRankCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudRank.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)
        private const string IMG_RANK_BG = "resource/game/mainUI/texture/mainui_ui_37.png";   // 坐骑竞榜边框
        private const string IMG_NAMEPLATE = "resource/game/mainUI/other/mainui_ui_38.png";   // 名牌底图(注意在 other/ 下,非 texture/)
        private const string IMG_EMPTY = "resource/game/common/texture/com_empty.png";        // icon 遗留占位(业务未接线,保持透明)
        private const string IMG_RED_DOT = "resource/game/mainUI/texture/com_red_point.png";  // 小红点

        // 竞榜卡实际占位(老端 _box_rank 563,117,152×194 → 右上锚:右缘内缩 720-563-152=5,顶部 117)
        private const float CardW = 152f, CardH = 194f;
        private const float CardRightInset = 5f, CardTop = 117f;

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudRank(竞榜卡)",
                Note = "右上竞榜/头号玩家榜卡,有界 root(右上 152×194)+ 样式全烤 prefab,代码零布局",
                Order = 25,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再统一激活(与 Login 系列 Creator 一致的安全写法)。
            // root 收成竞榜卡实际占位(右上锚),不再全屏;并入总装时非全屏锚 → offset 归零自动跳过,
            // 想挪整卡直接在 prefab 里拖 root。
            RectTransform root = UiCreatorKit.NewRoot("HudRank");
            AnchorTopRight(root, CardRightInset, CardTop, CardW, CardH);
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("MainUIRankView", root);
            UiCreatorKit.Stretch(viewRoot); // 填满有界 root
            var view = viewRoot.gameObject.AddComponent<MainUIRankView>();

            // ================= RankPanel(老端 _box_rank):右上坐骑竞榜 / 循环冲榜 / 头号玩家榜 =================
            RectTransform boxRank = UiCreatorKit.NewNode("RankPanel", viewRoot); // 老端: _box_rank
            UiCreatorKit.Stretch(boxRank); // 填满有界 root(152×194),子节点仍按老端卡内坐标摆
            view._box_rank = boxRank;

            Image imgBg = UiCreatorKit.NewImage("RankPanelBorder", boxRank); // 老端: _img_bg
            PlaceTL(imgBg.rectTransform, 0f, 0f, 154f, 157f, 152f, 194f);
            imgBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgBg, IMG_RANK_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = imgBg;

            // RankEffectSlot(老端 effect):特效挂点。老端把它顶成 720×1280 全屏盒,但该特效从未接线/验证;
            // 为所见即所得(不给编辑器留一个盖全屏的隐形大矩形),收成填满本卡 —— 真要挂全屏特效时去 prefab 改尺寸。
            RectTransform effect = UiCreatorKit.NewNode("RankEffectSlot", boxRank); // 老端: effect
            UiCreatorKit.Stretch(effect);
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
            // 名牌底图直接烤进 prefab(老端运行时 SetLayaTextureAsync 换图逻辑已删;图在 other/ 下)。
            UiCreatorKit.TrySetSprite(bg1, IMG_NAMEPLATE, UiCreatorKit.Palette.Panel);
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

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudRank.prefab 已生成: " + PrefabPath +
                      "(竞榜卡,人工核对后再并入 MainUIModule.prefab)");
        }

        /// <summary>右上锚定:锚点/轴心取父右上角,rightInset=距右缘、top=距顶。区域 root 专用(有界、不全屏)。</summary>
        private static void AnchorTopRight(RectTransform rt, float rightInset, float top, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-rightInset, -top);
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

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudRank",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "HudRank 是并入 MainUIModule 的区域子视图,不走 ViewManager.Open<T>()" +
                    "(MainUIRankViewBind 上没有 [UIView] 地址特性);预览直接把最新 prefab 实例化到 " +
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
            var view = _previewInstance.GetComponentInChildren<MainUIRankView>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] HudRank 预览实例缺少 MainUIRankView 组件");
                return;
            }
            view.Show();
        }
    }
}
