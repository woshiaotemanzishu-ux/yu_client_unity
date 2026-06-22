// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaServerItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaServerItemBind : BaseView
    {
        public Image _img_server;
        public TextMeshProUGUI _lb_server_name;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_server), _img_server);
            EnsureBound(nameof(_lb_server_name), _lb_server_name);
        }
    }
}
