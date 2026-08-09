using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Res;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 我要变强·行项(对标老端 daily/DailyStrongerItem.ts):名称(_lb_name)+ 描述(_lb_desc)+ 等级(_lb_level)+
    /// 前往(_btn_go)。由 DailyStrongerView 按 config_to_be_strong + 61801 状态表列表克隆填充。
    /// 降级:_btn_go 跳转按 jump_id 分发到具体外部系统的映射表未移植(规格§0"没有就 log TODO"),仅打日志。
    /// </summary>
    public sealed class DailyStrongerItem : DailyStrongerItemBind
    {
        private int _jumpId;

        protected override void OnInit()
        {
            BindClick(_btn_go, () => GameLog.Info("Daily", "点击[我要变强·前往] jump_id={0} → 待对接(OpenFun 跳转映射表未移植)", _jumpId));
        }

        /// <summary>填一条推荐项(对标 DailyStrongerItem.SetData);state==1 且 day_limit==1 → 置灰"今日已完成"。</summary>
        public void SetData(int id, bool finishedToday)
        {
            _jumpId = DailyConfigs.GetStrongJumpId(id);
            if (_lb_name != null) _lb_name.text = DailyConfigs.GetStrongName(id);
            if (_lb_desc != null) _lb_desc.text = DailyConfigs.GetStrongDesc(id);
            if (_lb_level != null) _lb_level.text = "Lv." + DailyConfigs.GetStrongLv(id);
            if (labelDisplay != null) labelDisplay.text = finishedToday ? "今日已完成" : "前往";
            if (_btn_go != null) _btn_go.gameObject.SetActive(!finishedToday);
            if (_img_icon != null)
                _ = ResManager.SetImageAsync(_img_icon, GameResPath.GetDailyIconPath(DailyConfigs.GetStrongIconId(id).ToString()), false, false);

            int starCount = Mathf.Clamp(DailyConfigs.GetStrongStar(id), 0, 5);
            if (star != null)
                for (int i = 0; i < star.childCount; i++) star.GetChild(i).gameObject.SetActive(i < starCount);
        }

        private static void BindClick(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
