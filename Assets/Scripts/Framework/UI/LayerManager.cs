using System.Collections.Generic;
using UnityEngine;

namespace Shenxiao.Framework.UI
{
    /// <summary>
    /// Holds the root Canvas and per-layer parents. Created once at launch by ViewManager.
    /// </summary>
    public class LayerManager
    {
        private readonly Dictionary<UILayer, Transform> _layers = new Dictionary<UILayer, Transform>();
        private Canvas _rootCanvas;

        public Canvas RootCanvas => _rootCanvas;

        public void Init(Canvas rootCanvas)
        {
            _rootCanvas = rootCanvas;
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                var go = new GameObject(layer.ToString(), typeof(RectTransform));
                go.transform.SetParent(rootCanvas.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _layers[layer] = go.transform;
            }
        }

        public Transform GetLayer(UILayer layer)
        {
            return _layers.TryGetValue(layer, out var t) ? t : _rootCanvas?.transform;
        }
    }
}
