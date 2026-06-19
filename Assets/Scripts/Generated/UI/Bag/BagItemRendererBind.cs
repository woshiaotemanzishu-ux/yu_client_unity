// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bag/bagItemRenderer.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bag
{
    public partial class BagItemRendererBind : BaseView
    {
        public RectTransform conta;
        public Image defaultImg;
        public Image up;
        public Image down;
        public Image ban;
        public TextMeshProUGUI grade;
        public Image time_limit;
        public Image @lock;
        public Image redMask;
        public Image _bad_icon;
        public RectTransform star_group;
        public Image star_1;
        public Image star_2;
        public Image star_3;
        public Image star_4;
        public RectTransform group_eff;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(conta), conta);
            EnsureBound(nameof(defaultImg), defaultImg);
            EnsureBound(nameof(up), up);
            EnsureBound(nameof(down), down);
            EnsureBound(nameof(ban), ban);
            EnsureBound(nameof(grade), grade);
            EnsureBound(nameof(time_limit), time_limit);
            EnsureBound(nameof(@lock), @lock);
            EnsureBound(nameof(redMask), redMask);
            EnsureBound(nameof(_bad_icon), _bad_icon);
            EnsureBound(nameof(star_group), star_group);
            EnsureBound(nameof(star_1), star_1);
            EnsureBound(nameof(star_2), star_2);
            EnsureBound(nameof(star_3), star_3);
            EnsureBound(nameof(star_4), star_4);
            EnsureBound(nameof(group_eff), group_eff);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
