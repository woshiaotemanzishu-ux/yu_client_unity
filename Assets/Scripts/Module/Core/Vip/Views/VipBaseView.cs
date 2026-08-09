using System.Collections.Generic;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Vip;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary>
    /// Read-only VIP page shell. Transaction leaves stay disabled until their protocol and
    /// authoritative runtime refresh paths are migrated and verified.
    /// </summary>
    public sealed class VipBaseView : VipBaseViewBind
    {
        private readonly List<VipTabButtonBind> _tabs = new List<VipTabButtonBind>(2);
        private VipPrivilegeCardViewBind _cardPage;
        private VipPrivilegeShowViewBind _benefitPage;
        private VipTopCardItemBind _topCards;
        private int _selectedTab;
        private int _selectedCardType = 4;
        private bool _subscribed;

        protected override void OnInit()
        {
            HideTemplates();
            BindClick(close_btn, Hide);
            BindClick(recharge_btn, OpenRecharge);
            DisableTransaction(check);
            BuildReadOnlyShell();
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            RefreshFromModel();
            SetTab(_selectedTab);
        }

        protected override void OnHide()
        {
            Unsubscribe();
            // Legacy close lifecycle resets the next open to the benefit tab.
            _selectedTab = 1;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void BuildReadOnlyShell()
        {
            if (tab_content != null && tab_content.content != null && _tpl_VipTabButton != null)
            {
                CreateTab("特权卡", 0);
                CreateTab("特权福利", 1);
            }

            _cardPage = CreateView<VipPrivilegeCardViewBind>(_tpl_VipPrivilegeCardView, view_content);
            _benefitPage = CreateView<VipPrivilegeShowViewBind>(_tpl_VipPrivilegeShowView, view_content);
            _topCards = CreateView<VipTopCardItemBind>(_tpl_VipTopCardItem, _Box_top_vip);

            if (_cardPage != null)
            {
                _cardPage.tip_label.text = string.Empty;
                _cardPage.tip_label.gameObject.SetActive(false);
                _cardPage.card_scroller.vertical = false;
                _cardPage.card_scroller.horizontal = false;
                CreateBlockedCardShell();
            }

            if (_benefitPage != null)
            {
                DisableTransaction(_benefitPage.receive_btn);
                DisableTransaction(_benefitPage.vip_box_btn);
                SetActive(_benefitPage.red_dot, false);
                SetActive(_benefitPage.vip_box_red_dot, false);
                SetActive(_benefitPage.left_red, false);
                SetActive(_benefitPage.right_red, false);
            }

            if (_topCards != null)
            {
                BindCardSelector(1, _topCards._Image_top_bg_vip_1, _topCards._Image_top_select_vip_1,
                    _topCards._Image_vip_level_1, _topCards._Image_time_bg_1, _topCards._Image_discount_bg_1);
                BindCardSelector(2, _topCards._Image_top_bg_vip_2, _topCards._Image_top_select_vip_2,
                    _topCards._Image_vip_level_2, _topCards._Image_time_bg_2, _topCards._Image_discount_bg_2);
                BindCardSelector(4, _topCards._Image_top_bg_vip_4, _topCards._Image_top_select_vip_4,
                    _topCards._Image_vip_level_4, _topCards._Image_time_bg_4, _topCards._Image_discount_bg_4);
            }
        }

        private void CreateBlockedCardShell()
        {
            if (_tpl_VipCardItem == null || _cardPage == null || _cardPage.card_scroller == null ||
                _cardPage.card_scroller.content == null) return;

            VipCardItemBind item = CreateView<VipCardItemBind>(_tpl_VipCardItem, _cardPage.card_scroller.content);
            if (item == null) return;
            SetActive(item.btn_group, false);
            SetActive(item.price_group, false);
            SetActive(item.more_btn, false);
            SetActive(item.more_label, false);
            SetActive(item.offer_img, false);
            SetActive(item.red_dot, false);
        }

        private void CreateTab(string title, int index)
        {
            GameObject instance = Instantiate(_tpl_VipTabButton, tab_content.content, false);
            instance.name = "VipTab_" + index;
            instance.SetActive(true);
            VipTabButtonBind tab = instance.GetComponent<VipTabButtonBind>();
            if (tab == null) return;
            tab.Show();
            tab.labelDisplay.text = title;
            SetActive(tab.vip_red, false);
            int captured = index;
            BindClick(tab._Image1, () => SetTab(captured));
            BindClick(tab._Image2, () => SetTab(captured));
            _tabs.Add(tab);
        }

        private void SetTab(int index)
        {
            _selectedTab = index == 1 ? 1 : 0;
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool selected = i == _selectedTab;
                SetActive(_tabs[i]._Image1, !selected);
                SetActive(_tabs[i]._Image2, selected);
            }

            SetViewVisible(_cardPage, _selectedTab == 0);
            SetViewVisible(_benefitPage, _selectedTab == 1);
            SetViewVisible(_topCards, _selectedTab == 0);
            SetHeaderVisible(_selectedTab == 1);
        }

        private void BindCardSelector(int cardType, params Component[] targets)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                int captured = cardType;
                BindClick(targets[i], () => SelectCard(captured));
            }
        }

        private void SelectCard(int cardType)
        {
            _selectedCardType = cardType;
            RefreshTopCards();
        }

        private void RefreshFromModel()
        {
            VipModel model = VipModel.Instance;
            VipModel.VipInfoSnapshot info = model.VipInfo;
            if (info == null)
            {
                txt_vip_rank.text = "V--";
                exp_text.text = "--/--";
                day_exp_text.text = string.Empty;
                toggle.gameObject.SetActive(false);
                exp_highlight.fillAmount = 0f;
            }
            else
            {
                txt_vip_rank.text = "V" + info.VipLevel;
                bool maxLevel = info.VipExp == 0 && info.NeedExp == 0;
                exp_text.text = maxLevel ? "已满级" : info.VipExp + "/" + info.NeedExp;
                exp_highlight.fillAmount = maxLevel || info.NeedExp == 0
                    ? 1f
                    : Mathf.Clamp01((float)info.VipExp / info.NeedExp);
                toggle.gameObject.SetActive(info.VipHide != 0);
                int activeCardType = GetActiveCardType(model);
                day_exp_text.text = activeCardType == 4
                    ? "每日登录+5点经验"
                    : activeCardType == 0
                        ? "需购买以下特权卡才能成功激活且获得vip经验哦"
                        : string.Empty;
            }

            RefreshTopCards();
        }

        private void RefreshTopCards()
        {
            if (_topCards == null) return;
            SetActive(_topCards._Image_top_select_vip_1, _selectedCardType == 1);
            SetActive(_topCards._Image_top_select_vip_2, _selectedCardType == 2);
            SetActive(_topCards._Image_top_select_vip_4, _selectedCardType == 4);
            _topCards._lable_time_1.text = "30天";
            _topCards._lable_time_2.text = "90天";
            _topCards._lable_time_4.text = "180天";
        }

        private static int GetActiveCardType(VipModel model)
        {
            long now = TimeUtil.NowSec();
            int activeCardType = 0;
            for (int i = 0; i < model.PrivilegeCards.Count; i++)
            {
                VipModel.PrivilegeCard card = model.PrivilegeCards[i];
                bool notExpired = card.Time == 0 || card.Time > now;
                if (card.IsActive == 1 && notExpired && card.CardType > activeCardType)
                    activeCardType = card.CardType;
            }

            return activeCardType;
        }

        private void SetHeaderVisible(bool visible)
        {
            SetActive(recharge_btn, visible);
            SetActive(vip_image, visible);
            SetActive(day_exp_text, visible);
            SetActive(exp_text, visible);
            SetActive(exp_highlight, visible);
            SetActive(cost_left_label, visible);
            SetActive(cost_right_label, visible);
            SetActive(_Image4, visible);
            SetActive(_Image2, visible);
            SetActive(_Image3, visible);
        }

        private void OpenRecharge()
        {
            VipFlow.Open("RechargeView");
            Hide();
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

        private void HideTemplates()
        {
            SetActive(_tpl_VipPrivilegeCardView, false);
            SetActive(_tpl_VipCardItem, false);
            SetActive(_tpl_VipSubFlagItem, false);
            SetActive(_tpl_VipPrivilegeShowView, false);
            SetActive(_tpl_VipAwardItem, false);
            SetActive(_tpl_VipInstructionItem, false);
            SetActive(_tpl_VipTabButton, false);
            SetActive(_tpl_VipTopCardItem, false);
        }

        private static T CreateView<T>(GameObject template, Transform parent) where T : BaseView
        {
            if (template == null || parent == null) return null;
            GameObject instance = Instantiate(template, parent, false);
            instance.name = typeof(T).Name;
            instance.SetActive(true);
            T view = instance.GetComponent<T>();
            if (view != null) view.Show();
            return view;
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

        private static void SetViewVisible(BaseView view, bool visible)
        {
            if (view == null) return;
            if (visible) view.Show();
            else view.Hide();
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static void SetActive(GameObject gameObject, bool active)
        {
            if (gameObject != null) gameObject.SetActive(active);
        }
    }
}
