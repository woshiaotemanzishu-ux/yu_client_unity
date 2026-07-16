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

        // 需要避开刘海 / 挖孔 / 灵动岛 / 底部手势条的层:HUD 元素钉在屏幕极边(顶栏 / 技能栏 / 摇杆),
        // 必须内缩到安全区。Scene(3D 场景背景)保持全屏出血;Window/Popup 多为居中且自带满铺遮罩,
        // 若整层内缩会让遮罩盖不住刘海角——如需让窗口内容避让,应在窗口 prefab 内部单挂 SafeAreaRoot,
        // 而非在此整层内缩。
        private static readonly HashSet<UILayer> SafeAreaLayers = new HashSet<UILayer> { UILayer.Main };

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
                // 该层锚点改由 SafeAreaRoot 按设备安全区实时内缩(无刘海设备上是 no-op)。
                if (SafeAreaLayers.Contains(layer))
                    go.AddComponent<SafeAreaRoot>();
                _layers[layer] = go.transform;
            }
        }

        public Transform GetLayer(UILayer layer)
        {
            return _layers.TryGetValue(layer, out var t) ? t : _rootCanvas?.transform;
        }
    }
}
