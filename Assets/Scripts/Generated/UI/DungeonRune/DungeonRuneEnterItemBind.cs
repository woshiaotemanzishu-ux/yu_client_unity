// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonRune/DungeonRuneEnterItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonRune
{
    public partial class DungeonRuneEnterItemBind : BaseView
    {
        public RectTransform _gp_con;
        public RectTransform _gp_model;
        public TextMeshProUGUI _lb_floor;
        public Image _img_passed;
        public Image _img_lock;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_con), _gp_con);
            EnsureBound(nameof(_gp_model), _gp_model);
            EnsureBound(nameof(_lb_floor), _lb_floor);
            EnsureBound(nameof(_img_passed), _img_passed);
            EnsureBound(nameof(_img_lock), _img_lock);
        }
    }
}
