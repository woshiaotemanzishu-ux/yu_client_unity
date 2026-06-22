// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossdomain/BossDomainDoubleView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Bossdomain
{
    public partial class BossDomainDoubleViewBind : BaseView
    {
        public Image Image;
        public Image close;
        public Image Image4;
        public RectTransform gp_reward;
        public TextMeshProUGUI name;
        public TextMeshProUGUI cur;
        public TextMeshProUGUI state;
        public TextMeshProUGUI desc;
        public Image _Image2;
        public TextMeshProUGUI _lb_win_name;
        public RectTransform btn;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay1;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(close), close);
            EnsureBound(nameof(Image4), Image4);
            EnsureBound(nameof(gp_reward), gp_reward);
            EnsureBound(nameof(name), name);
            EnsureBound(nameof(cur), cur);
            EnsureBound(nameof(state), state);
            EnsureBound(nameof(desc), desc);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_win_name), _lb_win_name);
            EnsureBound(nameof(btn), btn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay1), labelDisplay1);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
