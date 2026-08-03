using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Setting;

namespace Shenxiao.Editor.UiCreator.Setting
{
    /// <summary>
    /// 【系统设置】模块合并 prefab(SettingModule)纯代码建树生成器。
    ///
    /// 结构对标老端 yu_client/h5/laya/pages/resource/game/setting/*.scene(SettingView 652×936 +
    /// SettingChangeHeadView 646×541 + SettingChangeNameView 560×320 + 三个列表项模板),原地覆盖
    /// Assets/Prefabs/UI/Setting/SettingModule.prefab(SettingFlow 按地址
    /// prefabs/ui/setting/settingmodule 加载,GUID/地址不变)。
    ///
    /// 根下 3 个直接子节点 = 3 个子窗(与旧转换器产物同序):SettingChangeHeadView / SettingChangeNameView /
    /// SettingView。SettingFlow.OpenAsync 会先把全部直接子节点 SetActive(false) 再只 Show 主窗;
    /// BaseView.Show 自带 SetAsLastSibling,子窗叠放顺序运行时自理。
    ///
    /// 命名约定说明:本模块 Bind 字段近百个,为了 prefab↔Bind 对账直观,【绑定节点直接用老端变量名】
    /// (_box_top/_lb_name/…),仅纯视觉节点(标题字/分块头)用语义名——与 HudTop 的"全语义名"约定
    /// 有意不同,权衡的是设置面板字段量级下的可维护性。
    ///
    /// 布局【1:1 事实源 = 运行时快照】(2026-07-06 改法):主窗几何一律经 <see cref="SnapRect"/> 从
    /// output/manual_round/oldclient_setting_20_setting_stage.json 取【运行时结算值】(gx/gy 差值算局部,
    /// 见 UiSnapshot 类注释)——.scene 设计值与运行时差异很大(HBox/centerX/代码改位,如 _btn_copy
    /// 设计 142×45 实际 33×33),手抄设计值必跑偏。快照没有的节点(隐藏页/门禁块/子窗/负缩放页签)
    /// 用 .scene 设计值兜底(SnapRect 的 fallback 参数)。
    /// 基础设置页五个分块由 prefab 内 VerticalLayoutGroup 按显隐纵向紧排；各分块自己的横向偏移固化在 Row 内。
    /// _list_pick/_list_shield 的 Content 使用 prefab 内 GridLayoutGroup。运行时只刷新数据和显隐，不写坐标。
    ///
    /// 模板:_tpl_SettingShieldItem/_tpl_SettingSubscriptionItem/_tpl_SettingHeadItem 重建并回填各自
    /// Bind;_tpl_WithBtnHSlider/_tpl_CustomHeadItem 嵌套引用既有 Common prefab(单一事实源,不重建);
    /// _tpl_GodBefallMainView/_tpl_BaseAwardItem 无源可依(老端此两处对应完整子系统未移植)→ 挂空占位节点
    /// 满足 EnsureBound,运行时 HideTemplates 直接隐藏。
    ///
    /// 【烤满】(2026-07-06 用户要求"prefab 静态打开即与老端界面一致"):运行时才克隆的视觉件全部烤进
    /// prefab 当"预览/起步实例"——滑条(GreenHSlider 预览件 __PreviewSlider,运行时被真 WithBtnHSlider
    /// 替换)、头像(CustomHeadItem 实例,运行时收编刷真数据)、自动拾取 3 项/屏蔽 10 项(SettingShieldItem
    /// 实例,文案/勾选态读 config_setting.json,运行时收编刷服务器值)、角色名/区服/ID/滑条数值(快照样例
    /// 文本,运行时覆写)。对应运行时收编逻辑见 SettingView.EnsureSlider/RebuildShieldItems/RefreshHeadIcon。
    ///
    /// 元素尽量贴老端真实源图,贴不到回退占位色(TrySetSprite 告警)。尺寸/位置为起步值,可在编辑器手调。
    /// 入口在「神霄/重构UI 生成器」面板。真机包前记得跑 Addressable 自动分组。
    /// </summary>
    public static class SettingCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Setting/SettingModule.prefab";
        private const string WithBtnHSliderPrefab = "Assets/Prefabs/UI/Common/WithBtnHSlider.prefab";
        private const string GreenHSliderPrefab = "Assets/Prefabs/UI/Common/GreenHSlider.prefab";
        private const string CustomHeadItemPrefab = "Assets/Prefabs/UI/Common/CustomHeadItem.prefab";
        private const string SettingConfigJson = "Assets/GameRes/resource/config/server/config_setting.json";
        // 老端运行时快照(123123 号 设置面板打开态;1:1 几何事实源,见类注释)
        private const string SnapshotPath = "output/manual_round/oldclient_setting_20_setting_stage.json";

        /// <summary>快照里的 SettingView 子树根(Generate 时加载;缺快照=null → 全部走设计值兜底)。</summary>
        private static UiSnapshot.Node _snapRoot;

        /// <summary>config_setting.json(烤制列表项文案/默认勾选态用;运行时另有 SettingConfigs 走 Addressables)。</summary>
        private static Newtonsoft.Json.Linq.JObject _settingCfg;

        /// <summary>读 type=3 设置项的显示名与默认勾选态(缺配置回退)。</summary>
        private static (string name, bool on) SettingRow(int subtype)
        {
            if (_settingCfg?["3@" + subtype] is Newtonsoft.Json.Linq.JObject o)
            {
                return (o.Value<string>("name") ?? ("设置项" + subtype), (o.Value<int?>("is_open") ?? 0) >= 1);
            }
            return ("设置项" + subtype, true);
        }

        /// <summary>
        /// 取节点【运行时结算】局部矩形:在快照 SettingView 子树里按名找 node 与 parent,
        /// 用 gx/gy 差值算相对 parent 的左上角 + 结算 w/h;任一找不到回退设计值(fx,fy,fw,fh)。
        /// parentName 传 null 表示相对 SettingView 根。
        /// </summary>
        private static Rect SnapRect(string nodeName, string parentName, float fx, float fy, float fw, float fh)
        {
            UiSnapshot.Node node = UiSnapshot.FindIn(_snapRoot, nodeName);
            UiSnapshot.Node parent = parentName == null ? _snapRoot : UiSnapshot.FindIn(_snapRoot, parentName);
            if (node == null || parent == null) return new Rect(fx, fy, fw, fh);
            Vector2 local = node.LocalTo(parent);
            return new Rect(local.x, local.y, node.W, node.H);
        }

