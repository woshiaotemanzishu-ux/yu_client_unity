// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildMergeView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildMergeViewBind : BaseView
    {
        public Image btnClose;
        public Image btnRequest;
        public TextMeshProUGUI lblRequest;
        public Image btnApply;
        public TextMeshProUGUI lblApply;
        public Image imgRed;
        public Image btnHelp;
        public ScrollRect mergeList;
        public RectTransform noList;
        public GameObject _tpl_GuildMergeItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(btnClose), btnClose);
            EnsureBound(nameof(btnRequest), btnRequest);
            EnsureBound(nameof(lblRequest), lblRequest);
            EnsureBound(nameof(btnApply), btnApply);
            EnsureBound(nameof(lblApply), lblApply);
            EnsureBound(nameof(imgRed), imgRed);
            EnsureBound(nameof(btnHelp), btnHelp);
            EnsureBound(nameof(mergeList), mergeList);
            EnsureBound(nameof(noList), noList);
            EnsureBound(nameof(_tpl_GuildMergeItem), _tpl_GuildMergeItem);
        }
    }
}
