using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Net;
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
        /// <summary>物品基础展示数据(名称/图标/品质/类型/装备元数据),从 config_goods 的数字索引键解出。</summary>
        public sealed class GoodsBasic
        {
            public int TypeId;
            public string Name = "";
            public string Icon = "";   // goods_icon(图标资源 id,key "14")
            public int Color;          // 品质/颜色(0..8,key "18")
            public string Intro = "";  // intro(物品介绍/描述,key "2",对标老端 GoodsTooltips.intro)
            public int Type;           // type(物品大类,key "9";==10 装备 → 走装备 tips 分支,对标 UIToolTipMgr type==10)
            public int Subtype;        // subtype(子类,key "10")
            public int EquipType;      // equip_type(装备部位 1..10,key "13" → GetEquipPosName,对标 WordManager.GetEquipPos)
            public int CareerId;       // career_id(职业需求 0=通用,key "15")
            public int Level;          // level(需求等级,key "16",对标 GoodsTooltips/EquipToolTips basic.level)
            public string Getway = ""; // getway(获取途径/来源文本,key "3",对标 GoodsTooltips.ways=basic.getway)
            public string BaseAttrList = ""; // base_attrlist(装备基础属性 Erlang term [{attr_id,val},...],key "26",对标 EquipToolTips basic.base_attrlist)
        }

        /// <summary>装备配置行(config_equip_attr[type_id];字段下标见 config_table_default.json:1=stage 2=star 3=base_rating)。
        /// 极品/专有属性(下标 5 recommend_attr / 6 other_attr)待显示分支接入再加(本轮只读阶/星/评分)。</summary>
        public sealed class EquipAttr
        {
            public int Stage;            // 阶(对标 EquipToolTips grade=`${equip_vo.stage}阶`)
            public int Star;             // 星
            public int BaseRating;       // 基础评分(对标 EquipToolTips score 兜底 base_rating)
        }

        // config_goods 数字索引键(权威序见 config_table_default.json config_goods 字段列表;改这里=对齐配表字段顺序,勿散落魔法字符串)。
        private const string K_NAME = "1";
        private const string K_INTRO = "2";        // intro(物品介绍/描述)
        private const string K_GETWAY = "3";       // getway(获取途径/来源文本,对标 GoodsTooltips.ways)
        private const string K_TYPE = "9";         // type(物品大类;==10 装备)
        private const string K_SUBTYPE = "10";     // subtype(子类)
        private const string K_EQUIP_TYPE = "13";  // equip_type(装备部位 1..10)
        private const string K_ICON = "14";        // goods_icon
        private const string K_CAREER = "15";      // career_id(职业需求 0=通用)
        private const string K_LEVEL = "16";       // level(需求等级)
        private const string K_COLOR = "18";       // color/品质 0..8
        private const string K_BASE_ATTR = "26";   // base_attrlist(装备基础属性 Erlang term)

        // config_equip_attr 数字键(config_table_default.json:goods_id/stage/star/base_rating/class_type/recommend_attr/other_attr)。
        private const string KE_STAGE = "1";
        private const string KE_STAR = "2";
        private const string KE_BASE_RATING = "3";

        // GoodsType / ConfigItemAttr 具名字段(对标 WordManager.GetGoodsStyle cfg.type_name / GetProperties cfg.name)。
        private const string K_TYPE_NAME = "type_name";
        private const string K_ATTR_NAME = "name";

        // 装备部位名(equip_type 1..10,对标 WordManager.Equip_Pos_arr,硬编码同老端)。
        private static readonly string[] EQUIP_POS = { "武器", "头冠", "项链", "衣服", "护符", "裤子", "手镯", "护腕", "戒指", "鞋子" };
        // 职业名(career_id 0..4,对标 WordManager.GetCareerLimit)。
        private static readonly string[] CAREER_NAME = { "通用", "剑士", "武姬", "枪使", "弓手" };

        private static JObject _goods;
        private static JObject _notNormal;   // ConfigNotNormalGoods:货币/经验 type→{goods_id,desc}(GetMappingTypeId 用)
        private static JObject _goodsType;   // GoodsType:type→{type_name}(GetGoodsTypeName 用,对标 WordManager.GetGoodsStyle)
        private static JObject _itemAttr;    // ConfigItemAttr:attr_id→{name}(GetAttrName 用,对标 WordManager.GetProperties)
        private static JObject _equipAttr;   // config_equip_attr:type_id→{stage,star,base_rating,recommend_attr,other_attr}
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

            // 货币/经验映射表(client 配置;type→goods_id,如 3→31 金币、5→32 经验)。
            string nnKey = GameResPath.GetClientConfigPath("confignotnormalgoods");
            UnityEngine.TextAsset nnAsset = await ResManager.LoadAsync<UnityEngine.TextAsset>(nnKey);
            if (nnAsset != null)
            {
                _notNormal = JObject.Parse(nnAsset.text);
                ResManager.Release(nnAsset);
            }
            else
            {
                _notNormal = new JObject();
                GameLog.Warn("Goods", "missing ConfigNotNormalGoods: {0}(未同步?跑 神霄/配表/同步客户端配置)", nnKey);
            }

            // 物品大类名 / 属性名 / 装备配置(tips 类型行 + 装备基础属性行用;对标 WordManager.GetGoodsStyle/GetProperties + config_equip_attr)。
            _goodsType = await LoadConfigObj(GameResPath.GetServerConfigPath("goodstype"), "GoodsType");
            _itemAttr = await LoadConfigObj(GameResPath.GetClientConfigPath("configitemattr"), "ConfigItemAttr");
            _equipAttr = await LoadConfigObj(GameResPath.GetServerConfigPath("config_equip_attr"), "config_equip_attr");

            GameLog.Info("Goods", "config_goods={0} notNormal={1} goodsType={2} itemAttr={3} equipAttr={4}",
                _goods.Count, _notNormal.Count, _goodsType.Count, _itemAttr.Count, _equipAttr.Count);
        }

        /// <summary>加载一份 JObject 配置(缺失返回空 JObject + 警告,不让缺表炸链路;对标 EnsureLoaded 的容错)。</summary>
        private static async Task<JObject> LoadConfigObj(string key, string label)
        {
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Warn("Goods", "missing {0}: {1}(未同步?跑 神霄/配表/同步客户端配置)", label, key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
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
                Intro = ReadString(obj, K_INTRO),
                Type = ReadInt(obj, K_TYPE),
                Subtype = ReadInt(obj, K_SUBTYPE),
                EquipType = ReadInt(obj, K_EQUIP_TYPE),
                CareerId = ReadInt(obj, K_CAREER),
                Level = ReadInt(obj, K_LEVEL),
                Getway = ReadString(obj, K_GETWAY),
                BaseAttrList = ReadString(obj, K_BASE_ATTR),
            };
            _cache[typeId] = basic;
            return basic;
        }

        /// <summary>物品名(对标 GoodsModel.ts GetGoodsName);无则空串(调用方降级为 type_id 文本)。</summary>
        public static string GetGoodsName(int typeId) => GetGoodsBasicByTypeId(typeId)?.Name ?? "";

        /// <summary>图标资源 id(对标 GoodsModel.ts GetGoodsIcon 的 cfg.goods_icon);无则空串。</summary>
        public static string GetGoodsIcon(int typeId) => GetGoodsBasicByTypeId(typeId)?.Icon ?? "";

        /// <summary>物品介绍/描述(对标 config_goods key "2"=intro,老端 GoodsTooltips.intro);无则空串。
        /// 原文含 Laya HTML(&lt;br/&gt;/&lt;font color&gt;),由调用方(物品 tips)按 TMP 富文本转换显示。</summary>
        public static string GetGoodsIntro(int typeId) => GetGoodsBasicByTypeId(typeId)?.Intro ?? "";

        /// <summary>物品大类 type(config_goods key "9";==10 装备 → 走装备 tips,对标 UIToolTipMgr.DefaultAppendTips type==10)。</summary>
        public static int GetGoodsType(int typeId) => GetGoodsBasicByTypeId(typeId)?.Type ?? 0;

        /// <summary>是否装备(type==10,对标 UIToolTipMgr type==10 → AppendEquipTips)。</summary>
        public static bool IsEquip(int typeId) => GetGoodsType(typeId) == 10;

        /// <summary>获取途径/来源文本(config_goods key "3"=getway,对标 GoodsTooltips.ways=basic.getway);无则空串。</summary>
        public static string GetGoodsGetway(int typeId) => GetGoodsBasicByTypeId(typeId)?.Getway ?? "";

        /// <summary>物品大类文案(GoodsType[type].type_name,如 10→"装备";对标 WordManager.GetGoodsStyle);无则空串。</summary>
        public static string GetGoodsTypeName(int type)
        {
            if (_goodsType != null && _goodsType[type.ToString()] is JObject o) return ReadString(o, K_TYPE_NAME);
            return "";
        }

        /// <summary>属性名(ConfigItemAttr[attrId].name,如 1→"攻击";对标 WordManager.GetProperties);无则空串。</summary>
        public static string GetAttrName(int attrId)
        {
            if (_itemAttr != null && _itemAttr[attrId.ToString()] is JObject o) return ReadString(o, K_ATTR_NAME);
            return "";
        }

        /// <summary>装备部位名(equip_type 1..10 → 武器/头冠/…;对标 WordManager.GetEquipPos);越界返回 ""。</summary>
        public static string GetEquipPosName(int equipType)
        {
            int idx = equipType - 1;
            return (idx >= 0 && idx < EQUIP_POS.Length) ? EQUIP_POS[idx] : "";
        }

        /// <summary>职业名(career_id 0..4 → 通用/剑士/…;对标 WordManager.GetCareerLimit);越界返回 "通用"。</summary>
        public static string GetCareerName(int careerId)
        {
            return (careerId >= 0 && careerId < CAREER_NAME.Length) ? CAREER_NAME[careerId] : "通用";
        }

        /// <summary>装备配置行(config_equip_attr[type_id]:阶/星/评分/极品/专有属性,对标 EquipToolTips equip_vo);无则 null。</summary>
        public static EquipAttr GetEquipAttr(int typeId)
        {
            if (_equipAttr == null || !(_equipAttr[typeId.ToString()] is JObject o)) return null;
            return new EquipAttr
            {
                Stage = ReadInt(o, KE_STAGE),
                Star = ReadInt(o, KE_STAR),
                BaseRating = ReadInt(o, KE_BASE_RATING),
            };
        }

        /// <summary>
        /// 装备基础属性行(config_goods base_attrlist key "26" 的 Erlang term [{attr_id,val},...] → [(属性名,值)];
        /// 对标 EquipToolTips.GetBaseAndStrenProStrArr 的 base 部分:每项经 <see cref="GetAttrName"/> 取真名)。
        /// 缺属性名 → 兜底标 "属性{id}"(不臆造名,精确暴露缺哪个 attr_id)。无 base_attrlist 返回空表。
        /// </summary>
        public static List<(string name, long val)> GetBaseAttrs(int typeId)
        {
            var result = new List<(string, long)>();
            string raw = GetGoodsBasicByTypeId(typeId)?.BaseAttrList;
            if (string.IsNullOrEmpty(raw)) return result;

            ErlangTerm list = ErlangParser.Parse(raw);
            if (list?.Items == null) return result;
            foreach (ErlangTerm pair in list.Items)
            {
                if (!pair.IsCollection || pair.Items == null || pair.Items.Count < 2) continue;
                int attrId = pair.Get<int>(0);
                long val = pair.Get<long>(1);
                string name = GetAttrName(attrId);
                if (string.IsNullOrEmpty(name)) name = "属性" + attrId;
                result.Add((name, val));
            }
            return result;
        }

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
        /// 把(type, type_id)映射成真实 goods_id + 绑定标记(对标 GoodsModel.ts:2972-2991 GetMappingTypeId → [goods_id, lock])。
        ///   · type==0   普通物品 → (typeId, 0)
        ///   · type==100 绑定物品 → (typeId, 1)
        ///   · type==-1 / 255    货币:键是 typeId → ConfigNotNormalGoods[typeId].goods_id
        ///   · 其它(3/5/2/10…) 货币:键是 type   → ConfigNotNormalGoods[type].goods_id(3→31 金币、5→32 经验)
        /// 表里查不到该键 → 原样返回 typeId(不臆造)。
        /// 元组语义(special_goods_list {type,type_id,count})以现网 config_task 全量分布实证:首元仅取
        /// {0,2,3,5,10,255} 等 ConfigNotNormalGoods 类型键(非职业;0/10/255 不可能是职业),次元货币恒 0。
        /// </summary>
        public static (int goodsId, int locked) GetMappingTypeId(int type, int typeId)
        {
            if (type == 100) return (typeId, 1);
            if (type == 0) return (typeId, 0);
            int key = (type == -1 || type == 255) ? typeId : type;
            int mapped = LookupNotNormalGoodsId(key);
            return mapped > 0 ? (mapped, 0) : (typeId, 0);
        }

        /// <summary>ConfigNotNormalGoods[key].goods_id;无则 0。</summary>
        private static int LookupNotNormalGoodsId(int key)
        {
            if (_notNormal != null && _notNormal[key.ToString()] is JObject o) return ReadInt(o, "goods_id");
            return 0;
        }

        /// <summary>
        /// 货币/经验的中文 desc(ConfigNotNormalGoods[key].desc,如 "经验"/"金币"):config_goods 查不到名时的兜底名。
        /// key 规则同 <see cref="GetMappingTypeId"/>(255/-1 用 typeId,其它用 type)。type 为 0/100(普通物品)返回空。
        /// </summary>
        public static string GetNotNormalDesc(int type, int typeId)
        {
            if (_notNormal == null || type == 0 || type == 100) return "";
            int key = (type == -1 || type == 255) ? typeId : type;
            return _notNormal[key.ToString()] is JObject o ? ReadString(o, "desc") : "";
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
