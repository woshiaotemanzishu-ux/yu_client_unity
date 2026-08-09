using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.BossPersonal;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Boss.Views.BossPersonal
{
    public sealed class BossPersonalAlert : BossPersonalAlertBind
    {
        public sealed class Args
        {
            public string Name;
            public string Hint;
            public int Have;
            public int Need;
            public int Cost;
            public Action Confirm;
        }

        private Action _confirm;

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            BindClick(_img_close, Hide);
            BindClick(_btn_cancel, Hide);
            BindClick(_btn_enter, Confirm);
        }

        protected override void OnShow(object args)
        {
            Args data = args as Args;
            _confirm = data?.Confirm;
            if (_lb_name != null) _lb_name.text = data?.Name ?? "挑战消耗";
            if (_lb_hint != null) _lb_hint.text = data?.Hint ?? "挑战条件不足";
            if (_lb_num != null) _lb_num.text = (data?.Have ?? 0).ToString();
            if (_lb_need_num != null) _lb_need_num.text = (data?.Need ?? 0).ToString();
            if (_lb_cost != null) _lb_cost.text = (data?.Cost ?? 0).ToString();
        }

        protected override void OnHide() => _confirm = null;

        private void Confirm()
        {
            Action callback = _confirm;
            Hide();
            callback?.Invoke();
        }

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
