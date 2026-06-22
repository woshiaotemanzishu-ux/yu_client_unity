// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkRewardItemBind : BaseView
    {
        public Image imgBg;
        public TextMeshProUGUI htmlLastName;
        public ScrollRect listReward;

        protected override void BindNodes()
        {
            EnsureBound(nameof(imgBg), imgBg);
            EnsureBound(nameof(htmlLastName), htmlLastName);
            EnsureBound(nameof(listReward), listReward);
        }
    }
}
