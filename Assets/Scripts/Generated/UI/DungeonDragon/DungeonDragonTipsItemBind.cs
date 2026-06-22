// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonDragon/DungeonDragonTipsItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonDragon
{
    public partial class DungeonDragonTipsItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public TextMeshProUGUI _lb_desc0;
        public TextMeshProUGUI _lb_desc1;
        public ScrollRect Content;
        public RectTransform _btn_go;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_lb_desc0), _lb_desc0);
            EnsureBound(nameof(_lb_desc1), _lb_desc1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_btn_go), _btn_go);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
        }
    }
}
