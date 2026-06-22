// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonEquip/DungeonEquipClumpInviteItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonEquip
{
    public partial class DungeonEquipClumpInviteItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform _img_head;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_fighting;
        public TextMeshProUGUI _lb_times;
        public RectTransform _gp_invite;
        public Image _Image4;
        public TextMeshProUGUI _lb_invite;
        public GameObject _tpl_CustomHeadItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_head), _img_head);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_fighting), _lb_fighting);
            EnsureBound(nameof(_lb_times), _lb_times);
            EnsureBound(nameof(_gp_invite), _gp_invite);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_lb_invite), _lb_invite);
            EnsureBound(nameof(_tpl_CustomHeadItem), _tpl_CustomHeadItem);
        }
    }
}
