// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/gameNotice/GameNoticeListItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GameNotice
{
    public partial class GameNoticeListItemBind : BaseView
    {
        public Image _img_select;
        public TextMeshProUGUI _lab_title;
        public Image _img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_lab_title), _lab_title);
            EnsureBound(nameof(_img_red), _img_red);
        }
    }
}
