using System;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.BossField;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossAbyssEnterView : BossAbyssEnterViewBind
    {
        private BossModel.BossEntry _selected;
        private BossAbyssRoomItem _room;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_BossAbyssRoomItem != null) _tpl_BossAbyssRoomItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            BindClick(_btn_drop, () => BossController.Instance.RequestDropLog());
            BindClick(_img_shop, BossFieldFlow.OpenSoulShop);
            BindClick(_gp_attention, ToggleRemind);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            BossController.Instance.RequestBossList(BossModel.BossType.Abyss);
            Refresh();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();

        private void Refresh()
        {
            BossModel.BossTypeState state = BossModel.Instance.GetBossState(BossModel.BossType.Abyss);
            if (state == null || state.BossList.Count == 0)
            {
                return;
            }
            if (_selected == null || state.GetEntry(_selected.BossId) == null) _selected = state.BossList[0];
            if (attention != null) attention.gameObject.SetActive(_selected.IsRemind);
            if (_gp_tips != null) _gp_tips.gameObject.SetActive(false);
            BossAbyssRoomItem room = EnsureRoom();
            if (room != null) room.Show(new BossAbyssRoomItem.Args(state, _selected.BossId, Select));
        }

        private BossAbyssRoomItem EnsureRoom()
        {
            if (_room != null) return _room;
            if (_tpl_BossAbyssRoomItem == null || _gp_room_con == null)
                throw new InvalidOperationException("BossAbyssRoomItem template/container is not bound");
            GameObject go = Instantiate(_tpl_BossAbyssRoomItem, _gp_room_con);
            go.name = "BossAbyssRoomItem_Runtime";
            _room = go.GetComponent<BossAbyssRoomItem>();
            if (_room == null)
            {
                GameLog.Error("BossField", "BossAbyssRoomItem template is not runtime-subclass-owned; prefab GUID mismatch");
                Destroy(go);
                return null;
            }
            return _room;
        }

        private void Select(BossModel.BossEntry entry) { _selected = entry; Refresh(); }
        private void ToggleRemind()
        { if (_selected != null) BossController.Instance.SetBossRemind(BossModel.BossType.Abyss, _selected.BossId, !_selected.IsRemind); }
        private void OnList(int type) { if (type == BossModel.BossType.Abyss) Refresh(); }
        private void OnRemind(int type, int id) { if (type == BossModel.BossType.Abyss) Refresh(); }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On<int>(GlobalEvent.EVT_BOSS_LIST_UPDATE, OnList);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_BOSS_REMIND_UPDATE, OnRemind);
            _subscribed = true;
        }
        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BOSS_LIST_UPDATE, OnList);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_BOSS_REMIND_UPDATE, OnRemind);
            _subscribed = false;
        }
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
