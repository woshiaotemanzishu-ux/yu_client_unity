// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GCBagPanel.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GCBagPanelBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _Scroller1;
        public TextMeshProUGUI countLb;
        public RectTransform colorBtnGp;
        public RectTransform typeBtnGp;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(countLb), countLb);
            EnsureBound(nameof(colorBtnGp), colorBtnGp);
            EnsureBound(nameof(typeBtnGp), typeBtnGp);
        }
    }
}
