// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBefall/GodBefallRecoveryPropertyItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBefall
{
    public partial class GodBefallRecoveryPropertyItemBind : BaseView
    {
        public TextMeshProUGUI lable_property_cur_name;
        public TextMeshProUGUI lable_property_cur_value;
        public Image img_arrorw;
        public TextMeshProUGUI lable_property_next_value;

        protected override void BindNodes()
        {
            EnsureBound(nameof(lable_property_cur_name), lable_property_cur_name);
            EnsureBound(nameof(lable_property_cur_value), lable_property_cur_value);
            EnsureBound(nameof(img_arrorw), img_arrorw);
            EnsureBound(nameof(lable_property_next_value), lable_property_next_value);
        }
    }
}
