using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Pet;

namespace Shenxiao.Editor.UiCreator.Pet
{
    /// <summary>
    /// 【灵宠/培养】模块 prefab(PetModule = OutWardBaseView 培养页)纯代码建树生成器。
    ///
    /// 结构对标老端 yu_client/h5/laya/pages/resource/game/pet/OutWardBaseView.scene(720×992,坐骑/剑魄同修/
    /// 翼影/符相/神兵 六系统共用此布局,仅 type_id 不同)。原地覆盖 Assets/Prefabs/UI/Pet/PetModule.prefab
    /// (PetFlow 按地址 prefabs/ui/pet/petmodule 加载,GUID/地址不变);窗框(标题/页签/关闭)不在本 prefab,
    /// 由 PetFlow 用共享 BaseWindowSkin 装配(对标老端 MountPetView extends BaseWindowComponent)。
    ///
    /// 布局【1:1 事实源 = 运行时快照】:主体几何一律经 <see cref="SnapRect"/> 从
    /// output/manual_round/oldclient_pet_21_partner_stage.json(123123 号 剑魄同修页打开态)取运行时结算值
    /// (gx/gy 差值,见 UiSnapshot 类注释);快照没有的节点(等级线 donw_group_2/btn_group_2/select_btn/
    /// critGroup/preview_image 等隐藏件)用 .scene 设计值兜底(fallback 参数)。
    ///
    /// 【烤满】:战力标签/装备背包位/魔晶/技能位/培养材料这些运行时克隆件,烤既有模板 prefab 起步实例
    /// (Common/FightingShowSmallItem、PetEquip/PetEquipOutItem、Pet/PetRoundItem、Common/BaseAwardItem,
    /// 单一事实源不重建),运行时 OutWardBaseView 收编刷真数据。
    ///
    /// 命名约定:绑定节点直接用老端变量名(lv_button/star0/…,Bind 字段 105 个,对账直观);
    /// 注意老端命名陷阱:shadow_group 的子节点叫 star*(灰底星),star_group 的子节点叫 shadow*(亮星),
    /// 名字与视觉互换是老端原样,不纠正(Bind 字段同名对齐)。
    ///
    /// 元素尽量贴老端真实源图,贴不到回退占位色(TrySetSprite 告警)。入口在「神霄/重构UI 生成器」面板。
    /// 真机包前记得跑 Addressable 自动分组。
    /// </summary>
    public static class PetCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Pet/PetModule.prefab";
        private const string FightingShowSmallItemPrefab = "Assets/Prefabs/UI/Common/FightingShowSmallItem.prefab";
        private const string BaseAwardItemPrefab = "Assets/Prefabs/UI/Common/BaseAwardItem.prefab";
        private const string PetRoundItemPrefab = "Assets/Prefabs/UI/Pet/PetRoundItem.prefab";
        private const string PetEquipOutItemPrefab = "Assets/Prefabs/UI/PetEquip/PetEquipOutItem.prefab";
        // 老端运行时快照(123123 号 剑魄同修培养页打开态;1:1 几何事实源,见类注释)
        private const string SnapshotPath = "output/manual_round/oldclient_pet_21_partner_stage.json";

        /// <summary>快照里的 OutWardBaseView 子树根(Generate 时加载;缺快照=null → 全部走设计值兜底)。</summary>
        private static UiSnapshot.Node _snapRoot;

        // ---- 老端源图(pet/common/common4/role,TrySetSprite 缺图自动回退占位色) ----
        private const string IMG_PROPTITY = "resource/game/pet/texture/uiwg_010a.png";
        private const string IMG_NAME_BG = "resource/game/pet/texture/uirwv4_031.png";
        private const string IMG_STAR_DARK = "resource/game/pet/texture/pet_star_shadow.png";
        private const string IMG_STAR_LIT = "resource/game/pet/texture/pet_star.png";
        private const string IMG_PAGE_ARROW = "resource/game/pet/texture/uiwgg_006.png";
        private const string IMG_BLESS_RING_BG = "resource/game/pet/texture/uicj_014.png";
        private const string IMG_BLESS_RING = "resource/game/pet/texture/uicj_015.png";
        private const string IMG_ILLU_TAG_BG = "resource/game/pet/texture/uiwgg_002.png";
        private const string IMG_ILLU_ON = "resource/game/pet/texture/uity_045c.png";
        private const string IMG_ILLU_OFF = "resource/game/pet/texture/uity_045d.png";
        private const string IMG_PET_BAG = "resource/game/pet/texture/pet_bag.png";
        private const string IMG_ILLUSION_BTN = "resource/game/pet/texture/uiwg_010.png";
        private const string IMG_SUBTAB_1 = "resource/game/pet/texture/ui_title_up2.png";
        private const string IMG_SUBTAB_2 = "resource/game/pet/texture/ui_title_up1.png";
        private const string IMG_TXT_BG = "resource/game/pet/texture/img_txt_bg.png";
        private const string IMG_SKILL_TIP_BG = "resource/game/pet/texture/ui_chief_06.png";
        private const string IMG_SELECT_BTN = "resource/game/pet/texture/uiwg_023.png";
        private const string IMG_PREVIEW = "resource/game/pet/texture/uiwg_004.png";
        private const string IMG_RECT_BTN1 = "resource/game/common/texture/com_rect_btn1.png";
        private const string IMG_RED_POINT = "resource/game/common/texture/com_red_point.png";
        private const string IMG_GX = "resource/game/common/texture/com_ui_gx.png";
        private const string IMG_GX1 = "resource/game/common/texture/com_ui_gx1.png";
        private const string IMG_BOTTOM_BG = "resource/game/common4/other/bg_07.png";
        private const string IMG_SWITCH = "resource/game/role/texture/ui_change.png";

