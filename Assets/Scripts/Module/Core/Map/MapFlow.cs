using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>
    /// 地图模块编排:按需打开/关闭地图(对标老端 顶部状态条 _box_map → OPEN_MAP_VIEW "MapEnterView")。
    ///
    /// 老端入口经 MapEnterView 选区域/世界图;Unity 已写 WorldMapView(世界地图)/AreaMapView(区域地图)+ 列表项,MapEnterView 未移植 →
    /// 本 Flow 打开 MapModule 后只 Show 默认页 <c>WorldMapView</c>(世界地图,取首页);区域图 AreaMapView 经 <see cref="OpenSub"/> 后续接。
    /// 手法照抄 BagFlow:实例化合并 prefab → 全关 BaseView → 只 Show 主页。入口注册见 <see cref="MapBootstrap"/>(MainUIRouter "map")。
    /// 无独立关闭统管 → 顶部地图按钮再点关闭(Toggle)。
    /// </summary>
    public static class MapFlow
    {
        private const string MODULE = "map";
        private const string PREFAB = "MapModule";

        private static GameObject _moduleRoot;
        private static WorldMapView _mainView;
        private static bool _loading;

        public static void Toggle()
        {
            if (_mainView != null && _mainView.IsShown) { Close(); return; }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_mainView != null) _mainView.Hide();
        }

        /// <summary>打开地图模块内子窗(区域地图 AreaMapView…),按 View 子类名查找。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Map", "OpenSub({0}) 时地图模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Map", "地图子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        private static async Task OpenAsync()
        {
            if (_moduleRoot != null)
            {
                if (_mainView != null) _mainView.Show();
                return;
            }

            if (_loading) return;
            _loading = true;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("Map", "MapModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            BaseView[] views = root.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views) v.gameObject.SetActive(false);
            foreach (BaseView v in views)
            {
                if (v is WorldMapView wv) { _mainView = wv; break; }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Map", "MapModule 缺 WorldMapView(重跑 map 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Map", "地图打开(世界地图): {0}", key);
        }

        internal static void Reset()
        {
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _mainView = null;
            _loading = false;
        }
    }
}
