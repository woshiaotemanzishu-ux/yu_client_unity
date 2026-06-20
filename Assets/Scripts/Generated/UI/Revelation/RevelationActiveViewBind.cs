// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/revelation/RevelationActiveView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Revelation
{
    public partial class RevelationActiveViewBind : BaseView
    {
        public RectTransform _Group1;
        public Image img_bg;
        public Image img_title;
        public Image img_title2;
        public RectTransform gp_attrCon;
        public TextMeshProUGUI lb_title;
        public TextMeshProUGUI lb_tips;
        public Image btn_go;
        public TextMeshProUGUI lb_go;
        public GameObject _tpl_RecelationActivateItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(img_title2), img_title2);
            EnsureBound(nameof(gp_attrCon), gp_attrCon);
            EnsureBound(nameof(lb_title), lb_title);
            EnsureBound(nameof(lb_tips), lb_tips);
            EnsureBound(nameof(btn_go), btn_go);
            EnsureBound(nameof(lb_go), lb_go);
            EnsureBound(nameof(_tpl_RecelationActivateItem), _tpl_RecelationActivateItem);
        }
    }
}
