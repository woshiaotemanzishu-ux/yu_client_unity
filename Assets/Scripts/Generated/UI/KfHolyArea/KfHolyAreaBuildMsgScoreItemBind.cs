// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfHolyArea/KfHolyAreaBuildMsgScoreItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfHolyArea
{
    public partial class KfHolyAreaBuildMsgScoreItemBind : BaseView
    {
        public TextMeshProUGUI _lb_server_desc;
        public RectTransform _Group1;
        public Image _Image1;
        public Image _img_progress;
        public TextMeshProUGUI _lb_score;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_lb_server_desc), _lb_server_desc);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_progress), _img_progress);
            EnsureBound(nameof(_lb_score), _lb_score);
        }
    }
}
