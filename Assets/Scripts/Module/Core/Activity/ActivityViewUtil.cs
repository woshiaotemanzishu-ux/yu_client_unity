using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.CustomActivity;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Activity
{
    internal static class ActivityViewUtil
    {
        public static List<CustomActivityModel.DetailReward> Ordered(CustomActivityModel.DetailData detail)
        {
            var list = detail != null
                ? new List<CustomActivityModel.DetailReward>(detail.RewardList)
                : new List<CustomActivityModel.DetailReward>();
            list.Sort((a, b) =>
            {
                int aw = a.Status == 1 ? -1 : a.Status;
                int bw = b.Status == 1 ? -1 : b.Status;
                int state = aw.CompareTo(bw);
                return state != 0 ? state : a.Grade.CompareTo(b.Grade);
            });
            return list;
        }

        public static int ConditionInt(string condition, string key, int fallback = 0)
        {
            if (string.IsNullOrEmpty(condition)) return fallback;
            string escaped = Regex.Escape(key);
            Match m = Regex.Match(
                condition,
                @"(?:\{\s*|[\""']?)" + escaped + @"[\""']?\s*(?:,|:|=)\s*(\d+)",
                RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out int value) ? value : fallback;
        }

        public static int FirstConditionInt(string condition, int fallback = 0)
        {
            if (string.IsNullOrEmpty(condition)) return fallback;
            Match m = Regex.Match(condition, @"\d{9,}");
            return m.Success && int.TryParse(m.Value, out int value) ? value : fallback;
        }

        public static string Remaining(long endTime)
        {
            long seconds = endTime - Shenxiao.Framework.Util.TimeUtil.NowSec();
            if (seconds <= 0) return "00:00:00";
            long days = seconds / 86400;
            long hours = seconds / 3600 % 24;
            long minutes = seconds / 60 % 60;
            long secs = seconds % 60;
            return days > 0
                ? string.Format("{0}天 {1:00}:{2:00}:{3:00}", days, hours, minutes, secs)
                : string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, secs);
        }

        public static void BindClick(Component target, System.Action action)
        {
            if (target != null) UIUtil.AddClick(target, action);
        }

        public static void HideAndDestroy(List<GameObject> cells)
        {
            foreach (GameObject go in cells)
            {
                if (go == null) continue;
                go.GetComponent<BaseView>()?.Hide();
                Object.Destroy(go);
            }
            cells.Clear();
        }

        public static void ResetTop(ScrollRect scroll)
        {
            if (scroll == null) return;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }

        public static void OpenRecharge()
        {
            if (MainUIRouter.IsRegistered("recharge")) MainUIRouter.Open("recharge");
            else if (MainUIRouter.IsRegistered("RechargeView")) MainUIRouter.Open("RechargeView");
            else Shenxiao.Framework.Util.GameLog.Warn("Activity", "充值入口尚未注册，保留当前活动页");
        }

        public static void OpenDaily()
        {
            if (MainUIRouter.IsRegistered("daily")) MainUIRouter.Open("daily");
            else if (MainUIRouter.IsRegistered("DailyTaskView")) MainUIRouter.Open("DailyTaskView");
            else Shenxiao.Framework.Util.GameLog.Warn("Activity", "日常入口尚未注册，保留当前活动页");
        }
    }
}
