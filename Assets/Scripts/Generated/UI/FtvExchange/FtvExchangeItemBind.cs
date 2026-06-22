// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ftvExchange/FtvExchangeItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FtvExchange
{
    public partial class FtvExchangeItemBind : BaseView
    {
        public RectTransform goods;
        public Image _img_bg;
        public Image _img_top;
        public TextMeshProUGUI _lb_name;
        public RectTransform _gp_reward;
        public RectTransform _gp_goods;
        public RectTransform _gp_btn;
        public Image _Image1;
        public TextMeshProUGUI _lb_exc_num;
        public Image _img_drawed;
        public Image _img_rare;

        protected override void BindNodes()
        {
            EnsureBound(nameof(goods), goods);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_top), _img_top);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_gp_goods), _gp_goods);
            EnsureBound(nameof(_gp_btn), _gp_btn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_exc_num), _lb_exc_num);
            EnsureBound(nameof(_img_drawed), _img_drawed);
            EnsureBound(nameof(_img_rare), _img_rare);
        }
    }
}
