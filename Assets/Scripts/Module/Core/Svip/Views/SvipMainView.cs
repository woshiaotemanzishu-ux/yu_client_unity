using System;
using Shenxiao.Generated.UI.Svip;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Svip
{
    /// <summary>
    /// 至尊VIP界面(对标老客户端 svip/SvipMainView.ts):特权列表(SvipMainItem)+ 角色头像(image_role_icon/frame)+
    /// 联系客服(image_contact_us/image_add_customer)+ 关闭(btn_close)。
    ///
    /// 降级:VIP 数据(VipModel)/客服链接未移植 → 特权项模板隐藏、btn_close → Hide、联系客服 → TODO、OnShow TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class SvipMainView : SvipMainViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_SvipMainItem != null) _tpl_SvipMainItem.SetActive(false);
            BindBtn(btn_close, () => Hide());
            BindBtn(image_contact_us, () => GameLog.Info("Svip", "联系客服 → 待对接 客服链接"));
            BindBtn(image_add_customer, () => GameLog.Info("Svip", "添加客服 → 待对接 客服链接"));
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Svip", "至尊VIP界面打开 → 待对接 VIP 数据(VipModel)");
        }

        private void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
