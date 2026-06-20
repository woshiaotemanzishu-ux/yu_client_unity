using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包模块编排:按需打开/关闭背包面板(对标老客户端 BagComponentView 经主界面功能图标 SwitchView 打开)。
    ///
    /// 与 MainUIFlow 不同,背包不在 EVT_GAME_START 自动开,而是由主界面底部功能图标点击触发
    /// (MainFuncIconItem → MainUIRouter.Open("bag") → 本类,注册见 <see cref="BagBootstrap"/>)。
    /// 编排手法照抄 MainUIFlow:实例化 BagModule 合并 prefab → 收集模块内所有 BaseView → 全部关闭 →
    /// 只 Show 主背包面板 BagComponentView;子窗(一键使用/熔炼/扩展…)各自入口待后续接。
    ///
    /// BagComponentViewBind 无独立关闭按钮字段 → 关闭走「再次点击功能图标」的 <see cref="Toggle"/>
    /// (老端通常有关闭按钮,Unity 侧缺该字段,先用 toggle 兜底,便于实机开/关验收)。
    /// </summary>
    public static class BagFlow
    {
        private const string MODULE = "bag";
        private const string PREFAB = "BagModule";

        private static GameObject _moduleRoot;
        private static BagComponentView _bagView;
        private static bool _loading;

        /// <summary>切换显示:已开则关、未开则开(功能图标再次点击的直觉行为;无独立关闭按钮)。</summary>
        public static void Toggle()
        {
            if (_bagView != null && _bagView.IsShown)
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
            if (_bagView != null)
            {
                _bagView.Hide();
            }
        }

        private static async Task OpenAsync()
        {
            // 已实例化:直接复用(Show 会置顶 + 走 OnShow 刷新)。Unity fake-null:根被销毁时 _moduleRoot==null 成立。
            if (_moduleRoot != null)
            {
                if (_bagView != null)
                {
                    _bagView.Show();
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
                GameLog.Error("Bag", "BagModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            // 对标 MainUIFlow:收集模块内所有 BaseView,先全部关闭,只显示主背包面板。
            BaseView[] views = root.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views)
            {
                v.gameObject.SetActive(false);
            }

            foreach (BaseView v in views)
            {
                if (v is BagComponentView bv)
                {
                    _bagView = bv;
                    break;
                }
            }

            if (_bagView == null)
            {
                GameLog.Warn("Bag", "BagModule 缺 BagComponentView(重跑 bag 流水线:转换+回填)");
                return;
            }

            _bagView.Show();
            GameLog.Info("Bag", "背包打开: {0}", key);
        }

        /// <summary>断线(非游戏内自动重连)/登出时清理整模块根,避免残留旧实例。</summary>
        internal static void Reset()
        {
            if (_moduleRoot != null)
            {
                ResManager.ReleaseInstance(_moduleRoot);
            }
            _moduleRoot = null;
            _bagView = null;
            _loading = false;
        }
    }
}
