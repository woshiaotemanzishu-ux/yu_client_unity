// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonCommon/DungeonFailureView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonCommon
{
    public partial class DungeonFailureViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_title;
        public RectTransform _box_info1;
        public ScrollRect goods_list;
        public RectTransform _box3;
        public TextMeshProUGUI _html_desc;
        public TextMeshProUGUI _html_desc2;
        public RectTransform _box1;
        public Image _img_title_bg;
        public Image _img_title3;
        public Image _img_title2;
        public ScrollRect _gp_streng;
        public TextMeshProUGUI _desc;
        public RectTransform _box2;
        public Image _img_activity;
        public RectTransform goBtn;
        public RectTransform _btn_close;
        public TextMeshProUGUI _confiim;
        public GameObject _tpl_DungeonFailureStrengItem;
        public GameObject _tpl_CommonRewardItem;
        public GameObject _tpl_CongratulationObtainItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_box_info1), _box_info1);
            EnsureBound(nameof(goods_list), goods_list);
            EnsureBound(nameof(_box3), _box3);
            EnsureBound(nameof(_html_desc), _html_desc);
            EnsureBound(nameof(_html_desc2), _html_desc2);
            EnsureBound(nameof(_box1), _box1);
            EnsureBound(nameof(_img_title_bg), _img_title_bg);
            EnsureBound(nameof(_img_title3), _img_title3);
            EnsureBound(nameof(_img_title2), _img_title2);
            EnsureBound(nameof(_gp_streng), _gp_streng);
            EnsureBound(nameof(_desc), _desc);
            EnsureBound(nameof(_box2), _box2);
            EnsureBound(nameof(_img_activity), _img_activity);
            EnsureBound(nameof(goBtn), goBtn);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_confiim), _confiim);
            EnsureBound(nameof(_tpl_DungeonFailureStrengItem), _tpl_DungeonFailureStrengItem);
            EnsureBound(nameof(_tpl_CommonRewardItem), _tpl_CommonRewardItem);
            EnsureBound(nameof(_tpl_CongratulationObtainItem), _tpl_CongratulationObtainItem);
        }
    }
}
