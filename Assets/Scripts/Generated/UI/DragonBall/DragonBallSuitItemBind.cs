// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonBall/DragonBallSuitItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonBall
{
    public partial class DragonBallSuitItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_ball;
        public Image _img_lock;
        public TextMeshProUGUI _lb_desc;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_ball), _img_ball);
            EnsureBound(nameof(_img_lock), _img_lock);
            EnsureBound(nameof(_lb_desc), _lb_desc);
        }
    }
}
