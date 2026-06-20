// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/rune/RunePropertyItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Rune
{
    public partial class RunePropertyItemBind : BaseView
    {
        public TextMeshProUGUI pro_name;
        public TextMeshProUGUI value;

        protected override void BindNodes()
        {
            EnsureBound(nameof(pro_name), pro_name);
            EnsureBound(nameof(value), value);
        }
    }
}
