// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonRune/DungeonRuneTargetView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonRune
{
    public partial class DungeonRuneTargetViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_title_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_title;
        public ScrollRect _list_item;
        public Image _img_close;
        public RectTransform _gp_get;
        public Image _img_get;
        public TextMeshProUGUI _lb_get;
        public GameObject _tpl_DungeonRuneTargetItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_title_bg), _img_title_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_list_item), _list_item);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_gp_get), _gp_get);
            EnsureBound(nameof(_img_get), _img_get);
            EnsureBound(nameof(_lb_get), _lb_get);
            EnsureBound(nameof(_tpl_DungeonRuneTargetItem), _tpl_DungeonRuneTargetItem);
        }
    }
}
