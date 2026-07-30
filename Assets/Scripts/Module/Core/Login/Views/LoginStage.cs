using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录模块的外部展示舞台。背景图片与铺放方式直接保存在 LoginStage.prefab；
    /// 运行时只按 Prefab 当前选择的图片同步原图宽高比，不加载或替换背景资源。
    /// </summary>
    public sealed class LoginStage : MonoBehaviour
    {
        public Image webBackground;
        public AspectRatioFitter backgroundFitter;
        public RectTransform viewport;

        private void Awake()
        {
            RefreshBackgroundAspect();
        }

        private void OnValidate()
        {
            RefreshBackgroundAspect();
        }

        private void RefreshBackgroundAspect()
        {
            if (webBackground == null || backgroundFitter == null || webBackground.sprite == null)
            {
                return;
            }

            Rect spriteRect = webBackground.sprite.rect;
            if (spriteRect.height > 0f)
            {
                backgroundFitter.aspectRatio = spriteRect.width / spriteRect.height;
            }
        }
    }
}
