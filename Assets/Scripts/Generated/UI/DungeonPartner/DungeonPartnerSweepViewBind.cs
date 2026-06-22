// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPartner/DungeonPartnerSweepView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPartner
{
    public partial class DungeonPartnerSweepViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_bg3;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _html_left;
        public TextMeshProUGUI _html_cost;
        public Image _img_cost;
        public Image _img_tip;
        public TextMeshProUGUI _lb_tip;
        public ScrollRect _list_item;
        public Image _img_close;
        public GameObject _tpl_DungeonPartnerSweepItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_html_left), _html_left);
            EnsureBound(nameof(_html_cost), _html_cost);
            EnsureBound(nameof(_img_cost), _img_cost);
            EnsureBound(nameof(_img_tip), _img_tip);
            EnsureBound(nameof(_lb_tip), _lb_tip);
            EnsureBound(nameof(_list_item), _list_item);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_tpl_DungeonPartnerSweepItem), _tpl_DungeonPartnerSweepItem);
        }
    }
}
