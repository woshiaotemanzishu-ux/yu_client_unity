using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.CustomActivity;
using UnityEngine;

namespace Shenxiao.Module.Core.Activity
{
    public static class ActivityFlow
    {
        private const string Module = "activity";
        private const string Prefab = "ActivityModule";
        private static readonly Dictionary<string, int[]> BaseTypes = new Dictionary<string, int[]>
        {
            { "AccumRechargeView", new[] { 7, 14, 124, 125 } },
            { "ConRechargeView", new[] { 109 } },
            { "DailySupplyView", new[] { 61 } },
            { "CreatRoleGiftView", new[] { 122 } },
            { "rechargeReturnView", new[] { 74, 86 } },
        };

        private static GameObject _root;
        private static BaseView _current;
        private static bool _loading;

        public static void Toggle(string viewName)
        {
            if (_current != null && _current.IsShown &&
                string.Equals(_current.GetType().Name, viewName, StringComparison.OrdinalIgnoreCase))
            {
                Close();
                return;
            }
            _ = OpenAsync(viewName, FindEntry(viewName));
        }

        public static void Open(string viewName, CustomActivityModel.ActEntry info) => _ = OpenAsync(viewName, info);

        public static void Close()
        {
            _current?.Hide();
            _current = null;
        }

        private static CustomActivityModel.ActEntry FindEntry(string viewName)
        {
            if (!BaseTypes.TryGetValue(viewName, out int[] bases)) return null;
            foreach (int baseType in bases)
                foreach (CustomActivityModel.ActEntry entry in CustomActivityModel.Instance.ActList.Values)
                    if (entry.BaseType == baseType) return entry;
            return null;
        }

        private static async Task OpenAsync(string viewName, CustomActivityModel.ActEntry info)
        {
            if (_loading) return;
            if (_root == null)
            {
                _loading = true;
                try
                {
                    string key = GameResPath.GetUIPrefab(Module, Prefab);
                    _root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
                    if (_root != null) _root.name = Prefab;
                }
                catch (Exception e) { GameLog.Error("Activity", "ActivityModule load failed: {0}", e.Message); }
                finally { _loading = false; }
            }
            if (_root == null) return;

            BaseView target = null;
            foreach (BaseView view in _root.GetComponentsInChildren<BaseView>(true))
            {
                if (view.transform.parent != _root.transform) continue;
                if (string.Equals(view.GetType().Name, viewName, StringComparison.OrdinalIgnoreCase)) target = view;
                else if (view.IsShown) view.Hide();
                else view.gameObject.SetActive(false);
            }
            if (target == null)
            {
                GameLog.Warn("Activity", "ActivityModule missing runtime page {0}", viewName);
                return;
            }
            _current = target;
            target.Show(info);
        }

        internal static void Reset()
        {
            if (_root != null) ResManager.ReleaseInstance(_root);
            _root = null;
            _current = null;
            _loading = false;
        }
    }
}
