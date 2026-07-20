using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.PetEquip.Views
{
    /// <summary>侍魂装备四个固定穿戴槽的 Creator 序列化组件。</summary>
    public sealed class PetEquipSlotRowView : MonoBehaviour
    {
        public Image click;
        public Image background;
        public Image selectedMark;
        public TextMeshProUGUI lblPosition;
        public TextMeshProUGUI lblDetail;
    }
}
