// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/achv/AchvPropItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Achv
{
    public partial class AchvPropItemBind : BaseView
    {
        public RectTransform _Group1;
        public TextMeshProUGUI name_label;
        public TextMeshProUGUI cur_value;
        public Image arrow_img;
        public TextMeshProUGUI name_label1;
        public TextMeshProUGUI next_value;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(name_label), name_label);
            EnsureBound(nameof(cur_value), cur_value);
            EnsureBound(nameof(arrow_img), arrow_img);
            EnsureBound(nameof(name_label1), name_label1);
            EnsureBound(nameof(next_value), next_value);
        }
    }
}
