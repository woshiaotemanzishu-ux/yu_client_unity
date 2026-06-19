using System;
using Shenxiao.Generated.UI.Elim;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Elim
{
    /// <summary>
    /// 消除对局/排名项(对标老客户端 elim/ElimMainViewItem.ts):3 玩家名(name_0..2)/分数(score_0..2)+ 棋盘 + 排名(rank_lb)
    /// + 倒计时 + 关闭(_btn_close)/礼包(gift_btn)。
    ///
    /// 降级:对局数据(ElimGameMgr/排名协议)未移植 → 名字/分数默认、棋盘空、_btn_close → Hide、gift_btn → TODO。
    /// 由消除界面克隆。
    /// </summary>
    public sealed class ElimMainViewItem : ElimMainViewItemBind
    {
        protected override void OnInit()
        {
            BindBtn(_btn_close, () => Hide());
            BindBtn(gift_btn, () => GameLog.Info("Elim", "消除礼包 → 待对接 奖励协议"));
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
