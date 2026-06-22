// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonDragon/DungeonDragonRecordItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonDragon
{
    public partial class DungeonDragonRecordItemBind : BaseView
    {
        public Image _img_role;
        public TextMeshProUGUI _lb_name;
        public RectTransform _gp_role;
        public RectTransform _gp_model;
        public RectTransform _box1;
        public TextMeshProUGUI _lb_fight;
        public Image _img_leader;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_role), _img_role);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_gp_role), _gp_role);
            EnsureBound(nameof(_gp_model), _gp_model);
            EnsureBound(nameof(_box1), _box1);
            EnsureBound(nameof(_lb_fight), _lb_fight);
            EnsureBound(nameof(_img_leader), _img_leader);
        }
    }
}
