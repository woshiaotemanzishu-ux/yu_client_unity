// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonTalentSelectShow.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonTalentSelectShowBind : BaseView
    {
        public Image _Image1;
        public RectTransform _gp_item;
        public Image _Image2;
        public TextMeshProUGUI _lb_name;
        public RectTransform _gp_fight;
        public Image _Image3;
        public RectTransform _Group1;
        public Image _img_bg;
        public TextMeshProUGUI _lb_state;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public TextMeshProUGUI _lb_desc;
        public GameObject _tpl_FightingShowSmallItem;
        public GameObject _tpl_DemonTalentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_gp_fight), _gp_fight);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_state), _lb_state);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_tpl_FightingShowSmallItem), _tpl_FightingShowSmallItem);
            EnsureBound(nameof(_tpl_DemonTalentItem), _tpl_DemonTalentItem);
        }
    }
}
