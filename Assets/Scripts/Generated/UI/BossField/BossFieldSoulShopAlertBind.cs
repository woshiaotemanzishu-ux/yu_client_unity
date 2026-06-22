// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossField/BossFieldSoulShopAlert.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BossField
{
    public partial class BossFieldSoulShopAlertBind : BaseView
    {
        public Image _img_bg;
        public Image _img_title;
        public Image _img_close;
        public RectTransform _box_icon;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_have;
        public TextMeshProUGUI _lb_desc;
        public RectTransform _box_cancel;
        public Image _img_cancel;
        public TextMeshProUGUI _lb_cancel;
        public RectTransform _box_use;
        public Image _img_use;
        public TextMeshProUGUI _lb_use;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_box_icon), _box_icon);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_have), _lb_have);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_box_cancel), _box_cancel);
            EnsureBound(nameof(_img_cancel), _img_cancel);
            EnsureBound(nameof(_lb_cancel), _lb_cancel);
            EnsureBound(nameof(_box_use), _box_use);
            EnsureBound(nameof(_img_use), _img_use);
            EnsureBound(nameof(_lb_use), _lb_use);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
