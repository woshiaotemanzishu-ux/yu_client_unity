using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友/邮件/私聊列表项共用小工具:头像懒加载实例化(对标各老端 *Item.ts 里 `new CustomHeadItem(this.head)`
    /// 的模式——本工程 CustomHeadItem 是独立 common prefab,非各 Friend 子 prefab 自带模板,需运行时实例化,
    /// 对标 SettingView.RefreshHeadIcon 的加载路径)。
    /// </summary>
    internal static class FriendUiUtil
    {
        /// <summary>幂等:容器下已有 CustomHeadItem 直接复用,否则实例化 common/CustomHeadItem 挂入并居中铺满。</summary>
        public static async Task<CustomHeadItem> EnsureHead(RectTransform container)
        {
            if (container == null) return null;
            CustomHeadItem existing = container.GetComponentInChildren<CustomHeadItem>(true);
            if (existing != null) return existing;

            GameObject go = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "CustomHeadItem"), container);
            if (go == null) return null;
            go.name = "CustomHeadItem";
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            CustomHeadItem item = go.GetComponent<CustomHeadItem>();
            if (item != null)
            {
                item.gameObject.SetActive(true);
                item.Show();
                item.SetActiveFrame(false);
            }
            return item;
        }

        /// <summary>离线时长文案(对标老端 "(离线 xx前)"):秒数转粗粒度中文时长。</summary>
        public static string FormatOfflineDuration(int seconds)
        {
            if (seconds < 60) return seconds + "秒";
            if (seconds < 3600) return (seconds / 60) + "分钟";
            if (seconds < 86400) return (seconds / 3600) + "小时";
            return (seconds / 86400) + "天";
        }

        /// <summary>邮件时间文案(对标老端 TimeUtil.timeConversion(time,"yyyy-mm-dd hh:MM:ss"))。</summary>
        public static string FormatDateTime(int epochSec)
        {
            if (epochSec <= 0) return "";
            System.DateTime local = System.DateTimeOffset.FromUnixTimeSeconds(epochSec).LocalDateTime;
            return local.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>私聊气泡时间文案(对标老端 ChatModel.timeFormat,简化为 HH:mm)。</summary>
        public static string FormatChatTime(uint epochSec)
        {
            if (epochSec == 0) return "";
            System.DateTime local = System.DateTimeOffset.FromUnixTimeSeconds(epochSec).LocalDateTime;
            return local.ToString("HH:mm");
        }
    }
}
