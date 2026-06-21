using System.Collections.Generic;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面浮层通用开关:按需实例化独立浮层 prefab(根即视图,如 MainUIBuffView/MainUIFightModeView)并 Toggle 显隐。
    /// 顶部状态条/HUD 的浮层类入口(buff 列表/战斗模式…)都走它——避免每个浮层写一个近乎相同的 Flow。
    /// 注册见 <see cref="MainUIOverlayBootstrap"/>(key→Toggle(module,prefab));断线(非游戏内重连)清理。
    /// </summary>
    public static class MainUIOverlay
    {
        private static readonly Dictionary<string, GameObject> _roots = new Dictionary<string, GameObject>();
        private static readonly HashSet<string> _loading = new HashSet<string>();

        /// <summary>切换某浮层(prefab 名作 key):已开则关、未开则开;根上有 BaseView 走 Show/Hide,否则 SetActive。</summary>
        public static async void Toggle(string module, string prefab)
        {
            if (_roots.TryGetValue(prefab, out GameObject go) && go != null)
            {
                BaseView shown = go.GetComponent<BaseView>();
                if (shown != null) { if (shown.IsShown) shown.Hide(); else shown.Show(); }
                else go.SetActive(!go.activeSelf);
                return;
            }

            if (_loading.Contains(prefab)) return;
            _loading.Add(prefab);
            string key = GameResPath.GetUIPrefab(module, prefab);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading.Remove(prefab);

            if (root == null)
            {
                GameLog.Error("MainUI", "浮层 prefab 加载失败: {0}", key);
                return;
            }
            root.name = prefab;
            _roots[prefab] = root;

            BaseView view = root.GetComponent<BaseView>();
            if (view != null) view.Show(); else root.SetActive(true);
            GameLog.Info("MainUI", "浮层打开: {0}", prefab);
        }

        internal static void ResetAll()
        {
            foreach (GameObject go in _roots.Values)
            {
                if (go != null) ResManager.ReleaseInstance(go);
            }
            _roots.Clear();
            _loading.Clear();
        }
    }
}
