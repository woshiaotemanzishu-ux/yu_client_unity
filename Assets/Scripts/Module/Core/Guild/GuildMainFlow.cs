using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Guild.Views;
using UnityEngine;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// 公会主界面编排(自动循环 轮13a;对标老端 GuildMainBaseView extends BaseWindowComponent,套用
    /// BaseWindowSkinView 共享地基,同 DailyFlow/ShopFlow 范式)。老端真实 tabStrList=[信息|成员|排行|宝箱]
    /// (开服天数超窗口期后"排行"换成"其他"=GuildJoinView),**不存在独立"申请"页签**——申请列表是从
    /// 成员页"查看申请"按钮触发的弹层(<see cref="GuildApplyLookView"/>),与规格草案字面"申请页"描述不同,
    /// 以老端真实结构 GuildMainBaseView.ts/GuildMemberView.ts 为准(偏差记入工单 summary)。
    ///
    /// 本轮:信息(GuildMainView)/成员(GuildMemberView)真接线;排行(HolyTerritoryGuildView,圣域模块跨包)/
    /// 宝箱(GuildRewardBoxView,13b 仓库宝箱包)留 TODO(TabSpec.Enabled=false,不建按钮)。
    /// </summary>
    public static class GuildMainFlow
    {
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";
        private const string CONTENT_MODULE = "guild";
        private const string CONTENT_PREFAB = "GuildModule";

        private const int TAB_INFO = 0;
        private const int TAB_MEMBER = 1;

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static GuildMainView _mainView;
        private static GuildMemberView _memberView;
        private static GuildApplyLookView _applyView;
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
            if (_applyView != null && _applyView.IsShown) _applyView.Hide(); // 弹层脱管补救,随主窗关闭一并收拢
        }

        private static async Task OpenAsync()
        {
            if (_frameRoot != null)
            {
                if (_applyView != null && _applyView.IsShown) _applyView.Hide(); // 再开时收拢上次残留的弹层
                if (_window != null) { _window.Show(); _window.SelectTab(TAB_INFO); }
                GuildController.Instance.RequestBaseInfo();
                return;
            }

            if (_loading) return;
            _loading = true;

            await GuildConfigs.EnsureLoaded();
            await Shenxiao.Module.Core.Shop.ShopConfigs.EnsureLoaded(); // config_guild_prestige 头衔文案(成员页用)

            string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
            _frameRoot = await ResManager.InstantiateAsync(frameKey, ViewManager.GetLayer(UILayer.Window));

            string contentKey = GameResPath.GetUIPrefab(CONTENT_MODULE, CONTENT_PREFAB);
            _contentRoot = await ResManager.InstantiateAsync(contentKey, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (_frameRoot == null || _contentRoot == null)
            {
                GameLog.Error("Guild", "公会主界面加载失败(BaseWindowSkin 或 GuildModule 缺失)");
                return;
            }
            _frameRoot.name = FRAME_PREFAB;
            _contentRoot.name = CONTENT_PREFAB;
            foreach (Transform c in _contentRoot.transform) c.gameObject.SetActive(false);

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("Guild", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
                return;
            }

            _mainView = _contentRoot.GetComponentInChildren<GuildMainView>(true);
            _memberView = _contentRoot.GetComponentInChildren<GuildMemberView>(true);
            _applyView = _contentRoot.GetComponentInChildren<GuildApplyLookView>(true);

            var specs = new List<TabSpec>
            {
                new TabSpec
                {
                    Enabled = _mainView != null,
                    Label = "信息",
                    BackgroundImagePath = GameResPath.GetBigBgPath("ui_Guild_bg.jpg"),
                    ContentFactory = _mainView != null ? (System.Func<RectTransform, BaseView>)(p => Reparent(_mainView, p)) : null,
                },
                new TabSpec
                {
                    Enabled = _memberView != null,
                    Label = "成员",
                    BackgroundImagePath = GameResPath.GetBigBgPath("daily_bg.jpg"),
                    ContentFactory = _memberView != null ? (System.Func<RectTransform, BaseView>)(p => Reparent(_memberView, p)) : null,
                },
                new TabSpec { Enabled = false, Label = "排行" }, // HolyTerritoryGuildView,圣域模块跨包,TODO
                new TabSpec { Enabled = false, Label = "宝箱" }, // GuildRewardBoxView,归 13b 仓库宝箱包,TODO
            };

            _window.Show();
            _window.Configure(specs, TAB_INFO);
            GuildController.Instance.RequestBaseInfo();
            GameLog.Info("Guild", "公会主界面打开(信息/成员真接线,排行/宝箱 TODO)");
        }

        private static BaseView Reparent(BaseView view, RectTransform parent)
        {
            if (view == null) return null;
            view.transform.SetParent(parent, false);
            view.gameObject.SetActive(true);
            return view;
        }

        /// <summary>由 GuildMemberView"查看申请"驱动打开(GuildApplyLookView 是弹层,非 tab,留在
        /// _contentRoot 原位显示,不随 tab 切换而卸载)。</summary>
        public static void OpenApplyLook()
        {
            if (_applyView == null)
            {
                GameLog.Warn("Guild", "GuildApplyLookView 未加载(GuildModule.prefab 缺节点?)");
                return;
            }
            _applyView.transform.SetAsLastSibling();
            _applyView.Show();
        }

        /// <summary>断线/登出清理(public:CliVerify GuildCoreCase 渲染段收尾要跨程序集调用)。</summary>
        public static void Reset()
        {
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _mainView = null;
            _memberView = null;
            _applyView = null;
            _loading = false;
        }
    }
}
