// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equip/EquipStrenMasterView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Equip
{
    public partial class EquipStrenMasterViewBind : BaseView
    {
        public Image bg;
        public Image _Image1;
        public TextMeshProUGUI _lb_title;
        public RectTransform _Group1;
        public RectTransform group_cur;
        public Image _Image3;
        public TextMeshProUGUI cur_tip;
        public ScrollRect Content1;
        public RectTransform Content;
        public RectTransform group_next;
        public Image _Image4;
        public TextMeshProUGUI _Label1;
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
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(group_cur), group_cur);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(cur_tip), cur_tip);
            EnsureBound(nameof(Content1), Content1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(group_next), group_next);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Label1), _Label1);
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
