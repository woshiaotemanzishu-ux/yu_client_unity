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
    /// 结社仓库主体(对标老客户端 guild/GuildDepotView.ts;40101 界面+40102/103/104/105/106/107/108 增量):
    /// `_list_item`→<see cref="GuildDepotItem"/>(depot_goods,点击直接兑换积分)、`_list_msg`→
    /// <see cref="GuildDepotRecordItem"/>(exchange_records 日志流)。
    /// **降级(本轮范围收敛,见工单裁决)**:老端"静态兑换目录"(ConfigGuild.json.depot_goods 固定条目混入列表)/
    /// 品阶品质筛选下拉(`_dd_stage`/`_dd_color`,DownDropBtn 组件 Unity 未移植)/`_btn_destroy`→GuildCleanView
    /// (按条件批量销毁弹层,40109/110)均未接线,仅保留"真实仓库物品列表 + 兑换记录 + 积分显示 + 捐献入口"这条
    /// 核心链路(data-only 接真)。</summary>
    public sealed class GuildDepotView : GuildDepotViewBind
    {
        private readonly List<GuildDepotItem> _goodsRows = new List<GuildDepotItem>();
        private readonly List<GuildDepotRecordItem> _recordRows = new List<GuildDepotRecordItem>();

        protected override void OnInit()
        {
            if (_tpl_GuildDepotItem != null) _tpl_GuildDepotItem.SetActive(false);
            if (_tpl_GuildDepotRecordItem != null) _tpl_GuildDepotRecordItem.SetActive(false);
            if (_tpl_DownDropBtn != null) _tpl_DownDropBtn.SetActive(false);
            BindClick(_btn_donate, OnClickDonate);
            BindClick(_btn_destroy, OnClickDestroy);
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_GUILD_DEPOT_UPDATE, Refresh);
            GuildController.Instance.RequestDepotInfo(); // 对标老端 FireEvent:每次打开发 40101
            if (_btn_destroy != null) _btn_destroy.gameObject.SetActive(GuildModel.Instance.HasPermission(GuildModel.Permission.WAREHOUSE_MANAGER));
            Refresh();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GUILD_DEPOT_UPDATE, Refresh);
        }

        private void Refresh()
        {
            if (_lb_point != null) _lb_point.text = GuildModel.Instance.DepotScore.ToString();
            RefreshGoods();
            RefreshRecords();
        }

        private void RefreshGoods()
        {
            if (_tpl_GuildDepotItem == null || _list_item == null || _list_item.content == null) return;
            IReadOnlyList<GuildModel.DepotGoodsEntry> list = GuildModel.Instance.DepotGoods;
            EnsureGoodsRows(list.Count);
            for (int i = 0; i < _goodsRows.Count; i++)
            {
                bool active = i < list.Count;
                _goodsRows[i].gameObject.SetActive(active);
                if (!active) continue;
                GuildModel.DepotGoodsEntry entry = list[i];
                _goodsRows[i].SetData(entry.TypeId, entry.Num, false, () => OnClickGoods(entry));
            }
        }

        private void RefreshRecords()
        {
            if (_tpl_GuildDepotRecordItem == null || _list_msg == null || _list_msg.content == null) return;
            IReadOnlyList<GuildModel.DepotRecordEntry> list = GuildModel.Instance.DepotRecords;
            EnsureRecordRows(list.Count);
            for (int i = 0; i < _recordRows.Count; i++)
            {
                bool active = i < list.Count;
                _recordRows[i].gameObject.SetActive(active);
                if (active) _recordRows[i].SetData(list[i]);
            }
        }

        /// <summary>点击仓库物品=尝试兑换(对标老端 type==10 分支,本轮跳过"是否够积分"提示 tooltip,直接发送,
        /// 服务端积分不足会显码回错)。</summary>
        private void OnClickGoods(GuildModel.DepotGoodsEntry entry)
        {
            GuildController.Instance.ExchangeDepot(entry.GoodsId, entry.TypeId, 1);
        }

        private void OnClickDonate() => GuildMainFlow.OpenDepotSelect();

        private void OnClickDestroy()
        {
            GameLog.Info("Guild", "点击销毁 → GuildCleanView(按条件批量销毁,40109/110)未移植,TODO");
        }

        private void EnsureGoodsRows(int count)
        {
            while (_goodsRows.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_GuildDepotItem, _list_item.content);
                go.SetActive(true);
                GuildDepotItem item = go.GetComponent<GuildDepotItem>();
                if (item != null) _goodsRows.Add(item);
                else break;
            }
        }

        private void EnsureRecordRows(int count)
        {
            while (_recordRows.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_GuildDepotRecordItem, _list_msg.content);
                go.SetActive(true);
                GuildDepotRecordItem item = go.GetComponent<GuildDepotRecordItem>();
                if (item != null) _recordRows.Add(item);
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
