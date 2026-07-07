using System;
using Shenxiao.Generated.UI.Setting;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置-屏蔽项(对标老客户端 setting/SettingShieldItem.ts):
    /// 一行屏蔽开关——文案 _lb_text(=cfg.name)+ 勾选态(两张互斥的勾选图)。
    /// 老端:check_img 为未勾选态(is_open==0 时可见)、check_img_1 为已勾选态(is_open==1 时可见);
    /// 点击勾选图 → onToggle 回调(由父 View 注入:150 级校验/极简模式确认/10203 上报都在父级,
    /// 对标老端 CheckClickFun → PackageAndSend10203)。SetData 可重复调用刷新勾选态。
    /// </summary>
    public sealed class SettingShieldItem : SettingShieldItemBind
    {
        private Action _onToggle;
        private bool _clickBound;

        protected override void OnInit()
        {
        }

        /// <summary>
        /// 设置屏蔽项数据(对标老端 dataChanged):写文案 + 按勾选态切 check_img(未勾)/check_img_1(已勾)互斥;
        /// onToggle=点击勾选图回调(null 保持上次绑定,纯展示项可不传)。
        /// </summary>
        public void SetData(string text, bool on, Action onToggle = null)
        {
            if (_lb_text != null)
            {
                _lb_text.gameObject.SetActive(true);
                _lb_text.text = text ?? string.Empty;
            }

            if (onToggle != null) _onToggle = onToggle;

            // 老端:is_open==0 → check_img 可见、check_img_1 隐藏;is_open==1 → 反之。on=true 表示已勾选(is_open==1)。
            if (check_img != null)
            {
                check_img.gameObject.SetActive(!on);
                check_img.raycastTarget = true;
            }
            if (check_img_1 != null)
            {
                check_img_1.gameObject.SetActive(on);
                check_img_1.raycastTarget = true;
            }

            if (!_clickBound)
            {
                _clickBound = true;
                if (check_img != null) UIUtil.AddClick(check_img, OnClickCheck);
                if (check_img_1 != null) UIUtil.AddClick(check_img_1, OnClickCheck);
            }
        }

        private void OnClickCheck()
        {
            _onToggle?.Invoke();
        }
    }
}
