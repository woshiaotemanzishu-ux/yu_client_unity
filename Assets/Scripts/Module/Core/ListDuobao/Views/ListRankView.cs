using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.ListDuobao;
using Shenxiao.Module.Core.CustomActivity;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.ListDuobao
{
    public sealed class ListRankView : ListRankViewBind
    {
        private readonly List<GameObject> _rows = new List<GameObject>();
        private bool _serverTab;
        private bool _eventsBound;

        protected override void OnInit()
        {
            if (_tpl_ListRankItem != null) _tpl_ListRankItem.SetActive(false);
            if (_tpl_ListGoodsItem != null) _tpl_ListGoodsItem.SetActive(false);
            BindClick(_btn_close, ListDuobaoFlow.ClosePopup);
            BindClick(_btn_single, ShowPersonal);
            BindClick(_btn_all_, ShowServer);
        }

        protected override void OnShow(object args)
        {
            BindEvents();
            _serverTab = false;
            int sub = CustomActivityModel.Instance.ListDuobaoSubType;
            CustomActivityController.Instance.RequestListDuobaoRank(ListDuobaoFlow.BaseType, sub);
            RefreshData();
        }

        protected override void OnHide() { UnbindEvents(); ClearRows(); }

        public void RefreshData() => _ = RefreshDataAsync();

        private async Task RefreshDataAsync()
        {
            await ListDuobaoConfigs.EnsureLoaded();
            if (!IsShown) return;
            CustomActivityModel.ListDuobaoRankInfo info = CustomActivityModel.Instance.ListDuobaoRank;
            if (info == null || info.Type != ListDuobaoFlow.BaseType || info.SubType != CustomActivityModel.Instance.ListDuobaoSubType) return;
            ClearRows();
            if (_player_rank != null) _player_rank.gameObject.SetActive(!_serverTab);
            if (_server_rank != null) _server_rank.gameObject.SetActive(_serverTab);
            if (_lb_single != null) _lb_single.text = "个人排行";
            if (_lb_all != null) _lb_all.text = "全服排行";
            if (_lb_single != null) _lb_single.color = _serverTab ? new Color32(64, 52, 66, 255) : new Color32(255, 255, 255, 255);
            if (_lb_all != null) _lb_all.color = _serverTab ? new Color32(255, 255, 255, 255) : new Color32(64, 52, 66, 255);
            if (_btn_single != null) _ = ResManager.SetImageAsync(_btn_single, GameResPath.GetIcon("listDuobao", _serverTab ? "ui_cbdb_04" : "ui_cbdb_03"), false, false);
            if (_btn_all_ != null) _ = ResManager.SetImageAsync(_btn_all_, GameResPath.GetIcon("listDuobao", _serverTab ? "ui_cbdb_03" : "ui_cbdb_04"), false, false);
            int myRank = _serverTab ? info.ServerRank : info.Rank;
            long myScore = _serverTab ? info.ServerScore : info.Score;
            string owner = _serverTab ? "本区" : "我的";
            if (_lb_myrank != null) _lb_myrank.text = myRank == 0
                ? owner + "排名：<color=#d15e00>未上榜</color>"
                : owner + "排名：<color=#d15e00>第" + myRank + "名</color>";
            if (_lb_myscore != null) _lb_myscore.text = owner + "积分：<color=#0a953e>" + myScore + "</color>积分";
            if (_img_rank != null)
                _ = ResManager.SetImageAsync(_img_rank, GameResPath.GetIcon("listDuobao",
                    myRank > 0 && myRank < 4 ? "ui_rank" + myRank : "ui_rank4"), false, false);
            if (_lb_rank != null)
            {
                _lb_rank.gameObject.SetActive(myRank <= 0 || myRank >= 4);
                _lb_rank.fontSize = myRank == 0 ? 18 : 36;
                _lb_rank.text = myRank == 0 ? "未上榜" : myRank.ToString();
            }
            int target = 0;
            ListDuobaoConfigs.TryReadCondition(CustomActivityModel.Instance.GetActiveListDuobaoAct()?.Condition, "target", out target);
            if (_lb_tips != null)
            {
                _lb_tips.text = "个人积分达到" + target + "可获得区服排行奖励";
                _lb_tips.gameObject.SetActive(_serverTab);
            }

            if (_serverTab) RenderServerRanks(info);
            else RenderPersonalRanks(info);
            RefreshMyReward(info);
        }

        private void RenderPersonalRanks(CustomActivityModel.ListDuobaoRankInfo info)
        {
            var live = new Dictionary<int, CustomActivityModel.ListDuobaoRankEntry>();
            for (int i = 0; i < info.RankList.Count; i++) live[info.RankList[i].Rank] = info.RankList[i];
            RenderConfiguredRanks(1, _player_rank, (rank, row) =>
            {
                if (live.TryGetValue(rank, out CustomActivityModel.ListDuobaoRankEntry item))
                    AddRow(_player_rank, rank, "s" + item.ServerId + "." + item.RoleName, item.RoleScore, row);
                else AddRow(_player_rank, rank, "", 0, row);
            });
        }

        private void RenderServerRanks(CustomActivityModel.ListDuobaoRankInfo info)
        {
            var live = new Dictionary<int, CustomActivityModel.ListDuobaoServerRankEntry>();
            for (int i = 0; i < info.ServerRankList.Count; i++) live[info.ServerRankList[i].Rank] = info.ServerRankList[i];
            RenderConfiguredRanks(2, _server_rank, (rank, row) =>
            {
                if (live.TryGetValue(rank, out CustomActivityModel.ListDuobaoServerRankEntry item))
                    AddRow(_server_rank, rank, "s" + item.ServerId + "." + item.ServerName, item.ServerScore, row);
                else AddRow(_server_rank, rank, "", 0, row);
            });
        }

        private void RenderConfiguredRanks(int rankType, ScrollRect target, System.Action<int, ListDuobaoConfigs.RankRow> add)
        {
            List<ListDuobaoConfigs.RankRow> configs = ListDuobaoConfigs.GetRanks(
                ListDuobaoFlow.BaseType, CustomActivityModel.Instance.ListDuobaoSubType, rankType);
            for (int i = 0; i < configs.Count; i++)
                for (int rank = configs[i].RankMin; rank <= configs[i].RankMax; rank++) add(rank, configs[i]);
        }

        private void RefreshMyReward(CustomActivityModel.ListDuobaoRankInfo info)
        {
            if (_gp_reward == null || _tpl_ListGoodsItem == null) return;
            bool show = !_serverTab && info.Rank > 0;
            _gp_reward.gameObject.SetActive(show);
            if (!show) return;
            ListDuobaoConfigs.RankRow row = ListDuobaoConfigs.GetRanks(info.Type, info.SubType, 1)
                .Find(v => info.Rank >= v.RankMin && info.Rank <= v.RankMax);
            if (row == null) return;
            Transform parent = _gp_reward.content != null ? _gp_reward.content : _gp_reward.transform;
            GameObject go = Instantiate(_tpl_ListGoodsItem, parent);
            ListGoodsItem item = go.GetComponent<ListGoodsItem>();
            if (item != null) item.SetData(row.Reward);
            else go.SetActive(true);
            _rows.Add(go);
        }

        private void AddRow(ScrollRect list, int rank, string name, long score, ListDuobaoConfigs.RankRow row)
        {
            if (list == null || _tpl_ListRankItem == null) return;
            Transform parent = list.content != null ? list.content : list.transform;
            GameObject go = Instantiate(_tpl_ListRankItem, parent);
            ListRankItem item = go.GetComponent<ListRankItem>();
            if (item != null) item.SetData(rank, name, score, row.LimitValue, row.Reward, _tpl_ListGoodsItem);
            else go.SetActive(true);
            _rows.Add(go);
        }

        internal GameObject GoodsItemTemplate => _tpl_ListGoodsItem;

        private void ShowPersonal() { _serverTab = false; RefreshData(); }
        private void ShowServer() { _serverTab = true; RefreshData(); }
        private void OnDetail(int type, int subType) { if (type == ListDuobaoFlow.BaseType && subType == CustomActivityModel.Instance.ListDuobaoSubType) RefreshData(); }
        private void BindEvents() { if (_eventsBound) return; EventDispatcher.On<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail); _eventsBound = true; }
        private void UnbindEvents() { if (!_eventsBound) return; EventDispatcher.Off<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail); _eventsBound = false; }
        private void ClearRows() { for (int i = 0; i < _rows.Count; i++) if (_rows[i] != null) Destroy(_rows[i]); _rows.Clear(); }
        private static void BindClick(Component target, System.Action action) { if (target == null) return; Graphic g = target as Graphic ?? target.GetComponent<Graphic>(); if (g != null) UIUtil.ClearClicks(g); UIUtil.AddClick(target, action); }
    }
}
