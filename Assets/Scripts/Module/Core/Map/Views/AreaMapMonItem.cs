using System;
using Shenxiao.Generated.UI.Map;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>
    /// 区域地图怪物点(对标老客户端 map/AreaMapMonItem.ts):怪物图标(mon_icon)/等级(level)/描述(desc),点击(click_bg)定位。
    ///
    /// SetData(desc, level, onClick)。降级:怪物图标(配置)待接 → 等级/描述即时、点击回调可用。由 AreaMapView 克隆。
    /// </summary>
    public sealed class AreaMapMonItem : AreaMapMonItemBind
    {
        private Action _onClick;

        protected override void OnInit()
        {
            if (click_bg != null)
            {
                Image img = click_bg.GetComponentInChildren<Image>(true);
                if (img != null)
                {
                    img.raycastTarget = true;
                    UIUtil.AddClick(img, () => { if (_onClick != null) _onClick(); });
                }
            }
        }

        public void SetData(string descText, string levelText, Action onClick = null)
        {
            _onClick = onClick;
            if (desc != null) desc.text = descText ?? "";
            if (level != null) level.text = levelText ?? "";
        }
    }
}
