using System;
using TMPro;
using Shenxiao.Generated.UI.Elim;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Elim
{
    /// <summary>
    /// 消除主界面(对标老客户端 elim/ElimMainView.ts):棋盘(con_board)+ 分数(_score/_lb_myscore/_game_score)+
    /// 回合倒计时(_box_round/_count_down)+ 开始按钮(start_img)。
    ///
    /// 降级:消除游戏(ElimGameMgr/棋盘生成/分数协议)未移植 → 棋盘空、分数默认 0、start_img → TODO、OnShow TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class ElimMainView : ElimMainViewBind
    {
        protected override void OnInit()
        {
            BindBtn(start_img, () => GameLog.Info("Elim", "消除开始 → 待对接 ElimGameMgr/开始协议"));
            DefaultText(_score, "0");
            DefaultText(_lb_myscore, "0");
            DefaultText(_game_score, "0");
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Elim", "消除主界面打开 → 待对接 消除游戏数据(ElimGameMgr)");
        }

        private static void DefaultText(TextMeshProUGUI lb, string s)
        {
            if (lb != null) lb.text = s;
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
