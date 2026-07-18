using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面【顶部状态条】(MainUITopView)纯代码建树生成器。
    ///
    /// 结构对标老端 resource/game/mainUI/MainUITopView.json + 运行时快照
    /// Tools/ModuleManifest/snapshots/mainui_123123/page_snapshot_MainUITopView_*.json:
    ///   头像血条战力等级战斗模式(_box_head)/ 货币三连(_box_money,克隆 _tpl_MainUIMoneyItem)/
    ///   vip·充值·光环·客服四宫格(_box_icon)/ 小地图场景名坐标时间wifi电量(_box_map)。
    ///
    /// root="HudTop"【只扎顶部这一条】:锚顶部居中、720×130(对标老端 display_obj.centerX=0/top=0 + 内容实际
    /// 占位高 113),不再全屏 Stretch。子根 "MainUITopView" 挂业务类 MainUITopView,Stretch 填满这条有界 root。
    ///
    /// 布局换算:整棵树统一用 UiCreatorKit 的"父中心锚"摆位(见本文件 PlaceLaya),每层级子节点
    /// 换算时用该层级自己的设计画布宽高(取运行时快照里该容器的已结算 w/h,不是 Laya 编辑期的
    /// auto/0 值)。两处例外改用左上锚(PlaceTopLeft),因为现有(不可改的)业务代码对 RectTransform
    /// 摆位方式有硬约束:
    ///   1) _box_icon 下 4 个图标格 —— MainUITopView.ReflowIconBox 直接改 anchoredPosition.x 靠左紧排
    ///      隐藏项不占位,注释明写"子项 anchorMin/Max=(0,1)、pivot.x=0"。
    ///   2) 血条 _img_hp —— MainUITopView.SetHpBarWidth 用 SetSizeWithCurrentAnchors 收缩宽度,
    ///      需 pivot.x=0 才会"左边固定、从右边收缩",否则会以中心向两边收缩,视觉不对。
    ///
    /// _tpl_MainUIMoneyItem / _tpl_MainUITopBuffItem 做成未激活模板子节点(挂各自业务类、回填各自
    /// Bind 字段),放在 _box_money / _box_buff 下,并回填到 TopView 对应 _tpl_* 字段,供运行时
    /// BuildMoneyItems / 未来的 Buff 克隆逻辑 Instantiate。
    ///
    /// _tpl_CustomHeadItem / _tpl_ActivityIcon 两个 Bind 字段刻意留空:老端 MainUITopView.ts 的
    /// GetComponents/GetChildrenByNames 名单里根本没有这两个变量,也从未 new CustomHeadItem(...)/
    /// new ActivityIcon(...) 过——它们是全项目共享的模板类型,转换器给所有视图统一开了这两个字段位,
    /// 但本视图没有源节点可依;既有(未改动过的)Assets/Prefabs/UI/MainUI/MainUITopView.prefab 里
    /// 这两个字段也是 fileID: 0(未绑定),与此结论一致。业务代码 HideUnbackedIndicators 对它们做了
    /// null 判空,留空不会报错。
    ///
    /// 元素已尽量贴老端真实源图,贴不到回退占位色(见生成后 Debug.LogWarning + 本方法调用方报告的缺图清单)。
    /// 尺寸/位置为起步值,自行在编辑器手调。入口在「神霄/重构UI 生成器」面板。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文;模板类节点如 MainUIMoneyItem/MainUITopBuffItem 保留类名不改):
    //   _box_root              -> TopBarContentRoot
    //   _box_icon              -> QuickAccessRow(vip/充值/光环/客服 四宫格)
    //   _box_vip               -> VipEntry
    //   _img_vip_bg            -> VipEntryBg
    //   _img_vip               -> VipLevelIcon
    //   _img_vip_red           -> VipRedDot
    //   _box_recharge          -> RechargeEntry
    //   _img_recharge_bg       -> RechargeEntryBg
    //   _img_recharge_red      -> RechargeRedDot
    //   _box_halo              -> HaloEntry
    //   _img_halo              -> HaloEntryIcon
    //   _img_halo_red          -> HaloRedDot
    //   _box_cs                -> CustomerServiceEntry
    //   _img_cs                -> CustomerServiceIcon
    //   _img_cs_red            -> CustomerServiceRedDot
    //   _box_money             -> CurrencyRow
    //   _img_bg(MoneyItem)     -> MoneyItemBg
    //   _img_icon(MoneyItem)   -> CurrencyIcon
    //   _img_add               -> CurrencyAddIcon
    //   _lb_value              -> CurrencyValueLabel
    //   _box_effect(MoneyItem) -> CurrencyChangeEffectSlot
    //   _box_map               -> MiniMapBox
    //   _img_map_bg            -> MiniMapBg
    //   _lb_scene_name         -> SceneNameLabel
    //   _lb_role_pos           -> RolePositionLabel
    //   _img_wifi              -> WifiIcon
    //   _lb_time(MapBox)       -> SystemClockLabel
    //   _img_power_bg          -> BatteryIconBg
    //   _img_power             -> BatteryLevelIcon
    //   _box_head              -> PlayerHeadBox
    //   _img_head_bg           -> PlayerHeadPanelBg
    //   _img_hp                -> HpBar
    //   _box_buff              -> BuffRow
    //   _img_buff_count_bg     -> BuffOverflowCountBg
    //   _lb_buff_other_count   -> BuffOverflowCountLabel
    //   _img_bg(BuffItem)      -> BuffItemBg
    //   _img_mask(BuffItem)    -> BuffItemCdMask
    //   _lb_time(BuffItem)     -> BuffItemCountdownLabel
    //   _img_head              -> AvatarImage
    //   _img_frame             -> AvatarFrameDecor
    //   _img_fight             -> CombatPowerIcon
    //   _lb_hp                 -> HpValueLabel
    //   _lb_fighting           -> CombatPowerLabel
    //   _box_lv                -> LevelBadge
    //   _img_lv_bg             -> LevelBadgeBg
    //   _lb_lv                 -> LevelLabel
    //   _box_fight_mode        -> FightModeBadge
    //   _img_fight_mode        -> FightModeIcon
    public static class HudTopCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudTop.prefab";

        // ---------------------------------------------------------------- 老端源图(均已确认在 Assets/GameRes 下)
        private const string IMG_VIP_BG = "resource/game/mainUI/texture/mainui_icon_vip.png";
        private const string IMG_VIP = "resource/game/mainUI/texture/x_vip0.png";
        private const string IMG_RED_DOT = "resource/game/mainUI/texture/com_red_point.png";
        private const string IMG_RECHARGE_BG = "resource/game/mainUI/texture/mainui_icon_cz.png";
        private const string IMG_HALO = "resource/game/mainUI/texture/mainui_icon_zjgh.png";
        private const string IMG_CS = "resource/game/mainUI/texture/mainui_service_icon.png";
        private const string IMG_MAP_BG = "resource/game/mainUI/texture/map_bg_1.png";
        private const string IMG_WIFI = "resource/game/mainUI/texture/mainui_ui_3_3.png";
        private const string IMG_POWER_BG = "resource/game/mainUI/texture/mainui_ui_3_6.png";
        private const string IMG_POWER = "resource/game/mainUI/texture/mainui_ui_3_5.png";
        private const string IMG_HEAD_BORDER = "resource/game/mainUI/texture/mainui_role_border.png";
        private const string IMG_HP = "resource/game/mainUI/texture/uizjmv3_006.png";
        private const string IMG_BUFF_COUNT_BG = "resource/game/mainUI/texture/mainui_bufft_btn1.png";
        // 缺图:老端 head/texture/4005.png 全库(含 h5/cdn/Assets/GameRes)都没有,只有 1001/1003/2001/3001/4001
        // 五张头像;TrySetSprite 会自动回退占位色,报告里记入缺图清单,不影响结构。
        private const string IMG_HEAD_DEFAULT = "resource/game/head/texture/4005.png";
        private const string IMG_FIGHT = "resource/game/mainUI/texture/ui_zl.png";
        private const string IMG_LV_BG = "resource/game/mainUI/texture/mainui_head_circle_blue.png";
        private const string IMG_FIGHT_MODE = "resource/game/mainUI/texture/pkstatus_flag_0.png";
        private const string IMG_MONEY_BG = "resource/game/mainUI/texture/ui_money_bg_1.png";
        private const string IMG_MONEY_ICON_DIAMOND = "resource/game/mainUI/texture/diamond.png";
        private const string IMG_MONEY_ADD = "resource/game/mainUI/texture/ui_money_add_btn.png";
        // bufficon/4203001.png 只在 goodsicon/ 下有(不在 bufficon/),模板只建 1 个代表性图标(运行时会被
        // MainUITopBuffItem.SetData 换成真实 buff 图标),用确认存在的 guard1.png。
        private const string IMG_BUFF_ICON_DEFAULT = "resource/game/bufficon/texture/guard1.png";
        private const string IMG_BUFF_MASK = "resource/game/mainUI/texture/mainui_bufft_bg.png";

        // 顶部状态条设计画布(对标快照根节点 720 宽;高度按 _box_head/_box_map 结算高度 113/111 起步留余量)
        private const float TopBarWidth = UiCreatorKit.DesignWidth;
        private const float TopBarHeight = 130f;

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudTop(顶部状态条)",
                Note = "头像血条战力等级 + 货币三连 + vip/充值/光环/客服 + 小地图时间坐标",
                Order = 10,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建(与 Login 样板一致的防御写法),建完再激活。
            RectTransform root = UiCreatorKit.NewRoot("HudTop");
            // root 只扎顶部这一条(不再全屏 Stretch):锚顶部居中、宽 720×高 130,与内容实际占位一致。
            // 并入 MainUIModule 时因锚点非(0,0)-(1,1),总装的 offset 归零会自动跳过它,保持顶部条摆位。
            AnchorTop(root, TopBarWidth, TopBarHeight);
            root.gameObject.SetActive(false);

            RectTransform topViewRt = UiCreatorKit.NewNode("MainUITopView", root);
            UiCreatorKit.Stretch(topViewRt); // 视图填满有界 root(720×130);子节点仍按 720×130 画布换算,坐标不动
            var view = topViewRt.gameObject.AddComponent<MainUITopView>();

            RectTransform boxRoot = UiCreatorKit.NewNode("TopBarContentRoot", topViewRt); // 老端: _box_root
            UiCreatorKit.Stretch(boxRoot);
            view._box_root = boxRoot;

            // 隐藏模板统一挂在视图根(topViewRt)下的 __Templates,不与货币/buff 的真实克隆容器混放。
            RectTransform templates = NewTemplatesWrapper(topViewRt);

            BuildIconBox(boxRoot, view);
            BuildMoneyBox(boxRoot, view, templates);
            BuildMapBox(boxRoot, view);
            BuildHeadBox(boxRoot, view, templates);

            // 老端 MainUITopView.ts 没有任何地方 new CustomHeadItem(...)/new ActivityIcon(...),
            // 这两个 Bind 字段刻意留空(见类注释),HideUnbackedIndicators 对它们做了 null 判空。
            view._tpl_CustomHeadItem = null;
            view._tpl_ActivityIcon = null;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudTop.prefab 已生成: " + PrefabPath +
                      "(顶部状态条区域;真机包前记得跑 Addressable 自动分组)");
        }

        // ---------------------------------------------------------------- _box_icon(vip/充值/光环/客服)

        private static void BuildIconBox(Transform boxRoot, MainUITopView view)
        {
            // 老端 _box_icon: x=285,y=38,w=324,h=72(4 格 72 宽 + 12 间距;对标 ReflowIconBox 常量)。
            RectTransform icon = UiCreatorKit.NewNode("QuickAccessRow", boxRoot); // 老端: _box_icon
            PlaceLaya(icon, 275.1f, 38f, 324f, 72f, TopBarWidth, TopBarHeight); // x 285→275.1:回写手改(存档 65393a5ea),与 MiniMapBox 同向左移
            view._box_icon = icon;

            // 四个图标格用左上锚(PlaceTopLeft):ReflowIconBox 运行时直接改 anchoredPosition.x 靠左紧排。
            RectTransform vip = UiCreatorKit.NewNode("VipEntry", icon); // 老端: _box_vip
            PlaceTopLeft(vip, 0f, 0f, 72f, 72f);
            view._box_vip = vip;

            Image vipBg = UiCreatorKit.NewImage("VipEntryBg", vip); // 老端: _img_vip_bg
            PlaceLaya(vipBg.rectTransform, 12f, -3f, 72f, 72f, 72f, 72f);
            vipBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(vipBg, IMG_VIP_BG, UiCreatorKit.Palette.Panel);
            view._img_vip_bg = vipBg;

            Image vipLv = UiCreatorKit.NewImage("VipLevelIcon", vip); // 老端: _img_vip
            PlaceLaya(vipLv.rectTransform, 46f, 34f, 81f, 49f, 72f, 72f, 0.5f, 0f);
            vipLv.raycastTarget = false;
            UiCreatorKit.TrySetSprite(vipLv, IMG_VIP, UiCreatorKit.Palette.BtnSecond);
            view._img_vip = vipLv;

            Image vipRed = UiCreatorKit.NewImage("VipRedDot", vip); // 老端: _img_vip_red
            PlaceLaya(vipRed.rectTransform, 46f, 0f, 26f, 27f, 72f, 72f);
            vipRed.raycastTarget = false;
            UiCreatorKit.TrySetSprite(vipRed, IMG_RED_DOT, UiCreatorKit.Palette.Mark);
            vipRed.gameObject.SetActive(false);
            view._img_vip_red = vipRed;

            RectTransform recharge = UiCreatorKit.NewNode("RechargeEntry", icon); // 老端: _box_recharge
            PlaceTopLeft(recharge, 84f, 0f, 72f, 72f);
            view._box_recharge = recharge;

            Image rechargeBg = UiCreatorKit.NewImage("RechargeEntryBg", recharge); // 老端: _img_recharge_bg
            PlaceLaya(rechargeBg.rectTransform, 0f, 0f, 72f, 72f, 72f, 72f);
            rechargeBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(rechargeBg, IMG_RECHARGE_BG, UiCreatorKit.Palette.Panel);
            view._img_recharge_bg = rechargeBg;

            Image rechargeRed = UiCreatorKit.NewImage("RechargeRedDot", recharge); // 老端: _img_recharge_red
            PlaceLaya(rechargeRed.rectTransform, 46f, 0f, 26f, 27f, 72f, 72f);
            rechargeRed.raycastTarget = false;
            UiCreatorKit.TrySetSprite(rechargeRed, IMG_RED_DOT, UiCreatorKit.Palette.Mark);
            rechargeRed.gameObject.SetActive(false);
            view._img_recharge_red = rechargeRed;

            RectTransform halo = UiCreatorKit.NewNode("HaloEntry", icon); // 老端: _box_halo
            PlaceTopLeft(halo, 168f, 0f, 72f, 72f);
            view._box_halo = halo;

            Image haloImg = UiCreatorKit.NewImage("HaloEntryIcon", halo); // 老端: _img_halo
            PlaceLaya(haloImg.rectTransform, 0f, 0f, 72f, 72f, 72f, 72f);
            haloImg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(haloImg, IMG_HALO, UiCreatorKit.Palette.Panel);
            view._img_halo = haloImg;

            Image haloRed = UiCreatorKit.NewImage("HaloRedDot", halo); // 老端: _img_halo_red
            PlaceLaya(haloRed.rectTransform, 46f, 0f, 23f, 23f, 72f, 72f);
            haloRed.raycastTarget = false;
            UiCreatorKit.TrySetSprite(haloRed, IMG_RED_DOT, UiCreatorKit.Palette.Mark);
            haloRed.gameObject.SetActive(false);
            view._img_halo_red = haloRed;

            RectTransform cs = UiCreatorKit.NewNode("CustomerServiceEntry", icon); // 老端: _box_cs
            PlaceTopLeft(cs, 252f, 0f, 72f, 72f);
            view._box_cs = cs;

            Image csImg = UiCreatorKit.NewImage("CustomerServiceIcon", cs); // 老端: _img_cs
            PlaceLaya(csImg.rectTransform, 4f, 4f, 64f, 64f, 72f, 72f);
            csImg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(csImg, IMG_CS, UiCreatorKit.Palette.Panel);
            view._img_cs = csImg;

            Image csRed = UiCreatorKit.NewImage("CustomerServiceRedDot", cs); // 老端: _img_cs_red
            PlaceLaya(csRed.rectTransform, 45f, 0f, 23f, 23f, 72f, 72f);
            csRed.raycastTarget = false;
            UiCreatorKit.TrySetSprite(csRed, IMG_RED_DOT, UiCreatorKit.Palette.Mark);
            csRed.gameObject.SetActive(false);
            view._img_cs_red = csRed;
        }

        // ---------------------------------------------------------------- _box_money(货币三连)

        private static void BuildMoneyBox(Transform boxRoot, MainUITopView view, Transform templates)
        {
            // 老端 _box_money(HBox): x=311,y=2,w=400,h=34。运行时 BuildMoneyItems 只 Instantiate 不改
            // 位置,故这里补 HorizontalLayoutGroup 做自动排列(对标 Laya HBox:item 132 宽 + 2 间距)。
            RectTransform money = UiCreatorKit.NewNode("CurrencyRow", boxRoot); // 老端: _box_money
            PlaceLaya(money, 311f, 2f, 400f, 34f, TopBarWidth, TopBarHeight);
            var hbox = money.gameObject.AddComponent<HorizontalLayoutGroup>();
            hbox.childAlignment = TextAnchor.MiddleLeft;
            hbox.spacing = 2f;
            hbox.childForceExpandWidth = false;
            hbox.childForceExpandHeight = false;
            hbox.childControlWidth = false;
            hbox.childControlHeight = false;
            view._box_money = money;

            // 模板挪进视图根下的 __Templates(不与 money 行里真实克隆出来的货币项混放)。
            GameObject tpl = BuildMoneyItemTemplate(templates);
            view._tpl_MainUIMoneyItem = tpl;
        }

        /// <summary>对标 resource/game/mainUI/MainUIMoneyItem.json,画布 132x34。未激活模板,挂 MainUIMoneyItem
        /// 业务类并回填其 Bind 字段,运行时由 MainUITopView.BuildMoneyItems Instantiate 克隆 3 份。</summary>
        private static GameObject BuildMoneyItemTemplate(Transform parent)
        {
            const float w = 132f, h = 34f;
            RectTransform root = UiCreatorKit.NewNode("MainUIMoneyItem", parent); // 模板类名,不改
            UiCreatorKit.Place(root, 0f, 0f, w, h);
            var item = root.gameObject.AddComponent<MainUIMoneyItem>();

            Image bg = UiCreatorKit.NewImage("MoneyItemBg", root); // 老端: _img_bg
            PlaceLaya(bg.rectTransform, 0f, 0f, w, h, w, h);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_MONEY_BG, UiCreatorKit.Palette.Panel);
            item._img_bg = bg;

            Image icon = UiCreatorKit.NewImage("CurrencyIcon", root); // 老端: _img_icon
            PlaceLaya(icon.rectTransform, 8f, 4f, 25f, 25f, w, h);
            icon.raycastTarget = false;
            UiCreatorKit.TrySetSprite(icon, IMG_MONEY_ICON_DIAMOND, UiCreatorKit.Palette.BtnPrimary);
            item._img_icon = icon;

            Image add = UiCreatorKit.NewImage("CurrencyAddIcon", root); // 老端: _img_add
            PlaceLaya(add.rectTransform, 99f, 3f, 30f, 30f, w, h);
            add.raycastTarget = false;
            UiCreatorKit.TrySetSprite(add, IMG_MONEY_ADD, UiCreatorKit.Palette.Mark);
            item._img_add = add;

            // 老端 _lb_value 宽度随文本自适应(129/55/103 三种);模板固定给个够用起步宽度,左对齐贴 x=37。
            TextMeshProUGUI value = UiCreatorKit.NewText("CurrencyValueLabel", root, "0"); // 老端: _lb_value
            // y 9→4、高 h(=34)→25:回写手改(存档 65393a5ea),把 34 高的文本框在 34 高的条里居中。
            // 注意第 4 参原本传的是变量 h(既当节点高又当父高),这里必须换成字面量 25f。
            // 本模板运行时被 BuildMoneyItems 克隆 3 份,影响全部货币项。
            PlaceLaya(value.rectTransform, 37f, 4f, 90f, 25f, w, h);
            value.fontSize = 19f; // 老端 fontSize 38 * scaleX 0.5(内部倍字号缩放技巧)
            value.alignment = TextAlignmentOptions.Left;
            value.color = Color.white;
            item._lb_value = value;

            // 装钱数字变化特效占位容器(业务代码当前未使用,仅回填结构)。
            RectTransform effect = UiCreatorKit.NewNode("CurrencyChangeEffectSlot", root); // 老端: _box_effect
            PlaceLaya(effect, 20f, 16f, 150f, 150f, w, h, 0.5f, 0.5f);
            item._box_effect = effect;

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ---------------------------------------------------------------- _box_map(小地图/场景名/时间)

        private static void BuildMapBox(Transform boxRoot, MainUITopView view)
        {
            // 老端 _box_map: x=617,y=39,w=125,h=72。
            RectTransform map = UiCreatorKit.NewNode("MiniMapBox", boxRoot); // 老端: _box_map
            // x 617→594.1:回写手改(存档 65393a5ea)。原值右边缘 = 617+125 = 742,超出 720 设计画布 22px;
            // 拉回后右边缘 719.1,贴边留 0.9px。
            PlaceLaya(map, 594.1f, 39f, 125f, 72f, TopBarWidth, TopBarHeight);
            view._box_map = map;
            const float w = 125f, h = 72f;

            Image mapBg = UiCreatorKit.NewImage("MiniMapBg", map); // 老端: _img_map_bg
            PlaceLaya(mapBg.rectTransform, 0f, 0f, w, h, w, h);
            mapBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(mapBg, IMG_MAP_BG, UiCreatorKit.Palette.Panel);
            view._img_map_bg = mapBg;

            TextMeshProUGUI sceneName = UiCreatorKit.NewText("SceneNameLabel", map, "未知场景"); // 老端: _lb_scene_name
            PlaceLaya(sceneName.rectTransform, 62f, 26f, 144f, 18f, w, h, 0.5f, 0f);
            sceneName.fontSize = 18f; // 36 * 0.5
            sceneName.alignment = TextAlignmentOptions.Center;
            sceneName.color = Color.white;
            view._lb_scene_name = sceneName;

            // 老端默认 visible=True,业务代码 RefreshRole 里每次都强制 SetActive(false),留可见不影响运行时。
            TextMeshProUGUI rolePos = UiCreatorKit.NewText("RolePositionLabel", map, ""); // 老端: _lb_role_pos
            PlaceLaya(rolePos.rectTransform, 0f, 48f, 125f, 14f, w, h);
            rolePos.fontSize = 14f; // 28 * 0.5
            rolePos.alignment = TextAlignmentOptions.Center;
            rolePos.color = ParseColor("#fffd50", Color.yellow);
            view._lb_role_pos = rolePos;

            Image wifi = UiCreatorKit.NewImage("WifiIcon", map); // 老端: _img_wifi
            PlaceLaya(wifi.rectTransform, 103.4f, 0.2f, 20f, 16f, w, h); // 95,2→103.4,0.2:回写手改,与三件套对齐
            wifi.raycastTarget = false;
            UiCreatorKit.TrySetSprite(wifi, IMG_WIFI, UiCreatorKit.Palette.BtnNeutral);
            view._img_wifi = wifi;

            TextMeshProUGUI time = UiCreatorKit.NewText("SystemClockLabel", map, "00:00"); // 老端: _lb_time
            // 12,3,61,28 → 13.993,0.2,54.607,16:回写手改(存档 65393a5ea)。
            // 原值时钟右边缘 73 压住电池左边缘 58(重叠 14px),且高 28 与电池/wifi 的 16 不齐;
            // 改后三件套(时钟/电池/wifi)统一 y=0.2、高 16,横向互不重叠。
            PlaceLaya(time.rectTransform, 13.993f, 0.2f, 54.607f, 16f, w, h);
            time.fontSize = 14f; // 28 * 0.5
            time.alignment = TextAlignmentOptions.Left;
            time.color = Color.white;
            view._lb_time = time;

            Image powerBg = UiCreatorKit.NewImage("BatteryIconBg", map); // 老端: _img_power_bg
            PlaceLaya(powerBg.rectTransform, 68.6f, 0.2f, 30f, 16f, w, h); // 58,2→68.6,0.2:回写手改,让开时钟并与三件套对齐同一条 16px 带
            powerBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(powerBg, IMG_POWER_BG, UiCreatorKit.Palette.BtnNeutral);
            view._img_power_bg = powerBg;

            Image power = UiCreatorKit.NewImage("BatteryLevelIcon", powerBg.transform); // 老端: _img_power
            PlaceLaya(power.rectTransform, 3f, 4f, 22f, 10f, 30f, 16f);
            power.raycastTarget = false;
            UiCreatorKit.TrySetSprite(power, IMG_POWER, UiCreatorKit.Palette.Mark);
            view._img_power = power;
        }

        // ---------------------------------------------------------------- _box_head(头像/血条/战力/等级/buff/战斗模式)

        private static void BuildHeadBox(Transform boxRoot, MainUITopView view, Transform templates)
        {
            // 老端 _box_head: x=0,y=0,w=286,h=113。
            RectTransform head = UiCreatorKit.NewNode("PlayerHeadBox", boxRoot); // 老端: _box_head
            PlaceLaya(head, 0f, 0f, 286f, 113f, TopBarWidth, TopBarHeight);
            view._box_head = head;
            const float w = 286f, h = 113f;

            Image headBg = UiCreatorKit.NewImage("PlayerHeadPanelBg", head); // 老端: _img_head_bg
            PlaceLaya(headBg.rectTransform, 1f, 4f, 286f, 97f, w, h);
            headBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(headBg, IMG_HEAD_BORDER, UiCreatorKit.Palette.Panel);
            view._img_head_bg = headBg;

            // 血条:左上锚 + 左中枢轴(pivot.x=0),配合 SetHpBarWidth 的 SetSizeWithCurrentAnchors
            // 从右边收缩、左边固定(对标老端血条视觉)。
            Image hp = UiCreatorKit.NewImage("HpBar", head); // 老端: _img_hp
            PlaceTopLeft(hp.rectTransform, 89f, 44f, 182f, 14f, 0f);
            hp.raycastTarget = false;
            UiCreatorKit.TrySetSprite(hp, IMG_HP, UiCreatorKit.Palette.BtnPrimary);
            view._img_hp = hp;

            BuildBuffBox(head, view, w, h, templates);

            Image headImg = UiCreatorKit.NewImage("AvatarImage", head); // 老端: _img_head
            PlaceLaya(headImg.rectTransform, 47f, 47f, 76f, 76f, w, h, 0.5f, 0.5f);
            // AvatarImage(老端 _img_head)是 WireTopButtons 里 BindNav(_img_head,"setting") 的命中体,保留默认 raycastTarget=true。
            UiCreatorKit.TrySetSprite(headImg, IMG_HEAD_DEFAULT, UiCreatorKit.Palette.BtnSecond);
            view._img_head = headImg;

            Image frame = UiCreatorKit.NewImage("AvatarFrameDecor", head); // 老端: _img_frame
            PlaceLaya(frame.rectTransform, 47f, 47f, 125f, 125f, w, h, 0.5f, 0.5f);
            frame.raycastTarget = false;
            frame.color = new Color(1f, 1f, 1f, 0f);
            frame.gameObject.SetActive(false); // 无头像框装扮时隐藏(对标 RefreshHeadFrame 默认分支)
            view._img_frame = frame;

            Image fight = UiCreatorKit.NewImage("CombatPowerIcon", head); // 老端: _img_fight
            PlaceLaya(fight.rectTransform, 89f, 8f, 47f, 24f, w, h);
            fight.raycastTarget = false;
            UiCreatorKit.TrySetSprite(fight, IMG_FIGHT, UiCreatorKit.Palette.BtnPrimary);
            view._img_fight = fight;

            TextMeshProUGUI hpLabel = UiCreatorKit.NewText("HpValueLabel", head, "0/0"); // 老端: _lb_hp
            // 171,42,300,36 → 180,44,182,14:回写手改(存档 65393a5ea)。
            // 新值与血条 HpBar 的 PlaceTopLeft(hp, 89f, 44f, 182f, 14f, 0f) 逐字一致 —— 意图是让血量文字
            // 精确叠在血条 rect 上,而非原来那个 300×36 的松散框。
            PlaceLaya(hpLabel.rectTransform, 180f, 44f, 182f, 14f, w, h, 0.5f, 0f);
            hpLabel.fontSize = 18f; // 36 * 0.5
            hpLabel.alignment = TextAlignmentOptions.Center;
            hpLabel.color = Color.white;
            view._lb_hp = hpLabel;

            TextMeshProUGUI fighting = UiCreatorKit.NewText("CombatPowerLabel", head, "0"); // 老端: _lb_fighting
            PlaceLaya(fighting.rectTransform, 140f, 9f, 147f, 22f, w, h); // 宽 256→147:回写手改,纯收紧空文本框,x/y 不变(左边缘仍在 -3)
            fighting.fontSize = 20f; // 老端未做 2x/0.5 缩放
            fighting.alignment = TextAlignmentOptions.Left;
            fighting.color = ParseColor("#ffe57b", Color.yellow);
            view._lb_fighting = fighting;

            BuildLevelBox(head, view, w, h);
            BuildFightModeBox(head, view, w, h);
        }

        private static void BuildBuffBox(Transform head, MainUITopView view, float headW, float headH, Transform templates)
        {
            // 老端 _box_buff: x=91,y=73,w=188,h=30。
            RectTransform buff = UiCreatorKit.NewNode("BuffRow", head); // 老端: _box_buff
            PlaceLaya(buff, 91f, 73f, 188f, 30f, headW, headH);
            view._box_buff = buff;
            const float w = 188f, h = 30f;

            Image countBg = UiCreatorKit.NewImage("BuffOverflowCountBg", buff); // 老端: _img_buff_count_bg
            PlaceLaya(countBg.rectTransform, 143f, -1f, 32f, 32f, w, h);
            countBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(countBg, IMG_BUFF_COUNT_BG, UiCreatorKit.Palette.BtnNeutral);
            countBg.gameObject.SetActive(false); // HideUnbackedIndicators 运行时同样强制隐藏
            view._img_buff_count_bg = countBg;

            TextMeshProUGUI count = UiCreatorKit.NewText("BuffOverflowCountLabel", countBg.transform, "+1"); // 老端: _lb_buff_other_count
            PlaceLaya(count.rectTransform, 16f, 8f, 37f, 32f, 32f, 32f, 0.5f, 0f);
            count.fontSize = 16f; // 32 * 0.5
            count.alignment = TextAlignmentOptions.Left;
            count.color = Color.black;
            view._lb_buff_other_count = count;

            // 模板挪进视图根下的 __Templates(不与 buff 行里真实显示的项混放)。
            GameObject tpl = BuildBuffItemTemplate(templates, w, h);
            view._tpl_MainUITopBuffItem = tpl;
        }

        /// <summary>对标 resource/game/mainUI/MainUITopBuffItem.json,画布 30x30。未激活模板,挂
        /// MainUITopBuffItem 业务类并回填其 Bind 字段,摆在老端第 0 个槽位(SetPosition(0,0))。</summary>
        private static GameObject BuildBuffItemTemplate(Transform parent, float parentW, float parentH)
        {
            const float w = 30f, h = 30f;
            RectTransform root = UiCreatorKit.NewNode("MainUITopBuffItem", parent); // 模板类名,不改
            PlaceLaya(root, 0f, 0f, w, h, parentW, parentH);
            var item = root.gameObject.AddComponent<MainUITopBuffItem>();

            Image bg = UiCreatorKit.NewImage("BuffItemBg", root); // 老端: _img_bg
            PlaceLaya(bg.rectTransform, 0f, 0f, w, h, w, h);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_BUFF_ICON_DEFAULT, UiCreatorKit.Palette.Panel);
            item._img_bg = bg;

            Image mask = UiCreatorKit.NewImage("BuffItemCdMask", root); // 老端: _img_mask
            PlaceLaya(mask.rectTransform, 0f, 0f, w, h, w, h);
            mask.raycastTarget = false;
            UiCreatorKit.TrySetSprite(mask, IMG_BUFF_MASK, UiCreatorKit.Palette.Panel);
            mask.gameObject.SetActive(false); // 圆形 CD 遮罩未移植(MainUITopBuffItem.SetData 也强制隐藏)
            item._img_mask = mask;

            // 老端 CD 文本 x=15,y=15,w=0,h=0(自适应宽,数据未接时为空);模板给个居中偏下的起步框。
            TextMeshProUGUI time = UiCreatorKit.NewText("BuffItemCountdownLabel", root, ""); // 老端: _lb_time
            UiCreatorKit.Place(time.rectTransform, 0f, -8f, 28f, 14f);
            time.fontSize = 18f;
            time.alignment = TextAlignmentOptions.Center;
            time.color = ParseColor("#00fa64", Color.green);
            item._lb_time = time;

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static void BuildLevelBox(Transform head, MainUITopView view, float headW, float headH)
        {
            // 老端 _box_lv: x=1,y=3,w=40,h=38。
            RectTransform lv = UiCreatorKit.NewNode("LevelBadge", head); // 老端: _box_lv
            PlaceLaya(lv, 1f, 3f, 40f, 38f, headW, headH);
            view._box_lv = lv;
            const float w = 40f, h = 38f;

            Image lvBg = UiCreatorKit.NewImage("LevelBadgeBg", lv); // 老端: _img_lv_bg
            PlaceLaya(lvBg.rectTransform, 20f, 19f, 52f, 52f, w, h, 0.5f, 0.5f);
            lvBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(lvBg, IMG_LV_BG, UiCreatorKit.Palette.BtnSecond);
            view._img_lv_bg = lvBg;

            TextMeshProUGUI lvLabel = UiCreatorKit.NewText("LevelLabel", lv, "1"); // 老端: _lb_lv
            PlaceLaya(lvLabel.rectTransform, 20f, 10f, 80f, 40f, w, h, 0.5f, 0f);
            lvLabel.fontSize = 18f; // 36 * 0.5
            lvLabel.alignment = TextAlignmentOptions.Center;
            lvLabel.color = Color.white; // 对标 RefreshLevel 默认(未超转生等级)分支
            view._lb_lv = lvLabel;
        }

        private static void BuildFightModeBox(Transform head, MainUITopView view, float headW, float headH)
        {
            // 老端 _box_fight_mode: x=13,y=81,w=68,h=26。
            RectTransform mode = UiCreatorKit.NewNode("FightModeBadge", head); // 老端: _box_fight_mode
            PlaceLaya(mode, 13f, 81f, 68f, 26f, headW, headH);
            view._box_fight_mode = mode;
            const float w = 68f, h = 26f;

            Image modeImg = UiCreatorKit.NewImage("FightModeIcon", mode); // 老端: _img_fight_mode
            PlaceLaya(modeImg.rectTransform, -4f, -6f, 71f, 37f, w, h);
            modeImg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(modeImg, IMG_FIGHT_MODE, UiCreatorKit.Palette.BtnNeutral);
            view._img_fight_mode = modeImg;
        }

        // ---------------------------------------------------------------- 布局换算 helper(本文件私有)

        /// <summary>顶部整条状态栏根节点锚顶(对标老端 display_obj.centerX=0/top=0):水平居中、顶边贴顶。</summary>
        private static void AnchorTop(RectTransform rt, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 把 Laya 节点的 (x,y,w,h,anchorX,anchorY)(父左上原点,y 向下;anchorX/anchorY 是节点自身锚点
        /// 在 0~1 的位置,0=以左/上边为基准即 x/y 就是左上角,0.5=以自身中点为基准即 x/y 已是中心)
        /// 换算成 UiCreatorKit.Place 的父中心锚坐标并摆位。parentW/parentH 用该层级子节点公用的设计画布
        /// 宽高(取自运行时快照里该容器的已结算尺寸,不是 Laya 编辑期的 auto/0 值)。
        /// </summary>
        private static void PlaceLaya(RectTransform rt, float x, float y, float w, float h,
            float parentW, float parentH, float anchorX = 0f, float anchorY = 0f)
        {
            float centerTopLeftX = x + (0.5f - anchorX) * w;
            float centerTopLeftY = y + (0.5f - anchorY) * h;
            float cx = centerTopLeftX - parentW / 2f;
            float cy = -(centerTopLeftY - parentH / 2f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        /// <summary>
        /// 左上锚 + 左上枢轴摆位(anchoredPosition 直接等于左边距/顶边距,与 Laya 原生坐标系一致)。
        /// 仅服务两处运行时硬约束(见类注释):_box_icon 的四个图标格、血条 _img_hp。其余节点一律用
        /// PlaceLaya(中心锚),对齐 UiCreatorKit/其它 Creator 的统一约定。
        /// </summary>
        private static void PlaceTopLeft(RectTransform rt, float x, float y, float w, float h, float pivotX = 0f)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(pivotX, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x + pivotX * w, -y);
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : fallback;
        }

        /// <summary>
        /// 隐藏模板容器(对标 HudOverlayPopupsCreator.NewTemplatesWrapper 的同名约定,复制一份到本文件,
        /// 不跨文件引用):MainUIMoneyItem / MainUITopBuffItem 两个未激活模板统一挂在视图根下的
        /// "__Templates",与货币行/buff 行里真实承载克隆体的容器分开,自身默认 inactive。
        /// </summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        // ---------------------------------------------------------------- 预览

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudTop",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "MainUITopView 是 MainUIModule 内「模块合并 prefab」子窗口(BaseView.Show 由业务流程" +
                    "直接管理),没有 [UIView] 特性、不走 ViewManager.Open。预览改为:直接从最新 prefab " +
                    "实例化到 Main 层并调用 view.Show(),仅用于看结构/试交互,不代表真机的加载路径。",
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
                Debug.LogError("[UiCreator] 预览失败,找不到 " + PrefabPath + "(请先点生成)");
                return;
            }

            Transform layer = ViewManager.GetLayer(UILayer.Main);
            _previewInstance = Object.Instantiate(prefab, layer);
            var view = _previewInstance.GetComponentInChildren<MainUITopView>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] 预览失败,prefab 缺 MainUITopView 组件");
                return;
            }
            view.Show();
            Selection.activeObject = _previewInstance;
        }
    }
}
