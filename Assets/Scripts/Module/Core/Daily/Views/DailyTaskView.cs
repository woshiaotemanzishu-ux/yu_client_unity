using System.Collections.Generic;
using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 每日任务页(对标老端 daily/DailyTaskView.ts,DailyView 标签内容):列表(_list_item_con/Content 克隆
    /// DailyTaskItem)由 15701(act_type=1)驱动,开页重拉一次 + 数据变动即重刷(EVT_DAILY_TASK_UPDATE)。
    /// 底栏(_box_bottom/_tpl_DailyBottomView)仍隐藏——DailyBottomView.prefab 全仓零引用孤儿资产
    /// (r10_unity §5 结论),本轮不接(TODO,归后续活跃度形象/离线挂机/日历专项)。
    /// </summary>
    public sealed class DailyTaskView : DailyTaskViewBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_DailyTaskItem != null) _tpl_DailyTaskItem.SetActive(false);
            if (_tpl_DailyBottomView != null) _tpl_DailyBottomView.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            DailyController.Instance.RequestActivityList(DailyModel.ACT_UNLIMIT); // 对标老端开页再拉一次
            Refresh();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_DAILY_TASK_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_DAILY_TASK_UPDATE, Refresh);
            _subscribed = false;
        }

        private void Refresh()
        {
            foreach (GameObject go in _cells) if (go != null) Object.Destroy(go);
            _cells.Clear();
            if (_tpl_DailyTaskItem == null || _list_item_con == null || _list_item_con.content == null) return;
            DailyModel.DailyDataVo data = DailyModel.Instance.GetDailyData(DailyModel.ACT_UNLIMIT);
            if (data == null) return;
            foreach (DailyModel.ActivityVo vo in data.AcList)
            {
                GameObject cellGo = Object.Instantiate(_tpl_DailyTaskItem, _list_item_con.content);
                cellGo.SetActive(true);
                DailyTaskItem item = cellGo.GetComponent<DailyTaskItem>();
                if (item != null) item.SetData(vo);
                _cells.Add(cellGo);
            }
            GameLog.Info("Daily", "每日任务列表刷新 count={0}", data.AcList.Count);
        }
    }
}
