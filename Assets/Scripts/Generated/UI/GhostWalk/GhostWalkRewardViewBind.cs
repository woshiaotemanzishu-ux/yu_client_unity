// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkRewardViewBind : BaseView
    {
        public Image btnReturn;
        public Image img_line;
        public Image btnClose;
        public ScrollRect listReward;
        public TextMeshProUGUI lblReturn;
        public TextMeshProUGUI lblDesc;
        public GameObject _tpl_GhostWalkRewardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(btnReturn), btnReturn);
            EnsureBound(nameof(img_line), img_line);
            EnsureBound(nameof(btnClose), btnClose);
            EnsureBound(nameof(listReward), listReward);
            EnsureBound(nameof(lblReturn), lblReturn);
            EnsureBound(nameof(lblDesc), lblDesc);
            EnsureBound(nameof(_tpl_GhostWalkRewardItem), _tpl_GhostWalkRewardItem);
        }
    }
}
