// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/rune/RuneConvertItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Rune
{
    public partial class RuneConvertItemBind : BaseView
    {
        public Image bg;
        public Image _Image1;
        public RectTransform icon_conta;
        public TextMeshProUGUI goods_name;
        public TextMeshProUGUI pro;
        public TextMeshProUGUI condition;
        public RectTransform btn_conta;
        public Image buyBtn;
        public RectTransform cost_icon;
        public TextMeshProUGUI price;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(icon_conta), icon_conta);
            EnsureBound(nameof(goods_name), goods_name);
            EnsureBound(nameof(pro), pro);
            EnsureBound(nameof(condition), condition);
            EnsureBound(nameof(btn_conta), btn_conta);
            EnsureBound(nameof(buyBtn), buyBtn);
            EnsureBound(nameof(cost_icon), cost_icon);
            EnsureBound(nameof(price), price);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
