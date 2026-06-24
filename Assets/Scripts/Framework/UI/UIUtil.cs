using System;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Framework.UI
{
    /// <summary>
    /// UI 交互小助手,对标 Laya 的 Util.AddClickEvent:
    /// 转换器产出的 Image/Label/Box 默认不带 Button,业务接交互统一走这里,
    /// 不要在业务代码里手挂组件,也不要在各 View 重复写"找 Graphic / 补透明 Image"的样板。
    /// </summary>
    public static class UIUtil
    {
        /// <summary>给任意 Graphic(Image/TMP 文本)加点击;自动开 raycastTarget 并复用已有 Button。</summary>
        public static void AddClick(Graphic target, Action onClick)
        {
            if (target == null)
            {
                Debug.LogError("[UIUtil] AddClick 目标为空");
                return;
            }
            // raycastTarget=true 即可吃射线;颜色 alpha=0 不影响命中(uGUI 默认按整个 rect 命中,与色值透明无关),
            // 所以透明命中体照样可点,无需特判 alpha。
            target.raycastTarget = true;
            Button btn = target.GetComponent<Button>();
            if (btn == null)
            {
                btn = target.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
            }
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        /// <summary>
        /// 给任意 UI 组件加点击,自动解析命中体(把各 MainUI View 里重复的 RouteClick/BindNav/BindClick 收口到这里)。
        /// target 可能是 Image/TMP 文本/ScrollRect/RectTransform 或任意 Component:
        /// - 本身是 Graphic,或自身挂了 Graphic → 直接用它作命中体(被禁用则启用,否则它不进 GraphicRegistry、扫不到);
        /// - 是纯容器(只有 RectTransform 的 Box / ScrollRect 等,自身无 Graphic)→ 在容器自身补一张透明全幅 Image
        ///   作命中体(整盒可点,对标 Laya Box.AddClickEvent);
        /// - 容器自身无尺寸(纯布局盒)→ 退用已有子 Graphic,避免命中体零面积点不到。
        /// </summary>
        public static void AddClick(Component target, Action onClick)
        {
            if (target == null)
            {
                Debug.LogError("[UIUtil] AddClick 目标为空");
                return;
            }

            Graphic graphic = target as Graphic;
            if (graphic == null)
            {
                graphic = target.GetComponent<Graphic>();
            }

            if (graphic == null)
            {
                graphic = ResolveContainerRaycastTarget(target);
            }
            else if (!graphic.enabled)
            {
                // 被禁用的 Graphic 不会注册到 GraphicRegistry → 射线扫不到它,点击永不触发,先启用。
                graphic.enabled = true;
            }

            AddClick(graphic, onClick);
        }

        public static void AddClick(GameObject target, Action onClick)
        {
            if (target == null)
            {
                Debug.LogError("[UIUtil] AddClick 目标为空");
                return;
            }

            AddClick(target.transform, onClick);
        }

        public static void ClearClicks(Graphic target)
        {
            Button btn = target != null ? target.GetComponent<Button>() : null;
            if (btn != null) btn.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// 容器(自身无 Graphic)解析命中体:容器自身有尺寸 → 补一张透明全幅 Image(整盒可点);
        /// 自身零尺寸(纯布局盒,如某些自动撑开的 HBox/VBox) → 改用已有子 Graphic,避免命中体零面积点不到。
        /// </summary>
        private static Graphic ResolveContainerRaycastTarget(Component target)
        {
            RectTransform rt = target.transform as RectTransform;
            if (rt != null && (rt.rect.width <= 0f || rt.rect.height <= 0f))
            {
                Graphic child = target.GetComponentInChildren<Graphic>(true);
                if (child != null) return child;
            }
            return EnsureTransparentRaycastImage(target.gameObject);
        }

        /// <summary>在节点自身挂一张透明 Image 作命中体(alpha 0 不显形但可吃射线);已有则复用。</summary>
        private static Image EnsureTransparentRaycastImage(GameObject go)
        {
            Image image = go.GetComponent<Image>();
            if (image == null) image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            return image;
        }
    }
}
