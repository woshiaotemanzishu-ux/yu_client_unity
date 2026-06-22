// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/listDuobao/ListRankItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.ListDuobao
{
    public partial class ListRankItemBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _gp_reward;
        public RectTransform _gp_msg;
        public Image _img_rank;
        public TextMeshProUGUI _lb_rank;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_score;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_gp_reward), _gp_reward);
            EnsureBound(nameof(_gp_msg), _gp_msg);
            EnsureBound(nameof(_img_rank), _img_rank);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_score), _lb_score);
        }
    }
}
