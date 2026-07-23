using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 活动图标网格视图(对标 yu_client MainUIActivityView 的 ADD_ICON 流)—— 【槽位式】。
    ///
    /// 布局 100% 归 prefab、代码绝不算坐标:_gp_con 下由**美术手摆一批空槽位**(想摆几排/每排几个/摆哪都行),
    /// 本类只做一件事——把"本视图该显示的图标"**按顺序一个个填进这些槽**(自然溢流:填满一个进下一个),
    /// 图标继承所在槽的位置。槽比图标多 → 多余的槽隐藏;图标比槽多 → 超出的丢弃并告警。
    /// 【不再按 location_type 分组、不再用 GridLayoutGroup/VerticalLayoutGroup 自动排布】—— 想调布局去 prefab 摆槽位。
    /// location_type=6/7(老端 _box_notice 及其后续位)也统一由本视图显示:配置/服务器仍决定图标是否存在,
    /// 但 Unity 不再为通知位另开 HudNotice 屏幕区域。
    ///
    /// 右上「竞榜/头号玩家榜」卡片已拆到 <see cref="MainUIRankView"/>;折叠太极已提到 <see cref="MainUIFoldView"/>(总装层)。
    /// </summary>
    public sealed class MainUIActivityView : MainUIActivityViewBind
    {
        // 诸天玄门族(241/241@1@0):配置里是 loc5/6(本该归 Secondary),老端强制搬进活动网格;这里让活动视图认领
        // (Secondary 侧 ShouldOwnSecondaryIcon 同步排除)。槽位式下不再有"第四排"概念,只是纳入本视图的顺序流。
        private static bool IsForcedActivityIcon(string iconType)
        {
            return iconType == "241" || iconType == "241@1@0";
        }

        // 612 前缀 = 限时等级抢购(LimitLevelShop)图标。老端 SHOP_ACTIVITY_ICON_PREFIX="612",
        // Suppress612ShopActivityIconsForChatBar 把它们从活动区隐藏、挪到聊天条商城入口 → 活动网格不显。
        // (MainUISecondaryView 早已同样排除;此前 MainUIActivityView 漏了,导致满级号抢购档在活动区平铺过量。)
        private static bool Is612Icon(string iconType)
        {
            return !string.IsNullOrEmpty(iconType) && iconType.StartsWith("612");
        }

        private readonly Dictionary<string, ActivityIcon> _iconByType = new Dictionary<string, ActivityIcon>();
        private bool _activityFolded;

        protected override void OnInit()
        {
            if (_tpl_ActivityIcon != null) _tpl_ActivityIcon.SetActive(false);
            ClearDesignTimeSampleIcons();
        }

        /// <summary>清掉 prefab 里为"设计期可视化"塞进各槽的样例图标(编辑器可见、便于摆槽位;运行时清掉换真图标)。</summary>
        private void ClearDesignTimeSampleIcons()
        {
            if (_gp_con == null) return;
            for (int s = 0; s < _gp_con.childCount; s++)
            {
                Transform slot = _gp_con.GetChild(s);
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

        // 增/删任一活动图标 → 整体按顺序重填槽位(槽位式无增量,全量重填最简单稳妥,~20 个图标开销可忽略)。
        private void OnActivityIconChanged(string iconType) => RefreshSlotsAsync();
        private void OnActivityIconChanged(string iconType, int locationType) => RefreshSlotsAsync();
        private void OnOpenConditionChanged() => RefreshSlotsAsync();

        // 图标内容更新(红点/倒计时等):只刷该图标自身,不动槽位排布。
        private void OnActivityIconUpdated(string iconType)
        {
            if (_iconByType.TryGetValue(iconType, out ActivityIcon item) && item != null) item.Refresh();
        }

        /// <summary>太极折叠(全局事件,由 MainUIFoldView 广播):收放整片图标网格 _gp_con;展开时补填一次。</summary>
        private void OnActivityFold(bool folded)
        {
            _activityFolded = folded;
            if (_gp_con != null) _gp_con.gameObject.SetActive(!folded);
            if (!folded) RefreshSlotsAsync();
        }

        private async void RefreshSlotsAsync()
        {
            await ActivityIconManager.Instance.RefreshDefaultIconsAsync();
            if (this == null || _activityFolded) return;
            FillSlots();
        }

        /// <summary>把本视图该显示的图标按顺序填进 _gp_con 下的槽位(槽位位置由 prefab 决定,代码不算坐标)。</summary>
        private void FillSlots()
        {
            if (_gp_con == null) return;

            List<string> types = CollectOwnedIconTypes();
            int slotCount = _gp_con.childCount;
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
                RectTransform slot = _gp_con.GetChild(i) as RectTransform;
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
                GameLog.Warn("MainUI", "活动图标 {0} 个 > 槽位 {1} 个,超出的 {2} 个未显示(去 prefab 里 _gp_con 下多摆几个槽)",
                    types.Count, slotCount, types.Count - slotCount);
        }

        // 本视图认领所有活动区图标(含 loc6/7 通知位),按 (location_type, pos_index) 稳定排序。
        // location_type 仍是配置/服务端驱动的分类与排序元数据,不再对应多个 Unity 显示模块。
        private List<string> CollectOwnedIconTypes()
        {
            var list = new List<string>();
            foreach (KeyValuePair<string, ActivityIconManager.IconInfo> kv in ActivityIconManager.Instance.IconInfoByType)
            {
                if (kv.Value?.Data == null) continue;
                if (kv.Key == "153") continue; // 老端占位/无效图标,不显示
                // 612 限时抢购图标不进活动区(对标老端 Suppress612ShopActivityIconsForChatBar:活动区抑制、挪聊天条)。
                // 数据层仍在 ActivityIconManager(LimitLevelShop 变体驱动),只是活动网格不铺它们——否则满级号一堆抢购档会平铺过量。
                if (Is612Icon(kv.Key)) continue;
                if (ShouldOwnActivityIcon(kv.Value.Data.LocationType) || IsForcedActivityIcon(kv.Key))
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
                GameLog.Error("MainUI", "ActivityIcon template missing");
                return null;
            }

            GameObject go = Instantiate(_tpl_ActivityIcon, _gp_con); // 临时父,PlaceIconInSlot 会移进对应槽
            go.SetActive(true);
            ActivityIcon item = go.GetComponent<ActivityIcon>();
            if (item == null)
            {
                GameLog.Error("MainUI", "ActivityIcon template is not rebound to business script");
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

        private static bool ShouldOwnActivityIcon(int locationType)
        {
            return locationType == ActivityIconManager.LocationType.ActivityOne
                   || locationType == ActivityIconManager.LocationType.ActivityTwo
                   || locationType == ActivityIconManager.LocationType.ActivityOther
                   || locationType == ActivityIconManager.LocationType.Notice
                   || locationType == ActivityIconManager.LocationType.NoticeAfter
                   || locationType == ActivityIconManager.LocationType.ActivityFourth
                   || locationType == ActivityIconManager.LocationType.RightMiddle;
        }

        private static int CompareIconType(string a, string b)
        {
            MainUIConfigs.FunctionIconCfg ca = MainUIConfigs.GetFunctionIconCfg(a);
            MainUIConfigs.FunctionIconCfg cb = MainUIConfigs.GetFunctionIconCfg(b);
            if (ca == null && cb == null) return string.CompareOrdinal(a, b);
            if (ca == null) return 1;
            if (cb == null) return -1;
            int loc = ca.LocationType.CompareTo(cb.LocationType);
            return loc != 0 ? loc : ca.PosIndex.CompareTo(cb.PosIndex);
        }
    }
}
