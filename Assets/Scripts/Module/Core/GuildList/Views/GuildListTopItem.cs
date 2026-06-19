using System;
using Shenxiao.Generated.UI.GuildList;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GuildList
{
    /// <summary>
    /// 顶部公会项(对标老客户端 guildList/GuildListTopItem.ts):会名/会长/人数 + 申请(_btn_apply)/建会(_btn_build),
    /// 置顶展示(无公会时引导建会)。
    ///
    /// SetData(name, master, member)。降级:协议未移植 → 文本即时、apply/build → TODO。由 GuildListView 克隆。
    /// </summary>
    public sealed class GuildListTopItem : GuildListTopItemBind
    {
        protected override void OnInit()
        {
            BindBtn(_btn_apply, () => GameLog.Info("GuildList", "申请加入公会 → 待对接 申请协议"));
            BindBtn(_btn_build, () => GameLog.Info("GuildList", "建立公会 → 待对接 建会协议"));
        }

        public void SetData(string name, string master, string member)
        {
            if (_lb_name != null) _lb_name.text = name ?? "";
            if (_lb_master != null) _lb_master.text = master ?? "";
            if (_lb_member != null) _lb_member.text = member ?? "";
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
