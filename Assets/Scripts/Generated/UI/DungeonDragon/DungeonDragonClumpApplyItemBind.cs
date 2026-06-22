// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonDragon/DungeonDragonClumpApplyItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonDragon
{
    public partial class DungeonDragonClumpApplyItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform _img_head;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_fighting;
        public TextMeshProUGUI _lb_level;
        public RectTransform _gp_agree;
        public Image _Image4;
        public TextMeshProUGUI _Label1;
        public RectTransform _gp_refuse;
        public Image _Image5;
        public TextMeshProUGUI _Label2;
        public GameObject _tpl_CustomHeadItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_head), _img_head);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_fighting), _lb_fighting);
            EnsureBound(nameof(_lb_level), _lb_level);
            EnsureBound(nameof(_gp_agree), _gp_agree);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_gp_refuse), _gp_refuse);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(_tpl_CustomHeadItem), _tpl_CustomHeadItem);
        }
    }
}
