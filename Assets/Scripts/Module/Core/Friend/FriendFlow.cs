using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友模块编排:按需打开/关闭好友面板(对标老端 主 HUD 好友按钮 → FriendView)。
    ///
    /// FriendModule 合并 prefab 含 FriendView(主) + 加好友/申请/黑名单/菜单/邮件/客服 等子窗;本 tick 仅移植主窗
    /// <c>FriendView</c>,打开时隐藏所有顶层窗口再只 Show 主窗。子窗经 <see cref="OpenSub"/> 叠在主面板上(加好友/申请/黑名单按钮调用,
    /// 子窗未写时日志降级)。入口注册见 <see cref="FriendBootstrap"/>(MainUIRouter "friend",HUD 好友按钮触发)。
    /// FriendView 无独立关闭按钮 → HUD 好友按钮再点关闭(Toggle)。
    /// </summary>
    public static class FriendFlow
    {
        private const string MODULE = "friend";
        private const string PREFAB = "FriendModule";

        private static GameObject _moduleRoot;
        private static FriendView _mainView;
        private static bool _loading;

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

        /// <summary>打开好友模块内子窗(加好友/申请/黑名单…),叠在主面板上;按 View 子类名查找。未移植查不到 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Friend", "OpenSub({0}) 时好友模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName)
                {
                    v.Show();
                    return;
                }
            }
            GameLog.Info("Friend", "好友子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
                GameLog.Error("Friend", "FriendModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            foreach (Transform c in root.transform)
            {
                c.gameObject.SetActive(false);
            }

            foreach (BaseView v in root.GetComponentsInChildren<BaseView>(true))
            {
                if (v is FriendView fv)
                {
                    _mainView = fv;
                    break;
                }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Friend", "FriendModule 缺 FriendView(重跑 friend 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Friend", "好友面板打开: {0}", key);
        }

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
