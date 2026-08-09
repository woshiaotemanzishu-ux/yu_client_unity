using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Dungeon.Views.DungeonRune;
using UnityEngine;

namespace Shenxiao.Module.Core.Dungeon
{
    /// <summary>灵魄副本入口壳；视觉由现有 DungeonRuneModule.prefab 承担，本类不再运行时代码建树。</summary>
    public static class DungeonRuneShellView
    {
        private static GameObject _moduleRoot;
        private static DungeonRuneEnterView _enterView;
        private static Task<bool> _loadTask;
        private static int _openEpoch;

        public static void Show() => _ = ShowAsync(++_openEpoch);

        public static void Close()
        {
            ++_openEpoch;
            if (_enterView != null) _enterView.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
        }

        private static async Task ShowAsync(int epoch)
        {
            if (!await EnsureLoaded())
            {
                TipsManager.Toast("灵魄副本界面加载失败");
                return;
            }
            if (epoch != _openEpoch) return;
            _moduleRoot.SetActive(true);
            _enterView.Show();
            _enterView.transform.SetAsLastSibling();
        }

        private static async Task<bool> EnsureLoaded()
        {
            if (_moduleRoot != null && _enterView != null) return true;
            if (_loadTask != null && _loadTask.IsCompleted)
            {
                _loadTask = null;
                _moduleRoot = null;
                _enterView = null;
            }
            if (_loadTask == null) _loadTask = LoadPrefab();
            return await _loadTask;
        }

        private static async Task<bool> LoadPrefab()
        {
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("Dungeon", "DungeonRuneModule cannot load: Window layer missing");
                return false;
            }
            string key = GameResPath.GetUIPrefab("dungeonRune", "DungeonRuneModule");
            _moduleRoot = await ResManager.InstantiateAsync(key, parent);
            if (_moduleRoot == null)
            {
                GameLog.Error("Dungeon", "DungeonRuneModule prefab load failed: {0}", key);
                return false;
            }
            _moduleRoot.name = "DungeonRuneModule";
            foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true)) view.gameObject.SetActive(false);
            _enterView = _moduleRoot.GetComponentInChildren<DungeonRuneEnterView>(true);
            if (_enterView == null)
            {
                GameLog.Error("Dungeon", "DungeonRuneModule missing business DungeonRuneEnterView; Generated Bind is not a runtime page");
                Object.Destroy(_moduleRoot);
                _moduleRoot = null;
                return false;
            }
            _moduleRoot.SetActive(false);
            return true;
        }
    }
}
