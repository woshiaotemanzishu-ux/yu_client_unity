using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Login;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 创角页(重构版)纯代码建树生成器。
    ///
    /// 结构对标截图 5/6:全屏背景 + 全屏展示视频(VideoImage,选中职业播 create2/create3 视频)
    /// + 左侧职业选择列(4 张固定职业卡) + 中央 3D 角色模型容器(ModelCon,无视频职业的拼装链兜底)
    /// + 职业名 + 职业描述三连诗句图 + 名字框(输入 + ⟳ 随机) + 底部「踏入仙界」+ 返回。
    /// 职业卡做成 4 张固定卡 CareerItem_0..3(Bg 命中体 / Icon / Label),各自手调位置,不用模板克隆;
    /// 每张卡把 bg/icon/label 直接回填进 view.careerItems。挂 RoleCreateView 并回填全部 public 引用,
    /// 存 Assets/Prefabs/UI/Login/RoleCreateView.prefab。元素已贴老端真实源图,贴不到回退占位色。
    /// 尺寸/位置为 720×1280 起步值,自行在编辑器调。入口在「神霄/重构UI 生成器」面板。
    /// </summary>
    public static class RoleCreateCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Login/RoleCreateView.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)
        private const string IMG_BG = "resource/game/login/other/full_screen_bg.jpg";
        private const string IMG_NAME_BG = "resource/game/login/texture/ui_Login_08.png";   // 名字底框
        private const string IMG_RANDOM = "resource/game/login/texture/ui_Login_09.png";     // 随机名 ⟳
        private const string IMG_RETURN = "resource/game/login/texture/ui_Login_01.png";     // 返回
        private const string IMG_ENTER = "resource/game/login/texture/ui_Login_07.png";      // 踏入仙界
        private const string IMG_ITEM_BG = "resource/game/login/texture/ui_Login_03.png";    // 职业项默认底图(未选)

        // 布局起步值(720×1280,中心锚)
        private const float CareerColX = -300f, CareerColTopY = 360f, CareerStepY = -130f;
        private const float ItemW = 102f, ItemH = 113f, ItemIconW = 76f, ItemIconH = 76f;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Login",
                Name = "RoleCreate(创角)",
                Note = "职业选择列 + 3D 模型 + 职业名/描述 + 随机名 + 踏入仙界/返回",
                Order = 60,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建:避免 TMP_InputField.OnEnable 早于接引用而刷错;建完再激活。
            RectTransform root = UiCreatorKit.NewRoot("RoleCreateView");
            root.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<RoleCreateView>();

            // ---------- 全屏背景 ----------
            Image bg = UiCreatorKit.NewImage("Bg", root);
            UiCreatorKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_BG, UiCreatorKit.Palette.Bg);
            view.bgImg = bg;

            // ---------- 创角展示视频画面(全屏 RawImage,盖背景、垫所有 UI 之下) ----------
            // 运行时 RoleCreateView 喂 VideoPlayer 的 RenderTexture(create2 出场→create3 待机循环);
            // 默认不亮(enabled=false),视频首帧就绪才点亮;没交付视频的职业保持隐身走 3D 拼装链。
            RectTransform videoRt = UiCreatorKit.NewNode("VideoImage", root);
            UiCreatorKit.Stretch(videoRt);
            RawImage video = videoRt.gameObject.AddComponent<RawImage>();
            video.raycastTarget = false;
            video.enabled = false;
            view.videoImage = video;

            // ---------- 中央 3D 模型容器(对标老 _gp_model_con,纯布局盒;无视频职业的拼装链兜底) ----------
            RectTransform modelCon = UiCreatorKit.NewNode("ModelCon", root);
            UiCreatorKit.Place(modelCon, 0f, 0f, 720f, 1280f);
            view.modelCon = modelCon;

            // ---------- 左侧职业选择列(4 张固定职业卡,竖排起步,位置各自手调) ----------
            RectTransform careerCon = UiCreatorKit.NewNode("CareerCon", root);
            UiCreatorKit.Place(careerCon, CareerColX, 0f, 226f, 1000f);
            // 职业数当前固定 4(configlogin.CreateRole.UI);要增减就改这里 + 在 prefab 加/删卡。
            const int careerCount = 4;
            view.careerItems = new RoleCreateView.CareerItem[careerCount];
            for (int i = 0; i < careerCount; i++)
            {
                view.careerItems[i] = BuildCareerItem(careerCon, i, CareerColTopY + i * CareerStepY);
            }

            // ---------- 职业名 + 描述占位 ----------
            TextMeshProUGUI careerName = UiCreatorKit.NewText("CareerName", root, "剑主");
            UiCreatorKit.Place(careerName.rectTransform, 230f, 360f, 300f, 60f);
            careerName.fontSize = 44f;
            view.careerNameLabel = careerName;

            TextMeshProUGUI careerDesc = UiCreatorKit.NewText("CareerDesc", root, "");
            UiCreatorKit.Place(careerDesc.rectTransform, 230f, 300f, 320f, 120f);
            careerDesc.fontSize = 26f;
            careerDesc.alignment = TextAlignmentOptions.Top;
            view.careerDescLabel = careerDesc;

            // ---------- 职业介绍三连诗句图(右侧;图源运行时按职业换,起步占位) ----------
            Image tips = UiCreatorKit.NewImage("Tips", root);
            UiCreatorKit.Place(tips.rectTransform, 300f, 480f, 151f, 238f);
            tips.raycastTarget = false;
            tips.color = new Color(1f, 1f, 1f, 0.6f); // 起步半透占位(无固定图,运行时换)
            view.tipsImg = tips;

            Image tips2 = UiCreatorKit.NewImage("Tips2", root);
            UiCreatorKit.Place(tips2.rectTransform, 280f, 270f, 69f, 164f);
            tips2.raycastTarget = false;
            tips2.color = new Color(1f, 1f, 1f, 0.6f);
            view.tipsImg2 = tips2;

            Image tips3 = UiCreatorKit.NewImage("Tips3", root);
            UiCreatorKit.Place(tips3.rectTransform, 190f, 50f, 251f, 273f);
            tips3.raycastTarget = false;
            tips3.color = new Color(1f, 1f, 1f, 0.6f);
            view.tipsImg3 = tips3;

            // ---------- 名字框(底框 + 输入 + ⟳ 随机) ----------
            Image nameBg = UiCreatorKit.NewImage("NameBg", root);
            UiCreatorKit.Place(nameBg.rectTransform, -9f, -363f, 216f, 60f);
            nameBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(nameBg, IMG_NAME_BG, UiCreatorKit.Palette.Input);
            view.nameBgImg = nameBg;

            TMP_InputField nameInput = UiCreatorKit.NewInput("NameInput", root, "点击输入名称", false);
            UiCreatorKit.Place((RectTransform)nameInput.transform, -20f, -363f, 190f, 50f);
            // 名字框底图已由 NameBg 承担,输入框自身底图透明(只留文字层)
            Image inputBg = nameInput.GetComponent<Image>();
            if (inputBg != null) inputBg.color = new Color(1f, 1f, 1f, 0f);
            if (nameInput.textComponent != null) nameInput.textComponent.alignment = TextAlignmentOptions.Center;
            if (nameInput.placeholder is TMP_Text ph) ph.alignment = TextAlignmentOptions.Center;
            view.nameInput = nameInput;

            UiCreatorKit.ButtonParts random = UiCreatorKit.NewButton("RandomBtn", root, "");
            UiCreatorKit.Place(random.root, 93f, -352f, 53f, 83f);
            UiCreatorKit.TrySetSprite(random.bg, IMG_RANDOM, UiCreatorKit.Palette.BtnSecond);
            view.randomBtn = random.bg;

            // ---------- 踏入仙界(创建) ----------
            UiCreatorKit.ButtonParts enter = UiCreatorKit.NewButton("EnterBtn", root, "");
            UiCreatorKit.Place(enter.root, 0f, -520f, 378f, 140f);
            UiCreatorKit.TrySetSprite(enter.bg, IMG_ENTER, UiCreatorKit.Palette.BtnPrimary);
            view.enterBtn = enter.bg;

            // ---------- 返回 ----------
            UiCreatorKit.ButtonParts back = UiCreatorKit.NewButton("ReturnBtn", root, "");
            UiCreatorKit.Place(back.root, -300f, 560f, 124f, 91f);
            UiCreatorKit.TrySetSprite(back.bg, IMG_RETURN, UiCreatorKit.Palette.BtnNeutral);
            view.returnBtn = back.bg;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] RoleCreateView.prefab 已生成: " + PrefabPath +
                      "(可经 ViewManager.Open<RoleCreateView>() 加载;真机包前记得跑 Addressable 自动分组)");
        }

        /// <summary>建一张固定职业卡(各自手调位置),回填 RoleCreateView.CareerItem 直接引用(不再按名查找)。</summary>
        private static RoleCreateView.CareerItem BuildCareerItem(Transform parent, int index, float localY)
        {
            RectTransform item = UiCreatorKit.NewNode("CareerItem_" + index, parent);
            UiCreatorKit.Place(item, 0f, localY, ItemW, ItemH);

            Image bg = UiCreatorKit.NewImage("Bg", item);
            UiCreatorKit.Place(bg.rectTransform, 0f, 0f, ItemW, ItemW);
            bg.raycastTarget = true;   // 命中体
            UiCreatorKit.TrySetSprite(bg, IMG_ITEM_BG, UiCreatorKit.Palette.Panel);

            Image icon = UiCreatorKit.NewImage("Icon", item);
            UiCreatorKit.Place(icon.rectTransform, 0f, 0f, ItemIconW, ItemIconH);
            icon.raycastTarget = false;
            icon.color = Color.white;   // 图标运行时换肤,起步留白色不挡

            TextMeshProUGUI label = UiCreatorKit.NewText("Label", item, "职业");
            UiCreatorKit.Place(label.rectTransform, 0f, -ItemH * 0.5f + 12f, ItemW, 30f);
            label.fontSize = 24f;

            return new RoleCreateView.CareerItem
            {
                root = item.gameObject,
                bg = bg,
                icon = icon,
                label = label,
            };
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 RoleCreateView",
                    "请先进入 Play 模式(登录场景已起、UI 层已初始化)再点预览。\n\n" +
                    "预览经 ViewManager.Open<RoleCreateView>() 加载新 prefab,仅用于看结构/试交互。",
                    "好");
                return;
            }
            // 先释放上次预览缓存的实例,确保每次都从【最新】prefab 重新实例化。
            ViewManager.Dispose<RoleCreateView>();
            _ = ViewManager.Open<RoleCreateView>();
        }
    }
}
