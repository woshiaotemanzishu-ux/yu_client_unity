using System;
using Shenxiao.Generated.UI.MainStronger;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.MainStronger
{
    /// <summary>
    /// 变强功能按钮(对标老客户端 mainUI/MainUIStrongerView 内 MainUIStrongerBtn):图标(imgBtn)+ 功能名(lblName),点击进对应功能。
    ///
    /// SetData(name, onClick)。降级:功能图标(配置)待接 → 名字/点击即时可用。由 MainUIStrongerView 克隆。
    /// </summary>
    public sealed class MainUIStrongerBtn : MainUIStrongerBtnBind
    {
        private Action _onClick;

        protected override void OnInit()
        {
            if (imgBtn != null)
            {
                imgBtn.raycastTarget = true;
                UIUtil.AddClick(imgBtn, () => { if (_onClick != null) _onClick(); });
            }
        }

        public void SetData(string name, Action onClick = null)
        {
            _onClick = onClick;
            if (lblName != null) lblName.text = name ?? "";
        }
    }
}
