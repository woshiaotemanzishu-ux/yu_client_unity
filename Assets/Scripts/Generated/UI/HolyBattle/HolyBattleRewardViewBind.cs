// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holyBattle/HolyBattleRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolyBattle
{
    public partial class HolyBattleRewardViewBind : BaseView
    {
        public Image _Image1;
        public Image _img_title;
        public Image _img_close;
        public Image _Image3;
        public Image _img_tips;
        public TextMeshProUGUI _lb_tips;
        public ScrollRect _list_item_con;
        public ScrollRect _list_tab_con;
        public GameObject _tpl_HolyBattleRewardTabItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_list_item_con), _list_item_con);
            EnsureBound(nameof(_list_tab_con), _list_tab_con);
            EnsureBound(nameof(_tpl_HolyBattleRewardTabItem), _tpl_HolyBattleRewardTabItem);
        }
    }
}
