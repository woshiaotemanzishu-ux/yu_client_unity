using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Pet;
using UnityEngine;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 角色模块编排:多标签窗(对标老端 RoleView extends BaseWindowComponent;主界面 MainFunc.Role → OPEN_CHOSE_ROLE_VIEW)。
    /// **第三个走 BaseWindowSkinView 地基的分页大窗**(模板复制自 EquipFlow/DailyFlow,仅改 4 常量 + 内容源)。
    ///
    /// 打开 = 实例化共享窗框 BaseWindowSkin + 实例化 RoleModule(内容源,先全隐藏)→ Configure(N 标签):点标签把对应内容视图
    /// reparent 进窗框内容区 _gp_item_con(懒加载缓存)。老端 RoleView.tabStrList=[人物/垂神翼影/古法符相/殒锋天刃/玄穹云披]。
    /// 技能成长线轮3 追加"主动技能/被动技能"两档(对标老端 SkillSubView,实为独立二级菜单窗口而非 RoleView 自身标签,
    /// 但 Unity 转换期已把其内容节点扁平并入同一份 RoleModule.prefab,沿用本处 Tab 机制接入;天赋 InnateSkillView 留 3b)。
    ///
    /// 第21轮补齐:Wings/Artifact/HolyDevice/BackOrnament(type_id 3/4/5/12)**不烤新 prefab**,复用
    /// PetModule.prefab 里已有的 <see cref="OutWardBaseView"/>(老端本就是同一基类 pet/OutWardBaseView.ts,6 个
    /// XxxComponentView 子类只换 _type——Unity 侧对等:同一份 OutWardBaseView 类,4 个独立实例各自 SetType 一次,
    /// 不做跨窗口单例共享,见 <see cref="PreloadOutwardTabsAsync"/> 注释)。
    /// 入口注册见 <see cref="RoleBootstrap"/>(MainUIRouter "role");子窗(技能详情 SkillShowView…)经 <see cref="OpenSub"/>。再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class RoleFlow
    {
        private const string CONTENT_MODULE = "role";
        private const string CONTENT_PREFAB = "RoleModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";
        private const string OUTWARD_MODULE = "pet";
        private const string OUTWARD_PREFAB = "PetModule";

        // 老端 RoleView.viewClassList(标签索引 → 内容视图类名;index1-4 现由共用的 OutWardBaseView 承载,见 TabOutwardTypeId)
        private static readonly string[] TabContent =
        {
            "EquipmentView", "WingsComponentView", "ArtifactComponentView",
            "HolyDeviceComponentView", "BackOrnamentComponentView",
            "SkillInitiativeSubItem", "SkillPassiveItem", "InnateSkillView",
        };
        // 标签索引 → OutWard type_id(0=非 OutWard 页签;1=翼影 type_id=3,2=古法符相 type_id=4,3=殒锋天刃 type_id=5,
        // 4=玄穹云披 type_id=12,对标 mount.hrl FLY_ID/ARTIFACT_ID/HOLYORGAN_ID/NEW_BACK_DECORATION)。第21轮补齐。
        private static readonly int[] TabOutwardTypeId = { 0, 3, 4, 5, 12, 0, 0, 0 };
        private static readonly string[] TabTitles =
        {
            GameResPath.GetIcon("role", "title_name"),
            GameResPath.GetIcon("pet", "ui_yuyi"),
            GameResPath.GetIcon("pet", "ui_yushou"),
            GameResPath.GetIcon("pet", "ui_shenbin"),
            GameResPath.GetIcon("pet", "ui_beishi"),
            null, null, null,
        };
        private static readonly string[] TabLabels =
        {
            "\u4EBA\u7269", string.Empty, string.Empty, string.Empty, string.Empty,
            "\u4E3B\u52A8\u6280\u80FD", "\u88AB\u52A8\u6280\u80FD", "\u5929\u8D4B",
        };
        // 该标签内容视图是否已在 Unity 写好(写好才开放)。第21轮:Wings/Artifact/HolyDevice/BackOrnament 接上
        // 共用 OutWardBaseView,四个标签一并转正。
        private static readonly bool[] TabEnabled = { true, true, true, true, true, true, true, true };
        // 天赋(index7)按钮恒可见,但需 4 转开启(对标老端 tab_new_cond[2]/SkillUIModel.GetInnateOpenStatus:
        // turn>=innate_open_turn_cond=4);点击未达标 → toast「【4转开启】」+ 还原选择,不真的切换(见 OpenAsync)。
        private const int InnateTalentTabIndex = 7;
        private const int InnateTalentOpenTurn = 4;
        private const int DefaultTab = 0;

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static bool _loading;
        // 第21轮:index1-4(翼影/古法符相/殒锋天刃/玄穹云披)各自独立的 PetModule.prefab 克隆根(用于 ResManager.ReleaseInstance)
        // + 从中抠出来的 OutWardBaseView(用于 ContentFactory 同步 reparent)。见 PreloadOutwardTabsAsync。
        private static readonly Dictionary<int, GameObject> _outwardRoots = new Dictionary<int, GameObject>();
        private static readonly Dictionary<int, OutWardBaseView> _outwardViews = new Dictionary<int, OutWardBaseView>();

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_window != null) _window.Hide();
        }

        /// <summary>打开角色模块内子窗(技能详情 SkillShowView…),按 View 子类名在内容源里查找并 Show。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_contentRoot == null)
            {
                GameLog.Warn("Role", "OpenSub({0}) 时角色模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Role", "角色子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        private static async Task OpenAsync()
        {
            if (_frameRoot != null)
            {
                if (_window != null) _window.Show();
                return;
            }

            if (_loading) return;
            _loading = true;
            string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
            string contentKey = GameResPath.GetUIPrefab(CONTENT_MODULE, CONTENT_PREFAB);
            _frameRoot = await ResManager.InstantiateAsync(frameKey, ViewManager.GetLayer(UILayer.Window));
            _contentRoot = await ResManager.InstantiateAsync(contentKey, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (_frameRoot == null || _contentRoot == null)
            {
                GameLog.Error("Role", "五标签窗加载失败 frame={0} content={1}", frameKey, contentKey);
                return;
            }
            _frameRoot.name = FRAME_PREFAB;
            _contentRoot.name = CONTENT_PREFAB;

            foreach (Transform c in _contentRoot.transform) c.gameObject.SetActive(false);

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("Role", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
                return;
            }

            // TabSpec.ContentFactory 是同步 Func<RectTransform,BaseView>,OutWard 内容要预先异步加载好才能同步 reparent。
            await PreloadOutwardTabsAsync();

            var specs = new List<TabSpec>(TabContent.Length);
            for (int i = 0; i < TabContent.Length; i++)
            {
                string viewName = TabContent[i];
                bool enabled = TabEnabled[i];
                bool isInnateTab = i == InnateTalentTabIndex;
                int tabIndex = i;
                int outwardTypeId = TabOutwardTypeId[i];
                Func<RectTransform, BaseView> factory = null;
                if (enabled)
                {
                    factory = outwardTypeId > 0
                        ? (Func<RectTransform, BaseView>)(parent => ReparentOutwardTab(tabIndex, parent))
                        : (parent => ReparentContent(viewName, parent));
                }
                specs.Add(new TabSpec
                {
                    Enabled = enabled,
                    Label = TabLabels[i],
                    TitleImagePath = TabTitles[i],
                    ContentFactory = factory,
                    OpenCheck = isInnateTab ? (Func<bool>)(() => (RoleModel.Instance.Figure?.turn ?? 0) >= InnateTalentOpenTurn) : null,
                    LockedToast = isInnateTab ? "【" + InnateTalentOpenTurn + "转开启】" : null, // 【4转开启】
                });
            }

            _window.Show();
            _window.Configure(specs, DefaultTab);
            GameLog.Info("Role", "角色五标签窗打开(BaseWindowSkinView,默认 tab{0} 人物)", DefaultTab);
        }

        /// <summary>把内容源里名为 viewName 的内容视图 reparent 进窗框内容区(保留其原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentContent(string viewName, RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform t = _contentRoot.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("Role", "内容视图 [{0}] 不在 RoleModule 顶层", viewName);
                return null;
            }
            t.SetParent(parent, false);
            t.gameObject.SetActive(true);
            return t.GetComponent<BaseView>();
        }

        /// <summary>
        /// 为 index1-4(翼影/古法符相/殒锋天刃/玄穹云披)各自实例化一份 PetModule.prefab 克隆(不烤新 prefab,
        /// 只是再实例化一次已有的),抠出其 <see cref="OutWardBaseView"/> 并当场 SetType 定型(每份克隆终生固定一个
        /// type_id,不在多个标签间搬运——4 个独立实例,不是 PetFlow 那种"1 个实例切 2 个 type_id 的共享"模式)。
        /// 之所以不复用 PetFlow 的活实例:BaseWindowSkinView.SelectTab 按标签索引各自缓存 BaseView,若 4 个索引
        /// 指向同一个实例,索引相关的 Show()/Hide() 顺序会互相覆盖(先选的标签会被后处理的 Hide() 盖掉)——
        /// 分开 4 份实例可用现有框架代码正确工作,不需要改 BaseWindowSkinView.cs。
        /// 必须在 Configure() 建标签之前 await 完成,因为 TabSpec.ContentFactory 契约是同步的。
        /// </summary>
        private static async Task PreloadOutwardTabsAsync()
        {
            string outwardKey = GameResPath.GetUIPrefab(OUTWARD_MODULE, OUTWARD_PREFAB);
            for (int i = 0; i < TabOutwardTypeId.Length; i++)
            {
                int typeId = TabOutwardTypeId[i];
                if (typeId <= 0 || !TabEnabled[i]) continue;
                if (_outwardViews.ContainsKey(i)) continue;   // 重开窗(未 Reset)时已加载过

                GameObject root = await ResManager.InstantiateAsync(outwardKey, ViewManager.GetLayer(UILayer.Window));
                if (root == null)
                {
                    GameLog.Warn("Role", "角色页签[{0}] type_id={1} 加载 OutWardBaseView 失败({2})", TabContent[i], typeId, outwardKey);
                    continue;
                }
                root.name = "OutWard_" + TabContent[i];
                root.SetActive(false);   // 挂在 Window 层但先隐藏,选中该页签时 ReparentOutwardTab 才显示

                OutWardBaseView view = root.GetComponentInChildren<OutWardBaseView>(true);
                if (view == null)
                {
                    GameLog.Warn("Role", "PetModule.prefab 缺 OutWardBaseView(重跑 PetCreator 生成)");
                    ResManager.ReleaseInstance(root);
                    continue;
                }

                view.SetType(typeId);   // 定型 + 对标老端"打开页时补拉一次"(16002/16028 首次预取)
                _outwardRoots[i] = root;
                _outwardViews[i] = view;
            }
        }

        /// <summary>把预加载好的 OutWardBaseView(<see cref="_outwardViews"/>)reparent 进窗框内容区并显示。
        /// 同步执行(真正的加载已在 <see cref="PreloadOutwardTabsAsync"/> 完成)。</summary>
        private static BaseView ReparentOutwardTab(int tabIndex, RectTransform parent)
        {
            if (!_outwardViews.TryGetValue(tabIndex, out OutWardBaseView view) || view == null)
            {
                GameLog.Warn("Role", "角色页签[{0}] OutWard 视图未就绪(加载失败,见上方日志)", tabIndex);
                return null;
            }
            view.transform.SetParent(parent, false);
            view.gameObject.SetActive(true);
            return view;
        }

        internal static void Reset()
        {
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            foreach (GameObject root in _outwardRoots.Values)
            {
                if (root != null) ResManager.ReleaseInstance(root);
            }
            _outwardRoots.Clear();
            _outwardViews.Clear();
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _loading = false;
        }
    }
}
