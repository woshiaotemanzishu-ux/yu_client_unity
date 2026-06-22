// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonRotaryPreView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonRotaryPreViewBind : BaseView
    {
        public Image _Image1;
        public Image _close;
        public Image _Image3;
        public Image title_bg;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public GameObject _tpl_DemonRotaryPreItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_close), _close);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(title_bg), title_bg);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_DemonRotaryPreItem), _tpl_DemonRotaryPreItem);
        }
    }
}
