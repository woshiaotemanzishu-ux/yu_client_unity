// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/invest/MonthCardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Invest
{
    public partial class MonthCardViewBind : BaseView
    {
        public TextMeshProUGUI tip;
        public TextMeshProUGUI _lb_tips;
        public RectTransform reward_gp;
        public RectTransform item_gp;
        public RectTransform _gp_get;
        public Image _img_btn;
        public RectTransform gp1;
        public TextMeshProUGUI _lb_desc;
        public Image _img_red;
        public GameObject _tpl_MonthCardItem;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(tip), tip);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(reward_gp), reward_gp);
            EnsureBound(nameof(item_gp), item_gp);
            EnsureBound(nameof(_gp_get), _gp_get);
            EnsureBound(nameof(_img_btn), _img_btn);
            EnsureBound(nameof(gp1), gp1);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_tpl_MonthCardItem), _tpl_MonthCardItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
