// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GCRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GCRewardItemBind : BaseView
    {
        public RectTransform _gp_click;
        public Image _img_bg;
        public TextMeshProUGUI countLb;
        public RectTransform effectGp;
        public Image recivedImg;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_click), _gp_click);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(countLb), countLb);
            EnsureBound(nameof(effectGp), effectGp);
            EnsureBound(nameof(recivedImg), recivedImg);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
