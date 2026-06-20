using System;
using Shenxiao.Generated.UI.Cheat;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Cheat
{
    /// <summary>
    /// 作弊/GM 命令输入界面(对标老客户端 cheat/CheatInputView.ts):命令输入(cheatInput)+ 确认(okBtn)+ 搜索(serachInput/serachBtn)
    /// + 命令列表(TypeScrollView/ScrollView)+ 关闭(closeBtn)。开发用 GM 面板。
    ///
    /// 降级:GM 命令执行/搜索协议未移植 → okBtn/serachBtn 点击打 TODO(并回显输入内容)、closeBtn → Hide、OnShow TODO。
    /// 输入框由预制体上的 TMP_InputField 承载(可输入)。事件驱动,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class CheatInputView : CheatInputViewBind
    {
        protected override void OnInit()
        {
            BindBtn(closeBtn, () => Hide());
            BindBtn(okBtn, () => GameLog.Info("Cheat", "执行 GM 命令 [{0}] → 待对接 GM 命令协议", cheatInput != null ? cheatInput.text : ""));
            BindBtn(serachBtn, () => GameLog.Info("Cheat", "搜索 GM 命令 [{0}] → 待对接 命令搜索", serachInput != null ? serachInput.text : ""));
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Cheat", "GM 作弊面板打开 → 待对接 命令列表数据");
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
