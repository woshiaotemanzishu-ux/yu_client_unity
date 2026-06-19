using Shenxiao.Generated.UI.Map;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>
    /// 世界地图(对标老客户端 map/WorldMapView.ts):大地图滚动(scroll/map)+ 城市项(WorldMapItem)+ 云雾(map_cloud)。
    ///
    /// 降级:地图数据(MapModel/城市配置)未移植 → 城市项模板隐藏、OnShow 打 TODO。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class WorldMapView : WorldMapViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_WorldMapItem != null) _tpl_WorldMapItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Map", "世界地图打开 → 待对接 地图数据(MapModel/城市配置)");
        }
    }
}
