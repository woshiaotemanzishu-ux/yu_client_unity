using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Login;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 选服页(重构版)纯代码建树生成器。
    ///
    /// 结构对标截图4 / 老 LoginSelectServerView.scene:
    ///   全屏背景 + 面板框卡片 + 标题图「选择服务器」+ 顶部分割线 + 右上关闭按钮;
    ///   左侧 tabContainer(区服分组 tab 列)+ 一个默认隐藏的 tabTemplate(克隆源,内含 _img_bg/_lb_name);
    ///   右侧 itemContainer(服务器行)+ 一个默认隐藏的 itemTemplate(克隆源,内含 _img_bg/_img_mode/_img_tips/
    ///     _lb_server_name/_img_career/_lb_level)。
    ///   列表运行时由 ServerSelectView 从模板 Instantiate 填数据。
    /// 挂 ServerSelectView 并回填 public 引用(closeBtn/tabContainer/tabTemplate/itemContainer/itemTemplate),
    /// 存 Assets/Prefabs/UI/Login/ServerSelectView.prefab。元素已贴老端真实源图,贴不到回退占位色。
    /// 尺寸/位置为 720×1280 起步值,自行在编辑器调。入口在「神霄/重构UI 生成器」面板。
    /// </summary>
    public static class ServerSelectCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Login/ServerSelectView.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)
        private const string IMG_BG = "resource/game/login/other/full_screen_bg.png";
        private const string IMG_CARD = "resource/game/login/texture/ui_content_bg1.png";
        private const string IMG_TITLE = "resource/game/login/texture/ui_Login_20.png";       // 标题底「选择服务器」
        private const string IMG_TITLE_LINE = "resource/game/login/texture/com_titleline_4.png"; // 顶部分割线
        private const string IMG_CLOSE = "resource/game/login/texture/ui_close_btn_3.png";
        private const string IMG_TAB_BG = "resource/game/login/texture/ui_Login_21.png";        // tab 选中亮底
        private const string IMG_ITEM_BG = "resource/game/login/texture/bg_05.png";             // 服务器行底
        private const string IMG_ITEM_MODE = "resource/game/login/texture/ui_Login_27.png";     // 行左侧图标
        private const string IMG_ITEM_TIPS = "resource/game/login/texture/uidl_026.png";        // 「新」角标
        private const string IMG_ITEM_CAREER = "resource/game/login/texture/ui_Login_23.png";   // 职业角标

        // 卡片(中心为原点)
        private const float CardW = 700f, CardH = 1000f;
        private const float TitleY = 470f;       // 标题图(卡片顶部)
        private const float LineY = 410f;        // 分割线
        private const float CloseX = 320f, CloseY = 470f;

        // 左 tab 列(卡片内)
        private const float TabColX = -250f, TabColY = -30f, TabColW = 180f, TabColH = 760f;
        private const float TabItemW = 180f, TabItemH = 72f;

        // 右服务器列(卡片内)
        private const float ItemColX = 110f, ItemColY = -30f, ItemColW = 498f, ItemColH = 800f;
        private const float ItemW = 498f, ItemH = 96f;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Login",
                Name = "ServerSelect(选服)",
                Note = "标题+关闭 + 左侧区服 tab 列 + 右侧服务器列表(含两模板项)",
                Order = 40,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再激活(对齐 LoginPanelCreator)。
            RectTransform root = UiCreatorKit.NewRoot("ServerSelectView");
            root.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<ServerSelectView>();

            // 全屏背景
            Image bg = UiCreatorKit.NewImage("Bg", root);
            UiCreatorKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_BG, UiCreatorKit.Palette.Bg);

            // 面板框卡片
            Image card = UiCreatorKit.NewImage("Card", root);
            UiCreatorKit.Place(card.rectTransform, 0f, 0f, CardW, CardH);
            card.raycastTarget = false;
            UiCreatorKit.TrySetSprite(card, IMG_CARD, UiCreatorKit.Palette.Panel);

            // 标题图「选择服务器」
            Image title = UiCreatorKit.NewImage("TitleImg", card.transform);
            UiCreatorKit.Place(title.rectTransform, 0f, TitleY, 196f, 57f);
            title.raycastTarget = false;
            UiCreatorKit.TrySetSprite(title, IMG_TITLE, UiCreatorKit.Palette.BtnSecond);
            TextMeshProUGUI titleLabel = UiCreatorKit.NewText("TitleLabel", title.transform, "选择服务器");
            UiCreatorKit.Stretch(titleLabel.rectTransform);

            // 顶部分割线
            Image line = UiCreatorKit.NewImage("TitleLine", card.transform);
            UiCreatorKit.Place(line.rectTransform, 0f, LineY, 540f, 20f);
            line.raycastTarget = false;
            UiCreatorKit.TrySetSprite(line, IMG_TITLE_LINE, UiCreatorKit.Palette.Panel);

            // 关闭按钮(右上)
            Image close = UiCreatorKit.NewImage("CloseBtn", card.transform);
            UiCreatorKit.Place(close.rectTransform, CloseX, CloseY, 95f, 95f);
            UiCreatorKit.TrySetSprite(close, IMG_CLOSE, UiCreatorKit.Palette.BtnSecond);
            view.closeBtn = close;

            // ---------- 左侧区服 tab 列 ----------
            RectTransform tabContainer = UiCreatorKit.NewNode("TabContainer", card.transform);
            UiCreatorKit.Place(tabContainer, TabColX, TabColY, TabColW, TabColH);
            view.tabContainer = tabContainer;
            view.tabTemplate = BuildTabTemplate(tabContainer);

            // ---------- 右侧服务器列表 ----------
            RectTransform itemContainer = UiCreatorKit.NewNode("ItemContainer", card.transform);
            UiCreatorKit.Place(itemContainer, ItemColX, ItemColY, ItemColW, ItemColH);
            view.itemContainer = itemContainer;
            view.itemTemplate = BuildItemTemplate(itemContainer);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] ServerSelectView.prefab 已生成: " + PrefabPath +
                      "(可经 ViewManager.Open<ServerSelectView>() 加载;真机包前记得跑 Addressable 自动分组)");
        }

        // ---------- 模板项:tab(对标 LoginSelectServerTabItem.scene)----------
        // 子节点名必须与 ServerSelectView 运行时按名查找的一致:_img_bg / _lb_name
        private static GameObject BuildTabTemplate(Transform parent)
        {
            RectTransform tpl = UiCreatorKit.NewNode("TabTemplate", parent);
            // 锚到容器左上角:运行时按 -i*(h+space) 往下排(对齐老 list.content 行为)
            tpl.anchorMin = tpl.anchorMax = tpl.pivot = new Vector2(0.5f, 1f);
            tpl.sizeDelta = new Vector2(TabItemW, TabItemH);
            tpl.anchoredPosition = Vector2.zero;

            Image bg = UiCreatorKit.NewImage("_img_bg", tpl);
            UiCreatorKit.Stretch(bg.rectTransform);
            UiCreatorKit.TrySetSprite(bg, IMG_TAB_BG, UiCreatorKit.Palette.BtnSecond);

            TextMeshProUGUI name = UiCreatorKit.NewText("_lb_name", tpl, "区服");
            UiCreatorKit.Stretch(name.rectTransform);
            name.fontSize = 24f;

            tpl.gameObject.SetActive(false); // 模板默认隐藏(运行时克隆出来再显示)
            return tpl.gameObject;
        }

        // ---------- 模板项:服务器行(对标 LoginSelectServerItem.scene)----------
        // 子节点名:_img_bg / _img_mode / _img_tips / _lb_server_name / _img_career / _lb_level
        private static GameObject BuildItemTemplate(Transform parent)
        {
            RectTransform tpl = UiCreatorKit.NewNode("ItemTemplate", parent);
            tpl.anchorMin = tpl.anchorMax = tpl.pivot = new Vector2(0.5f, 1f);
            tpl.sizeDelta = new Vector2(ItemW, ItemH);
            tpl.anchoredPosition = Vector2.zero;

            // 行底(命中体)
            Image bg = UiCreatorKit.NewImage("_img_bg", tpl);
            UiCreatorKit.Stretch(bg.rectTransform);
            UiCreatorKit.TrySetSprite(bg, IMG_ITEM_BG, UiCreatorKit.Palette.Input);

            // 左侧模式图标(scene 默认显示)
            Image mode = UiCreatorKit.NewImage("_img_mode", tpl);
            UiCreatorKit.Place(mode.rectTransform, -210f, 5f, 42f, 55f);
            mode.raycastTarget = false;
            UiCreatorKit.TrySetSprite(mode, IMG_ITEM_MODE, UiCreatorKit.Palette.Mark);

            // 服务器名
            TextMeshProUGUI nameLabel = UiCreatorKit.NewText("_lb_server_name", tpl, "服务器 0001");
            UiCreatorKit.Place(nameLabel.rectTransform, -90f, 13f, 220f, 40f);
            nameLabel.fontSize = 24f;
            nameLabel.alignment = TextAlignmentOptions.Left;

            // 职业角标(运行时按 career 控制 enabled)
            Image career = UiCreatorKit.NewImage("_img_career", tpl);
            UiCreatorKit.Place(career.rectTransform, 70f, 0f, 46f, 47f);
            career.raycastTarget = false;
            UiCreatorKit.TrySetSprite(career, IMG_ITEM_CAREER, UiCreatorKit.Palette.BtnSecond);

            // 等级文本
            TextMeshProUGUI level = UiCreatorKit.NewText("_lb_level", tpl, "LV.99");
            UiCreatorKit.Place(level.rectTransform, 130f, 12f, 90f, 40f);
            level.fontSize = 24f;
            level.alignment = TextAlignmentOptions.Left;

            // 「新」角标(运行时按 isNew 控制 enabled)
            Image tips = UiCreatorKit.NewImage("_img_tips", tpl);
            UiCreatorKit.Place(tips.rectTransform, 180f, 12f, 71f, 71f);
            tips.raycastTarget = false;
            UiCreatorKit.TrySetSprite(tips, IMG_ITEM_TIPS, UiCreatorKit.Palette.BtnPrimary);

            tpl.gameObject.SetActive(false); // 模板默认隐藏
            return tpl.gameObject;
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 ServerSelectView",
                    "请先进入 Play 模式(登录场景已起、UI 层已初始化)再点预览。\n\n" +
                    "预览经 ViewManager.Open<ServerSelectView>() 加载新 prefab,仅用于看结构/试交互。",
                    "好");
                return;
            }
            // 先释放上次预览缓存的实例,确保每次都从【最新】prefab 重新实例化。
            ViewManager.Dispose<ServerSelectView>();
            _ = ViewManager.Open<ServerSelectView>();
        }
    }
}
