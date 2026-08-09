using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.BossPersonal;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Boss.Views.BossPersonal
{
    /// <summary>
    /// 专属大妖入口。对标老端 BossPersonalEnterView：61020 列表、选择、次数展示、挑战、掉落记录、
    /// 战魂商店及 VIP 次数弹窗。票券/奖励/模型资源闭包尚不完整时不伪造数据。
    /// </summary>
    public sealed class BossPersonalEnterView : BossPersonalEnterViewBind
    {
        private readonly List<BossPersonalItem> _items = new List<BossPersonalItem>();
        private DungeonModel.DunState _selected;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_BossPersonalItem != null) _tpl_BossPersonalItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            BindClick(_btn_dun, EnterSelected);
            BindClick(_btn_add, BossPersonalFlow.OpenVipAdd);
            BindClick(_btn_drop, RequestDropLog);
            BindClick(_img_shop, OpenSoulShop);
            BindClick(lb_vip, OpenVip);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            DungeonController.Instance.RequestState(DungeonModel.TYPE_VIP_PERSON_BOSS);
            _ = RebuildAsync();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();

        private async Task RebuildAsync()
        {
            await DungeonConfigs.EnsureLoaded();
            if (!IsShown) return;
            Rebuild();
        }

        private void Rebuild()
        {
            for (int i = 0; i < _items.Count; i++) _items[i].gameObject.SetActive(false);
            if (!DungeonModel.Instance.DunStatesByType.TryGetValue(
                    DungeonModel.TYPE_VIP_PERSON_BOSS, out List<DungeonModel.DunState> states) || states == null)
            {
                SetEmptyState();
                return;
            }

            for (int i = 0; i < states.Count; i++)
            {
                DungeonModel.DunState state = states[i];
                BossPersonalItem item = GetOrCreate(i);
                if (item == null) continue;
                item.gameObject.SetActive(true);
                item.Show(new BossPersonalItem.Args(state, i, Select));
            }
            if (_selected == null || states.Find(v => v.DunId == _selected.DunId) == null)
                _selected = states.Count > 0 ? states[0] : null;
            UpdateSelection();
            RefreshSelected();
        }

        private BossPersonalItem GetOrCreate(int index)
        {
            if (index < _items.Count) return _items[index];
            if (_tpl_BossPersonalItem == null || _sl_dg_room == null || _sl_dg_room.content == null)
                throw new InvalidOperationException("BossPersonalItem template/content is not bound");
            GameObject go = Instantiate(_tpl_BossPersonalItem, _sl_dg_room.content);
            go.name = "BossPersonalItem_" + index;
            BossPersonalItem item = go.GetComponent<BossPersonalItem>();
            if (item == null)
            {
                GameLog.Error("BossPersonal", "BossPersonalItem template is not runtime-subclass-owned; prefab GUID mismatch");
                Destroy(go);
                return null;
            }
            _items.Add(item);
            return item;
        }

        private void Select(DungeonModel.DunState state)
        {
            _selected = state;
            UpdateSelection();
            RefreshSelected();
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < _items.Count; i++)
                _items[i].SetSelected(_selected != null && _items[i].DunId == _selected.DunId);
        }

        private void RefreshSelected()
        {
            if (_selected == null) { SetEmptyState(); return; }
            string name = DungeonConfigs.GetName(_selected.DunId);
            if (_lb_boss_name != null) _lb_boss_name.text = name;
            if (_lb_remain_time != null)
                _lb_remain_time.text = string.Format("今日已挑战 {0} 次", _selected.DailyCount);
            if (_lb_enter != null) _lb_enter.text = "挑战大妖";
            // 老端首挑/红点来自 ex_data key=10/first_flag；DailyCount 与其没有已证明的等价关系。
            // 权威字段缺失时隐藏动态状态，不猜测玩家首挑状态。
            if (_lb_first != null) _lb_first.gameObject.SetActive(false);
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            if (_lb_cond != null) _lb_cond.text = DungeonConfigs.GetCondition(_selected.DunId);
            if (_reward_con != null) _reward_con.verticalNormalizedPosition = 1f;
            SetScrollArrows();
        }

        private void SetEmptyState()
        {
            _selected = null;
            if (_lb_boss_name != null) _lb_boss_name.text = "专属大妖";
            if (_lb_remain_time != null) _lb_remain_time.text = "数据加载中";
            if (_lb_first != null) _lb_first.gameObject.SetActive(false);
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            SetScrollArrows();
        }

        private void SetScrollArrows()
        {
            bool overflow = _sl_dg_room != null && _sl_dg_room.content != null && _sl_dg_room.viewport != null
                && _sl_dg_room.content.rect.width > _sl_dg_room.viewport.rect.width + 0.5f;
            if (_img_left != null) _img_left.gameObject.SetActive(overflow);
            if (_img_right != null) _img_right.gameObject.SetActive(overflow);
        }

        private void EnterSelected()
        {
            if (_selected == null) return;
            DungeonController.Instance.Enter(_selected.DunId);
        }

        private static void RequestDropLog()
        {
            BossController.Instance.RequestDropLog();
            GameLog.Info("BossPersonal", "已请求掉落记录；展示窗由 Boss 主路线承载");
        }

        private static void OpenSoulShop()
        {
            GameLog.Info("BossPersonal", "战魂商店入口待 BossFieldFlow 注册");
        }

        private static void OpenVip()
        {
            GameLog.Info("BossPersonal", "VIP入口属于跨模块路由，当前仅登记 blocker");
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_DUNGEON_UPDATE, Rebuild);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_DUNGEON_UPDATE, Rebuild);
            _subscribed = false;
        }

        private static void BindClick(Component target, Action action)
        {
            if (target == null || action == null) return;
            Image image = target as Image ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }
    }
}
