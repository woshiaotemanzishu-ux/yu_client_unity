using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Halo
{
    /// <summary>
    /// 光环模块编排:按需打开/关闭光环面板(对标老端 顶部状态条 _box_halo → OPEN_VIEW "HaloMainView")。
    /// 打开 HaloModule 后只 Show HaloMainView(自带关闭按钮)。入口注册见 <see cref="HaloBootstrap"/>(MainUIRouter "halo")。再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class HaloFlow
    {
        private const string MODULE = "halo";
        private const string PREFAB = "HaloModule";

        private static GameObject _moduleRoot;
        private static HaloMainView _mainView;
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
                GameLog.Error("Halo", "HaloModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            BaseView[] views = root.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views) v.gameObject.SetActive(false);
            foreach (BaseView v in views)
            {
                if (v is HaloMainView hv) { _mainView = hv; break; }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Halo", "HaloModule 缺 HaloMainView(重跑 halo 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Halo", "光环面板打开: {0}", key);
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
