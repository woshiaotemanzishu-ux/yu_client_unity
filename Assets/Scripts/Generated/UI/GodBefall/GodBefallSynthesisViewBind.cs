// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBefall/GodBefallSynthesisView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBefall
{
    public partial class GodBefallSynthesisViewBind : BaseView
    {
        public Image img_bg;
        public Image img_bottom_bg;
        public TextMeshProUGUI lable_title;
        public Image img_close_btn;
        public Image img_btn_synthesis;
        public TextMeshProUGUI lable;
        public Image img_red;
        public ScrollRect list_equip;
        public RectTransform empty_gp;
        public Image _Image5;
        public TextMeshProUGUI _Label1;
        public GameObject _tpl_GodBefallSynthesisItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_bottom_bg), img_bottom_bg);
            EnsureBound(nameof(lable_title), lable_title);
            EnsureBound(nameof(img_close_btn), img_close_btn);
            EnsureBound(nameof(img_btn_synthesis), img_btn_synthesis);
            EnsureBound(nameof(lable), lable);
            EnsureBound(nameof(img_red), img_red);
            EnsureBound(nameof(list_equip), list_equip);
            EnsureBound(nameof(empty_gp), empty_gp);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_tpl_GodBefallSynthesisItem), _tpl_GodBefallSynthesisItem);
        }
    }
}
