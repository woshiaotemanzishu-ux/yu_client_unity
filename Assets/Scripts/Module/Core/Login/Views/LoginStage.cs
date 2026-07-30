using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录模块的外部展示舞台。背景图片、铺放方式和宽高比均直接保存在 LoginStage.prefab，
    /// 运行时只使用这里序列化的 720 宽视口承载各登录页面。
    /// </summary>
    public sealed class LoginStage : MonoBehaviour
    {
        public RectTransform viewport;
    }
}
