// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/gashapon/GashaponMainItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Gashapon
{
    public partial class GashaponMainItemBind : BaseView
    {
        public RectTransform item_group;
        public Image _color;
        public Image luck_bg;
        public TextMeshProUGUI Text;

        protected override void BindNodes()
        {
            EnsureBound(nameof(item_group), item_group);
            EnsureBound(nameof(_color), _color);
            EnsureBound(nameof(luck_bg), luck_bg);
            EnsureBound(nameof(Text), Text);
        }
    }
}
