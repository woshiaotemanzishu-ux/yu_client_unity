// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildMergeItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildMergeItemBind : BaseView
    {
        public RectTransform gTop;
        public Image imgTop;
        public RectTransform btnBox;
        public Image btnMerge;
        public TextMeshProUGUI lblMerge;
        public Image btnRefuse;
        public TextMeshProUGUI lblRefuse;
        public TextMeshProUGUI lblState;
        public TextMeshProUGUI lblGuildName;
        public TextMeshProUGUI lblGuildLevel;
        public TextMeshProUGUI lblMaster;
        public TextMeshProUGUI lblNum;
        public TextMeshProUGUI lblScore;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gTop), gTop);
            EnsureBound(nameof(imgTop), imgTop);
            EnsureBound(nameof(btnBox), btnBox);
            EnsureBound(nameof(btnMerge), btnMerge);
            EnsureBound(nameof(lblMerge), lblMerge);
            EnsureBound(nameof(btnRefuse), btnRefuse);
            EnsureBound(nameof(lblRefuse), lblRefuse);
            EnsureBound(nameof(lblState), lblState);
            EnsureBound(nameof(lblGuildName), lblGuildName);
            EnsureBound(nameof(lblGuildLevel), lblGuildLevel);
            EnsureBound(nameof(lblMaster), lblMaster);
            EnsureBound(nameof(lblNum), lblNum);
            EnsureBound(nameof(lblScore), lblScore);
        }
    }
}
