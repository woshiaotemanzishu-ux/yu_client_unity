// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossField/BossFieldVitView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BossField
{
    public partial class BossFieldVitViewBind : BaseView
    {
        public RectTransform _box_con;
        public Image _img_bg;
        public RectTransform _box_time;
        public TextMeshProUGUI _lb_desc;
        public TextMeshProUGUI _lb_time;
        public Image _img_line;
        public RectTransform _box_vitmax;
        public TextMeshProUGUI _lb_desc2;
        public ScrollRect _panel_item;
        public RectTransform _vbox_item;
        public GameObject _tpl_BossFieldVitItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_con), _box_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_box_time), _box_time);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_img_line), _img_line);
            EnsureBound(nameof(_box_vitmax), _box_vitmax);
            EnsureBound(nameof(_lb_desc2), _lb_desc2);
            EnsureBound(nameof(_panel_item), _panel_item);
            EnsureBound(nameof(_vbox_item), _vbox_item);
            EnsureBound(nameof(_tpl_BossFieldVitItem), _tpl_BossFieldVitItem);
        }
    }
}
