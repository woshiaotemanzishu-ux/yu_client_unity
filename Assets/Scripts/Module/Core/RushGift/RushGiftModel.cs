using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.RushGift
{
    /// <summary>
    /// 冲级豪礼(等级礼包)数据层(对标老端 commonModel/WelfareModel.ts;服务端 pt_417/welfare)。
    /// 主线链序:task 100420(ctype 54)要求领取过 35 级冲级礼包(server data_task.erl award_lv_gift 匹配)。
    /// 41700 全量落此(SetList 清空重建);41701 领取成功后本地置 Received=2(ApplyReceived,对标老端领取即改状态,
    /// On41701 里控制器还会再发一次 41700 对标老端刷新)。
    /// </summary>
    public sealed class RushGiftModel
    {
        public static readonly RushGiftModel Instance = new RushGiftModel();
        private RushGiftModel() { }

        /// <summary>礼包状态(字段名对照 ClientProtocol.json "41700" giftbag_state 单项)。
        /// Received: 0=条件不足(未达等级)/1=可领/2=已领/4=已被领完(bag_upperlimit 限量领完)。</summary>
        public sealed class GiftVo
        {
            public int Lv;
            public int Received;
            public long EndTime;
            public int RemainNum;
        }

        public readonly List<GiftVo> List = new List<GiftVo>();
        public bool HasData { get; private set; }

        public GiftVo Get(int lv)
        {
            return List.Find(g => g.Lv == lv);
        }

        /// <summary>41700 全量(对标老端 SetScmd41700:清空重建)。</summary>
        public void SetList(List<GiftVo> list)
        {
            List.Clear();
            if (list != null) List.AddRange(list);
            HasData = true;
        }

        /// <summary>41701 领取成功后本地置已领(对标老端领取按钮点击即改 UI 状态;code==1 时调用)。</summary>
        public void ApplyReceived(int lv)
        {
            GiftVo vo = Get(lv);
            if (vo != null) vo.Received = 2;
        }

        public void Clear()
        {
            List.Clear();
            HasData = false;
        }
    }

    /// <summary>
    /// config_rush_giftbag 读取器(具名键=bag_lv 字符串;字段 bag_lv/bag_name/bag_upperlimit/bag_upperday/
    /// bag_gift_man/bag_gift_woman/limit_gift_man/limit_gift_woman/task_show)。表经 ClientConfigSync 从
    /// yu_client cdn 同步(已在 config_rush_giftbag 同步清单内)。
    /// </summary>
    public static class RushGiftConfigs
    {
        private static JObject _bag;

        public static bool IsLoaded => _bag != null;

        public static async Task EnsureLoaded()
        {
            if (_bag != null) return;
            string key = GameResPath.GetServerConfigPath("config_rush_giftbag");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("RushGift", "missing config_rush_giftbag: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _bag = new JObject();
                return;
            }
            _bag = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("RushGift", "config_rush_giftbag={0}", _bag.Count);
        }

        /// <summary>礼包名(具名键 bag_name);缺表/缺项降级 "{lv}级礼包"(标出而非臆造)。</summary>
        public static string GetName(int lv)
        {
            if (_bag?[lv.ToString()] is JObject obj)
            {
                string name = obj.Value<string>("bag_name");
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return lv + "级礼包";
        }
    }
}
