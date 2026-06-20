// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/pet/OutWardExpGoods.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Pet
{
    public partial class OutWardExpGoodsBind : BaseView
    {
        public RectTransform item_gp;
        public RectTransform effect_gp;
        public TextMeshProUGUI item_name;
        public Image click;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(item_gp), item_gp);
            EnsureBound(nameof(effect_gp), effect_gp);
            EnsureBound(nameof(item_name), item_name);
            EnsureBound(nameof(click), click);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
