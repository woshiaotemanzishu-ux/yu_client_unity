using UnityEngine;

namespace Shenxiao.Framework.UI
{
    /// <summary>
    /// 让一个 RectTransform 覆盖 root Canvas 的完整逻辑矩形，即使它位于固定尺寸的弹窗根下。
    /// 用于 Web 宽屏/移动长屏下的模态遮罩；弹窗本体仍保留 Prefab 中的人工尺寸与坐标。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class RootCanvasRectFitter : MonoBehaviour
    {
        private readonly Vector3[] _worldCorners = new Vector3[4];
        private Vector2 _lastRootSize = new Vector2(-1f, -1f);
        private Vector2Int _lastScreen = new Vector2Int(-1, -1);
        private bool _applying;

        private void OnEnable() => RefreshNow();
        private void OnTransformParentChanged() => RefreshNow();
        private void OnCanvasHierarchyChanged() => RefreshNow();
        private void OnRectTransformDimensionsChange() => RefreshNow();

        private void LateUpdate()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform root = canvas != null && canvas.rootCanvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : null;
            if (root == null) return;

            Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
            if ((root.rect.size - _lastRootSize).sqrMagnitude > 0.01f || screen != _lastScreen)
                RefreshNow();
        }

        public void RefreshNow()
        {
            if (_applying) return;
            RectTransform rect = transform as RectTransform;
            RectTransform parent = rect != null ? rect.parent as RectTransform : null;
            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform root = canvas != null && canvas.rootCanvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : null;
            if (rect == null || parent == null || root == null) return;

            _applying = true;
            root.GetWorldCorners(_worldCorners);
            Vector3 bottomLeft = parent.InverseTransformPoint(_worldCorners[0]);
            Vector3 topRight = parent.InverseTransformPoint(_worldCorners[2]);
            Vector2 size = new Vector2(
                Mathf.Abs(topRight.x - bottomLeft.x),
                Mathf.Abs(topRight.y - bottomLeft.y));
            Vector2 center = new Vector2(
                (bottomLeft.x + topRight.x) * 0.5f,
                (bottomLeft.y + topRight.y) * 0.5f);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center - parent.rect.center;
            rect.sizeDelta = size;

            _lastRootSize = root.rect.size;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            _applying = false;
        }
    }
}
