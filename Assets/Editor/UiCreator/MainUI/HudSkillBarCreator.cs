using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 从 HudSkillCreator 拆出的技能条区域;root 收成实际占位(底部中 742×107,bottom=254);
    /// 布局数值全部来自运行时快照,拆分未改动。
    ///
    /// 数据源:
    ///   A. 运行时快照(满级号)Tools/ModuleManifest/snapshots/mainui_123123/page_snapshot_*.json,
    ///      nodeTree 上每个节点自带 x/y/width/height/anchorX/anchorY/pivotX/pivotY/globalBounds —
    ///      本生成器的所有布局数值都是拿 globalBounds(= 已按锚点/主元素换算过的层级坐标)算出来的
    ///      "子节点中心 - 父节点中心"中心锚偏移,而不是直接拿 x/y 当左上角(那样在有 anchorX/anchorY
    ///      的节点上会算错,典型例子见 HudJoystickCreator 一节)。
    ///   B. 老端场景 JSON e:/GitProject/yu_client/h5/bin/resource/game/mainUI/*.json,仅在快照没有
    ///      该节点时(MainUISkillItemGod 未在满级号快照里出现)当唯一依据。
    ///
    /// 关键技术决定(#号标注,详见方法内注释):
    ///   #1 【已槽位化】技能槽位置原靠 MainUISkillView.FixedPositions + MainUISkillItem.SetPosition
    ///      (左上锚下 rt.anchoredPosition=(x,−y))在代码里摆;该 SetPosition 机制已废——现在
    ///      SkillIconGrid 下由本生成器预铺 4 个 45×47 左上锚空槽(Slot_0..3,anchoredPosition 即旧公式的
    ///      等价终态 (4,-99)/(39,-64)/(96,-63)/(132,-101),已用快照 4 个真实槽位反推验证),槽位在 prefab
    ///      可拖;运行时 RefreshSkills 把克隆体撑满所在槽(PlaceIconInSlot),模板自身锚点不再是载重项。
    ///   #2 _img_progress 同理需要左中锚点(0,0.5),因为 MainUIAutoBrushView.SetProgressWidth 直接改
    ///      sizeDelta.x,中心锚点会导致进度条从两侧往中间缩,左锚点才会保持左边不动、只缩右边。
    ///      (进度条本体在 HudAutoBrushCreator。)
    ///   #3 _tpl_MainUISkillItem 模板本体只建 1 个,不预铺 4 个"实际槽位"的常驻实例——因为
    ///      MainUISkillView.RefreshSkills 每次都会 Instantiate(_tpl_MainUISkillItem,...)
    ///      产出新克隆挂到 _box_skill_con 下,并不会清空/复用手摆的旧节点;如果预铺 4 个常驻实例,
    ///      运行时会变成 4(手摆) + 4(克隆) = 8 个图标重叠。4 个真实槽位现以【空槽】(Slot_0..3,
    ///      见 #1)形式铺在 SkillIconGrid 下,克隆体运行时撑满入槽。模板本体挂在 MainUISkillView
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
    // 模板自身节点名(MainUISkillItem / CirCleCdView / MainUISkillItemGod)按规则保留原样,
    // 运行时按这个精确字符串做克隆/调试识别,不翻译。
    public static class HudSkillBarCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudSkillBar.prefab";

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

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudSkillBar(技能条)",
                Note = "底部中技能条:莲花技能盘+伙伴技能位+自动战斗钮,有界 root 742×107",
                Order = 40,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建(对标 Login/RoleCreate 样板的安全建树习惯);建完再激活。
            RectTransform root = UiCreatorKit.NewRoot("HudSkillBar");
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(742f, 107f);
            root.anchoredPosition = new Vector2(0f, 254f);
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("MainUISkillView", root);
            UiCreatorKit.Stretch(viewRoot);
            var view = viewRoot.gameObject.AddComponent<MainUISkillView>();

            // 伙伴技能位:伙伴系统未移植,当前只显示一把锁(对标 MainUISkillView.InitPartnerSkill 的边界注释)。
            RectTransform partnerBox = UiCreatorKit.NewNode("PartnerSkillSlot", viewRoot); // 老端: _box_partner_skill
            UiCreatorKit.Place(partnerBox, 154.5f, -24f, 57f, 59f);
            view._box_partner_skill = partnerBox;

            Image partnerLock = UiCreatorKit.NewImage("PartnerLockIcon", partnerBox); // 老端: _img_partner_lock
            UiCreatorKit.Place(partnerLock.rectTransform, 0f, 0f, 57f, 59f);
            UiCreatorKit.TrySetSprite(partnerLock, IMG_PARTNER_LOCK, UiCreatorKit.Palette.BtnNeutral);
            view._img_partner_lock = partnerLock;

            // 自动战斗按钮盒(AutoFightBox 是纯结构容器,MainUISkillViewBind 没有对应字段,不用回填)。
            RectTransform autoBox = UiCreatorKit.NewNode("AutoFightBox", viewRoot); // 老端: auto_box
            UiCreatorKit.Place(autoBox, 302.5f, 0f, 137f, 107f);

            Image autoBg = UiCreatorKit.NewImage("AutoFightBg", autoBox); // 老端: auto_bg
            UiCreatorKit.Place(autoBg.rectTransform, 0f, 0f, 111f, 107f);
            UiCreatorKit.TrySetSprite(autoBg, IMG_AUTO_BG, UiCreatorKit.Palette.Panel);

            Image autoFight = UiCreatorKit.NewImage("AutoFightButton", autoBox); // 老端: _img_auto_fight
            UiCreatorKit.Place(autoFight.rectTransform, 2.5f, 5f, 132f, 65f);
            UiCreatorKit.TrySetSprite(autoFight, IMG_AUTO_FIGHT_OFF, UiCreatorKit.Palette.BtnPrimary);
            view._img_auto_fight = autoFight;

            // 技能条本体
            RectTransform skillBox = UiCreatorKit.NewNode("SkillBox", viewRoot); // 老端: skill_box
            UiCreatorKit.Place(skillBox, 0.5f, -11f, 179f, 85f);
            view.skill_box = skillBox;

            Image skillBg = UiCreatorKit.NewImage("SkillBarBg", skillBox); // 老端: _img_bg
            UiCreatorKit.Place(skillBg.rectTransform, 0f, 0f, 179f, 85f);
            UiCreatorKit.TrySetSprite(skillBg, IMG_SKILL_BAR_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = skillBg;

            RectTransform skillCon = UiCreatorKit.NewNode("SkillIconGrid", skillBox); // 老端: _box_skill_con
            UiCreatorKit.Place(skillCon, 0f, 29.5f, 179f, 144f);
            view._box_skill_con = skillCon;

            // 【槽位基线】4 个 45×47 空槽:位置=老端 MainUISkillView.fixedPositions 的等价终态
            // (anchoredPosition=(x,−y),见类注释 #1)。槽位置全在 prefab 可拖,运行时 RefreshSkills 按序
            // 把克隆体撑满填进槽;技能图标运行时必填,槽内不放样例。
            Vector2[] skillSlotPositions =
            {
                new Vector2(4f, -99f),
                new Vector2(39f, -64f),
                new Vector2(96f, -63f),
                new Vector2(132f, -101f),
            };
            for (int i = 0; i < skillSlotPositions.Length; i++)
            {
                RectTransform slot = UiCreatorKit.NewNode("Slot_" + i, skillCon);
                slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0f, 1f);
                slot.sizeDelta = new Vector2(45f, 47f);
                slot.anchoredPosition = skillSlotPositions[i];
            }

            // 模板统一挂在 __Templates(视图自己根下的隐藏容器)里,不再混进 SkillIconGrid——
            // 后者是 MainUISkillView.RefreshSkills 运行时 Instantiate 克隆体的真实落点(见类注释 #3),
            // 模板本体只是克隆源,分开放才不会被误当成"第 5 个手摆槽位"。
            RectTransform templates = NewTemplatesWrapper(viewRoot);
            BuildSkillItemTemplate(view, templates);
            BuildCirCleCdTemplate(view, templates);

            // 神祇变身按钮:未接线模板(见类注释 #4),借用伙伴技能位坐标当占位,默认禁用不冲突。
            BuildSkillItemGodTemplate(templates, 154.5f, -24f);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudSkillBar.prefab 已生成: " + PrefabPath);
        }

        /// <summary>
        /// #1(改写):SetPosition 机制已废,槽位在 prefab(SkillIconGrid 下 Slot_0..3,见类注释 #1)。
        /// 模板自身锚点不再是载重项——克隆后由 MainUISkillView.PlaceIconInSlot 重锚撑满所在槽;
        /// 这里仍按 45×47 左上锚建(与槽同尺寸,编辑器里看模板不变形)。
        /// 模板内部子节点(con/bg/icon/lock)用中心锚点摆位不受影响(子节点定位只看自身尺寸和
        /// "父节点矩形"宽高,与父节点自己的锚点/枢轴无关)。
        /// </summary>
        private static void BuildSkillItemTemplate(MainUISkillView view, Transform templatesRoot)
        {
            RectTransform itemRt = UiCreatorKit.NewNode("MainUISkillItem", templatesRoot);
            PlaceTopLeft(itemRt, 4f, -99f, 45f, 47f); // 编辑器占位(旧 FixedPositions[0] 终态);运行时入槽被重锚,数值不载重
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

        // ===================== 预览 =====================

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudSkillBar",
                    "请先进入 Play 模式(主界面已加载、UI 层已初始化)再点预览。\n\n" +
                    "预览会把最新 HudSkillBar.prefab 实例化到 Main 层,并调用 MainUISkillView.Show()," +
                    "仅用于看结构/试交互。",
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
            _previewInstance.name = "HudSkillBar(Preview)";

            var view = _previewInstance.GetComponentInChildren<MainUISkillView>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] HudSkillBar 预览实例缺少 MainUISkillView 组件");
                return;
            }
            view.Show();
        }
    }
}
