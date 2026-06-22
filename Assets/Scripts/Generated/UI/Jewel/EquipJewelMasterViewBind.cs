// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/jewel/EquipJewelMasterView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Jewel
{
    public partial class EquipJewelMasterViewBind : BaseView
    {
        public Image img_bg;
        public Image img_title;
        public TextMeshProUGUI lb_title;
        public RectTransform gp_attr;
        public RectTransform group_cur;
        public Image _Image3;
        public TextMeshProUGUI lb_cur;
        public ScrollRect Content1;
        public RectTransform Content;
        public RectTransform group_next;
        public Image _Image4;
        public TextMeshProUGUI lb_next;
        public Image btn_active;
        public TextMeshProUGUI lb_active;
        public Image img_redAc;
        public Image btn_close;
        public RectTransform gp_stren;
        public TextMeshProUGUI lb_stren1;
        public TextMeshProUGUI lb_stren2;
        public TextMeshProUGUI lb_stren3;
        public GameObject _tpl_EquipMasterItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(lb_title), lb_title);
            EnsureBound(nameof(gp_attr), gp_attr);
            EnsureBound(nameof(group_cur), group_cur);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(lb_cur), lb_cur);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(group_next), group_next);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(lb_next), lb_next);
            EnsureBound(nameof(btn_active), btn_active);
            EnsureBound(nameof(lb_active), lb_active);
            EnsureBound(nameof(img_redAc), img_redAc);
            EnsureBound(nameof(btn_close), btn_close);
            EnsureBound(nameof(gp_stren), gp_stren);
            EnsureBound(nameof(lb_stren1), lb_stren1);
            EnsureBound(nameof(lb_stren2), lb_stren2);
            EnsureBound(nameof(lb_stren3), lb_stren3);
            EnsureBound(nameof(_tpl_EquipMasterItem), _tpl_EquipMasterItem);
        }
    }
}
