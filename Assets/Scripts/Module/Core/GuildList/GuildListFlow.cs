using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.GuildList
{
    /// <summary>
    /// 公会列表模块编排:按需打开/关闭公会列表面板(无会态)。
    ///
    /// 对标老端 主界面 `MainFunc.Guild(=8)` → `GuildModel.OpenGuildView`:有会开 GuildMainBaseView、
    /// 无会开 GuildListView。会籍分支已在 <see cref="GuildListBootstrap"/> 落地(轮13a:
    /// RoleModel.GuildId&gt;0 → <see cref="Shenxiao.Module.Core.Guild.GuildMainFlow"/>,否则 → 本 Flow)。
    /// 手法照抄 BagFlow/RoleFlow。
    /// 无独立关闭按钮 → 再次点击图标 <see cref="Toggle"/> 关闭(与老端 BaseWindowComponent 再点关闭一致)。
    ///
    /// **存档**:GuildModule.prefab 里烤好但零引用的 GuildJoinView 节点未采用——本 Flow 的 <see cref="GuildListView"/>
    /// (GuildListViewBind)已是真业务代码入口,迁移 GuildJoinView 无协议收益,故保留死节点原样,不接线(轮13a)。
    /// </summary>
    public static class GuildListFlow
    {
        private const string MODULE = "guildList";
        private const string PREFAB = "GuildListModule";

        private static GameObject _moduleRoot;
        private static GuildListView _mainView;
        private static bool _loading;

        /// <summary>切换显示:已开则关、未开则开。</summary>
        public static void Toggle()
        {
            if (_mainView != null && _mainView.IsShown)
            {
                Close();
                return;
            }
            _ = OpenAsync();
        }

        public static void Open()
        {
            _ = OpenAsync();
        }

        public static void Close()
        {
            if (_mainView != null)
            {
                _mainView.Hide();
            }
        }

        private static async Task OpenAsync()
        {
            if (_moduleRoot != null)
            {
                if (_mainView != null)
                {
                    _mainView.Show();
                }
                return;
            }

            if (_loading)
            {
                return;
            }
            _loading = true;

            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("GuildList", "GuildListModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            BaseView[] views = root.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views)
            {
                v.gameObject.SetActive(false);
            }

            foreach (BaseView v in views)
            {
                if (v is GuildListView gv)
                {
                    _mainView = gv;
                    break;
                }
            }

            if (_mainView == null)
            {
                GameLog.Warn("GuildList", "GuildListModule 缺 GuildListView(重跑 guildList 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("GuildList", "公会列表打开: {0}", key);
        }

        /// <summary>断线(非游戏内自动重连)/登出时清理整模块根。</summary>
        internal static void Reset()
        {
            if (_moduleRoot != null)
            {
                ResManager.ReleaseInstance(_moduleRoot);
            }
            _moduleRoot = null;
            _mainView = null;
            _loading = false;
        }
    }
}
