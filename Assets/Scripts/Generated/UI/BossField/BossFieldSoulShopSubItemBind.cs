// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossField/BossFieldSoulShopSubItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BossField
{
    public partial class BossFieldSoulShopSubItemBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _box_icon;
        public Image _img_quota;
        public TextMeshProUGUI _lb_quota;
        public TextMeshProUGUI _lb_name;
        public Image _img_icon;
        public TextMeshProUGUI _lb_prive;
        public RectTransform _box_buy;
        public Image _img_buy;
        public TextMeshProUGUI _lb_buy;
        public RectTransform _box_soldout;
        public TextMeshProUGUI _lb_soldout;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_box_icon), _box_icon);
            EnsureBound(nameof(_img_quota), _img_quota);
            EnsureBound(nameof(_lb_quota), _lb_quota);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_lb_prive), _lb_prive);
            EnsureBound(nameof(_box_buy), _box_buy);
            EnsureBound(nameof(_img_buy), _img_buy);
            EnsureBound(nameof(_lb_buy), _lb_buy);
            EnsureBound(nameof(_box_soldout), _box_soldout);
            EnsureBound(nameof(_lb_soldout), _lb_soldout);
        }
    }
}
