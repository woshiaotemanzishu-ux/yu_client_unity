// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/jewel/EquipJewelItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Jewel
{
    public partial class EquipJewelItemBind : BaseView
    {
        public Image bg;
        public Image icon;
        public Image _add;
        public Image @lock;
        public Image _Image1;
        public TextMeshProUGUI tips;
        public Image red_dot;
        public Image arrowImg;
        public RectTransform _gp_eff;
        public RectTransform _gp_click;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(_add), _add);
            EnsureBound(nameof(@lock), @lock);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(red_dot), red_dot);
            EnsureBound(nameof(arrowImg), arrowImg);
            EnsureBound(nameof(_gp_eff), _gp_eff);
            EnsureBound(nameof(_gp_click), _gp_click);
        }
    }
}