        // ---- 老端源图(setting/common/common4,TrySetSprite 缺图自动回退占位色) ----
        private const string IMG_BG03 = "resource/game/setting/other/bg_03.png";
        private const string IMG_TITLE_BG = "resource/game/setting/texture/com_title_bg_1_.png";
        private const string IMG_UITC_027 = "resource/game/setting/texture/uitc_027.png";
        private const string IMG_UITC_027A = "resource/game/setting/texture/uitc_027a.png";
        private const string IMG_UISY_017 = "resource/game/setting/texture/uisy_017.png";
        private const string IMG_BTN_RECT8 = "resource/game/setting/texture/ui_button_rect8.png";
        private const string IMG_BTN_RECT9 = "resource/game/setting/texture/ui_button_rect9.png";
        private const string IMG_GUILD_07 = "resource/game/setting/texture/ui_guild_07.png";
        private const string IMG_XXSZ_005 = "resource/game/setting/texture/uixxsz_005.png";
        private const string IMG_XXSZ_006 = "resource/game/setting/texture/uixxsz_006.png";
        private const string IMG_XXSZ_007 = "resource/game/setting/texture/uixxsz_007.png";
        private const string IMG_XXSZ_008 = "resource/game/setting/texture/uixxsz_008.png";
        private const string IMG_XXSZ_009 = "resource/game/setting/texture/uixxsz_009.png";
        private const string IMG_TAB_RECT5 = "resource/game/setting/texture/ui_button_rect5.png";
        private const string IMG_TAB_MID1 = "resource/game/setting/texture/ui_button_mid_1.png";
        private const string IMG_FRIENDS_25 = "resource/game/setting/texture/ui_friends_25.png";
        private const string IMG_CONTENT_BG1 = "resource/game/setting/texture/ui_content_bg1.png";
        private const string IMG_HEAD_BG10 = "resource/game/setting/texture/bg_10.png";
        private const string IMG_TAKE_PHOTO = "resource/game/setting/texture/uightx_001.png";
        private const string IMG_HEAD_KUANG = "resource/game/setting/texture/head_circle_kuang.png";
        private const string IMG_NAME_BG = "resource/game/setting/texture/uism_029b.jpg";
        private const string IMG_SELECT = "resource/game/setting/texture/uixt_015.png";
        private const string IMG_USE_TAG = "resource/game/setting/texture/uixt_013.png";
        private const string IMG_NUM_BG = "resource/game/setting/texture/ui_num_bg.png";
        private const string IMG_CLOSE = "resource/game/common/texture/uity_016k.png";
        private const string IMG_GX = "resource/game/common/texture/com_ui_gx.png";
        private const string IMG_GX1 = "resource/game/common/texture/com_ui_gx1.png";
        private const string IMG_RECT_BTN1 = "resource/game/common/texture/com_rect_btn1.png";
        private const string IMG_RECT_BTN2 = "resource/game/common/texture/com_rect_btn2.png";
        private const string IMG_BTN_RECT12 = "resource/game/common/texture/ui_button_rect12.png";
        private const string IMG_SUB_BG7 = "resource/game/common/texture/com_sub_bg_7.png";
        private const string IMG_COMMON_BG03 = "resource/game/common4/texture/common_bg_03.png";

        private static readonly Color BrownText = Hex("#663915");
        private static readonly Color TitleOrange = Hex("#b94a00");
        private static readonly Color BlueId = Hex("#2971e6");
        private static readonly Color GreenValue = Hex("#0a953e");
        private static readonly Color BlockHeader = Hex("#6773a5");

        private static GameObject _previewInstance;

        // SettingModule 已进入人工精修阶段，不再注册到批量重建面板。
        // Generate 仅保留为历史诊断/灾难恢复入口，日常修改必须直接编辑当前 Prefab。
        public static void Generate()
        {
            // 1:1 几何事实源:运行时快照(缺失只告警,全部几何回退设计值)。
            UiSnapshot snap = UiSnapshot.Load(SnapshotPath);
            _snapRoot = snap != null ? snap.Find("SettingView") : null;
            if (_snapRoot == null)
            {
                Debug.LogWarning("[UiCreator] Setting 运行时快照缺失(" + SnapshotPath + "),回退 .scene 设计值(可能与老端跑偏)");
            }
            _settingCfg = System.IO.File.Exists(SettingConfigJson)
                ? Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(SettingConfigJson))
                : null;
            if (_settingCfg == null)
            {
                Debug.LogWarning("[UiCreator] config_setting.json 缺失,列表项文案用占位");
            }

            RectTransform root = UiCreatorKit.NewRoot("SettingModule");
            root.gameObject.SetActive(false);

