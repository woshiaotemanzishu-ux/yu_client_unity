// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guildidol/GuildIdolRuneSelView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guildidol
{
    public partial class GuildIdolRuneSelViewBind : BaseView
    {
        public Image _Image1;
        public Image _img_title_bg;
        public TextMeshProUGUI _lb_title;
        public Image _Image3;
        public ScrollRect _list;
        public RectTransform _gp_null;
        public Image _Image4;
        public TextMeshProUGUI _Label2;
        public Image close_img;
        public GameObject _tpl_GuildIdolRuneSelItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_title_bg), _img_title_bg);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_list), _list);
            EnsureBound(nameof(_gp_null), _gp_null);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(close_img), close_img);
            EnsureBound(nameof(_tpl_GuildIdolRuneSelItem), _tpl_GuildIdolRuneSelItem);
        }
    }
}
