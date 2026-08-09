using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.DungeonHeart
{
    /// <summary>加载现有人工 Prefab；Task 模块负责以权威任务状态调用 Open。</summary>
    public static class DungeonHeartFlow
    {
        private const string Module = "dungeonHeart";
        private const string Prefab = "DungeonHeartModule";
        private static GameObject _root;
        private static DungeonHeartEnterView _view;
        private static bool _loading;

        public static void Open(int dungeonId) => _ = OpenAsync(dungeonId);

        public static void Close() => _view?.Hide();

        private static async Task OpenAsync(int dungeonId)
        {
            GameObject root = await EnsureRootAsync();
            if (root == null) return;
            if (_view == null)
            {
                foreach (DungeonHeartEnterView candidate in root.GetComponentsInChildren<DungeonHeartEnterView>(true))
                {
                    if (candidate.transform.parent == root.transform)
                    {
                        _view = candidate;
                        break;
                    }
                }
            }
            if (_view == null)
            {
                GameLog.Error("DungeonHeart", "DungeonHeartModule is missing its wired DungeonHeartEnterView");
                return;
            }
            _view.Show(new DungeonHeartEnterView.Args { DungeonId = dungeonId });
        }

        private static async Task<GameObject> EnsureRootAsync()
        {
            if (_root != null || _loading) return _root;
            _loading = true;
            try
            {
                string key = GameResPath.GetUIPrefab(Module, Prefab);
                _root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Popup));
                if (_root != null) _root.name = Prefab;
                else GameLog.Error("DungeonHeart", "DungeonHeartModule load failed: {0}", key);
            }
            catch (Exception exception)
            {
                GameLog.Error("DungeonHeart", "DungeonHeartModule load exception: {0}", exception.Message);
            }
            finally
            {
                _loading = false;
            }
            return _root;
        }

        internal static void Reset()
        {
            if (_root != null) ResManager.ReleaseInstance(_root);
            _root = null;
            _view = null;
            _loading = false;
        }
    }
}
