using Shenxiao.Generated.UI.MainUI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 顶级玩家提示项(对标老客户端 TopPlayerTipItem.ts):活动图标旁显示一段提示文案的小气泡,
    /// 老端按文本宽自适应 bg + 贴屏幕边时水平翻转 bg。
    ///
    /// SetDes(text) 填文案。降级:bg 文本自适应宽与贴边翻转(依赖屏幕坐标 localToGlobal)归预制体
    /// (建议 bg 挂 ContentSizeFitter / 由调用方定位);事件驱动(图标提示),默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class TopPlayerTipItem : TopPlayerTipItemBind
    {
        /// <summary>填提示文案(对标 SetDes)。</summary>
        public void SetDes(string text)
        {
            if (des != null) des.text = text ?? "";
        }
    }
}
