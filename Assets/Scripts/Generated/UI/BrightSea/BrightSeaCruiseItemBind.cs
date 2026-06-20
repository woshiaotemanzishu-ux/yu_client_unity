// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/brightSea/BrightSeaCruiseItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BrightSea
{
    public partial class BrightSeaCruiseItemBind : BaseView
    {
        public Image _bg;
        public RectTransform _con_select_eff;
        public RectTransform _con_root;
        public RectTransform _con_selected;
        public Image _selected_frame;
        public Image _selected_bg2;
        public RectTransform _Content;
        public RectTransform _con_model;
        public RectTransform _con_chip;
        public Image _name_bg;
        public TextMeshProUGUI _lb_name;
        public RectTransform _con_levelup_eff;
        public RectTransform _con_levelup_eff2;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_con_select_eff), _con_select_eff);
            EnsureBound(nameof(_con_root), _con_root);
            EnsureBound(nameof(_con_selected), _con_selected);
            EnsureBound(nameof(_selected_frame), _selected_frame);
            EnsureBound(nameof(_selected_bg2), _selected_bg2);
            EnsureBound(nameof(_Content), _Content);
            EnsureBound(nameof(_con_model), _con_model);
            EnsureBound(nameof(_con_chip), _con_chip);
            EnsureBound(nameof(_name_bg), _name_bg);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_con_levelup_eff), _con_levelup_eff);
            EnsureBound(nameof(_con_levelup_eff2), _con_levelup_eff2);
        }
    }
}
