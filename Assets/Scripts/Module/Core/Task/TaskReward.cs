using System.Collections.Generic;
using System.Text;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Tasks
{
    /// <summary>
    /// 任务奖励解析(对话弹层与完成弹层共用,对标老端 DialogueController.On12102 + TaskFinishView.SetTaskReward)。
    /// 奖励来自 config_task 两列 Erlang 文本,经 <see cref="ErlangParser"/> 解析。
    /// 【权威字段序】config_table_default.json 的 config_task 列名:下标 23 = award_list、24 = special_goods_list。
    ///   注意:本类 Build 的形参名 specialGoodsList / awardList 与配表名【互换】——因 TaskConfigs 把字段 23 读进
    ///   SpecialGoodsList、字段 24 读进 AwardList(变量名与配表名错位,但解析逻辑已各自对齐到正确的数据形态)。
    ///
    /// 形参 specialGoodsList(= 配表字段 23 award_list,通用/货币奖励)经 <see cref="AppendSpecialGoods"/> 解析:
    ///   · flat 3 元组 {type, type_id, count}:首元 type 是 ConfigNotNormalGoods 类型键(非职业!现网分布
    ///     {0:65,2:32,3:542,5:501,10:1,255:32} 全是货币类型键)。经 <see cref="GoodsModel.GetMappingTypeId"/> 还原:
    ///       {0,goods_id,n}=真实物品;{3,0,n}/{5,0,n}/{2,0,n}=金币/经验/绑定灵玉;{255,k,n}/{-1,k,n}=货币键在 type_id。
    ///     通用奖励(所有职业都给),无职业过滤,全部计入。
    ///   · 嵌套 {career, [{type,type_id,count},...]} 职业定制礼包(现网 18 个 circle/循环任务):按当前职业过滤后解析子列表。
    ///
    /// 形参 awardList(= 配表字段 24 special_goods_list,职业专属奖励)经 <see cref="AppendAward"/> 解析:
    ///   形如 [{career,?,type_id,count}] 4 元组,首元 career∈{1,2,3,4}。现网 26 个任务全部 arity-4、首元分布恰
    ///   {1:26,2:26,3:26,4:26}——每个任务给全部 4 职业各一件武器,故【必须按当前职业过滤】只展示本职业那一件
    ///   (对标老端 special_goods_list 的 vo[0]==career 过滤);漏过滤即把 4 职业武器全展示("一堆东西")。取索引 2/3 为 type_id/count。
    ///
    /// 名称:config_goods 真名优先(<see cref="GoodsModel.GetGoodsName"/>);货币缺名退回 ConfigNotNormalGoods.desc
    /// (经验/金币…),再缺退 "物品 {id}"。真实图标由 <see cref="BaseAwardItem"/> 经 goods_id 显示。
    /// 数量缩写由格子件(<see cref="Shenxiao.Module.Core.Common.EquipmentItem"/> / BaseAwardItem)经 FormatCountNum 处理。
    /// </summary>
    public static class TaskReward
    {
        public readonly struct Entry
        {
            public readonly int TypeId;      // 解出的真实 goods_id(BaseAwardItem.SetData / 名称图标查询用)
            public readonly long Count;
            public readonly bool IsCurrency; // 经 ConfigNotNormalGoods 解出的货币/经验(走文本,不进物品图标格)
            public readonly string Name;     // 展示名(config_goods 名 → ConfigNotNormalGoods.desc → "物品 {id}")

            public Entry(int typeId, long count, bool isCurrency, string name)
            {
                TypeId = typeId;
                Count = count;
                IsCurrency = isCurrency;
                Name = name;
            }
        }

        /// <summary>
        /// 解析某任务的可展示奖励。flat 3 元组是通用奖励(所有职业);<paramref name="career"/> 用于过滤嵌套
        /// {career,[...]} 职业定制礼包(circle/循环任务)——只计入与当前职业相符的子礼包。
        /// </summary>
        public static List<Entry> Build(string specialGoodsList, string awardList, int career)
        {
            var result = new List<Entry>();
            AppendSpecialGoods(result, specialGoodsList, career);
            AppendAward(result, awardList, career);
            return result;
        }

        /// <summary>奖励列表单行可读文本(名称 ×数量),供 TEMP 壳/日志/对话奖励摘要展示;名称在 <see cref="Build"/> 时已解析。</summary>
        public static string ToText(IReadOnlyList<Entry> entries, string separator = "\n")
        {
            if (entries == null || entries.Count == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(separator);
                Entry e = entries[i];
                sb.Append(string.IsNullOrEmpty(e.Name) ? "奖励" : e.Name).Append(" ×").Append(e.Count);
            }
            return sb.ToString();
        }

        // special_goods_list 两形态(均经 GoodsModel.GetMappingTypeId 还原真实 goods_id):
        //   · flat 3 元组 {type, type_id, count}:通用奖励(所有职业都给)。
        //   · 嵌套 {career, [{type,type_id,count},...]}:职业定制礼包(circle/循环任务),按当前职业过滤后解析子列表。
        private static void AppendSpecialGoods(List<Entry> result, string text, int career)
        {
            ErlangTerm root = ErlangParser.Parse(text);
            if (root?.Items == null) return;
            foreach (ErlangTerm t in root.Items)
            {
                if (t?.Items == null || t.Items.Count < 2) continue;
                // 嵌套职业定制礼包:第 2 元是子奖励列表 → {career, [子元组...]},按当前职业过滤(非本职业跳过)。
                if (t.Items[1] != null && t.Items[1].Type == ErlangTerm.Kind.List)
                {
                    if (t.Get<int>(0) != career) continue;
                    IReadOnlyList<ErlangTerm> subList = t.Items[1].Items;
                    if (subList == null) continue;
                    foreach (ErlangTerm sub in subList) AppendTriple(result, sub);
                    continue;
                }
                // flat 3 元组(通用奖励)。
                AppendTriple(result, t);
            }
        }

        /// <summary>把一个 {type, type_id, count} 元组解析进结果(经 GoodsModel.GetMappingTypeId 还原真实 goods_id)。</summary>
        private static void AppendTriple(List<Entry> result, ErlangTerm t)
        {
            if (t?.Items == null || t.Items.Count < 3) return;
            int type = t.Get<int>(0);
            int typeId = t.Get<int>(1);
            long count = t.Get<long>(2);
            (int goodsId, int _) = GoodsModel.GetMappingTypeId(type, typeId);
            bool isCurrency = type != 0 && type != 100;
            result.Add(new Entry(goodsId, count, isCurrency, ResolveName(goodsId, type, typeId, isCurrency)));
        }

        // 此列对标 config_task 字段 24 = special_goods_list(职业专属奖励),每行 4 元组 {career, ?, type_id, count}。
        // 【现网全量实证】字段 24 共 26 个任务、全部 arity-4,首元(career)分布恰为 {1:26,2:26,3:26,4:26}——
        //   即每个任务都给全部 4 职业各一件武器,必须按当前职业过滤,只展示本职业那一件(对标老端
        //   DialogueController.On12102 对 special_goods_list 的 vo[0]==career 过滤)。漏过滤会把 4 职业武器全展示。
        // 注:Unity TaskConfigs 把字段 23/24 读进的变量名与配表名互换(23→SpecialGoodsList、24→AwardList),
        //   故这里收到的 awardList 实为配表 special_goods_list(职业专属),职业过滤落在本方法。
        private static void AppendAward(List<Entry> result, string text, int career)
        {
            ErlangTerm root = ErlangParser.Parse(text);
            if (root?.Items == null) return;
            foreach (ErlangTerm t in root.Items)
            {
                if (t?.Items == null || t.Items.Count < 4) continue;
                if (t.Get<int>(0) != career) continue;   // 只取当前职业那一行(career 在索引 0)
                int typeId = t.Get<int>(2);
                long count = t.Get<long>(3);
                result.Add(new Entry(typeId, count, false, ResolveName(typeId, 0, typeId, false)));
            }
        }

        /// <summary>展示名:config_goods 真名 → (货币)ConfigNotNormalGoods.desc → 降级 "物品 {id}"/"奖励"。</summary>
        private static string ResolveName(int goodsId, int type, int typeId, bool isCurrency)
        {
            string name = GoodsModel.GetGoodsName(goodsId);
            if (!string.IsNullOrEmpty(name)) return name;
            if (isCurrency)
            {
                string desc = GoodsModel.GetNotNormalDesc(type, typeId);
                return string.IsNullOrEmpty(desc) ? "奖励" : desc;
            }
            return "物品 " + goodsId;
        }
    }
}
