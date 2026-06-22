// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPolar/DungeonPolarChatView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPolar
{
    public partial class DungeonPolarChatViewBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _gp_content;
        public Image _Image1;
        public GameObject _tpl_DungeonPolarChatItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_gp_content), _gp_content);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_tpl_DungeonPolarChatItem), _tpl_DungeonPolarChatItem);
        }
    }
}
