// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/logGift/LogGiftView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.LogGift
{
    public partial class LogGiftViewBind : BaseView
    {
        public Image _img_001;
        public Image _img_002;
        public Image _img_004;
        public Image _Image1;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public RectTransform _gp_time;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_001), _img_001);
            EnsureBound(nameof(_img_002), _img_002);
            EnsureBound(nameof(_img_004), _img_004);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_gp_time), _gp_time);
        }
    }
}
