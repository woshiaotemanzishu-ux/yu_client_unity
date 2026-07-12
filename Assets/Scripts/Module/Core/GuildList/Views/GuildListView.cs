using System;
using System.Collections.Generic;
using System.Linq;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.GuildList;
using Shenxiao.Module.Core.Guild;
using Shenxiao.Module.Core.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GuildList
{
    /// <summary>
    /// 公会列表界面(对标老客户端 guildList/GuildListView.ts):置顶前3名(GuildListTopItem)+ 全量列表
    /// (GuildListItem,老端本地按战力降序重排,不是服务端排序口径)+ 建会(_btn_build,复用 GuildJoinController.Create
    /// 默认名,同 GuildJoinShellView 既有占位约定,真实输入框 GuildBuildView 未接线)/一键批量申请(_btn_apply,40003)。
    ///
    /// 缝合点(轮13a 主线任务之一):接 GuildJoinController/Model 40001/40003/40004 数据链,单行申请走新增
    /// 40002(GuildListItem/TopItem 已各自持 guild_id)。
    /// </summary>
    public sealed class GuildListView : GuildListViewBind
    {
        private readonly List<GuildListTopItem> _topRows = new List<GuildListTopItem>();
        private readonly List<GuildListItem> _rows = new List<GuildListItem>();

        protected override void OnInit()
        {
            if (_tpl_GuildListItem != null) _tpl_GuildListItem.SetActive(false);
            if (_tpl_GuildListTopItem != null) _tpl_GuildListTopItem.SetActive(false);
            BindBtn(_btn_close, () => Hide());
            BindBtn(_btn_build, OnClickBuild);
            BindBtn(_btn_apply, OnClickApplyAll);
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_GUILD_UPDATE, Rebuild);
            GuildJoinController.Instance.RequestList(); // 对标老端 GuildListView.LoadSuccess:打开发 40001(999,1)
            // 对标老端同一处 30008 补触发:仅当主线任务为"加入结社"型(101080)且未完成才发,非无条件
            // (老端 MainTaskIsJoinGuildTaskType() 判定;101080 是当前 MainLineTaskVo 即代表尚未推进过去)。
            TaskVo joinTask = TaskModel.Instance.MainLineTaskVo;
            if (joinTask != null && joinTask.TaskId == 101080) GuildJoinController.Instance.NotifyTaskCheck();
            Rebuild();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GUILD_UPDATE, Rebuild);
        }

        private void OnClickBuild()
        {
            if (GuildModel.IsHasGuild()) { TipsManager.Toast("您当前已有所属结社"); return; }
            // GuildBuildView(输入结社名弹窗)未接线,复用 GuildJoinController 既有占位默认名(同 GuildJoinShellView 约定)。
            GuildJoinController.Instance.Create("神霄阁");
        }

        private void OnClickApplyAll()
        {
            if (GuildModel.IsHasGuild()) { TipsManager.Toast("您当前已有所属结社"); return; }
            GuildJoinController.Instance.ApplyAll();
            // 对标老端:点击后立即检查主线任务(不等回包),101080(加入结社)命中即关面板。
            TaskVo task = TaskModel.Instance.MainLineTaskVo;
            if (task != null && task.TaskId == 101080) Hide();
        }

        /// <summary>对标老端 UpdateView:列表按 combat_power 降序(服务端排序口径不同,老端本地重排一次,
        /// r13_server_pt400 已确认此非移植引入的差异)。置顶3位取排序后前3(允许 null 占位,老端 top_list
        /// 固定3格,不足补空)。</summary>
        private void Rebuild()
        {
            List<GuildJoinModel.GuildBrief> sorted = GuildJoinModel.Instance.List
                .OrderByDescending(g => g.CombatPower)
                .ToList();

            EnsureTopRows();
            for (int i = 0; i < _topRows.Count; i++)
            {
                _topRows[i].SetData(i < sorted.Count ? sorted[i] : null);
            }

            EnsureRows(sorted.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i < sorted.Count;
                _rows[i].gameObject.SetActive(active);
                if (active) _rows[i].SetData(sorted[i]);
            }
        }

        private void EnsureTopRows()
        {
            if (_dg_top == null || _tpl_GuildListTopItem == null) return;
            while (_topRows.Count < 3)
            {
                GameObject go = UnityEngine.Object.Instantiate(_tpl_GuildListTopItem, _dg_top);
                go.SetActive(true);
                GuildListTopItem item = go.GetComponent<GuildListTopItem>();
                if (item != null) _topRows.Add(item);
                else break;
            }
        }

        private void EnsureRows(int count)
        {
            if (Content == null || Content.content == null || _tpl_GuildListItem == null) return;
            while (_rows.Count < count)
            {
                GameObject go = UnityEngine.Object.Instantiate(_tpl_GuildListItem, Content.content);
                go.SetActive(true);
                GuildListItem item = go.GetComponent<GuildListItem>();
                if (item != null) _rows.Add(item);
                else break;
            }
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
