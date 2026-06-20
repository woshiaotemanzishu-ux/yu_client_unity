using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友/邮件模块编排:FriendModule 合并 prefab 含多个顶层窗口(FriendView 好友 / EmailView 邮件 / 加好友·申请·黑名单·菜单·客服…)。
    ///
    /// 统一以 <see cref="OpenView"/>(name) 作核心:确保模块加载 → 隐藏所有顶层窗口 → 只 Show 指定主窗(替换语义)。
    /// 这样主 HUD 的 好友按钮(Toggle→FriendView)与 二级HUD 邮件按钮(ToggleEmail→EmailView)各自切换、互不残留。
    /// 子窗(加好友/申请/黑名单)经 <see cref="OpenSub"/> 叠在当前主窗上(覆盖语义,不隐藏主窗)。
    /// 入口注册见 <see cref="FriendBootstrap"/>(MainUIRouter "friend"/"email")。无关闭按钮的窗 → 由对应 HUD 按钮再点关闭。
    /// </summary>
    public static class FriendFlow
    {
        private const string MODULE = "friend";
        private const string PREFAB = "FriendModule";

        private static GameObject _moduleRoot;
        private static BaseView _shown;
        private static bool _loading;

        // ---- 好友(FriendView)----
        public static void Toggle()
        {
            if (_shown != null && _shown.IsShown && _shown is FriendView) { _shown.Hide(); return; }
            OpenView("FriendView");
        }

        public static void Open() => OpenView("FriendView");

        // ---- 邮件(EmailView,二级HUD 邮件按钮)----
        public static void ToggleEmail() => ToggleView("EmailView");

        public static void Close()
        {
            if (_shown != null) _shown.Hide();
        }

        /// <summary>切换某顶层主窗:已是它且显示中 → 关;否则打开它(替换当前主窗)。</summary>
        public static void ToggleView(string viewTypeName)
        {
            if (_shown != null && _shown.IsShown && _shown.GetType().Name == viewTypeName) { _shown.Hide(); return; }
            OpenView(viewTypeName);
        }

        /// <summary>打开 FriendModule 内某顶层主窗(替换语义:隐藏其余顶层窗,只显它)。</summary>
        public static void OpenView(string viewTypeName) => _ = OpenViewAsync(viewTypeName);

        /// <summary>打开模块内子窗(加好友/申请/黑名单…),叠在当前主窗上(覆盖,不隐藏主窗);按 View 子类名查找。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Friend", "OpenSub({0}) 时好友模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Friend", "好友子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        private static async Task OpenViewAsync(string viewTypeName)
        {
            if (!await EnsureModuleAsync()) return;

            // 隐藏所有顶层窗口,再只显目标主窗(替换语义,避免上一个主窗残留)。
            foreach (Transform c in _moduleRoot.transform)
            {
                c.gameObject.SetActive(false);
            }

            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName)
                {
                    v.Show();
                    _shown = v;
                    GameLog.Info("Friend", "打开 {0}", viewTypeName);
                    return;
                }
            }
            GameLog.Warn("Friend", "FriendModule 缺主窗 {0}(重跑 friend 流水线:转换+回填)", viewTypeName);
        }

        private static async Task<bool> EnsureModuleAsync()
        {
            if (_moduleRoot != null) return true;
            if (_loading) return false;
            _loading = true;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;
            if (root == null)
            {
                GameLog.Error("Friend", "FriendModule prefab load failed: {0}", key);
                return false;
            }
            _moduleRoot = root;
            _moduleRoot.name = PREFAB;
            return true;
        }

        internal static void Reset()
        {
            if (_moduleRoot != null)
            {
                ResManager.ReleaseInstance(_moduleRoot);
            }
            _moduleRoot = null;
            _shown = null;
            _loading = false;
        }
    }
}
