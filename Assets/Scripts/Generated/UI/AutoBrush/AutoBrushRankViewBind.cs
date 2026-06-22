// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/autoBrush/AutoBrushRankView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.AutoBrush
{
    public partial class AutoBrushRankViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_bg3;
        public Image _img_bg4;
        public Image _img_close;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _lb_title2;
        public TextMeshProUGUI _lb_title3;
        public TextMeshProUGUI _lb_title4;
        public TextMeshProUGUI _lb_title5;
        public TextMeshProUGUI _lb_none;
        public ScrollRect _list_item;
        public TextMeshProUGUI _html_my_rank;
        public TextMeshProUGUI _html_my_level;
        public GameObject _tpl_AutoBrushRankItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_img_bg4), _img_bg4);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_lb_title2), _lb_title2);
            EnsureBound(nameof(_lb_title3), _lb_title3);
            EnsureBound(nameof(_lb_title4), _lb_title4);
            EnsureBound(nameof(_lb_title5), _lb_title5);
            EnsureBound(nameof(_lb_none), _lb_none);
            EnsureBound(nameof(_list_item), _list_item);
            EnsureBound(nameof(_html_my_rank), _html_my_rank);
            EnsureBound(nameof(_html_my_level), _html_my_level);
            EnsureBound(nameof(_tpl_AutoBrushRankItem), _tpl_AutoBrushRankItem);
        }
    }
}
