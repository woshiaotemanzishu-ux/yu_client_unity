// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildGiftViewOne.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildGiftViewOneBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image Image;
        public Image _btn_close;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public TextMeshProUGUI text;
        public RectTransform _btn_get;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public Image red;
        public Image title;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(text), text);
            EnsureBound(nameof(_btn_get), _btn_get);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(red), red);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
