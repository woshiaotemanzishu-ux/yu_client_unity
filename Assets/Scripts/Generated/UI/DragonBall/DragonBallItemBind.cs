// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonBall/DragonBallItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonBall
{
    public partial class DragonBallItemBind : BaseView
    {
        public Image _img_icon;
        public RectTransform _box_effect;
        public Image _img_select;
        public Image _img_red;
        public Image _img_lock;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_box_effect), _box_effect);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_img_red), _img_red);
            EnsureBound(nameof(_img_lock), _img_lock);
        }
    }
}
