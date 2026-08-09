using System;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Adventure
{
    /// <summary>AdventureModule 的页面内生命周期；不发送任何投掷、重置、购买或刷新事务。</summary>
    public static class AdventureFlow
    {
        private const string Module = "Adventure";
        private const string Prefab = "AdventureModule";
        private static GameObject _root;
        private static AdventureMainView _main;
        private static AdventureShopView _shop;
        private static Task<bool> _loadTask;
        private static int _generation;

        public static void Toggle()
        {
            if (_main != null && _main.IsShown) Close();
            else Open();
        }

        public static void Open()
        {
            if (!AdventureModel.Instance.IsActivityOpen())
            {
                TipsManager.Toast("活动未开启");
                return;
            }
            _ = OpenAsync(_generation);
        }

        public static void Close()
        {
            _shop?.Hide();
            _main?.Hide();
        }

        public static void OpenShop()
        {
            if (_shop == null)
            {
                TipsManager.Toast("冒险商店尚未加载");
                return;
            }
            _shop.Show();
        }

        public static void CloseShop() => _shop?.Hide();

        private static async Task OpenAsync(int generation)
        {
            if (!await EnsureLoaded(generation) || generation != _generation) return;
            _main.Show();
        }

        private static async Task<bool> EnsureLoaded(int generation)
        {
            if (_root != null && _main != null && _shop != null) return true;
            Task<bool> task = _loadTask;
            if (task == null) _loadTask = task = LoadAsync(generation);
            try { return await task; }
            finally { if (ReferenceEquals(_loadTask, task)) _loadTask = null; }
        }

        private static async Task<bool> LoadAsync(int generation)
        {
            GameObject root = null;
            try
            {
                string key = GameResPath.GetUIPrefab(Module, Prefab);
                root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
                if (generation != _generation)
                {
                    if (root != null) ResManager.ReleaseInstance(root);
                    return false;
                }
                if (root == null)
                {
                    GameLog.Error("Adventure", "prefab load failed: {0}", key);
                    return false;
                }

                AdventureMainView main = root.GetComponentInChildren<AdventureMainView>(true);
                AdventureShopView shop = root.GetComponentInChildren<AdventureShopView>(true);
                if (main == null || shop == null)
                {
                    GameLog.Error("Adventure", "AdventureModule missing business Main/Shop view");
                    ResManager.ReleaseInstance(root);
                    return false;
                }

                foreach (AdventureItem template in root.GetComponentsInChildren<AdventureItem>(true))
                    template.gameObject.SetActive(false);
                foreach (AdventureShopItem template in root.GetComponentsInChildren<AdventureShopItem>(true))
                    template.gameObject.SetActive(false);
                main.gameObject.SetActive(false);
                shop.gameObject.SetActive(false);
                root.name = Prefab;
                _root = root;
                _main = main;
                _shop = shop;
                return true;
            }
            catch (Exception e)
            {
                if (root != null) ResManager.ReleaseInstance(root);
                GameLog.Error("Adventure", "load failed: {0}", e);
                return false;
            }
        }

        internal static void Reset()
        {
            unchecked { _generation++; }
            Close();
            if (_root != null) ResManager.ReleaseInstance(_root);
            _root = null;
            _main = null;
            _shop = null;
            _loadTask = null;
        }
    }
}
