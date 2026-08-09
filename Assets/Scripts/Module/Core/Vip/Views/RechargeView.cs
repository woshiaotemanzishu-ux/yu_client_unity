using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Vip;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary>
    /// Read-only recharge page shell. Product payment, welfare claim and platform payment
    /// remain intentionally disabled pending authoritative runtime verification.
    /// </summary>
    public sealed class RechargeView : RechargeViewBind
    {
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_RechargeItem != null) _tpl_RechargeItem.SetActive(false);
            BindClick(_img_close, Hide);
            BindClick(_btn_recharge, BackToVip);
            BindClick(down_img, ScrollToBottom);
            DisableTransaction(more_btn);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            RefreshFromModel();
        }

        protected override void OnHide()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void BackToVip()
        {
            VipFlow.Open("VipBaseView");
            Hide();
        }

        private void ScrollToBottom()
        {
            if (Content != null) Content.verticalNormalizedPosition = 0f;
        }

        private void RefreshFromModel()
        {
            VipModel.VipInfoSnapshot info = VipModel.Instance.VipInfo;
            if (info == null)
            {
                txt_vip_rank.text = "V--";
                _lb_exp_text.text = "--/--";
                _lb_maxlv.gameObject.SetActive(false);
                _lb_cost_left.gameObject.SetActive(false);
                _lb_cost_right.gameObject.SetActive(false);
                _img_diamond.gameObject.SetActive(false);
                _img_exp_highlight.fillAmount = 0f;
                day_exp_text.text = string.Empty;
                day_exp_text.gameObject.SetActive(false);
                return;
            }

            txt_vip_rank.text = "V" + info.VipLevel;
            bool maxLevel = info.VipExp == 0 && info.NeedExp == 0;
            _lb_exp_text.text = maxLevel ? string.Empty : info.VipExp + "/" + info.NeedExp;
            _lb_maxlv.gameObject.SetActive(maxLevel);
            _lb_cost_left.gameObject.SetActive(!maxLevel);
            _lb_cost_right.gameObject.SetActive(!maxLevel);
            _img_diamond.gameObject.SetActive(!maxLevel);
            _img_exp_highlight.fillAmount = maxLevel || info.NeedExp == 0
                ? 1f
                : Mathf.Clamp01((float)info.VipExp / info.NeedExp);
            bool type4CardActive = HasActiveType4Card();
            day_exp_text.text = type4CardActive ? "每日登录+5点经验" : string.Empty;
            day_exp_text.gameObject.SetActive(!maxLevel && type4CardActive);
        }

        private static bool HasActiveType4Card()
        {
            long now = TimeUtil.NowSec();
            for (int i = 0; i < VipModel.Instance.PrivilegeCards.Count; i++)
            {
                VipModel.PrivilegeCard card = VipModel.Instance.PrivilegeCards[i];
                if (card.CardType == 4 && card.IsActive == 1 && (card.Time == 0 || card.Time > now)) return true;
            }

            return false;
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            VipModel.Instance.Changed += RefreshFromModel;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            VipModel.Instance.Changed -= RefreshFromModel;
            _subscribed = false;
        }

        private static void BindClick(Component target, System.Action action)
        {
            Image image = target as Image ?? (target != null ? target.GetComponentInChildren<Image>(true) : null);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }

        private static void DisableTransaction(Component target)
        {
            Image image = target as Image ?? (target != null ? target.GetComponentInChildren<Image>(true) : null);
            if (image != null) image.raycastTarget = false;
        }
    }
}
