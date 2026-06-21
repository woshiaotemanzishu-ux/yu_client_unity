// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonTalentItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonTalentItemBind : BaseView
    {
        public Image item_bg;
        public Image _img_add;
        public Image _icon;
        public TextMeshProUGUI _lb_lv;
        public Image _img;
        public Image _img_activate;
        public Image _img_level;
        public TextMeshProUGUI _lb_num;

        protected override void BindNodes()
        {
            EnsureBound(nameof(item_bg), item_bg);
            EnsureBound(nameof(_img_add), _img_add);
            EnsureBound(nameof(_icon), _icon);
            EnsureBound(nameof(_lb_lv), _lb_lv);
            EnsureBound(nameof(_img), _img);
            EnsureBound(nameof(_img_activate), _img_activate);
            EnsureBound(nameof(_img_level), _img_level);
            EnsureBound(nameof(_lb_num), _lb_num);
        }
    }
}
