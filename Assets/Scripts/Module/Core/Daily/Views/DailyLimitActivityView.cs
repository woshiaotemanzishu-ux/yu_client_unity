using System.Collections.Generic;
using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 限时活动页(对标老端 daily/DailyLimitActivityView.ts,DailyView 标签内容):列表(_list_item_con/Content
    /// 克隆 DailyLimitActivityItem)由 15701(act_type=2)驱动,预约/领奖态叠加 15718/15719/15720 状态表。
    /// 底栏同 DailyTaskView 仍隐藏(TODO)。
    /// </summary>
    public sealed class DailyLimitActivityView : DailyLimitActivityViewBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_DailyLimitActivityItem != null) _tpl_DailyLimitActivityItem.SetActive(false);
            if (_tpl_DailyBottomView != null) _tpl_DailyBottomView.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            DailyController.Instance.RequestActivityList(DailyModel.ACT_LIMIT); // 对标老端开页再拉一次
            DailyController.Instance.RequestSignUpList();
            Refresh();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_DAILY_LIMIT_UPDATE, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_DAILY_SIGNUP_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_DAILY_LIMIT_UPDATE, Refresh);
            EventDispatcher.Off(GlobalEvent.EVT_DAILY_SIGNUP_UPDATE, Refresh);
            _subscribed = false;
        }

        private void Refresh()
        {
            foreach (GameObject go in _cells) if (go != null) Object.Destroy(go);
            _cells.Clear();
            if (_tpl_DailyLimitActivityItem == null || _list_item_con == null || _list_item_con.content == null) return;
            DailyModel.DailyDataVo data = DailyModel.Instance.GetDailyData(DailyModel.ACT_LIMIT);
            if (data == null) return;
            foreach (DailyModel.ActivityVo vo in data.AcList)
            {
                GameObject cellGo = Object.Instantiate(_tpl_DailyLimitActivityItem, _list_item_con.content);
                cellGo.SetActive(true);
                DailyLimitActivityItem item = cellGo.GetComponent<DailyLimitActivityItem>();
                int? res = DailyModel.Instance.TryGetReservation(vo.Module, vo.ModuleSub, vo.AcSub, out int status) ? status : (int?)null;
                if (item != null)
                {
                    item.Show();
                    item.SetData(vo, res);
                }
                _cells.Add(cellGo);
            }
            GameLog.Info("Daily", "限时活动列表刷新 count={0}", data.AcList.Count);
        }
    }
}
