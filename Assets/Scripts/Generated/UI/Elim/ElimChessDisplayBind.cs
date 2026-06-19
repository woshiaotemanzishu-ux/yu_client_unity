// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/elim/ElimChessDisplay.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Elim
{
    public partial class ElimChessDisplayBind : BaseView
    {
        public RectTransform effect_gp_1;
        public Image _chess_icon;
        public TextMeshProUGUI buff_name;

        protected override void BindNodes()
        {
            EnsureBound(nameof(effect_gp_1), effect_gp_1);
            EnsureBound(nameof(_chess_icon), _chess_icon);
            EnsureBound(nameof(buff_name), buff_name);
        }
    }
}
