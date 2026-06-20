// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chat/SystemItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chat
{
    public partial class SystemItemBind : BaseView
    {
        public RectTransform sysCon;
        public RectTransform SpriteGraphic;
        public RectTransform _Group1;
        public Image sysIcon;
        public TextMeshProUGUI txt_sys_channel;
        public TextMeshProUGUI txt_sys_content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(sysCon), sysCon);
            EnsureBound(nameof(SpriteGraphic), SpriteGraphic);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(sysIcon), sysIcon);
            EnsureBound(nameof(txt_sys_channel), txt_sys_channel);
            EnsureBound(nameof(txt_sys_content), txt_sys_content);
        }
    }
}
