// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/activity/CreatRoleGiftView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Activity
{
    public partial class CreatRoleGiftViewBind : BaseView
    {
        public Image _img_tips;
        public ScrollRect _group_item;
        public TextMeshProUGUI _lb_time2;
        public RectTransform _gp_up;
        public TextMeshProUGUI _lb_btn;
        public Image _img_red;
        public RectTransform _box_eff;
        public Image _img_got;
        public GameObject _tpl_AccumRechargeItem;
        public GameObject _tpl_CommonRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_group_item), _group_item);
            EnsureBound(nameof(_lb_time2), _lb_time2);
            EnsureBound(nameof(_gp_up), _gp_up);
            EnsureBound(nameof(_lb_btn), _lb_btn);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_box_eff), _box_eff);
            EnsureBound(nameof(_img_got), _img_got);
            EnsureBound(nameof(_tpl_AccumRechargeItem), _tpl_AccumRechargeItem);
            EnsureBound(nameof(_tpl_CommonRewardItem), _tpl_CommonRewardItem);
        }
    }
}
