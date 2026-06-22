// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonTalentSelectItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonTalentSelectItemBind : BaseView
    {
        public RectTransform _gp_item;
        public RectTransform _gp_effect;
        public RectTransform _gp_select;
        public Image _Image1;
        public Image _Image2;
        public Image _red_dot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_gp_effect), _gp_effect);
            EnsureBound(nameof(_gp_select), _gp_select);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_red_dot), _red_dot);
        }
    }
}
