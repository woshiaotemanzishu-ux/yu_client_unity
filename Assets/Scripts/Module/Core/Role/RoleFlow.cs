using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 角色模块编排:多标签窗(对标老端 RoleView extends BaseWindowComponent;主界面 MainFunc.Role → OPEN_CHOSE_ROLE_VIEW)。
    /// **第三个走 BaseWindowSkinView 地基的分页大窗**(模板复制自 EquipFlow/DailyFlow,仅改 4 常量 + 内容源)。
    ///
    /// 打开 = 实例化共享窗框 BaseWindowSkin + 实例化 RoleModule(内容源,先全隐藏)→ Configure(N 标签):点标签把对应内容视图
    /// reparent 进窗框内容区 _gp_item_con(懒加载缓存)。老端 RoleView.tabStrList=[人物/垂神翼影/古法符相/殒锋天刃/玄穹云披]
    /// (仅 tab0 EquipmentView 已移植开放,余 4 标签 disabled——Wings/Artifact/HolyDevice/BackOrnament 未移植,写好置 true 即开)。
    /// 技能成长线轮3 追加"主动技能/被动技能"两档(对标老端 SkillSubView,实为独立二级菜单窗口而非 RoleView 自身标签,
    /// 但 Unity 转换期已把其内容节点扁平并入同一份 RoleModule.prefab,沿用本处 Tab 机制接入;天赋 InnateSkillView 留 3b)。
    /// 入口注册见 <see cref="RoleBootstrap"/>(MainUIRouter "role");子窗(技能详情 SkillShowView…)经 <see cref="OpenSub"/>。再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class RoleFlow
    {
        private const string CONTENT_MODULE = "role";
        private const string CONTENT_PREFAB = "RoleModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";

        // 老端 RoleView.viewClassList(标签索引 → 内容视图类名)
        private static readonly string[] TabContent =
        {
            "EquipmentView", "WingsComponentView", "ArtifactComponentView",
            "HolyDeviceComponentView", "BackOrnamentComponentView",
            "SkillInitiativeSubItem", "SkillPassiveItem", "InnateSkillView",
        };
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
        // 该标签内容视图是否已在 Unity 写好(写好才开放;其余 disabled,写完置 true 即开)
        private static readonly bool[] TabEnabled = { true, false, false, false, false, true, true, true };
        // 天赋(index7)按钮恒可见,但需 4 转开启(对标老端 tab_new_cond[2]/SkillUIModel.GetInnateOpenStatus:
        // turn>=innate_open_turn_cond=4);点击未达标 → toast「【4转开启】」+ 还原选择,不真的切换(见 OpenAsync)。
        private const int InnateTalentTabIndex = 7;
        private const int InnateTalentOpenTurn = 4;
        private const int DefaultTab = 0;

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static bool _loading;

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

            var specs = new List<TabSpec>(TabContent.Length);
            for (int i = 0; i < TabContent.Length; i++)
            {
                string viewName = TabContent[i];
                bool enabled = TabEnabled[i];
                bool isInnateTab = i == InnateTalentTabIndex;
                specs.Add(new TabSpec
                {
                    Enabled = enabled,
                    Label = TabLabels[i],
                    TitleImagePath = TabTitles[i],
                    ContentFactory = enabled ? (Func<RectTransform, BaseView>)(parent => ReparentContent(viewName, parent)) : null,
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

        internal static void Reset()
        {
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _loading = false;
        }
    }
}
