using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.ListDuobao;
using Shenxiao.Module.Core.CustomActivity;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.ListDuobao
{
    public sealed class ListRewardView : ListRewardViewBind
    {
        private readonly List<GameObject> _rows = new List<GameObject>();
        private bool _eventsBound;

        protected override void OnInit()
        {
            if (_tpl_ListRewardItem != null) _tpl_ListRewardItem.SetActive(false);
            BindClick(_btn_close, ListDuobaoFlow.ClosePopup);
        }

        protected override void OnShow(object args) { BindEvents(); RefreshData(); }
        protected override void OnHide() { UnbindEvents(); ClearRows(); }
        public void RefreshData() => _ = RefreshDataAsync();

        private async Task RefreshDataAsync()
        {
            await ListDuobaoConfigs.EnsureLoaded();
            if (!IsShown) return;
            CustomActivityModel.ListDuobaoStageInfo info = CustomActivityModel.Instance.ListDuobaoStage;
            if (info == null || _gp_reward == null || _tpl_ListRewardItem == null) return;
            ClearRows();
            List<ListDuobaoConfigs.StageRow> rows = ListDuobaoConfigs.GetStages(info.Type, info.SubType);
            // 老端 ListRewardView.ts:76-80：已领取项后置，其余保持阶段顺序。
            rows.Sort((a, b) => IsClaimed(info, a.RewardId).CompareTo(IsClaimed(info, b.RewardId)));
            Transform parent = _gp_reward.content != null ? _gp_reward.content : _gp_reward.transform;
            for (int i = 0; i < rows.Count; i++)
            {
                ListDuobaoConfigs.StageRow row = rows[i];
                CustomActivityModel.ListDuobaoStageState state = info.StageList.Find(v => v.Id == row.RewardId);
                GameObject go = Instantiate(_tpl_ListRewardItem, parent);
                go.SetActive(true);
                ListRewardItem item = go.GetComponent<ListRewardItem>();
                item?.SetData(row, state.GotType, info.Score, ListDuobaoFlow.GoodsItemTemplate);
                _rows.Add(go);
            }
        }

        private static bool IsClaimed(CustomActivityModel.ListDuobaoStageInfo info, int rewardId) =>
            info.StageList.Find(v => v.Id == rewardId).GotType == 2;

        private void OnDetail(int type, int subType) { if (type == ListDuobaoFlow.BaseType && subType == CustomActivityModel.Instance.ListDuobaoSubType) RefreshData(); }
        private void BindEvents() { if (_eventsBound) return; EventDispatcher.On<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail); _eventsBound = true; }
        private void UnbindEvents() { if (!_eventsBound) return; EventDispatcher.Off<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail); _eventsBound = false; }
        private void ClearRows() { for (int i = 0; i < _rows.Count; i++) if (_rows[i] != null) Destroy(_rows[i]); _rows.Clear(); }
        private static void BindClick(Component target, System.Action action) { if (target == null) return; Graphic g = target as Graphic ?? target.GetComponent<Graphic>(); if (g != null) UIUtil.ClearClicks(g); UIUtil.AddClick(target, action); }
    }
}
