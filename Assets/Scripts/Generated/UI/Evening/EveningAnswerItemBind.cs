// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningAnswerItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningAnswerItemBind : BaseView
    {
        public RectTransform _Group1;
        public Image _bg;
        public Image _img_rank;
        public TextMeshProUGUI _lb_rank;
        public TextMeshProUGUI _lb_name;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_img_rank), _img_rank);
            EnsureBound(nameof(_lb_rank), _lb_rank);
            EnsureBound(nameof(_lb_name), _lb_name);
        }
    }
}
