// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/transferJob/TransferJobCardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.TransferJob
{
    public partial class TransferJobCardViewBind : BaseView
    {
        public TextMeshProUGUI lblTitle;
        public TextMeshProUGUI lblDesc;
        public ScrollRect listTransfer;
        public RectTransform spClose;
        public GameObject _tpl_TransferJobCardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(lblTitle), lblTitle);
            EnsureBound(nameof(lblDesc), lblDesc);
            EnsureBound(nameof(listTransfer), listTransfer);
            EnsureBound(nameof(spClose), spClose);
            EnsureBound(nameof(_tpl_TransferJobCardItem), _tpl_TransferJobCardItem);
        }
    }
}
