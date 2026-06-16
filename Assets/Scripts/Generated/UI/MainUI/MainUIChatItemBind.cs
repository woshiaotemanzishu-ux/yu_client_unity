// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIChatItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIChatItemBind : BaseView
    {
        public RectTransform _Group1;
        public RectTransform _gp_title;
        public Image titleBg;
        public TextMeshProUGUI title;
        public RectTransform _gp_content;
        public TextMeshProUGUI contentLabel;
        public Image _img_trumpet;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_gp_title), _gp_title);
            EnsureBound(nameof(titleBg), titleBg);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(_gp_content), _gp_content);
            EnsureBound(nameof(contentLabel), contentLabel);
            EnsureBound(nameof(_img_trumpet), _img_trumpet);
        }
    }
}
