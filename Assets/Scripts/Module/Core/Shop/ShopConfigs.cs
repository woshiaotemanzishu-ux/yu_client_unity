using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 商店配置读取器(自动循环 轮11):
    ///   · ClientShopConfig.json(client)   —— ShopSeries[shop_type]=[{id,desc}] 二级子页签定义(仅灵玉/善缘两类型有)
    ///   · config_shop.json(server,241条)  —— 服务端权威商品表(15301 goods_list 本质是它按 shop_type 过滤后下发,
    ///     Unity 端 15301 已直接拿到过滤后的数据,本表本轮无直接消费点,仅同步登记,供以后至尊VIP/喇叭跳转复用)
    ///   · config_limit_shop_config.json(server,42条) —— 抢购(64000/64001)静态数据,GetVieData(id) 用
    ///   · config_mystery_shop_good.json(server,352条) —— 神秘/神纹商店格子静态配置,GetMysteryGoodCfg(cfgId) 用
    ///   · config_mystery_shop_hit.json(server,7条)    —— 按已刷新次数区间决定刷新消耗,GetRefreshCfg(times,type) 用
    ///   · config_quick_buy_price.json(server,35条)    —— QuickBuyView 速购单价表(UI 未接壳,仅登记+基础访问器)
    ///   · config_guild_prestige.json(server,11条)     —— 结社头衔购买条件文案(guild_title condition 用)
    /// 均从 yu_client cdn/resource/config/{client,server}/ 原样拷入 Assets/GameRes/resource/config/{client,server}/
    /// (与既有 DailyConfigs/PartnerConfigs 同规格——具名键 JSON,不需 ErlangParser 解外层,内层部分字段值本身
    /// 是 Erlang term 字符串,如 mystery good 的 "goods"/"old_price"/"discount"、guild_prestige 的 "rewards"、
    /// mystery hit 的 "cost",各自消费点按需用 <see cref="Shenxiao.Framework.Net.ErlangParser"/> 再解一层)。
    /// ConfigNotNormalGoods(client)/config_goods(server) 已在 ClientConfigSync 白名单登记多轮,本类不重复加载,
    /// 直接复用 <see cref="Shenxiao.Module.Core.Common.GoodsModel"/>。
    /// </summary>
    public static class ShopConfigs
    {
        private static JObject _clientShopConfig;
        private static JObject _shop;
        private static JObject _limitShopConfig;
        private static JObject _mysteryShopGood;
        private static JObject _mysteryShopHit;
        private static JObject _quickBuyPrice;
        private static JObject _guildPrestige;

        public static bool IsLoaded => _clientShopConfig != null;

        public static async Task EnsureLoaded()
        {
            if (_clientShopConfig != null) return;
            _clientShopConfig = await LoadClient("ClientShopConfig");
            _shop = await LoadServer("config_shop");
            _limitShopConfig = await LoadServer("config_limit_shop_config");
            _mysteryShopGood = await LoadServer("config_mystery_shop_good");
            _mysteryShopHit = await LoadServer("config_mystery_shop_hit");
            _quickBuyPrice = await LoadServer("config_quick_buy_price");
            _guildPrestige = await LoadServer("config_guild_prestige");
            GameLog.Info("Shop", "ShopConfigs 加载: shopSeries={0} shop={1} vie={2} mysteryGood={3} mysteryHit={4} quickBuy={5} guildPrestige={6}",
                (_clientShopConfig["ShopSeries"] as JObject)?.Count ?? 0, _shop.Count, _limitShopConfig.Count,
                _mysteryShopGood.Count, _mysteryShopHit.Count, _quickBuyPrice.Count, _guildPrestige.Count);
        }

        private static async Task<JObject> LoadClient(string cfg)
        {
            string key = GameResPath.GetClientConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Shop", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        private static async Task<JObject> LoadServer(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Shop", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>ClientShopConfig.ShopSeries[shop_type](对标 ShopView.ts 的 ClientShopConfig.ShopSeries[vo.shop_type]);
        /// 目前只有 shop_type=2(灵玉)/16(善缘) 配了子系列,其余类型恒返回空表(单页平铺)。</summary>
        public static List<(int id, string desc)> GetShopSeries(int shopType)
        {
            var result = new List<(int, string)>();
            JObject series = _clientShopConfig?["ShopSeries"] as JObject;
            if (series?[shopType.ToString()] is Newtonsoft.Json.Linq.JArray arr)
            {
                foreach (Newtonsoft.Json.Linq.JToken t in arr)
                    if (t is JObject o) result.Add((ReadInt(o, "id"), ReadString(o, "desc")));
            }
            return result;
        }

        /// <summary>抢购(64000/64001)静态配置行(对标 ShopModel.ts GetVieData(id))。</summary>
        public static JObject GetVieData(int id) => _limitShopConfig?[id.ToString()] as JObject;

        /// <summary>神秘/神纹商店格子静态配置(对标 GetMysteryShopCfg(cfg_id))。</summary>
        public static JObject GetMysteryGoodCfg(int cfgId) => _mysteryShopGood?[cfgId.ToString()] as JObject;

        /// <summary>按已刷新次数区间(times+1 落在 [min,max])+ type 找刷新消耗配置(对标 GetRefreshCfg)。</summary>
        public static JObject GetRefreshCfg(int times, int type)
        {
            if (_mysteryShopHit == null) return null;
            foreach (Newtonsoft.Json.Linq.JProperty prop in _mysteryShopHit.Properties())
            {
                if (!(prop.Value is JObject o)) continue;
                if (ReadInt(o, "type") != type) continue;
                int next = times + 1;
                if (next >= ReadInt(o, "min") && next <= ReadInt(o, "max")) return o;
            }
            return null;
        }

        /// <summary>结社头衔购买条件配置(对标 guild_title condition 分支用的 config_guild_prestige[title_id])。</summary>
        public static JObject GetGuildPrestige(int titleId) => _guildPrestige?[titleId.ToString()] as JObject;

        /// <summary>QuickBuyView 速购单价表(UI 未接壳,本轮仅提供访问器供以后接线)。</summary>
        public static JObject GetQuickBuyPrice(int goodsTypeId) => _quickBuyPrice?[goodsTypeId.ToString()] as JObject;

        /// <summary>config_shop.json 原始行(本轮无直接消费点,留作至尊VIP商城/聊天喇叭跳转未来复用)。</summary>
        public static JObject GetShopCfgRow(int keyId) => _shop?[keyId.ToString()] as JObject;

        // ---------- JSON 读取小工具(数字索引/字符串键混排容错,同 DailyConfigs/PartnerConfigs 套路) ----------

        public static int ReadInt(JObject obj, string key)
        {
            if (obj == null) return 0;
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<double>();
            return int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        public static string ReadString(JObject obj, string key)
        {
            if (obj == null) return "";
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }
    }
}
