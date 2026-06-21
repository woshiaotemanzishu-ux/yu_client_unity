// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildRenameView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildRenameViewBind : BaseView
    {
        public Image btnClose;
        public Image btnSure;
        public TextMeshProUGUI lblSure;
        public TextMeshProUGUI lblNum;
        public TMP_InputField inputName;
        public TextMeshProUGUI htmlCost;

        protected override void BindNodes()
        {
            EnsureBound(nameof(btnClose), btnClose);
            EnsureBound(nameof(btnSure), btnSure);
            EnsureBound(nameof(lblSure), lblSure);
            EnsureBound(nameof(lblNum), lblNum);
            EnsureBound(nameof(inputName), inputName);
            EnsureBound(nameof(htmlCost), htmlCost);
        }
    }
}
