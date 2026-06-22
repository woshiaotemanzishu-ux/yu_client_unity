// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningDragonWaitView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningDragonWaitViewBind : BaseView
    {
        public RectTransform _group_open;
        public Image _Image1;
        public TextMeshProUGUI _Label1_1;
        public TextMeshProUGUI _lb_open_time;
        public GameObject _tpl_EveningAnswerItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_group_open), _group_open);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Label1_1), _Label1_1);
            EnsureBound(nameof(_lb_open_time), _lb_open_time);
            EnsureBound(nameof(_tpl_EveningAnswerItem), _tpl_EveningAnswerItem);
        }
    }
}
