using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    public sealed class UIEffectStageCameraGuard : MonoBehaviour
    {
        public Camera Owner;

        private void LateUpdate()
        {
            UIEffectStage.ExcludeStageLayerFromOtherCameras(Owner);
        }
    }
}
