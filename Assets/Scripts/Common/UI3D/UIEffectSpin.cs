using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 恒速自转。用来替代【转换器产出的退化旋转动画】:老端 360°/循环旋转被转成「2 帧四元数曲线 identity→identity」
    /// (起末都是 0°≡360°≡同一旋转),legacy Animation 播了也不转。EffectRotationRepair 按退化曲线的 ε 轴/时长
    /// 推断出 轴+转速 挂上本组件,并停用那条坏 Animation。轴/转速在编辑器烤进 prefab,运行时纯自转,不依赖任何 clip。
    /// </summary>
    public sealed class UIEffectSpin : MonoBehaviour
    {
        [SerializeField] private Vector3 _degreesPerSecond;

        public Vector3 DegreesPerSecond
        {
            get => _degreesPerSecond;
            set => _degreesPerSecond = value;
        }

        private void Update()
        {
            if (_degreesPerSecond == Vector3.zero) return;
            transform.localRotation *= Quaternion.Euler(_degreesPerSecond * Time.deltaTime);
        }
    }
}
