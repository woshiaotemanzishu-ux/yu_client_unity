using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>UI effect slot backed by an effect/objs runtime prefab.</summary>
    public sealed class UIEffectSlot : UIDynamicResourceSlot
    {
        [SerializeField] private string _effectName = "";
        [SerializeField] private Vector2 _position;
        [SerializeField] private Vector3 _scale = Vector3.one;
        [SerializeField] private float _rotationY;

        public string EffectName => _effectName;
        public Vector2 Position => _position;
        public Vector3 Scale => _scale == default ? Vector3.one : _scale;
        public float RotationY => _rotationY;

        public void ConfigureEffect(string slotId, string effectName, string addressKey, string source, string note,
            Vector2 position, Vector3 scale, float rotationY)
        {
            Configure(UIDynamicResourceKind.UIEffect, slotId, addressKey, source, note);
            _effectName = effectName ?? "";
            _position = position;
            _scale = scale == default ? Vector3.one : scale;
            _rotationY = rotationY;
        }
    }
}