            // 与旧转换器产物同序:改头像 / 改名 / 主窗(SettingFlow 加载后统一隐藏再 Show 主窗)。
            BuildChangeHeadView(root);
            BuildChangeNameView(root);
            BuildSettingView(root);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] SettingModule.prefab 已生成: " + PrefabPath +
                      "(系统设置模块;真机包前记得跑 Addressable 自动分组)");
        }

        // =====================================================================================
        // 主窗 SettingView(老端 SettingView.scene,画布 652×936)
        // =====================================================================================

        private static void BuildSettingView(Transform moduleRoot)
        {
            const float W = 652f, H = 936f;
            RectTransform rt = UiCreatorKit.NewNode("SettingView", moduleRoot);
            UiCreatorKit.Place(rt, 0f, 0f, W, H);
            var view = rt.gameObject.AddComponent<SettingView>();

            // 模态底(对标老端 use_background 的框架底,快照里是 730×1290 的 com_sub_bg_7 全屏底):
            // 挡穿透;点击关闭由 View 按名约定接。
            Image dim = UiCreatorKit.NewImage("ModalDim", rt);
            UiCreatorKit.Place(dim.rectTransform, 0f, 0f, 730f, 1290f);
            UiCreatorKit.TrySetSprite(dim, IMG_SUB_BG7, new Color(0f, 0f, 0f, 0.55f));
            dim.raycastTarget = true;

            view._img_bg = SImg(rt, "_img_bg", IMG_BG03, null, 0f, 17f, 643f, 867f, W, H);
            view._img_bg.raycastTarget = true; // 面板主体吞掉点击,只有面板外命中 ModalDim(点空处关闭)

            BuildTopBox(rt, view, W, H);
            BuildBottomBox(rt, view, W, H);

            // 老端 _img_line/_img_line2 运行时无皮肤(不渲染),建成透明占位保 Bind(1:1 = 不可见)。
            view._img_line = SImg(rt, "_img_line", null, null, 11f, 216f, 620f, 8f, W, H, new Color(1f, 1f, 1f, 0f));
            view._img_line2 = SImg(rt, "_img_line2", null, null, 11f, 757f, 620f, 8f, W, H, new Color(1f, 1f, 1f, 0f));

            BuildBaseSettingPage(rt, view, W, H);
            BuildShieldPage(rt, view, W, H);
            BuildTabBox(rt, view, W, H);

            // 底部空白关闭热区(老端 _img_empty 无皮肤,BindClose 命中体)+ 右上关闭。
            view._img_empty = SImg(rt, "_img_empty", null, null, 321f, 880f, 306f, 59f, W, H, new Color(1f, 1f, 1f, 0f));
            view._img_empty.raycastTarget = true;
            view._img_close = SImg(rt, "_img_close", IMG_CLOSE, null, 599f, 0f, 53f, 58f, W, H);
            view._img_close.raycastTarget = true;

            // 老端隐藏遗留(block_user 屏蔽此人钮/协议隐私链接),建出满足 Bind,保持隐藏。
            RectTransform blockUser = UiCreatorKit.NewNode("block_user", rt);
            PlaceLaya(blockUser, 495f, 656f, 100f, 100f, W, H);
            Img(blockUser, "BlockUserBg", IMG_XXSZ_006, 5f, 0f, 90f, 93f, 100f, 100f);
            blockUser.gameObject.SetActive(false);
            view.block_user = blockUser;

            // 协议/隐私链接:快照可见(《用户协议》479,695 /《隐私保护指引》435,730);打开网页未移植,仅展示。
            view._lb_agree = SLbl(rt, "_lb_agree", "《用户协议》", null, 479f, 695f, 132f, 22f, 22f, Hex("#3cad66"), W, H);
            view._lb_privacy = SLbl(rt, "_lb_privacy", "《隐私保护指引》", null, 435f, 730f, 176f, 22f, 22f, Hex("#3cad66"), W, H);

            BuildSettingViewTemplates(rt, view);
        }

        // ---------------------------------------------------------------- 顶部角色信息(_box_top)

        private static void BuildTopBox(RectTransform parent, SettingView view, float pw, float ph)
        {
            const float W = 590f, H = 300f;
            RectTransform top = UiCreatorKit.NewNode("_box_top", parent);
            PlaceLaya(top, 0f, 58f, W, H, pw, ph);
            view._box_top = top;

            // 标题组(快照 -5,-37)
            RectTransform group1 = SNode(top, "_Group1", "_box_top", -5f, -37f, 590f, 55f, W, H, out Rect rG1);
            view._Group1 = group1;
            view._Image1 = SImg(group1, "_Image1", IMG_TITLE_BG, "_Group1", 186f, 13f, 253f, 29f, rG1.width, rG1.height);
            view._Image2 = Img(group1, "_Image2", IMG_UITC_027, 238f, 12f, 114f, 31f, rG1.width, rG1.height);
            view._Image2.gameObject.SetActive(false); // 老端运行时不可见(不入快照),设计值占位保 Bind
            view._Image3 = Img(group1, "_Image3", IMG_UISY_017, 477f, 33f, 82f, 13f, rG1.width, rG1.height);
            view._Image3.gameObject.SetActive(false);
            view._lb_title_name = SLbl(group1, "_lb_title_name", "系统设置", "_Group1", 267f, 17.5f, 88f, 22f, 22f, TitleOrange, rG1.width, rG1.height);
            view._lb_title_name.fontStyle = FontStyles.Bold;

            // 头像槽:烤入 CustomHeadItem 起步实例(快照里就有;运行时 RefreshHeadIcon 直接收编刷真数据)。
            RectTransform roleHead = SNode(top, "_role_head", "_box_top", 60f, 25f, 87f, 87f, W, H, out _);
            view._role_head = roleHead;
            HorizontalLayoutGroup headLayout = roleHead.gameObject.AddComponent<HorizontalLayoutGroup>();
            headLayout.childAlignment = TextAnchor.MiddleCenter;
            headLayout.childControlWidth = false;
            headLayout.childControlHeight = false;
            headLayout.childForceExpandWidth = false;
            headLayout.childForceExpandHeight = false;
            GameObject bakedHead = InstantiateExisting(CustomHeadItemPrefab, roleHead);
            if (bakedHead != null)
            {
                bakedHead.name = "CustomHeadItem";
                var headRt = (RectTransform)bakedHead.transform;
                headRt.anchorMin = headRt.anchorMax = headRt.pivot = new Vector2(0.5f, 0.5f);
                headRt.anchoredPosition = Vector2.zero;
            }

            RectTransform group2 = SNode(top, "_Group2", "_box_top", 306f, 34f, 155f, 30f, W, H, out Rect rG2);
            view._Group2 = group2;
            view._lb_name = SLbl(group2, "_lb_name", "太叔德鸣", "_Group2", 0f, 2f, 104f, 26f, 24f, Hex("#d15e00"), rG2.width, rG2.height); // 快照样例文本,运行时覆写

            view._label3 = SLbl(top, "_label3", "角色名称:", "_box_top", 183.9f, 34f, 133f, 30f, 24f, BrownText, W, H);
            view._label2 = SLbl(top, "_label2", "区服名称:", "_box_top", 183.9f, 77f, 133f, 30f, 24f, BrownText, W, H);
            view._ID = SLbl(top, "_ID", "角 色 ID:", "_box_top", 181.9f, 118f, 133f, 30f, 24f, BrownText, W, H);
            view.id_number1 = Lbl(top, "id_number1", "UID:", 306f, 120f, 133f, 23f, 24f, BlueId, W, H, TextAlignmentOptions.Left);
            view.id_number1.gameObject.SetActive(false);
            view.id_number = SLbl(top, "id_number", "4294967366", "_box_top", 306f, 120f, 116f, 24f, 24f, BlueId, W, H);   // 快照样例,运行时覆写
            view.id_ser_name = SLbl(top, "id_ser_name", "1-2区", "_box_top", 306f, 75f, 251f, 30f, 24f, BlueId, W, H);   // 快照样例,运行时覆写

            // 老端 scene/runtime 最终值:完整的 125×37 红色按钮，而不是过期快照里的 35×35 小图标。
            RectTransform changeHead = UiCreatorKit.NewNode("change_head_btn", top);
            PlaceLaya(changeHead, 46f, 116f, 125f, 37f, W, H);
            view.change_head_btn = changeHead;
            view._Image4 = Img(changeHead, "_Image4", IMG_BTN_RECT8, 10f, 1f, 96f, 32f, 125f, 37f);
            view._Image4.raycastTarget = true;
            view._Label1 = Lbl(changeHead, "_Label1", "更换头像", 19f, 5f, 80f, 24f, 18f, Color.white, 125f, 37f);
            view._Label1.fontStyle = FontStyles.Bold;

            // 老端 scene/runtime 最终值:100×34 青色按钮，图片原始 142×45 以 0.7 缩放后正好落入容器。
            RectTransform btnCopy = UiCreatorKit.NewNode("_btn_copy", top);
            PlaceLaya(btnCopy, 500f, 115f, 100f, 34f, W, H);
            view._btn_copy = btnCopy;
            view._Image6 = Img(btnCopy, "_Image6", IMG_BTN_RECT9, 0f, 1.25f, 99.4f, 31.5f, 100f, 34f);
            view._Image6.raycastTarget = true;
            view._Label41 = Lbl(btnCopy, "_Label41", "复制", 23f, 4f, 54f, 24f, 18f, Color.white, 100f, 34f);
            view._Label41.fontStyle = FontStyles.Bold;

            // 改名钮(单图,快照 477,28,44×44)
            view._btn_changename = SImg(top, "_btn_changename", IMG_GUILD_07, "_box_top", 477f, 28f, 44f, 44f, W, H);
            view._btn_changename.raycastTarget = true;
        }

        // ---------------------------------------------------------------- 底部五钮(_box_bottom)

        private static void BuildBottomBox(RectTransform parent, SettingView view, float pw, float ph)
        {
            // 快照:_box_bottom 46,774,590×100;五钮 x=0/112/224/336/448(HBox 已结算,直接绝对摆位)。
            RectTransform bottom = SNode(parent, "_box_bottom", null, 46f, 774f, 590f, 100f, pw, ph, out Rect rBottom);
            view._box_bottom = bottom;

            view.change_role = BottomBtn(bottom, rBottom, "change_role", "_Image15", IMG_XXSZ_006, "切换角色",
                0f, 5f, 0f, out Image i15, out TextMeshProUGUI l8);
            view._Image15 = i15; view._Label8 = l8;
            view.return_login = BottomBtn(bottom, rBottom, "return_login", "_Image16", IMG_XXSZ_007, "返回登录",
                112f, 5f, 2f, out Image i16, out TextMeshProUGUI l9);
            view._Image16 = i16; view._Label9 = l9;
            view.simple_mode_btn = BottomBtn(bottom, rBottom, "simple_mode_btn", "simple_mode_bg", IMG_XXSZ_005, "极简模式",
                224f, 6f, -1f, out Image iSimple, out TextMeshProUGUI lSimple);
            view.simple_mode_bg = iSimple; view.simple_mode_label = lSimple;
            view.confirm_flee = BottomBtn(bottom, rBottom, "confirm_flee", "_Image14", IMG_XXSZ_009, "确定",
                336f, 6f, 0f, out Image i14, out TextMeshProUGUI l7);
            view._Image14 = i14; view._Label7 = l7;

            // confirm_res 无文字标签(老端只有底图;快照 448,0 内图 -1,0,107×93)。
            RectTransform res = SNode(bottom, "confirm_res", "_box_bottom", 448f, 0f, 100f, 100f, rBottom.width, rBottom.height, out Rect rRes);
            view.confirm_res = res;
            view._Image141 = SImg(res, "_Image141", IMG_XXSZ_008, "confirm_res", -1f, 0f, 107f, 93f, rRes.width, rRes.height);
            view._Image141.raycastTarget = true;
        }

        /// <summary>底部功能钮:100×100 盒(快照 x)+ 90×93 底图(命中体,快照内偏移)+ 隐藏文字标签(老端 visible=false)。</summary>
        private static RectTransform BottomBtn(Transform parent, Rect parentRect, string name, string bgBindName,
            string skin, string label, float fx, float bgX, float bgY, out Image bg, out TextMeshProUGUI text)
        {
            RectTransform box = SNode(parent, name, "_box_bottom", fx, 0f, 100f, 100f, parentRect.width, parentRect.height, out Rect rBox);
            bg = SImg(box, bgBindName, skin, name, bgX, bgY, 90f, 93f, rBox.width, rBox.height);
            bg.raycastTarget = true;
            text = Lbl(box, name + "Label", label, 0f, 38f, 100f, 26f, 24f, Color.white, rBox.width, rBox.height);
            text.gameObject.SetActive(false);
            return box;
        }

        // ---------------------------------------------------------------- 基础设置页(_box_base_setting)

        private static void BuildBaseSettingPage(RectTransform parent, SettingView view, float pw, float ph)
        {
            ScrollRect scroll = NewScrollPage("_box_base_setting", parent, out RectTransform content, 643f, 560f);
            PlaceLaya((RectTransform)scroll.transform, 0f, 226f, 643f, 530f, pw, ph);
            view._box_base_setting = scroll;
            ConfigureVerticalFlow(content);

            // 五个分块交给 prefab 内 VerticalLayoutGroup 紧排。每行保留自己的横向偏移，
            // 运行时只切换 Row 显隐，不再写 anchoredPosition，避免不同分辨率二次换算漂移。
            RectTransform sliderBox = FlowBlock("_box_slider", content, 32f, 562f, 165f);
            view._box_slider = sliderBox;
            BuildSliderBlock(sliderBox, view);

            RectTransform pickBox = FlowBlock("_box_pick", content, 60f, 556f, 148f);
            view._box_pick = pickBox;
            SImg(pickBox, "Image_112", IMG_FRIENDS_25, "_box_pick", 0f, 0f, 280f, 30f, 556f, 148f); // 快照名,分块头底
            TextMeshProUGUI pickHeader = SLbl(pickBox, "Label_83", "自动拾取", "_box_pick", 29f, 4f, 88f, 22f, 22f, BlockHeader, 556f, 148f);
            pickHeader.fontStyle = FontStyles.Bold;
            Rect rPickList = SnapRect("_list_pick", "_box_pick", 7f, 51f, 529f, 104f);
            ScrollRect pickList = NewScrollPage("_list_pick", pickBox, out RectTransform pickContent, rPickList.width, rPickList.height);
            PlaceLaya((RectTransform)pickList.transform, rPickList.x, rPickList.y, rPickList.width, rPickList.height, 556f, 148f);
            view._list_pick = pickList;
            ConfigureItemGrid(pickContent);
            BakeShieldItems(pickContent, new[] { 17, 18, 19 }); // 自动拾取三项静态预览(运行时收编刷真值)

            // 三块勾选区由纵向流在静态 prefab 和运行时使用同一套排布。
            view._box_horse = BuildCheckBlock(content, "_box_horse", "坐骑设置", "自动上下坐骑",
                SettingRow(202).on, out Image horseCheck);
            view._img_horse_check = horseCheck;

            view._box_god = BuildCheckBlock(content, "_box_god", "降神技能", "自动释放降神变身",
                SettingRow(201).on, out Image godCheck);
            view._img_god_check = godCheck;

            // 任务块:双勾(自动/手动)。
            RectTransform taskBox = FlowBlock("_box_task", content, 60f, 565f, 88f);
            view._box_task = taskBox;
            Img(taskBox, "TaskHeaderBg", IMG_FRIENDS_25, 0f, 0f, 280f, 31f, 565f, 88f);
            TextMeshProUGUI taskHeader = Lbl(taskBox, "TaskHeaderLabel", "任务设置", 30f, 4f, 110f, 24f, 22f, BlockHeader, 565f, 88f, TextAlignmentOptions.Left);
            taskHeader.fontStyle = FontStyles.Bold;
            Lbl(taskBox, "AutoTaskLabel", "自动任务", 56f, 55.5f, 100f, 24f, 22f, BrownText, 565f, 88f, TextAlignmentOptions.Left);
            Lbl(taskBox, "ManualTaskLabel", "手动任务", 220f, 55.5f, 100f, 24f, 22f, BrownText, 565f, 88f, TextAlignmentOptions.Left);
            bool taskAuto = SettingRow(21).on; // 默认自动任务(config_setting 3@21)
            view._img_task_check1 = Img(taskBox, "_img_task_check1", taskAuto ? IMG_GX1 : IMG_GX, 10f, 47f, 34f, 34f, 565f, 88f);
            view._img_task_check1.raycastTarget = true;
            view._img_task_check2 = Img(taskBox, "_img_task_check2", taskAuto ? IMG_GX : IMG_GX1, 170f, 47f, 34f, 34f, 565f, 88f);
            view._img_task_check2.raycastTarget = true;
        }

        /// <summary>坐骑/降神同构分块:分块头 + 描述 + 单勾选图(默认勾选态按 config_setting 烤入)。</summary>
        private static RectTransform BuildCheckBlock(Transform content, string name, string header, string desc,
            bool isChecked, out Image check)
        {
            RectTransform box = FlowBlock(name, content, 60f, 565f, 88f);
            Img(box, name + "HeaderBg", IMG_FRIENDS_25, 0f, 0f, 280f, 31f, 565f, 88f);
            TextMeshProUGUI headerLbl = Lbl(box, name + "HeaderLabel", header, 30f, 4f, 110f, 24f, 22f, BlockHeader, 565f, 88f, TextAlignmentOptions.Left);
            headerLbl.fontStyle = FontStyles.Bold;
            Lbl(box, name + "DescLabel", desc, 56f, 55f, 260f, 24f, 22f, BrownText, 565f, 88f, TextAlignmentOptions.Left);
            check = Img(box, "_img" + name.Substring(4) + "_check", isChecked ? IMG_GX1 : IMG_GX, 10f, 47f, 34f, 34f, 565f, 88f);
            check.raycastTarget = true;
            return box;
        }

        /// <summary>四滑条区(音乐/音效/特效数量/同屏人数,对标 _box_slider 的 2×2 排布;几何全走快照)。</summary>
        private static void BuildSliderBlock(RectTransform sliderBox, SettingView view)
        {
            const float W = 562f, H = 165f;

            view._con_music = SliderCell(sliderBox, "_con_music", "group_music", "_Label61", "_music_value",
                "slider_music", "_Group81", "slider_mask21", "音乐", "50", 28f, 4f, W, H,
                out RectTransform gMusic, out TextMeshProUGUI lbMusic, out TextMeshProUGUI musicVal,
                out RectTransform sMusic, out RectTransform gm1, out RectTransform mask1);
            view.group_music = gMusic; view._Label61 = lbMusic; view._music_value = musicVal;
            view.slider_music = sMusic; view._Group81 = gm1; view.slider_mask21 = mask1;

            view._con_audio = SliderCell(sliderBox, "_con_audio", "gourp_audio", "_Label611", "_sound_value",
                "slider_audio", "_Group8", "slider_mask2", "音效", "50", 319.5f, 4f, W, H,
                out RectTransform gAudio, out TextMeshProUGUI lbAudio, out TextMeshProUGUI soundVal,
                out RectTransform sAudio, out RectTransform ga1, out RectTransform mask2);
            view.gourp_audio = gAudio; view._Label611 = lbAudio; view._sound_value = soundVal;
            view.slider_audio = sAudio; view._Group8 = ga1; view.slider_mask2 = mask2;

            view._con_effect = SliderCell(sliderBox, "_con_effect", "group_effect", "_Label6", "_effect_value",
                "slider_conta2", "_Group811", "slider_mask211", "特效数量", "8", 28f, 82.5f, W, H,
                out RectTransform gEffect, out TextMeshProUGUI lbEffect, out TextMeshProUGUI effectVal,
                out RectTransform sEffect, out RectTransform ge1, out RectTransform mask3);
            view.group_effect = gEffect; view._Label6 = lbEffect; view._effect_value = effectVal;
            view.slider_conta2 = sEffect; view._Group811 = ge1; view.slider_mask211 = mask3;

            view._con_screen = SliderCell(sliderBox, "_con_screen", "group_screen", "_Label511", "_screen_value",
                "slider_conta1", "_Group7", "slider_mask1", "同屏人数", "5", 324f, 83f, W, H,
                out RectTransform gScreen, out TextMeshProUGUI lbScreen, out TextMeshProUGUI screenVal,
                out RectTransform sScreen, out RectTransform gs1, out RectTransform mask4);
            view.group_screen = gScreen; view._Label511 = lbScreen; view._screen_value = screenVal;
            view.slider_conta1 = sScreen; view._Group7 = gs1; view.slider_mask1 = mask4;
        }

        /// <summary>单个滑条格:标签+数值 一行(HBox 结算位来自快照)+ 滑条容器(内烤 GreenHSlider 预览件,
        /// 运行时 EnsureSlider 销毁预览、克隆真 WithBtnHSlider)+ 遮罩占位组。</summary>
        private static RectTransform SliderCell(Transform parent, string cellName, string groupName,
            string labelName, string valueName, string sliderName, string maskGroupName, string maskName,
            string title, string sampleValue, float fx, float fy, float pw, float ph,
            out RectTransform group, out TextMeshProUGUI label, out TextMeshProUGUI value, out RectTransform sliderCon,
            out RectTransform maskGroup, out RectTransform mask)
        {
            RectTransform cell = SNode(parent, cellName, "_box_slider", fx, fy, 250f, 79f, pw, ph, out Rect rCell);

            group = SNode(cell, groupName, cellName, 44f, 4f, 200f, 32f, rCell.width, rCell.height, out Rect rGroup);
            label = SLbl(group, labelName, title, groupName, 0f, 2f, 88f, 22f, 22f, BrownText, rGroup.width, rGroup.height);
            value = SLbl(group, valueName, sampleValue, groupName, 98f, 0f, 22f, 22f, 22f, GreenValue, rGroup.width, rGroup.height);

            sliderCon = SNode(cell, sliderName, cellName, -20f, 10f, 254f, 31f, rCell.width, rCell.height, out _);

            // 静态预览滑条(快照:GreenHSlider 位于容器 (42,-7)):纯视觉件,运行时被真滑条替换。
            GameObject preview = InstantiateExisting(GreenHSliderPrefab, sliderCon);
            if (preview != null)
            {
                preview.name = "__PreviewSlider";
                var prt = (RectTransform)preview.transform;
                prt.anchorMin = prt.anchorMax = new Vector2(0f, 1f);
                prt.pivot = new Vector2(0f, 1f);
                prt.anchoredPosition = new Vector2(42f, 7f);
            }

            // 老端极简模式遮罩组(slider_mask*),逻辑未启用 → 建空占位满足 Bind,默认隐藏。
            maskGroup = SNode(cell, maskGroupName, cellName, 0f, 0f, 257f, 69f, rCell.width, rCell.height, out _);
            mask = UiCreatorKit.NewNode(maskName, maskGroup);
            UiCreatorKit.Stretch(mask);
            maskGroup.gameObject.SetActive(false);
            return cell;
        }

        // ---------------------------------------------------------------- 屏蔽列表页(_box_shield_setting)

        private static void BuildShieldPage(RectTransform parent, SettingView view, float pw, float ph)
        {
            RectTransform page = UiCreatorKit.NewNode("_box_shield_setting", parent);
            PlaceLaya(page, 0f, 226f, 643f, 530f, pw, ph);
            page.gameObject.SetActive(false); // 默认展示基础设置页(SelectTab 运行时切)
            view._box_shield_setting = page;

            RectTransform shield = UiCreatorKit.NewNode("_box_shield", page);
            PlaceLaya(shield, 60f, 10f, 536f, 268f, 643f, 530f);
            view._box_shield = shield;
            Img(shield, "ShieldHeaderBg", IMG_FRIENDS_25, 1f, 0f, 280f, 31f, 536f, 268f);
            TextMeshProUGUI header = Lbl(shield, "ShieldHeaderLabel", "屏蔽设置", 31f, 5f, 110f, 24f, 22f, BlockHeader, 536f, 268f, TextAlignmentOptions.Left);
            header.fontStyle = FontStyles.Bold;

            ScrollRect list = NewScrollPage("_list_shield", shield, out RectTransform content, 554f, 390f);
            PlaceLaya((RectTransform)list.transform, 0f, 43f, 554f, 440f, 536f, 268f);
            view._list_shield = list;
            ConfigureItemGrid(content);
            // 屏蔽 10 项静态预览(老端 ShowInShieldDic 顺序,跳过微信推送 24;运行时收编刷真值)。
            BakeShieldItems(content, new[] { 1, 2, 3, 10, 14, 20, 22, 25, 5, 26 });
        }

        // ---------------------------------------------------------------- 页签(_box_tab)

        private static void BuildTabBox(RectTransform parent, SettingView view, float pw, float ph)
        {
            RectTransform tab = UiCreatorKit.NewNode("_box_tab", parent);
            PlaceLaya(tab, 8f, 879f, 621f, 57f, pw, ph);
            view._box_tab = tab;

            RectTransform baseTab = UiCreatorKit.NewNode("_box_tab_base_setting", tab);
            PlaceLaya(baseTab, 0f, 0f, 150f, 57f, 621f, 57f);
            view._box_tab_base_setting = baseTab;
            view._img_tab_setting = Img(baseTab, "_img_tab_setting", IMG_TAB_RECT5, 0f, 0f, 150f, 57f, 150f, 57f);
            view._img_tab_setting.raycastTarget = true;
            // 老端 .scene:_img_tab_setting scaleY=-1(底图竖直翻转;快照因负缩放 w/h 不可信,此处走设计值)。
            view._img_tab_setting.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            view._lb_tab_setting = Lbl(baseTab, "_lb_tab_setting", "基本设置", 0f, 5f, 150f, 52f, 22f, Hex("#9b572f"), 150f, 57f);
            view._lb_tab_setting.fontStyle = FontStyles.Bold;

            RectTransform shieldTab = UiCreatorKit.NewNode("_box_tab_shield_setting", tab);
            PlaceLaya(shieldTab, 155f, 0f, 150f, 57f, 621f, 57f);
            view._box_tab_shield_setting = shieldTab;
            view._img_tab_shield = Img(shieldTab, "_img_tab_shield", IMG_TAB_MID1, -1f, 9f, 152f, 52f, 150f, 57f);
            view._img_tab_shield.raycastTarget = true;
            view._lb_tab_shield = Lbl(shieldTab, "_lb_tab_shield", "屏蔽设置", 0f, 5f, 150f, 52f, 22f, Color.white);
            view._lb_tab_shield.fontStyle = FontStyles.Bold;
        }

        // ---------------------------------------------------------------- 主窗模板(__Templates)

        private static void BuildSettingViewTemplates(RectTransform viewRoot, SettingView view)
        {
            RectTransform templates = NewTemplatesWrapper(viewRoot);

            view._tpl_SettingShieldItem = BuildShieldItemTemplate(templates);
            view._tpl_SettingSubscriptionItem = BuildSubscriptionItemTemplate(templates);

            GameObject slider = InstantiateExisting(WithBtnHSliderPrefab, templates);
            if (slider != null) { slider.name = "WithBtnHSlider"; slider.SetActive(false); }
            view._tpl_WithBtnHSlider = slider;

            GameObject head = InstantiateExisting(CustomHeadItemPrefab, templates);
            if (head != null) { head.name = "CustomHeadItem"; head.SetActive(false); }
            view._tpl_CustomHeadItem = head;

            // 降神主界面未移植,无源可依 → 空占位满足 EnsureBound(运行时 HideTemplates 直接隐藏)。
            RectTransform godStub = UiCreatorKit.NewNode("GodBefallMainViewStub", templates);
            UiCreatorKit.Place(godStub, 0f, 0f, 10f, 10f);
            godStub.gameObject.SetActive(false);
            view._tpl_GodBefallMainView = godStub.gameObject;
        }

        /// <summary>对标 SettingShieldItem.scene(250×50):未勾 check_img / 已勾 check_img_1 + 文案。
        /// 模板与烤制预览实例共用(勾选态互斥显隐与运行时 SetData 同规)。</summary>
        private static GameObject BuildShieldItemNode(Transform parent, string nodeName, string label, bool isChecked)
        {
            const float W = 250f, H = 50f;
            RectTransform root = UiCreatorKit.NewNode(nodeName, parent);
            UiCreatorKit.Place(root, 0f, 0f, W, H);
            var item = root.gameObject.AddComponent<SettingShieldItem>();

            item.check_img = Img(root, "check_img", IMG_GX, 0f, 7f, 38f, 38f, W, H);
            item.check_img.raycastTarget = true;
            item.check_img.gameObject.SetActive(!isChecked);
            item.check_img_1 = Img(root, "check_img_1", IMG_GX1, -1f, 4f, 38f, 38f, W, H);
            item.check_img_1.raycastTarget = true;
            item.check_img_1.gameObject.SetActive(isChecked);
            item._lb_text = Lbl(root, "_lb_text", label, 48f, 13f, 200f, 26f, 22f, BrownText, W, H, TextAlignmentOptions.Left);

            return root.gameObject;
        }

        private static GameObject BuildShieldItemTemplate(Transform parent)
        {
            GameObject tpl = BuildShieldItemNode(parent, "SettingShieldItem", "屏蔽项", true); // 模板类名,不改
            tpl.SetActive(false);
            return tpl;
        }

        /// <summary>把列表项烤成静态预览(两列网格由 Content 的 GridLayoutGroup 排布;文案/勾选态读 config_setting.json)。
        /// 运行时 RebuildShieldItems 会按同顺序收编这些实例并刷服务器真值。</summary>
        private static void BakeShieldItems(RectTransform content, int[] subtypes)
        {
            for (int i = 0; i < subtypes.Length; i++)
            {
                (string name, bool on) = SettingRow(subtypes[i]);
                GameObject go = BuildShieldItemNode(content, "SettingShieldItem_" + subtypes[i], name, on);
            }
        }

        /// <summary>对标 SettingSubscriptionItem.scene(565×50):标题头/描述行+订阅钮+已订阅标(微信订阅,未移植先建骨架)。</summary>
        private static GameObject BuildSubscriptionItemTemplate(Transform parent)
        {
            const float W = 565f, H = 50f;
            RectTransform root = UiCreatorKit.NewNode("SettingSubscriptionItem", parent); // 模板类名,不改
            UiCreatorKit.Place(root, 0f, 0f, W, H);
            var item = root.gameObject.AddComponent<SettingSubscriptionItem>();

            RectTransform title = UiCreatorKit.NewNode("_box_title", root);
            PlaceLaya(title, 0f, 11f, 425f, 31f, W, H);
            item._box_title = title;
            item._Image61 = Img(title, "_Image61", IMG_FRIENDS_25, 9f, 0f, 280f, 31f, 425f, 31f);
            item._Label3 = Lbl(title, "_Label3", "屏蔽设置", 39f, 5f, 110f, 24f, 22f, BlockHeader, 425f, 31f, TextAlignmentOptions.Left);
            item._Label3.fontStyle = FontStyles.Bold;

            RectTransform desc = UiCreatorKit.NewNode("_box_desc", root);
            PlaceLaya(desc, 12f, 8f, 553f, 36f, W, H);
            item._box_desc = desc;
            item._Label6 = Lbl(desc, "_Label6", "", 18f, 8f, 450f, 24f, 22f, BrownText, 553f, 36f, TextAlignmentOptions.Left);

            RectTransform btn = UiCreatorKit.NewNode("_btn", desc);
            PlaceLaya(btn, 453f, 1f, 100f, 36f, 553f, 36f);
            btn.gameObject.SetActive(false);
            item._btn = btn;
            item._Image6 = Img(btn, "_Image6", IMG_BTN_RECT12, 0f, 0f, 100f, 36f, 100f, 36f);
            item._Image6.raycastTarget = true;
            item._Label41 = Lbl(btn, "_Label41", "订阅", 0f, 5f, 100f, 26f, 16f, Color.white, 100f, 36f);
            item._Label41.fontStyle = FontStyles.Bold;

            item._Label4 = Lbl(desc, "_Label4", "已订阅", 472f, 8f, 70f, 24f, 22f, Hex("#555454"), 553f, 36f, TextAlignmentOptions.Left);
            item._Label4.gameObject.SetActive(false);

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // =====================================================================================
        // 改头像子窗 SettingChangeHeadView(老端 646×541)
        // =====================================================================================

        private static void BuildChangeHeadView(Transform moduleRoot)
        {
            const float W = 646f, H = 541f;
            RectTransform rt = UiCreatorKit.NewNode("SettingChangeHeadView", moduleRoot);
            UiCreatorKit.Place(rt, 0f, 0f, W, H);
            var view = rt.gameObject.AddComponent<SettingChangeHeadView>();

            Image dim = UiCreatorKit.NewImage("ModalDim", rt);
            UiCreatorKit.Place(dim.rectTransform, 0f, 0f, 2200f, 2600f);
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;

            view._Image1 = Img(rt, "_Image1", IMG_BG03, 0f, 0f, 646f, 507f, W, H);
            view._Image2 = Img(rt, "_Image2", IMG_CONTENT_BG1, 19.5f, 58f, 613f, 345f, W, H);

            RectTransform group1 = UiCreatorKit.NewNode("_Group1", rt);
            PlaceLaya(group1, 53.5f, 8f, 539f, 57f, W, H);
            view._Group1 = group1;
            view._Image3 = Img(group1, "_Image3", IMG_TITLE_BG, 131f, 9f, 270f, 42f, 539f, 57f);
            view._Image4 = Img(group1, "_Image4", IMG_UITC_027A, 212f, 13f, 115f, 31f, 539f, 57f);
            view._Image4.gameObject.SetActive(false);
            view._Image5 = Img(group1, "_Image5", IMG_UISY_017, 434f, 22f, 82f, 13f, 539f, 57f);
            view._Image5.gameObject.SetActive(false);
            TextMeshProUGUI title = Lbl(group1, "TitleLabel", "更换头像", 224f, 13f, 120f, 30f, 22f, TitleOrange, 539f, 57f, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;

            ScrollRect scroll = NewScrollPage("scroll", rt, out RectTransform content, 605f, 331f);
            PlaceLaya((RectTransform)scroll.transform, 27f, 65f, 605f, 331f, W, H);
            view.scroll = scroll;

            RectTransform ok = UiCreatorKit.NewNode("okBtn", rt);
            PlaceLaya(ok, 233.5f, 423f, 177f, 65f, W, H);
            view.okBtn = ok;
            view._Image6 = Img(ok, "_Image6", IMG_RECT_BTN1, 18f, 5.5f, 141f, 49f, 177f, 65f);
            view._Image6.raycastTarget = true;
            view._Label1 = Lbl(ok, "_Label1", "选择并保存", 28f, 18f, 130f, 30f, 24f, Color.white, 177f, 65f, TextAlignmentOptions.Left);

            view._close_btn = Img(rt, "_close_btn", IMG_CLOSE, 615f, -18f, 50f, 57f, W, H);
            view._close_btn.raycastTarget = true;

            RectTransform templates = NewTemplatesWrapper(rt);
            view._tpl_SettingHeadItem = BuildHeadItemTemplate(templates);

            rt.gameObject.SetActive(false);
        }

        /// <summary>对标 SettingHeadItem.scene(143×163):底板+头像框/头像+名牌+选中框+使用中标。</summary>
        private static GameObject BuildHeadItemTemplate(Transform parent)
        {
            const float W = 143f, H = 163f;
            RectTransform root = UiCreatorKit.NewNode("SettingHeadItem", parent); // 模板类名,不改
            UiCreatorKit.Place(root, 0f, 0f, W, H);
            var item = root.gameObject.AddComponent<SettingHeadItem>();

            Image bg = UiCreatorKit.NewImage("_Image1", root);
            UiCreatorKit.Stretch(bg.rectTransform);
            UiCreatorKit.TrySetSprite(bg, IMG_HEAD_BG10, UiCreatorKit.Palette.Panel);
            item._Image1 = bg;

            item._img_take_photo = Img(root, "_img_take_photo", IMG_TAKE_PHOTO, 25.5f, 25.5f, 88f, 88f, W, H);
            item._img_take_photo.gameObject.SetActive(false);

            RectTransform roleHead = UiCreatorKit.NewNode("role_head", root);
            PlaceLaya(roleHead, 9f, 22.5f, 121f, 102f, W, H);
            item.role_head = roleHead;
            item._img_frame = Img(roleHead, "_img_frame", IMG_HEAD_KUANG, 10.5f, 1f, 100f, 100f, 121f, 102f);
            item._img_role_head = Img(roleHead, "_img_role_head", null, 15.5f, 6f, 90f, 90f, 121f, 102f, UiCreatorKit.Palette.BtnSecond);
            item._img_role_head.raycastTarget = true;

            RectTransform nameGroup = UiCreatorKit.NewNode("_Group1", root);
            PlaceLaya(nameGroup, 9f, 127f, 125f, 23f, W, H);
            item._Group1 = nameGroup;
            item._Image2 = Img(nameGroup, "_Image2", IMG_NAME_BG, 0f, 0f, 125f, 23f, 125f, 23f);
            item._lb_name = Lbl(nameGroup, "_lb_name", "", 0f, 0f, 125f, 23f, 16f, Color.white, 125f, 23f);

            item._img_select = Img(root, "_img_select", IMG_SELECT, -6.5f, -4.5f, 156f, 172f, W, H);
            item._img_select.gameObject.SetActive(false);
            item.use_tag = Img(root, "use_tag", IMG_USE_TAG, 46.5f, 2f, 50f, 24f, W, H);
            item.use_tag.gameObject.SetActive(false);

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // =====================================================================================
        // 改名子窗 SettingChangeNameView(老端 560×320)
        // =====================================================================================

        private static void BuildChangeNameView(Transform moduleRoot)
        {
            const float W = 560f, H = 320f;
            RectTransform rt = UiCreatorKit.NewNode("SettingChangeNameView", moduleRoot);
            UiCreatorKit.Place(rt, 0f, 0f, W, H);
            var view = rt.gameObject.AddComponent<SettingChangeNameView>();

            Image dim = UiCreatorKit.NewImage("ModalDim", rt);
            UiCreatorKit.Place(dim.rectTransform, 0f, 0f, 2200f, 2600f);
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;

            view._Image11 = Img(rt, "_Image11", IMG_COMMON_BG03, 0f, 0f, 540f, 319f, W, H);
            Img(rt, "MiddleBg", IMG_CONTENT_BG1, 17f, 59f, 507f, 179f, W, H);

            RectTransform group1 = UiCreatorKit.NewNode("_Group1", rt);
            PlaceLaya(group1, 9.5f, 10f, 501f, 55f, W, H);
            view._Group1 = group1;
            view._Image2 = Img(group1, "_Image2", IMG_TITLE_BG, 135f, 9f, 267f, 42f, 501f, 55f);
            view._Image3 = Img(group1, "_Image3", "resource/game/setting/texture/uildzdz_008.png", 180f, 12f, 140f, 32f, 501f, 55f);
            view._Image3.gameObject.SetActive(false);
            view._Image4 = Img(group1, "_Image4", IMG_UISY_017, 432f, 35f, 82f, 13f, 501f, 55f);
            view._Image4.gameObject.SetActive(false);
            TextMeshProUGUI title = Lbl(group1, "TitleLabel", "角色名修改", 195f, 12f, 140f, 30f, 22f, TitleOrange, 501f, 55f, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;

            // 输入区
            RectTransform group2 = UiCreatorKit.NewNode("_Group2", rt);
            PlaceLaya(group2, 27f, 87f, 440f, 100f, W, H);
            view._Group2 = group2;
            view._img_uixt_011 = Img(group2, "_img_uixt_011", null, 0f, 1f, 2f, 100f, 440f, 100f, new Color(1f, 1f, 1f, 0f));

            RectTransform input = UiCreatorKit.NewNode("input", group2);
            PlaceLaya(input, 115.5f, 11f, 259f, 40f, 440f, 100f);
            view.input = input;
            view._Image1 = Img(input, "_Image1", IMG_NUM_BG, 1f, -1f, 251f, 44f, 259f, 40f);
            view._Image1.gameObject.SetActive(false);
            RectTransform rect1 = UiCreatorKit.NewNode("_Rect1", input);
            PlaceLaya(rect1, -9.5f, 1f, 300f, 48f, 259f, 40f);
            rect1.gameObject.SetActive(false);
            view._Rect1 = rect1;

            TMP_InputField nameInput = UiCreatorKit.NewInput("InptextDisplay", input, "请输入新的角色名字", false);
            PlaceLaya((RectTransform)nameInput.transform, 1f, -2f, 252f, 44f, 259f, 40f);
            UiCreatorKit.TrySetSprite(nameInput.GetComponent<Image>(), IMG_NUM_BG, UiCreatorKit.Palette.Input);
            view.InptextDisplay = nameInput;

            // 老端遗留的两个隐藏显示标签(Text/default),建出满足 Bind、保持隐藏。
            TextMeshProUGUI legacyText = Lbl(input, "LegacyText", "", 10f, 0f, 240f, 40f, 20f, Hex("#4a3a32"), 259f, 40f);
            legacyText.gameObject.SetActive(false);
            view.Text = legacyText;
            TextMeshProUGUI legacyDefault = Lbl(input, "LegacyDefault", "请输入新的角色名字", 10f, 0f, 240f, 40f, 20f, Hex("#956851"), 259f, 40f);
            legacyDefault.gameObject.SetActive(false);
            view.@default = legacyDefault;

            // 首次免费/消耗行
            view.free_label = Lbl(rt, "free_label", "首次改名免费", 197f, 174f, 170f, 30f, 24f, BrownText, W, H, TextAlignmentOptions.Left);
            RectTransform cost = UiCreatorKit.NewNode("cost_conta", rt);
            PlaceLaya(cost, 54f, 159.5f, 474f, 53f, W, H);
            cost.gameObject.SetActive(false); // 老端由消耗配置驱动显隐,配置未移植 → 先隐藏,免费文案顶上
            view.cost_conta = cost;
            view._lb_cost = Lbl(cost, "_lb_cost", "消耗:", 128f, 15f, 70f, 26f, 22f, BrownText, 474f, 53f, TextAlignmentOptions.Left);
            RectTransform icon1 = UiCreatorKit.NewNode("icon1", cost);
            PlaceLaya(icon1, 157f, 7f, 40f, 40f, 474f, 53f);
            view.icon1 = icon1;
            view.cost1 = Lbl(cost, "cost1", "", 194f, 13f, 100f, 26f, 22f, BrownText, 474f, 53f, TextAlignmentOptions.Left);
            view.extra = Lbl(cost, "extra", "或", 232f, 15f, 30f, 26f, 22f, BrownText, 474f, 53f, TextAlignmentOptions.Left);
            RectTransform icon2 = UiCreatorKit.NewNode("icon2", cost);
            PlaceLaya(icon2, 248f, 7f, 40f, 40f, 474f, 53f);
            view.icon2 = icon2;
            view.cost2 = Lbl(cost, "cost2", "", 292f, 13f, 100f, 26f, 22f, BrownText, 474f, 53f, TextAlignmentOptions.Left);

            // 取消/确定
            RectTransform group3 = UiCreatorKit.NewNode("_Group3", rt);
            PlaceLaya(group3, 86.3f, 253.5f, 491f, 67f, W, H);
            view._Group3 = group3;

            RectTransform cancel = UiCreatorKit.NewNode("cancleBtn", group3);
            PlaceLaya(cancel, 0f, 1f, 142f, 49f, 491f, 67f);
            view.cancleBtn = cancel;
            view._Image5 = Img(cancel, "_Image5", IMG_RECT_BTN2, 0f, 0f, 142f, 49f, 142f, 49f);
            view._Image5.raycastTarget = true;
            view._Label1 = Lbl(cancel, "_Label1", "取消", 0f, 10f, 142f, 30f, 24f, Color.white, 142f, 49f);

            RectTransform confirm = UiCreatorKit.NewNode("confirmBtn", group3);
            PlaceLaya(confirm, 221f, 1f, 141f, 48f, 491f, 67f);
            view.confirmBtn = confirm;
            view._Image6 = Img(confirm, "_Image6", IMG_RECT_BTN1, 0f, 1f, 141f, 48f, 141f, 48f);
            view._Image6.raycastTarget = true;
            view._Label2 = Lbl(confirm, "_Label2", "确定", 0f, 10f, 141f, 30f, 24f, Color.white, 141f, 48f);

            view._close_btn = Img(rt, "_close_btn", IMG_CLOSE, 509f, -15f, 52f, 59f, W, H);
            view._close_btn.raycastTarget = true;

            // 改名奖励项未移植,无源可依 → 空占位满足 EnsureBound。
            RectTransform templates = NewTemplatesWrapper(rt);
            RectTransform awardStub = UiCreatorKit.NewNode("BaseAwardItemStub", templates);
            UiCreatorKit.Place(awardStub, 0f, 0f, 10f, 10f);
            awardStub.gameObject.SetActive(false);
            view._tpl_BaseAwardItem = awardStub.gameObject;

            rt.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- 布局/元素 helper(本文件私有)

        /// <summary>Laya 左上原点 → Unity 中心锚(同 HudTopCreator.PlaceLaya)。</summary>
        private static void PlaceLaya(RectTransform rt, float x, float y, float w, float h,
            float parentW, float parentH, float anchorX = 0f, float anchorY = 0f)
        {
            float centerTopLeftX = x + (0.5f - anchorX) * w;
            float centerTopLeftY = y + (0.5f - anchorY) * h;
            float cx = centerTopLeftX - parentW / 2f;
            float cy = -(centerTopLeftY - parentH / 2f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        private static void ConfigureVerticalFlow(RectTransform content)
        {
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 10, 0);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>纵向流的一行；Block 的横向偏移/尺寸仍由 Creator 固化，运行时只显隐整行。</summary>
        private static RectTransform FlowBlock(string name, Transform content, float x, float w, float h)
        {
            RectTransform row = UiCreatorKit.NewNode(name + "_Row", content);
            row.sizeDelta = new Vector2(643f, h);
            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = h;
            element.preferredHeight = h;
            element.flexibleHeight = 0f;

            RectTransform block = UiCreatorKit.NewNode(name, row);
            block.anchorMin = block.anchorMax = new Vector2(0f, 1f);
            block.pivot = new Vector2(0f, 1f);
            block.sizeDelta = new Vector2(w, h);
            block.anchoredPosition = new Vector2(x, 0f);
            return block;
        }

        private static void ConfigureItemGrid(RectTransform content)
        {
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset();
            grid.cellSize = new Vector2(250f, 50f);
            grid.spacing = Vector2.zero;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>快照几何 Image:矩形经 SnapRect(节点名, snapParent)取运行时结算值,快照缺失回退设计值。</summary>
        private static Image SImg(Transform parent, string name, string skin, string snapParent,
            float fx, float fy, float fw, float fh, float parentW, float parentH, Color? fallback = null)
        {
            Rect r = SnapRect(name, snapParent, fx, fy, fw, fh);
            return Img(parent, name, skin, r.x, r.y, r.width, r.height, parentW, parentH, fallback);
        }

        /// <summary>快照几何 TMP 文本(左对齐;宽度给 +60 余量防截断——快照 w 是紧贴文本的结算宽)。</summary>
        private static TextMeshProUGUI SLbl(Transform parent, string name, string text, string snapParent,
            float fx, float fy, float fw, float fh, float fontSize, Color color, float parentW, float parentH,
            TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            Rect r = SnapRect(name, snapParent, fx, fy, fw, fh);
            return Lbl(parent, name, text, r.x, r.y, r.width + 60f, r.height, fontSize, color, parentW, parentH, align);
        }

        /// <summary>快照几何容器节点。</summary>
        private static RectTransform SNode(Transform parent, string name, string snapParent,
            float fx, float fy, float fw, float fh, float parentW, float parentH, out Rect r)
        {
            r = SnapRect(name, snapParent, fx, fy, fw, fh);
            RectTransform rt = UiCreatorKit.NewNode(name, parent);
            PlaceLaya(rt, r.x, r.y, r.width, r.height, parentW, parentH);
            return rt;
        }

        /// <summary>Laya 坐标摆位的 Image(skin=null 时用 fallback 纯色;默认不挡点击)。</summary>
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

        /// <summary>Laya 坐标摆位的 TMP 文本。</summary>
        private static TextMeshProUGUI Lbl(Transform parent, string name, string text,
            float x, float y, float w, float h, float fontSize, Color color,
            float parentW = 0f, float parentH = 0f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI t = UiCreatorKit.NewText(name, parent, text);
            if (parentW > 0f) PlaceLaya(t.rectTransform, x, y, w, h, parentW, parentH);
            else UiCreatorKit.Place(t.rectTransform, x, y, w, h);
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            return t;
        }

        /// <summary>
        /// ScrollRect(Viewport+RectMask2D+Content) 基架；具体列表/页面的布局组件由调用方在 Creator 中配置。
        /// Content 左上锚(0,1)/枢轴(0,1),尺寸给起步值。
        /// </summary>
        private static ScrollRect NewScrollPage(string name, Transform parent, out RectTransform content,
            float contentW, float contentH)
        {
            RectTransform root = UiCreatorKit.NewNode(name, parent);
            ScrollRect sr = root.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = UiCreatorKit.NewNode("Viewport", root);
            UiCreatorKit.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = UiCreatorKit.NewNode("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(contentW, contentH);

            sr.viewport = viewport;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 30f;
            return sr;
        }

        /// <summary>隐藏模板容器(对标 HudOverlayPopupsCreator.NewTemplatesWrapper 同名约定,复制一份不跨文件引用)。</summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>嵌套实例化已存在的 Common prefab(WithBtnHSlider/CustomHeadItem,单一事实源不重建)。</summary>
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
                EditorUtility.DisplayDialog("预览 SettingModule",
                    "请先进入 Play 模式(UI 层已初始化)再点预览。\n\n" +
                    "预览复刻 SettingFlow.OpenAsync:实例化最新 prefab 到 Window 层,隐藏全部直接子节点后只 Show 主窗 SettingView。",
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
            foreach (Transform c in _previewInstance.transform)
            {
                c.gameObject.SetActive(false);
            }
            var main = _previewInstance.GetComponentInChildren<SettingView>(true);
            if (main == null)
            {
                Debug.LogError("[UiCreator] 预览失败,prefab 缺 SettingView 组件");
                return;
            }
            main.Show();
            Selection.activeObject = _previewInstance;
        }
    }
}
