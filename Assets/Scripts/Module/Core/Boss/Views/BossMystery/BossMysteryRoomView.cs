using System;
using System.Collections.Generic;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.BossMystery;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossMystery
{
    public sealed class BossMysteryRoomView : BossMysteryRoomViewBind
    {
        public Action<BossModel.BossEntry> SelectionChanged;
        private readonly List<BossMysteryMonItem> _items = new List<BossMysteryMonItem>();
        private GameObject _itemTemplate;
        private int _selectedIndex;

        protected override void OnInit()
        {
            if (challengeBtn != null) UIUtil.AddClick(challengeBtn, EnterSelectedBoss);
        }

        protected override void OnShow(object args) => RefreshFromModel();

        public void SetItemTemplate(GameObject template) => _itemTemplate = template;

        public void RefreshFromModel()
        {
            BossModel.BossTypeState state = BossModel.Instance.GetBossState(BossMysteryFlow.BossType);
            if (challenge_num != null)
                challenge_num.text = state == null ? "今日挑战次数：--" : $"今日挑战次数：{state.Count}/{state.AllCount}";
            if (scene_role != null) scene_role.text = "当前进入人数：需运行态探针";

            if (state == null || _itemTemplate == null || _scroll_con == null || _scroll_con.content == null) return;
            for (int i = 0; i < _items.Count; i++) _items[i].gameObject.SetActive(false);
            for (int i = 0; i < state.BossList.Count; i++)
            {
                int index = i;
                BossMysteryMonItem item = GetOrCreate(i);
                if (item == null) break;
                item.gameObject.SetActive(true);
                item.SetData(state.BossList[i], i == _selectedIndex, () => Select(index));
            }
            if (state.BossList.Count > 0) Select(Mathf.Clamp(_selectedIndex, 0, state.BossList.Count - 1));
        }

        private BossMysteryMonItem GetOrCreate(int index)
        {
            if (index < _items.Count) return _items[index];
            GameObject go = Instantiate(_itemTemplate, _scroll_con.content);
            go.name = "BossMysteryMonItem_" + index;
            go.SetActive(true);
            BossMysteryMonItem item = go.GetComponent<BossMysteryMonItem>();
            if (item == null)
            {
                Destroy(go);
                return null;
            }
            item.Show();
            _items.Add(item);
            return item;
        }

        private void Select(int index)
        {
            BossModel.BossTypeState state = BossModel.Instance.GetBossState(BossMysteryFlow.BossType);
            if (state == null || index < 0 || index >= state.BossList.Count) return;
            _selectedIndex = index;
            for (int i = 0; i < _items.Count; i++) _items[i].SetSelected(i == index);
            SelectionChanged?.Invoke(state.BossList[index]);
            if (btn_label != null) btn_label.text = state.Count > 0 ? "进入秘境" : "次数不足";
        }

        private void EnterSelectedBoss()
        {
            BossModel.BossTypeState state = BossModel.Instance.GetBossState(BossMysteryFlow.BossType);
            if (state == null || state.Count <= 0 || _selectedIndex < 0 || _selectedIndex >= state.BossList.Count) return;
            BossController.Instance.EnterBoss(BossMysteryFlow.BossType, state.BossList[_selectedIndex].BossId);
        }
    }
}
