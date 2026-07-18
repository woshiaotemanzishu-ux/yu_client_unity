using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面「通知位」图标区纯代码建树生成器 —— 所见即所得版。
    ///
    /// 这是什么:老端 _box_notice,configfunctionicon 里 location_type=6 的「限时活动倒计时入口」——
    /// 活动日历/跨服玩法(九魂圣殿/结社战/巅峰竞技/跨服1vN/婚礼…共 29 条配置)开启前后在此显示带倒计时
    /// 文本的入口图标;服务器推送的运营活动图标(如开服神鹤)也落这里。**平时这里是空的**
    /// (满解锁号运行时快照 _box_notice children=0),只在活动开启时出现,常年同屏 ≤2 个。
    ///
    /// 这一簇原本混在 HudSecondary(MainUISecondaryView)里,位置全靠老端 RefreshIconPos 在代码里算;
    /// 现拆成独立区域。几何以运行时快照为准(铁律:.scene 设计值会跑偏):_box_notice 原点
    /// globalBounds=(705,475)、right:15,老端图标挂原点【上方】(x=-72,y=-72) → 首图标实际占
    /// (633..705, 403..475)。
    ///
    /// 设计原则(用户定):布局归 prefab、代码不碰布局 ——【槽位式】(同 HudActivity 已验收模式):
    /// NoticeSlots 下由**美术手摆空槽位**,运行时 MainUINoticeView 清样例、把 loc6 图标按顺序填进各槽,
    /// 代码绝不算坐标。想调布局:直接在 prefab 的 NoticeSlots 下拖 Slot_* 节点。
    ///
    ///   HudNotice(root)             —— 有界:右上锚,右缘内缩 15、顶 403(= 快照首图标位),149×72
    ///     MainUINoticeView(view)     —— Stretch 填满 root,挂业务类
    ///       __Templates/ActivityIcon —— 图标克隆模板(嵌套引用 ActivityIcon.prefab,建完禁用)
    ///       NoticeSlots(_box_notice) —— 槽位容器:2 个 72×72 空槽,从右往左排(老端向左生长习惯);
    ///                                   仅 Slot_0 带样例图(真 loc6 图标 621 跨服1vN),免得编辑器里
    ///                                   看起来像常驻的一排活动图标
    ///
    /// 存 Assets/Prefabs/UI/MainUI/Regions/HudNotice.prefab,供人工核对后再并入 MainUIModule.prefab。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   _box_notice -> NoticeSlots(槽位容器)
    public static class HudNoticeCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudNotice.prefab";

        // 图标克隆模板:直接嵌套引用现成的 ActivityIcon.prefab(改模板去改该 prefab,本区自动跟随)。
        private const string TPL_ACTIVITY_ICON = "Assets/Prefabs/UI/MainUI/ActivityIcon.prefab";
        // 槽位样例图(仅设计期可视,运行时被 MainUINoticeView 清掉):用真 loc6 图标(621 跨服1vN),
        // 别用活动区图标贴图冒充(151_1 那种会让人误以为是 HudActivity 的图标)。
        private const string IMG_SAMPLE_ICON = "resource/game/icon/texture/621.png";

        // ---- 布局起步值(全部落进 prefab、供预制体里手调;建完代码不再有布局计算)----
        private const float SlotW = 72f, SlotH = 72f;   // ActivityIcon.WIDTH/HEIGHT
        private const float SlotGap = 5f;               // 槽间距
        private const int SlotCount = 2;                // 同屏常年 ≤2 个(超出运行时告警,去 prefab 加槽)

        // 区域外框:右上锚。快照 _box_notice 原点 globalBounds=(705,475)、right:15;老端图标挂原点上方
        // (y=-72) → 首图标顶 = 475-72 = 403。(.scene 里 y:-410 是跑偏的设计值,以快照为准。)
        private const float RegionRightInset = 15f, RegionTop = 403f;
        // 垂直方向锚【屏幕底边】而非顶边:老端 _box_notice 是 MainUISecondaryView 的子节点,
        // 而 SecondaryView 是 left=0/right=0/bottom=290 的锚底 view —— 整簇随屏幕底边走。
        // 本区域拆成独立 prefab 时误用了右上锚(随顶边走),屏幕一变高就与老端分叉。
        // 底边距屏幕底 = 1280 - 403 - 72 = 805(RegionTop/RegionH 均取自运行时快照)。
        // 注:按 .scene 设计值(y:-410)推算只得 700,差的 105 是 MainUISecondaryView 自身高度;
        // 依「快照优先、.scene 设计值会跑偏」铁律,以 805 为准。
        private const float DesignHeight = 1280f;
        private const float RegionBottomUp = DesignHeight - RegionTop - RegionH;
        private const float RegionW = SlotCount * SlotW + (SlotCount - 1) * SlotGap; // 226
        private const float RegionH = SlotH;

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudNotice(通知位)",
                Note = "竞榜卡下方通知图标簇(老端 _box_notice),槽位式有界区,布局全在 prefab 可拖",
                Order = 26,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再统一激活(与 Login 系列 Creator 一致的安全写法)。
            RectTransform root = UiCreatorKit.NewRoot("HudNotice");
            AnchorBottomRight(root, RegionRightInset, RegionBottomUp, RegionW, RegionH);
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("MainUINoticeView", root);
            UiCreatorKit.Stretch(viewRoot); // 填满有界 root
            var view = viewRoot.gameObject.AddComponent<MainUINoticeView>();

            // 隐藏模板挂载点:图标模板是纯克隆源,不应出现在可见容器里。
            RectTransform templates = NewTemplatesWrapper(viewRoot);
            view._tpl_ActivityIcon = BuildActivityIconTemplate(templates);

            // ================= NoticeSlots(老端 _box_notice):【槽位容器】,子节点=空槽位,运行时按顺序填图标 =================
            RectTransform slots = UiCreatorKit.NewNode("NoticeSlots", viewRoot); // 老端: _box_notice
            UiCreatorKit.Stretch(slots); // 填满 view(槽位相对它右上定位)
            view._box_notice = slots;

            // 槽从右往左排(对齐老端向左生长的习惯):Slot_0 贴右缘,依次左移。位置全在 prefab,随便拖/加/删。
            for (int i = 0; i < SlotCount; i++)
                BuildSlot(slots, i, i * (SlotW + SlotGap));

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudNotice.prefab 已生成: " + PrefabPath +
                      "(槽位式:NoticeSlots 下 " + SlotCount + " 个空槽,位置全在 prefab 可拖;人工核对后再并入 MainUIModule.prefab)");
        }

        /// <summary>建一个空槽位:右上锚定的 72×72 RectTransform。只给 Slot_0 放样例图(仅设计期可视,
        /// 运行时被清掉)——平时该区就是空的,一排样例会造成"这里常驻一堆图标"的错觉。</summary>
        private static void BuildSlot(RectTransform parent, int index, float rightInset)
        {
            RectTransform slot = UiCreatorKit.NewNode("Slot_" + index, parent);
            AnchorTopRight(slot, rightInset, 0f, SlotW, SlotH);
            if (index != 0) return;
            Image sample = UiCreatorKit.NewImage("Sample", slot);
            UiCreatorKit.Stretch(sample.rectTransform);
            UiCreatorKit.TrySetSprite(sample, IMG_SAMPLE_ICON, UiCreatorKit.Palette.BtnNeutral);
        }

        /// <summary>
        /// 图标克隆模板:嵌套实例化现成的 ActivityIcon.prefab(保持 prefab 连接,改 ActivityIcon.prefab 自动跟随),
        /// 建完禁用,只作克隆源。克隆体位置由所在槽决定(MainUINoticeView.PlaceIconInSlot 居中),模板自身锚点无关紧要。
        /// </summary>
        private static GameObject BuildActivityIconTemplate(Transform parent)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(TPL_ACTIVITY_ICON);
            if (asset == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + TPL_ACTIVITY_ICON + ",HudNotice 模板缺失(先生成/检查 ActivityIcon.prefab)");
                return null;
            }
            var tpl = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            tpl.SetActive(false);
            return tpl;
        }

        /// <summary>建一个隐藏的模板挂载容器(__Templates):专收纯克隆源,不让它们裸露在可见容器里。</summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>右上锚定:锚点/轴心取父右上角,rightInset=距右缘、top=距顶。
        /// 只给【容器内部】的槽位用(NoticeSlots 高度固定 = 槽高,垂直方向填满,用顶锚或底锚等价);
        /// 区域根请用 AnchorBottomRight —— 它要跟随屏幕底边。</summary>
        private static void AnchorTopRight(RectTransform rt, float rightInset, float top, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-rightInset, -top);
        }

        /// <summary>右下锚定:锚点/轴心取父右下角,rightInset=距右缘、bottomUp=距底边。
        /// 区域根用它跟随屏幕底边(老端 _box_notice 挂在锚底的 MainUISecondaryView 下)。</summary>
        private static void AnchorBottomRight(RectTransform rt, float rightInset, float bottomUp, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-rightInset, bottomUp);
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudNotice",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "HudNotice 是并入 MainUIModule 的区域子视图,不走 ViewManager.Open<T>();" +
                    "预览直接把最新 prefab 实例化到 Window 层并手动调用 view.Show(),仅用于看结构。",
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

            Transform parent = ViewManager.GetLayer(UILayer.Window);
            _previewInstance = Object.Instantiate(prefab, parent);
            var view = _previewInstance.GetComponentInChildren<MainUINoticeView>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] HudNotice 预览实例缺少 MainUINoticeView 组件");
                return;
            }
            view.Show();
        }
    }
}
