// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/elim/ElimBlockDisplay.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Elim
{
    public partial class ElimBlockDisplayBind : BaseView
    {
        public RectTransform con_chess;
        public RectTransform effect_gp;
        public TextMeshProUGUI buff_name;
        public GameObject _tpl_ElimChessDisplay;

        protected override void BindNodes()
        {
            EnsureBound(nameof(con_chess), con_chess);
            EnsureBound(nameof(effect_gp), effect_gp);
            EnsureBound(nameof(buff_name), buff_name);
            EnsureBound(nameof(_tpl_ElimChessDisplay), _tpl_ElimChessDisplay);
        }
    }
}
