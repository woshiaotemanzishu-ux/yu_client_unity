// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eudaemon/EudaemonRankItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eudaemon
{
    public partial class EudaemonRankItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_rank;
        public TextMeshProUGUI _lb_rank;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_score;
        public ScrollRect _panel_item;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_rank), _img_rank);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_score), _lb_score);
            EnsureBound(nameof(_panel_item), _panel_item);
        }
    }
}
