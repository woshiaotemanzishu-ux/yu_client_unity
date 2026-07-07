using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 战斗(PK)模式选择弹窗(对标老客户端 MainUIFightModeView.ts):按当前场景 requirement 的
    /// pkstate_list 列出可选模式(PkStatusModel.FightModeInfo),高亮主角当前 pk_status;
    /// 点击项发 13012 切换(PkStatusController),成功(EVT_PK_CHANGE_SUCCESS)提示「切换成功」并关闭;
    /// 和平切换冷却中(RoleModel.PeaceCdActive)点击提示「冷却中」,冷却结束自动关闭(对标老端 OnTime)。
    /// 列表项克隆走 MainUIDownView 同款模板模式(_tpl 隐藏 + Instantiate 到 _gp_item)。
    /// </summary>
    public sealed class MainUIFightModeView : MainUIFightModeViewBind
    {
        // 对标老端每项 43px 纵向行距(SetPosition(0,(index-1)*43))。
        private const float ITEM_GAP = 43f;

        private readonly List<MainUIFightModeItem> _items = new List<MainUIFightModeItem>();
        private readonly Dictionary<int, int> _pkStateToIndex = new Dictionary<int, int>();
        private int _curPkStatus = int.MinValue;
        private int _refreshVersion;
        private bool _peaceCdWasActive;

        protected override void OnInit()
        {
            if (_tpl_MainUIFightModeItem != null) _tpl_MainUIFightModeItem.SetActive(false);
            EventDispatcher.On(GlobalEvent.EVT_PK_CHANGE_SUCCESS, OnChangeSuccess);
            EventDispatcher.On(GlobalEvent.EVT_PK_STATUS_CHANGED, OnPkStatusChanged);
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_PK_CHANGE_SUCCESS, OnChangeSuccess);
            EventDispatcher.Off(GlobalEvent.EVT_PK_STATUS_CHANGED, OnPkStatusChanged);
        }

        private void OnDestroy()
        {
            EventDispatcher.Off(GlobalEvent.EVT_PK_CHANGE_SUCCESS, OnChangeSuccess);
            EventDispatcher.Off(GlobalEvent.EVT_PK_STATUS_CHANGED, OnPkStatusChanged);
        }

        protected override void OnShow(object args)
        {
            _peaceCdWasActive = false;
            _ = RefreshFromSceneAsync();
        }

        /// <summary>对标老端 LoadSuccess:场景 requirement.pkstate_list → 建项 + 高亮当前 pk_status。</summary>
        private async Task RefreshFromSceneAsync()
        {
            int version = ++_refreshVersion;
            await MainUIConfigs.EnsureSceneLoaded();
            if (this == null || version != _refreshVersion || !IsShown) return;

            MainUIConfigs.SceneCfg cfg = MainUIConfigs.GetSceneCfg(RoleModel.Instance.SceneId);
            int[] states = cfg != null ? cfg.PkStateList : System.Array.Empty<int>();

            var modes = new List<FightModeInfoData>(states.Length);
            foreach (int pkState in states)
            {
                FightModeInfoData info = PkStatusModel.Get(pkState);
                if (info != null) modes.Add(info);
            }
            RefreshModes(modes, RoleModel.Instance.PkStatus);
        }

        /// <summary>按可用 PK 模式列表铺项(对标 LoadSuccess 建项循环 + SetPkState);null/空=清空。</summary>
        public void RefreshModes(IList<FightModeInfoData> modes, int curPkStatus)
        {
            _pkStateToIndex.Clear();
            int count = modes?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                MainUIFightModeItem item = GetOrCreateItem(i);
                if (item == null) continue;
                item.gameObject.SetActive(true);
                FightModeInfoData info = modes[i];
                item.SetData(info, OnClickMode);
                SetItemPosition(item, i);
                if (info != null) _pkStateToIndex[info.PkStatus] = i;
            }
            for (int i = count; i < _items.Count; i++)
            {
                if (_items[i] != null) _items[i].gameObject.SetActive(false);
            }

            _curPkStatus = int.MinValue;
            SetPkState(curPkStatus);

            if (count == 0)
            {
                GameLog.Info("MainUI", "PK 模式列表为空(场景 {0} 无 pkstate_list)", RoleModel.Instance.SceneId);
            }
        }

        /// <summary>对标 SetPkState:旧选中取消高亮、新选中高亮。</summary>
        private void SetPkState(int pkStatus)
        {
            if (_curPkStatus == pkStatus) return;
            if (_pkStateToIndex.TryGetValue(_curPkStatus, out int oldIdx) && oldIdx < _items.Count && _items[oldIdx] != null)
                _items[oldIdx].SetSelect(false);
            if (_pkStateToIndex.TryGetValue(pkStatus, out int newIdx) && newIdx < _items.Count && _items[newIdx] != null)
                _items[newIdx].SetSelect(true);
            _curPkStatus = pkStatus;
        }

        /// <summary>对标 click_func:和平CD 中提示「冷却中」,否则发 13012 切换。</summary>
        private void OnClickMode(int pkStatus)
        {
            if (RoleModel.Instance.PeaceCdActive)
            {
                TipsManager.Toast("冷却中");
                return;
            }
            PkStatusController.Instance.SendChangePkStatus(pkStatus);
        }

        /// <summary>13012 切换成功 → 提示并关闭(对标老端 CHANGE_SUCCESS 绑定)。</summary>
        private void OnChangeSuccess()
        {
            if (!IsShown) return;
            TipsManager.Toast("切换成功");
            Hide();
        }

        /// <summary>主角 pk_status 被动变化(广播/自块同步)时,弹窗开着就刷新高亮。</summary>
        private void OnPkStatusChanged()
        {
            if (!IsShown) return;
            SetPkState(RoleModel.Instance.PkStatus);
        }

        /// <summary>冷却结束自动关闭(对标老端 StartTime/OnTime:left_time<0 → Close)。</summary>
        private void Update()
        {
            if (!IsShown) return;
            if (RoleModel.Instance.PeaceCdActive)
            {
                _peaceCdWasActive = true;
                return;
            }
            if (_peaceCdWasActive)
            {
                _peaceCdWasActive = false;
                Hide();
            }
        }

        private MainUIFightModeItem GetOrCreateItem(int index)
        {
            while (_items.Count <= index) _items.Add(null);
            if (_items[index] != null) return _items[index];

            if (_tpl_MainUIFightModeItem == null || _gp_item == null)
            {
                GameLog.Error("MainUI", "MainUIFightModeView 缺 _tpl_MainUIFightModeItem 或 _gp_item");
                return null;
            }

            GameObject go = Instantiate(_tpl_MainUIFightModeItem, _gp_item);
            go.SetActive(true);

            MainUIFightModeItem item = go.GetComponent<MainUIFightModeItem>();
            if (item == null)
            {
                GameLog.Error("MainUI", "_tpl_MainUIFightModeItem 缺 MainUIFightModeItem 组件(回填?)");
                Destroy(go);
                return null;
            }

            _items[index] = item;
            return item;
        }

        /// <summary>动态项纵向摆放(对标 SetPosition 的 index*43);容器若已挂布局组件则此偏移被覆盖,无害。</summary>
        private static void SetItemPosition(MainUIFightModeItem item, int index)
        {
            RectTransform rt = (RectTransform)item.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(index * ITEM_GAP));
        }
    }
}
