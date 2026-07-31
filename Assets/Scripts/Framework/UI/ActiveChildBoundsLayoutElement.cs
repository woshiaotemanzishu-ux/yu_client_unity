using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.Framework.UI
{
    /// <summary>
    /// 以当前激活的直属子节点边界计算 preferred height。
    /// 用于老 Laya VBox 转换后“标题 + 可变正文/条目容器”的分组节点：视觉偏移仍保存在 Prefab，
    /// 运行时只由正文内容和显隐驱动高度，不需要页面代码写死 section 高度。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ActiveChildBoundsLayoutElement : UIBehaviour, ILayoutElement
    {
        [SerializeField] private float extraBottom;

        private float _preferredHeight;

        public float minWidth => -1f;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public float minHeight => _preferredHeight;
        public float preferredHeight => _preferredHeight;
        public float flexibleHeight => -1f;
        public int layoutPriority => 1;

        public void CalculateLayoutInputHorizontal() { }

        public void CalculateLayoutInputVertical()
        {
            float bottom = 0f;
            RectTransform self = transform as RectTransform;
            if (self == null)
            {
                _preferredHeight = extraBottom;
                return;
            }

            for (int i = 0; i < self.childCount; i++)
            {
                if (!(self.GetChild(i) is RectTransform child) || !child.gameObject.activeInHierarchy)
                    continue;

                float height = LayoutUtility.GetPreferredHeight(child);
                if (height <= 0f) height = child.rect.height;

                // 转换产物的 section 子节点均为顶部锚定；以 pivot 到顶部的距离加可见高度计算底边。
                float childBottom = -child.anchoredPosition.y + child.pivot.y * height;
                bottom = Mathf.Max(bottom, childBottom);
            }

            _preferredHeight = Mathf.Max(0f, bottom + extraBottom);
        }

        public void SetLayoutHorizontal() { }
        public void SetLayoutVertical() { }

        protected override void OnEnable()
        {
            base.OnEnable();
            MarkDirty();
        }

        protected override void OnDisable()
        {
            MarkDirty();
            base.OnDisable();
        }

        private void OnTransformChildrenChanged()
        {
            MarkDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            MarkDirty();
        }
#endif

        private void MarkDirty()
        {
            if (transform is RectTransform rect)
                LayoutRebuilder.MarkLayoutForRebuild(rect);
        }
    }
}
