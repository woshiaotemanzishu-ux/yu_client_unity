using System.Collections.Generic;
using System.Text;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Tasks
{
    /// <summary>
    /// 任务奖励解析(对话弹层与完成弹层共用,对标老端 DialogueController.On12102 + TaskFinishView.SetTaskReward
    /// 的 special_goods_list / award_list 装配)。奖励来自 config_task 的 special_goods_list(字段 23)与
    /// award_list(字段 24)——均为 config_task 配表里的 Erlang 文本(老端也是按 task_id 查 task_vo.special_goods_list,
    /// 不是 12102 实包字段),经 <see cref="ErlangParser"/> 解析。
    ///
    /// special_goods_list 元组语义【已用现网 config_task 全量分布实证 = {type, type_id, count}】:
    ///   首元 type 是 ConfigNotNormalGoods 的类型键(不是职业!实证首元分布 {0:86,2:32,3:542,5:501,10:1,255:32}
    ///   全是货币类型键——0/10/255 不可能是职业,货币恒 {type,0,count} 次元为 0)。经
    ///   <see cref="GoodsModel.GetMappingTypeId"/> 还原真实 goods_id:
    ///     · {0, goods_id, n}       → 真实物品(type=0,type_id 即 goods_id),如 {0,17020001,2}
    ///     · {3,0,n}/{5,0,n}/{2,0,n}→ 货币:金币(ConfigNotNormalGoods[3]=31)/经验(=32)/绑定灵玉(=35)…
    ///     · {255,k,n} / {-1,k,n}   → 货币键在 type_id(k),查 ConfigNotNormalGoods[k]
    ///   flat 3 元组是通用奖励(所有职业都给),无职业过滤,全部计入。
    ///   注:另有少量嵌套 {career,[{...}]} 职业定制礼包(circle/循环任务)——本类按"非 3 元组"跳过,留后续轮处理。
    ///
    /// award_list:形如 [{a,b,type_id,count}],4 元组,取索引 2/3 为真实物品 type_id/count(无歧义),全部计入。
    ///
    /// 为何不照抄老端 special_goods 解析:老端 TaskFinishView(vo[1] 当职业)/DialogueController(vo[0] 当职业)把它当
    /// 早期 4 元组 {career,?,type_id,count} 读、再 push 成真实物品——两处索引互相矛盾,且对现网 3 元组数据已失配
    /// (career 索引落到货币类型/数量上)。故以现网数据全量分布 + 引擎 GetMappingTypeId 契约为权威,不照抄残留静态码。
    /// 名称:config_goods 真名优先(<see cref="GoodsModel.GetGoodsName"/>);货币缺名退回 ConfigNotNormalGoods.desc
    /// (经验/金币…),再缺退 "物品 {id}"。真实图标由 <see cref="BaseAwardItem"/> 经 goods_id 显示。
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
        /// 解析某任务的可展示奖励。<paramref name="career"/> 当前不用于 flat 3 元组(通用奖励),
        /// 保留给后续嵌套 {career,[...]} 职业定制礼包过滤。
        /// </summary>
        public static List<Entry> Build(string specialGoodsList, string awardList, int career)
        {
            var result = new List<Entry>();
            AppendSpecialGoods(result, specialGoodsList);
            AppendAward(result, awardList);
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

        // special_goods_list:{type, type_id, count}(实证语义),经 GoodsModel.GetMappingTypeId 还原真实 goods_id。
        private static void AppendSpecialGoods(List<Entry> result, string text)
        {
            ErlangTerm root = ErlangParser.Parse(text);
            if (root?.Items == null) return;
            foreach (ErlangTerm t in root.Items)
            {
                if (t?.Items == null || t.Items.Count < 3) continue; // 嵌套 {career,[...]} 等非 3 元组:本轮跳过
                int type = t.Get<int>(0);
                int typeId = t.Get<int>(1);
                long count = t.Get<long>(2);
                (int goodsId, int _) = GoodsModel.GetMappingTypeId(type, typeId);
                bool isCurrency = type != 0 && type != 100;
                result.Add(new Entry(goodsId, count, isCurrency, ResolveName(goodsId, type, typeId, isCurrency)));
            }
        }

        // award_list:{a, b, type_id, count},真实物品(取索引 2/3)。
        private static void AppendAward(List<Entry> result, string text)
        {
            ErlangTerm root = ErlangParser.Parse(text);
            if (root?.Items == null) return;
            foreach (ErlangTerm t in root.Items)
            {
                if (t?.Items == null || t.Items.Count < 4) continue;
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
