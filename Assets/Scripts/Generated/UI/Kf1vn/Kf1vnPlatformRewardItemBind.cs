// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnPlatformRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnPlatformRewardItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI title;
        public ScrollRect _Scroller1;
        public RectTransform Content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
        }
    }
}
