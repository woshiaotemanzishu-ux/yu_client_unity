// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/transferJob/TransferJobCardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.TransferJob
{
    public partial class TransferJobCardItemBind : BaseView
    {
        public Image btnSure;
        public TextMeshProUGUI lblTransfer;
        public Image imgJob;
        public TextMeshProUGUI lblDesc;
        public TextMeshProUGUI lblType;

        protected override void BindNodes()
        {
            EnsureBound(nameof(btnSure), btnSure);
            EnsureBound(nameof(lblTransfer), lblTransfer);
            EnsureBound(nameof(imgJob), imgJob);
            EnsureBound(nameof(lblDesc), lblDesc);
            EnsureBound(nameof(lblType), lblType);
        }
    }
}
