using System.Collections.Generic;
using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 我要变强页(对标老端 daily/DailyStrongerView.ts,DailyView 标签内容,100级开):列表
    /// (_item_scroller/Content 克隆 DailyStrongerItem)由 config_to_be_strong 全量条目 + 61801 状态表驱动。
    /// 三个筛选子 Tab(_Scroller1/Content1 克隆 DailyStrongerTab)按 config_to_be_strong.type 分类；列表按
    /// 老端 100+star、等级未达扣 lv、今日完成扣 20、同分 id 降序的规则排序，切页停止惯性并回顶。
    /// </summary>
    public sealed class DailyStrongerView : DailyStrongerViewBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();
        private readonly List<GameObject> _tabs = new List<GameObject>();
        private bool _subscribed;
        private int _selectedType = 1;

        private static readonly string[] TabLabels = { "我要经验", "我要绑玉", "我要装备" };

        protected override void OnInit()
        {
            if (_tpl_DailyStrongerItem != null) _tpl_DailyStrongerItem.SetActive(false);
            if (_tpl_DailyStrongerTab != null) _tpl_DailyStrongerTab.SetActive(false);
            BuildTabs();
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
            ids.RemoveAll(id => DailyConfigs.GetStrongType(id) != _selectedType);
            ids.Sort(CompareStrongIds);
            foreach (int id in ids)
            {
                GameObject cellGo = Object.Instantiate(_tpl_DailyStrongerItem, _item_scroller.content);
                cellGo.SetActive(true);
                DailyStrongerItem item = cellGo.GetComponent<DailyStrongerItem>();
                DailyModel.StrongStateVo state = DailyModel.Instance.GetStrongerById(id);
                bool finishedToday = state != null && state.State == 1 && DailyConfigs.GetStrongDayLimit(id) == 1;
                if (item != null)
                {
                    item.Show();
                    item.SetData(id, finishedToday);
                }
                _cells.Add(cellGo);
            }
            if (_item_scroller != null) _item_scroller.StopMovement();
            if (_item_scroller != null && _item_scroller.content != null)
                _item_scroller.content.anchoredPosition = Vector2.zero;
            GameLog.Info("Daily", "我要变强列表刷新 type={0} count={1}", _selectedType, ids.Count);
        }

        private void BuildTabs()
        {
            if (_tabs.Count > 0 || _tpl_DailyStrongerTab == null || Content1 == null) return;
            for (int i = 0; i < TabLabels.Length; i++)
            {
                int type = i + 1;
                GameObject tabGo = Object.Instantiate(_tpl_DailyStrongerTab, Content1);
                tabGo.SetActive(true);
                DailyStrongerTabBind tab = tabGo.GetComponent<DailyStrongerTabBind>();
                if (tab != null)
                {
                    tab.Show();
                    if (tab._lb != null) tab._lb.text = TabLabels[i];
                    if (tab._Image1 != null)
                    {
                        tab._Image1.raycastTarget = true;
                        UIUtil.AddClick(tab._Image1, () => SelectType(type));
                    }
                }
                _tabs.Add(tabGo);
            }
            UpdateTabStates();
        }

        private void SelectType(int type)
        {
            if (_selectedType == type) return;
            _selectedType = type;
            UpdateTabStates();
            Refresh();
        }

        private void UpdateTabStates()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                DailyStrongerTabBind tab = _tabs[i] != null ? _tabs[i].GetComponent<DailyStrongerTabBind>() : null;
                bool selected = i + 1 == _selectedType;
                if (tab != null && tab._lb != null)
                    tab._lb.color = selected ? new Color32(155, 87, 47, 255) : Color.white;
            }
        }

        private static int CompareStrongIds(int a, int b)
        {
            int aScore = StrongScore(a);
            int bScore = StrongScore(b);
            int scoreCompare = bScore.CompareTo(aScore);
            return scoreCompare != 0 ? scoreCompare : b.CompareTo(a);
        }

        private static int StrongScore(int id)
        {
            int score = 100 + DailyConfigs.GetStrongStar(id);
            int level = DailyConfigs.GetStrongLv(id);
            if (RoleModel.Instance.Level < level) score -= level;
            DailyModel.StrongStateVo state = DailyModel.Instance.GetStrongerById(id);
            if (state != null && state.State == 1) score -= 20;
            return score;
        }
    }
}
