// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkBossEntryItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkBossEntryItemBind : BaseView
    {
        public Image imgBg;
        public Image imgBossIcon;
        public TextMeshProUGUI lblBossName;
        public TextMeshProUGUI lblSceneName;
        public TextMeshProUGUI htmlLiveState;
        public ScrollRect listPoint;

        protected override void BindNodes()
        {
            EnsureBound(nameof(imgBg), imgBg);
            EnsureBound(nameof(imgBossIcon), imgBossIcon);
            EnsureBound(nameof(lblBossName), lblBossName);
            EnsureBound(nameof(lblSceneName), lblSceneName);
            EnsureBound(nameof(htmlLiveState), htmlLiveState);
            EnsureBound(nameof(listPoint), listPoint);
        }
    }
}
