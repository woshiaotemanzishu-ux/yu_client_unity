using System;
using Shenxiao.Generated.UI.TransferJob;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.TransferJob
{
    /// <summary>
    /// 转职卡项(对标老客户端 transferJob/TransferJobCardItem.ts):职业图标(imgJob)+ 转职名(lblTransfer)/说明(lblDesc)/
    /// 类型(lblType)+ 确认(btnSure)。
    ///
    /// SetData(transfer, desc, type)。降级:职业图标(配置)待接、转职协议未移植 → 文本即时、btnSure → TODO。由 TransferJobCardView 克隆。
    /// </summary>
    public sealed class TransferJobCardItem : TransferJobCardItemBind
    {
        protected override void OnInit()
        {
            BindBtn(btnSure, () => GameLog.Info("TransferJob", "确认转职 → 待对接 转职协议"));
        }

        public void SetData(string transfer, string desc, string type)
        {
            if (lblTransfer != null) lblTransfer.text = transfer ?? "";
            if (lblDesc != null) lblDesc.text = desc ?? "";
            if (lblType != null) lblType.text = type ?? "";
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
