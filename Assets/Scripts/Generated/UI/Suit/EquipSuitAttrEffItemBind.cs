// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/suit/EquipSuitAttrEffItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Suit
{
    public partial class EquipSuitAttrEffItemBind : BaseView
    {
        public Image _img_bg;
        public Image bg_1;
        public Image bg_2;
        public Image line;
        public TextMeshProUGUI _lb_num;
        public TextMeshProUGUI _lb_attr;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(bg_1), bg_1);
            EnsureBound(nameof(bg_2), bg_2);
            EnsureBound(nameof(line), line);
            EnsureBound(nameof(_lb_num), _lb_num);
            EnsureBound(nameof(_lb_attr), _lb_attr);
        }
    }
}
