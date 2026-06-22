// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/bossMystery/BossMysteryRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BossMystery
{
    public partial class BossMysteryRewardViewBind : BaseView
    {
        public Image bg_img;
        public Image _Image1;
        public Image _img_close;
        public ScrollRect _Scroller1;
        public TextMeshProUGUI _Label1;
        public Image _img_title;
        public TextMeshProUGUI _lb_title;
        public GameObject _tpl_BossMysteryRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg_img), bg_img);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_tpl_BossMysteryRewardItem), _tpl_BossMysteryRewardItem);
        }
    }
}
