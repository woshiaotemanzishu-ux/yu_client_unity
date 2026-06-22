// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnShopView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnShopViewBind : BaseView
    {
        public RectTransform _Group3;
        public Image bg_img;
        public Image img_title;
        public TextMeshProUGUI lb_title;
        public RectTransform sub_conta;
        public ScrollRect _list_tab_con;
        public RectTransform closeBtn;
        public Image btn_img;
        public GameObject _tpl_Kf1vnQuizHistoryView;
        public GameObject _tpl_Kf1vnQuizHistoryItem;
        public GameObject _tpl_HolyBattleRewardTabItem;
        public GameObject _tpl_Kf1vnTabItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group3), _Group3);
            EnsureBound(nameof(bg_img), bg_img);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(lb_title), lb_title);
            EnsureBound(nameof(sub_conta), sub_conta);
            EnsureBound(nameof(_list_tab_con), _list_tab_con);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(btn_img), btn_img);
            EnsureBound(nameof(_tpl_Kf1vnQuizHistoryView), _tpl_Kf1vnQuizHistoryView);
            EnsureBound(nameof(_tpl_Kf1vnQuizHistoryItem), _tpl_Kf1vnQuizHistoryItem);
            EnsureBound(nameof(_tpl_HolyBattleRewardTabItem), _tpl_HolyBattleRewardTabItem);
            EnsureBound(nameof(_tpl_Kf1vnTabItem), _tpl_Kf1vnTabItem);
        }
    }
}
