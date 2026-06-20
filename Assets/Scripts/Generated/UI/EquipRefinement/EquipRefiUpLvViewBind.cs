// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equipRefinement/EquipRefiUpLvView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.EquipRefinement
{
    public partial class EquipRefiUpLvViewBind : BaseView
    {
        public RectTransform gp_con;
        public RectTransform _gp_guaoxiao;
        public Image _img_0;
        public Image _Image1;
        public Image _Image0;
        public Image _Image2;
        public RectTransform itemGp;
        public RectTransform _gp_equip;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI refText;
        public TextMeshProUGUI _lb_desc;
        public GameObject _tpl_EquipRefiUpTabItem;
        public GameObject _tpl_EquipmentItem;
        public GameObject _tpl_EquipRefiAttrsItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_con), gp_con);
            EnsureBound(nameof(_gp_guaoxiao), _gp_guaoxiao);
            EnsureBound(nameof(_img_0), _img_0);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image0), _Image0);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(itemGp), itemGp);
            EnsureBound(nameof(_gp_equip), _gp_equip);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(refText), refText);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_tpl_EquipRefiUpTabItem), _tpl_EquipRefiUpTabItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
            EnsureBound(nameof(_tpl_EquipRefiAttrsItem), _tpl_EquipRefiAttrsItem);
        }
    }
}
