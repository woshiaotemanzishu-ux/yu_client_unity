// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ftvActiveness/FtvActivenessPersonalItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FtvActiveness
{
    public partial class FtvActivenessPersonalItemBind : BaseView
    {
        public RectTransform _gp_btn;
        public RectTransform _gp_awarditem;
        public Image _img_mask;
        public Image _img_received;
        public Image _img_click;
        public Image _Image1;
        public TextMeshProUGUI _lb_dec;
        public Image _red;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_btn), _gp_btn);
            EnsureBound(nameof(_gp_awarditem), _gp_awarditem);
            EnsureBound(nameof(_img_mask), _img_mask);
            EnsureBound(nameof(_img_received), _img_received);
            EnsureBound(nameof(_img_click), _img_click);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_dec), _lb_dec);
            EnsureBound(nameof(_red), _red);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
