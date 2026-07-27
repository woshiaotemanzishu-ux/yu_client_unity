using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    public sealed class UIEffectStageCameraGuard : MonoBehaviour
    {
        public Camera Owner;
        // 序列化在场景对象上，脚本域重载后仍保留。UIEffectStage 会扫描该槽位，
        // 避免静态计数归零后把新特效放回旧相机的同一坐标，造成不同 RT 串台。
        public int StageSlot;
        public string ChannelKey;

        // 仅作通道相机标记和域重载后的槽位恢复。相机层隔离由单一 Runner 每帧统一维护，
        // 避免通道数增加后重复扫描全部 Camera。
    }
}
