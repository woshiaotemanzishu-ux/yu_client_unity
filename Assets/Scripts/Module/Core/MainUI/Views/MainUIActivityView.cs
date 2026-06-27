using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Activity entry view, aligned with yu_client MainUIActivityView LoadSuccess and ADD_ICON flow.
    /// </summary>
    public sealed class MainUIActivityView : MainUIActivityViewBind
    {
        private readonly Dictionary<string, ActivityIcon> _iconByType = new Dictionary<string, ActivityIcon>();
        private bool _activityFolded;
        private bool _clickBound;

        protected override void OnInit()
        {
            _ = ResManager.SetLayaTextureAsync(bg1, GameResPath.GetIcon("mainUI", "mainui_ui_38"), nativeSize: false);
            effect.sizeDelta = new Vector2(720f, 1280f);

            Vector2 redPos = _img_red.rectTransform.anchoredPosition;
            redPos.x = 120f;
            _img_red.rectTransform.anchoredPosition = redPos;

            player_name.text = "";
            name.text = "";
            time.text = "";
            _box_rank.gameObject.SetActive(false);
            _img_red.gameObject.SetActive(false);
            effect.gameObject.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_tpl_ActivityIcon != null) _tpl_ActivityIcon.SetActive(false);
            if (_tpl_TopPlayerTipItem != null) _tpl_TopPlayerTipItem.SetActive(false);
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On<string, int>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_ADD, OnActivityIconAdd);
            EventDispatcher.On<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnActivityIconDelete);
            EventDispatcher.On<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, OnActivityIconUpdate);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnOpenConditionChanged);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnOpenConditionChanged);
            RefreshActivityIconAsync();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off<string, int>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_ADD, OnActivityIconAdd);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnActivityIconDelete);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, OnActivityIconUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnOpenConditionChanged);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnOpenConditionChanged);
        }

        private void OnOpenConditionChanged()
        {
            RefreshActivityIconAsync();
        }

        private async void RefreshActivityIconAsync()
        {
            await ActivityIconManager.Instance.RefreshDefaultIconsAsync();
            if (this == null) return;
            foreach (KeyValuePair<string, ActivityIconManager.IconInfo> kv in ActivityIconManager.Instance.IconInfoByType)
            {
                if (kv.Value?.Data != null && ShouldOwnActivityIcon(kv.Value.Data.LocationType))
                {
                    CreateActivityIcon(kv.Key);
                }
            }
            RefreshIcon();
        }

        private void OnActivityIconAdd(string iconType, int locationType)
        {
            if (!ShouldOwnActivityIcon(locationType)) return;
            CreateActivityIcon(iconType);
            RefreshIcon();
        }

        private void OnActivityIconDelete(string iconType)
        {
            if (!_iconByType.TryGetValue(iconType, out ActivityIcon item)) return;
            _iconByType.Remove(iconType);
            Destroy(item.gameObject);
            RefreshIcon();
        }

        private void OnActivityIconUpdate(string iconType)
        {
            if (!_iconByType.TryGetValue(iconType, out ActivityIcon item)) return;
            item.Refresh();
            RefreshIcon();
        }

        private void CreateActivityIcon(string iconType)
        {
            if (string.IsNullOrEmpty(iconType) || iconType == "153") return;
            if (_iconByType.TryGetValue(iconType, out ActivityIcon existing))
            {
                existing.Refresh();
                return;
            }

            if (_tpl_ActivityIcon == null || _gp_con == null)
            {
                GameLog.Error("MainUI", "ActivityIcon template missing");
                return;
            }

            GameObject go = Instantiate(_tpl_ActivityIcon, _gp_con);
            go.SetActive(true);

            ActivityIcon item = go.GetComponent<ActivityIcon>();
            if (item == null)
            {
                GameLog.Error("MainUI", "ActivityIcon template is not rebound to business script");
                Destroy(go);
                return;
            }

            item.Show();
            item.SetIconType(iconType);
            item.SetVisible(false);
            _iconByType[iconType] = item;
        }

        private void RefreshIcon()
        {
            List<ActivityIcon> first = new List<ActivityIcon>();
            List<ActivityIcon> second = new List<ActivityIcon>();
            List<ActivityIcon> other = new List<ActivityIcon>();
            List<ActivityIcon> fourth = new List<ActivityIcon>();
            List<ActivityIcon> rightMiddle = new List<ActivityIcon>();

            foreach (ActivityIcon item in _iconByType.Values)
            {
                MainUIConfigs.FunctionIconCfg cfg = item.Cfg ?? MainUIConfigs.GetFunctionIconCfg(item.IconType);
                if (cfg == null) continue;
                if (cfg.LocationType == ActivityIconManager.LocationType.ActivityOne) first.Add(item);
                else if (cfg.LocationType == ActivityIconManager.LocationType.ActivityTwo) second.Add(item);
                else if (cfg.LocationType == ActivityIconManager.LocationType.ActivityOther) other.Add(item);
                else if (cfg.LocationType == ActivityIconManager.LocationType.ActivityFourth || cfg.LocationType == 10) fourth.Add(item);
                else if (cfg.LocationType == ActivityIconManager.LocationType.RightMiddle || cfg.LocationType == 9) rightMiddle.Add(item);
            }

            SortIcons(first);
            SortIcons(second);
            SortIcons(other);
            SortIcons(fourth);
            SortIcons(rightMiddle);

            if (_activityFolded)
            {
                HideAllIcons();
                if (_img_turn != null) _img_turn.transform.SetAsLastSibling();
                return;
            }

            RefreshIconPos(first, 1, null, null, null);
            RefreshIconPos(second, 2, first, null, null);
            RefreshIconPos(other, 3, first, second, null);
            RefreshIconPos(fourth, 4, first, second, other);
            RefreshIconPos(rightMiddle, 9, null, null, null);
            // 折叠箭头【固定在 prefab 里烤出的位置】(对标老端:它占第四排第一格,从不被图标布局重定位)。
            // 只保证它画在图标之上(后置兄弟),不再用 UpdateTurnBtnPos 移动它——那正是它"越点越往上跑"的根因。
            if (_img_turn != null) _img_turn.transform.SetAsLastSibling();
        }

        private void HideAllIcons()
        {
            foreach (ActivityIcon item in _iconByType.Values)
            {
                if (item == null) continue;
                item.SetVisible(false);
            }
        }

        private void BindButtons()
        {
            if (_clickBound) return;
            if (_img_turn != null)
            {
                _img_turn.raycastTarget = true; // 收放箭头要能接收点击(老端 mouseEnabled);不设则 ToggleActivityFold 永不触发
                UIUtil.AddClick(_img_turn, ToggleActivityFold);
            }
            RouteClick(_box_rank, "activity_rank");
            _clickBound = true;
        }

        private static void RouteClick(Component target, string viewKey)
        {
            if (target == null) return;
            UIUtil.AddClick(target, () => MainUIRouter.Open(viewKey));
        }

        private void ToggleActivityFold()
        {
            _activityFolded = !_activityFolded;
            // 对标老端 ShowAnimation:收起/展开「其他模块」(活动功能图标),箭头【原地旋转】不移动(老端只改 rotation)。
            foreach (ActivityIcon item in _iconByType.Values)
                if (item != null) item.SetVisible(!_activityFolded);
            if (_img_turn != null)
                _img_turn.rectTransform.localEulerAngles = new Vector3(0f, 0f, _activityFolded ? 45f : 0f);
        }

        private void RefreshIconPos(List<ActivityIcon> list, int lineNo, List<ActivityIcon> first,
            List<ActivityIcon> second, List<ActivityIcon> third)
        {
            if (list == null || list.Count == 0) return;

            const float hgap = 5f;
            const float vgap = 20f;
            float initX = 0f;
            float initY = 0f;

            if (lineNo == 2)
            {
                if (first != null && first.Count > 0) initY = ActivityIcon.HEIGHT + vgap;
            }
            else if (lineNo == 3)
            {
                if (first != null && first.Count > 0) initY = ActivityIcon.HEIGHT + vgap;
                if (second != null && second.Count > 0) initY += ActivityIcon.HEIGHT + vgap;
            }
            else if (lineNo == 4)
            {
                if (first != null && first.Count > 0) initY = ActivityIcon.HEIGHT + vgap;
                if (second != null && second.Count > 0) initY += ActivityIcon.HEIGHT + vgap;
                if (third != null && third.Count > 0) initY += ActivityIcon.HEIGHT + vgap + 10f;
            }
            else if (lineNo == 9)
            {
                initX = 648f;
                initY = 280f;
            }

            int lineMax = lineNo == 9 ? 1 : 7;
            int count = lineNo == 4 ? 2 : 1;
            for (int i = 0; i < list.Count; i++)
            {
                ActivityIcon item = list[i];
                int nowLine = Mathf.FloorToInt((count - 1) / (float)lineMax);
                float x = lineNo == 9 ? initX : ((count - 1) % lineMax) * (ActivityIcon.WIDTH + hgap);
                float y = lineNo == 9
                    ? initY + (count - 1) * (ActivityIcon.HEIGHT + vgap)
                    : initY + nowLine * (ActivityIcon.HEIGHT + vgap);

                item.SetPosition(x, y);
                item.SetAlpha(1f);
                item.SetScale(1f);
                item.SetVisible(true);
                count++;
            }
        }

        private void UpdateTurnBtnPos(List<ActivityIcon> first, List<ActivityIcon> second, List<ActivityIcon> other,
            List<ActivityIcon> fourth = null)
        {
            if (_img_turn == null) return;

            // 按【左侧网格实际最低图标】定位收放箭头:不靠分组计数(组内换行也会漏算),取所有左侧图标的最低 anchoredPos.y,
            // 把箭头放到最低一行同高、其左侧(对齐老端 y≈410)。展开态有图标才算;无图标则保持 prefab 默认位。
            float lowestY = 0f; // anchoredPosition.y 越负越低
            bool any = false;
            foreach (List<ActivityIcon> list in new[] { first, second, other, fourth })
            {
                if (list == null) continue;
                foreach (ActivityIcon item in list)
                {
                    if (item == null) continue;
                    float y = ((RectTransform)item.transform).anchoredPosition.y;
                    if (y < lowestY) lowestY = y;
                    any = true;
                }
            }
            if (!any) return; // 无受管图标(如被收起/未建)→ 不动,保留 prefab 位

            Vector2 pos = _img_turn.rectTransform.anchoredPosition;
            pos.x = 35f;
            pos.y = lowestY; // 与最低一行图标同高,在其左侧
            _img_turn.rectTransform.anchoredPosition = pos;
        }

        private static bool ShouldOwnActivityIcon(int locationType)
        {
            return locationType == ActivityIconManager.LocationType.ActivityOne
                   || locationType == ActivityIconManager.LocationType.ActivityTwo
                   || locationType == ActivityIconManager.LocationType.ActivityOther
                   || locationType == ActivityIconManager.LocationType.ActivityFourth
                   || locationType == ActivityIconManager.LocationType.RightMiddle
                   || locationType == 9
                   || locationType == 10;
        }

        private static void SortIcons(List<ActivityIcon> list)
        {
            list.Sort(CompareIcon);
        }

        private static int CompareIcon(ActivityIcon a, ActivityIcon b)
        {
            MainUIConfigs.FunctionIconCfg ca = a.Cfg ?? MainUIConfigs.GetFunctionIconCfg(a.IconType);
            MainUIConfigs.FunctionIconCfg cb = b.Cfg ?? MainUIConfigs.GetFunctionIconCfg(b.IconType);
            if (ca == null && cb == null) return 0;
            if (ca == null) return 1;
            if (cb == null) return -1;

            int loc = ca.LocationType.CompareTo(cb.LocationType);
            return loc != 0 ? loc : ca.PosIndex.CompareTo(cb.PosIndex);
        }
    }
}
