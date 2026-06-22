// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GCDevourPanel.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GCDevourPanelBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _btn_devour;
        public Image _img_btn;
        public TextMeshProUGUI _lb_btn;
        public ScrollRect _Scroller1;
        public Image _img_mat;
        public Image _img_line;
        public TextMeshProUGUI _lb_tips;
        public TextMeshProUGUI _lb_num;
        public TextMeshProUGUI nothingLb;
        public RectTransform dropBtnGp;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_btn_devour), _btn_devour);
            EnsureBound(nameof(_img_btn), _img_btn);
            EnsureBound(nameof(_lb_btn), _lb_btn);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_img_mat), _img_mat);
            EnsureBound(nameof(_img_line), _img_line);
            EnsureBound(nameof(_lb_tips), _lb_tips);
            EnsureBound(nameof(_lb_num), _lb_num);
            EnsureBound(nameof(nothingLb), nothingLb);
            EnsureBound(nameof(dropBtnGp), dropBtnGp);
        }
    }
}
