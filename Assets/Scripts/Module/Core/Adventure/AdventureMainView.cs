using System;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Adventure;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Adventure
{
    /// <summary>天天冒险主页面的安全增量接管；事务与跨岛模型/配置保持显式阻断。</summary>
    public sealed class AdventureMainView : AdventureMainViewBind
    {
        private bool _useTicket = true;
        private float _nextClockRefresh;

        protected override void OnInit()
        {
            BindClick(_img_extro_1, ToggleTicket);
            BindClick(_img_extro_2, ToggleTicket);
            BindClick(_btn, OnBlockedAction);
            BindClick(_img_shop, AdventureFlow.OpenShop);
            BindClick(_img_suit, () => TipsManager.Toast("神装打造跳转跨模块，待统一入口接入"));
            BindClick(_lb_halo_tips, () => TipsManager.Toast("光环跳转跨模块，待统一入口接入"));
            BindClick(_img_ad, () => TipsManager.Toast("广告增次尚未完成安全接入"));

            SetActive(_img_num, false);
            SetActive(_red, false);
            SetActive(_img_shop_red, false);
            SetActive(_box_halo, false);
            SetActive(_img_ad, false);
            ApplyTicketToggle();
        }

        protected override void OnShow(object args)
        {
            AdventureModel.Instance.Changed += Refresh;
            if (!AdventureModel.Instance.HasBoardState)
                AdventureController.Instance.RequestBoardState();
            Refresh();
            RefreshClock();
        }

        protected override void OnHide()
        {
            AdventureModel.Instance.Changed -= Refresh;
        }

        private void Update()
        {
            if (!IsShown || Time.unscaledTime < _nextClockRefresh) return;
            RefreshClock();
        }

        private void Refresh()
        {
            if (!IsShown) return;
            AdventureModel model = AdventureModel.Instance;
            if (labelDisplay != null) labelDisplay.text = model.IsAtResetPosition ? "重置" : "前进";
            if (_lb_ref_num != null)
                _lb_ref_num.text = "剩余重置次数:--";
            SetActive(_lb_vip, false);
            if (_lb_tik != null) _lb_tik.text = "冒险券:--张";
            if (_lb_tik_desc != null) _lb_tik_desc.text = "15";

            int free = model.GetFreeActionRemaining();
            if (_lb_cost_num != null)
            {
                if (!model.HasBoardState) _lb_cost_num.text = "数据加载中";
                else if (free > 0) _lb_cost_num.text = model.IsAtResetPosition
                    ? "剩余免费重置:" + free + "次"
                    : "剩余免费:" + free + "次";
                else _lb_cost_num.text = "费用配置尚未迁移";
            }
            SetActive(_img_cost_icon, false);
            SetActive(_red, model.HasFreeThrowRed);
            SetActive(_img_shop_red, false);
        }

        private void ToggleTicket()
        {
            _useTicket = !_useTicket;
            ApplyTicketToggle();
        }

        private void ApplyTicketToggle()
        {
            SetActive(_img_extro_1, !_useTicket);
            SetActive(_img_extro_2, _useTicket);
        }

        private void OnBlockedAction()
        {
            AdventureModel model = AdventureModel.Instance;
            if (!model.IsActivityOpen()) TipsManager.Toast("活动已经结束");
            else if (!model.HasBoardState) TipsManager.Toast("棋盘数据尚未加载");
            else if (model.IsAtResetPosition) TipsManager.Toast("重置事务尚未完成安全接入");
            else TipsManager.Toast("投掷事务尚未完成安全接入");
            // 明确不发送 42702/42703；结果移动、奖励弹窗和即时刷新叶保持 blocked。
        }

        private void RefreshClock()
        {
            _nextClockRefresh = Time.unscaledTime + 1f;
            if (_lb_time == null) return;
            TimeSpan remain = DateTime.Today.AddDays(1) - DateTime.Now;
            if (remain < TimeSpan.Zero) remain = TimeSpan.Zero;
            _lb_time.text = "距下次系统重置:  " +
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
