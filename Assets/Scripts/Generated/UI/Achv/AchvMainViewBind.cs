// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/achv/AchvMainView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Achv
{
    public partial class AchvMainViewBind : BaseView
    {
        public RectTransform totalGp;
        public Image ui_bg;
        public RectTransform _box1;
        public Image _roundbg;
        public Image roundImg;
        public RectTransform roundBox;
        public Image _Image6;
        public TextMeshProUGUI totalLb;
        public RectTransform achv_effect;
        public RectTransform _box_progress;
        public RectTransform _box_pg;
        public Image _Image2;
        public Image _pg_bg;
        public ScrollRect _pg_main_panel;
        public RectTransform Content;
        public Image exp_image;
        public RectTransform active_btn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public Image red_dot;
        public RectTransform exp_effect_group;
        public TextMeshProUGUI exp_label;
        public TextMeshProUGUI lv_label;
        public RectTransform _box2;
        public Image _Image3;
        public Image _Image5;
        public ScrollRect _Scroller1;
        public RectTransform Content11;
        public ScrollRect achv_scroller;
        public ScrollRect Content1;
        public ScrollRect _Scroller2;
        public RectTransform Content111;
        public RectTransform dispersionGp;
        public ScrollRect _Scroller3;
        public RectTransform tabScroller;
        public RectTransform Content1111;
        public GameObject _tpl_AchvChildItem;
        public GameObject _tpl_AchvPropItem;
        public GameObject _tpl_AchvTabBar;
        public GameObject _tpl_AchvTabBtn;
        public GameObject _tpl_AchvTabSubBtn;
        public GameObject _tpl_AchvTotalItem;
        public GameObject _tpl_FightingShowSmallItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(totalGp), totalGp);
            EnsureBound(nameof(ui_bg), ui_bg);
            EnsureBound(nameof(_box1), _box1);
            EnsureBound(nameof(_roundbg), _roundbg);
            EnsureBound(nameof(roundImg), roundImg);
            EnsureBound(nameof(roundBox), roundBox);
            EnsureBound(nameof(_Image6), _Image6);
            EnsureBound(nameof(totalLb), totalLb);
            EnsureBound(nameof(achv_effect), achv_effect);
            EnsureBound(nameof(_box_progress), _box_progress);
            EnsureBound(nameof(_box_pg), _box_pg);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_pg_bg), _pg_bg);
            EnsureBound(nameof(_pg_main_panel), _pg_main_panel);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(exp_image), exp_image);
            EnsureBound(nameof(active_btn), active_btn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(exp_effect_group), exp_effect_group);
            EnsureBound(nameof(exp_label), exp_label);
            EnsureBound(nameof(lv_label), lv_label);
            EnsureBound(nameof(_box2), _box2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content11), Content11);
            EnsureBound(nameof(achv_scroller), achv_scroller);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(_Scroller2), _Scroller2);
            EnsureBound(nameof(Content111), Content111);
            EnsureBound(nameof(dispersionGp), dispersionGp);
            EnsureBound(nameof(_Scroller3), _Scroller3);
            EnsureBound(nameof(tabScroller), tabScroller);
            EnsureBound(nameof(Content1111), Content1111);
            EnsureBound(nameof(_tpl_AchvChildItem), _tpl_AchvChildItem);
            EnsureBound(nameof(_tpl_AchvPropItem), _tpl_AchvPropItem);
            EnsureBound(nameof(_tpl_AchvTabBar), _tpl_AchvTabBar);
            EnsureBound(nameof(_tpl_AchvTabBtn), _tpl_AchvTabBtn);
            EnsureBound(nameof(_tpl_AchvTabSubBtn), _tpl_AchvTabSubBtn);
            EnsureBound(nameof(_tpl_AchvTotalItem), _tpl_AchvTotalItem);
            EnsureBound(nameof(_tpl_FightingShowSmallItem), _tpl_FightingShowSmallItem);
        }
    }
}
