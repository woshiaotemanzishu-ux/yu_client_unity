using Shenxiao.Generated.UI.VipActive;

namespace Shenxiao.Module.Core.VipActive
{
    /// <summary>
    /// VIP活动页签(对标老客户端 vipActive/VipActiveTab.ts):普通态(_con_normal,含 _lbl_name)/选中态(_con_select,含 _lbl_name1),
    /// 选中切换显隐。
    ///
    /// SetData(name) + SetSelected(bool)。降级:数据由 VipActiveView 提供;选中切换自包含。由 VipActiveView 克隆。
    /// ⚠ 模板节点名小写 `vipActiveTab` → 可能受 filler 大小写坑影响不自动挂(同 BagItemRenderer),代码就绪。
    /// </summary>
    public sealed class VipActiveTab : VipActiveTabBind
    {
        public void SetData(string name)
        {
            if (_lbl_name != null) _lbl_name.text = name ?? "";
            if (_lbl_name1 != null) _lbl_name1.text = name ?? "";
        }

        public void SetSelected(bool selected)
        {
            if (_con_select != null) _con_select.gameObject.SetActive(selected);
            if (_con_normal != null) _con_normal.gameObject.SetActive(!selected);
        }
    }
}
