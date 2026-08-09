using System;
using System.Collections.Generic;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.BossField;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossAbyssRoomItem : BossAbyssRoomItemBind
    {
        public sealed class Args
        {
            public readonly BossModel.BossTypeState State;
            public readonly int SelectedId;
            public readonly Action<BossModel.BossEntry> Select;
            public Args(BossModel.BossTypeState state, int selectedId, Action<BossModel.BossEntry> select)
            { State = state; SelectedId = selectedId; Select = select; }
        }

        private readonly List<RectTransform> _slots = new List<RectTransform>();
        private BossModel.BossTypeState _state;
        private BossModel.BossEntry _selected;
        private Action<BossModel.BossEntry> _select;

        protected override void OnInit()
        {
            _slots.AddRange(new[]{_Group1,_Group2,_Group3,_Group4,_Group5,_Group6,_Group7,_Group8});
            for (int i = 0; i < _slots.Count; i++)
            {
                int index = i;
                BindClick(_slots[i], () => SelectIndex(index));
            }
            BindClick(_btn_go, Enter);
            BindClick(_gp_vip, () => GameLog.Info("BossField", "Abyss VIP跳转为跨模块 blocker"));
        }

        protected override void OnShow(object args)
        {
            Args data = args as Args;
            if (data == null) return;
            _state = data.State; _select = data.Select;
            _selected = _state?.GetEntry(data.SelectedId);
            if (_selected == null && _state != null && _state.BossList.Count > 0) _selected = _state.BossList[0];
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i] != null) _slots[i].gameObject.SetActive(_state != null && i < _state.BossList.Count);
        }

        private void SelectIndex(int index)
        {
            if (_state == null || index < 0 || index >= _state.BossList.Count) return;
            _selected = _state.BossList[index];
            _select?.Invoke(_selected);
        }
        private void Enter()
        { if (_selected != null) BossController.Instance.EnterBoss(BossModel.BossType.Abyss, _selected.BossId); }
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
