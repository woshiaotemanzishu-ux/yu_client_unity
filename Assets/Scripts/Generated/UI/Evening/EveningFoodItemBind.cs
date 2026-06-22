// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningFoodItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningFoodItemBind : BaseView
    {
        public Image Image;
        public Image img_1;
        public TextMeshProUGUI name;
        public Image img2;
        public Image img_food_icon;
        public TextMeshProUGUI buff;
        public RectTransform btn_cost22;
        public Image btn_cost;
        public Image icon_cost;
        public TextMeshProUGUI cost;
        public TextMeshProUGUI tip;

        protected override void BindNodes()
        {
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(img_1), img_1);
            EnsureBound(nameof(name), name);
            EnsureBound(nameof(img2), img2);
            EnsureBound(nameof(img_food_icon), img_food_icon);
            EnsureBound(nameof(buff), buff);
            EnsureBound(nameof(btn_cost22), btn_cost22);
            EnsureBound(nameof(btn_cost), btn_cost);
            EnsureBound(nameof(icon_cost), icon_cost);
            EnsureBound(nameof(cost), cost);
            EnsureBound(nameof(tip), tip);
        }
    }
}
