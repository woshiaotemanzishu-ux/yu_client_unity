using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.LimitLevelShop;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.LimitLevelShop
{
    public sealed class LimitLevelShopTabItem : LimitLevelShopTabItemBind
    {
        private Action _onClick;
        private LimitLevelShopModel.GiftEntry _gift;
        private ushort _grade;

        protected override void OnInit() => BindClick(_img_click, HandleClick);

        public void SetData(LimitLevelShopModel.GiftEntry gift, LimitLevelShopModel.GiftConfigEntry cfg,
            Vector2 position, Action onClick)
        {
            if (!IsInitialized) Show();
            _gift = gift;
            _grade = cfg?.Grade ?? 0;
            _onClick = onClick;
            if (transform is RectTransform rect) rect.anchoredPosition = position;

            IReadOnlyList<ErlangTerm> tabShow = LimitLevelShopView.FindTuple(cfg?.Show, "tab_show");
            if (tabShow != null && tabShow.Count >= 3)
            {
                LoadImage(_img_name, tabShow[1].As<string>());
                LoadImage(_img_show, tabShow[2].As<string>());
            }
            bool oldBought = gift != null && gift.OpenTimes >= 2 && gift.GetState(_grade, true) == 1;
            if (_img_only_buy_once != null)
            {
                _img_only_buy_once.gameObject.SetActive(oldBought);
                if (oldBought) LoadImage(_img_only_buy_once, "ui_only_buy_once");
            }
        }

        public void SetSelected(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }

        private void HandleClick()
        {
            if (_gift != null && _gift.OpenTimes >= 2 && _gift.GetState(_grade, true) == 1)
            {
                TipsManager.Toast("抢购限时返场！只能购买之前未购买的礼包");
                return;
            }
            _onClick?.Invoke();
        }

        private static void LoadImage(Image image, string name)
        {
            if (image == null || string.IsNullOrEmpty(name)) return;
            image.gameObject.SetActive(true);
            _ = ResManager.SetImageAsync(image, GameResPath.GetIconOtherPath("limitLevelShop", name), false, false);
        }

        private static void BindClick(Component target, Action action)
        {
            if (target == null) return;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>();
            if (graphic != null) UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(target, action);
        }
    }
}
