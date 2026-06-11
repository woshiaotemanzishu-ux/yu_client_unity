using System;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Framework.UI
{
    /// <summary>
    /// UI 交互小助手,对标 Laya 的 Util.AddClickEvent:
    /// 转换器产出的 Image/Label 默认不带 Button,业务接交互统一走这里,
    /// 不要在业务代码里手挂组件。
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
            target.raycastTarget = true;
            Button btn = target.GetComponent<Button>();
            if (btn == null)
            {
                btn = target.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
            }
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        public static void ClearClicks(Graphic target)
        {
            Button btn = target != null ? target.GetComponent<Button>() : null;
            if (btn != null) btn.onClick.RemoveAllListeners();
        }
    }
}
