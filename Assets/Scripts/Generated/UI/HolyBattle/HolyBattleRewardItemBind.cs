// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holyBattle/HolyBattleRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolyBattle
{
    public partial class HolyBattleRewardItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_desc;
        public ScrollRect _list_award_con;
        public RectTransform _gp_get;
        public Image _Image1;
        public TextMeshProUGUI _lb_get;
        public Image _img_red;
        public Image _img_get;
        public GameObject _tpl_CommonRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_list_award_con), _list_award_con);
            EnsureBound(nameof(_gp_get), _gp_get);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_get), _lb_get);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_img_get), _img_get);
            EnsureBound(nameof(_tpl_CommonRewardItem), _tpl_CommonRewardItem);
        }
    }
}
