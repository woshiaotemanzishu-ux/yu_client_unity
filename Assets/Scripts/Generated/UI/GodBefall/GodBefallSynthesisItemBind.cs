// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBefall/GodBefallSynthesisItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBefall
{
    public partial class GodBefallSynthesisItemBind : BaseView
    {
        public Image img_bg;
        public ScrollRect list_equip;
        public Image img_equal;
        public ScrollRect list_single_equip;
        public Image img_check_bg;
        public Image img_check_select;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(list_equip), list_equip);
            EnsureBound(nameof(img_equal), img_equal);
            EnsureBound(nameof(list_single_equip), list_single_equip);
            EnsureBound(nameof(img_check_bg), img_check_bg);
            EnsureBound(nameof(img_check_select), img_check_select);
        }
    }
}
