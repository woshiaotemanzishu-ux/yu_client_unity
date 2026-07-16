using UnityEngine;

namespace Shenxiao.Framework.UI
{
    /// <summary>
    /// 把挂载节点的 RectTransform 收缩到设备「安全区」内(避开刘海 / 挖孔 / 灵动岛 / 底部手势条),
    /// 让钉在屏幕极边的 HUD(顶栏 / 技能栏 / 摇杆)不被遮挡。
    ///
    /// 用法/约束:
    /// - 节点必须是「铺满整块画布」的 RectTransform 的直接子级。因为安全区换算用 Screen 像素归一化成
    ///   锚点(anchorMin/Max),只有当本节点对齐整屏时数学才成立。LayerManager 对 Main 层自动挂本组件。
    /// - 背景 / 满铺遮罩不要挂本组件——它们要全屏出血、顶到真实屏幕边缘,否则刘海机上会露黑边。
    /// - 编辑器 / 无刘海设备上 Screen.safeArea == 整屏,本组件是 no-op(锚点仍是 0~1),不影响既有截图验证。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaRoot : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastArea;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _rt = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            // 只在 转屏 / 分辨率变化 / 安全区变化 时重算,平时零成本。
            if (Screen.safeArea != _lastArea
                || Screen.width != _lastScreen.x
                || Screen.height != _lastScreen.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rt == null) _rt = (RectTransform)transform;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect area = Screen.safeArea;
            _lastArea = area;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            Vector2 min = area.position;                 // 安全区左下角(屏幕像素)
            Vector2 max = area.position + area.size;      // 安全区右上角(屏幕像素)
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            // 兜底:某些机型初始化早期会给出异常安全区,避免把界面缩没/顶出屏幕。
            if (!IsSane(min, max)) return;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }

        private static bool IsSane(Vector2 min, Vector2 max)
        {
            return min.x >= 0f && min.y >= 0f && max.x <= 1f && max.y <= 1f
                && max.x - min.x > 0.5f && max.y - min.y > 0.5f;
        }
    }
}
