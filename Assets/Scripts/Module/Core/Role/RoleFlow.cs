using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 角色模块编排:按需打开/关闭角色面板(对标老端 主界面 MainFunc.Role → OPEN_CHOSE_ROLE_VIEW → RoleView)。
    ///
    /// 老端 RoleView 是分页窗(人物 EquipmentView / 垂神翼影 / 古法符相 / 殒锋天刃 / 玄穹云披);Unity 侧目前
    /// 仅移植了首页 <c>EquipmentView</c>(人物·装备/属性),故本 Flow 打开 RoleModule 后只 Show 它(= 老端 tab0)。
    /// 其余分页待移植后再接分页切换。手法照抄 BagFlow/MainUIFlow:实例化合并 prefab → 全关 BaseView → 只 Show 主页。
    /// 入口注册见 <see cref="RoleBootstrap"/>(MainUIRouter "role")。无独立关闭按钮 → 再次点击图标 <see cref="Toggle"/> 关闭
    /// (与老端 BaseWindowComponent 再点关闭一致)。
    /// </summary>
    public static class RoleFlow
    {
        private const string MODULE = "role";
        private const string PREFAB = "RoleModule";

        private static GameObject _moduleRoot;
        private static EquipmentView _mainView;
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
                GameLog.Error("Role", "RoleModule prefab load failed: {0}", key);
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
                if (v is EquipmentView ev)
                {
                    _mainView = ev;
                    break;
                }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Role", "RoleModule 缺 EquipmentView(重跑 role 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Role", "角色面板打开: {0}", key);
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
