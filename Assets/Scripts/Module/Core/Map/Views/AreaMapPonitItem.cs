using Shenxiao.Generated.UI.Map;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>
    /// 区域地图传送/标记点(对标老客户端 map/AreaMapPonitItem.ts):点(point)+ 描述(desc)。
    ///
    /// SetData(desc)。降级:点图标(配置)待接 → 描述即时。由 AreaMapView 克隆。
    /// </summary>
    public sealed class AreaMapPonitItem : AreaMapPonitItemBind
    {
        public void SetData(string descText)
        {
            if (desc != null) desc.text = descText ?? "";
        }
    }
}
