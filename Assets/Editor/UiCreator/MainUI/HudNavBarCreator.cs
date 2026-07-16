using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 从 HudBottomBarCreator 拆出的底部导航区域;root 收成实际占位(底部居中、870×DownH);
    /// 布局数值全部来自运行时快照,拆分未改动。唯一结构性新增:FuncIconRow(_gp_icon_con)下摆 5 个空槽
    /// (【槽位式】,对标 HudActivityCreator.BuildSlot 先例),配套 MainUIDownView 改为按槽填图标,
    /// 不再代码算 105 间距——想改布局直接在 prefab 里拖 Slot_*。
    ///
    ///   MainUIDownView —— 经验条 + 5 个功能图标(模板克隆) + 翻面转盘。
    ///     底部贴齐、宽 870(比设计宽 720 左右各溢出 75,对应老端 x=-75 的整体偏移)、水平居中,
    ///     故用「anchorMin=anchorMax=pivot=(0.5,0)/sizeDelta(870,DownH)」
    ///     (拆分后这组锚定收在区域 root 上,视图子根 Stretch 填满 root)。
    ///
    /// 换算与取舍(详见各方法注释):
    /// - DownView 老端是「高度≈0、子节点全部用负 y 向上延展」的贴底容器,y=0 是可视条底边而非顶边,
    ///   不能直接套 PlaceTL,改用 PlaceBottom(见其注释的换算推导)。
    /// - MainUIDownTips.json(背包已满提示气泡)在 MainUIDownViewBind/MainUIDownView.cs/MainFuncIconItemBind
    ///   均无引用字段,不并入本区,详见生成日志。
    ///
    /// 存 Assets/Prefabs/UI/MainUI/Regions/HudNavBar.prefab,供人工核对后再并入 MainUIModule.prefab。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   DownView:
    //     _box_con                 -> DownViewContent
    //     _img_bg(DownView 根下)   -> BottomBarBackground
    //     _gp_icon_con             -> FuncIconRow
    //     _gp_turn                 -> TurnButtonSlot
    //     _img_turn                -> FlipButtonImage
    //     _img_red(翻面红点)       -> FlipButtonRedDot
    //     _Group1(DownView 内)     -> HiddenProgressGroup
    //     _Image1                  -> HiddenProgressBarFill
    //     _Image2                  -> HiddenProgressDividerA
    //     _Image3                  -> HiddenProgressDividerB
    //     _Image4                  -> ExpBarTrack
    //     _img_exp                 -> ExpBarFill
    //     _box_exp_effect          -> ExpBarSparkleSlot
    //     _lb_exp                  -> ExpLabel
    //   MainFuncIconItem 模板:
    //     _img_icon                -> FuncIconImage
    //     _img_red                 -> FuncIconRedDot
    public static class HudNavBarCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudNavBar.prefab";

        // ---- DownView 源图 ----
        // 注:bag/pet/equip/treasure 四张功能图标(均已确认存在于 Assets/GameRes)由业务代码
        // MainUIDownView.BuildFuncIcons 在运行时按 MainUIModel.MainFuncIcons 配置逐个 ResManager.SetImageAsync
        // 换上,模板本身只贴一张代表图(IMG_ROLE),故这里不重复声明常量(槽位样例图直接按名拼路径,见 BuildSlot)。
        private const string IMG_DOWN_BG = "resource/game/mainUI/other/uizjmv3_001.png";
        private const string IMG_ROLE = "resource/game/mainUI/texture/role.png";
        private const string IMG_TURN = "resource/game/mainUI/texture/uizjmv3_015.png";
        private const string IMG_PRO_BAR = "resource/game/mainUI/texture/ui_pro_bar_1.png";
        private const string IMG_LINE = "resource/game/mainUI/texture/line.png";
        private const string IMG_EXP = "resource/game/mainUI/texture/exp.png"; // sizeGrid 3,5,3,5(九宫格,见报告)
        private const string IMG_RED_DOT_MAINUI = "resource/game/mainUI/texture/com_red_point.png";
        private const string IMG_RED_DOT_COMMON = "resource/game/common/texture/com_red_point.png";

        // 设计尺寸(对标快照:DownView 870 宽溢出屏幕左右各 75)
        private const float DownW = 870f, DownH = 150f; // DownH 是本生成器为「负 y 向上延展」内容选的换算容器高,见 PlaceBottom 注释

        // 功能图标槽位:尺寸=MainFuncIconItem 模板根的宽高(BuildFuncIconTemplate 里 100×100),
        // x=i*105 对标老端 UpdateIconItem 的 SetPosition((index-1)*105, 0) 间距。
        private const float FuncSlotW = 100f, FuncSlotH = 100f, FuncSlotGap = 105f;

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudNavBar(底部导航)",
                Note = "经验条+5功能图标(槽位式)+翻面钮,有界 root 底中 870 宽",
                Order = 51,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再统一激活(与 Login 系列 / HudActivityCreator 一致的安全写法)。
            RectTransform root = UiCreatorKit.NewRoot("HudNavBar");
            AnchorBottomCenter(root, DownW, DownH);
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("MainUIDownView", root);
            UiCreatorKit.Stretch(viewRoot); // 填满有界 root
            var view = viewRoot.gameObject.AddComponent<MainUIDownView>();

            // 隐藏模板挂载点:功能图标模板是纯克隆源,不应裸露在可见的 FuncIconRow 容器里。
            RectTransform templates = NewTemplatesWrapper(viewRoot);

            RectTransform boxCon = UiCreatorKit.NewNode("DownViewContent", viewRoot); // 老端: _box_con
            UiCreatorKit.Stretch(boxCon); // box_con 与 viewRoot 同尺寸同中心,子节点仍可用 PlaceBottom(DownW,DownH) 换算
            view._box_con = boxCon;

            Image imgBg = UiCreatorKit.NewImage("BottomBarBackground", boxCon); // 老端: _img_bg
            PlaceBottom(imgBg.rectTransform, 75f, -144f, 720f, 140f);
            imgBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgBg, IMG_DOWN_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = imgBg;

            RectTransform gpIconCon = UiCreatorKit.NewNode("FuncIconRow", boxCon); // 老端: _gp_icon_con
            PlaceBottom(gpIconCon, 135f, -110f, 545f, 104f);
            view._gp_icon_con = gpIconCon;
            view._tpl_MainFuncIconItem = BuildFuncIconTemplate(templates);

            // 【槽位基线】FuncIconRow 下摆 5 个空槽(对标 HudActivityCreator.BuildSlot 先例;槽位位置全在 prefab,
            // 想改布局直接拖 Slot_*,代码绝不算坐标)。每槽带一张真实功能图当样例(role/bag/pet/equip/treasure,
            // 与真实底栏一致)——样例仅设计期可视,运行时被 MainUIDownView.ClearDesignTimeSampleIcons 清掉,
            // 再由 BuildFuncIcons 把克隆图标按顺序填进槽。
            string[] sampleIcons = { "role", "bag", "pet", "equip", "treasure" };
            for (int i = 0; i < sampleIcons.Length; i++)
            {
                BuildSlot(gpIconCon, i, i * FuncSlotGap, 0f, sampleIcons[i]);
            }

            RectTransform gpTurn = UiCreatorKit.NewNode("TurnButtonSlot", boxCon); // 老端: _gp_turn
            PlaceBottom(gpTurn, 676f, -127f, 115f, 115f);
            view._gp_turn = gpTurn;

            Image imgTurn = UiCreatorKit.NewImage("FlipButtonImage", gpTurn); // 老端: _img_turn
            PlaceTL(imgTurn.rectTransform, 0f, 0f, 115f, 115f, 115f, 115f);
            UiCreatorKit.TrySetSprite(imgTurn, IMG_TURN, UiCreatorKit.Palette.BtnSecond);
            view._img_turn = imgTurn;

            Image turnRed = UiCreatorKit.NewImage("FlipButtonRedDot", gpTurn); // 老端: _img_red
            PlaceTL(turnRed.rectTransform, 87f, 0f, 28f, 29f, 115f, 115f);
            UiCreatorKit.TrySetSprite(turnRed, IMG_RED_DOT_MAINUI, UiCreatorKit.Palette.Mark);
            view._img_red = turnRed; // 老端场景默认 visible=true;HideUnbackedIndicators 会在 OnInit 立即隐藏

            // HiddenProgressGroup:老端场景声明 visible=false 的旧进度条变体(带两条分隔线),恒定隐藏、无代码引用。
            RectTransform group1 = UiCreatorKit.NewNode("HiddenProgressGroup", boxCon); // 老端: _Group1
            PlaceBottom(group1, 73f, -10f, 603f, 11f);
            group1.gameObject.SetActive(false); // 老端场景声明 visible=false
            view._Group1 = group1;

            Image image1 = UiCreatorKit.NewImage("HiddenProgressBarFill", group1); // 老端: _Image1
            PlaceTL(image1.rectTransform, -58f, 0f, 720f, 12f, 603f, 11f);
            UiCreatorKit.TrySetSprite(image1, IMG_PRO_BAR, UiCreatorKit.Palette.Panel);
            view._Image1 = image1;

            Image image2 = UiCreatorKit.NewImage("HiddenProgressDividerA", group1); // 老端: _Image2
            PlaceTL(image2.rectTransform, 287f, 0f, 146f, 11f, 603f, 11f);
            UiCreatorKit.TrySetSprite(image2, IMG_LINE, UiCreatorKit.Palette.Panel);
            view._Image2 = image2;

            Image image3 = UiCreatorKit.NewImage("HiddenProgressDividerB", group1); // 老端: _Image3
            PlaceTL(image3.rectTransform, 503f, 0f, 146f, 11f, 603f, 11f);
            UiCreatorKit.TrySetSprite(image3, IMG_LINE, UiCreatorKit.Palette.Panel);
            view._Image3 = image3;

            // ExpBarTrack:经验条背景轨道(常显),真正的经验填充在下方 ExpBarFill(_img_exp)上叠加。
            Image image4 = UiCreatorKit.NewImage("ExpBarTrack", boxCon); // 老端: _Image4
            PlaceBottom(image4.rectTransform, 75f, -12f, 720f, 12f);
            UiCreatorKit.TrySetSprite(image4, IMG_PRO_BAR, UiCreatorKit.Palette.Panel);
            view._Image4 = image4;

            // ExpBarFill:经验条:起步按满宽贴(722,对标 EXP_BAR_MAX_WIDTH),OnInit 的 RefreshExp() 会立即按真实 Exp/ExpLim 改宽。
            Image imgExp = UiCreatorKit.NewImage("ExpBarFill", viewRoot); // 老端: _img_exp
            PlaceBottom(imgExp.rectTransform, 73f, -12f, 722f, 12f);
            UiCreatorKit.TrySetSprite(imgExp, IMG_EXP, UiCreatorKit.Palette.BtnPrimary);
            view._img_exp = imgExp;

            // ExpBarSparkleSlot:经验条闪光特效挂点,老端用 anchorX=1+right 钉在经验条右端(不随经验条宽度变化),
            // 换算成 Unity 右中锚定,近似复刻(纯特效挂点,像素级位置不影响业务逻辑)。
            RectTransform boxExpEffect = UiCreatorKit.NewNode("ExpBarSparkleSlot", imgExp.transform); // 老端: _box_exp_effect
            PlaceRightMiddle(boxExpEffect, -25f, 50f, 50f);
            view._box_exp_effect = boxExpEffect;

            // ExpLabel:老端 runtime 是 x=254,y=-12,w=724,h=24,fontSize=24,scale=0.5。
            // Unity 不保留内部 0.5 缩放,直接换算成最终视觉框 362x12 / 12px 字号,中心仍落在屏幕 x=360。
            TextMeshProUGUI lbExp = UiCreatorKit.NewText("ExpLabel", viewRoot, "0 / 0"); // 老端: _lb_exp
            PlaceBottom(lbExp.rectTransform, 254f, -12f, 362f, 12f);
            lbExp.fontSize = 12f;
            view._lb_exp = lbExp;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudNavBar.prefab 已生成: " + PrefabPath +
                      "(底部导航区域,槽位式:FuncIconRow 下 " + sampleIcons.Length + " 个空槽,位置全在 prefab 可拖;人工核对后再并入 MainUIModule.prefab)");
        }

        /// <summary>
        /// _tpl_MainFuncIconItem:单个功能图标模板(默认隐藏),运行时 BuildFuncIcons 按当前开放的功能数
        /// 从这一份模板克隆、按顺序填进 FuncIconRow 下的槽位,故这里只建一份,不是快照里看到的 5 个运行时克隆实例。
        /// </summary>
        private static GameObject BuildFuncIconTemplate(Transform parent)
        {
            RectTransform item = UiCreatorKit.NewNode("MainFuncIconItem", parent);
            PlaceTL(item, 0f, 0f, 100f, 100f, 545f, 104f); // 起步落在第一格位置,克隆后由 BuildFuncIcons 移进槽位
            var view = item.gameObject.AddComponent<MainFuncIconItem>();

            Image icon = UiCreatorKit.NewImage("FuncIconImage", item); // 老端: _img_icon
            PlaceTL(icon.rectTransform, 8f, 6f, 84f, 88f, 100f, 100f);
            UiCreatorKit.TrySetSprite(icon, IMG_ROLE, UiCreatorKit.Palette.BtnSecond); // 起步贴"角色"代表图,运行时按配置换(role/bag/pet/equip/treasure)
            view._img_icon = icon;

            Image red = UiCreatorKit.NewImage("FuncIconRedDot", item); // 老端: _img_red
            PlaceTL(red.rectTransform, 72f, 0f, 24f, 24f, 100f, 100f);
            UiCreatorKit.TrySetSprite(red, IMG_RED_DOT_COMMON, UiCreatorKit.Palette.Mark);
            red.gameObject.SetActive(false); // SetData 里 SetRedState(false),红点系统未移植
            view._img_red = red;

            item.gameObject.SetActive(false); // 模板默认隐藏
            return item.gameObject;
        }

        /// <summary>建一个空槽位:左上锚定的 100×100 RectTransform(=MainFuncIconItem 模板尺寸)+ 一个样例图标子节点
        /// (Stretch 填满槽;仅设计期可视,运行时被 MainUIDownView 清掉)。</summary>
        private static void BuildSlot(RectTransform parent, int index, float x, float y, string sampleIcon)
        {
            RectTransform slot = UiCreatorKit.NewNode("Slot_" + index, parent);
            AnchorTopLeft(slot, x, y, FuncSlotW, FuncSlotH);
            Image sample = UiCreatorKit.NewImage("Sample", slot);
            UiCreatorKit.Stretch(sample.rectTransform);
            UiCreatorKit.TrySetSprite(sample, "resource/game/mainUI/texture/" + sampleIcon + ".png", UiCreatorKit.Palette.BtnNeutral);
        }

        // ================================================================ 布局换算 helper(本文件专用)

        /// <summary>
        /// 建一个隐藏的模板挂载容器(__Templates):专收纯克隆源(MainFuncIconItem 模板),
        /// 不让它裸露在可见的业务容器(FuncIconRow 等)里当"不可见的子节点"。
        /// </summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>贴底、水平居中、固定宽高(区域 root 用:870 比屏宽 720 左右各溢出 75)。</summary>
        private static void AnchorBottomCenter(RectTransform rt, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(width, height);
            // 老端 DownView 基线 y=1279(720x1280 画布),比屏幕底边高 1px。
            rt.anchoredPosition = new Vector2(0f, 1f);
        }

        /// <summary>左上锚定(槽位用,与 HudActivityCreator.AnchorTopLeft 同款):x 向右、y 向下为正。</summary>
        private static void AnchorTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        /// <summary>
        /// Laya 左上原点 (x,y,w,h) → Unity 中心锚 anchoredPosition 换算(对标 HudActivityCreator 的 PlaceTL 约定):
        /// cx = x + w/2 - parentW/2,cy = -(y + h/2 - parentH/2)。用于「y=0 在顶」的常规容器(模板/按钮内部子树)。
        /// </summary>
        private static void PlaceTL(RectTransform rt, float x, float y, float w, float h, float parentW, float parentH)
        {
            float cx = x + w * 0.5f - parentW * 0.5f;
            float cy = -(y + h * 0.5f - parentH * 0.5f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        /// <summary>
        /// DownView 专用换算:老端该 view 高度≈0/1、子节点全部用【负 y 向上延展】贴底
        /// (y=0 是可视条底边=屏幕底边,不是常规「y=0 在顶」的容器),不能直接用 PlaceTL。
        /// 等价处理:先把 y 平移 +DownH(换成「0..DownH 从上到下」的常规框架)再套用同一套公式,
        /// 化简得 cy = -(y + h/2 + DownH/2);cx 不受影响,仍是 x + w/2 - DownW/2。
        /// 用于 DownView 及其挂在 _box_con/viewRoot 同一坐标系下的子节点。
        /// </summary>
        private static void PlaceBottom(RectTransform rt, float x, float y, float w, float h)
        {
            float cx = x + w * 0.5f - DownW * 0.5f;
            float cy = -(y + h * 0.5f + DownH * 0.5f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        /// <summary>右中锚定(老端 _box_exp_effect 用 anchorX=1+right 钉在经验条右端,不随经验条宽度变化)。</summary>
        private static void PlaceRightMiddle(RectTransform rt, float offsetX, float w, float h)
        {
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(offsetX, 0f);
        }

        // ================================================================ 预览

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudNavBar",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "HudNavBar 是并入 MainUIModule 的区域子视图,不走 ViewManager.Open<T>()" +
                    "(MainUIDownViewBind 没有 [UIView] 地址特性);预览直接把最新 prefab 实例化到 " +
                    "Window 层并手动调用 view.Show(),仅用于看结构/试交互。",
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

            Transform parentLayer = ViewManager.GetLayer(UILayer.Window);
            _previewInstance = Object.Instantiate(prefab, parentLayer);
            var view = _previewInstance.GetComponentInChildren<MainUIDownView>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] HudNavBar 预览实例缺少 MainUIDownView 组件");
                return;
            }
            view.Show();
        }
    }
}
