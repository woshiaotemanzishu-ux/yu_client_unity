using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 场景角色离屏合成相机的单一帧驱动器。相机本身保持 disabled，避免 Unity 自动调度与
    /// 手动渲染叠加；LateUpdate 完成模型位置/动作更新后再把本帧写入 RenderTexture。
    /// </summary>
    public sealed class SceneCharacterStageDriver : MonoBehaviour
    {
        private void LateUpdate()
        {
            SceneCharacterStage.RenderFrame();
        }
    }
}
