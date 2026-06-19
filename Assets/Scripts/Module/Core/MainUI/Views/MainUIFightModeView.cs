using System.Collections.Generic;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 战斗(PK)模式选择弹窗(对标老客户端 MainUIFightModeView.ts):按当前场景可用的 PK 状态列出模式项,
    /// 点击切换、高亮当前模式;切换成功提示并关闭;和平 CD 中点击提示「冷却中」。
    ///
    /// 降级:PkStatusModel(FightModeInfo 配置 + CHANGE_PK_STATUS/CHANGE_SUCCESS 协议)、场景
    /// requirement 的 pkstate_list、和平CD(peace_cd_time)均未移植 → 列表暂空 + 日志「待对接」;
    /// 点击只打日志、不发协议。事件驱动弹层(点 PK 图标打开),默认关闭、不进 FirstPass。
    /// 列表项克隆走 MainUIDownView 同款模板模式(_tpl 隐藏 + Instantiate 到 _gp_item)。
    /// </summary>
    public sealed class MainUIFightModeView : MainUIFightModeViewBind
    {
        // 对标老端每项 43px 纵向行距(SetPosition(0,(index-1)*43))。
        private const float ITEM_GAP = 43f;

        private readonly List<MainUIFightModeItem> _items = new List<MainUIFightModeItem>();
        private readonly Dictionary<int, int> _pkStateToIndex = new Dictionary<int, int>();
        private int _curPkStatus = int.MinValue;

        protected override void OnInit()
        {
            if (_tpl_MainUIFightModeItem != null) _tpl_MainUIFightModeItem.SetActive(false);
            // TODO 待接:PkStatusModel.CHANGE_SUCCESS → 提示「切换成功」+关闭;RoleVo.peace_cd_time → CD 倒计时自动关。
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess 从场景 requirement.pkstate_list 建项 + 高亮当前 pk_status;数据源未移植 → 空列表。
            RefreshModes(null, int.MinValue);
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
                GameLog.Info("MainUI", "PK 模式列表为空 → 待对接 PK 模式数据(PkStatusModel.FightModeInfo / 场景 pkstate_list)");
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

        /// <summary>对标 click_func:和平CD 中提示「冷却中」,否则发切换协议。降级:协议未移植 → 打日志。</summary>
        private void OnClickMode(int pkStatus)
        {
            // TODO 待接:peace_cd_is_playing → 提示「冷却中」;否则 PkStatusModel.Fire(CHANGE_PK_STATUS, pkStatus)。
            GameLog.Info("MainUI", "点击切换 PK 模式 pk_status={0} → 待对接切换协议 CHANGE_PK_STATUS", pkStatus);
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
