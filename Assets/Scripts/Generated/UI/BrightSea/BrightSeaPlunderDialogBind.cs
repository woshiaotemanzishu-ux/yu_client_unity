// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/brightSea/BrightSeaPlunderDialog.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BrightSea
{
    public partial class BrightSeaPlunderDialogBind : BaseView
    {
        public Image _Image2;
        public Image _Img_bg;
        public Image _close_btn;
        public RectTransform _Group1;
        public Image _Image4;
        public TextMeshProUGUI title;
        public RectTransform _con_head;
        public TextMeshProUGUI _role_name;
        public TextMeshProUGUI _guild_name;
        public TextMeshProUGUI _ship_name;
        public RectTransform _con_reward;
        public Image Image;
        public RectTransform _ok_btn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public GameObject _tpl_BaseAwardItem;
        public GameObject _tpl_CustomHeadItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Img_bg), _Img_bg);
            EnsureBound(nameof(_close_btn), _close_btn);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(_con_head), _con_head);
            EnsureBound(nameof(_role_name), _role_name);
            EnsureBound(nameof(_guild_name), _guild_name);
            EnsureBound(nameof(_ship_name), _ship_name);
            EnsureBound(nameof(_con_reward), _con_reward);
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(_ok_btn), _ok_btn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
            EnsureBound(nameof(_tpl_CustomHeadItem), _tpl_CustomHeadItem);
        }
    }
}
