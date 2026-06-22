// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eudaemon/EudaemonShopItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eudaemon
{
    public partial class EudaemonShopItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_name;
        public RectTransform _box_item;
        public TextMeshProUGUI _html_limit;
        public RectTransform _box_buy;
        public Image _img_buy;
        public TextMeshProUGUI _lb_buy;
        public TextMeshProUGUI _lb_money;
        public Image _img_icon;
        public TextMeshProUGUI _lb_sell_out;
        public Image _img_hot;
        public Image _img_sell_out;
        public TextMeshProUGUI _lb_limit_cond;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_box_item), _box_item);
            EnsureBound(nameof(_html_limit), _html_limit);
            EnsureBound(nameof(_box_buy), _box_buy);
            EnsureBound(nameof(_img_buy), _img_buy);
            EnsureBound(nameof(_lb_buy), _lb_buy);
            EnsureBound(nameof(_lb_money), _lb_money);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_lb_sell_out), _lb_sell_out);
            EnsureBound(nameof(_img_hot), _img_hot);
            EnsureBound(nameof(_img_sell_out), _img_sell_out);
            EnsureBound(nameof(_lb_limit_cond), _lb_limit_cond);
        }
    }
}
