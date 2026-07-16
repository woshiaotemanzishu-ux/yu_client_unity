using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 动态头饰挂点校准器。头饰 prefab 挂在角色 head_mount 下，完整继承身体头骨的位移与旋转；
    /// 本组件只在挂点局部空间校准定位骨的位置、附加旋转和相对缩放，不再逐帧追身体骨的世界位置。
    /// 头饰自己的 Timeline 仍独立播放发丝/飘带子骨动画。
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class AnimatedAttachmentPositionFollower : MonoBehaviour
    {
        private Transform _targetBone;
        private Transform _attachmentBone;
        private Transform _socket;
        private Transform _alignmentSpace;
        private Vector3 _targetSocketPosition;
        private Vector3 _positionOffset;
        private Vector3 _rotationOffset;
        private float _scaleMultiplier = 1f;
        private Quaternion _baseLocalRotation;
        private Vector3 _baseLocalScale = Vector3.one;

        public Vector3 PositionOffset => _positionOffset;
        public Vector3 RotationOffset => _rotationOffset;
        public float ScaleMultiplier => _scaleMultiplier;
        public Vector3 ReferenceWorldScale => _alignmentSpace != null
            ? _alignmentSpace.lossyScale : Vector3.one;
        public Vector3 ReferenceWorldEuler => _alignmentSpace != null
            ? _alignmentSpace.eulerAngles : Vector3.zero;
        public Vector3 AttachmentWorldScale => transform.lossyScale;

        public void Initialize(Transform targetBone, Transform attachmentBone)
        {
            Initialize(targetBone, attachmentBone, null, Vector3.zero, Vector3.zero, 1f);
        }

        public void Initialize(Transform targetBone, Transform attachmentBone,
            Transform alignmentSpace, Vector3 positionOffset)
        {
            Initialize(targetBone, attachmentBone, alignmentSpace, positionOffset, Vector3.zero, 1f);
        }

        public void Initialize(Transform targetBone, Transform attachmentBone,
            Transform alignmentSpace, Vector3 positionOffset, Vector3 rotationOffset, float scaleMultiplier)
        {
            _targetBone = targetBone;
            _attachmentBone = attachmentBone;
            _socket = transform.parent;
            _alignmentSpace = alignmentSpace;
            _positionOffset = positionOffset;
            _rotationOffset = rotationOffset;
            _scaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
            _baseLocalRotation = transform.localRotation;
            _baseLocalScale = transform.localScale;

            // head_mount 在绑定姿态相对角色根为单位变换，运行时提供完整头骨动画增量。
            // 把身体 head 在挂点空间中的稳定基准只记录一次；之后不再追它的世界位置。
            if (_socket != null && _targetBone != null)
                _targetSocketPosition = _socket.InverseTransformPoint(_targetBone.position);
            SnapNow();
        }

        /// <summary>
        /// 调试期实时修改头饰相对 head_mount 绑定姿态的局部校准量。旋转和缩放只作用于头饰 prefab 根；
        /// 不会修改角色展示台，也不会写资产。确认后由工具输出数值再反烘焙到 Art 源。
        /// </summary>
        public void SetTuning(Vector3 positionOffset, Vector3 rotationOffset, float scaleMultiplier)
        {
            _positionOffset = positionOffset;
            _rotationOffset = rotationOffset;
            _scaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
            SnapNow();
        }

        private void LateUpdate()
        {
            SnapNow();
        }

        public void SnapNow()
        {
            if (_targetBone == null || _attachmentBone == null || _socket == null)
            {
                enabled = false;
                return;
            }

            // 两套 Timeline 都完成本帧采样后，先应用头饰自身校准，再让定位骨落到挂点内的固定位置。
            // 父级 head_mount 已经完整继承身体头骨动画；这里严禁再用身体 head 的世界位置追随，否则会重新引入漂移。
            transform.localRotation = _baseLocalRotation * Quaternion.Euler(_rotationOffset);
            transform.localScale = Vector3.Scale(_baseLocalScale, Vector3.one * _scaleMultiplier);
            Vector3 currentSocketPosition = _socket.InverseTransformPoint(_attachmentBone.position);
            transform.localPosition += _targetSocketPosition + _positionOffset - currentSocketPosition;
        }
    }
}
