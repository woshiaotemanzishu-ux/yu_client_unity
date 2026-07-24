using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 活动图标网格视图(对标 yu_client MainUIActivityView 的 ADD_ICON 流)—— 【分组槽位式】。
    ///
    /// 布局 100% 归 prefab、代码绝不算坐标:_gp_con 下由**美术手摆一批空槽位**(想摆几排/每排几个/摆哪都行),
    /// 本类只做一件事——按配置的 location_type/pos_index 把图标填进对应逻辑组的槽位，图标继承槽的位置。
    /// 逻辑组仍对标老端 ActivityOne/Two/Other/Fourth；但它们只是同一个 HudActivity prefab 里的子层级，
    /// 不再拆成 HudNotice/HudSecondary 等多个屏幕模块。坐标、间距、尺寸和第四组缩进仍全部归 prefab。
    /// location_type=6/7(老端 _box_notice 及其后续位)并入 Other 组显示，配置/服务器仍决定图标是否存在。
    /// location_type=4/5 分别填进 HudActivityLeft/HudActivityRight 的空槽；它们只是视觉区域拆分，
    /// 图标增删、开关、排序、倒计时、红点与模板克隆仍全部由本类统一处理。
    ///
    /// 右上「竞榜/头号玩家榜」卡片已拆到 <see cref="MainUIRankView"/>;折叠太极已提到 <see cref="MainUIFoldView"/>(总装层)。
    /// </summary>
    public sealed class MainUIActivityView : MainUIActivityViewBind
    {
        private const string GroupOneName = "Group_ActivityOne";
        private const string GroupTwoName = "Group_ActivityTwo";
        private const string GroupOtherName = "Group_ActivityOther";
        private const string GroupFourthName = "Group_ActivityFourth";
        private const string SlotPrefix = "Slot_";

        private readonly Dictionary<string, ActivityIcon> _iconByType = new Dictionary<string, ActivityIcon>();
        private bool _activityFolded;

        private sealed class IconGroups
        {
            public readonly List<string> One = new List<string>();
            public readonly List<string> Two = new List<string>();
            public readonly List<string> Other = new List<string>();
            public readonly List<string> Fourth = new List<string>();
            public readonly List<string> Left = new List<string>();
            public readonly List<string> Right = new List<string>();

            public List<string> Flatten()
            {
                var result = new List<string>(One.Count + Two.Count + Other.Count + Fourth.Count + Left.Count + Right.Count);
                result.AddRange(One);
                result.AddRange(Two);
                result.AddRange(Other);
                result.AddRange(Fourth);
                result.AddRange(Left);
                result.AddRange(Right);
                return result;
            }
        }

        private struct SlotAssignment
        {
            public string IconType;
            public RectTransform Slot;
        }

        protected override void OnInit()
        {
            if (_tpl_ActivityIcon != null) _tpl_ActivityIcon.SetActive(false);
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
            if (_gp_side_left != null) _gp_side_left.gameObject.SetActive(!folded);
            if (_gp_side_right != null) _gp_side_right.gameObject.SetActive(!folded);
            if (!folded) RefreshSlotsAsync();
        }

        private async void RefreshSlotsAsync()
        {
            await ActivityIconManager.Instance.RefreshDefaultIconsAsync();
            if (this == null || _activityFolded) return;
            FillSlots();
        }

        /// <summary>把本视图该显示的图标填进 _gp_con 下的分组槽位(槽位位置由 prefab 决定,代码不算坐标)。</summary>
        private void FillSlots()
        {
            if (_gp_con == null) return;

            IconGroups groups = CollectOwnedIconGroups();
            List<RectTransform> oneSlots = FindGroupSlots(GroupOneName);
            List<RectTransform> twoSlots = FindGroupSlots(GroupTwoName);
            List<RectTransform> otherSlots = FindGroupSlots(GroupOtherName);
            List<RectTransform> fourthSlots = FindGroupSlots(GroupFourthName);
            List<RectTransform> leftSlots = FindExternalSlots(_gp_side_left);
            List<RectTransform> rightSlots = FindExternalSlots(_gp_side_right);

            // 兼容尚未重建的旧 HudActivity:旧 prefab 只有 IconGrid/Slot_0..N，仍可按旧平铺方式显示；
            // 用户重建一次 HudActivity 后自动切到分组槽位，不会出现“代码已更新但预制体一片空”的中间态。
            bool hasGroupedSlots = oneSlots.Count + twoSlots.Count + otherSlots.Count + fourthSlots.Count > 0;
            var assignments = new List<SlotAssignment>();
            var allSlots = new List<RectTransform>();
            if (hasGroupedSlots)
            {
                // 对标老端 FormatIconList:第一组超过本排容量时溢到第二组末尾，第二组再溢到 Other 组末尾。
                SpillOverflow(groups.One, oneSlots.Count, groups.Two);
                SpillOverflow(groups.Two, twoSlots.Count, groups.Other);

                AddAssignments(groups.One, oneSlots, assignments);
                AddAssignments(groups.Two, twoSlots, assignments);
                AddAssignments(groups.Other, otherSlots, assignments);
                AddAssignments(groups.Fourth, fourthSlots, assignments);
                AddAssignments(groups.Left, leftSlots, assignments);
                AddAssignments(groups.Right, rightSlots, assignments);
                allSlots.AddRange(oneSlots);
                allSlots.AddRange(twoSlots);
                allSlots.AddRange(otherSlots);
                allSlots.AddRange(fourthSlots);
                allSlots.AddRange(leftSlots);
                allSlots.AddRange(rightSlots);

                WarnOverflow(GroupOtherName, groups.Other.Count, otherSlots.Count);
                WarnOverflow(GroupFourthName, groups.Fourth.Count, fourthSlots.Count);
                WarnOverflow("HudActivityLeft", groups.Left.Count, leftSlots.Count);
                WarnOverflow("HudActivityRight", groups.Right.Count, rightSlots.Count);
            }
            else
            {
                CollectSlotsRecursive(_gp_con, allSlots);
                AddAssignments(groups.Flatten(), allSlots, assignments);
                WarnOverflow("LegacyFlat", groups.Flatten().Count, allSlots.Count);
            }

            // 释放不再显示的图标(超出槽位容量的、或已关闭的活动)。
            var shownSet = new HashSet<string>();
            for (int i = 0; i < assignments.Count; i++) shownSet.Add(assignments[i].IconType);
            var toRemove = new List<string>();
            foreach (KeyValuePair<string, ActivityIcon> kv in _iconByType)
                if (!shownSet.Contains(kv.Key)) toRemove.Add(kv.Key);
            for (int i = 0; i < toRemove.Count; i++)
            {
                if (_iconByType[toRemove[i]] != null) Destroy(_iconByType[toRemove[i]].gameObject);
                _iconByType.Remove(toRemove[i]);
            }

            // 先隐藏所有槽，再只激活已有图标的槽；组之间不会因某组数量不足而互相抢槽/改变换行。
            for (int i = 0; i < allSlots.Count; i++)
            {
                if (allSlots[i] != null) allSlots[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < assignments.Count; i++)
            {
                SlotAssignment assignment = assignments[i];
                ActivityIcon icon = GetOrCreateIcon(assignment.IconType);
                if (icon == null || assignment.Slot == null) continue;
                PlaceIconInSlot(icon, assignment.Slot);
                assignment.Slot.gameObject.SetActive(true);
            }
        }

        // 本视图认领所有活动区图标(含 loc6/7 通知位)，但保留老端逻辑分组；这些组都显示在同一个 HudActivity。
        private IconGroups CollectOwnedIconGroups()
        {
            var groups = new IconGroups();
            foreach (KeyValuePair<string, ActivityIconManager.IconInfo> kv in ActivityIconManager.Instance.IconInfoByType)
            {
                if (kv.Value?.Data == null) continue;
                int location = kv.Value.Data.LocationType;
                if (location == ActivityIconManager.LocationType.ActivityOne) groups.One.Add(kv.Key);
                else if (location == ActivityIconManager.LocationType.ActivityTwo) groups.Two.Add(kv.Key);
                else if (location == ActivityIconManager.LocationType.ActivityFourth) groups.Fourth.Add(kv.Key);
                else if (location == ActivityIconManager.LocationType.Left) groups.Left.Add(kv.Key);
                else if (location == ActivityIconManager.LocationType.Right) groups.Right.Add(kv.Key);
                else if (location == ActivityIconManager.LocationType.ActivityOther
                         || location == ActivityIconManager.LocationType.Notice
                         || location == ActivityIconManager.LocationType.NoticeAfter
                         || location == ActivityIconManager.LocationType.RightMiddle)
                    groups.Other.Add(kv.Key);
            }
            groups.One.Sort(CompareIconType);
            groups.Two.Sort(CompareIconType);
            groups.Other.Sort(CompareIconType);
            groups.Fourth.Sort(CompareIconType);
            groups.Left.Sort(CompareIconType);
            groups.Right.Sort(CompareIconType);
            return groups;
        }

        private List<RectTransform> FindGroupSlots(string groupName)
        {
            var result = new List<RectTransform>();
            if (_gp_con == null) return result;
            Transform group = _gp_con.Find(groupName);
            if (group != null) CollectSlotsRecursive(group, result);
            return result;
        }

        private static List<RectTransform> FindExternalSlots(RectTransform root)
        {
            var result = new List<RectTransform>();
            CollectSlotsRecursive(root, result);
            return result;
        }

        private static void CollectSlotsRecursive(Transform root, List<RectTransform> result)
        {
            if (root == null) return;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name.StartsWith(SlotPrefix) && child is RectTransform slot)
                    result.Add(slot);
                else
                    CollectSlotsRecursive(child, result);
            }
        }

        private static void SpillOverflow(List<string> source, int capacity, List<string> destination)
        {
            if (capacity < 0) capacity = 0;
            if (source.Count <= capacity) return;
            int overflowCount = source.Count - capacity;
            destination.AddRange(source.GetRange(capacity, overflowCount));
            source.RemoveRange(capacity, overflowCount);
        }

        private static void AddAssignments(List<string> iconTypes, List<RectTransform> slots, List<SlotAssignment> result)
        {
            int count = Mathf.Min(iconTypes.Count, slots.Count);
            for (int i = 0; i < count; i++)
                result.Add(new SlotAssignment { IconType = iconTypes[i], Slot = slots[i] });
        }

        private static void WarnOverflow(string groupName, int iconCount, int slotCount)
        {
            if (iconCount <= slotCount) return;
            GameLog.Warn("MainUI", "活动组 {0}:图标 {1} 个 > 槽位 {2} 个,超出的 {3} 个未显示(在 HudActivity prefab 对应 Group 下增加槽位)",
                groupName, iconCount, slotCount, iconCount - slotCount);
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
