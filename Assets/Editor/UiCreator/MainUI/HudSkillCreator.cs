using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面「技能·战斗区」纯代码建树生成器。
    ///
    /// 一个 prefab 里三个常驻视图子根:技能条(MainUISkillView)、斩妖挂机(MainUIAutoBrushView)、
    /// 摇杆(UIJoyStick)。三者原本各自是独立的转换器碎片化 prefab(Assets/Prefabs/UI/MainUI/*.prefab,
    /// 未改动),本生成器改用纯代码建树,把它们合成一个可手调的区域 prefab,
    /// 存 Assets/Prefabs/UI/MainUI/Regions/HudSkill.prefab。
    ///
    /// 数据源:
    ///   A. 运行时快照(满级号)Tools/ModuleManifest/snapshots/mainui_123123/page_snapshot_*.json,
    ///      nodeTree 上每个节点自带 x/y/width/height/anchorX/anchorY/pivotX/pivotY/globalBounds —
    ///      本生成器的所有布局数值都是拿 globalBounds(= 已按锚点/主元素换算过的层级坐标)算出来的
    ///      "子节点中心 - 父节点中心"中心锚偏移,而不是直接拿 x/y 当左上角(那样在有 anchorX/anchorY
    ///      的节点上会算错,典型例子见下面 UIJoyStick 一节)。
    ///   B. 老端场景 JSON e:/GitProject/yu_client/h5/bin/resource/game/mainUI/*.json,仅在快照没有
    ///      该节点时(MainUISkillItemGod 未在满级号快照里出现)当唯一依据。
    ///
    /// 关键技术决定(#号标注,详见方法内注释):
    ///   #1 _tpl_MainUISkillItem 必须用【左上锚点+左上枢轴】(0,1),不能用本 Kit 默认的中心锚点——
    ///      因为 MainUISkillView.GetOrCreateItem 克隆它后调用 item.SetPosition(x,y) 实际执行的是
    ///      "rt.anchoredPosition = new Vector2(x, -y)",这个公式只有在左上锚点下才会把克隆体摆到
    ///      老端 FixedPositions 想要的位置(已用快照 4 个真实槽位反推验证,分毫不差)。
    ///   #2 _img_progress 同理需要左中锚点(0,0.5),因为 MainUIAutoBrushView.SetProgressWidth 直接改
    ///      sizeDelta.x,中心锚点会导致进度条从两侧往中间缩,左锚点才会保持左边不动、只缩右边。
    ///   #3 _tpl_MainUISkillItem 模板本体只建 1 个,不预铺 4 个"实际槽位"的常驻实例——因为
    ///      MainUISkillView.RefreshSkills 每次都会 Instantiate(_tpl_MainUISkillItem,...)
    ///      产出新克隆挂到 _box_skill_con 下,并不会清空/复用手摆的旧节点;如果预铺 4 个常驻实例,
    ///      运行时会变成 4(手摆) + 4(克隆) = 8 个图标重叠。4 个真实槽位坐标已经硬编码在
    ///      MainUISkillView.FixedPositions 里,建树期不需要重复摆。模板本体挂在 MainUISkillView
    ///      根下的 __Templates 隐藏容器里(不再混进 _box_skill_con——后者是克隆体真实落点,分开放
    ///      才不会被误当成"第 5 个手摆槽位"),CirCleCdView 模板同理。
    ///   #4 MainUISkillItemGod 当前没有任何 Bind 字段引用它(MainUISkillViewBind 没有
    ///      _tpl_MainUISkillItemGod 这种槽位,伙伴/神祇系统都未接线),按任务要求仍建出来当备用模板,
    ///      挂在 MainUISkillView 根下的 __Templates 容器里、默认禁用、不被任何字段引用,等后续系统
    ///      接线时手挂。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文;下列 con/bg/icon/_img_mask/_lb_cd 等短名在文件内出现多次,
    // 均按各自所在模板/容器区分,已在文件里逐条附 "老端: xxx" 注释,可用注释反查):
    //   _box_partner_skill        -> PartnerSkillSlot
    //   _img_partner_lock         -> PartnerLockIcon
    //   auto_box                  -> AutoFightBox
    //   auto_bg                   -> AutoFightBg
    //   _img_auto_fight           -> AutoFightButton
    //   skill_box                 -> SkillBox
    //   _img_bg(技能条底图)        -> SkillBarBg
    //   _box_skill_con            -> SkillIconGrid
    //   con(MainUISkillItem)      -> IconGroup
    //   bg(MainUISkillItem)       -> SkillIconBg
    //   icon(MainUISkillItem)     -> SkillIconImage
    //   lock(MainUISkillItem)     -> SkillLockOverlay
    //   _img_mask(CirCleCdView)   -> CdMaskImage
    //   _lb_cd(CirCleCdView)      -> CdCountdownLabel
    //   con(MainUISkillItemGod)   -> GodIconGroup
    //   bg(MainUISkillItemGod)    -> GodIconBg
    //   icon(MainUISkillItemGod)  -> GodIconImage
    //   _Image1(God)              -> HighlightOverlay
    //   _img_keep(God)            -> DurationOverlay
    //   _img_mask(God)            -> GodCooldownMask
    //   _lb_cd(God)               -> GodCooldownLabel
    //   _gp_eff(God)              -> TransformEffectSlot
    //   _img_bg(斩妖挂机主图)      -> AutoBrushIconBg
    //   _img_bg2                  -> TopBadgeBg
    //   _img_bg3                  -> LevelBadgeBg
    //   _lb_level                 -> LevelLabel
    //   _img_bg4                  -> ProgressBarBg
    //   _img_progress             -> ProgressBarFill
    //   _box_effect               -> CompletionEffectSlot
    //   _box_auto_level           -> AutoToggleButton
    //   _img_auto_level           -> AutoToggleBg
    //   _lb_auto_level            -> AutoToggleLabel
    //   _img_red(斩妖挂机主图红点) -> IconRedDot
    //   click_gp                  -> OpenAutoBrushButton
    //   _img_red2                 -> ToggleRedDot
    //   _box_challenge_effect     -> ChallengeEffectSlot
    //   _gp_root                  -> JoystickDial
    //   _img_bg(摇杆底图)          -> DialBg
    //   _img_arrow                -> DirectionArrow
    //   _img_middle_circle        -> KnobImage
    // 模板自身节点名(MainUISkillItem / CirCleCdView / MainUISkillItemGod)按规则保留原样,
    // 运行时按这个精确字符串做克隆/调试识别,不翻译。
    public static class HudSkillCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudSkill.prefab";

        // ---------- 技能条(MainUISkillView) ----------
        private const string IMG_PARTNER_LOCK = "resource/game/mainUI/texture/uiqlhx_017.png";
        private const string IMG_AUTO_BG = "resource/game/mainUI/texture/auto_bg.png";
        private const string IMG_AUTO_FIGHT_OFF = "resource/game/mainUI/texture/uizjmgj_003a.png";
        private const string IMG_SKILL_BAR_BG = "resource/game/mainUI/other/uizjmgj_002.png";
        private const string IMG_SKILL_ITEM_BG = "resource/game/mainUI/texture/uiqlhx_018.png";
        private const string IMG_SKILL_ITEM_LOCK = "resource/game/mainUI/texture/uirw_045cc.png";
        private const string IMG_SKILL_ICON_SAMPLE = "resource/game/skillIcon/59100011.png"; // 代表图;真实技能图标(402012 等)运行时由 SetData 换
        private const string IMG_CD_MASK = "resource/game/common/texture/ui_11.png";

        // ---------- 神祇变身模板(未接线,见类注释 #4) ----------
        private const string IMG_GOD_BG = "resource/game/mainUI/texture/uizjmgj_003.png";
        private const string IMG_GOD_ICON_PLACEHOLDER = "resource/game/common/texture/com_empty.png"; // 无 godBefallIcon 资源,沿用设计态占位图
        private const string IMG_GOD_IMAGE1 = "resource/game/mainUI/texture/uijs_044.png";
        private const string IMG_GOD_KEEP = "resource/game/mainUI/texture/uijs_045.png";
        private const string IMG_GOD_MASK = "resource/game/common/texture/ui_circle_mask.png";

        // ---------- 斩妖挂机(MainUIAutoBrushView) ----------
        private const string IMG_AB_BG = "resource/game/mainUI/texture/ui_zy_14.png";
        private const string IMG_AB_BG2 = "resource/game/mainUI/texture/ui_zy_15.png";
        private const string IMG_AB_BG3 = "resource/game/mainUI/texture/ui_zy_16.png";
        private const string IMG_AB_BG4 = "resource/game/mainUI/texture/ui_zy_17.png";
        private const string IMG_AB_PROGRESS = "resource/game/mainUI/texture/ui_zy_18.png"; // 老端 sizeGrid="2,2,3,2"(九宫格),本轮未动导入设置,见报告
        private const string IMG_AB_AUTO_LEVEL = "resource/game/mainUI/texture/ui_zy_19.png";
        private const string IMG_AB_RED = "resource/game/mainUI/texture/com_red_point.png";

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
                Name = "HudSkill(技能·战斗区)",
                Note = "技能条 + 斩妖挂机 + 摇杆,三视图子根合一区域 prefab",
                Order = 40,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建(对标 Login/RoleCreate 样板的安全建树习惯);建完再激活。
            RectTransform hudRoot = UiCreatorKit.NewRoot("HudSkill");
            hudRoot.gameObject.SetActive(false);

            BuildSkillView(hudRoot);
            BuildAutoBrushView(hudRoot);
            BuildJoyStick(hudRoot);

            hudRoot.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(hudRoot.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudSkill.prefab 已生成: " + PrefabPath +
                      "(三个子根各自是独立 BaseView,尚未接入 MainUIModule/MainUIFlow,需要后续手工组装或替换)");
        }

        // ===================== 技能条 MainUISkillView =====================

        /// <summary>
        /// 子根锚定:centerX=0 / bottom=254(720×1280 设计画布,720 宽 1280 高,中心锚点)。
        /// centerY = -DesignHeight/2 + bottom + h/2 = -640 + 254 + 107/2 = -332.5。
        /// </summary>
        private static void BuildSkillView(Transform hudRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUISkillView", hudRoot);
            UiCreatorKit.Place(root, 0f, -332.5f, 742f, 107f);
            var view = root.gameObject.AddComponent<MainUISkillView>();

            // 伙伴技能位:伙伴系统未移植,当前只显示一把锁(对标 MainUISkillView.InitPartnerSkill 的边界注释)。
            RectTransform partnerBox = UiCreatorKit.NewNode("PartnerSkillSlot", root); // 老端: _box_partner_skill
            UiCreatorKit.Place(partnerBox, 154.5f, -24f, 57f, 59f);
            view._box_partner_skill = partnerBox;

            Image partnerLock = UiCreatorKit.NewImage("PartnerLockIcon", partnerBox); // 老端: _img_partner_lock
            UiCreatorKit.Place(partnerLock.rectTransform, 0f, 0f, 57f, 59f);
            UiCreatorKit.TrySetSprite(partnerLock, IMG_PARTNER_LOCK, UiCreatorKit.Palette.BtnNeutral);
            view._img_partner_lock = partnerLock;

            // 自动战斗按钮盒(AutoFightBox 是纯结构容器,MainUISkillViewBind 没有对应字段,不用回填)。
            RectTransform autoBox = UiCreatorKit.NewNode("AutoFightBox", root); // 老端: auto_box
            UiCreatorKit.Place(autoBox, 302.5f, 0f, 137f, 107f);

            Image autoBg = UiCreatorKit.NewImage("AutoFightBg", autoBox); // 老端: auto_bg
            UiCreatorKit.Place(autoBg.rectTransform, 0f, 0f, 111f, 107f);
            UiCreatorKit.TrySetSprite(autoBg, IMG_AUTO_BG, UiCreatorKit.Palette.Panel);

            Image autoFight = UiCreatorKit.NewImage("AutoFightButton", autoBox); // 老端: _img_auto_fight
            UiCreatorKit.Place(autoFight.rectTransform, 2.5f, 5f, 132f, 65f);
            UiCreatorKit.TrySetSprite(autoFight, IMG_AUTO_FIGHT_OFF, UiCreatorKit.Palette.BtnPrimary);
            view._img_auto_fight = autoFight;

            // 技能条本体
            RectTransform skillBox = UiCreatorKit.NewNode("SkillBox", root); // 老端: skill_box
            UiCreatorKit.Place(skillBox, 0.5f, -11f, 179f, 85f);
            view.skill_box = skillBox;

            Image skillBg = UiCreatorKit.NewImage("SkillBarBg", skillBox); // 老端: _img_bg
            UiCreatorKit.Place(skillBg.rectTransform, 0f, 0f, 179f, 85f);
            UiCreatorKit.TrySetSprite(skillBg, IMG_SKILL_BAR_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = skillBg;

            RectTransform skillCon = UiCreatorKit.NewNode("SkillIconGrid", skillBox); // 老端: _box_skill_con
            UiCreatorKit.Place(skillCon, 0f, 29.5f, 179f, 144f);
            view._box_skill_con = skillCon;

            // 模板统一挂在 __Templates(视图自己根下的隐藏容器)里,不再混进 SkillIconGrid——
            // 后者是 MainUISkillView.RefreshSkills 运行时 Instantiate 克隆体的真实落点(见类注释 #3),
            // 模板本体只是克隆源,分开放才不会被误当成"第 5 个手摆槽位"。
            RectTransform templates = NewTemplatesWrapper(root);
            BuildSkillItemTemplate(view, templates);
            BuildCirCleCdTemplate(view, templates);

            // 神祇变身按钮:未接线模板(见类注释 #4),借用伙伴技能位坐标当占位,默认禁用不冲突。
            BuildSkillItemGodTemplate(templates, 154.5f, -24f);
        }

        /// <summary>
        /// #1 _tpl_MainUISkillItem 必须是左上锚点+左上枢轴(0,1),不能用 UiCreatorKit.Place 的中心锚点。
        /// 依据:MainUISkillItem.SetPosition 执行 rt.anchoredPosition = new Vector2(x, -y),
        /// 而 MainUISkillView.FixedPositions[0] = (4,99)(老端 _box_skill_con 内左上角坐标,与快照
        /// 4 个真实槽位完全吻合)。只有克隆体的锚点/枢轴都在左上时,anchoredPosition=(4,-99) 才会让
        /// 克隆体的左上角落在 _box_skill_con 左上角往右 4、往下 99 的位置——用快照反推验证:
        /// _box_skill_con 179×144,左上角=(-89.5,+72)(中心锚坐标系);克隆体 45×47 左上角落到
        /// (-89.5+4, 72-99)=(-85.5,-27),其中心=(-63,-50.5),与快照量出的槽位 1 中心坐标一致。
        /// 模板内部子节点(con/bg/icon/lock)用中心锚点摆位不受影响(子节点定位只看自身尺寸和
        /// "父节点矩形"宽高,与父节点自己的锚点/枢轴无关)。
        /// </summary>
        private static void BuildSkillItemTemplate(MainUISkillView view, Transform templatesRoot)
        {
            RectTransform itemRt = UiCreatorKit.NewNode("MainUISkillItem", templatesRoot);
            PlaceTopLeft(itemRt, 4f, -99f, 45f, 47f); // FixedPositions[0]=(4,99) → anchoredPosition=(4,-99)
            var item = itemRt.gameObject.AddComponent<MainUISkillItem>();

            RectTransform con = UiCreatorKit.NewNode("IconGroup", itemRt); // 老端: con
            UiCreatorKit.Place(con, 0f, 0f, 45f, 47f);
            item.con = con;

            Image bg = UiCreatorKit.NewImage("SkillIconBg", con); // 老端: bg
            UiCreatorKit.Place(bg.rectTransform, 0f, 0f, 63f, 63f);
            UiCreatorKit.TrySetSprite(bg, IMG_SKILL_ITEM_BG, UiCreatorKit.Palette.Panel);
            item.bg = bg;

            Image icon = UiCreatorKit.NewImage("SkillIconImage", con); // 老端: icon
            UiCreatorKit.Place(icon.rectTransform, 0.5f, -0.5f, 38f, 38f);
            UiCreatorKit.TrySetSprite(icon, IMG_SKILL_ICON_SAMPLE, Color.white);
            item.icon = icon;

            Image lockImg = UiCreatorKit.NewImage("SkillLockOverlay", itemRt); // 老端: lock
            UiCreatorKit.Place(lockImg.rectTransform, 0.5f, -0.5f, 38f, 38f);
            UiCreatorKit.TrySetSprite(lockImg, IMG_SKILL_ITEM_LOCK, UiCreatorKit.Palette.BtnNeutral);
            lockImg.gameObject.SetActive(false); // 默认已学会(对标快照 vis=false / UpdateLock(false))
            item.@lock = lockImg;

            itemRt.gameObject.SetActive(false); // 模板本身禁用,由 MainUISkillView.OnInit 再次强制禁用
            view._tpl_MainUISkillItem = itemRt.gameObject;
        }

        /// <summary>
        /// CD 圆遮罩模板:MainUISkillViewBind._tpl_CirCleCdView 只声明了引用,当前业务代码
        /// (MainUISkillView/MainUISkillItem)都没有克隆/挂接它的逻辑(CD 系统未移植,见 MainUISkillItem
        /// 类注释"降级...CirCleCdView...不显示")。这里按快照结构(_img_mask + _lb_cd)建出最小可用模板,
        /// 满足 EnsureBound 非空即可,不嵌到每个技能槽里(避免和 #3 一样出现无人清理的孤立实例)。
        /// </summary>
        private static void BuildCirCleCdTemplate(MainUISkillView view, Transform templatesRoot)
        {
            RectTransform tplRt = UiCreatorKit.NewNode("CirCleCdView", templatesRoot);
            UiCreatorKit.Place(tplRt, 0f, 0f, 38f, 38f);

            Image mask = UiCreatorKit.NewImage("CdMaskImage", tplRt); // 老端: _img_mask
            UiCreatorKit.Place(mask.rectTransform, 0f, 0f, 38f, 38f);
            UiCreatorKit.TrySetSprite(mask, IMG_CD_MASK, UiCreatorKit.Palette.Mark);
            mask.gameObject.SetActive(false); // 对标快照 vis=false(无 CD 时不显示遮罩)

            TextMeshProUGUI cd = UiCreatorKit.NewText("CdCountdownLabel", tplRt, "0"); // 老端: _lb_cd
            UiCreatorKit.Place(cd.rectTransform, 0f, 0f, 100f, 40f);
            cd.fontSize = 24f;
            cd.color = Color.red;
            cd.fontStyle = FontStyles.Bold;

            tplRt.gameObject.SetActive(false);
            view._tpl_CirCleCdView = tplRt.gameObject;
        }

        /// <summary>
        /// 神祇(变身)技能按钮模板(见类注释 #4):没有任何现有 Bind 字段引用它,纯粹按任务要求建出来
        /// 备用。结构/尺寸取自老端设计源 MainUISkillItemGod.json(满级号快照没有这个节点,当前号没有
        /// 出战神祇/未觉醒,取不到运行时真实值)。该源文件里各子节点都带 centerX/centerY:0(相对父节点
        /// 居中的布局关系,取值 0 即"完全居中"),因此忽略源文件里配套的 x/y 陈旧值,直接按尺寸居中摆放。
        /// </summary>
        private static void BuildSkillItemGodTemplate(Transform parent, float x, float y)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUISkillItemGod", parent);
            UiCreatorKit.Place(root, x, y, 90f, 90f);
            var god = root.gameObject.AddComponent<MainUISkillItemGod>();

            RectTransform con = UiCreatorKit.NewNode("GodIconGroup", root); // 老端: con
            UiCreatorKit.Place(con, 0f, 0f, 90f, 90f);
            god.con = con;

            Image bg = UiCreatorKit.NewImage("GodIconBg", con); // 老端: bg
            UiCreatorKit.Place(bg.rectTransform, 0f, 0f, 90f, 90f);
            UiCreatorKit.TrySetSprite(bg, IMG_GOD_BG, UiCreatorKit.Palette.Panel);
            god.bg = bg;

            Image icon = UiCreatorKit.NewImage("GodIconImage", con); // 老端: icon
            UiCreatorKit.Place(icon.rectTransform, 0f, 0f, 70f, 70f);
            UiCreatorKit.TrySetSprite(icon, IMG_GOD_ICON_PLACEHOLDER, Color.white);
            god.icon = icon;

            Image image1 = UiCreatorKit.NewImage("HighlightOverlay", con); // 老端: _Image1
            UiCreatorKit.Place(image1.rectTransform, 0f, 0f, 90f, 90f);
            UiCreatorKit.TrySetSprite(image1, IMG_GOD_IMAGE1, UiCreatorKit.Palette.BtnSecond);
            image1.gameObject.SetActive(false); // 对标设计源 visible:false
            god._Image1 = image1;

            Image keep = UiCreatorKit.NewImage("DurationOverlay", con); // 老端: _img_keep
            UiCreatorKit.Place(keep.rectTransform, 0f, 0f, 90f, 90f);
            UiCreatorKit.TrySetSprite(keep, IMG_GOD_KEEP, UiCreatorKit.Palette.BtnPrimary);
            god._img_keep = keep;

            Image mask = UiCreatorKit.NewImage("GodCooldownMask", con); // 老端: _img_mask
            UiCreatorKit.Place(mask.rectTransform, 0f, 0f, 76f, 76f);
            UiCreatorKit.TrySetSprite(mask, IMG_GOD_MASK, new Color(1f, 1f, 1f, 0.5f));
            Color maskColor = mask.color; maskColor.a = 0.5f; mask.color = maskColor; // 对标设计源 alpha:0.5
            mask.gameObject.SetActive(false); // 对标设计源 visible:false
            god._img_mask = mask;

            TextMeshProUGUI cd = UiCreatorKit.NewText("GodCooldownLabel", con, string.Empty); // 老端: _lb_cd
            UiCreatorKit.Place(cd.rectTransform, 0f, 0f, 100f, 100f);
            cd.fontSize = 24f;
            cd.color = Color.red;
            cd.fontStyle = FontStyles.Bold;
            god._lb_cd = cd;

            RectTransform eff = UiCreatorKit.NewNode("TransformEffectSlot", con); // 老端: _gp_eff
            UiCreatorKit.Place(eff, 0f, 0f, 150f, 150f);
            god._gp_eff = eff;

            root.gameObject.SetActive(false); // 未接线模板,默认禁用(见类注释 #4)
        }

        // ===================== 斩妖挂机 MainUIAutoBrushView =====================

        /// <summary>
        /// 子根锚定:x=26 / bottom=262(720×1280 设计画布左下角起算),整体再叠加 scale 0.9。
        /// centerX = -DesignWidth/2 + x + w/2 = -360 + 26 + 45 = -289。
        /// centerY = -DesignHeight/2 + bottom + h/2 = -640 + 262 + 68 = -310。
        /// </summary>
        private static void BuildAutoBrushView(Transform hudRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIAutoBrushView", hudRoot);
            UiCreatorKit.Place(root, -289f, -310f, 90f, 136f);
            root.localScale = new Vector3(0.9f, 0.9f, 1f);
            var view = root.gameObject.AddComponent<MainUIAutoBrushView>();

            Image bg = UiCreatorKit.NewImage("AutoBrushIconBg", root); // 老端: _img_bg
            UiCreatorKit.Place(bg.rectTransform, 8f, 9f, 90f, 90f);
            UiCreatorKit.TrySetSprite(bg, IMG_AB_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = bg;

            Image bg2 = UiCreatorKit.NewImage("TopBadgeBg", root); // 老端: _img_bg2
            UiCreatorKit.Place(bg2.rectTransform, -2f, 44f, 58f, 32f);
            UiCreatorKit.TrySetSprite(bg2, IMG_AB_BG2, UiCreatorKit.Palette.Panel);
            view._img_bg2 = bg2;

            Image bg3 = UiCreatorKit.NewImage("LevelBadgeBg", root); // 老端: _img_bg3
            UiCreatorKit.Place(bg3.rectTransform, 1f, -3f, 68f, 18f);
            UiCreatorKit.TrySetSprite(bg3, IMG_AB_BG3, UiCreatorKit.Palette.Panel);
            view._img_bg3 = bg3;

            TextMeshProUGUI lbLevel = UiCreatorKit.NewText("LevelLabel", bg3.transform, "第1关"); // 老端: _lb_level
            UiCreatorKit.Place(lbLevel.rectTransform, -2f, -1f, 48f, 16f);
            lbLevel.fontSize = 16f;
            lbLevel.color = new Color(0f, 250f / 255f, 100f / 255f); // 对标老端 #00fa64
            view._lb_level = lbLevel;

            Image bg4 = UiCreatorKit.NewImage("ProgressBarBg", root); // 老端: _img_bg4
            UiCreatorKit.Place(bg4.rectTransform, 1f, -17f, 68f, 8f);
            UiCreatorKit.TrySetSprite(bg4, IMG_AB_BG4, UiCreatorKit.Palette.Panel);
            view._img_bg4 = bg4;

            // #2 左中锚点(0,0.5):SetProgressWidth 直接改 sizeDelta.x,需要左边不动、只缩右边。
            Image progress = UiCreatorKit.NewImage("ProgressBarFill", bg4.transform); // 老端: _img_progress
            PlaceLeftCenter(progress.rectTransform, 0f, 0f, 68f, 8f);
            UiCreatorKit.TrySetSprite(progress, IMG_AB_PROGRESS, UiCreatorKit.Palette.BtnPrimary);
            view._img_progress = progress;

            RectTransform boxEffect = UiCreatorKit.NewNode("CompletionEffectSlot", root); // 老端: _box_effect
            UiCreatorKit.Place(boxEffect, 1f, 18f, 100f, 100f);
            view._box_effect = boxEffect;

            RectTransform boxAutoLevel = UiCreatorKit.NewNode("AutoToggleButton", root); // 老端: _box_auto_level
            UiCreatorKit.Place(boxAutoLevel, 1f, -40f, 102f, 36f);
            view._box_auto_level = boxAutoLevel;

            Image autoLevelImg = UiCreatorKit.NewImage("AutoToggleBg", boxAutoLevel); // 老端: _img_auto_level
            UiCreatorKit.Place(autoLevelImg.rectTransform, 0f, 0f, 102f, 36f);
            UiCreatorKit.TrySetSprite(autoLevelImg, IMG_AB_AUTO_LEVEL, UiCreatorKit.Palette.BtnSecond);
            view._img_auto_level = autoLevelImg;

            TextMeshProUGUI lbAutoLevel = UiCreatorKit.NewText("AutoToggleLabel", boxAutoLevel, "自动闯关"); // 老端: _lb_auto_level
            UiCreatorKit.Place(lbAutoLevel.rectTransform, -2f, 1f, 80f, 20f);
            lbAutoLevel.fontSize = 20f;
            lbAutoLevel.color = Color.white;
            view._lb_auto_level = lbAutoLevel;

            Image red = UiCreatorKit.NewImage("IconRedDot", root); // 老端: _img_red
            UiCreatorKit.Place(red.rectTransform, 26f, 54.5f, 26f, 27f);
            UiCreatorKit.TrySetSprite(red, IMG_AB_RED, UiCreatorKit.Palette.Mark);
            view._img_red = red;

            // OpenAutoBrushButton 是纯命中容器(无 skin),UIUtil.AddClick 对纯 RectTransform 容器会在运行时
            // 自动补透明命中 Image(见 Framework/UI/UIUtil.ResolveContainerRaycastTarget),建树期不用补图。
            RectTransform clickGp = UiCreatorKit.NewNode("OpenAutoBrushButton", root); // 老端: click_gp
            UiCreatorKit.Place(clickGp, 8f, 9f, 90f, 90f);
            view.click_gp = clickGp;

            Image red2 = UiCreatorKit.NewImage("ToggleRedDot", root); // 老端: _img_red2
            UiCreatorKit.Place(red2.rectTransform, 33f, -27.5f, 26f, 27f);
            UiCreatorKit.TrySetSprite(red2, IMG_AB_RED, UiCreatorKit.Palette.Mark);
            view._img_red2 = red2;

            // 快照里多出的挑战特效盒,MainUIAutoBrushViewBind 没有对应字段(_box_effect 才是业务用的那个),
            // 只为节点覆盖度保留结构,不接引用、不影响 Bind。
            RectTransform challengeEffect = UiCreatorKit.NewNode("ChallengeEffectSlot", root); // 老端: _box_challenge_effect
            UiCreatorKit.Place(challengeEffect, 2f, 39f, 128f, 128f);
            Image challengeImg = UiCreatorKit.NewImage("ChallengeEffectImg", challengeEffect);
            UiCreatorKit.Place(challengeImg.rectTransform, 0f, 0f, 128f, 128f);
            challengeImg.color = new Color(1f, 0.85f, 0.3f, 0.18f); // 源节点无 skin,占位半透明色块
            challengeImg.raycastTarget = false;
        }

        // ===================== 摇杆 UIJoyStick =====================

        /// <summary>
        /// 子根锚定:左下角,默认禁用(任务未给精确数值,按 200×200 尺寸留边距估一个,可手调)。
        /// _gp_root 的快照原始位置换算下来是 (-100,+100)(锚点在自身中心,x/y=0 时紧贴父节点左上角,
        /// 三象限探出父节点外)——但 UIJoyStick.OnInit 一开始就把它禁用,Update 里每次显示前都会先用
        /// SceneInput.StartScreen 重新赋值 anchoredPosition,建树期这个默认值永远不会被看到,
        /// 所以简化成与父节点同尺寸居中(0,0,200,200),不复刻这个视觉上无意义的偏移。
        /// </summary>
        private static void BuildJoyStick(Transform hudRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("UIJoyStick", hudRoot);
            UiCreatorKit.Place(root, -220f, -500f, 200f, 200f);
            var view = root.gameObject.AddComponent<UIJoyStick>();

            RectTransform gpRoot = UiCreatorKit.NewNode("JoystickDial", root); // 老端: _gp_root
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

            root.gameObject.SetActive(false); // 对标快照根节点 visible=false;摇杆按下场景空白处才显示
        }

        // ===================== 建树期专用锚点/容器(UiCreatorKit 之外的几种特例) =====================

        /// <summary>
        /// 模板/克隆源的隐藏容器(本文件私有,不跨文件复用):独立于真正承载克隆体的容器(如
        /// SkillIconGrid),自身默认 inactive,MainUISkillItem / CirCleCdView / MainUISkillItemGod
        /// 三个模板都挂在它下面,避免模板节点和运行时克隆体混在同一个父节点下。
        /// </summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>左上锚点+左上枢轴摆位,给需要"Laya 风格 x,y=左上角"直接赋值的克隆模板用(见 #1)。</summary>
        private static void PlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>左中锚点+左中枢轴摆位,给需要"改宽度只缩右边"的进度条用(见 #2)。</summary>
        private static void PlaceLeftCenter(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        // ===================== 预览 =====================

        /// <summary>
        /// 照 Login 样板的形状适配:HudSkill 没有单一顶层 View(三个子根各自是独立 BaseView,
        /// 也没有 [UIView] 寻址特性),不能直接 ViewManager.Open&lt;T&gt;()。改为直接把最新 prefab
        /// 实例化到 Main 层,再对里面每个 BaseView 调 Show()(对标 MainUIFlow.ShowFirstPassViews 的做法)。
        /// </summary>
        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudSkill",
                    "请先进入 Play 模式(主界面已加载、UI 层已初始化)再点预览。\n\n" +
                    "预览会把最新 HudSkill.prefab 实例化到 Main 层,并对三个子根分别调用 Show()," +
                    "仅用于看结构/试交互;此 prefab 未接 ViewManager 寻址,不经 ViewManager.Open。",
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
            _previewInstance.name = "HudSkill(Preview)";

            BaseView[] views = _previewInstance.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views)
            {
                v.Show();
            }
        }
    }
}
