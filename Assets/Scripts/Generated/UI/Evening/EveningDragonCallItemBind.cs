// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningDragonCallItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningDragonCallItemBind : BaseView
    {
        public Image _Image11;
        public RectTransform _gp_item;
        public TextMeshProUGUI _lb_name;
        public RectTransform _btn_use;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public Image _reddot;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_btn_use), _btn_use);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_reddot), _reddot);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
