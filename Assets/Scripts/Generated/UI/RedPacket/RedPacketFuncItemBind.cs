// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/redPacket/RedPacketFuncItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.RedPacket
{
    public partial class RedPacketFuncItemBind : BaseView
    {
        public TextMeshProUGUI _lb_content;
        public RectTransform _btn_go;
        public TextMeshProUGUI _lb_go;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_lb_content), _lb_content);
            EnsureBound(nameof(_btn_go), _btn_go);
            EnsureBound(nameof(_lb_go), _lb_go);
        }
    }
}
