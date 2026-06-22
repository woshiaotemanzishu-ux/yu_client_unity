// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkShopItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkShopItemBind : BaseView
    {
        public Image imgLine;
        public Image btnBuy;
        public Image imgMoney;
        public TextMeshProUGUI lblPrice;
        public TextMeshProUGUI lblName;
        public TextMeshProUGUI htmlLimit;
        public Image goodsNode;

        protected override void BindNodes()
        {
            EnsureBound(nameof(imgLine), imgLine);
            EnsureBound(nameof(btnBuy), btnBuy);
            EnsureBound(nameof(imgMoney), imgMoney);
            EnsureBound(nameof(lblPrice), lblPrice);
            EnsureBound(nameof(lblName), lblName);
            EnsureBound(nameof(htmlLimit), htmlLimit);
            EnsureBound(nameof(goodsNode), goodsNode);
        }
    }
}
