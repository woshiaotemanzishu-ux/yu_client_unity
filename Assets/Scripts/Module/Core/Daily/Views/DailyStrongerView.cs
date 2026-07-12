using System.Collections.Generic;
using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 我要变强页(对标老端 daily/DailyStrongerView.ts,DailyView 标签内容,100级开):列表
    /// (_item_scroller/Content 克隆 DailyStrongerItem)由 config_to_be_strong 全量条目 + 61801 状态表驱动。
    /// 降级:3 个筛选子 Tab(_Scroller1/Content1 克隆 DailyStrongerTab,老端"我要经验/我要绑玉/我要装备"
    /// 分类+排序权重)未接线,先展示全量单一列表(TODO,子 Tab 模板先隐藏)。
    /// </summary>
    public sealed class DailyStrongerView : DailyStrongerViewBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_DailyStrongerItem != null) _tpl_DailyStrongerItem.SetActive(false);
            if (_tpl_DailyStrongerTab != null) _tpl_DailyStrongerTab.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            _ = EnsureConfigThenRefresh();
            DailyController.Instance.RequestStrongerList(); // 对标老端 DailyStrongerView 加载即 Fire(REQUEST_STRONGER_DATA)
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_DAILY_STRONGER_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_DAILY_STRONGER_UPDATE, Refresh);
            _subscribed = false;
        }

        private async System.Threading.Tasks.Task EnsureConfigThenRefresh()
        {
            await DailyConfigs.EnsureLoaded();
            Refresh();
        }

        private void Refresh()
        {
            foreach (GameObject go in _cells) if (go != null) Object.Destroy(go);
            _cells.Clear();
            if (_tpl_DailyStrongerItem == null || _item_scroller == null || _item_scroller.content == null) return;
            if (!DailyConfigs.IsLoaded) return;
            List<int> ids = DailyConfigs.AllToBeStrongIds();
            foreach (int id in ids)
            {
                GameObject cellGo = Object.Instantiate(_tpl_DailyStrongerItem, _item_scroller.content);
                cellGo.SetActive(true);
                DailyStrongerItem item = cellGo.GetComponent<DailyStrongerItem>();
                DailyModel.StrongStateVo state = DailyModel.Instance.GetStrongerById(id);
                bool finishedToday = state != null && state.State == 1 && DailyConfigs.GetStrongDayLimit(id) == 1;
                if (item != null) item.SetData(id, finishedToday);
                _cells.Add(cellGo);
            }
            GameLog.Info("Daily", "我要变强列表刷新 count={0}", ids.Count);
        }
    }
}
