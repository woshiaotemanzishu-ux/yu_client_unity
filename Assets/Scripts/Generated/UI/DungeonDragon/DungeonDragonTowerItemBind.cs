// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonDragon/DungeonDragonTowerItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonDragon
{
    public partial class DungeonDragonTowerItemBind : BaseView
    {
        public RectTransform _gp_con;
        public Image _Image1;
        public Image _Image2;
        public Image _img_hp;
        public Image _img_bg;
        public TextMeshProUGUI _lb_hp;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_img_hp), _img_hp);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_hp), _lb_hp);
        }
    }
}
