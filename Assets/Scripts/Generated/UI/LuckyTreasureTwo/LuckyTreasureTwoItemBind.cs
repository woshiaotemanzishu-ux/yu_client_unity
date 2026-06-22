// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/luckyTreasureTwo/LuckyTreasureTwoItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.LuckyTreasureTwo
{
    public partial class LuckyTreasureTwoItemBind : BaseView
    {
        public Image Image;
        public RectTransform item_gp;
        public TextMeshProUGUI name_text;
        public Image grade_img;

        protected override void BindNodes()
        {
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(item_gp), item_gp);
            EnsureBound(nameof(name_text), name_text);
            EnsureBound(nameof(grade_img), grade_img);
        }
    }
}
