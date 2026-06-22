// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBeast/GodBeastStrenItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBeast
{
    public partial class GodBeastStrenItemBind : BaseView
    {
        public Image _Image1;
        public Image selectImg;
        public TextMeshProUGUI _lb_name;
        public RectTransform itemGp;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(selectImg), selectImg);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(itemGp), itemGp);
        }
    }
}
