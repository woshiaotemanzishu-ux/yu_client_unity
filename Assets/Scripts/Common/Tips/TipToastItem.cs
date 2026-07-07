using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Common.Tips
{
    /// <summary>
    /// 单条飘字提示的节点引用(对标老端 MessageItem:_img_tips + _lb_content_html)。
    /// 挂在 TipToastView.prefab 的 ToastTemplate 上,由 TipToastCreator 建树回填。
    /// 样式(底图/字号/颜色/描边)全在 prefab 里手调;运行时只写 text 与功能性宽度自适应。
    /// </summary>
    public sealed class TipToastItem : MonoBehaviour
    {
        public CanvasGroup canvasGroup;   // 出现淡入 / 消失淡出(整条含底图)
        public Image bg;                  // 九宫格底图(mainui_ui_45,宽度按文本自适应)
        public TextMeshProUGUI label;     // 文案(支持 TMP 富文本)
    }
}
