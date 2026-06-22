// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/activity/AccumRechargeView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Activity
{
    public partial class AccumRechargeViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_title;
        public Image _titleBg;
        public Image bg;
        public RectTransform _gp_tips;
        public Image _Image1;
        public TextMeshProUGUI _Label1;
        public Image time_bg;
        public RectTransform _gp_time;
        public ScrollRect _group_item;
        public TextMeshProUGUI _lb_desc;
        public GameObject _tpl_AccumRechargeItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_titleBg), _titleBg);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_gp_tips), _gp_tips);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(time_bg), time_bg);
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(_group_item), _group_item);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_tpl_AccumRechargeItem), _tpl_AccumRechargeItem);
        }
    }
}
