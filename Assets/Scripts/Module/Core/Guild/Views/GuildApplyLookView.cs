using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 申请列表弹层(对标老客户端 guild/GuildApplyLookView.ts):40008 全量列表 + 全部同意(_btn_pass→40016
    /// type=1)/全部拒绝(_btn_refuse→40016 type=2);单条审批在 <see cref="GuildApplyLookItem"/>(40009)。
    /// 由 GuildMemberView 的"查看申请"按钮驱动打开(<see cref="GuildMainFlow.OpenApplyLook"/>),非 4 页签之一
    /// (老端 GuildMainBaseView.tabStrList 本身也没有独立"申请"页签,是从成员页触发的弹层——与规格草案
    /// 描述的"申请页"字面不同,以老端真实结构为准,偏差见工单 summary)。
    /// </summary>
    public sealed class GuildApplyLookView : GuildApplyLookViewBind
    {
        private readonly List<GuildApplyLookItem> _rows = new List<GuildApplyLookItem>();

        protected override void OnInit()
        {
            if (_tpl_GuildApplyLookItem != null) _tpl_GuildApplyLookItem.SetActive(false);
            BindClick(_btn_pass, () => GuildController.Instance.BulkHandleApply(1));
            BindClick(_btn_refuse, () => GuildController.Instance.BulkHandleApply(2));
            BindClick(_Image1, Hide);
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_GUILD_APPLY_UPDATE, Refresh);
            Refresh();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GUILD_APPLY_UPDATE, Refresh);
        }

        private void Refresh()
        {
            if (_tpl_GuildApplyLookItem == null || _list_items == null || _list_items.content == null) return;
            IReadOnlyList<GuildModel.ApplyEntry> list = GuildModel.Instance.Applies;
            EnsureRows(list.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i < list.Count;
                _rows[i].gameObject.SetActive(active);
                if (active) _rows[i].SetData(list[i]);
            }
            if (_group_empty != null) _group_empty.gameObject.SetActive(list.Count <= 0);
        }

        private void EnsureRows(int count)
        {
            while (_rows.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_GuildApplyLookItem, _list_items.content);
                go.SetActive(true);
                GuildApplyLookItem item = go.GetComponent<GuildApplyLookItem>();
                if (item != null) _rows.Add(item);
                else break;
            }
        }

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
