using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Runtime parity for old-client MainUISecondaryView.LoadSuccess.
    /// Keep dynamic visibility and _box_right reparenting in the View layer,
    /// not in generated Bind code or prefab pixel edits.
    /// </summary>
    public sealed class MainUISecondaryView : MainUISecondaryViewBind
    {
        private const float HGap = 5f;
        private const float VGap = 5f;
        private static readonly Vector2 LeftHidePos = new Vector2(-20f, 0f);
        private static readonly Vector2 RightHidePos = new Vector2(-50f, -40f);
        private static readonly Vector2 NoticeHidePos = new Vector2(-50f, 315f);
        private static readonly Vector2 StrengthenIconPos = new Vector2(-249f, 6f);

        private readonly Dictionary<string, ActivityIcon> _leftIcons = new Dictionary<string, ActivityIcon>();
        private readonly Dictionary<string, ActivityIcon> _rightIcons = new Dictionary<string, ActivityIcon>();
        private readonly Dictionary<string, ActivityIcon> _noticeIcons = new Dictionary<string, ActivityIcon>();

        protected override void OnInit()
        {
            // Old client hides these entries until data/events open them.
            _box_god.gameObject.SetActive(false);
            _gp_t_map.gameObject.SetActive(false);
            _box_team.gameObject.SetActive(false);

            // 二级 HUD 常显入口按钮 → 经 MainUIRouter 解耦打开对应面板(各模块 Bootstrap 注册 key)。
            // 邮件 _box_email → "email"(FriendModule.EmailView);聊天 _box_chat → "chat"(ChatParentView);红包 _box_red_packet → "redpacket"(RedPacketMainView)。
            RouteClick(_box_email, "email");
            RouteClick(_box_chat, "chat");
            RouteClick(_box_red_packet, "redpacket");
            RouteClick(_box_level_rew, "levelreward");
            RouteClick(_box_firstblood, "firstblood");

            // Old client removes _box_right from Secondary, adds it to Main layer,
            // then applies right=0 and centerY=250.
            // Laya centerY is positive downward; LayaRectMath maps that to
            // Unity anchoredPosition.y = -250 when pivot.y is 0.5.
            if (_box_right != null)
            {
                _box_right.SetParent(transform.parent, false);
                _box_right.anchorMin = new Vector2(1f, 0.5f);
                _box_right.anchorMax = new Vector2(1f, 0.5f);
                _box_right.pivot = new Vector2(1f, 0.5f);
                _box_right.anchoredPosition = new Vector2(0f, -250f);
                _box_right.gameObject.SetActive(true);
            }
        }

        /// <summary>二级 HUD 按钮(Image 或含 Image 容器)→ 经 MainUIRouter 解耦打开面板(MainUI 不直接依赖各业务模块)。</summary>
        private static void RouteClick(Component target, string viewKey)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => MainUIRouter.Open(viewKey));
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On<string, int>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_ADD, OnActivityIconAdd);
            EventDispatcher.On<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnActivityIconDelete);
            EventDispatcher.On<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, OnActivityIconUpdate);
            InitActivityIconAsync();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off<string, int>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_ADD, OnActivityIconAdd);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnActivityIconDelete);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, OnActivityIconUpdate);
        }

        private async void InitActivityIconAsync()
        {
            await ActivityIconManager.Instance.EnsureDefaultIconsAsync();
            foreach (KeyValuePair<string, ActivityIconManager.IconInfo> kv in ActivityIconManager.Instance.IconInfoByType)
            {
                int locationType = kv.Value?.Data?.LocationType ?? 0;
                if (ShouldOwnSecondaryIcon(kv.Key, locationType))
                {
                    CreateSecondaryIcon(kv.Key, locationType);
                }
            }
            RefreshAllIcons();
        }

        private void OnActivityIconAdd(string iconType, int locationType)
        {
            if (!ShouldOwnSecondaryIcon(iconType, locationType)) return;
            CreateSecondaryIcon(iconType, locationType);
            RefreshAllIcons();
        }

        private void OnActivityIconDelete(string iconType)
        {
            if (DeleteIcon(_leftIcons, iconType)) { RefreshAllIcons(); return; }
            if (DeleteIcon(_rightIcons, iconType)) { RefreshAllIcons(); return; }
            if (DeleteIcon(_noticeIcons, iconType)) RefreshAllIcons();
        }

        private void OnActivityIconUpdate(string iconType)
        {
            ActivityIcon icon = GetIcon(iconType);
            if (icon == null) return;
            icon.Refresh();
            RefreshAllIcons();
        }

        private void CreateSecondaryIcon(string iconType, int locationType)
        {
            Dictionary<string, ActivityIcon> owner = GetOwnerDictionary(locationType);
            RectTransform parent = GetOwnerParent(locationType);
            if (owner == null || parent == null) return;
            if (owner.TryGetValue(iconType, out ActivityIcon existing))
            {
                existing.Refresh();
                return;
            }

            if (_tpl_ActivityIcon == null)
            {
                GameLog.Error("MainUI", "Secondary ActivityIcon template missing");
                return;
            }

            GameObject go = Instantiate(_tpl_ActivityIcon, parent);
            go.SetActive(true);

            ActivityIcon item = go.GetComponent<ActivityIcon>();
            if (item == null)
            {
                GameLog.Error("MainUI", "Secondary ActivityIcon template is not rebound to business script");
                Destroy(go);
                return;
            }

            item.Show();
            item.SetIconType(iconType);
            item.SetVisible(false);
            owner[iconType] = item;
        }

        private void RefreshAllIcons()
        {
            RefreshIcon(_leftIcons, ActivityIconManager.LocationType.Left);
            RefreshIcon(_rightIcons, ActivityIconManager.LocationType.Right);
            RefreshIcon(_noticeIcons, ActivityIconManager.LocationType.Notice);
        }

        private void RefreshIcon(Dictionary<string, ActivityIcon> source, int locationType)
        {
            if (source == null || source.Count == 0) return;

            List<ActivityIcon> icons = new List<ActivityIcon>();
            foreach (ActivityIcon item in source.Values)
            {
                if (item != null) icons.Add(item);
            }
            SortIcons(icons);
            RefreshIconPos(icons, locationType);
        }

        private void RefreshIconPos(List<ActivityIcon> icons, int locationType)
        {
            int lineMax = GetLineMaxIconNum(locationType);
            int layoutIndex = 0;
            foreach (ActivityIcon item in icons)
            {
                if (item == null) continue;

                if (Is612Icon(item.IconType))
                {
                    Vector2 hidePos = GetHidePos(locationType);
                    item.SetPosition(hidePos.x, hidePos.y);
                    item.SetScale(0.5f);
                    item.SetAlpha(0f);
                    item.SetVisible(false);
                    continue;
                }

                Vector2 pos = GetVisiblePos(item, locationType, lineMax, layoutIndex);
                if (!IsStrengthenIcon(item.IconType))
                {
                    layoutIndex++;
                }

                item.SetPosition(pos.x, pos.y);
                item.SetScale(1f);
                item.SetAlpha(1f);
                item.SetVisible(true);
            }
        }

        private static Vector2 GetVisiblePos(ActivityIcon item, int locationType, int lineMax, int layoutIndex)
        {
            if (locationType == ActivityIconManager.LocationType.Left)
            {
                return new Vector2(
                    Mathf.Floor(layoutIndex / (float)lineMax) * (ActivityIcon.WIDTH + HGap),
                    -(layoutIndex % lineMax) * (ActivityIcon.HEIGHT + VGap) - ActivityIcon.HEIGHT);
            }

            if (locationType == ActivityIconManager.LocationType.Right)
            {
                if (IsStrengthenIcon(item.IconType)) return StrengthenIconPos;
                return new Vector2(
                    -ActivityIcon.WIDTH - Mathf.Floor(layoutIndex / (float)lineMax) * ActivityIcon.WIDTH,
                    -(layoutIndex % lineMax) * ActivityIcon.HEIGHT - ActivityIcon.HEIGHT);
            }

            return new Vector2(
                -Mathf.Floor(layoutIndex / (float)lineMax) * (ActivityIcon.WIDTH + HGap) - ActivityIcon.WIDTH,
                -(layoutIndex % lineMax) * (ActivityIcon.HEIGHT + VGap) - ActivityIcon.HEIGHT);
        }

        private static int GetLineMaxIconNum(int locationType)
        {
            if (locationType == ActivityIconManager.LocationType.Right) return 6;
            return 1;
        }

        private ActivityIcon GetIcon(string iconType)
        {
            if (_leftIcons.TryGetValue(iconType, out ActivityIcon item)) return item;
            if (_rightIcons.TryGetValue(iconType, out item)) return item;
            return _noticeIcons.TryGetValue(iconType, out item) ? item : null;
        }

        private static bool DeleteIcon(Dictionary<string, ActivityIcon> owner, string iconType)
        {
            if (owner == null || !owner.TryGetValue(iconType, out ActivityIcon item)) return false;
            owner.Remove(iconType);
            if (item != null) Destroy(item.gameObject);
            return true;
        }

        private Dictionary<string, ActivityIcon> GetOwnerDictionary(int locationType)
        {
            if (locationType == ActivityIconManager.LocationType.Left) return _leftIcons;
            if (locationType == ActivityIconManager.LocationType.Right) return _rightIcons;
            if (locationType == ActivityIconManager.LocationType.Notice) return _noticeIcons;
            return null;
        }

        private RectTransform GetOwnerParent(int locationType)
        {
            if (locationType == ActivityIconManager.LocationType.Left) return _box_left;
            if (locationType == ActivityIconManager.LocationType.Right) return _box_right;
            if (locationType == ActivityIconManager.LocationType.Notice) return _box_notice;
            return null;
        }

        private static Vector2 GetHidePos(int locationType)
        {
            if (locationType == ActivityIconManager.LocationType.Left) return LeftHidePos;
            if (locationType == ActivityIconManager.LocationType.Right) return RightHidePos;
            return NoticeHidePos;
        }

        private static bool ShouldOwnSecondaryIcon(string iconType, int locationType)
        {
            if (iconType == "158") return false;
            return locationType == ActivityIconManager.LocationType.Left
                   || locationType == ActivityIconManager.LocationType.Right
                   || locationType == ActivityIconManager.LocationType.Notice;
        }

        private static bool IsStrengthenIcon(string iconType)
        {
            return iconType == "158";
        }

        private static bool Is612Icon(string iconType)
        {
            return !string.IsNullOrEmpty(iconType) && iconType.StartsWith("612");
        }

        private static void SortIcons(List<ActivityIcon> icons)
        {
            icons.Sort(CompareIcon);
        }

        private static int CompareIcon(ActivityIcon a, ActivityIcon b)
        {
            MainUIConfigs.FunctionIconCfg ca = a.Cfg ?? MainUIConfigs.GetFunctionIconCfg(a.IconType);
            MainUIConfigs.FunctionIconCfg cb = b.Cfg ?? MainUIConfigs.GetFunctionIconCfg(b.IconType);
            if (ca == null && cb == null) return 0;
            if (ca == null) return 1;
            if (cb == null) return -1;
            return ca.PosIndex.CompareTo(cb.PosIndex);
        }
    }
}
