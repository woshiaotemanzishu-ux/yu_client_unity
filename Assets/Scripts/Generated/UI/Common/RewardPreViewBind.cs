// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/RewardPreView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class RewardPreViewBind : BaseView
    {
        public RectTransform _box;
        public Image _Image1;
        public RectTransform _Group1;
        public Image _Image2;
        public Image title_img;
        public Image bg_img_1;
        public Image _btn_close;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public GameObject _tpl_RewardPreItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box), _box);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(title_img), title_img);
            EnsureBound(nameof(bg_img_1), bg_img_1);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_RewardPreItem), _tpl_RewardPreItem);
        }
    }
}
