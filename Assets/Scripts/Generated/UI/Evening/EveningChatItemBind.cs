// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningChatItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningChatItemBind : BaseView
    {
        public RectTransform normalGroup;
        public RectTransform sysGroup;
        public TextMeshProUGUI sysContent;
        public RectTransform _Group1;
        public Image sysIcon;
        public TextMeshProUGUI _Label1;
        public GameObject _tpl_CustomHeadItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(normalGroup), normalGroup);
            EnsureBound(nameof(sysGroup), sysGroup);
            EnsureBound(nameof(sysContent), sysContent);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(sysIcon), sysIcon);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_tpl_CustomHeadItem), _tpl_CustomHeadItem);
        }
    }
}
