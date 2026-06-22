// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonTower/DungeonTowerItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonTower
{
    public partial class DungeonTowerItemBind : BaseView
    {
        public RectTransform _box_pos;
        public Image _img_titile;
        public TextMeshProUGUI _lb_title;
        public Image _img1;
        public Image _img_select;
        public RectTransform _box_reward;
        public Image _img_got;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_pos), _box_pos);
            EnsureBound(nameof(_img_titile), _img_titile);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_img1), _img1);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_box_reward), _box_reward);
            EnsureBound(nameof(_img_got), _img_got);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
