// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/revelation/RevelationDevourPropItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Revelation
{
    public partial class RevelationDevourPropItemBind : BaseView
    {
        public TextMeshProUGUI curLb;
        public TextMeshProUGUI nextLb;
        public Image arrow;

        protected override void BindNodes()
        {
            EnsureBound(nameof(curLb), curLb);
            EnsureBound(nameof(nextLb), nextLb);
            EnsureBound(nameof(arrow), arrow);
        }
    }
}
