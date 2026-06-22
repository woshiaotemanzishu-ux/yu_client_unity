// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonWhisper/dwMonItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonWhisper
{
    public partial class DwMonItemBind : BaseView
    {
        public RectTransform _gp_con;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_time;
        public Image _img_line;
        public Image _img_select;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_img_line), _img_line);
            EnsureBound(nameof(_img_select), _img_select);
        }
    }
}
