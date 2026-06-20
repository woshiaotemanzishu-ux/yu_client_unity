// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/brightSea/BrightSeaCruiseLogView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.BrightSea
{
    public partial class BrightSeaCruiseLogViewBind : BaseView
    {
        public Image _Image1;
        public Image _close_btn;
        public Image _Image2;
        public Image _Image3;
        public Image _Image4;
        public Image _Image5;
        public ScrollRect _gp_item_con;
        public RectTransform _con_empty;
        public TextMeshProUGUI label;
        public Image img;
        public GameObject _tpl_BrightSeaCruiseLogItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_close_btn), _close_btn);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_gp_item_con), _gp_item_con);
            EnsureBound(nameof(_con_empty), _con_empty);
            EnsureBound(nameof(label), label);
            EnsureBound(nameof(img), img);
            EnsureBound(nameof(_tpl_BrightSeaCruiseLogItem), _tpl_BrightSeaCruiseLogItem);
        }
    }
}
