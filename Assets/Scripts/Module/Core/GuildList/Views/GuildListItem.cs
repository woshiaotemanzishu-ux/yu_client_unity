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
    /// 公会项(对标老客户端 guildList/GuildListItem.ts):会名(_lb_name)/会长(_lb_master)/人数(_lb_member)/
    /// 条件(_lb_cond:无条件限制 或 "战力N以上",已申请则"等待审批")+ 申请(_btn_apply→40002 单个申请)。
    /// </summary>
    public sealed class GuildListItem : GuildListItemBind
    {
        private GuildJoinModel.GuildBrief _data;

        private static readonly Color COLOR_WHITE = Color.white;
        private static readonly Color COLOR_GREEN = new Color(0.42f, 0.85f, 0.46f);

        protected override void OnInit()
        {
            BindBtn(_btn_apply, OnClickApply);
        }

        public void SetData(GuildJoinModel.GuildBrief data)
        {
            _data = data;
            if (data == null) return;

            if (_lb_name != null) _lb_name.text = data.Name;
            if (_lb_master != null) _lb_master.text = data.ChiefName;
            if (_lb_member != null) _lb_member.text = data.MemberNum + "/" + data.MemberCapacity;

            if (_lb_cond != null)
            {
                if (data.IsApply)
                {
                    _lb_cond.color = COLOR_GREEN;
                    _lb_cond.text = "等待审批";
                }
                else
                {
                    string str = data.AutoApprovePower == 0 ? "无条件限制" : "战力" + data.AutoApprovePower + "以上";
                    long myPower = Shenxiao.Module.Core.Role.RoleModel.Instance.CombatPower;
                    _lb_cond.color = myPower >= data.AutoApprovePower ? COLOR_WHITE : COLOR_GREEN;
                    _lb_cond.text = str;
                }
            }
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
