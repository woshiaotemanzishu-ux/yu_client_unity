// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/pet/IllusionPropItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Pet
{
    public partial class IllusionPropItemBind : BaseView
    {
        public TextMeshProUGUI prop_text;
        public Image up_arrow;
        public TextMeshProUGUI next_text;

        protected override void BindNodes()
        {
            EnsureBound(nameof(prop_text), prop_text);
            EnsureBound(nameof(up_arrow), up_arrow);
            EnsureBound(nameof(next_text), next_text);
        }
    }
}
