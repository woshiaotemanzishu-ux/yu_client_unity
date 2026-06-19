using System;
using Shenxiao.Generated.UI.GuildList;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GuildList
{
    /// <summary>
    /// 公会项(对标老客户端 guildList/GuildListItem.ts):会名(_lb_name)/会长(_lb_master)/人数(_lb_member)/
    /// 条件(_lb_cond)+ 申请(_btn_apply)。
    ///
    /// SetData(name, master, member, cond)。降级:申请协议未移植 → 文本即时、_btn_apply → TODO。由 GuildListView 克隆。
    /// </summary>
    public sealed class GuildListItem : GuildListItemBind
    {
        protected override void OnInit()
        {
            BindBtn(_btn_apply, () => GameLog.Info("GuildList", "申请加入公会 → 待对接 申请协议"));
        }

        public void SetData(string name, string master, string member, string cond)
        {
            if (_lb_name != null) _lb_name.text = name ?? "";
            if (_lb_master != null) _lb_master.text = master ?? "";
            if (_lb_member != null) _lb_member.text = member ?? "";
            if (_lb_cond != null) _lb_cond.text = cond ?? "";
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
