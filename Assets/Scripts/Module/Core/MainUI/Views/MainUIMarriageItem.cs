using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 婚恋新手预览项(对标老客户端 MainUIMarriageItem.ts):一个图标,点击打开婚恋预览。
    ///
    /// SetIcon(icon) 设图标(mainUI atlas);OnInit 绑点击。降级:MarriageModel.ClickForeShowFunc(婚恋预览)
    /// 与 MainUIManager 移动组件未移植 → 点击打日志「待对接」;移动/定位归预制体。事件驱动,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class MainUIMarriageItem : MainUIMarriageItemBind
    {
        private bool _clickBound;

        protected override void OnInit()
        {
            BindClickOnce();
        }

        /// <summary>设图标(对标 SetIcon:GetIcon("mainUI", icon))。</summary>
        public void SetIcon(string icon)
        {
            if (_img != null && !string.IsNullOrEmpty(icon))
                _ = ResManager.SetImageAsync(_img, GameResPath.GetIcon("mainUI", icon), nativeSize: false);
            BindClickOnce();
        }

        private void BindClickOnce()
        {
            if (_clickBound || _img == null) return;
            _img.raycastTarget = true;
            UIUtil.AddClick(_img, OnClick);
            _clickBound = true;
        }

        private void OnClick()
        {
            // TODO 待接:MarriageModel.ClickForeShowFunc()(打开婚恋预览)。
            GameLog.Info("MainUI", "点击婚恋预览图标 → 待对接 MarriageModel.ClickForeShowFunc");
        }
    }
}
