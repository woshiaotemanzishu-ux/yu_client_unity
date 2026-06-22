// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/baby/BabyPropItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Baby
{
    public partial class BabyPropItemBind : BaseView
    {
        public TextMeshProUGUI nameLb;
        public TextMeshProUGUI nextLb;
        public Image arrow;

        protected override void BindNodes()
        {
            EnsureBound(nameof(nameLb), nameLb);
            EnsureBound(nameof(nextLb), nextLb);
            EnsureBound(nameof(arrow), arrow);
        }
    }
}
