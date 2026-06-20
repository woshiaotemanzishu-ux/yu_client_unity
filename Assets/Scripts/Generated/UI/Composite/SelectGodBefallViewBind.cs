// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/SelectGodBefallView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class SelectGodBefallViewBind : BaseView
    {
        public Image bg;
        public Image item_bg;
        public Image tips_img;
        public TextMeshProUGUI tips_label;
        public ScrollRect mat_scroller;
        public Image close_btn;
        public Image img_title;
        public TextMeshProUGUI lb_title;
        public RectTransform gp_tips;
        public TextMeshProUGUI need_text;
        public TextMeshProUGUI need_label;
        public GameObject _tpl_EquipmentItem;
        public GameObject _tpl_CompositeSelectEquipItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(item_bg), item_bg);
            EnsureBound(nameof(tips_img), tips_img);
            EnsureBound(nameof(tips_label), tips_label);
            EnsureBound(nameof(mat_scroller), mat_scroller);
            EnsureBound(nameof(close_btn), close_btn);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(lb_title), lb_title);
            EnsureBound(nameof(gp_tips), gp_tips);
            EnsureBound(nameof(need_text), need_text);
            EnsureBound(nameof(need_label), need_label);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
            EnsureBound(nameof(_tpl_CompositeSelectEquipItem), _tpl_CompositeSelectEquipItem);
        }
    }
}
