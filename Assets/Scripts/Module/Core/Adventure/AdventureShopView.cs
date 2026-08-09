using System;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Adventure;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Adventure
{
    /// <summary>冒险商店弹窗外壳；六格数据与刷新/购买事务等待 42704..42706 正式接入。</summary>
    public sealed class AdventureShopView : AdventureShopViewBind
    {
        private float _nextClockRefresh;

        protected override void OnInit()
        {
            BindClick(_img_close, AdventureFlow.CloseShop);
            BindClick(_gp_btn, () => TipsManager.Toast("商店刷新事务尚未完成安全接入"));
            if (_lb_ref != null) _lb_ref.text = "刷新";
            if (_lb_cost != null) _lb_cost.text = "";
            SetActive(_img_cost, false);
        }

        protected override void OnShow(object args)
        {
            RefreshClock();
            TipsManager.Toast("商店数据配置与 42704 查询尚未接入");
        }

        private void Update()
        {
            if (!IsShown || Time.unscaledTime < _nextClockRefresh) return;
            RefreshClock();
        }

        private void RefreshClock()
        {
            _nextClockRefresh = Time.unscaledTime + 1f;
            if (_lb_time == null) return;
            TimeSpan remain = DateTime.Today.AddDays(1) - DateTime.Now;
            if (remain < TimeSpan.Zero) remain = TimeSpan.Zero;
            _lb_time.text = "距商店自动刷新:" +
                ((int)remain.TotalHours).ToString("00") + ":" +
                remain.Minutes.ToString("00") + ":" + remain.Seconds.ToString("00");
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static void BindClick(Component target, Action action)
        {
            if (target == null) return;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
            if (graphic != null) UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(target, action);
        }
    }
}
