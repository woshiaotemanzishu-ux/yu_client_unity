using Shenxiao.Generated.UI.TestAct;

namespace Shenxiao.Module.Core.TestAct
{
    /// <summary>
    /// 测试活动登录页(对标老客户端 testAct/TestActLoginView.ts):奖励图(_img_good)+ 多行说明(_lb_desc..4)。
    /// SetDesc(text)。降级:奖励图/数据待接 → 主说明即时。由 TestActBaseView 克隆。
    /// </summary>
    public sealed class TestActLoginView : TestActLoginViewBind
    {
        public void SetDesc(string desc)
        {
            if (_lb_desc != null) _lb_desc.text = desc ?? "";
        }
    }
}
