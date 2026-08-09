using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.BossField;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    /// <summary>新野外大妖入口：46xxx 权威列表/体力、选择摘要、提醒、商店和掉落入口。</summary>
    public sealed class BossFieldEnterView : BossFieldEnterViewBind
    {
        private BossModel.BossEntry _selected;
        private int _selectedType = BossModel.BossType.Field;
        private BossFieldRoomItem _room;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_BossFieldRoomItem != null) _tpl_BossFieldRoomItem.SetActive(false);
            if (_tpl_BossFieldRewardItem != null) _tpl_BossFieldRewardItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            BindClick(_img_drop, RequestDropLog);
            BindClick(_img_shop, BossFieldFlow.OpenSoulShop);
            BindClick(_img_attention, ToggleRemind);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            BossController.Instance.RequestBossList(BossModel.BossType.Field);
            BossController.Instance.RequestBossList(BossModel.BossType.FieldSpecial);
            BossController.Instance.RequestBossList(BossModel.BossType.FieldInfinite);
            BossController.Instance.RequestBossVitDetail(BossModel.BossType.Field);
            _ = RefreshAsync();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();

        private async Task RefreshAsync() { await BossConfigs.EnsureLoaded(); if (IsShown) Refresh(); }

        private void Refresh()
        {
            BossModel.BossTypeState state = BossModel.Instance.GetBossState(_selectedType);
            if (state == null || state.BossList.Count == 0)
            {
                SetEmpty();
                return;
            }
            if (_selected == null || state.GetEntry(_selected.BossId) == null) _selected = state.BossList[0];
            if (_lb_attention != null) _lb_attention.text = _selected.IsRemind ? "已关注" : "关注";
            if (_lb_vit != null) _lb_vit.text = "体力 " + state.Vit;
            if (_lb_drop_tips != null) _lb_drop_tips.gameObject.SetActive(false);
            BossFieldRoomItem room = EnsureRoom();
            if (room != null) room.Show(new BossFieldRoomItem.Args(_selectedType, _selected.BossId));
        }

        private BossFieldRoomItem EnsureRoom()
        {
            if (_room != null) return _room;
            if (_tpl_BossFieldRoomItem == null || _box_room == null)
                throw new InvalidOperationException("BossFieldRoomItem template/container is not bound");
            GameObject go = Instantiate(_tpl_BossFieldRoomItem, _box_room);
            go.name = "BossFieldRoomItem_Runtime";
            _room = go.GetComponent<BossFieldRoomItem>();
            if (_room == null)
            {
                GameLog.Error("BossField", "BossFieldRoomItem template is not runtime-subclass-owned; prefab GUID mismatch");
                Destroy(go);
                return null;
            }
            return _room;
        }

        private void SetEmpty()
        {
            _selected = null;
            if (_lb_vit != null) _lb_vit.text = "体力 --";
        }

        private void ToggleRemind()
        {
            if (_selected == null) return;
            BossController.Instance.SetBossRemind(_selectedType, _selected.BossId, !_selected.IsRemind);
        }

        private static void RequestDropLog() => BossController.Instance.RequestDropLog();

        private void OnBossList(int bossType) { if (bossType == _selectedType) Refresh(); }
        private void OnVit(int bossType) { if (bossType == BossModel.BossType.Field) Refresh(); }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On<int>(GlobalEvent.EVT_BOSS_LIST_UPDATE, OnBossList);
            EventDispatcher.On<int>(GlobalEvent.EVT_BOSS_VIT_UPDATE, OnVit);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_BOSS_REMIND_UPDATE, OnRemind);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BOSS_LIST_UPDATE, OnBossList);
            EventDispatcher.Off<int>(GlobalEvent.EVT_BOSS_VIT_UPDATE, OnVit);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_BOSS_REMIND_UPDATE, OnRemind);
            _subscribed = false;
        }

        private void OnRemind(int bossType, int bossId)
        { if (_selected != null && bossType == _selectedType && bossId == _selected.BossId) Refresh(); }

        private static void BindClick(Component target, Action action)
        {
            if (target == null) return;
            Image image = target as Image ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }
    }
}
