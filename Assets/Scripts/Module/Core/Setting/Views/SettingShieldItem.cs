using Shenxiao.Generated.UI.Setting;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置-屏蔽项(对标老客户端 setting/SettingShieldItem.ts):
    /// 一行屏蔽开关——文案 _lb_text(=cfg.name)+ 勾选态(两张互斥的勾选图)。
    /// 老端:check_img 为未勾选态(is_open==0 时可见)、check_img_1 为已勾选态(is_open==1 时可见);
    /// 点击勾选图走 CheckClickFun → 校验(150 级前禁取消自动拾取/极简模式 Alert)后 PackageAndSend10203 上报,
    /// 并监听 SettingModel.UPDATE_ONE_SETTING_INFO 刷新自身。
    ///
    /// 降级:SettingModel(10203 上报/极简模式/事件刷新)、RoleManager 等级校验、Alert/SysInfo、PlatformManager 字号
    /// 均未移植 → 本类只做最小可用的"降级渲染器":SetData(text,on) 写文案 + 切勾选态(check_img/check_img_1 互斥)。
    /// OnInit 不做特殊处理(无红点/无 _tpl_*/无关闭按钮)。Null-guard 每处访问。
    /// </summary>
    public sealed class SettingShieldItem : SettingShieldItemBind
    {
        protected override void OnInit()
        {
        }

        /// <summary>
        /// 设置屏蔽项数据(对标老端 dataChanged):
        /// 写入文案 _lb_text=cfg.name;按勾选态切 check_img(未勾选,on=false 时可见)/ check_img_1(已勾选,on=true 时可见)互斥显隐。
        /// </summary>
        public void SetData(string text, bool on)
        {
            if (_lb_text != null)
            {
                _lb_text.gameObject.SetActive(true);
                _lb_text.text = text ?? string.Empty;
            }

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
        }
    }
}
