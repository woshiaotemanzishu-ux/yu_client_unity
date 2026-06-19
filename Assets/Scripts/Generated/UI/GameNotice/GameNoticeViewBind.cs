// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/gameNotice/GameNoticeView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GameNotice
{
    public partial class GameNoticeViewBind : BaseView
    {
        public Image _img_next;
        public TextMeshProUGUI _lab_title;
        public ScrollRect _list_title;
        public ScrollRect _gp_content;
        public RectTransform _gp_item;
        public GameObject _tpl_GameNoticeContentItem;
        public GameObject _tpl_GameNoticeListItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_next), _img_next);
            EnsureBound(nameof(_lab_title), _lab_title);
            EnsureBound(nameof(_list_title), _list_title);
            EnsureBound(nameof(_gp_content), _gp_content);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_tpl_GameNoticeContentItem), _tpl_GameNoticeContentItem);
            EnsureBound(nameof(_tpl_GameNoticeListItem), _tpl_GameNoticeListItem);
        }
    }
}
