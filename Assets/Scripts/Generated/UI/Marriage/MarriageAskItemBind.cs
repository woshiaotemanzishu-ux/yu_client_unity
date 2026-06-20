// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageAskItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageAskItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_select;
        public Image _img_ring;
        public Image _img_name;
        public Image _img_cost;
        public TextMeshProUGUI label1;
        public TextMeshProUGUI _lb_c;
        public TextMeshProUGUI _lb_cost;
        public TextMeshProUGUI _lb_en;
        public TextMeshProUGUI _lb_nickname;
        public TextMeshProUGUI _lb_award;
        public RectTransform _group_dsgt;
        public ScrollRect scroll;
        public RectTransform _group_item;
        public Image _img_dsgt;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_img_ring), _img_ring);
            EnsureBound(nameof(_img_name), _img_name);
            EnsureBound(nameof(_img_cost), _img_cost);
            EnsureBound(nameof(label1), label1);
            EnsureBound(nameof(_lb_c), _lb_c);
            EnsureBound(nameof(_lb_cost), _lb_cost);
            EnsureBound(nameof(_lb_en), _lb_en);
            EnsureBound(nameof(_lb_nickname), _lb_nickname);
            EnsureBound(nameof(_lb_award), _lb_award);
            EnsureBound(nameof(_group_dsgt), _group_dsgt);
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(_group_item), _group_item);
            EnsureBound(nameof(_img_dsgt), _img_dsgt);
        }
    }
}
