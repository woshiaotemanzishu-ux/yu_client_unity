using System;
using Shenxiao.Generated.UI.MainStronger;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.MainStronger
{
    /// <summary>我要变强列表项；功能目标由 MainStrongerFlow 注册表提供。</summary>
    public sealed class MainUIStrongerBtn : MainUIStrongerBtnBind
    {
        private Action _onClick;

        protected override void OnInit()
        {
            if (imgBtn == null) return;
            imgBtn.raycastTarget = true;
            UIUtil.AddClick(imgBtn, () => _onClick?.Invoke());
        }

        public void SetData(string name, Action onClick = null)
        {
            _onClick = onClick;
            if (lblName != null) lblName.text = name ?? string.Empty;
        }

        public void SetData(MainStrongerConfigs.Feature feature)
        {
            SetData(feature?.Name, () => MainStrongerFlow.TryOpen(feature));
        }
    }
}
