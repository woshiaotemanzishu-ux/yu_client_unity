using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.BossField;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossAbyssFailureView : BossAbyssFailureViewBind
    {
        public sealed class Args { public string OwnerName; public long Fighting; }
        private float _closeAt;

        protected override void OnInit()
        { if (_Image1 != null) UIUtil.AddClick(_Image1, Hide); }

        protected override void OnShow(object args)
        {
            Args data = args as Args;
            if (_lb_name != null) _lb_name.text = "归属者：" + (data?.OwnerName ?? "");
            if (_lb_fighting != null) _lb_fighting.text = (data?.Fighting ?? 0).ToString();
            _closeAt = Time.unscaledTime + 10f;
        }

        private void Update()
        {
            if (!IsShown) return;
            int left = Mathf.Max(0, Mathf.CeilToInt(_closeAt - Time.unscaledTime));
            if (_lb_tips != null) _lb_tips.text = "点击任意位置继续(" + left + "s)";
            if (left <= 0) Hide();
        }
    }
}
