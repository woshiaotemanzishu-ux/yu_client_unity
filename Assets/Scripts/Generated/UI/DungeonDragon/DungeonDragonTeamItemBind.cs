// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonDragon/DungeonDragonTeamItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonDragon
{
    public partial class DungeonDragonTeamItemBind : BaseView
    {
        public RectTransform _gp_team;
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _lb_name;
        public RectTransform _gp_head_con;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_team), _gp_team);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_gp_head_con), _gp_head_con);
        }
    }
}
