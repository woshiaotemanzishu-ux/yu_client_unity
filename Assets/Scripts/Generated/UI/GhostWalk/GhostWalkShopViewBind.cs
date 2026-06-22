// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkShopView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkShopViewBind : BaseView
    {
        public Image imgBg;
        public Image imgHead;
        public Image imgMoney;
        public Image btnClose;
        public TextMeshProUGUI lblNum;
        public TextMeshProUGUI lblRefreshTime;
        public ScrollRect listGoods;
        public GameObject _tpl_GhostWalkShopItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(imgBg), imgBg);
            EnsureBound(nameof(imgHead), imgHead);
            EnsureBound(nameof(imgMoney), imgMoney);
            EnsureBound(nameof(btnClose), btnClose);
            EnsureBound(nameof(lblNum), lblNum);
            EnsureBound(nameof(lblRefreshTime), lblRefreshTime);
            EnsureBound(nameof(listGoods), listGoods);
            EnsureBound(nameof(_tpl_GhostWalkShopItem), _tpl_GhostWalkShopItem);
        }
    }
}
