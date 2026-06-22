// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkBossEntryView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkBossEntryViewBind : BaseView
    {
        public Image btnClose;
        public Image imgLine;
        public Image btnReward;
        public ScrollRect listBoss;
        public GameObject _tpl_GhostWalkBossEntryItem;
        public GameObject _tpl_GhostWalkBossEntryPoint;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(btnClose), btnClose);
            EnsureBound(nameof(imgLine), imgLine);
            EnsureBound(nameof(btnReward), btnReward);
            EnsureBound(nameof(listBoss), listBoss);
            EnsureBound(nameof(_tpl_GhostWalkBossEntryItem), _tpl_GhostWalkBossEntryItem);
            EnsureBound(nameof(_tpl_GhostWalkBossEntryPoint), _tpl_GhostWalkBossEntryPoint);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
