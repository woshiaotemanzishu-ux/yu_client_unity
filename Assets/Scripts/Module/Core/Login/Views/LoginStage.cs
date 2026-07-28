using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录模块的外部展示舞台。全屏 Web 背景与居中 720 宽自适应高度视口都在这里,
    /// 六个原有登录 prefab 只作为视口子节点加载:长竖屏纵向铺满,横向宽屏只补左右。
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
