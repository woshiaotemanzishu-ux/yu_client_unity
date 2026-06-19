// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bag/OneKeyUseTab.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bag
{
    public partial class OneKeyUseTabBind : BaseView
    {
        public RectTransform _Group1;
        public Image Unselect;
        public Image Select;
        public TextMeshProUGUI nameLb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(Unselect), Unselect);
            EnsureBound(nameof(Select), Select);
            EnsureBound(nameof(nameLb), nameLb);
        }
    }
}
