// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonExp/DungeonExpUseExpView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonExp
{
    public partial class DungeonExpUseExpViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg3;
        public TextMeshProUGUI _lb_title;
        public Image _img_bg2;
        public Image _img_close;
        public ScrollRect _panel_item;
        public GameObject _tpl_DungeonExpUseExpItem;
        public GameObject _tpl_DungeonExpFightSceneItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_panel_item), _panel_item);
            EnsureBound(nameof(_tpl_DungeonExpUseExpItem), _tpl_DungeonExpUseExpItem);
            EnsureBound(nameof(_tpl_DungeonExpFightSceneItem), _tpl_DungeonExpFightSceneItem);
        }
    }
}
