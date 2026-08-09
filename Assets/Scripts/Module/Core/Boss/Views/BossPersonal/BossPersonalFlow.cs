using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossPersonal
{
    /// <summary>专属大妖合并 Prefab 的页面/弹窗编排；不重建人工 Prefab。</summary>
    public static class BossPersonalFlow
    {
        private const string Module = "bossPersonal";
        private const string Prefab = "BossPersonalModule";
        private static GameObject _root;
        private static BaseView _page;
        private static bool _loading;

        public static void Open() => _ = OpenPageAsync(nameof(BossPersonalEnterView), null);

        public static void OpenAlert(BossPersonalAlert.Args args) =>
            _ = OpenPopupAsync(nameof(BossPersonalAlert), args);

        public static void OpenVipAdd() => _ = OpenPopupAsync(nameof(BossVipAddView), null);

        public static void Close()
        {
            _page?.Hide();
            _page = null;
        }

        private static async Task<GameObject> EnsureRootAsync()
        {
            if (_root != null || _loading) return _root;
            _loading = true;
            try
            {
                string key = GameResPath.GetUIPrefab(Module, Prefab);
                _root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
                if (_root != null) _root.name = Prefab;
                else GameLog.Error("BossPersonal", "BossPersonalModule load failed: {0}", key);
            }
            catch (Exception e)
            {
                GameLog.Error("BossPersonal", "BossPersonalModule load exception: {0}", e.Message);
            }
            finally { _loading = false; }
            return _root;
        }

        private static async Task OpenPageAsync(string viewName, object args)
        {
            GameObject root = await EnsureRootAsync();
            if (root == null) return;
            BaseView target = FindTopLevel(root, viewName);
            if (target == null)
            {
                GameLog.Error("BossPersonal", "missing runtime page: {0}", viewName);
                return;
            }
            foreach (BaseView view in root.GetComponentsInChildren<BaseView>(true))
                if (view.transform.parent == root.transform && view != target) view.Hide();
            _page = target;
            target.Show(args);
        }

        private static async Task OpenPopupAsync(string viewName, object args)
        {
            GameObject root = await EnsureRootAsync();
            if (root == null) return;
            BaseView target = FindTopLevel(root, viewName);
            if (target == null)
            {
                GameLog.Error("BossPersonal", "missing runtime popup: {0}", viewName);
                return;
            }
            target.Show(args);
        }

        private static BaseView FindTopLevel(GameObject root, string viewName)
        {
            foreach (BaseView view in root.GetComponentsInChildren<BaseView>(true))
                if (view.transform.parent == root.transform && view.GetType().Name == viewName) return view;
            return null;
        }

        internal static void Reset()
        {
            if (_root != null) ResManager.ReleaseInstance(_root);
            _root = null;
            _page = null;
            _loading = false;
        }
    }
}
