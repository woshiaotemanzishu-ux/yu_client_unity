using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 公会成员页(对标老客户端 guild/GuildMemberView.ts):成员列表(40006,服务端无分页,规模=member_capacity)+
    /// 审批设置入口(_btn_set,40010/11,TODO 弹层)+ 查看申请(_btn_apply,40008,含5秒去抖+自动开申请弹层)。
    /// </summary>
    public sealed class GuildMemberView : GuildMemberViewBind
    {
        private readonly List<GuildMemberItem> _rows = new List<GuildMemberItem>();
        /// <summary>对标老端 View 实例字段 apply_click_time(去抖计时,非 Model 状态)。</summary>
        private long _lastApplyClickSec;

        protected override void OnInit()
        {
            if (_tpl_GuildMemberItem != null) _tpl_GuildMemberItem.SetActive(false);
            if (red != null) red.gameObject.SetActive(false);
            BindClick(_btn_set, OnClickSet);
            BindClick(_btn_apply, OnClickApply);
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_GUILD_MEMBER_UPDATE, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_GUILD_APPLY_UPDATE, RefreshRed);
            EventDispatcher.On(GlobalEvent.EVT_GUILD_APPLY_AUTO_OPEN, OnAutoOpenApply);
            GuildController.Instance.RequestMembers(); // 对标老端 GuildMemberView.FireEvent:每次加载发 40006
            Refresh();
            RefreshRed();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GUILD_MEMBER_UPDATE, Refresh);
            EventDispatcher.Off(GlobalEvent.EVT_GUILD_APPLY_UPDATE, RefreshRed);
            EventDispatcher.Off(GlobalEvent.EVT_GUILD_APPLY_AUTO_OPEN, OnAutoOpenApply);
        }

        private void Refresh()
        {
            if (_tpl_GuildMemberItem == null || _list_menber == null || _list_menber.content == null) return;
            IReadOnlyList<GuildModel.MemberEntry> list = GuildModel.Instance.Members;
            EnsureRows(list.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i < list.Count;
                _rows[i].gameObject.SetActive(active);
                if (active) _rows[i].SetData(list[i]);
            }
        }

        private void EnsureRows(int count)
        {
            while (_rows.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_GuildMemberItem, _list_menber.content);
                go.SetActive(true);
                GuildMemberItem item = go.GetComponent<GuildMemberItem>();
                if (item != null) _rows.Add(item);
                else break; // 理论不应发生(GuildModule.prefab 回填已挂本类),避免死循环
            }
        }

        /// <summary>近似红点:申请列表非空即显(对标老端 RedKey.GUILD_APPLY,本轮无统一红点系统,直接读数据)。</summary>
        private void RefreshRed()
        {
            if (red != null) red.gameObject.SetActive(GuildModel.Instance.Applies.Count > 0);
        }

        private void OnClickSet()
        {
            if (!GuildModel.Instance.HasPermission(GuildModel.Permission.APPROVE_SETTING))
            {
                TipsManager.Toast("您没有操作权限");
                return;
            }
            GameLog.Info("Guild", "点击审批设置 → GuildApplySetView 未移植(40010/11 数据链已通),TODO");
        }

        /// <summary>对标老端 InitEvent _btn_apply:无权限→toast;5秒内重复点直接用缓存数据开层,
        /// 否则发 40008 且置 apply_request_mark,回包到达由 EVT_GUILD_APPLY_AUTO_OPEN 驱动开层。</summary>
        private void OnClickApply()
        {
            if (!GuildModel.Instance.HasPermission(GuildModel.Permission.APPROVE_APPLY))
            {
                TipsManager.Toast("您没有操作权限");
                return;
            }
            if (GuildModel.Instance.ApplyRequestMark) return;

            long now = TimeUtil.NowSec();
            if (now - _lastApplyClickSec > 5)
            {
                _lastApplyClickSec = now;
                GuildModel.Instance.ApplyRequestMark = true;
                GuildController.Instance.RequestApplyList();
            }
            else if (GuildModel.Instance.Applies.Count > 0)
            {
                GuildMainFlow.OpenApplyLook();
            }
        }

        private void OnAutoOpenApply() => GuildMainFlow.OpenApplyLook();

        private static void BindClick(UnityEngine.Component target, System.Action onClick)
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
