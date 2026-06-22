// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPolar/DungeonPolarRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPolar
{
    public partial class DungeonPolarRewardViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image4;
        public TextMeshProUGUI _Label1;
        public Image _close_btn;
        public ScrollRect Content;
        public Image img_title;
        public TextMeshProUGUI lb_title;
        public GameObject _tpl_DungeonPolarRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_close_btn), _close_btn);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(lb_title), lb_title);
            EnsureBound(nameof(_tpl_DungeonPolarRewardItem), _tpl_DungeonPolarRewardItem);
        }
    }
}
