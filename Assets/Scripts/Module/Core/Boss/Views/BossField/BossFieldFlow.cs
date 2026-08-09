using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    /// <summary>BossField 合并 Prefab 的页面/弹窗编排；所有页面继续以 Prefab 为视觉事实源。</summary>
    public static class BossFieldFlow
    {
        private const string Module = "bossField";
        private const string Prefab = "BossFieldModule";
        private static GameObject _root;
        private static BaseView _page;
        private static bool _loading;

        public static void Open() => _ = OpenPageAsync(nameof(BossFieldEnterView), null);
        public static void OpenAbyss() => _ = OpenPageAsync(nameof(BossAbyssEnterView), null);
        public static void OpenSoulShop() => _ = OpenPopupAsync(nameof(BossFieldSoulShopView), null);
        public static void OpenSoulShopAlert(BossFieldSoulShopAlert.Args args) =>
            _ = OpenPopupAsync(nameof(BossFieldSoulShopAlert), args);
        public static void OpenTired() => _ = OpenPopupAsync(nameof(BossFieldTiredNewView), null);
        public static void OpenVitBuy() => _ = OpenPopupAsync(nameof(BossFieldVitBuyView), null);
        public static void OpenRelive() => _ = OpenPopupAsync(nameof(BossFieldReliveView), null);
        public static void OpenAbyssFailure(BossAbyssFailureView.Args args) =>
            _ = OpenPopupAsync(nameof(BossAbyssFailureView), args);

        internal static void OpenPopupForSoulItem() => GameLog.Info("BossField", "战魂增益道具使用入口缺 Goods 正式流程，当前 blocker");

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
                else GameLog.Error("BossField", "BossFieldModule load failed: {0}", key);
            }
            catch (Exception e) { GameLog.Error("BossField", "BossFieldModule load exception: {0}", e.Message); }
            finally { _loading = false; }
            return _root;
        }

        private static async Task OpenPageAsync(string name, object args)
        {
            GameObject root = await EnsureRootAsync();
            if (root == null) return;
            BaseView target = FindTopLevel(root, name);
            if (target == null) { GameLog.Error("BossField", "missing runtime page: {0}", name); return; }
            foreach (BaseView view in root.GetComponentsInChildren<BaseView>(true))
                if (view.transform.parent == root.transform && view != target) view.Hide();
            _page = target;
            target.Show(args);
        }

        private static async Task OpenPopupAsync(string name, object args)
        {
            GameObject root = await EnsureRootAsync();
            if (root == null) return;
            BaseView target = FindTopLevel(root, name);
            if (target == null) { GameLog.Error("BossField", "missing runtime popup: {0}", name); return; }
            target.Show(args);
        }

        private static BaseView FindTopLevel(GameObject root, string name)
        {
            foreach (BaseView view in root.GetComponentsInChildren<BaseView>(true))
                if (view.transform.parent == root.transform && view.GetType().Name == name) return view;
            return null;
        }

        internal static void Reset()
        {
            if (_root != null) ResManager.ReleaseInstance(_root);
            _root = null; _page = null; _loading = false;
        }
    }
}
