using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 「通知位」图标区(老端 MainUISecondaryView 的 _box_notice 簇,竞榜卡下方的开服活动/神鹤类通知图标)
    /// —— 从 HudSecondary 拆出的独立区域视图,【槽位式】(同 <see cref="MainUIActivityView"/> 已验收模式)。
    ///
    /// 布局 100% 归 prefab、代码绝不算坐标:_box_notice 下由**美术手摆一批空槽位**(想摆几个/横排竖排/摆哪都行),
    /// 本类只把"通知位该显示的图标"**按顺序一个个填进这些槽**,图标继承所在槽的位置。
    /// 槽比图标多 → 多余的槽隐藏;图标比槽多 → 超出的丢弃并告警。
    /// 老端 RefreshIconPos 的坐标计算/隐藏位/缩放透明渐隐一概不移植 —— 想调布局去 HudNotice.prefab 摆槽位。
    /// </summary>
    public sealed class MainUINoticeView : MainUINoticeViewBind
    {
        private readonly Dictionary<string, ActivityIcon> _iconByType = new Dictionary<string, ActivityIcon>();
        private bool _activityFolded;

        protected override void OnInit()
        {
            if (_tpl_ActivityIcon != null) _tpl_ActivityIcon.SetActive(false);
            ClearDesignTimeSampleIcons();
        }

        /// <summary>清掉 prefab 里为“设计期可视化”塞进各槽的样例图标(编辑器可见、便于摆槽位;运行时清掉换真图标)。</summary>
        private void ClearDesignTimeSampleIcons()
        {
            if (_box_notice == null) return;
            for (int s = 0; s < _box_notice.childCount; s++)
            {
                Transform slot = _box_notice.GetChild(s);
                for (int i = slot.childCount - 1; i >= 0; i--)
                {
                    GameObject c = slot.GetChild(i).gameObject;
                    c.SetActive(false);
                    Destroy(c);
                }
            }
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On<string, int>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_ADD, OnActivityIconChanged);
            EventDispatcher.On<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnActivityIconChanged);
            EventDispatcher.On<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, OnActivityIconUpdated);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnOpenConditionChanged);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnOpenConditionChanged);
            EventDispatcher.On<bool>(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, OnActivityFold);
            RefreshSlotsAsync();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off<string, int>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_ADD, OnActivityIconChanged);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnActivityIconChanged);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, OnActivityIconUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnOpenConditionChanged);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnOpenConditionChanged);
            EventDispatcher.Off<bool>(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, OnActivityFold);
        }

        // 增/删任一活动图标 → 整体按顺序重填槽位(槽位式无增量,全量重填最简单稳妥,通知位常年 ≤3 个开销可忽略)。
        private void OnActivityIconChanged(string iconType) => RefreshSlotsAsync();
        private void OnActivityIconChanged(string iconType, int locationType) => RefreshSlotsAsync();
        private void OnOpenConditionChanged() => RefreshSlotsAsync();

        // 图标内容更新(红点/倒计时等):只刷该图标自身,不动槽位排布。
        private void OnActivityIconUpdated(string iconType)
        {
            if (_iconByType.TryGetValue(iconType, out ActivityIcon item) && item != null) item.Refresh();
        }

        /// <summary>太极折叠(全局事件,由 MainUIFoldView 广播):收放整片通知位 _box_notice;展开时补填一次。</summary>
        private void OnActivityFold(bool folded)
        {
            _activityFolded = folded;
            if (_box_notice != null) _box_notice.gameObject.SetActive(!folded);
            if (!folded) RefreshSlotsAsync();
        }

        private async void RefreshSlotsAsync()
        {
            await ActivityIconManager.Instance.RefreshDefaultIconsAsync();
            if (this == null || _activityFolded) return;
            FillSlots();
        }

        /// <summary>把通知位该显示的图标按顺序填进 _box_notice 下的槽位(槽位位置由 prefab 决定,代码不算坐标)。</summary>
        private void FillSlots()
        {
            if (_box_notice == null) return;

            List<string> types = CollectOwnedIconTypes();
            int slotCount = _box_notice.childCount;
            int shown = Mathf.Min(slotCount, types.Count);

            // 释放不再显示的图标(超出槽位容量的、或已关闭的活动)。
            var shownSet = new HashSet<string>();
            for (int i = 0; i < shown; i++) shownSet.Add(types[i]);
            var toRemove = new List<string>();
            foreach (KeyValuePair<string, ActivityIcon> kv in _iconByType)
                if (!shownSet.Contains(kv.Key)) toRemove.Add(kv.Key);
            for (int i = 0; i < toRemove.Count; i++)
            {
                if (_iconByType[toRemove[i]] != null) Destroy(_iconByType[toRemove[i]].gameObject);
                _iconByType.Remove(toRemove[i]);
            }

            // 逐槽填:前 shown 个槽放图标,其余槽隐藏。
            for (int i = 0; i < slotCount; i++)
            {
                RectTransform slot = _box_notice.GetChild(i) as RectTransform;
                if (slot == null) continue;
                if (i < shown)
                {
                    ActivityIcon icon = GetOrCreateIcon(types[i]);
                    if (icon != null) PlaceIconInSlot(icon, slot);
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.gameObject.SetActive(false); // 空槽隐藏
                }
            }

            if (types.Count > slotCount)
                GameLog.Warn("MainUI", "通知位图标 {0} 个 > 槽位 {1} 个,超出的 {2} 个未显示(去 HudNotice.prefab 里 NoticeSlots 下多摆几个槽)",
                    types.Count, slotCount, types.Count - slotCount);
        }

        // 通知位该认领的图标类型(location_type=6),按 pos_index 稳定排序作为填充顺序。
        // 排除项对齐原 MainUISecondaryView:158(强化,特殊位)、241 族(老端强制搬活动网格)、612 前缀(限时抢购,通知位隐藏)。
        private List<string> CollectOwnedIconTypes()
        {
            var list = new List<string>();
            foreach (KeyValuePair<string, ActivityIconManager.IconInfo> kv in ActivityIconManager.Instance.IconInfoByType)
            {
                if (kv.Value?.Data == null) continue;
                if (kv.Value.Data.LocationType != ActivityIconManager.LocationType.Notice) continue;
                if (kv.Key == "158" || kv.Key == "241" || kv.Key == "241@1@0") continue;
                if (!string.IsNullOrEmpty(kv.Key) && kv.Key.StartsWith("612")) continue;
                list.Add(kv.Key);
            }
            list.Sort(CompareIconType);
            return list;
        }

        private ActivityIcon GetOrCreateIcon(string iconType)
        {
            if (_iconByType.TryGetValue(iconType, out ActivityIcon existing) && existing != null)
            {
                existing.Refresh();
                return existing;
            }
            if (_tpl_ActivityIcon == null)
            {
                GameLog.Error("MainUI", "Notice ActivityIcon template missing");
                return null;
            }

            GameObject go = Instantiate(_tpl_ActivityIcon, _box_notice); // 临时父,PlaceIconInSlot 会移进对应槽
            go.SetActive(true);
            ActivityIcon item = go.GetComponent<ActivityIcon>();
            if (item == null)
            {
                GameLog.Error("MainUI", "Notice ActivityIcon template is not rebound to business script");
                Destroy(go);
                return null;
            }
            item.Show();
            item.SetIconType(iconType);
            item.SetVisible(true);
            _iconByType[iconType] = item;
            return item;
        }

        // 撑满所在槽:槽多大图多大,显示尺寸完全由 prefab 的槽控制(修复:横条图标被压进方形模板)。
        private static void PlaceIconInSlot(ActivityIcon icon, RectTransform slot)
        {
            var rt = (RectTransform)icon.transform;
            rt.SetParent(slot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            icon.gameObject.SetActive(true);
        }

        private static int CompareIconType(string a, string b)
        {
            MainUIConfigs.FunctionIconCfg ca = MainUIConfigs.GetFunctionIconCfg(a);
            MainUIConfigs.FunctionIconCfg cb = MainUIConfigs.GetFunctionIconCfg(b);
            if (ca == null && cb == null) return string.CompareOrdinal(a, b);
            if (ca == null) return 1;
            if (cb == null) return -1;
            return ca.PosIndex.CompareTo(cb.PosIndex);
        }
    }
}
