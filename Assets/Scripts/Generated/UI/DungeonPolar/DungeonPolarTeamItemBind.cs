// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPolar/DungeonPolarTeamItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPolar
{
    public partial class DungeonPolarTeamItemBind : BaseView
    {
        public RectTransform _gp_team;
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI _lb_name;
        public RectTransform _gp_join;
        public Image _img_join;
        public TextMeshProUGUI _Label1;
        public RectTransform _gp_head_con;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_team), _gp_team);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_gp_join), _gp_join);
            EnsureBound(nameof(_img_join), _img_join);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_gp_head_con), _gp_head_con);
        }
    }
}
