using Shenxiao.Generated.UI.Map;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>
    /// 区域地图路径点(对标老客户端 map/AreaMapWayPonitItem.ts):寻路路径上的标记点(point/end_pos),纯视觉。
    ///
    /// 无业务字段,由 AreaMapView 沿路径克隆排布;位置/图由地图寻路数据驱动(待 SceneManager 寻路移植)。
    /// </summary>
    public sealed class AreaMapWayPonitItem : AreaMapWayPonitItemBind
    {
        public void SetData(UnityEngine.Vector2 position, bool destination)
        {
            if (transform is UnityEngine.RectTransform root) root.anchoredPosition = position;
            if (point != null) point.gameObject.SetActive(!destination);
            if (end_pos != null) end_pos.gameObject.SetActive(destination);
        }
    }
}
