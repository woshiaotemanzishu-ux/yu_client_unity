using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Framework.UI
{
    /// <summary>
    /// Central UI manager. Open / close views by type. Each type maps to one prefab Addressable key
    /// derived from a [UIView] attribute on the *Bind class.
    /// </summary>
    public static class ViewManager
    {
        private static LayerManager _layers;
        private static readonly Dictionary<Type, BaseView> _views = new Dictionary<Type, BaseView>();

        public static void Init(LayerManager layers)
        {
            _layers = layers;
        }

        /// <summary>给"模块合并 prefab"这类自管窗口的流程取层节点(如登录模块整体挂 Window 层)。</summary>
        public static Transform GetLayer(UILayer layer)
        {
            return _layers?.GetLayer(layer);
        }

        public static async Task<T> Open<T>(object args = null) where T : BaseView
        {
            if (_views.TryGetValue(typeof(T), out var existed))
            {
                existed.InternalShow(args);
                return (T)existed;
            }

            string addrKey = ResolveAddrKey(typeof(T));
            if (string.IsNullOrEmpty(addrKey))
            {
                GameLog.Error("UI", "no UIView attribute on {0}", typeof(T).Name);
                return null;
            }

            var go = await ResManager.InstantiateAsync(addrKey, _layers?.GetLayer(UILayer.Window));
            if (go == null) return null;

            var view = go.GetComponent<T>();
            if (view == null)
            {
                GameLog.Error("UI", "prefab {0} missing component {1}", addrKey, typeof(T).Name);
                ResManager.ReleaseInstance(go);
                return null;
            }

            // Re-parent to the correct layer based on the view's preference.
            var parent = _layers?.GetLayer(view.Layer);
            if (parent != null) go.transform.SetParent(parent, false);

            _views[typeof(T)] = view;
            view.InternalShow(args);
            return view;
        }

        public static void Close<T>() where T : BaseView
        {
            if (!_views.TryGetValue(typeof(T), out var view)) return;
            view.InternalHide();
        }

        public static void Dispose<T>() where T : BaseView
        {
            if (!_views.TryGetValue(typeof(T), out var view)) return;
            view.InternalHide();
            view.InternalDispose();
            _views.Remove(typeof(T));
            if (view.gameObject != null) ResManager.ReleaseInstance(view.gameObject);
        }

        public static T Get<T>() where T : BaseView
        {
            return _views.TryGetValue(typeof(T), out var v) ? v as T : null;
        }

        private static string ResolveAddrKey(Type t)
        {
            var attrs = t.GetCustomAttributes(typeof(UIViewAttribute), true);
            return attrs.Length > 0 ? ((UIViewAttribute)attrs[0]).AddrKey : null;
        }
    }

    /// <summary>
    /// Marks a view class with its prefab Addressable key.
    /// UI generators emit this attribute on the generated *Bind class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class UIViewAttribute : Attribute
    {
        public string AddrKey { get; }
        public UIViewAttribute(string addrKey) { AddrKey = addrKey; }
    }
}
