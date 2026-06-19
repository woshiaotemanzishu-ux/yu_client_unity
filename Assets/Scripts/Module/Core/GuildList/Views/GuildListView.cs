using System;
using Shenxiao.Generated.UI.GuildList;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GuildList
{
    /// <summary>
    /// 公会列表界面(对标老客户端 guildList/GuildListView.ts):公会列表(GuildListItem/TopItem)+ 建会(_btn_build)/
    /// 申请(_btn_apply)/关闭(_btn_close)。
    ///
    /// 降级:GuildModel/公会列表协议未移植 → 列表项模板隐藏、列表空、close → Hide、build/apply → TODO、OnShow TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class GuildListView : GuildListViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_GuildListItem != null) _tpl_GuildListItem.SetActive(false);
            if (_tpl_GuildListTopItem != null) _tpl_GuildListTopItem.SetActive(false);
            BindBtn(_btn_close, () => Hide());
            BindBtn(_btn_build, () => GameLog.Info("GuildList", "建立公会 → 待对接 GuildModel 建会协议"));
            BindBtn(_btn_apply, () => GameLog.Info("GuildList", "申请公会 → 待对接 GuildModel 申请协议"));
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("GuildList", "公会列表打开 → 待对接 公会列表数据(GuildModel)");
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
