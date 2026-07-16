using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 静态部件挂点对齐器。部件 prefab 挂到角色 socket 后，把自身 locator 对齐到 socket 原点；
    /// 位置/旋转/缩放校准均在 socket 局部空间生效。标准资源应保持 0/0/1，调参只用于旧资源迁移验收。
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class AttachmentSocketAligner : MonoBehaviour
    {
        private Transform _locator;
        private Transform _socket;
        private Vector3 _positionOffset;
        private Vector3 _rotationOffset;
        private float _scaleMultiplier = 1f;
        private Vector3 _baseLocalScale = Vector3.one;
        private Quaternion _locatorRotationInAttachment = Quaternion.identity;

        public Vector3 PositionOffset => _positionOffset;
        public Vector3 RotationOffset => _rotationOffset;
        public float ScaleMultiplier => _scaleMultiplier;
        public Transform Locator => _locator;
        public Vector3 ReferenceWorldScale => _socket != null ? _socket.lossyScale : Vector3.one;
        public Vector3 ReferenceWorldEuler => _socket != null ? _socket.eulerAngles : Vector3.zero;
        public Vector3 AttachmentWorldScale => transform.lossyScale;

        public void Initialize(Transform locator, Vector3 positionOffset,
            Vector3 rotationOffset, float scaleMultiplier)
        {
            _locator = locator;
            _socket = transform.parent;
            _positionOffset = positionOffset;
            _rotationOffset = rotationOffset;
            _scaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
            _baseLocalScale = transform.localScale;
            // locator 描述“武器的哪个点、以什么轴向”对接角色 socket。它不要求与 prefab 根重合；
            // 记录 locator 相对附件根的旋转，Snap 时才能同时解出位置与朝向。旧实现只把 locator
            // 的位置推到 socket，却直接沿用 prefab 根旋转，导致 1200 明明有 Bone_wq_r 轴向仍然剑尖朝反。
            _locatorRotationInAttachment = Quaternion.Inverse(transform.rotation) * _locator.rotation;
            SnapNow();
        }

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
            if (_locator == null || _socket == null)
            {
                enabled = false;
                return;
            }

            transform.localScale = Vector3.Scale(_baseLocalScale, Vector3.one * _scaleMultiplier);
            Quaternion targetRotation = _socket.rotation * Quaternion.Euler(_rotationOffset);
            transform.rotation = targetRotation * Quaternion.Inverse(_locatorRotationInAttachment);

            Vector3 targetPosition = _socket.TransformPoint(_positionOffset);
            transform.position += targetPosition - _locator.position;
        }
    }
}
