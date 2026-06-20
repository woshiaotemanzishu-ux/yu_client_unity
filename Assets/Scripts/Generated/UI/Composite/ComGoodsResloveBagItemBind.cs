// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/ComGoodsResloveBagItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class ComGoodsResloveBagItemBind : BaseView
    {
        public RectTransform equip_con;
        public Image selectBg;
        public RectTransform clickBg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(equip_con), equip_con);
            EnsureBound(nameof(selectBg), selectBg);
            EnsureBound(nameof(clickBg), clickBg);
        }
    }
}
