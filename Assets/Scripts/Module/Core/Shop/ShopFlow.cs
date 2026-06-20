using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 商城模块编排:按需打开/关闭商城面板(对标老端 主 HUD 商城按钮 → ShopCommonView)。
    ///
    /// ShopModule 合并 prefab 含 ShopCommonView(常规,主) + ShopMysteriousView(神秘)/ShopVieView/ShopBulkPurchaseView(批量购买);
    /// 本 tick 仅移植主窗 <c>ShopCommonView</c>,打开时隐藏所有顶层窗口再只 Show 主窗。其余商城分类/批量购买经 <see cref="OpenSub"/>
    /// 后续接(分类页签 tab 待补)。入口注册见 <see cref="ShopBootstrap"/>(MainUIRouter "shop",HUD 商城按钮触发)。
    /// 主窗无独立关闭按钮 → HUD 商城按钮再点关闭(Toggle)。
    /// </summary>
    public static class ShopFlow
    {
        private const string MODULE = "shop";
        private const string PREFAB = "ShopModule";

        private static GameObject _moduleRoot;
        private static ShopCommonView _mainView;
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

        /// <summary>打开商城模块内子窗(神秘商店/批量购买…),叠在主面板上;按 View 子类名查找。未移植查不到 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Shop", "OpenSub({0}) 时商城模块未打开", viewTypeName);
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
            GameLog.Info("Shop", "商城子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
                GameLog.Error("Shop", "ShopModule prefab load failed: {0}", key);
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
                if (v is ShopCommonView sv)
                {
                    _mainView = sv;
                    break;
                }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Shop", "ShopModule 缺 ShopCommonView(重跑 shop 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Shop", "商城面板打开: {0}", key);
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
