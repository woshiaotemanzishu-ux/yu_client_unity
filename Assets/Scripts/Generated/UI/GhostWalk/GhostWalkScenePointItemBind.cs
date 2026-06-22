// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkScenePointItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkScenePointItemBind : BaseView
    {
        public TextMeshProUGUI lblPointName;
        public Image _img_line;
        public Image selectBox;

        protected override void BindNodes()
        {
            EnsureBound(nameof(lblPointName), lblPointName);
            EnsureBound(nameof(_img_line), _img_line);
            EnsureBound(nameof(selectBox), selectBox);
        }
    }
}
