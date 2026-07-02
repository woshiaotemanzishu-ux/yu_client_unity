using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Compose
{
    /// <summary>
    /// 神装合成(通用装备合成 COMPOSE_EQUIP type=2,yu_server pt_150 段内 15020;老端 CompositeController.ts)数据层。
    /// 「神装合成」=通用装备合成,无专门协议;服务端合成成功且规则 type==2 → red_equip_combine +1,
    /// 解主线 101725(ctype73)「合成一次神装」。15020 回包套值 + config_goods_compose(type==2 规则)读取均在此。
    /// </summary>
    public sealed class ComposeModel
    {
        public static readonly ComposeModel Instance = new ComposeModel();
        private ComposeModel() { }

        /// <summary>最近一次 15020 合成结果(对标老端 on15020:code:i, compose_type:c, rule_id:i, goods_id:l)。</summary>
        public int LastCode { get; private set; }
        public int LastComposeType { get; private set; }
        public int LastRuleId { get; private set; }
        public long LastGoodsId { get; private set; }
        public bool HasResult { get; private set; }

        /// <summary>15020 回包套值(code==1 成功;非 1 也如实记录,壳层/日志按 code 判断)。</summary>
        public void Apply15020(int code, int composeType, int ruleId, long goodsId)
        {
            LastCode = code;
            LastComposeType = composeType;
            LastRuleId = ruleId;
            LastGoodsId = goodsId;
            HasResult = true;
        }

        public void Clear()
        {
            LastCode = 0;
            LastComposeType = 0;
            LastRuleId = 0;
            LastGoodsId = 0;
            HasResult = false;
        }
    }

    /// <summary>
    /// config_goods_compose 读取器(数字索引键,18 列;列序权威来源 yu_client cdn/resource/config/server/
    /// config_table_default.json 的 "config_goods_compose" 字段名列表——实证摘录:
    /// ["id","name","extra","type","role_lv","sex","condition","subtype","regular_mat","irregular_mat",
    ///  "hole_list","cost","goods","fail_goods","ratio_type","ratio","bind_type","tv_type"]):
    ///   · "0"  = id(主键)
    ///   · "1"  = name(规则名,如「武器」「护符」)
    ///   · "3"  = type(规则大类;type==2=装备类,对标工单「神装合成」范围)
    ///   · "8"  = regular_mat(固定材料,Erlang term 字符串,形如 "[[{bind_type,type_id,num}]]";
    ///            ⚠实证:954 条 type==2 规则中仅 288 条 regular_mat 非空,其余 666 条走 irregular_mat
    ///            (每孔位一份「候选 type_id 列表」,选任一满足即可,结构不同、非本工单匹配范围——工单
    ///            明确「只按 regular_mat 的 type_id 在背包找 goods_id」,故 irregular_mat 规则如实显示
    ///            0 材料/无法匹配,不臆造成 regular_mat 语义)。
    ///   · "9"  = irregular_mat(候选材料,Erlang term 字符串;本读取器原样存供后续如实扩展,当前不参与匹配)。
    ///   · "12" = goods(合成产物,Erlang term 字符串,形如 "[{bind_type,type_id,num}]",取首项 type_id 展示名)。
    /// 表经 ClientConfigSync 从 yu_client cdn 同步进 Assets/GameRes/resource/config/server/config_goods_compose.json。
    /// </summary>
    public static class ComposeConfigs
    {
        private static JObject _compose;

        public static bool IsLoaded => _compose != null;

        public readonly struct MatEntry
        {
            public readonly int BindType;
            public readonly int TypeId;
            public readonly long Num;
            public MatEntry(int bindType, int typeId, long num) { BindType = bindType; TypeId = typeId; Num = num; }
        }

        public sealed class Rule
        {
            public int Id;
            public string Name;
            public int Type;
            public List<MatEntry> RegularMat = new List<MatEntry>();   // 列8(固定材料,可能为空——见类注释)
            public List<MatEntry> Goods = new List<MatEntry>();        // 列12(合成产物)
        }

        public static async Task EnsureLoaded()
        {
            if (_compose != null) return;
            string key = GameResPath.GetServerConfigPath("config_goods_compose");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Compose", "missing config_goods_compose: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _compose = new JObject();
                return;
            }
            _compose = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("Compose", "config_goods_compose={0}", _compose.Count);
        }

        /// <summary>type==2(装备类)规则前 N 条(按 JSON 键序,即 config 内原始顺序;工单「前 6 条」用此)。</summary>
        public static List<Rule> GetEquipRules(int limit)
        {
            var result = new List<Rule>();
            if (_compose == null) return result;
            foreach (var kv in _compose)
            {
                if (!(kv.Value is JObject obj)) continue;
                if (ReadInt(obj, "3") != 2) continue;   // type==2 装备类
                result.Add(ReadRule(obj));
                if (result.Count >= limit) break;
            }
            return result;
        }

        /// <summary>type==2 规则总数(供 CLI 用例断言 >0)。</summary>
        public static int CountEquipRules()
        {
            if (_compose == null) return 0;
            int n = 0;
            foreach (var kv in _compose)
            {
                if (kv.Value is JObject obj && ReadInt(obj, "3") == 2) n++;
            }
            return n;
        }

        private static Rule ReadRule(JObject obj)
        {
            var rule = new Rule
            {
                Id = ReadInt(obj, "0"),
                Name = ReadString(obj, "1"),
                Type = ReadInt(obj, "3"),
            };
            rule.RegularMat = ParseMatList(ReadString(obj, "8"));
            rule.Goods = ParseMatList(ReadString(obj, "12"));
            return rule;
        }

        /// <summary>解析 "[{bind_type,type_id,num},...]" 或 "[[{...}]]"(嵌套一层)形式的 Erlang term 材料/产物列表,
        /// 对标既有 GoodsModel.GetBaseAttrs / TaskReward.AppendTriple 的 ErlangTerm 遍历写法。
        /// 嵌套一层时展开取内层元组(regular_mat 常见 "[[{0,x,1}]]" 双层包裹,产物 goods 通常单层)。</summary>
        private static List<MatEntry> ParseMatList(string raw)
        {
            var result = new List<MatEntry>();
            if (string.IsNullOrEmpty(raw)) return result;
            ErlangTerm root = ErlangParser.Parse(raw);
            if (root?.Items == null) return result;
            foreach (ErlangTerm t in root.Items)
            {
                if (t == null) continue;
                if (t.Type == ErlangTerm.Kind.Tuple && t.Items != null && t.Items.Count >= 3)
                {
                    result.Add(new MatEntry(t.Get<int>(0), t.Get<int>(1), t.Get<long>(2)));
                }
                else if (t.Type == ErlangTerm.Kind.List && t.Items != null)
                {
                    // 嵌套一层(如 regular_mat "[[{0,x,1}]]"):展开内层元组。
                    foreach (ErlangTerm inner in t.Items)
                    {
                        if (inner?.Items != null && inner.Type == ErlangTerm.Kind.Tuple && inner.Items.Count >= 3)
                            result.Add(new MatEntry(inner.Get<int>(0), inner.Get<int>(1), inner.Get<long>(2)));
                    }
                }
            }
            return result;
        }

        private static int ReadInt(JObject obj, string key)
        {
            Newtonsoft.Json.Linq.JToken token = obj[key];
            if (token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null) return 0;
            return token.Type == Newtonsoft.Json.Linq.JTokenType.Integer ? token.Value<int>()
                : int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static string ReadString(JObject obj, string key)
        {
            Newtonsoft.Json.Linq.JToken token = obj[key];
            return token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null ? "" : token.ToString();
        }
    }
}
