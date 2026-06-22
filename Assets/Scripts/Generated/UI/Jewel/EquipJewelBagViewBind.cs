// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/jewel/EquipJewelBagView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Jewel
{
    public partial class EquipJewelBagViewBind : BaseView
    {
        public Image _Image11;
        public Image _Image2;
        public TextMeshProUGUI _Label1;
        public Image _Image3;
        public Image _btn_up;
        public TextMeshProUGUI lb_up;
        public Image _reddot;
        public Image _btn_off;
        public TextMeshProUGUI lb_off;
        public ScrollRect _Scroller1;
        public RectTransform _Group2;
        public Image _Image4;
        public TextMeshProUGUI _Label2;
        public Image closeBtn;
        public GameObject _tpl_EquipJewelBagItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_btn_up), _btn_up);
            EnsureBound(nameof(lb_up), lb_up);
            EnsureBound(nameof(_reddot), _reddot);
            EnsureBound(nameof(_btn_off), _btn_off);
            EnsureBound(nameof(lb_off), lb_off);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(_tpl_EquipJewelBagItem), _tpl_EquipJewelBagItem);
        }
    }
}
