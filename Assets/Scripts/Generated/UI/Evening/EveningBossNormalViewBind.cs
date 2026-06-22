// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/evening/EveningBossNormalView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Evening
{
    public partial class EveningBossNormalViewBind : BaseView
    {
        public RectTransform _gp_call;
        public Image _btn_call;
        public RectTransform _wayfinding_gp;
        public Image _btn_exit;
        public RectTransform _gp_time;
        public Image _Image1;
        public TextMeshProUGUI _lb_time;
        public GameObject _tpl_EveningBossPanel;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_call), _gp_call);
            EnsureBound(nameof(_btn_call), _btn_call);
            EnsureBound(nameof(_wayfinding_gp), _wayfinding_gp);
            EnsureBound(nameof(_btn_exit), _btn_exit);
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_tpl_EveningBossPanel), _tpl_EveningBossPanel);
        }
    }
}
