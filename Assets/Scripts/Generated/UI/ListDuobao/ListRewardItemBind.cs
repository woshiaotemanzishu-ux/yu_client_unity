// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/listDuobao/ListRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.ListDuobao
{
    public partial class ListRewardItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_have;
        public TextMeshProUGUI _lb_msg;
        public TextMeshProUGUI _lb_score;
        public ScrollRect _gp_item;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_have), _img_have);
            EnsureBound(nameof(_lb_msg), _lb_msg);
            EnsureBound(nameof(_lb_score), _lb_score);
            EnsureBound(nameof(_gp_item), _gp_item);
        }
    }
}
