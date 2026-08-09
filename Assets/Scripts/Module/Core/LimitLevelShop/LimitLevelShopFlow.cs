using System;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.LimitLevelShop
{
    public static class LimitLevelShopFlow
    {
        private const string Module = "LimitLevelShop";
        private const string Prefab = "LimitLevelShopModule";
        private static GameObject _root;
        private static LimitLevelShopView _view;
        private static Task<bool> _loadTask;
        private static int _generation;

        public static void Toggle(string iconType)
        {
            if (_view != null && _view.IsShown && _view.IconType == iconType) Close();
            else Open(iconType);
        }

        public static void Open(string iconType)
        {
            LimitLevelShopModel.GiftEntry gift = LimitLevelShopModel.Instance.FindByIcon(iconType);
            if (gift == null)
            {
                TipsManager.Toast("活动未开启");
                return;
            }
            if (gift.Type == 66)
            {
                // 老端 type=66 走 AssistanceGiftView；该页面不在本文件岛，禁止错开成普通抢购页。
                TipsManager.Toast("该推送礼包页面尚未接入");
                return;
            }
            if (!LimitLevelShopModel.Instance.TryGetGiftConfig(gift.Type, gift.Subtype, out _))
                LimitLevelShopController.Instance.RequestGiftConfig(gift.Type, gift.Subtype, 0);
            _ = OpenAsync(iconType, _generation);
        }

        public static void Close() => _view?.Hide();

        private static async Task OpenAsync(string iconType, int generation)
        {
            if (!await EnsureLoaded(generation) || generation != _generation) return;
            _view.Show(iconType);
        }

        private static async Task<bool> EnsureLoaded(int generation)
        {
            if (_root != null && _view != null) return true;
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
                root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Popup));
                if (generation != _generation)
                {
                    if (root != null) ResManager.ReleaseInstance(root);
                    return false;
                }
                if (root == null)
                {
                    GameLog.Error("LimitLevelShop", "prefab load failed: {0}", key);
                    return false;
                }
                LimitLevelShopView view = root.GetComponentInChildren<LimitLevelShopView>(true);
                if (view == null)
                {
                    GameLog.Error("LimitLevelShop", "LimitLevelShopModule missing LimitLevelShopView business component");
                    ResManager.ReleaseInstance(root);
                    return false;
                }
                foreach (LimitLevelShopTabItem template in root.GetComponentsInChildren<LimitLevelShopTabItem>(true))
                    template.gameObject.SetActive(false);
                foreach (LimitLevelShopReward template in root.GetComponentsInChildren<LimitLevelShopReward>(true))
                    template.gameObject.SetActive(false);
                root.name = Prefab;
                view.gameObject.SetActive(false);
                _root = root;
                _view = view;
                return true;
            }
            catch (Exception e)
            {
                if (root != null) ResManager.ReleaseInstance(root);
                GameLog.Error("LimitLevelShop", "load failed: {0}", e);
                return false;
            }
        }

        internal static void Reset()
        {
            unchecked { _generation++; }
            Close();
            if (_root != null) ResManager.ReleaseInstance(_root);
            _root = null;
            _view = null;
            _loadTask = null;
        }
    }
}
