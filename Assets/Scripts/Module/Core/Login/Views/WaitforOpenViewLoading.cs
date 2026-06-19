using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 等待开服 Loading(对标老客户端 login/WaitforOpenViewLoading.ts):旋转圈(_img_circle,老端 2s 转一圈)+ loading 文字。
    ///
    /// 旋转自包含 → 完整可用(Update 持续转);SetText(str) 设提示文字。降级:多源 loading 计数 + 15s 超时自动关
    /// 逻辑(LoginManager)未移植。事件驱动,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class WaitforOpenViewLoading : WaitforOpenViewLoadingBind
    {
        /// <summary>旋转速度(度/秒;对标 2000ms 一圈 = 180°/s,负向)。</summary>
        [SerializeField] private float _rotateSpeed = 180f;

        private void Update()
        {
            if (_img_circle != null)
                _img_circle.transform.Rotate(0f, 0f, -_rotateSpeed * Time.deltaTime);
        }

        /// <summary>设 loading 文字(对标 _lb_loading.text)。</summary>
        public void SetText(string str)
        {
            if (_lb_loading != null) _lb_loading.text = str ?? "";
        }
    }
}
