// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBefall/GodBefallBagItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBefall
{
    public partial class GodBefallBagItemBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _gp_item;
        public Image _img_icon;
        public Image _img_lock;
        public Image _img_up;
        public Image _reddot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_lock), _img_lock);
            EnsureBound(nameof(_img_up), _img_up);
            EnsureBound(nameof(_reddot), _reddot);
        }
    }
}
