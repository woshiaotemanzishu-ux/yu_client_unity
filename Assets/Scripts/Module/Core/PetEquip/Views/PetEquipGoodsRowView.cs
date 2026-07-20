using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.PetEquip.Views
{
    /// <summary>侍魂装备背包、强化材料和打造材料共用的 Creator 序列化行组件。</summary>
    public sealed class PetEquipGoodsRowView : MonoBehaviour
    {
        public Image click;
        public Image background;
        public Image selectedMark;
        public TextMeshProUGUI lblName;
        public TextMeshProUGUI lblDetail;
    }
}
