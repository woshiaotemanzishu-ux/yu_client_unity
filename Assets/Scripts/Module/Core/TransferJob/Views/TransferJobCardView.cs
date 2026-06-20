using System;
using Shenxiao.Generated.UI.TransferJob;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.TransferJob
{
    /// <summary>
    /// 转职卡界面(对标老客户端 transferJob/TransferJobCardView.ts):转职卡列表(TransferJobCardItem)+ 标题(lblTitle)/
    /// 说明(lblDesc)+ 关闭(spClose)。
    ///
    /// 降级:转职数据/协议(TransferJobModel)未移植 → 卡项模板隐藏、列表空、spClose → Hide、OnShow TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class TransferJobCardView : TransferJobCardViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_TransferJobCardItem != null) _tpl_TransferJobCardItem.SetActive(false);
            BindBtn(spClose, () => Hide());
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("TransferJob", "转职卡界面打开 → 待对接 转职数据(TransferJobModel)");
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
