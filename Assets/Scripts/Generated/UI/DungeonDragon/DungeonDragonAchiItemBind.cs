// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonDragon/DungeonDragonAchiItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonDragon
{
    public partial class DungeonDragonAchiItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform _gp_get;
        public Image _Image2;
        public TextMeshProUGUI _lb_get;
        public Image _img_get_red;
        public RectTransform _img_check;
        public Image _Image22;
        public TextMeshProUGUI _lb_get2;
        public TextMeshProUGUI _lb_wave;
        public ScrollRect Content;
        public GameObject _tpl_DungeonDragonRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_gp_get), _gp_get);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_get), _lb_get);
            EnsureBound(nameof(_img_get_red), _img_get_red);
            EnsureBound(nameof(_img_check), _img_check);
            EnsureBound(nameof(_Image22), _Image22);
            EnsureBound(nameof(_lb_get2), _lb_get2);
            EnsureBound(nameof(_lb_wave), _lb_wave);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_DungeonDragonRewardItem), _tpl_DungeonDragonRewardItem);
        }
    }
}
