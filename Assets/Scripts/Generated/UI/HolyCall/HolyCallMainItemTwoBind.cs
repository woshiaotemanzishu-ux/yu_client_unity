// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holyCall/HolyCallMainItemTwo.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolyCall
{
    public partial class HolyCallMainItemTwoBind : BaseView
    {
        public Image _Image1;
        public RectTransform _btn_get;
        public Image btnImg;
        public TextMeshProUGUI btnLb;
        public Image reddot;
        public ScrollRect _Scroller1;
        public RectTransform itemGp;
        public Image _img_tips;
        public TextMeshProUGUI tipsLb;
        public Image receviedImg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_btn_get), _btn_get);
            EnsureBound(nameof(btnImg), btnImg);
            EnsureBound(nameof(btnLb), btnLb);
            EnsureBound(nameof(reddot), reddot);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(itemGp), itemGp);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(tipsLb), tipsLb);
            EnsureBound(nameof(receviedImg), receviedImg);
        }
    }
}
