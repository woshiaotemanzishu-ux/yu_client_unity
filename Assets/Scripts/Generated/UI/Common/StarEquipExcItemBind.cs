// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/StarEquipExcItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class StarEquipExcItemBind : BaseView
    {
        public Image star;
        public TextMeshProUGUI attLb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(star), star);
            EnsureBound(nameof(attLb), attLb);
        }
    }
}
