using Shenxiao.Generated.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// CommonModule 中物品详情弹层的 Prefab 布局引用。
    /// 单面板、装备对比双面板和两种遮罩的层级/位置均由 Prefab 固化，业务代码只切显隐和填数据。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemTipsModalLayout : MonoBehaviour
    {
        public Image dimBlocker;
        public Image compareBlocker;
        public GoodsTooltipsBind goods;
        public EquipToolTipsBind equipSingle;
        public EquipToolTipsBind compareCurrent;
        public EquipToolTipsBind compareCandidate;
        public EquipmentItem equipSingleIcon;
        public EquipmentItem compareCurrentIcon;
        public EquipmentItem compareCandidateIcon;
    }
}
