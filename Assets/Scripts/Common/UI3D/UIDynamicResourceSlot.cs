using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    public enum UIDynamicResourceKind
    {
        Unknown = 0,
        Image = 1,
        UIEffect = 2,
        Prefab = 3,
        Model = 4,
    }

    /// <summary>
    /// Visible binding for resources that are loaded dynamically at runtime.
    /// The host UI prefab stores the same Addressable key that runtime code loads,
    /// so editor inspection and remote loading point at one asset boundary.
    /// </summary>
    public class UIDynamicResourceSlot : MonoBehaviour
    {
        [SerializeField] private UIDynamicResourceKind _kind = UIDynamicResourceKind.Unknown;
        [SerializeField] private string _slotId = "";
        [SerializeField] private string _addressKey = "";
        [SerializeField] private string _source = "";
        [SerializeField] private string _note = "";

        public UIDynamicResourceKind Kind => _kind;
        public string SlotId => _slotId;
        public string AddressKey => _addressKey;
        public string Source => _source;
        public string Note => _note;

        public void Configure(UIDynamicResourceKind kind, string slotId, string addressKey, string source, string note)
        {
            _kind = kind;
            _slotId = slotId ?? "";
            _addressKey = addressKey ?? "";
            _source = source ?? "";
            _note = note ?? "";
        }
    }
}
