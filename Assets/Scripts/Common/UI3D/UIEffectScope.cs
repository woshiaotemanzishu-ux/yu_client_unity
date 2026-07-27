using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 可选的界面级共享作用域。用于需要保持窗口局部 sibling/遮挡关系的复杂界面；
    /// 作用域内仍是“每个渲染带一套 Camera/RT”，而不是每个特效一套。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIEffectScope : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("共享通道的覆盖矩形。留空时使用当前节点的 RectTransform。")]
        private RectTransform _channelRoot;

        public RectTransform ChannelRoot
        {
            get
            {
                if (_channelRoot != null) return _channelRoot;
                return transform as RectTransform;
            }
        }
    }
}
