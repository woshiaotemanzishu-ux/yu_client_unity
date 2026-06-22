// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnQuizSelectItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnQuizSelectItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI desc;
        public Image select_bg;
        public RectTransform _box_click;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(desc), desc);
            EnsureBound(nameof(select_bg), select_bg);
            EnsureBound(nameof(_box_click), _box_click);
        }
    }
}
