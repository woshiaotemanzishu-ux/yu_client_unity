using System;
using Shenxiao.Generated.UI.TransferJob;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.TransferJob
{
    /// <summary>
    /// 转职卡项(对标老客户端 transferJob/TransferJobCardItem.ts):职业图标(imgJob)+ 说明(lblDesc=desc2)/
    /// 类型(lblType=desc1)+ 确认(btnSure)。lblTransfer 是按钮自身静态文案"转职",老端从不随数据改写,
    /// 本类同样不提供 setter(照抄老端 updateItem:只写 imgJob/lblType/lblDesc 三项)。
    /// </summary>
    public sealed class TransferJobCardItem : TransferJobCardItemBind
    {
        private Action _onSure;

        protected override void OnInit()
        {
            BindBtn(btnSure, () => _onSure?.Invoke());
        }

        /// <summary>对标老端 updateItem:imgJob 贴 career_{career}_{sex} 图(AtlasUrl('transferJob',...)),
        /// lblType=desc1(archetype 短评),lblDesc=desc2(风味文案)。贴图源在本仓库尚未导入,SetImageAsync
        /// 找不到时静默保留占位(不炸,对标其余"缺图先降级"约定)。</summary>
        public void SetData(int career, int sex, string desc1, string desc2)
        {
            if (lblType != null) lblType.text = desc1 ?? "";
            if (lblDesc != null) lblDesc.text = desc2 ?? "";
            if (imgJob != null)
            {
                _ = ResManager.SetImageAsync(imgJob, GameResPath.GetIcon("transferJob", "career_" + career + "_" + sex), nativeSize: false);
            }
        }

        public void BindSure(Action onSure) => _onSure = onSure;

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
