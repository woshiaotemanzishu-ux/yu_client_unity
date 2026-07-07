using System;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置模块编排:按需打开/关闭设置面板(对标老端 主 HUD 设置按钮 → SettingView)。
    ///
    /// SettingModule 合并 prefab 含 SettingView(主) + SettingChangeHeadView(改头像)/SettingChangeNameView(改名)子窗;
    /// 本 tick 三窗 View 全写,打开时隐藏所有顶层窗口再只 Show 主窗(默认 active 的是子窗,需隐藏)。入口注册见
    /// <see cref="SettingBootstrap"/>(MainUIRouter "setting",由 HUD 设置按钮 MainUIChatView 触发)。
    /// 子窗经 <see cref="OpenSub"/> 叠在主面板上(主面板 改头像/改名 按钮调用)。
    /// </summary>
    public static class SettingFlow
    {
        private const string MODULE = "setting";
        private const string PREFAB = "SettingModule";

        private static GameObject _moduleRoot;
        private static SettingView _mainView;
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

        /// <summary>打开设置模块内子窗(改头像/改名),叠在主面板上;按 View 子类名查找。未移植查不到 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Setting", "OpenSub({0}) 时设置模块未打开", viewTypeName);
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
            GameLog.Info("Setting", "设置子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        private static async Task OpenAsync()
        {
            if (_moduleRoot != null)
            {
                if (_mainView != null)
                {
                    _mainView.Show();
                    return;
                }

                GameLog.Warn("Setting", "SettingModule root exists but main view is missing, recreate module");
                ResManager.ReleaseInstance(_moduleRoot);
                _moduleRoot = null;
            }

            if (_loading)
            {
                return;
            }
            _loading = true;

            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = null;
            try
            {
                root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            }
            catch (Exception e)
            {
                GameLog.Error("Setting", "SettingModule prefab load exception: {0}, {1}", key, e.Message);
                return;
            }
            finally
            {
                _loading = false;
            }

            if (root == null)
            {
                GameLog.Error("Setting", "SettingModule prefab load failed: {0}", key);
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
                if (v is SettingView sv)
                {
                    _mainView = sv;
                    break;
                }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Setting", "SettingModule 缺 SettingView(重跑 setting 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Setting", "设置面板打开: {0}", key);
        }

        // ---------------------------------------------------------------- 底部按钮流程(换角色/返回登录/修复异常)

        /// <summary>换角色(对标老端 CHANGE_ROLE):禁自动重连 → 断线(触发全模块 teardown)→ 重连发 10000,
        /// 角色列表到达后 LoginFlow.OnRoleList 自动进选角页。</summary>
        public static void BackToRoleSelect()
        {
            _ = BackToRoleSelectAsync();
        }

        private static async Task BackToRoleSelectAsync()
        {
            Close();
            LoginController.Instance.ClearInGameReconnectState();
            await NetManager.DisconnectAsync();
            LoginRequestResult result = await LoginController.Instance.ConnectGameAsync();
            if (!result.success)
            {
                TipsManager.Toast("连接失败: " + result.message);
                LoginFlow.ShowLogin();
            }
        }

        /// <summary>返回登录(对标老端 CHANGE_ACCOUNT):禁自动重连 → 断线 → 回账号登录页。</summary>
        public static void BackToLogin()
        {
            _ = BackToLoginAsync();
        }

        private static async Task BackToLoginAsync()
        {
            Close();
            LoginController.Instance.ClearInGameReconnectState();
            await NetManager.DisconnectAsync();
            LoginFlow.ShowLogin();
        }

        /// <summary>修复异常(老端 = window.location.reload):保留游戏内自动重连状态直接断线,
        /// LoginController 2s 后自动重连并以原角色续接(等价"重启修复")。</summary>
        public static void ReconnectRepair()
        {
            Close();
            GameLog.Info("Setting", "修复异常 → 主动断线走游戏内自动重连");
            _ = NetManager.DisconnectAsync();
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
