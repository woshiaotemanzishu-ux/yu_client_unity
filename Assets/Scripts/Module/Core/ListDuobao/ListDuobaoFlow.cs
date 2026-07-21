using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.CustomActivity;
using UnityEngine;

namespace Shenxiao.Module.Core.ListDuobao
{
    public static class ListDuobaoFlow
    {
        public const int BaseType = 116;
        private const string Module = "ListDuobao";
        private const string Prefab = "ListDuobaoModule";

        private static GameObject _root;
        private static ListDuobaoView _main;
        private static ListRankView _rank;
        private static ListRewardView _reward;
        private static ListDuobaoRecordView _record;
        private static Task<bool> _loadTask;
        private static int _generation;

        public static void Toggle()
        {
            if (_main != null && _main.IsShown) Close();
            else Open();
        }

        public static void Open()
        {
            CustomActivityModel.ActEntry act = ResolveActiveAct();
            if (act == null)
            {
                TipsManager.Toast("连服夺宝活动暂未开启");
                return;
            }
            CustomActivityController.Instance.RequestListDuobaoStage(BaseType, act.SubType);
            CustomActivityController.Instance.RequestListDuobaoRank(BaseType, act.SubType);
            _ = OpenAsync();
        }

        public static void OpenRank() => _ = OpenPopupAsync(Popup.Rank);
        public static void OpenReward() => _ = OpenPopupAsync(Popup.Reward);
        public static void OpenRecord() => _ = OpenPopupAsync(Popup.Record);

        public static void ClosePopup()
        {
            _rank?.Hide();
            _reward?.Hide();
            _record?.Hide();
        }

        public static void Close()
        {
            ClosePopup();
            _main?.Hide();
        }

        internal static GameObject GoodsItemTemplate => _rank != null ? _rank.GoodsItemTemplate : null;

        private static async Task OpenAsync()
        {
            if (!await EnsureLoaded()) return;
            ClosePopup();
            _main.Show();
        }

        private static async Task OpenPopupAsync(Popup popup)
        {
            if (ResolveActiveAct() == null) return;
            if (!await EnsureLoaded()) return;
            ClosePopup();
            if (!_main.IsShown) _main.Show();
            if (popup == Popup.Rank) _rank.Show();
            else if (popup == Popup.Reward) _reward.Show();
            else _record.Show();
        }

        private static async Task<bool> EnsureLoaded()
        {
            if (_root != null && _main != null && _rank != null && _reward != null && _record != null) return true;
            Task<bool> task = _loadTask;
            if (task == null)
            {
                task = LoadAsync(_generation);
                _loadTask = task;
            }
            try
            {
                return await task;
            }
            finally
            {
                if (ReferenceEquals(_loadTask, task)) _loadTask = null;
            }
        }

        private static async Task<bool> LoadAsync(int generation)
        {
            string key = GameResPath.GetUIPrefab(Module, Prefab);
            GameObject root = null;
            try
            {
                root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
                if (generation != _generation)
                {
                    if (root != null) ResManager.ReleaseInstance(root);
                    return false;
                }
                if (root == null)
                {
                    GameLog.Error("ListDuobao", "prefab load failed: {0}", key);
                    return false;
                }

                ListDuobaoView main = root.GetComponentInChildren<ListDuobaoView>(true);
                ListRankView rank = root.GetComponentInChildren<ListRankView>(true);
                ListRewardView reward = root.GetComponentInChildren<ListRewardView>(true);
                ListDuobaoRecordView record = root.GetComponentInChildren<ListDuobaoRecordView>(true);
                if (main == null || rank == null || reward == null || record == null)
                {
                    GameLog.Error("ListDuobao", "ListDuobaoModule missing business views; run ListDuobaoBindUpgrader");
                    ResManager.ReleaseInstance(root);
                    return false;
                }

                main.gameObject.SetActive(false);
                rank.gameObject.SetActive(false);
                reward.gameObject.SetActive(false);
                record.gameObject.SetActive(false);
                await ListDuobaoConfigs.EnsureLoaded();
                if (generation != _generation)
                {
                    ResManager.ReleaseInstance(root);
                    return false;
                }

                _root = root;
                _root.name = Prefab;
                _main = main;
                _rank = rank;
                _reward = reward;
                _record = record;
                return true;
            }
            catch (Exception e)
            {
                if (root != null) ResManager.ReleaseInstance(root);
                GameLog.Error("ListDuobao", "load failed: {0}", e);
                return false;
            }
        }

        private static CustomActivityModel.ActEntry ResolveActiveAct()
        {
            CustomActivityModel model = CustomActivityModel.Instance;
            CustomActivityModel.ActEntry current = model.GetActiveListDuobaoAct();
            if (current != null && current.BaseType == BaseType) return current;
            foreach (KeyValuePair<long, CustomActivityModel.ActEntry> pair in model.ActList)
            {
                if (pair.Value.BaseType != BaseType) continue;
                model.SetListDuobaoSubType(pair.Value.SubType);
                return pair.Value;
            }
            return null;
        }

        internal static void Reset()
        {
            unchecked { _generation++; }
            Close();
            if (_root != null) ResManager.ReleaseInstance(_root);
            _root = null;
            _main = null;
            _rank = null;
            _reward = null;
            _record = null;
            _loadTask = null;
        }

        private enum Popup { Rank, Reward, Record }
    }
}
