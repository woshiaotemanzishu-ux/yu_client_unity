using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Guild;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Runtime parity for old-client MainUISecondaryView.LoadSuccess.
    /// 「通知位」图标簇(老端 _box_notice,location_type=6)已统一交给 <see cref="MainUIActivityView"/> 显示,
    /// 本视图只剩 left/right 两簇——现同样改【槽位式】:
    /// 布局 100% 归 prefab,_box_left/_box_right 下由美术手摆空槽位,本类只按序把图标填进槽,代码不算坐标;
    /// _box_right 的最终位置也已直接烤在 prefab(HudSecondary 根下右缘 centerY-250),不再运行时 reparent/搬家。
    /// </summary>
    public sealed class MainUISecondaryView : MainUISecondaryViewBind
    {
        private readonly Dictionary<string, ActivityIcon> _leftIcons = new Dictionary<string, ActivityIcon>();
        private readonly Dictionary<string, ActivityIcon> _rightIcons = new Dictionary<string, ActivityIcon>();
        // 太极收起态(对标老端 SecondaryView 也消费 CHANGE_ACTIVITY_STATE):折叠时两簇容器整体收起,
        // 且周期刷新不得把它们弹回(见 RefreshSlotsAsync 守卫)。
        private bool _activityFolded;

        protected override void OnInit()
        {
            // Old client hides these entries until data/events open them.
            _box_god.gameObject.SetActive(false);
            _gp_t_map.gameObject.SetActive(false);
            _box_team.gameObject.SetActive(false);

            // TODO(经验丸/挂机经验胶囊): 对标老端 MainUISecondaryView.RefOutlineExp。三条件同时满足
            // 才显示 _box_outline_exp:OnHookModel.exp_effect>0(有挂机经验数据) &&
            // SceneManager.IsFieldScene()(野外场景) &&
            // task_model.newest_finish_task_id >= TaskModel.AfkReceiveTimesTaskId(已完成领取离线经验任务);
            // 不满足时 _box_outline_exp 与 _box_old_outline_exp 都应隐藏(两者互斥,新号/主城初期都不显示)。
            // OnHookModel/TaskModel 移植到位后,在此按上述条件订阅刷新并点亮对应的丸子。
            // 兜底:数据未接入前默认全部隐藏,防止(不管 prefab 是否已按此重新生成)新号一进城就露出
            // 占位的"0经验/分"文本。
            _box_outline_exp.gameObject.SetActive(false);
            _box_old_outline_exp.gameObject.SetActive(false);

            // 二级 HUD 常显入口按钮 → 经 MainUIRouter 解耦打开对应面板(各模块 Bootstrap 注册 key)。
            // 邮件 _box_email → "email"(FriendModule.EmailView);聊天 _box_chat → "chat"(ChatParentView);红包 _box_red_packet → "redpacket"(RedPacketMainView)。
            RouteClick(_box_email, "email");
            RouteClick(_box_chat, "chat");
            RouteClick(_box_red_packet, "redpacket");
            RouteClick(_box_level_rew, "levelreward");
            RouteClick(_box_firstblood, "firstblood");
            RouteClick(_box_daily_find, "dailyfind");
            RouteClick(_box_help, "guildhelp");
            RouteClick(_box_sea, "brightsea");
            RouteClick(_box_team, "team_invite");
            RouteClick(_box_gift_push, "pushgift");
            RouteClick(_box_outline_exp, "onhook");
            RouteClick(_box_exp_btn, "onhook");
            RouteClick(_box_old_outline_exp, "onhook");
            RouteClick(_img_add, "onhook_addition");
            RouteClick(_box_please, "marriage_gift_tips");
            RouteClick(_box_god, "232");
            RouteClick(_img_rpr, "redpacket_rain");
            RouteClick(_img_tt_record, "tt_record");

            // _box_right 最终位置已直接烤在 prefab(HudSecondary 根下右缘 centerY-250),不再运行时搬家/改锚——
            // 注意它在 prefab 里是 MainUISecondaryView 的兄弟节点,Bind 字段照常引用。
            ClearDesignTimeSampleIcons();
        }

        /// <summary>清掉 prefab 里为“设计期可视化”塞进各槽的样例图标(编辑器可见、便于摆槽位;运行时清掉换真图标)。</summary>
        private void ClearDesignTimeSampleIcons()
        {
            ClearSampleIcons(_box_left);
            ClearSampleIcons(_box_right);
        }

        private static void ClearSampleIcons(RectTransform container)
        {
            if (container == null) return;
            for (int s = 0; s < container.childCount; s++)
            {
                Transform slot = container.GetChild(s);
                for (int i = slot.childCount - 1; i >= 0; i--)
                {
                    GameObject c = slot.GetChild(i).gameObject;
                    c.SetActive(false);
                    Destroy(c);
                }
            }
        }

        /// <summary>二级 HUD 按钮(Image 或含 Image 容器)→ 经 MainUIRouter 解耦打开面板(MainUI 不直接依赖各业务模块)。</summary>
        private static void RouteClick(Component target, string viewKey)
        {
            if (target == null) return;
            UIUtil.AddClick(target, () => MainUIRouter.Open(viewKey));
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On<string, int>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_ADD, OnActivityIconAdd);
            EventDispatcher.On<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnActivityIconDelete);
            EventDispatcher.On<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, OnActivityIconUpdate);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnOpenConditionChanged);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnOpenConditionChanged);
            EventDispatcher.On<bool>(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, OnActivityFold);
            // 对标老端 MainUISecondaryView.ts:777-781(DAY_CHANGE)+:784-790(REFRESH_SERVER_TIME,一次性)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnServerTimeRefreshOnce);
            RefreshSlotsAsync();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off<string, int>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_ADD, OnActivityIconAdd);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, OnActivityIconDelete);
            EventDispatcher.Off<string>(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, OnActivityIconUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnOpenConditionChanged);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnOpenConditionChanged);
            EventDispatcher.Off<bool>(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, OnActivityFold);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            // 老端为一次性订阅(用完即解绑),但视图可能在其触发前就被关闭——这里兜底注销,防止 Off 未配对残留。
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnServerTimeRefreshOnce);
        }

        // 增/删任一活动图标 → 两簇按顺序整体重填槽位(槽位式无增量,全量重填最简单稳妥,两簇常年个位数开销可忽略)。
        private void OnActivityIconAdd(string iconType, int locationType) => RefreshSlotsAsync();
        private void OnActivityIconDelete(string iconType) => RefreshSlotsAsync();
        private void OnOpenConditionChanged() => RefreshSlotsAsync();

        // 图标内容更新(红点/倒计时等):只刷该图标自身,不动槽位排布。
        private void OnActivityIconUpdate(string iconType)
        {
            ActivityIcon icon = GetIcon(iconType);
            if (icon != null) icon.Refresh();
        }

        /// <summary>太极收起/展开(全局事件,由 MainUIFoldView 广播):容器级收放 left/right 两簇;展开时补填一次
        /// (对标老端 SecondaryView.ShowAnimation,容器整体收放)。</summary>
        private void OnActivityFold(bool folded)
        {
            _activityFolded = folded;
            if (_box_left != null) _box_left.gameObject.SetActive(!folded);
            if (_box_right != null) _box_right.gameObject.SetActive(!folded);
            if (!folded) RefreshSlotsAsync();
        }

        // 对标老端 IsActiveGuildHelp(MainUISecondaryView.ts:920-922)=
        // GuildModel.IsCanOpenAssist() && !SceneManager.NotShowHelpScene()。
        // 后半场景门(NotShowHelpScene 依赖 IsSeaHegemonyScene/IsNoonPartyScene/IsSeaAsserScene/
        // IsHolyBattleFightScene/IsHolyBattleWaitScene/IsPolarDungeon/IsKFPolarDungeon,SceneManager.ts:2092-2096)
        // Unity 的 Scene/SceneManager.cs 尚无对应方法(该文件不在本包所有权内,不可越权新增),
        // 本端先只接 IsCanOpenAssist 半(公会成员+等级+开服天门槛,数据已全通,非猜测);
        // 场景门缺口(几个特殊玩法场景内本该隐藏而未隐藏)留 todos_left。
        // IsCanOpenAssist(GuildModel.ts:1012-1022): IsHasGuild()(guild_id>0) && role_lv>=kv[26] && cur_day>=kv[28]。
        private static bool IsActiveGuildHelp()
        {
            if (RoleModel.Instance.GuildId <= 0) return false;
            int.TryParse(GuildConfigs.GetKv(26), out int condLv);
            int.TryParse(GuildConfigs.GetKv(28), out int openDay);
            return RoleModel.Instance.Level >= condLv && ServerTimeModel.GetOpenServerDay() >= openDay;
        }

        // 对标老端 MainUISecondaryView.ts:777-781:跨天后复判 _box_help 显隐。
        private async void OnServerDayChange()
        {
            await GuildConfigs.EnsureLoaded();
            if (this == null || _box_help == null) return; // view destroyed during await
            _box_help.gameObject.SetActive(IsActiveGuildHelp());
        }

        // 对标老端 MainUISecondaryView.ts:784-790:一次性订阅(拿到首次服务器时间后判一次即解绑)。
        private async void OnServerTimeRefreshOnce()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnServerTimeRefreshOnce);
            await GuildConfigs.EnsureLoaded();
            if (this == null || _box_help == null) return; // view destroyed during await
            _box_help.gameObject.SetActive(IsActiveGuildHelp());
        }

        private async void RefreshSlotsAsync()
        {
            await ActivityIconManager.Instance.RefreshDefaultIconsAsync();
            if (this == null || _activityFolded) return;
            FillSlots(_box_left, _leftIcons, ActivityIconManager.LocationType.Left);
            FillSlots(_box_right, _rightIcons, ActivityIconManager.LocationType.Right);
        }

        /// <summary>把该簇该显示的图标按顺序填进容器下的槽位(槽位位置由 prefab 决定,代码不算坐标)。</summary>
        private void FillSlots(RectTransform container, Dictionary<string, ActivityIcon> iconByType, int locationType)
        {
            if (container == null) return;

            List<string> types = CollectOwnedIconTypes(locationType);
            int slotCount = container.childCount;
            int shown = Mathf.Min(slotCount, types.Count);

            // 释放不再显示的图标(超出槽位容量的、或已关闭的活动)。
            var shownSet = new HashSet<string>();
            for (int i = 0; i < shown; i++) shownSet.Add(types[i]);
            var toRemove = new List<string>();
            foreach (KeyValuePair<string, ActivityIcon> kv in iconByType)
                if (!shownSet.Contains(kv.Key)) toRemove.Add(kv.Key);
            for (int i = 0; i < toRemove.Count; i++)
            {
                if (iconByType[toRemove[i]] != null) Destroy(iconByType[toRemove[i]].gameObject);
                iconByType.Remove(toRemove[i]);
            }

            // 逐槽填:前 shown 个槽放图标,其余槽隐藏。
            for (int i = 0; i < slotCount; i++)
            {
                RectTransform slot = container.GetChild(i) as RectTransform;
                if (slot == null) continue;
                if (i < shown)
                {
                    ActivityIcon icon = GetOrCreateIcon(iconByType, container, types[i]);
                    if (icon != null) PlaceIconInSlot(icon, slot);
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.gameObject.SetActive(false); // 空槽隐藏
                }
            }

            if (types.Count > slotCount)
                GameLog.Warn("MainUI", "{0}簇图标 {1} 个 > 槽位 {2} 个,超出的 {3} 个未显示(去 HudSecondary.prefab 里对应容器下多摆几个槽)",
                    locationType == ActivityIconManager.LocationType.Left ? "左" : "右",
                    types.Count, slotCount, types.Count - slotCount);
        }

        // 该簇该认领的图标类型(左簇 loc4 / 右簇 loc5),按 pos_index 稳定排序作为填充顺序。
        // 158(变强)是 HudChat 的固定入口，不由 HudSecondary 重复实例化。
        private List<string> CollectOwnedIconTypes(int locationType)
        {
            var list = new List<string>();
            foreach (KeyValuePair<string, ActivityIconManager.IconInfo> kv in ActivityIconManager.Instance.IconInfoByType)
            {
                if (kv.Value?.Data == null) continue;
                if (kv.Value.Data.LocationType != locationType) continue;
                if (kv.Key == "158") continue;
                list.Add(kv.Key);
            }
            list.Sort(CompareIconType);
            return list;
        }

        private ActivityIcon GetOrCreateIcon(Dictionary<string, ActivityIcon> iconByType, RectTransform container, string iconType)
        {
            if (iconByType.TryGetValue(iconType, out ActivityIcon existing) && existing != null)
            {
                existing.Refresh();
                return existing;
            }
            if (_tpl_ActivityIcon == null)
            {
                GameLog.Error("MainUI", "Secondary ActivityIcon template missing");
                return null;
            }

            GameObject go = Instantiate(_tpl_ActivityIcon, container); // 临时父,PlaceIconInSlot 会移进对应槽
            go.SetActive(true);
            ActivityIcon item = go.GetComponent<ActivityIcon>();
            if (item == null)
            {
                GameLog.Error("MainUI", "Secondary ActivityIcon template is not rebound to business script");
                Destroy(go);
                return null;
            }
            item.Show();
            item.SetIconType(iconType);
            item.SetVisible(true);
            iconByType[iconType] = item;
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

        private ActivityIcon GetIcon(string iconType)
        {
            if (_leftIcons.TryGetValue(iconType, out ActivityIcon item)) return item;
            return _rightIcons.TryGetValue(iconType, out item) ? item : null;
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
