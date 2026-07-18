using System.Threading.Tasks;
using Shenxiao.Editor.LayaUI;
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
    /// 主界面「战斗类弹层组」纯代码建树生成器,共两类产物:
    ///
    /// 1) HudOverlayCombat.prefab(bundle):打BOSS大血条(MainUIHiterBigBloodView)+ 复活(MainUIReliveView)+
    ///    掉落/采集进度条(DropProgress)+ 飘花/红包特效容器(FlowerEffectView)+ 主UI特效层(MainUIEffectView)+
    ///    伙伴技特效层(MainUIEffectPartnerSkillView)六个子视图合一,全部默认 inactive,事件驱动开合;
    ///    之后由"总装"把这棵树整体嵌进 MainUIModule.prefab(本生成器不碰 MainUIModule)。
    /// 2) CollectBarView.prefab / FightingUpView.prefab:各自独立 prefab,MainUIFlow 用
    ///    GameResPath.GetUIPrefab("mainUI","CollectBarView"/"FightingUpView") 按精确文件名单独加载,
    ///    所以必须各存一份、文件名精确,不放进 bundle。
    ///
    /// 数据源:老端 e:/GitProject/yu_client/h5/bin/resource/game/mainUI/*.json(UTF-8,非文档旧注所述的 GBK;
    /// 已用 python 实测确认)。位置换算按 Laya 左上原点 → Unity 中心锚:
    ///   cx = x + w*(0.5-anchorX) - parentW/2,cy = parentH/2 - y - h*(0.5-anchorY)。
    /// MainUIHiterBigBloodView 与 FightingUpView 的【根节点】屏幕位置额外有运行态快照佐证
    /// (40_final_snap.json 抓到 FightingUpView 实际 x=168,y=765;HiterBigBlood 由同批调查记录 x=365,y=127),
    /// 直接换算入 HudOverlayCombat/FightingUpView.prefab 的根位置,不用瞎猜。
    ///
    /// 元素已贴老端真实源图(TrySetSprite),贴不到回退占位色。sizeGrid(九宫)图集中在
    /// bg_13.png / com_empty.png / mainUI_progress.png,三者的 Sprite 导入设置(spriteBorder)已被更早的
    /// 转换批次配好,这里只需把 Image.type 设 Sliced 即可生效,不用碰导入设置。
    /// 特效类视图(FlowerEffectView/MainUIEffectView/MainUIEffectPartnerSkillView)老端本就是无皮肤的纯容器,
    /// 按结构建 RectTransform 挂点即可,不建视觉占位。
    /// 入口在「神霄/重构UI 生成器」面板,Module="MainUI"。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   _box_con(大血条)          -> BloodBarContainer
    //   _img_bg(大血条)           -> HpBarBackgroundImage
    //   _img_next_blood          -> BloodBarTrackImage
    //   _img_dark_blood          -> BloodTrailBarImage
    //   _img_light_blood         -> BloodCurrentBarImage
    //   _lb_name                 -> BossNameLabel
    //   _lb_left_blood           -> RemainingCountLabel
    //   _lb_hp                   -> HpValueLabel
    //   _lb_anger                -> FatigueValueLabel
    //   _lb_vit                  -> VitalityValueLabel
    //   drop_gp                  -> DropTipPanel
    //   drop_lb                  -> DropTipLabel
    //   _img_head                -> MonsterHeadImage
    //   _box_head                -> MonsterHeadSlot
    //   _tpl_CustomHeadItem(节点名) -> CustomHeadItem(模板自身命名对齐类名约定,并改嵌进新建的 __Templates 隐藏容器)
    //   _img_bg(复活窗)           -> ReviveBackgroundImage
    //   _img_bg2                 -> ReviveTitleBannerImage
    //   _lb_left_count           -> ReviveCountdownNumberLabel
    //   _lb_des2                 -> ReviveDeathMessageLabel
    //   _lb_des                  -> ReviveCountdownHintLabel
    //   _Image1                  -> ProgressBackgroundImage
    //   _gp_progress             -> ProgressBarTrack
    //   _progress_bar            -> ProgressBarFill
    //   _img(掉落进度条)          -> ProgressIndicatorIcon
    //   _lb_tips                 -> PickupStatusLabel
    //   _group_eff               -> EffectAnchorGroup
    //   _gp_rpr                  -> RedPacketRainGroup
    //   _Group1~_Group14         -> RedPacketSpawnPoint1~RedPacketSpawnPoint14
    //   _box_eff(主UI特效层)      -> MainEffectAnchorBox
    //   _box_eff(伙伴技特效层)    -> PartnerSkillEffectAnchorBox
    //   _img_bg(采集条)          -> CollectBarBackgroundImage
    //   _img_progress            -> CollectProgressFillImage
    //   _img_num_bg              -> RepairPercentBackgroundImage
    //   _txt_progress            -> ProgressPercentLabel
    //   _img_state               -> CollectStatusImage
    //   _img_bg(战力飘字)         -> FightingUpBackgroundImage
    //   _lb_fight                -> FightPowerValueLabel
    //   _lb_add_fight            -> FightPowerDeltaLabel
    //   _box_effect              -> FightPowerEffectAnchor
    public static class HudOverlayCombatCreator
    {
        private const string BundlePrefabPath = "Assets/Prefabs/UI/MainUI/Overlays/HudOverlayCombat.prefab";
        private const string CollectBarPrefabPath = "Assets/Prefabs/UI/MainUI/CollectBarView.prefab";
        private const string FightingUpPrefabPath = "Assets/Prefabs/UI/MainUI/FightingUpView.prefab";
        private const string CustomHeadItemPrefabPath = "Assets/Prefabs/UI/Common/CustomHeadItem.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)
        private const string IMG_HITER_BG = "resource/game/mainUI/texture/mainui_ui_20.png";
        private const string IMG_HITER_NEXT = "resource/game/mainUI/texture/mainui_boss_blood_2.png";
        private const string IMG_HITER_DARK = "resource/game/mainUI/texture/mainui_boss_blood_6.png";
        private const string IMG_HITER_LIGHT = "resource/game/mainUI/texture/mainui_boss_blood_1.png";
        private const string IMG_HITER_DROPBG = "resource/game/common4/texture/bg_13.png";

        private const string IMG_RELIVE_BG = "resource/game/common/texture/com_empty.png";

        private const string IMG_DROP_BG = "resource/game/mainUI/texture/mainUI_progressbg.png";
        private const string IMG_DROP_BAR = "resource/game/mainUI/texture/mainUI_progress.png";
        private const string IMG_DROP_TIP = "resource/game/mainUI/texture/mainUI_progress_tips.png";

        private const string IMG_COLLECT_BG = "resource/game/mainUI/texture/ui_cj_01.png";
        private const string IMG_COLLECT_PROGRESS = "resource/game/mainUI/texture/ui_cj_02.png";
        private const string IMG_COLLECT_NUMBG = "resource/game/mainUI/texture/uity_038.png";

        // 老端 FightingUpView.LoadSuccess 用 GameResPath.GetIcon("mainUI","manui_ui_fighting_bg") 运行时贴的背景光晕图
        // (JSON 里 _img_bg 本身无 skin),原图 398x95,比 385x115 的 view 略宽略矮,按真实像素铺(不拉伸变形)。
        private const string IMG_FIGHTING_BG = "resource/game/mainUI/texture/manui_ui_fighting_bg.png";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudOverlayCombat(战斗弹层×6)",
                Note = "大血条+复活+掉落进度+飘花+特效层+伙伴技特效,合一 bundle,全部默认 inactive,待总装嵌入 MainUIModule",
                Order = 70,
                Generate = GenerateBundle,
                Preview = PreviewBundle,
                PrefabPath = BundlePrefabPath,
            });
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "CollectBarView(采集条)",
                Note = "采集/任务采集进度条,独立 prefab,MainUIFlow 按精确文件名单独加载",
                Order = 71,
                Generate = GenerateCollectBar,
                Preview = PreviewCollectBar,
                PrefabPath = CollectBarPrefabPath,
            });
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "FightingUpView(战力飘字)",
                Note = "战力提升飘字提示,独立 prefab,MainUIFlow 按精确文件名单独加载",
                Order = 72,
                Generate = GenerateFightingUp,
                Preview = PreviewFightingUp,
                PrefabPath = FightingUpPrefabPath,
            });
        }

        // ============================================================ Bundle: HudOverlayCombat

        public static void GenerateBundle()
        {
            // 整棵树在 root 未激活时构建(对标 Login 样板,避免子节点 OnEnable 早于建树完成)。
            RectTransform root = UiCreatorKit.NewRoot("HudOverlayCombat");
            // root 改回【全屏 Stretch】。原先收成 260×160 的"居中分组把手"(拖根=整组平移),但那样 root 矩形
            // 只有屏幕正中一小块,子根就没法锚到屏幕边缘 —— 而 BOSS 大血条老端是右上对齐、FlowerEffectView
            // 老端是四边铺满,两者都需要一个与屏幕同尺寸的父矩形才能复刻。
            // 改全屏是零位移的:原 root 中心锚 pos(0,0),矩形中心就在屏幕中心;全屏后矩形=屏幕,中心仍在屏幕中心,
            // 因此所有用 UiCreatorKit.Place(中心锚)摆位的子根位置一律不变。
            // 代价是失去"拖此根=整组平移"的编辑器便利 —— 该便利与"子根要能贴屏幕边"本质冲突,取后者。
            UiCreatorKit.Stretch(root);
            root.gameObject.SetActive(false);

            // 六个子视图各自独立 BaseView,建完各自关掉(事件驱动弹层默认不显示),
            // 最后再把 bundle 根节点本身打开(根节点只是容器,不是 BaseView)。
            BuildHiterBigBlood(root).gameObject.SetActive(false);
            BuildRelive(root).gameObject.SetActive(false);
            BuildDropProgress(root).gameObject.SetActive(false);
            BuildFlowerEffect(root).gameObject.SetActive(false);
            BuildEffect(root).gameObject.SetActive(false);
            BuildEffectPartnerSkill(root).gameObject.SetActive(false);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, BundlePrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudOverlayCombat.prefab 已生成: " + BundlePrefabPath +
                      "(6 个子视图默认 inactive,待总装嵌入 MainUIModule;真机包前记得跑 Addressable 自动分组)");
        }

        /// <summary>打BOSS大血条(对标老端 MainUIHiterBigBloodView.json,327x78,含 15 个具名子节点)。</summary>
        private static RectTransform BuildHiterBigBlood(Transform parent)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIHiterBigBloodView", parent);
            // 运行态佐证:老端 x=365,y=127(720x1280 全屏画布下),327x78 → 中心锚 cx=168.5,cy=474。
            // 锚【屏幕右上角】:老端 MainUIHiterBigBloodView 是 x = stageWidth - width - Offset_X(右对齐)、
            // y = Offset_Y + 刘海高(顶部对齐),即右上贴边而非居中。由 x=365/宽327 反解 Offset_X = 720-365-327 = 28。
            // pivot 保持中心 (0.5,0.5) 不变,只改 anchor:pos = 节点中心相对右上角的偏移
            //   x = -(720 - 528.5) = -191.5   (528.5 = 365 + 327/2)
            //   y = -166                      (166 = 127 + 78/2)
            // 校验 720×1280:锚点(720,顶),中心 =(528.5, 距顶166),与原中心锚 Place(168.5,474) 完全一致(零位移)。
            // 刘海不在这里补:交给 UILayer.Main 上的 SafeAreaRoot 统一处理,补 60px 会与安全区叠成双倍内缩。
            // 旁证:老端 MainUIHiterBigBloodView.ts:117-118 残留的、被注释掉的 Unity 风格标注正是 anchorMin=anchorMax=(1,1)。
            root.anchorMin = root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(327f, 78f);
            root.anchoredPosition = new Vector2(-191.5f, -166f);
            var view = root.gameObject.AddComponent<MainUIHiterBigBloodView>();

            RectTransform boxCon = UiCreatorKit.NewNode("BloodBarContainer", root); // 老端: _box_con
            UiCreatorKit.Place(boxCon, 0f, 0f, 327f, 78f);
            view._box_con = boxCon;

            Image imgBg = UiCreatorKit.NewImage("HpBarBackgroundImage", boxCon); // 老端: _img_bg
            // 老端 anchorX=1 + scaleX=-1(右锚点再水平镜像)两次变换相互抵消,视觉上仍铺满 _box_con,
            // 建树期直接 Stretch 即可,不用还原镜像小技巧。
            UiCreatorKit.Stretch(imgBg.rectTransform);
            imgBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgBg, IMG_HITER_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = imgBg;

            Image imgNext = UiCreatorKit.NewImage("BloodBarTrackImage", boxCon); // 老端: _img_next_blood
            UiCreatorKit.Place(imgNext.rectTransform, -11.5f, -4f, 204f, 10f);
            imgNext.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgNext, IMG_HITER_NEXT, UiCreatorKit.Palette.BtnNeutral);
            view._img_next_blood = imgNext;

            Image imgDark = UiCreatorKit.NewImage("BloodTrailBarImage", boxCon); // 老端: _img_dark_blood
            imgDark.raycastTarget = false;
            SetRightPivotBar(imgDark.rectTransform, 90.5f, -4f, 204f, 10f);
            UiCreatorKit.TrySetSprite(imgDark, IMG_HITER_DARK, new Color(0.35f, 0.10f, 0.10f, 1f));
            view._img_dark_blood = imgDark;

            Image imgLight = UiCreatorKit.NewImage("BloodCurrentBarImage", boxCon); // 老端: _img_light_blood
            imgLight.raycastTarget = false;
            SetRightPivotBar(imgLight.rectTransform, 90.5f, -4f, 204f, 10f);
            UiCreatorKit.TrySetSprite(imgLight, IMG_HITER_LIGHT, new Color(0.85f, 0.20f, 0.20f, 1f));
            view._img_light_blood = imgLight;

            TextMeshProUGUI lbName = UiCreatorKit.NewText("BossNameLabel", boxCon, ""); // 老端: _lb_name
            UiCreatorKit.Place(lbName.rectTransform, -9.5f, 34f, 200f, 30f);
            lbName.alignment = TextAlignmentOptions.Right;
            lbName.fontSize = 18f;
            view._lb_name = lbName;

            TextMeshProUGUI lbLeftBlood = UiCreatorKit.NewText("RemainingCountLabel", boxCon, "X2"); // 老端: _lb_left_blood
            UiCreatorKit.Place(lbLeftBlood.rectTransform, -138f, 3f, 120f, 30f);
            lbLeftBlood.fontSize = 16f;
            SetColorHex(lbLeftBlood, "#48435b");
            view._lb_left_blood = lbLeftBlood;

            TextMeshProUGUI lbHp = UiCreatorKit.NewText("HpValueLabel", boxCon, ""); // 老端: _lb_hp
            UiCreatorKit.Place(lbHp.rectTransform, -31f, -13f, 120f, 30f);
            lbHp.fontSize = 21f;
            view._lb_hp = lbHp;

            TextMeshProUGUI lbAnger = UiCreatorKit.NewText("FatigueValueLabel", boxCon, "疲劳值：30"); // 老端: _lb_anger
            UiCreatorKit.Place(lbAnger.rectTransform, -11.5f, -26f, 200f, 30f);
            lbAnger.alignment = TextAlignmentOptions.Right;
            lbAnger.fontSize = 18f;
            lbAnger.gameObject.SetActive(false); // 老端 visible:false(业务 HideExtras 运行时也会再隐藏一次)
            view._lb_anger = lbAnger;

            TextMeshProUGUI lbVit = UiCreatorKit.NewText("VitalityValueLabel", boxCon, ""); // 老端: _lb_vit
            UiCreatorKit.Place(lbVit.rectTransform, -11.5f, -30.5f, 200f, 19f);
            lbVit.alignment = TextAlignmentOptions.Right;
            lbVit.fontSize = 18f;
            lbVit.gameObject.SetActive(false); // 老端 visible:false
            view._lb_vit = lbVit;

            Image dropGp = UiCreatorKit.NewImage("DropTipPanel", boxCon); // 老端: drop_gp
            UiCreatorKit.Place(dropGp.rectTransform, -27.5f, -40.5f, 260f, 35f);
            dropGp.raycastTarget = false;
            dropGp.type = Image.Type.Sliced; // sizeGrid 22,13,23,17(九宫;bg_13.png 的 spriteBorder 已配好,直接生效)
            UiCreatorKit.TrySetSprite(dropGp, IMG_HITER_DROPBG, UiCreatorKit.Palette.Panel);
            view.drop_gp = dropGp;

            // 老端 drop_lb 外面还包了一层匿名 HBox,纯布局壳、无绑定意义,建树期拉平不建。
            TextMeshProUGUI dropLb = UiCreatorKit.NewText("DropTipLabel", dropGp.transform, ""); // 老端: drop_lb
            UiCreatorKit.Stretch(dropLb.rectTransform);
            dropLb.fontSize = 20f;
            view.drop_lb = dropLb;

            // 老端与 _box_con 同级(不在 _box_con 内)。
            Image imgHead = UiCreatorKit.NewImage("MonsterHeadImage", root); // 老端: _img_head
            UiCreatorKit.Place(imgHead.rectTransform, 124.5f, 3f, 64f, 64f);
            imgHead.raycastTarget = false;
            imgHead.color = UiCreatorKit.Palette.Panel; // 老端无 skin(怪物头像待接 GetMonsterHeadPath,业务代码默认隐藏)
            view._img_head = imgHead;

            RectTransform boxHead = UiCreatorKit.NewNode("MonsterHeadSlot", root); // 老端: _box_head
            UiCreatorKit.Place(boxHead, 124.5f, 3f, 64f, 64f);
            view._box_head = boxHead;

            // _tpl_CustomHeadItem 老端 json 里没有(运行时动态挂的头像模板节点);嵌套现成的
            // Common/CustomHeadItem.prefab 作模板,对标仓库内 _tpl_XXX 命名/用法约定(Instantiate 克隆后用,自身常驻关闭)。
            // 模板统一收进本视图根节点下的 __Templates 隐藏容器,不与运行时挂点 MonsterHeadSlot 混放。
            RectTransform templatesRoot = NewTemplatesWrapper(root);
            GameObject tplHead = NestPrefab(CustomHeadItemPrefabPath, templatesRoot, "CustomHeadItem"); // 老端: _tpl_CustomHeadItem
            if (tplHead != null)
            {
                UiCreatorKit.Place((RectTransform)tplHead.transform, 0f, 0f, 64f, 64f);
                tplHead.SetActive(false);
            }
            view._tpl_CustomHeadItem = tplHead;

            return root;
        }

        /// <summary>复活倒计时窗(对标老端 MainUIReliveView.json,720x600,居中)。</summary>
        private static RectTransform BuildRelive(Transform parent)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIReliveView", parent);
            UiCreatorKit.Place(root, 0f, 0f, 720f, 600f); // 老端 centerX=0,centerY=0(整体居中)
            var view = root.gameObject.AddComponent<MainUIReliveView>();

            Image bg = UiCreatorKit.NewImage("ReviveBackgroundImage", root); // 老端: _img_bg
            UiCreatorKit.Place(bg.rectTransform, 0f, 122.5f, 720f, 355f);
            bg.raycastTarget = false;
            bg.type = Image.Type.Sliced; // sizeGrid 233,0,0,0(九宫;com_empty.png 的 spriteBorder 已配好,top=233)
            UiCreatorKit.TrySetSprite(bg, IMG_RELIVE_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = bg;

            Image bg2 = UiCreatorKit.NewImage("ReviveTitleBannerImage", root); // 老端: _img_bg2
            UiCreatorKit.Place(bg2.rectTransform, 12f, 192.5f, 302f, 83f);
            bg2.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg2, IMG_RELIVE_BG, UiCreatorKit.Palette.Panel);
            view._img_bg2 = bg2;

            TextMeshProUGUI lbLeftCount = UiCreatorKit.NewText("ReviveCountdownNumberLabel", root, "0"); // 老端: _lb_left_count
            UiCreatorKit.Place(lbLeftCount.rectTransform, 0f, 92f, 200f, 110f);
            lbLeftCount.fontSize = 90f;
            SetColorHex(lbLeftCount, "#9ef325");
            view._lb_left_count = lbLeftCount;

            TextMeshProUGUI lbDes2 = UiCreatorKit.NewText("ReviveDeathMessageLabel", root, "很遗憾！您已被击杀！"); // 老端: _lb_des2
            UiCreatorKit.Place(lbDes2.rectTransform, 0f, 134f, 320f, 40f);
            lbDes2.fontSize = 24f;
            SetColorHex(lbDes2, "#663915");
            view._lb_des2 = lbDes2;

            TextMeshProUGUI lbDes = UiCreatorKit.NewText("ReviveCountdownHintLabel", root, "自动复活倒计时"); // 老端: _lb_des
            UiCreatorKit.Place(lbDes.rectTransform, 0f, -13f, 320f, 40f);
            lbDes.fontSize = 24f;
            SetColorHex(lbDes, "#663915");
            view._lb_des = lbDes;

            return root;
        }

        /// <summary>掉落/采集进度条(对标老端 DropProgress.json,394x32)。</summary>
        private static RectTransform BuildDropProgress(Transform parent)
        {
            RectTransform root = UiCreatorKit.NewNode("DropProgress", parent);
            // 锚【屏幕底边】:老端 DropProgress.ts:27-28 是 centerX=0 + bottom=450(水平居中 + 底边距屏幕底 450)。
            // pivot 保持中心 (0.5,0.5),故 pos.y = 450 + 32/2 = 466(中心距底)。
            // ⚠ 这一处【在 720×1280 基准档也会移位】(中心距底 190 → 466),是有意修正而非回归:
            // 原值 -450 是本 Creator 自己标注的占位猜测(注释原文"无运行态佐证,占位默认屏幕位置"),
            // 不是快照实测值;老端 bottom=450 才是真值。验收时这一处需要单独截图确认。
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(394f, 32f);
            root.anchoredPosition = new Vector2(0f, 466f);
            var view = root.gameObject.AddComponent<DropProgress>();

            Image image1 = UiCreatorKit.NewImage("ProgressBackgroundImage", root); // 老端: _Image1
            UiCreatorKit.Place(image1.rectTransform, 0f, 0f, 394f, 32f);
            image1.raycastTarget = false;
            UiCreatorKit.TrySetSprite(image1, IMG_DROP_BG, UiCreatorKit.Palette.Panel);
            view._Image1 = image1;

            // 老端 anchorX=0(默认左上角枢轴):业务代码 StartDrop 只改 sizeDelta.x(10→满宽),
            // 需要左边固定往右长,故 pivot 设 (0, .5),anchoredPosition.x 记左边缘坐标(而非中心)。
            RectTransform gpProgress = UiCreatorKit.NewNode("ProgressBarTrack", root); // 老端: _gp_progress
            gpProgress.anchorMin = gpProgress.anchorMax = new Vector2(0.5f, 0.5f);
            gpProgress.pivot = new Vector2(0f, 0.5f);
            gpProgress.sizeDelta = new Vector2(347f, 20f); // 满宽(业务 OnInit 会缓存这个值当 _fullWidth)
            gpProgress.anchoredPosition = new Vector2(-184f, 0f);
            view._gp_progress = gpProgress;

            Image progressBar = UiCreatorKit.NewImage("ProgressBarFill", gpProgress); // 老端: _progress_bar
            // 老端 top/bottom/left/right=0,铺满 _gp_progress;Stretch 后会随 _gp_progress.sizeDelta 自动跟着变,不用另写代码。
            UiCreatorKit.Stretch(progressBar.rectTransform);
            progressBar.raycastTarget = false;
            progressBar.type = Image.Type.Sliced; // sizeGrid 5,10,5,10(九宫;mainUI_progress.png 的 spriteBorder 已配好)
            UiCreatorKit.TrySetSprite(progressBar, IMG_DROP_BAR, UiCreatorKit.Palette.BtnPrimary);
            view._progress_bar = progressBar;

            Image img = UiCreatorKit.NewImage("ProgressIndicatorIcon", gpProgress); // 老端: _img
            img.raycastTarget = false;
            // 同 _gp_progress 用左对齐坐标系,配合业务代码 SetIconX 的字面量(对标老端 ANCHORED_POSX=320)。
            // 取舍:老端静态场景里 _img 的资源摆位换算约在 x≈161.5(right=-3 锚,已含在 347 宽的 _gp_progress 局部坐标里),
            // 这其实是"动画终点"的设计稿姿态;但业务 OnInit 只在首次初始化时把预制体当前位置当成 _iconStartX 缓存一次,
            // 若照抄终点姿态会导致 _iconStartX==_iconEndX(320),动画变成原地不动。这里故意从左端(0)起步,
            // 配合已写死的 _iconEndX=320 才有实际滑动效果,不是抄漏,是刻意调整。
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.pivot = new Vector2(0f, 0.5f);
            img.rectTransform.sizeDelta = new Vector2(30f, 44f);
            img.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            UiCreatorKit.TrySetSprite(img, IMG_DROP_TIP, UiCreatorKit.Palette.BtnSecond);
            view._img = img;

            TextMeshProUGUI lbTips = UiCreatorKit.NewText("PickupStatusLabel", root, "拾取中..."); // 老端: _lb_tips
            UiCreatorKit.Place(lbTips.rectTransform, 0f, 0f, 200f, 32f);
            lbTips.fontSize = 16f;
            view._lb_tips = lbTips;

            return root;
        }

        /// <summary>婚礼花/红包雨特效容器(对标老端 FlowerEffectView.json,720x1280 全屏,纯挂点无皮肤)。</summary>
        private static RectTransform BuildFlowerEffect(Transform parent)
        {
            RectTransform root = UiCreatorKit.NewNode("FlowerEffectView", parent);
            // 老端 top/bottom/left/right=0 铺满全屏纯特效容器。之前因为 bundle 根收成了居中把手、Stretch 会
            // 跟着把手缩小,才退而用绝对 720×1280 中心锚;现在 bundle 根已改回全屏 Stretch,这里可以直接
            // Stretch 真正铺满 —— 绝对 720×1280 在宽屏下盖不住两侧,特效会露边。
            UiCreatorKit.Stretch(root);
            var view = root.gameObject.AddComponent<FlowerEffectView>();

            RectTransform groupEff = UiCreatorKit.NewNode("EffectAnchorGroup", root); // 老端: _group_eff
            UiCreatorKit.Stretch(groupEff);
            view._group_eff = groupEff;

            RectTransform gpRpr = UiCreatorKit.NewNode("RedPacketRainGroup", root); // 老端: _gp_rpr
            UiCreatorKit.Stretch(gpRpr);
            view._gp_rpr = gpRpr;

            view._Group1 = BuildGroup(gpRpr, "RedPacketSpawnPoint1", -216.5f, 425.5f); // 老端: _Group1
            view._Group2 = BuildGroup(gpRpr, "RedPacketSpawnPoint2", -76.5f, 608.5f); // 老端: _Group2
            view._Group3 = BuildGroup(gpRpr, "RedPacketSpawnPoint3", 254.5f, 573.5f); // 老端: _Group3
            view._Group4 = BuildGroup(gpRpr, "RedPacketSpawnPoint4", -40.5f, 356.5f); // 老端: _Group4
            view._Group5 = BuildGroup(gpRpr, "RedPacketSpawnPoint5", 145.5f, 393.5f); // 老端: _Group5
            view._Group6 = BuildGroup(gpRpr, "RedPacketSpawnPoint6", 188.5f, 115.5f); // 老端: _Group6
            view._Group7 = BuildGroup(gpRpr, "RedPacketSpawnPoint7", -116.5f, 115.5f); // 老端: _Group7
            view._Group8 = BuildGroup(gpRpr, "RedPacketSpawnPoint8", -244.5f, -104.5f); // 老端: _Group8
            view._Group9 = BuildGroup(gpRpr, "RedPacketSpawnPoint9", -67.5f, -168.5f); // 老端: _Group9
            view._Group10 = BuildGroup(gpRpr, "RedPacketSpawnPoint10", 64.5f, -66.5f); // 老端: _Group10
            view._Group11 = BuildGroup(gpRpr, "RedPacketSpawnPoint11", 204.5f, -255.5f); // 老端: _Group11
            view._Group12 = BuildGroup(gpRpr, "RedPacketSpawnPoint12", -13.5f, -514.5f); // 老端: _Group12
            view._Group13 = BuildGroup(gpRpr, "RedPacketSpawnPoint13", 165.5f, -450.5f); // 老端: _Group13
            view._Group14 = BuildGroup(gpRpr, "RedPacketSpawnPoint14", -192.5f, -390.5f); // 老端: _Group14

            return root;
        }

        private static RectTransform BuildGroup(Transform parent, string name, float x, float y)
        {
            RectTransform rt = UiCreatorKit.NewNode(name, parent);
            UiCreatorKit.Place(rt, x, y, 81f, 119f);
            return rt;
        }

        /// <summary>主UI特效层(对标老端 MainUIEffectView.json,本体是 1x1 挂点,_box_eff 2000x1280)。</summary>
        private static RectTransform BuildEffect(Transform parent)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIEffectView", parent);
            UiCreatorKit.Place(root, 0f, 0f, 20f, 20f); // 老端本体 1x1(纯挂点),给个可点选的起步尺寸,不影响 _box_eff 自身绝对尺寸
            var view = root.gameObject.AddComponent<MainUIEffectView>();

            RectTransform boxEff = UiCreatorKit.NewNode("MainEffectAnchorBox", root); // 老端: _box_eff
            UiCreatorKit.Place(boxEff, 0f, 0f, 2000f, 1280f);
            view._box_eff = boxEff;

            return root;
        }

        /// <summary>伙伴技能特效层(对标老端 MainUIEffectPartnerSkillView.json,本体 1x1 挂点偏左上,_box_eff 1000x400)。</summary>
        private static RectTransform BuildEffectPartnerSkill(Transform parent)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIEffectPartnerSkillView", parent);
            // 老端 left=10,top=300(1x1 挂点)换算:cx=-349.5,cy=339.5。
            UiCreatorKit.Place(root, -349.5f, 339.5f, 20f, 20f);
            var view = root.gameObject.AddComponent<MainUIEffectPartnerSkillView>();

            RectTransform boxEff = UiCreatorKit.NewNode("PartnerSkillEffectAnchorBox", root); // 老端: _box_eff
            UiCreatorKit.Place(boxEff, 0f, 0f, 1000f, 400f);
            view._box_eff = boxEff;

            return root;
        }

        public static void PreviewBundle()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudOverlayCombat",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "该 bundle 尚未注册 Addressable(要等总装嵌入 MainUIModule 才分组),这里直接从 prefab 资产实例化," +
                    "仅用于看结构,不代表最终真机加载路径。",
                    "好");
                return;
            }

            if (_bundlePreviewInstance != null)
            {
                Object.Destroy(_bundlePreviewInstance);
                _bundlePreviewInstance = null;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BundlePrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogWarning("[UiCreator] 未找到 " + BundlePrefabPath + ",请先点生成。");
                return;
            }

            Transform layer = ViewManager.GetLayer(UILayer.Main);
            _bundlePreviewInstance = Object.Instantiate(prefabAsset, layer);
            _bundlePreviewInstance.name = "HudOverlayCombat(Preview)";

            // 逐个 Show() 六个子视图,方便一次性看全结构(事件驱动弹层默认关闭,预览时手动强制打开)。
            BaseView[] views = _bundlePreviewInstance.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views)
            {
                v.Show();
            }
        }

        private static GameObject _bundlePreviewInstance;

        // ============================================================ 独立 prefab: CollectBarView

        public static void GenerateCollectBar()
        {
            GameObject go = new GameObject("CollectBarView", typeof(RectTransform));
            RectTransform root = (RectTransform)go.transform;
            root.gameObject.SetActive(false);
            // 老端 CollectBarView 无跟随定位(不像伤害飘字那样贴世界坐标),固定屏幕坐标;
            // 无运行态佐证,占位默认位置,自行在编辑器调。
            UiCreatorKit.Place(root, 0f, -300f, 250f, 104f);
            var view = root.gameObject.AddComponent<CollectBarView>();

            Image imgBg = UiCreatorKit.NewImage("CollectBarBackgroundImage", root); // 老端: _img_bg
            UiCreatorKit.Place(imgBg.rectTransform, 0f, 0f, 250f, 104f);
            imgBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgBg, IMG_COLLECT_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = imgBg;

            Image imgProgress = UiCreatorKit.NewImage("CollectProgressFillImage", root); // 老端: _img_progress
            UiCreatorKit.Place(imgProgress.rectTransform, 0f, -7f, 46f, 74f);
            imgProgress.raycastTarget = false;
            // Image.type/填充方向由业务 OnInit 强制设成 Filled/Vertical/Bottom,这里只管贴图。
            UiCreatorKit.TrySetSprite(imgProgress, IMG_COLLECT_PROGRESS, UiCreatorKit.Palette.BtnPrimary);
            view._img_progress = imgProgress;

            Image imgNumBg = UiCreatorKit.NewImage("RepairPercentBackgroundImage", root); // 老端: _img_num_bg
            UiCreatorKit.Place(imgNumBg.rectTransform, -0.5f, -70f, 215f, 28f);
            imgNumBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgNumBg, IMG_COLLECT_NUMBG, UiCreatorKit.Palette.Panel);
            Color numBgColor = imgNumBg.color;
            numBgColor.a = 0.4f; // 老端 alpha=0.4
            imgNumBg.color = numBgColor;
            imgNumBg.gameObject.SetActive(false); // 老端 visible=false(海域"修复中%"专用,任务采集不显示;业务 OnInit 也会再隐藏一次)
            view._img_num_bg = imgNumBg;

            TextMeshProUGUI txtProgress = UiCreatorKit.NewText("ProgressPercentLabel", root, ""); // 老端: _txt_progress
            UiCreatorKit.Place(txtProgress.rectTransform, 0f, -70f, 184f, 20f);
            txtProgress.fontSize = 20f;
            txtProgress.gameObject.SetActive(false); // 老端 visible=false
            view._txt_progress = txtProgress;

            Image imgState = UiCreatorKit.NewImage("CollectStatusImage", root); // 老端: _img_state
            UiCreatorKit.Place(imgState.rectTransform, 0.5f, -265f, 172f, 54f);
            imgState.raycastTarget = false;
            imgState.color = UiCreatorKit.Palette.Panel; // 老端无 skin,业务代码当前也未使用此字段
            view._img_state = imgState;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, CollectBarPrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] CollectBarView.prefab 已生成: " + CollectBarPrefabPath +
                      "(独立 prefab,MainUIFlow.EnsureCollectBarViewAsync 按精确文件名加载;真机包前记得跑 Addressable 自动分组)");
        }

        public static void PreviewCollectBar()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 CollectBarView",
                    "请先进入 Play 模式(游戏已起、mainUI addressable 可用)再点预览。", "好");
                return;
            }
            _ = PreviewCollectBarAsync();
        }

        private static async Task PreviewCollectBarAsync()
        {
            string key = GameResPath.GetUIPrefab("mainUI", "CollectBarView");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Main));
            if (go == null)
            {
                Debug.LogWarning("[UiCreator] CollectBarView 预览加载失败: " + key + "(检查 addressable/是否已生成)");
                return;
            }
            CollectBarView view = go.GetComponent<CollectBarView>();
            if (view == null)
            {
                Debug.LogWarning("[UiCreator] CollectBarView 预览缺组件(重跑生成)");
                return;
            }
            view.BeginCollect(3f);
        }

        // ============================================================ 独立 prefab: FightingUpView

        public static void GenerateFightingUp()
        {
            GameObject go = new GameObject("FightingUpView", typeof(RectTransform));
            RectTransform root = (RectTransform)go.transform;
            root.gameObject.SetActive(false);
            // 运行态佐证:老端 x=168,y=765(720x1280 全屏画布下),385x115 → 中心锚 cx=0.5,cy=-182.5。
            UiCreatorKit.Place(root, 0.5f, -182.5f, 385f, 115f);
            var view = root.gameObject.AddComponent<FightingUpView>();

            Image imgBg = UiCreatorKit.NewImage("FightingUpBackgroundImage", root); // 老端: _img_bg
            // 老端 JSON 里 _img_bg 无 x/y/width/height,实际按原图 398x95 原生尺寸贴在 x=0,y=0(左上锚)。
            // 按文件头换算公式: cx = 0 + 398*(0.5-0) - 385/2 = 6.5, cy = 115/2 - 0 - 95*(0.5-0) = 10。
            UiCreatorKit.Place(imgBg.rectTransform, 6.5f, 10f, 398f, 95f);
            imgBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgBg, IMG_FIGHTING_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = imgBg;

            TextMeshProUGUI lbFight = UiCreatorKit.NewText("FightPowerValueLabel", root, "0"); // 老端: _lb_fight
            UiCreatorKit.Place(lbFight.rectTransform, -73.5f, 24.5f, 160f, 60f);
            lbFight.fontSize = 36f; // 老端走位图字体 fight_up(未转换),这里用 TMP 渐变+描边近似其金色数字观感
            StyleFightNumberLabel(lbFight,
                top: new Color32(0xFF, 0xF3, 0xC8, 0xFF), bottom: new Color32(0xFF, 0x8A, 0x1E, 0xFF),
                outline: new Color32(0x6B, 0x2E, 0x0D, 0xFF), outlineStrokePx: 2f);
            view._lb_fight = lbFight;

            TextMeshProUGUI lbAddFight = UiCreatorKit.NewText("FightPowerDeltaLabel", root, "+0"); // 老端: _lb_add_fight
            UiCreatorKit.Place(lbAddFight.rectTransform, 38.5f, 39.5f, 120f, 40f);
            lbAddFight.fontSize = 26f; // 老端走位图字体 fight_up2(未转换),这里用 TMP 渐变+描边近似其绿色数字观感
            StyleFightNumberLabel(lbAddFight,
                top: new Color32(0xD9, 0xFF, 0x9E, 0xFF), bottom: new Color32(0x4C, 0x9A, 0x1E, 0xFF),
                outline: new Color32(0x1F, 0x4D, 0x0A, 0xFF), outlineStrokePx: 2f);
            view._lb_add_fight = lbAddFight;

            // 老端 _box_effect 在 open_callback 里被临时改成"铺满整个 stage"再贴 ui_zhanli 特效(centerX=-125,
            // centerY=0),这样特效能溢出 385x115 的小飘字框、铺出一片光效。Unity 侧不依赖运行时读 Canvas 尺寸,
            // 直接在建树期把挂点做成一个比飘字框大得多、以设计分辨率宽度为准的锚框(720x480,居中),效果等价
            // 且不用在 View 里现改 RectTransform(那属于"样式/结构",建树期定好更符合本文件约定)。
            RectTransform boxEffect = UiCreatorKit.NewNode("FightPowerEffectAnchor", root); // 老端: _box_effect
            UiCreatorKit.Place(boxEffect, 0f, 0f, UiCreatorKit.DesignWidth, 480f);
            view._box_effect = boxEffect;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, FightingUpPrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] FightingUpView.prefab 已生成: " + FightingUpPrefabPath +
                      "(独立 prefab,MainUIFlow.ShowFightingUpAsync 按精确文件名加载;真机包前记得跑 Addressable 自动分组)");
        }

        public static void PreviewFightingUp()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 FightingUpView",
                    "请先进入 Play 模式(游戏已起、mainUI addressable 可用)再点预览。", "好");
                return;
            }
            _ = PreviewFightingUpAsync();
        }

        private static async Task PreviewFightingUpAsync()
        {
            string key = GameResPath.GetUIPrefab("mainUI", "FightingUpView");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            if (go == null)
            {
                Debug.LogWarning("[UiCreator] FightingUpView 预览加载失败: " + key + "(检查 addressable/是否已生成)");
                return;
            }
            FightingUpView view = go.GetComponent<FightingUpView>();
            if (view == null)
            {
                Debug.LogWarning("[UiCreator] FightingUpView 预览缺组件(重跑生成)");
                return;
            }
            view.Show(new FightUpData { OldFight = 1000, NewFight = 1580 });
        }

        // ============================================================ 本文件私有 helper(UiCreatorKit 禁止改,缺的写这里)

        /// <summary>
        /// 老端 anchorX=1(右对齐)且业务代码会动态改 sizeDelta.x 的血条段:pivot 设到右边 (1,.5),
        /// anchoredPosition.x 记右边缘坐标(而非中心),这样 sizeDelta.x 变小时右边固定、往左收缩,对齐老端表现。
        /// </summary>
        private static void SetRightPivotBar(RectTransform rt, float rightEdgeX, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(rightEdgeX, y);
        }

        private static void SetColorHex(TextMeshProUGUI text, string hex)
        {
            if (text != null && ColorUtility.TryParseHtmlString(hex, out Color c)) text.color = c;
        }

        /// <summary>喂给 LayaTextStyles.ApplyOutline 复用其"按(字体,颜色,粗细)缓存描边材质"逻辑用;
        /// 只取用它的材质生成/去重能力,不调用 Save() 落报告文件(这里不是批量转换流程)。</summary>
        private static readonly LayaUIReport FightUpStyleReport = new LayaUIReport("MainUI_FightingUp");

        /// <summary>
        /// 战力飘字位图字体(fight_up/fight_up2)近似:项目里这两款老端位图字体资产还没转换成 TMP FontAsset,
        /// 这里用 TMP 渐变色(顶浅底深)+ SDF 描边模拟老端"金色/绿色数字带深色描边"的观感,粗体+斜体贴近老端
        /// 字形的倾斜手感(实测 fight_up.png 是暖金渐变+棕色描边,fight_up2.png 是绿色渐变+深绿描边)。
        /// 描边材质直接复用 LayaTextStyles.ApplyOutline(和批量转换器同一套按字体/颜色/粗细去重缓存到
        /// Assets/GameRes/Fonts/Materials/ 的做法,不在这里另起一套内嵌材质的写法)。
        /// </summary>
        private static void StyleFightNumberLabel(TextMeshProUGUI label, Color32 top, Color32 bottom, Color outline, float outlineStrokePx)
        {
            if (label == null) return;
            label.alignment = TextAlignmentOptions.Left;
            label.fontStyle = FontStyles.Bold | FontStyles.Italic;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableVertexGradient = true;
            label.colorGradient = new VertexGradient(top, top, bottom, bottom);
            label.color = Color.white; // 用 vertex gradient 上色时基色保持白,避免和渐变相乘变暗

            LayaTextStyles.ApplyOutline(label, outline, outlineStrokePx, FightUpStyleReport, label.name);
        }

        /// <summary>建一个默认关闭的隐藏容器,专门收纳 _tpl_XXX 克隆模板节点,不与可见/运行时挂点混放。</summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>把现成 prefab 作为嵌套实例挂到 parent 下并改名(对标仓库内 _tpl_XXX 模板节点用法)。</summary>
        private static GameObject NestPrefab(string assetPath, Transform parent, string renameTo)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabAsset == null)
            {
                Debug.LogWarning("[UiCreator] 未找到嵌套预制体: " + assetPath);
                return null;
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
            if (instance == null) return null;
            instance.transform.SetParent(parent, false);
            if (!string.IsNullOrEmpty(renameTo)) instance.name = renameTo;
            return instance;
        }
    }
}
