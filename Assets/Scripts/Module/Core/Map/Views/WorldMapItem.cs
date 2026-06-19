using Shenxiao.Generated.UI.Map;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>
    /// 世界地图城市项(对标老客户端 map/WorldMapItem.ts):城市图(city_img)/名(city_name)/等级(city_lv)/头像/坐标点。
    ///
    /// SetData(name, lv)。降级:城市图/头像(地图配置)待接 → 名/等级即时。由 WorldMapView 克隆。
    /// </summary>
    public sealed class WorldMapItem : WorldMapItemBind
    {
        public void SetData(string cityName, string cityLv)
        {
            if (city_name != null) city_name.text = cityName ?? "";
            if (city_lv != null) city_lv.text = cityLv ?? "";
        }
    }
}
