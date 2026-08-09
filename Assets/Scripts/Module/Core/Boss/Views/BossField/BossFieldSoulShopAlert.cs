using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.BossField;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossFieldSoulShopAlert : BossFieldSoulShopAlertBind
    {
        public sealed class Args { public string Name; public string Description; public long Have; public Action Use; }
        private Action _use;
        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            BindClick(_img_close, Hide); BindClick(_box_cancel, Hide); BindClick(_box_use, Use);
        }
        protected override void OnShow(object args)
        {
            Args data = args as Args; _use = data?.Use;
            if (_lb_name != null) _lb_name.text = data?.Name ?? "战魂增益道具";
            if (_lb_desc != null) _lb_desc.text = data?.Description ?? "";
            if (_lb_have != null) _lb_have.text = (data?.Have ?? 0).ToString();
        }
        protected override void OnHide() => _use = null;
        private void Use() { Action action = _use; Hide(); action?.Invoke(); }
        private static void BindClick(Component target, Action action)
        {
            if (target == null) return;
            Image image = target as Image ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }
    }
}
