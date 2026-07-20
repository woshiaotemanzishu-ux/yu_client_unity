using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚姻(征友/戒指/结婚,自动循环 轮16)配置读取器——4 张地基表,均从
    /// yu_client cdn/resource/config/server/ 原样拷入(与 KfBossConfigs 同规格,数字/复合键扁平 JSON):
    ///   · config_marriage_constant(41条,数字键 id)——婚姻通用常量(开启等级/发布消耗/求婚亲密度/礼包消耗/
    ///     恩爱称号返还物品等),对标老端 MarriageModel.GetConstantData(key)。
    ///   · config_ring_star(501条,stage 1-50,复合键"stage@star")——戒指阶星属性表(upgrade_pray_num/
    ///     attr_list/marriage_attr),17210 战力自算 TODO 依赖此表(本轮未接算法,见 MarriageController 注释)。
    ///     轮16三镜头验收裁决2:拷贝源=yu_client cdn\resource\config\server(法定同步源,LayaUISettings.
    ///     CdnResourceRoot,历轮同源),实际 501 条,与服务端 data_ring.erl 501+1 兜底精确吻合。
    ///   · config_flower_tools(6条,数字键=goods_id)——鲜花道具表(intimacy/charm/fame/特效)。
    ///   · config_love_dsgt_cfg(10条,数字键=顺位id)——恩爱称号档位表(dsgt=真实称号id,love_num=解锁门槛)。
    /// **跳过 config_personal_tag_info**(60条)。轮16三镜头验收 M9 订正:该表有 3 个活视图消费
    /// (MarriageComView.ts:89/MarriageFriendItem.ts:109/MarriageIssueView.ts:102·174 经 GetTagsStr 渲染
    /// 标签文案,同属尾包依赖的还有 config_fame_lv),并非"仅死链消费"——死的只是其编辑入口 MarriageTagView
    /// (模块从未加载)。本轮数据层不消费标签文案,仍不导入该表;17200 player_list 的 tag_list 字段仍如实
    /// 解析落地;UI 尾包接线时须补导 config_personal_tag_info+config_fame_lv。
    /// </summary>
    public static class MarriageConfigs
    {
        private static JObject _constant;
        private static JObject _ringStar;
        private static JObject _flowerTools;
        private static JObject _loveDsgt;

        public static bool IsLoaded => _constant != null;

        public static async Task EnsureLoaded()
        {
            if (_constant != null) return;
            _constant = await LoadServer("config_marriage_constant");
            _ringStar = await LoadServer("config_ring_star");
            _flowerTools = await LoadServer("config_flower_tools");
            _loveDsgt = await LoadServer("config_love_dsgt_cfg");
            GameLog.Info("Marriage", "MarriageConfigs 加载: constant={0} ringStar={1} flowerTools={2} loveDsgt={3}",
                _constant.Count, _ringStar.Count, _flowerTools.Count, _loveDsgt.Count);
        }

        private static async Task<JObject> LoadServer(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Marriage", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>config_marriage_constant 单行取值(对标老端 GetConstantData(key)),constant 字段原样
        /// 透出字符串(部分本身是 JSON 数组串,如 "[{...}]",调用方按需再解析,数据层不二次解析)。</summary>
        public static string GetConstant(int key)
        {
            if (!(_constant?[key.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return ReadRaw(row, "constant");
        }

        /// <summary>config_ring_star 单行(复合键 "stage@star")。</summary>
        public sealed class RingStarRow
        {
            public int Stage;
            public int Star;
            public long UpgradePrayNum;
            public string AttrList = "[]";
            public string MarriageAttr = "[]";
        }

        public static RingStarRow GetRingStar(int stage, int star)
        {
            string compound = stage.ToString(CultureInfo.InvariantCulture) + "@" + star.ToString(CultureInfo.InvariantCulture);
            if (!(_ringStar?[compound] is JObject row)) return null;
            return new RingStarRow
            {
                Stage = stage,
                Star = star,
                UpgradePrayNum = ReadInt(row, "upgrade_pray_num"),
                AttrList = ReadRaw(row, "attr_list"),
                MarriageAttr = ReadRaw(row, "marriage_attr"),
            };
        }

        /// <summary>config_flower_tools 单行(数字键=goods_id)。</summary>
        public sealed class FlowerToolRow
        {
            public int GoodsId;
            public int Intimacy;
            public int Charm;
            public int Fame;
            public int NeedLv;
            public int NeedVip;
            public int IsSell;
            public int IsTv;
            public int EffectType;
            public string Effect = "";
        }

        public static FlowerToolRow GetFlowerTool(int goodsId)
        {
            if (!(_flowerTools?[goodsId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new FlowerToolRow
            {
                GoodsId = goodsId,
                Intimacy = ReadInt(row, "intimacy"),
                Charm = ReadInt(row, "charm"),
                Fame = ReadInt(row, "fame"),
                NeedLv = ReadInt(row, "need_lv"),
                NeedVip = ReadInt(row, "need_vip"),
                IsSell = ReadInt(row, "is_sell"),
                IsTv = ReadInt(row, "is_tv"),
                EffectType = ReadInt(row, "effect_type"),
                Effect = ReadString(row, "effect"),
            };
        }

        /// <summary>config_love_dsgt_cfg 单行(数字键=顺位 id 0-9;dsgt=真实称号 id,love_num=解锁门槛)。</summary>
        public sealed class LoveDsgtRow
        {
            public int Id;
            public int Dsgt;
            public long LoveNum;
        }

        public static LoveDsgtRow GetLoveDsgt(int id)
        {
            if (!(_loveDsgt?[id.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new LoveDsgtRow
            {
                Id = id,
                Dsgt = ReadInt(row, "dsgt"),
                LoveNum = ReadInt(row, "love_num"),
            };
        }

        public static int ConstantCount => _constant?.Count ?? 0;
        public static int RingStarCount => _ringStar?.Count ?? 0;
        public static int FlowerToolsCount => _flowerTools?.Count ?? 0;
        public static int LoveDsgtCount => _loveDsgt?.Count ?? 0;

        // ---------- JSON 读取小工具(同 BossConfigs/RankConfigs 套路,自成一份不跨模块耦合) ----------

        private static int ReadInt(JObject obj, string key)
        {
            if (obj == null) return 0;
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<double>();
            return int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static string ReadString(JObject obj, string key)
        {
            if (obj == null) return "";
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }

        private static string ReadRaw(JObject obj, string key) => ReadString(obj, key);
    }
}
