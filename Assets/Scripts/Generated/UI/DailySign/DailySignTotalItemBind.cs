// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dailySign/DailySignTotalItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DailySign
{
    public partial class DailySignTotalItemBind : BaseView
    {
        public RectTransform click_group;
        public Image bg_1;
        public Image bg_2;
        public RectTransform _gp_effect;
        public RectTransform reward_group;
        public RectTransform mask_group;
        public Image _Image1;
        public Image _Image2;
        public Image _Image3;
        public TextMeshProUGUI day_label;
        public Image _red;
        public Image _dot;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(click_group), click_group);
            EnsureBound(nameof(bg_1), bg_1);
            EnsureBound(nameof(bg_2), bg_2);
            EnsureBound(nameof(_gp_effect), _gp_effect);
            EnsureBound(nameof(reward_group), reward_group);
            EnsureBound(nameof(mask_group), mask_group);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(day_label), day_label);
            EnsureBound(nameof(_red), _red);
            EnsureBound(nameof(_dot), _dot);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
