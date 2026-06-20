// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/daily/DailyStrongerItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Daily
{
    public partial class DailyStrongerItemBind : BaseView
    {
        public Image bg;
        public RectTransform star;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_desc;
        public RectTransform _btn_go;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public TextMeshProUGUI _lb_level;
        public Image image2;
        public Image _img_icon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(star), star);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_btn_go), _btn_go);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_lb_level), _lb_level);
            EnsureBound(nameof(image2), image2);
            EnsureBound(nameof(_img_icon), _img_icon);
        }
    }
}
