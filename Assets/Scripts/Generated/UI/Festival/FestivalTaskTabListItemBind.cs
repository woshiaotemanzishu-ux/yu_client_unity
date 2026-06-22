// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/festival/FestivalTaskTabListItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Festival
{
    public partial class FestivalTaskTabListItemBind : BaseView
    {
        public RectTransform itemBox;
        public Image iconImg;
        public TextMeshProUGUI descLab;
        public Image redImg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(itemBox), itemBox);
            EnsureBound(nameof(iconImg), iconImg);
            EnsureBound(nameof(descLab), descLab);
            EnsureBound(nameof(redImg), redImg);
        }
    }
}
