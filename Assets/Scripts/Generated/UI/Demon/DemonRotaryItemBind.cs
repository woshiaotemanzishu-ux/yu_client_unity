// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonRotaryItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonRotaryItemBind : BaseView
    {
        public Image _bg;
        public RectTransform _eff;
        public Image _good;
        public Image _img_clip;
        public Image _new;
        public Image click;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_eff), _eff);
            EnsureBound(nameof(_good), _good);
            EnsureBound(nameof(_img_clip), _img_clip);
            EnsureBound(nameof(_new), _new);
            EnsureBound(nameof(click), click);
        }
    }
}
