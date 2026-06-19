using Shenxiao.Generated.UI.Map;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>
    /// 区域地图(对标老客户端 map/AreaMapView.ts):地图滚动(map_scroll)+ 传送点/怪物点/路径点(3 模板)+ 主角点(main_role_point)
    /// + 怪物列表(mon_scroll)。
    ///
    /// 降级:地图/场景数据(MapModel/SceneManager)未移植 → 3 个列表项模板隐藏、OnShow 打 TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class AreaMapView : AreaMapViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_AreaMapMonItem != null) _tpl_AreaMapMonItem.SetActive(false);
            if (_tpl_AreaMapPonitItem != null) _tpl_AreaMapPonitItem.SetActive(false);
            if (_tpl_AreaMapWayPonitItem != null) _tpl_AreaMapWayPonitItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Map", "区域地图打开 → 待对接 地图/场景数据(MapModel/SceneManager)");
        }
    }
}
