using System;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// BagModule 内 Activity 子窗共用的全屏背景。节点与图片保存在 Prefab；运行时只切显隐和绑定“点背景关闭”。
    /// 对标老端 BaseView1.use_background + click_bg_toClose。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BagActivityModalLayout : MonoBehaviour
    {
        [SerializeField] private Image blocker;

        public Image Blocker => blocker;

        public void Show(BaseView activeView, Action close)
        {
            if (blocker == null) return;
            PlaceDirectlyBelow(activeView);
            UIUtil.ClearClicks(blocker);
            UIUtil.AddClick(blocker, close);
            blocker.gameObject.SetActive(true);
        }

        /// <summary>共享遮罩始终位于当前最上层 Activity 子窗正下方，避免点击上层外侧穿透到底层子窗。</summary>
        private void PlaceDirectlyBelow(BaseView activeView)
        {
            if (activeView == null || blocker == null || blocker.transform.parent == null) return;

            Transform sibling = activeView.transform;
            Transform commonParent = blocker.transform.parent;
            while (sibling != null && sibling.parent != commonParent) sibling = sibling.parent;
            if (sibling == null) return;

            sibling.SetAsLastSibling();
            int siblingIndex = sibling.GetSiblingIndex();
            int blockerIndex = blocker.transform.GetSiblingIndex();
            int targetIndex = blockerIndex < siblingIndex ? siblingIndex - 1 : siblingIndex;
            blocker.transform.SetSiblingIndex(targetIndex);
        }

        public void Hide()
        {
            if (blocker == null) return;
            UIUtil.ClearClicks(blocker);
            blocker.gameObject.SetActive(false);
        }
    }
}
