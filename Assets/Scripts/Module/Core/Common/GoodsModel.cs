using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 物品基础数据访问器(对标老客户端 commonModel/GoodsModel.ts 的 GetGoodsBasicByTypeId / GetGoodsName /
    /// GetGoodsIcon / GetMappingTypeId)。表由 ClientConfigSync 从 yu_client cdn/resource/config/server/
    /// config_goods.json 同步进 Assets/GameRes/resource/config/server/config_goods.json(地址
    /// resource/config/server/config_goods)。
    ///
    /// 字段为【数字索引键】(同 config_task,非 config_npc 的具名键)。键序以权威 schema 为准:
    /// yu_client cdn/resource/config/server/config_table_default.json 的 config_goods 字段名列表(下标=键,共 38 字段):
    ///   · "0"  = type_id(主键)
    ///   · "1"  = goods_name(物品名,对标 GoodsModel.ts GetGoodsName 的 cfg.goods_name)
    ///   · "9"  = type(物品大类)、"10" = subtype(子类)—— 【不是】图标/品质!
    ///   · "14" = goods_icon(图标资源 id → <see cref="GameResPath.GetGoodsIconPath"/>,对标 cfg.goods_icon)
    ///   · "18" = color(品质/颜色 0..8 → com_goods_plate_{color},对标 cfg.color)
    /// 真实样本校验(键 14/键 18):101011010=初樱轻剑(icon 1010101,color 1)、102011010=初樱仙剑(icon 1020101,color 1)。
    /// 注:第 4 轮曾误把 "9"/"10" 当 icon/color,实取到的是 type/subtype(如 10/1),拼出 10.png 等不存在的 key →
    /// 图标恒降级隐藏;现已订正为 14/18(yu_client/cdn/resource/game/goodsIcon/1010101.png 真实存在,加载即显)。
    ///
    /// 用途:奖励/物品显示把 type_id 还原成真实【名称】(<see cref="BaseAwardItem"/> / 完成弹层 / 对话奖励摘要),
    /// 真实【图标】走 ResManager.SetImageAsync。
    /// 降级(精确 blocker):goodsIcon 的 png 尚未导入 Unity(Assets/GameRes/resource/game/goodsIcon/ 为空)→
    /// 名称可即时显示,图标加载失败时由调用方降级 + 写明缺哪个 key(对标任务包 P1 "缺图标资源降级显示名称")。
    /// </summary>
    public static class GoodsModel
    {
        /// <summary>物品基础展示数据(名称/图标/品质),从 config_goods 的数字索引键解出。</summary>
        public sealed class GoodsBasic
        {
            public int TypeId;
            public string Name = "";
            public string Icon = "";   // goods_icon(图标资源 id)
            public int Color;          // 品质/颜色(0..8)
        }

        // config_goods 数字索引键(见类注释;改这里=对齐配表字段顺序,勿散落魔法字符串)。
        private const string K_NAME = "1";
        private const string K_ICON = "14";   // goods_icon(注:键 "9" 是 type、"10" 是 subtype,勿混)
        private const string K_COLOR = "18";   // color/品质 0..8

        private static JObject _goods;
        private static readonly Dictionary<int, GoodsBasic> _cache = new Dictionary<int, GoodsBasic>();

        public static bool IsLoaded => _goods != null;

        /// <summary>加载 config_goods(对标 TaskConfigs/NpcConfigs.EnsureLoaded;进游戏后由 TaskController 预载)。</summary>
        public static async Task EnsureLoaded()
        {
            if (_goods != null) return;

            string key = GameResPath.GetServerConfigPath("config_goods");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Goods", "missing config_goods: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _goods = new JObject();
                return;
            }

            _goods = JObject.Parse(asset.text);
            _cache.Clear();
            ResManager.Release(asset);
            GameLog.Info("Goods", "config_goods loaded: {0} goods", _goods.Count);
        }

        /// <summary>按 type_id 取物品基础数据(对标 GoodsModel.ts GetGoodsBasicByTypeId);未加载/无此物品返回 null。</summary>
        public static GoodsBasic GetGoodsBasicByTypeId(int typeId)
        {
            if (typeId <= 0 || _goods == null) return null;
            if (_cache.TryGetValue(typeId, out GoodsBasic cached)) return cached;
            if (!(_goods[typeId.ToString()] is JObject obj)) return null;

            GoodsBasic basic = new GoodsBasic
            {
                TypeId = typeId,
                Name = ReadString(obj, K_NAME),
                Icon = ReadString(obj, K_ICON),
                Color = ReadInt(obj, K_COLOR),
            };
            _cache[typeId] = basic;
            return basic;
        }

        /// <summary>物品名(对标 GoodsModel.ts GetGoodsName);无则空串(调用方降级为 type_id 文本)。</summary>
        public static string GetGoodsName(int typeId) => GetGoodsBasicByTypeId(typeId)?.Name ?? "";

        /// <summary>图标资源 id(对标 GoodsModel.ts GetGoodsIcon 的 cfg.goods_icon);无则空串。</summary>
        public static string GetGoodsIcon(int typeId) => GetGoodsBasicByTypeId(typeId)?.Icon ?? "";

        /// <summary>品质/颜色(0..8,对标 cfg.color);无则 0。</summary>
        public static int GetColor(int typeId) => GetGoodsBasicByTypeId(typeId)?.Color ?? 0;

        /// <summary>
        /// 品质底板色(com_goods_plate_{color} 用):基于 <see cref="GetColor"/>,叠加老端
        /// BaseAwardItem.ts:273-274 的特例(type_id 26270005/26260005 强制 7)。供 BaseAwardItem 底板与
        /// 完成弹层奖励行共用,避免特例散落多处。
        /// </summary>
        public static int GetDisplayColor(int typeId)
        {
            if (typeId == 26270005 || typeId == 26260005) return 7;
            return GetColor(typeId);
        }

        /// <summary>
        /// 把(type, type_id)映射成真实 goods_id + 绑定标记(对标 GoodsModel.ts GetMappingTypeId → [goods_id, lock])。
        ///   · type==0   普通物品 → (typeId, 0)
        ///   · type==100 绑定物品 → (typeId, 1)
        ///   · 其它(-1/255/货币经验类)需客户端 ConfigNotNormalGoods 映射(尚未同步)→ 暂原样返回 + 不臆造。
        /// </summary>
        public static (int goodsId, int locked) GetMappingTypeId(int type, int typeId)
        {
            if (type == 100) return (typeId, 1);
            // type==0 普通物品,或特殊类型(ConfigNotNormalGoods 未移植):原样返回,绝不臆造映射。
            return (typeId, 0);
        }

        // —— 数字索引键读取小工具(字符串/数字混排容错,同 NpcConfigs)——
        private static int ReadInt(JObject obj, string key)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            return token.Type == JTokenType.Integer ? token.Value<int>()
                : int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static string ReadString(JObject obj, string key)
        {
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }
    }
}
