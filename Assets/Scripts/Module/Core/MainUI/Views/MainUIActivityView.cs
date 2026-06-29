using System.Collections;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.CustomActivity;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Activity entry view, aligned with yu_client MainUIActivityView LoadSuccess and ADD_ICON flow.
    /// </summary>
    public sealed class MainUIActivityView : MainUIActivityViewBind
    {
        private const string TopPlayerIconType = "331@10@0";

        private readonly Dictionary<string, ActivityIcon> _iconByType = new Dictionary<string, ActivityIcon>();
        private bool _activityFolded;
        private bool _clickBound;

        // 头号玩家榜面板(_box_rank):奖励道具 + 逐秒倒计时。
        private EquipmentItem _topPlayerReward;
        private bool _rewardBuilding;       // 奖励 item 异步实例化中(防多 rank_type 事件并发重复创建)
        private int _pendingRewardTypeId;   // 最新待应用的奖励 type_id(建好后统一 SetData)
        private Coroutine _topPlayerTimer;
        private long _topPlayerEndTimeSec;

        private static readonly Color TimeRunColor = new Color(0x88 / 255f, 1f, 0x43 / 255f); // #88ff43 绿(倒计时)
        private static readonly Color TimeEndColor = new Color(1f, 0x47 / 255f, 0x15 / 255f); // #ff4715 红(已结束/结算中)

        // 老端 MainUIActivityView.icon_dir(MainUIActivityView.ts:71-90):rank_type_(sex|career) → 头号玩家榜奖励道具 type_id。
        private static readonly Dictionary<string, int> TopPlayerRewardTypeId = new Dictionary<string, int>
        {
            { "1_1", 101046074 }, { "2_1", 101066074 }, { "3_1", 101026074 }, { "4_1", 101106074 }, { "5_1", 101086074 }, { "6_1", 101016074 },
            { "1_2", 102046074 }, { "2_2", 102066074 }, { "3_2", 102026074 }, { "4_2", 102106074 }, { "5_2", 102086074 }, { "6_2", 102016074 },
            { "6_3", 103016074 }, { "6_4", 104016074 },
            { "13_1", 101106074 }, { "13_2", 102106074 }, { "14_1", 101086074 }, { "14_2", 102086074 },
        };

        protected override void OnInit()
        {
            _ = ResManager.SetLayaTextureAsync(bg1, GameResPath.GetIcon("mainUI", "mainui_ui_38"), nativeSize: false);
            // _box_rank 面板边框(老端 _box_rank/_img_bg.skin = mainui_ui_37);保留 prefab 尺寸(154×157)只换图。
            // 用 SetImageAsync:不做 texture→other 路径重写(SetLayaTextureAsync 会重写,而 mainui_ui_37 只在 texture/ 下,重写后找不到)。
            if (_img_bg != null) _ = ResManager.SetImageAsync(_img_bg, GameResPath.GetIcon("mainUI", "mainui_ui_37"), false, false);
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
            EventDispatcher.On<int>(GlobalEvent.EVT_TOPPLAYER_MAIN_DATA, OnTopPlayerMainData);
            RefreshActivityIconAsync();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off<string, int>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_ADD, OnActivityIconAdd);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnActivityIconDelete);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, OnActivityIconUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnOpenConditionChanged);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnOpenConditionChanged);
            EventDispatcher.Off<int>(GlobalEvent.EVT_TOPPLAYER_MAIN_DATA, OnTopPlayerMainData);
            StopTopPlayerTimer();
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

        // 对标老端 UPDATE_TOP_PLAYER_MAIN_DATA / UpdateTopPlayerData:面板现身 + 奖励道具 + 榜首名/活动名 + 倒计时。
        // 头号玩家路用 2D 奖励(icon_box),隐 3D 模型(model_gp 是循环冲榜 22702 路才用,未移植)。
        private void OnTopPlayerMainData(int rankType)
        {
            if (this == null || _box_rank == null) return;
            if (!TopPlayerController.GatePasses())
            {
                _box_rank.gameObject.SetActive(false);
                StopTopPlayerTimer();
                return;
            }

            TopPlayerModel.RankInfo info = TopPlayerModel.Instance.GetRankInfo(rankType);
            if (info == null) return;

            // 面板现身:OnInit 里 _box_rank 被 SetActive(false),老端靠 ShowAnimation 切 visible,这里在拿到数据时打开。
            _box_rank.gameObject.SetActive(true);
            if (icon_box != null) icon_box.gameObject.SetActive(true);
            if (model_gp != null) model_gp.gameObject.SetActive(false); // 3D 模型路(循环冲榜)未移植,头号玩家路不用

            // 活动名 + 榜首名(无人则“榜单虚位以待”)。
            RushRankConfigs.RushRankCfg cfg = RushRankConfigs.Get(rankType);
            if (name != null && cfg != null) name.text = cfg.Name;
            if (player_name != null) player_name.text = info.FirstName() ?? "榜单虚位以待";

            // 奖励道具(对标老端 icon_dir[now_key] → EquipmentItem.SetData)。
            BuildTopPlayerReward(rankType);

            // 倒计时(对标老端 StartTimer/OnTime + open_day>7 的“已结束”终态)。
            RefreshTopPlayerCountdown(info.EndTime);

            // 注:老端 effect 节点在本端被 OnInit 设成 720×1280 全屏盒,而 _box_rank 一直隐藏从没验证过此特效;
            // 现在面板现身,贸然挂全屏 ui_cb01 会盖屏,故先不挂(待确认特效挂点/尺寸后再补)。
        }

        // 头号玩家榜奖励道具:icon_dir[now_key] → type_id → EquipmentItem.SetData(对标老端 reward = new EquipmentItem)。
        // 注:本 prefab 的 _tpl_EquipmentItem 没烤进模板(fileID:0),故直接按 key 加载 EquipmentItem.prefab 实例化(同 ItemTipsView 套路)。
        private async void BuildTopPlayerReward(int rankType)
        {
            if (icon_box == null) return;

            // now_key = rank_type==6 ? rank_type_career : rank_type_sex(老端 UpdateTopPlayerData)。
            int key2 = rankType == 6 ? RoleModel.Instance.Career : RoleModel.Instance.Sex;
            string key = rankType + "_" + key2;
            if (!TopPlayerRewardTypeId.TryGetValue(key, out int typeId)) return;
            _pendingRewardTypeId = typeId; // 记最新;建好后统一应用,避免并发事件丢更新

            if (_topPlayerReward == null && !_rewardBuilding)
            {
                _rewardBuilding = true;
                GameObject go = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "EquipmentItem"), icon_box);
                _rewardBuilding = false;
                if (this == null) { if (go != null) Destroy(go); return; }
                if (go == null) return;
                go.SetActive(true);
                _topPlayerReward = go.GetComponent<EquipmentItem>();
                if (_topPlayerReward == null) { Destroy(go); return; }
                _topPlayerReward.Show();
                ((RectTransform)_topPlayerReward.transform).anchoredPosition = Vector2.zero; // 居中于 icon_box(老端 SetItemSize(80,80))
            }
            if (_topPlayerReward != null) _topPlayerReward.SetData(_pendingRewardTypeId, 0);
        }

        // 倒计时(对标老端 OnTime):剩余>0 显示“h小时m分s秒”绿色;≤0 或开服>7 显示终态(已结束/结算中)红色。
        private void RefreshTopPlayerCountdown(long endTimeSec)
        {
            _topPlayerEndTimeSec = endTimeSec;
            StopTopPlayerTimer();

            // 老端 UpdateTopPlayerData:open_day>7 直接“已结束”,不跑计时。
            if (ServerTimeModel.GetOpenServerDay() > 7) { SetTimeText("已结束", TimeEndColor); return; }
            if (endTimeSec <= 0) { SetTimeText("", TimeRunColor); return; }
            TickTopPlayerTime();
            _topPlayerTimer = StartCoroutine(TopPlayerTimerRoutine()); // 前面 StopTopPlayerTimer 已置空,这里直接起
        }

        private IEnumerator TopPlayerTimerRoutine()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                yield return wait;
                TickTopPlayerTime();
            }
        }

        private void TickTopPlayerTime()
        {
            long left = _topPlayerEndTimeSec - TimeUtil.NowSec();
            if (left <= 0)
            {
                SetTimeText(ServerTimeModel.GetOpenServerDay() >= 8 ? "结算中" : "已结束", TimeEndColor);
                StopTopPlayerTimer();
                return;
            }
            SetTimeText(FormatRemain(left), TimeRunColor);
        }

        private void SetTimeText(string text, Color color)
        {
            if (time == null) return;
            time.text = text;
            time.color = color;
        }

        private void StopTopPlayerTimer()
        {
            if (_topPlayerTimer != null) { StopCoroutine(_topPlayerTimer); _topPlayerTimer = null; }
        }

        // 对标老端 timeConvert(left, "hh-mm-ss"):有天数 → “D天h小时m分”(丢秒,h 取模 24);否则 “h小时m分s秒”。
        private static string FormatRemain(long sec)
        {
            long d = sec / 86400;
            long h = sec / 3600 % 24;
            long m = sec / 60 % 60;
            long s = sec % 60;
            return d > 0 ? d + "天" + h + "小时" + m + "分" : h + "小时" + m + "分" + s + "秒";
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

            ActivityIcon topPlayer = null;
            foreach (ActivityIcon item in _iconByType.Values)
            {
                MainUIConfigs.FunctionIconCfg cfg = item.Cfg ?? MainUIConfigs.GetFunctionIconCfg(item.IconType);
                if (cfg == null) continue;
                // 头号玩家:不进通用桶,固定第二排第二格(对标老端 FormatIconList 的 top_player_obj 特判)。
                if (cfg.IconType == TopPlayerIconType) { topPlayer = item; continue; }
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

            // 头号玩家固定第二排第二格:插到 second 的 index 1(老端 second_arr = [secondFirst, topPlayer, ...rest])。
            if (topPlayer != null)
            {
                int insertAt = second.Count >= 1 ? 1 : second.Count;
                second.Insert(insertAt, topPlayer);
            }

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
            // 对标老端:太极是单按钮但事件驱动两个视图。广播给 MainUISecondaryView 同步收放它的 left/right/notice 簇
            // (超值礼包等就在 Secondary 簇里;本视图只管自己的 _iconByType)。
            EventDispatcher.Emit(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, _activityFolded);
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
