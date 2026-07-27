using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>共享 UI 特效通道的单一帧驱动器。</summary>
    public sealed class UIEffectServiceRunner : MonoBehaviour
    {
        private void OnEnable()
        {
            UIEffectStage.AttachRunner(this);
        }

        private void LateUpdate()
        {
            UIEffectStage.Tick();
            UIEffectStage.ExcludeStageLayerFromOtherCameras(null);
        }

        private void OnDestroy()
        {
            UIEffectStage.DetachRunner(this);
        }
    }
}
