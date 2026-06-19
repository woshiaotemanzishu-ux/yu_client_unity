using Shenxiao.Generated.UI.Bag;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 熔炼属性弹窗(对标老客户端 bag/SmeltPropView.ts):显示熔炼累计总属性(标题 + 属性文本)。
    ///
    /// SetData(title, propText) 填标题 + 属性文本。降级:熔炼属性数据(SmeltModel)未移植 → 默认空,待数据。
    /// 由 BagSmeltView 的属性按钮打开。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class SmeltPropView : SmeltPropViewBind
    {
        /// <summary>填熔炼属性(对标 SetData)。</summary>
        public void SetData(string title, string propText)
        {
            if (property_title != null && title != null) property_title.text = title;
            if (prop_text != null) prop_text.text = propText ?? "";
        }
    }
}
