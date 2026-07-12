using System;
using Shenxiao.Generated.UI.GuildList;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GuildList
{
    /// <summary>
    /// 顶部公会项(对标老客户端 guildList/GuildListTopItem.ts):会名/会长/人数 + 申请(_btn_apply→40002)/
    /// 建会(_btn_build→复用 GuildJoinController.Create 占位默认名),置顶展示前3名(战力序,数据为空则
    /// 显"建会"引导态,隐藏数据组;老端灰化底图效果本轮未移植,仅接显隐)。
    /// </summary>
    public sealed class GuildListTopItem : GuildListTopItemBind
    {
        private GuildJoinModel.GuildBrief _data;

        protected override void OnInit()
        {
            BindBtn(_btn_apply, OnClickApply);
            BindBtn(_btn_build, OnClickBuild);
        }

        /// <summary>data==null → 空位态(隐藏数据组,显示建会引导;对标老端 dataChanged 的 else 分支)。</summary>
        public void SetData(GuildJoinModel.GuildBrief data)
        {
            _data = data;
            bool has = data != null;
            if (_gp_data != null) _gp_data.gameObject.SetActive(has);
            if (_btn_build != null) _btn_build.gameObject.SetActive(!has);
            if (!has) return;

            if (_lb_name != null) _lb_name.text = data.Name;
            if (_lb_member != null) _lb_member.text = data.MemberNum + "/" + data.MemberCapacity;
            if (_lb_master != null) _lb_master.text = data.ChiefName;
        }

        private void OnClickApply()
        {
            if (_data == null) return;
            if (GuildModel.IsHasGuild())
            {
                Shenxiao.Common.Tips.TipsManager.Toast("您当前已有所属结社");
                return;
            }
            GuildJoinController.Instance.ApplyOne(_data.GuildId);
        }

        private void OnClickBuild()
        {
            if (GuildModel.IsHasGuild())
            {
                Shenxiao.Common.Tips.TipsManager.Toast("您当前已有所属结社");
                return;
            }
            GuildJoinController.Instance.Create("神霄阁");
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