        private static readonly Color NameWhite = Hex("#ffffff");
        private static readonly Color StageYellow = Hex("#fffb98");
        private static readonly Color BtnBrown = Hex("#81452B");
        private static readonly Color BlessTitle = Hex("#663915");
        private static readonly Color BlessValue = Hex("#57160c");
        private static readonly Color TitleGray = Hex("#525153");
        private static readonly Color IlluBrown = Hex("#7B3434");

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Pet",
                Name = "PetModule(灵宠/培养 OutWardBaseView)",
                Note = "坐骑/剑魄同修共用培养页:模型区+星级条+魔晶/技能位+培养材料+祝福环+一键提升;窗框页签由 PetFlow 配 BaseWindowSkin",
                Order = 20,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            UiSnapshot snap = UiSnapshot.Load(SnapshotPath);
            _snapRoot = snap != null ? snap.Find("OutWardBaseView") : null;
            if (_snapRoot == null)
            {
                Debug.LogWarning("[UiCreator] Pet 运行时快照缺失(" + SnapshotPath + "),回退 .scene 设计值(可能与老端跑偏)");
            }

            RectTransform root = UiCreatorKit.NewRoot("PetModule");
            root.gameObject.SetActive(false);

            BuildOutWardBaseView(root);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] PetModule.prefab 已生成: " + PrefabPath +
                      "(灵宠培养页;真机包前记得跑 Addressable 自动分组)");
        }

        // =====================================================================================
        // 培养页 OutWardBaseView(720×992,窗框内容区尺寸;快照 _gp_item_con 子树)
        // =====================================================================================

        private static void BuildOutWardBaseView(Transform moduleRoot)
        {
            const float W = 720f, H = 992f;
            RectTransform rt = UiCreatorKit.NewNode("OutWardBaseView", moduleRoot);
            UiCreatorKit.Place(rt, 0f, 0f, W, H);
            var view = rt.gameObject.AddComponent<OutWardBaseView>();

            // 3D 模型展示区(SetRoleModel 类容器,归 3D 线;快照 720×684 空容器)
            view.res = SNode(rt, "res", null, 0f, 0f, 720f, 684f, W, H, out _);

            BuildTopGroup(rt, view, W, H);
            BuildBottomGroup(rt, view, W, H);

            // 同修仙灵入口(FairyWish 系统未移植 → 建位隐藏,保 Bind;快照 573,5 146×146)
            view.enter_btn = SNode(rt, "enter_btn", null, 573f, 5f, 146f, 146f, W, H, out _);
            view.enter_btn.gameObject.SetActive(false);

            view.effect_group = SNode(rt, "effect_group", null, 360f, 429f, 0f, 0f, W, H, out _);

            // 等级线引导锚点(.scene 设计值;老端 UPDATE_TASK_STEP_SPE 在此显手指)
            RectTransform boxArrow = UiCreatorKit.NewNode("_box_arrow", rt);
            PlaceLaya(boxArrow, 312f, 713f, 100f, 100f, W, H);
            view._box_arrow = boxArrow;

            BuildTemplates(rt, view);
        }

        // ---------------------------------------------------------------- 顶部(top_group)

        private static void BuildTopGroup(RectTransform parent, OutWardBaseView view, float pw, float ph)
        {
            RectTransform top = SNode(parent, "top_group", null, 0f, 1f, 720f, 300f, pw, ph, out Rect rTop);
            view.top_group = top;

            // 战力标签(烤 FightingShowSmallItem 起步实例;运行时收编刷 combat)
            RectTransform gpFight = SNode(top, "_gp_fight", "top_group", 1f, 65f, 720f, 41f, rTop.width, rTop.height, out _);
            view._gp_fight = gpFight;
            GameObject fight = InstantiateExisting(FightingShowSmallItemPrefab, gpFight);
            if (fight != null)
            {
                fight.name = "FightingShowSmallItem";
                var frt = (RectTransform)fight.transform;
                frt.anchorMin = frt.anchorMax = new Vector2(0f, 1f);
                frt.pivot = new Vector2(0f, 1f);
                frt.anchoredPosition = new Vector2(265f, 0f);   // 快照 l=(265,0) 191×83
            }

            RectTransform group3 = SNode(top, "_Group3", "top_group", 132f, 75f, 456f, 30f, rTop.width, rTop.height, out Rect rG3);
            view._Group3 = group3;
            view.proptity_btn = SImg(group3, "proptity_btn", IMG_PROPTITY, "_Group3", 362f, -58f, 46f, 43f, rG3.width, rG3.height);
            view.proptity_btn.raycastTarget = true;
            view.Separate_img = SImg(group3, "Separate_img", null, "_Group3", 203f, 272f, 90f, 90f, rG3.width, rG3.height, new Color(1f, 1f, 1f, 0f));

            // 名字/阶数条
            RectTransform group5 = SNode(top, "_Group5", "top_group", 210f, 12f, 300f, 50f, rTop.width, rTop.height, out Rect rG5);
            view._Group5 = group5;
            view._Image1 = SImg(group5, "_Image1", IMG_NAME_BG, "_Group5", -3f, 9f, 306f, 32f, rG5.width, rG5.height);
            RectTransform group4 = SNode(group5, "_Group4", "_Group5", 81f, 9f, 138f, 33f, rG5.width, rG5.height, out Rect rG4);
            view._Group4 = group4;
            view.res_name = SLbl(group4, "res_name", "", "_Group4", 0f, 1f, 72f, 33f, 24f, NameWhite, rG4.width, rG4.height);
            view.res_stage = SLbl(group4, "res_stage", "", "_Group4", 87f, 1f, 36f, 33f, 24f, StageYellow, rG4.width, rG4.height);
            view.lvsystem_lv = SLbl(group4, "lvsystem_lv", "", "_Group4", 138f, 1f, 60f, 33f, 24f, StageYellow, rG4.width, rG4.height);
            view.lvsystem_lv.gameObject.SetActive(false);   // 培养线隐藏(老端 ChooseView scaleX=0),等级线显示

            // 星级条:老端命名陷阱——shadow_group 装 star*(灰底),star_group 装 shadow*(亮星),按名对齐 Bind
            RectTransform group6 = SNode(top, "_Group6", "top_group", 211f, 117f, 377f, 33f, rTop.width, rTop.height, out Rect rG6);
            view._Group6 = group6;

            RectTransform shadowGroup = SNode(group6, "shadow_group", "_Group6", 0f, 0f, 377f, 33f, rG6.width, rG6.height, out _);
            view.shadow_group = shadowGroup;
            Image[] darks = new Image[10];
            for (int i = 0; i < 10; i++)
            {
                string name = i == 0 ? "star" : "star" + (i - 1);
                darks[i] = Img(shadowGroup, name, IMG_STAR_DARK, i * 31f, 0f, 28f, 28f, 377f, 33f);
            }
            view.star = darks[0];
            view.star0 = darks[1]; view.star1 = darks[2]; view.star2 = darks[3]; view.star3 = darks[4];
            view.star4 = darks[5]; view.star5 = darks[6]; view.star6 = darks[7]; view.star7 = darks[8]; view.star8 = darks[9];

            RectTransform starGroup = SNode(group6, "star_group", "_Group6", 0f, 0f, 377f, 33f, rG6.width, rG6.height, out _);
            view.star_group = starGroup;
            Image[] lits = new Image[10];
            for (int i = 0; i < 10; i++)
            {
                string name = i == 0 ? "shadow" : "shadow" + (i - 1);
                lits[i] = Img(starGroup, name, IMG_STAR_LIT, i * 31f, 0f, 28f, 28f, 377f, 33f);
            }
            view.shadow = lits[0];
            view.shadow0 = lits[1]; view.shadow1 = lits[2]; view.shadow2 = lits[3]; view.shadow3 = lits[4];
            view.shadow4 = lits[5]; view.shadow5 = lits[6]; view.shadow6 = lits[7]; view.shadow7 = lits[8]; view.shadow8 = lits[9];

            view.star_effect = SNode(group6, "star_effect", "_Group6", 0f, 0f, 0f, 0f, rG6.width, rG6.height, out _);
        }

        // ---------------------------------------------------------------- 下部(bottom_group)

        private static void BuildBottomGroup(RectTransform parent, OutWardBaseView view, float pw, float ph)
        {
            RectTransform bottom = SNode(parent, "bottom_group", null, 0f, 566f, 720f, 422f, pw, ph, out Rect rB);
            view.bottom_group = bottom;
            float BW = rB.width, BH = rB.height;

            // 上一个/下一个外观翻页箭头(快照 y 为负:相对 bottom_group 上方)
            RectTransform before = SNode(bottom, "before_btn", "bottom_group", 8f, -294f, 110f, 170f, BW, BH, out Rect rBefore);
            view.before_btn = before;
            view.before_btn1 = SImg(before, "before_btn1", IMG_PAGE_ARROW, "before_btn", 7f, 58f, 36f, 54f, rBefore.width, rBefore.height);
            view.before_btn1.raycastTarget = true;

            RectTransform after = SNode(bottom, "after_btn", "bottom_group", 630f, -294f, 110f, 170f, BW, BH, out Rect rAfter);
            view.after_btn = after;
            view.after_btn1 = SImg(after, "after_btn1", IMG_PAGE_ARROW, "after_btn", 74f, 112f, 36f, 54f, rAfter.width, rAfter.height);
            view.after_btn1.raycastTarget = true;
            view.after_btn1.rectTransform.localScale = new Vector3(-1f, 1f, 1f);   // 老端右箭头=左箭头镜像

            // 侍魂装备背包位(烤 4 个 PetEquipOutItem 起步实例;运行时收编)
            RectTransform equip = SNode(bottom, "_group_equip", "bottom_group", 0f, -344f, 720f, 280f, BW, BH, out _);
            view._group_equip = equip;
            Vector2[] equipPos = { new Vector2(43f, 0f), new Vector2(590f, 0f), new Vector2(43f, 170f), new Vector2(590f, 170f) };
            for (int i = 0; i < equipPos.Length; i++)
            {
                GameObject item = InstantiateExisting(PetEquipOutItemPrefab, equip);
                if (item == null) continue;
                item.name = "PetEquipOutItem_" + i;
                var irt = (RectTransform)item.transform;
                irt.anchorMin = irt.anchorMax = new Vector2(0f, 1f);
                irt.pivot = new Vector2(0f, 1f);
                irt.anchoredPosition = new Vector2(equipPos[i].x, -equipPos[i].y);
            }

            BuildDownGroup1(bottom, view, BW, BH);

            // 等级线容器(OutwardLvSystem 子件未移植 → 空容器隐藏,保 Bind;.scene 720×457)
            RectTransform down2 = UiCreatorKit.NewNode("donw_group_2", bottom);
            PlaceLaya(down2, 0f, 0f, 720f, 457f, BW, BH);
            down2.gameObject.SetActive(false);
            view.donw_group_2 = down2;

            // 侧边按钮(middle_group;快照 116×0,子项 y 相对为负=在上方)
            RectTransform middle = SNode(bottom, "middle_group", "bottom_group", 0f, 0f, 116f, 0f, BW, BH, out Rect rMid);
            view.middle_group = middle;
            view.bag_btn = SImg(middle, "bag_btn", IMG_PET_BAG, "middle_group", 18f, -83f, 80f, 80f, rMid.width, rMid.height);
            view.bag_btn.raycastTarget = true;
            view.illusion_btn = SImg(middle, "illusion_btn", IMG_ILLUSION_BTN, "middle_group", 24f, -550f, 91f, 85f, rMid.width, rMid.height);
            view.illusion_btn.raycastTarget = true;
            view.bag_red = Img(middle, "bag_red", IMG_RED_POINT, 63f, -84f, 24f, 24f, rMid.width, rMid.height);
            view.bag_red.gameObject.SetActive(false);
            view.illu_red = Img(middle, "illu_red", IMG_RED_POINT, 86f, -562f, 24f, 24f, rMid.width, rMid.height);
            view.illu_red.gameObject.SetActive(false);
            view.preview_image = Img(middle, "preview_image", IMG_PREVIEW, 595f, 16f, 90f, 90f, rMid.width, rMid.height);
            view.preview_image.gameObject.SetActive(false);

            // 培养线/等级线 子页签(tab_group)
            RectTransform tab = SNode(bottom, "tab_group", "bottom_group", 144f, 44f, 139f, 43f, BW, BH, out Rect rTab);
            view.tab_group = tab;

            RectTransform btnGroup1 = SNode(tab, "btn_group_1", "tab_group", -2f, -3f, 144f, 42f, rTab.width, rTab.height, out Rect rBg1);
            view.btn_group_1 = btnGroup1;
            view.select_1 = SImg(btnGroup1, "select_1", IMG_SUBTAB_1, "btn_group_1", 0f, 0f, 74f, 41f, rBg1.width, rBg1.height);
            view.select_1.raycastTarget = true;
            view.btn_group_1_red = Img(btnGroup1, "btn_group_1_red", IMG_RED_POINT, 74f, -12f, 24f, 24f, rBg1.width, rBg1.height);
            view.btn_group_1_red.gameObject.SetActive(false);

            RectTransform btnGroup2 = UiCreatorKit.NewNode("btn_group_2", tab);
            PlaceLaya(btnGroup2, -2f, -3f, 144f, 42f, rTab.width, rTab.height);
            btnGroup2.gameObject.SetActive(false);   // 培养线显示 btn_group_1,等级线显示 btn_group_2(老端 ChooseView)
            view.btn_group_2 = btnGroup2;
            view.select_2 = Img(btnGroup2, "select_2", IMG_SUBTAB_2, 0f, 0f, 74f, 41f, 144f, 42f);
            view.select_2.raycastTarget = true;
            view.btn_group_2_red = Img(btnGroup2, "btn_group_2_red", IMG_RED_POINT, 75f, -12f, 24f, 24f, 144f, 42f);
            view.btn_group_2_red.gameObject.SetActive(false);

            view.btn_switch = SImg(tab, "btn_switch", IMG_SWITCH, "tab_group", 94f, -11f, 54f, 54f, rTab.width, rTab.height);
            view.btn_switch.raycastTarget = true;

            // 等级线技能提示条(.scene centerX=0/y=97,ChooseView 培养线隐藏)
            Image skillImg = Img(bottom, "skillImg", IMG_SKILL_TIP_BG, 200f, 97f, 320f, 30f, BW, BH);
            skillImg.gameObject.SetActive(false);
            view.skillImg = skillImg;
            view.skillLevelLab = Lbl(skillImg.rectTransform, "skillLevelLab", "", 15f, 5f, 283f, 22f, 22f, IlluBrown, 320f, 30f);
        }

        // ---------------------------------------------------------------- 培养线内容(down_group_1)

        private static void BuildDownGroup1(RectTransform bottom, OutWardBaseView view, float BW, float BH)
        {
            RectTransform down1 = SNode(bottom, "down_group_1", "bottom_group", 0f, 0f, 720f, 436f, BW, BH, out Rect rD1);
            view.down_group_1 = down1;
            float DW = rD1.width, DH = rD1.height;

            view.bottom_img = SImg(down1, "bottom_img", IMG_BOTTOM_BG, "down_group_1", 0f, 20f, 720f, 416f, DW, DH);
            view.bottom_img.raycastTarget = true;   // 面板主体吞点击

            // 魔晶/技能位区(_Group9)
            RectTransform group9 = SNode(down1, "_Group9", "down_group_1", 11f, 124f, 698f, 125f, DW, DH, out Rect rG9);
            view._Group9 = group9;

            RectTransform crystal = SNode(group9, "crystal_group", "_Group9", 212f, 29f, 275f, 85f, rG9.width, rG9.height, out _);
            view.crystal_group = crystal;
            for (int i = 0; i < 3; i++)
            {
                GameObject item = InstantiateExisting(PetRoundItemPrefab, crystal);
                if (item == null) continue;
                item.name = "PetRoundItem_crystal" + i;
                var irt = (RectTransform)item.transform;
                irt.anchorMin = irt.anchorMax = new Vector2(0f, 1f);
                irt.pivot = new Vector2(0f, 1f);
                irt.anchoredPosition = new Vector2(i * 95f, 0f);
            }

            RectTransform skill = SNode(group9, "skill_group", "_Group9", 127f, -222f, 490f, 163f, rG9.width, rG9.height, out _);
            view.skill_group = skill;
            Vector2[] skillPos = { new Vector2(0f, 24f), new Vector2(115f, 59f), new Vector2(230f, 59f), new Vector2(345f, 24f) };
            for (int i = 0; i < skillPos.Length; i++)
            {
                GameObject item = InstantiateExisting(PetRoundItemPrefab, skill);
                if (item == null) continue;
                item.name = "PetRoundItem_skill" + i;
                var irt = (RectTransform)item.transform;
                irt.anchorMin = irt.anchorMax = new Vector2(0f, 1f);
                irt.pivot = new Vector2(0f, 1f);
                irt.anchoredPosition = new Vector2(skillPos[i].x, -skillPos[i].y);
            }

            RectTransform childTitle = SNode(group9, "child_title", "_Group9", 277f, -15f, 134f, 30f, rG9.width, rG9.height, out Rect rCT);
            Img(childTitle, "img_txt_bg", IMG_TXT_BG, -14f, 0f, 162f, 30f, rCT.width, rCT.height);
            view._Text2 = SLbl(childTitle, "_Text2", "魔晶", "child_title", 32f, 0f, 70f, 30f, 22f, TitleGray, rCT.width, rCT.height);

            // 培养材料标题 + 材料位
            RectTransform childTitle2 = SNode(down1, "child_titile2", "down_group_1", 273f, 258f, 191f, 38f, DW, DH, out Rect rCT2);
            Img(childTitle2, "img_txt_bg", IMG_TXT_BG, 14f, 4f, 162f, 30f, rCT2.width, rCT2.height);
            view.material_text = SLbl(childTitle2, "material_text", "培养材料", "child_titile2", 52f, 8f, 88f, 22f, 22f, TitleGray, rCT2.width, rCT2.height);

            RectTransform material = SNode(down1, "material_group", "down_group_1", 201f, 309f, 332f, 90f, DW, DH, out _);
            view.material_group = material;
            for (int i = 0; i < 2; i++)
            {
                GameObject item = InstantiateExisting(BaseAwardItemPrefab, material);
                if (item == null) continue;
                item.name = "BaseAwardItem_" + i;
                var irt = (RectTransform)item.transform;
                irt.anchorMin = irt.anchorMax = new Vector2(0f, 1f);
                irt.pivot = new Vector2(0f, 1f);
                irt.anchoredPosition = new Vector2(i * 112f, 0f);
                irt.localScale = new Vector3(0.69f, 0.69f, 1f);   // 快照 90 高/原 130 高 ≈ 0.69(老端运行时缩放)
            }

            // 自动购买
            RectTransform autoGp = SNode(down1, "autoGp", "down_group_1", 406f, 289f, 149f, 47f, DW, DH, out Rect rAuto);
            view.autoGp = autoGp;
            view._Label1 = SLbl(autoGp, "_Label1", "自动购买", "autoGp", 40f, -1f, 88f, 47f, 22f, BlessTitle, rAuto.width, rAuto.height);
            view._Image14 = SImg(autoGp, "_Image14", IMG_GX, "autoGp", 0f, 5f, 34f, 33f, rAuto.width, rAuto.height);
            view._Image14.raycastTarget = true;
            view.autoImg = Img(autoGp, "autoImg", IMG_GX1, 0f, 5f, 34f, 33f, rAuto.width, rAuto.height);
            view.autoImg.raycastTarget = true;
            view.autoImg.gameObject.SetActive(false);   // 勾选态互斥显隐,运行时按 auto_buy 切

            // 祝福值环(培养线)/等级经验环(等级线)
            RectTransform expGroup = SNode(down1, "exp_group", "down_group_1", 22f, 288f, 120f, 120f, DW, DH, out Rect rExp);
            view.exp_group = expGroup;
            view.exp_bg = SImg(expGroup, "exp_bg", IMG_BLESS_RING_BG, "exp_group", -11f, -10f, 142f, 141f, rExp.width, rExp.height);
            view.exp_highlight = SImg(expGroup, "exp_highlight", IMG_BLESS_RING, "exp_group", 11f, 15f, 96f, 96f, rExp.width, rExp.height);
            view.level_text = SLbl(expGroup, "level_text", "祝福值:", "exp_group", 25f, 42f, 72f, 22f, 22f, BlessTitle, rExp.width, rExp.height);
            view.level_text.fontStyle = FontStyles.Bold;
            view.level_value = SLbl(expGroup, "level_value", "", "exp_group", 33f, 67f, 52f, 18f, 18f, BlessValue, rExp.width, rExp.height);
            view.exp_effect_group = SNode(expGroup, "exp_effect_group", "exp_group", 0f, 0f, 0f, 0f, rExp.width, rExp.height, out _);
            Image expMask = Img(expGroup, "exp_mask", null, 60f, 60f, 96f, 96f, rExp.width, rExp.height, new Color(1f, 1f, 1f, 0f));
            expMask.gameObject.SetActive(false);   // 老端运行时遮罩件,Unity 用 fillAmount 替代
            view.exp_mask = expMask;

            // 一键提升(培养线主按钮;引导 in_view step2 的手指目标)
            RectTransform lvButton = SNode(down1, "lv_button", "down_group_1", 555f, 323f, 141f, 52f, DW, DH, out Rect rLv);
            view.lv_button = lvButton;
            view.lv_button_img = SImg(lvButton, "lv_button_img", IMG_RECT_BTN1, "lv_button", 0f, 1f, 141f, 52f, rLv.width, rLv.height);
            view.lv_button_img.raycastTarget = true;
            view.lv_button_text = SLbl(lvButton, "lv_button_text", "一键提升", "lv_button", 9f, 2f, 141f, 48f, 20f, BtnBrown, rLv.width, rLv.height, TextAlignmentOptions.Center);
            view.lv_button_text.fontStyle = FontStyles.Bold;
            view.lv_btn_reddot = Img(down1, "lv_btn_reddot", IMG_RED_POINT, 672f, 319f, 24f, 24f, DW, DH);
            view.lv_btn_reddot.gameObject.SetActive(false);

            // 页内引导特效槽(MainUIGuideManager 按 slotId 在 target 子树查找消费;autoPlay 必须 false 防双挂,
            // 键值对齐 HudTaskTeam 的 main_ui_guide_* 槽 = 老端 StoryModel.CreateFinger 两件套)
            AddGuideEffectSlots(lvButton);

            // 祝福加成富文本(老端 HTMLDivElement,运行时填)
            view.blessLb = Lbl(down1, "blessLb", "", 50f, 380f, 200f, 24f, 20f, NameWhite, DW, DH);

            // 幻化状态角标(illu_group;快照显示 unuse_gp)
            RectTransform illu = SNode(down1, "illu_group", "down_group_1", 525f, 5f, 192f, 67f, DW, DH, out Rect rIllu);
            view.illu_group = illu;
            RectTransform usingGp = UiCreatorKit.NewNode("using_gp", illu);
            PlaceLaya(usingGp, 78f, 20f, 101f, 39f, rIllu.width, rIllu.height);
            usingGp.gameObject.SetActive(false);
            view.using_gp = usingGp;
            view._Image10 = Img(usingGp, "_Image10", IMG_ILLU_TAG_BG, 34f, 3f, 67f, 33f, 101f, 39f);
            view._Image11 = Img(usingGp, "_Image11", IMG_ILLU_ON, 1f, -6f, 52f, 51f, 101f, 39f);
            RectTransform unuseGp = SNode(illu, "unuse_gp", "illu_group", 78f, 20f, 101f, 39f, rIllu.width, rIllu.height, out Rect rUnuse);
            view.unuse_gp = unuseGp;
            view._Image12 = SImg(unuseGp, "_Image12", IMG_ILLU_TAG_BG, "unuse_gp", 34f, 3f, 67f, 33f, rUnuse.width, rUnuse.height);
            view._Image13 = SImg(unuseGp, "_Image13", IMG_ILLU_OFF, "unuse_gp", 1f, -6f, 52f, 51f, rUnuse.width, rUnuse.height);
            view.illu_label = SLbl(illu, "illu_label", "幻化", "illu_group", 130f, 28f, 44f, 22f, 22f, IlluBrown, rIllu.width, rIllu.height);

            // 暴击培养组(.scene 隐藏遗留,建位保 Bind)
            RectTransform crit = UiCreatorKit.NewNode("critGroup", down1);
            PlaceLaya(crit, 400f, 319f, 50f, 35f, DW, DH);
            crit.localScale = new Vector3(0.8f, 0.8f, 1f);
            crit.gameObject.SetActive(false);
            view.critGroup = crit;
            view.critEffectGroup = UiCreatorKit.NewNode("critEffectGroup", crit);
            UiCreatorKit.Place(view.critEffectGroup, -50f, 0f, 10f, 10f);
            view.critLabel = Lbl(crit, "critLabel", "", 0f, -35f, 100f, 30f, 30f, NameWhite, 50f, 35f);

            // 幻化选择钮(.scene 隐藏遗留,建位保 Bind)
            RectTransform selectBtn = UiCreatorKit.NewNode("select_btn", down1);
            PlaceLaya(selectBtn, 286f, 45f, 157f, 61f, DW, DH);
            selectBtn.gameObject.SetActive(false);
            view.select_btn = selectBtn;
            view.select_img = Img(selectBtn, "select_img", IMG_SELECT_BTN, 0f, 0f, 157f, 61f, 157f, 61f);
            view.select_text = Lbl(selectBtn, "select_text", "幻化", 0f, 15f, 157f, 30f, 26f, NameWhite, 157f, 61f, TextAlignmentOptions.Center);
            view.select_image = Img(selectBtn, "select_image", null, 0f, 0f, 157f, 61f, 157f, 61f, new Color(1f, 1f, 1f, 0f));
        }

        // ---------------------------------------------------------------- 模板(__Templates)

        private static void BuildTemplates(RectTransform viewRoot, OutWardBaseView view)
        {
            RectTransform templates = UiCreatorKit.NewNode("__Templates", viewRoot);
            UiCreatorKit.Place(templates, 0f, 0f, 100f, 100f);
            templates.gameObject.SetActive(false);

            view._tpl_BaseAwardItem = NestTemplate(BaseAwardItemPrefab, "BaseAwardItem", templates);
            view._tpl_FightingShowSmallItem = NestTemplate(FightingShowSmallItemPrefab, "FightingShowSmallItem", templates);
            view._tpl_PetRoundItem = NestTemplate(PetRoundItemPrefab, "PetRoundItem", templates);
            view._tpl_PetEquipOutItem = NestTemplate(PetEquipOutItemPrefab, "PetEquipOutItem", templates);

            // 同修仙灵入口(FairyWish 未移植,无独立 prefab 可依)→ 空占位满足 EnsureBound
            RectTransform wishStub = UiCreatorKit.NewNode("FairyWishEnterBtnStub", templates);
            UiCreatorKit.Place(wishStub, 0f, 0f, 10f, 10f);
            wishStub.gameObject.SetActive(false);
            view._tpl_FairyWishEnterBtn = wishStub.gameObject;
        }

        private static GameObject NestTemplate(string assetPath, string name, Transform parent)
        {
            GameObject go = InstantiateExisting(assetPath, parent);
            if (go == null)
            {
                RectTransform stub = UiCreatorKit.NewNode(name + "Stub", parent);
                UiCreatorKit.Place(stub, 0f, 0f, 10f, 10f);
                stub.gameObject.SetActive(false);
                return stub.gameObject;
            }
            go.name = name;
            go.SetActive(false);
            return go;
        }

        // ---------------------------------------------------------------- 布局/元素 helper(同 SettingCreator 约定)

        /// <summary>
        /// 取节点【运行时结算】局部矩形:在快照 OutWardBaseView 子树里按名找 node 与 parent,
        /// 用 gx/gy 差值算相对 parent 的左上角 + 结算 w/h;任一找不到回退设计值(fx,fy,fw,fh)。
        /// parentName 传 null 表示相对 OutWardBaseView 根。
        /// </summary>
        private static Rect SnapRect(string nodeName, string parentName, float fx, float fy, float fw, float fh)
        {
            UiSnapshot.Node node = UiSnapshot.FindIn(_snapRoot, nodeName);
            UiSnapshot.Node parent = parentName == null ? _snapRoot : UiSnapshot.FindIn(_snapRoot, parentName);
            if (node == null || parent == null) return new Rect(fx, fy, fw, fh);
            Vector2 local = node.LocalTo(parent);
            return new Rect(local.x, local.y, node.W, node.H);
        }

        /// <summary>Laya 左上原点 → Unity 中心锚(同 SettingCreator.PlaceLaya)。</summary>
        private static void PlaceLaya(RectTransform rt, float x, float y, float w, float h,
            float parentW, float parentH, float anchorX = 0f, float anchorY = 0f)
        {
            float centerTopLeftX = x + (0.5f - anchorX) * w;
            float centerTopLeftY = y + (0.5f - anchorY) * h;
            float cx = centerTopLeftX - parentW / 2f;
            float cy = -(centerTopLeftY - parentH / 2f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        private static Image SImg(Transform parent, string name, string skin, string snapParent,
            float fx, float fy, float fw, float fh, float parentW, float parentH, Color? fallback = null)
        {
            Rect r = SnapRect(name, snapParent, fx, fy, fw, fh);
            return Img(parent, name, skin, r.x, r.y, r.width, r.height, parentW, parentH, fallback);
        }

        private static TextMeshProUGUI SLbl(Transform parent, string name, string text, string snapParent,
            float fx, float fy, float fw, float fh, float fontSize, Color color, float parentW, float parentH,
            TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            Rect r = SnapRect(name, snapParent, fx, fy, fw, fh);
            return Lbl(parent, name, text, r.x, r.y, r.width + 60f, r.height, fontSize, color, parentW, parentH, align);
        }

        private static RectTransform SNode(Transform parent, string name, string snapParent,
            float fx, float fy, float fw, float fh, float parentW, float parentH, out Rect r)
        {
            r = SnapRect(name, snapParent, fx, fy, fw, fh);
            RectTransform rt = UiCreatorKit.NewNode(name, parent);
            PlaceLaya(rt, r.x, r.y, r.width, r.height, parentW, parentH);
            return rt;
        }

        private static Image Img(Transform parent, string name, string skin,
            float x, float y, float w, float h, float parentW, float parentH, Color? fallback = null)
        {
            Image img = UiCreatorKit.NewImage(name, parent);
            PlaceLaya(img.rectTransform, x, y, w, h, parentW, parentH);
            img.raycastTarget = false;
            Color fb = fallback ?? UiCreatorKit.Palette.Panel;
            if (!string.IsNullOrEmpty(skin)) UiCreatorKit.TrySetSprite(img, skin, fb);
            else img.color = fb;
            return img;
        }

        private static TextMeshProUGUI Lbl(Transform parent, string name, string text,
            float x, float y, float w, float h, float fontSize, Color color,
            float parentW = 0f, float parentH = 0f, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            TextMeshProUGUI t = UiCreatorKit.NewText(name, parent, text);
            if (parentW > 0f) PlaceLaya(t.rectTransform, x, y, w, h, parentW, parentH);
            else UiCreatorKit.Place(t.rectTransform, x, y, w, h);
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            return t;
        }

        /// <summary>给引导手指目标挂 选中光圈/点击手指 两个特效槽(对标老端 ui_yindaoxiaoguo/ui_dianjizhiyin)。</summary>
        private static void AddGuideEffectSlots(RectTransform target)
        {
            AddGuideSlot(target, "main_ui_guide_select", "ui_yindaoxiaoguo",
                "effect/objs/ui_effect/ui_yindaoxiaoguo/ui_yindaoxiaoguo", "选中光圈特效;MainUIGuideManager 手动消费,autoPlay 保持 false 防双挂");
            AddGuideSlot(target, "main_ui_guide_finger", "ui_dianjizhiyin",
                "effect/objs/ui_effect/ui_dianjizhiyin/ui_dianjizhiyin", "点击手指特效;MainUIGuideManager 手动消费,autoPlay 保持 false 防双挂");
        }

        private static void AddGuideSlot(RectTransform target, string slotId, string effectName, string addressKey, string note)
        {
            RectTransform holder = UiCreatorKit.NewNode(slotId, target);
            UiCreatorKit.Place(holder, 0f, 0f, 0f, 0f);
            var slot = holder.gameObject.AddComponent<Shenxiao.Common.UI3D.UIEffectSlot>();
            slot.ConfigureEffect(slotId, effectName, addressKey,
                "yu_client StoryModel.CreateFinger / OutWardBaseView 页内引导", note,
                Vector2.zero, Vector3.one, 0f);
        }

        private static GameObject InstantiateExisting(string assetPath, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning("[UiCreator] 找不到既有模板 prefab(应已存在,不在本次生成范围内): " + assetPath);
                return null;
            }
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
        }

        // ---------------------------------------------------------------- 预览

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 PetModule",
                    "请先进入 Play 模式(UI 层已初始化)再点预览。\n\n" +
                    "预览复刻 PetFlow:实例化 BaseWindowSkin 窗框 + PetModule 内容到 Window 层并 Show 培养页。",
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

            Transform layer = ViewManager.GetLayer(UILayer.Window);
            _previewInstance = Object.Instantiate(prefab, layer);
            var main = _previewInstance.GetComponentInChildren<OutWardBaseView>(true);
            if (main == null)
            {
                Debug.LogError("[UiCreator] 预览失败,prefab 缺 OutWardBaseView 组件");
                return;
            }
            main.Show();
            Selection.activeObject = _previewInstance;
        }
    }
}
