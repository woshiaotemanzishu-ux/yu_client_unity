using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Login;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 选角页(重构版)纯代码建树生成器(对标老端 LoginSelectRoleView.scene + LoginSelectRoleItem.scene)。
    ///
    /// 结构:全屏背景 + 3D 模型容器 _gp_model(铺满,模型台贴 RawImage 进这里)+
    ///   角色卡容器 _box_items(横排)里放一张隐藏模板卡 _tpl_item(底图 _img_bg / 选中胶囊 _img_bg2 /
    ///   头像 _img_head / 名字 _lb_name / 等级行 _hbox_lv{ _lb_turn / _img_sc / _lb_lv })——
    ///   运行时 RoleSelectView 克隆模板填角色数据,超出角色数=空槽(创建入口,贴 ui_Login_04)。
    ///   底部:踏入仙界 _img_enter(ui_Login_07)/ 返回 _img_return(ui_Login_01)。
    /// 挂 RoleSelectView 回填全部 public 引用,存 Assets/Prefabs/UI/Login/RoleSelectView.prefab。
    /// 元素已贴老端真实源图,贴不到回退占位色。尺寸/位置为 720×1280 起步值,自行在编辑器调。
    /// 入口在「神霄/重构UI 生成器」面板。
    /// </summary>
    public static class RoleSelectCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Login/RoleSelectView.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)
        private const string IMG_BG = "resource/game/login/other/full_screen_bg.jpg";
        private const string IMG_CARD_BG = "resource/game/login/texture/ui_Login_03.png";   // 未选底图(头像框)
        private const string IMG_CARD_BG2 = "resource/game/login/texture/ui_Login_06.png";  // 未选名牌胶囊
        private const string IMG_SC = "resource/game/login/texture/uisc_006.png";           // 升仙角标
        private const string IMG_ENTER = "resource/game/login/texture/ui_Login_07.png";      // 踏入仙界
        private const string IMG_RETURN = "resource/game/login/texture/ui_Login_01.png";     // 返回

        // 卡片尺寸(对标 LoginSelectRoleItem.scene:124×150,内部 _img_bg 102×102 头像框、_img_bg2 82×126 名牌)
        private const float CardW = 150f, CardH = 230f;
        private const float HeadFrameW = 110f, HeadFrameH = 110f;
        private const float NamePlateW = 96f, NamePlateH = 140f;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Login",
                Name = "RoleSelect(选角)",
                Note = "角色卡列表(模板克隆)+ 3D 模型 + 踏入仙界/返回",
                Order = 50,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再激活(与 LoginPanelCreator 一致)。
            RectTransform root = UiCreatorKit.NewRoot("RoleSelectView");
            root.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<RoleSelectView>();

            // 全屏背景
            Image bg = UiCreatorKit.NewImage("Bg", root);
            UiCreatorKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_BG, UiCreatorKit.Palette.Bg);

            // 3D 模型容器(铺满;UIModelStage 把 RawImage 贴进来)。放在背景之上、卡片之下。
            RectTransform model = UiCreatorKit.NewNode("_gp_model", root);
            UiCreatorKit.Stretch(model);
            view._gp_model = model;

            // 角色卡横排容器(屏幕下半,模型上方)。挂 HorizontalLayoutGroup 自动排列克隆卡。
            RectTransform boxItems = UiCreatorKit.NewNode("_box_items", root);
            UiCreatorKit.Place(boxItems, 0f, -360f, UiCreatorKit.DesignWidth, CardH);
            var hlg = boxItems.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 16f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            view._box_items = boxItems;

            // 模板卡(默认隐藏,作克隆源)。建在容器下,RoleSelectView 克隆它填数据。
            view._tpl_item = BuildTemplateCard(boxItems);

            // 踏入仙界(底部居中偏右,对标老端 _img_enter)
            Image enter = UiCreatorKit.NewImage("_img_enter", root);
            UiCreatorKit.Place(enter.rectTransform, 0f, -540f, 378f, 140f);
            UiCreatorKit.TrySetSprite(enter, IMG_ENTER, UiCreatorKit.Palette.BtnPrimary);
            view._img_enter = enter;

            // 返回(左上角,对标老端 _img_return centerX=-375)
            Image ret = UiCreatorKit.NewImage("_img_return", root);
            UiCreatorKit.Place(ret.rectTransform, -298f, 595f, 124f, 91f);
            UiCreatorKit.TrySetSprite(ret, IMG_RETURN, UiCreatorKit.Palette.BtnSecond);
            view._img_return = ret;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] RoleSelectView.prefab 已生成: " + PrefabPath +
                      "(可经 ViewManager.Open<RoleSelectView>() 加载;真机包前记得跑 Addressable 自动分组)");
        }

        /// <summary>建一张模板角色卡(节点名与 RoleSelectView 按名查找一致);默认隐藏作克隆源。</summary>
        private static RectTransform BuildTemplateCard(Transform parent)
        {
            RectTransform card = UiCreatorKit.NewNode("_tpl_item", parent);
            UiCreatorKit.Place(card, 0f, 0f, CardW, CardH);

            // 头像框底图(未选 ui_Login_03;点击命中体,raycastTarget 默认开)
            Image bgImg = UiCreatorKit.NewImage("_img_bg", card);
            UiCreatorKit.Place(bgImg.rectTransform, 0f, 50f, HeadFrameW, HeadFrameH);
            UiCreatorKit.TrySetSprite(bgImg, IMG_CARD_BG, UiCreatorKit.Palette.Panel);

            // 头像(贴在头像框内,运行时换图)
            Image head = UiCreatorKit.NewImage("_img_head", bgImg.transform);
            UiCreatorKit.Place(head.rectTransform, 0f, 0f, 90f, 90f);
            head.raycastTarget = false;
            head.color = UiCreatorKit.Palette.Mark;   // 占位:运行时换成真头像

            // 名牌胶囊(未选 ui_Login_06,承载名字/等级)
            Image bg2 = UiCreatorKit.NewImage("_img_bg2", card);
            UiCreatorKit.Place(bg2.rectTransform, 0f, -65f, NamePlateW, NamePlateH);
            bg2.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg2, IMG_CARD_BG2, UiCreatorKit.Palette.BtnSecond);

            // 名字
            TextMeshProUGUI name = UiCreatorKit.NewText("_lb_name", card, "角色名");
            UiCreatorKit.Place(name.rectTransform, 0f, -42f, NamePlateW, 30f);
            name.fontSize = 22f;

            // 等级行:转生 + 升仙角标 + 等级(横排)
            RectTransform hbox = UiCreatorKit.NewNode("_hbox_lv", card);
            UiCreatorKit.Place(hbox, 0f, -118f, NamePlateW, 26f);
            var lvLayout = hbox.gameObject.AddComponent<HorizontalLayoutGroup>();
            lvLayout.childAlignment = TextAnchor.MiddleCenter;
            lvLayout.spacing = 2f;
            lvLayout.childForceExpandWidth = false;
            lvLayout.childForceExpandHeight = false;
            lvLayout.childControlWidth = true;
            lvLayout.childControlHeight = false;

            TextMeshProUGUI turn = UiCreatorKit.NewText("_lb_turn", hbox, "1转");
            UiCreatorKit.Place(turn.rectTransform, 0f, 0f, 36f, 24f);
            turn.fontSize = 20f;

            Image sc = UiCreatorKit.NewImage("_img_sc", hbox);
            UiCreatorKit.Place(sc.rectTransform, 0f, 0f, 22f, 24f);
            sc.raycastTarget = false;
            UiCreatorKit.TrySetSprite(sc, IMG_SC, UiCreatorKit.Palette.Mark);
            sc.enabled = false;   // 默认不显示升仙角标

            TextMeshProUGUI lv = UiCreatorKit.NewText("_lb_lv", hbox, "1级");
            UiCreatorKit.Place(lv.rectTransform, 0f, 0f, 50f, 24f);
            lv.fontSize = 20f;

            card.gameObject.SetActive(false);   // 模板默认隐藏,RoleSelectView 克隆后激活
            return card;
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 RoleSelectView",
                    "请先进入 Play 模式(登录场景已起、UI 层已初始化)再点预览。\n\n" +
                    "预览经 ViewManager.Open<RoleSelectView>() 加载新 prefab,仅用于看结构/试交互。\n" +
                    "注意:需先有角色列表数据(走过 10000 回包)才能看到角色卡;否则只显示空槽创建入口。",
                    "好");
                return;
            }
            ViewManager.Dispose<RoleSelectView>();
            _ = ViewManager.Open<RoleSelectView>();
        }
    }
}
