using System.Collections.Generic;
using System.Text;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Tasks
{
    /// <summary>
    /// 任务奖励解析(对话弹层与完成弹层共用,对标老端 DialogueController.On12102 + TaskFinishView.SetTaskReward
    /// 的 special_goods_list / award_list 装配)。奖励数据来自 config_task 的 special_goods_list(字段 23)与
    /// award_list(字段 24),均为 Erlang 文本,经 <see cref="ErlangParser"/> 解析。
    ///
    /// 实测现网 config_task 数据格式(D:\...\config_task.json):
    ///   · special_goods_list: 形如 [{5,0,150000},{3,0,20000},{0,17020001,2}],3 元组 {career, type_id, count}。
    ///       career 为职业(实测 3/5 等)或 0(通用,所有职业都给);type_id==0 表示货币/经验数值(count 即数量)。
    ///       仅当 career==当前职业 或 career==0 时计入(对标 On12102 的 vo[0] 职业过滤)。
    ///   · award_list: 形如 [{1,1,101011010,1},{2,2,102011010,1}],4 元组 {a, b, type_id, count}。
    ///       type_id 为真实物品 id、count 为数量(取索引 2/3,无歧义);全部计入(任务奖励总表)。
    ///
    /// 说明:老端 TaskFinishView.SetTaskReward 用 vo[1] 作职业过滤、按 [0,vo[2],vo[3]] 读取,与现网 3 元组数据不符
    /// (那是早期 4 元组格式的残留),故此处以真实 config 数据为准、职业索引取 [0](与 On12102 一致)。
    /// 物品名经 <see cref="GoodsModel"/>(config_goods)还原成真实名称(ToText 用,替换裸 type_id);真实图标需
    /// goodsIcon png 导入后由 BaseAwardItem 显示(未导入则降级,精确 blocker 见 BaseAwardItem.RefreshIcon)。
    /// </summary>
    public static class TaskReward
    {
        public readonly struct Entry
        {
            public readonly int TypeId;
            public readonly long Count;
            public readonly bool IsCurrency; // type_id==0:货币/经验数值,而非具体物品

            public Entry(int typeId, long count, bool isCurrency)
            {
                TypeId = typeId;
                Count = count;
                IsCurrency = isCurrency;
            }
        }

        /// <summary>解析某任务的可展示奖励(职业过滤后)。career 取 <c>RoleModel.Instance.Career</c>。</summary>
        public static List<Entry> Build(string specialGoodsList, string awardList, int career)
        {
            var result = new List<Entry>();
            AppendSpecialGoods(result, specialGoodsList, career);
            AppendAward(result, awardList);
            return result;
        }

        /// <summary>
        /// 奖励列表的单行可读文本,供 TEMP 壳/日志/对话奖励摘要直接展示。物品名经 <see cref="GoodsModel"/>
        /// 还原成真实名称(对标老端 GoodsModel.GetGoodsName,替换裸 type_id);config_goods 未加载或查不到时
        /// 才退回 "物品 {type_id}"。货币/经验(type_id==0)无 goods 记录,保持 "奖励 ×N"(对标老端货币不进 goods 表)。
        /// </summary>
        public static string ToText(IReadOnlyList<Entry> entries, string separator = "\n")
        {
            if (entries == null || entries.Count == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(separator);
                Entry e = entries[i];
                if (e.IsCurrency)
                {
                    sb.Append("奖励 ×").Append(e.Count);
                    continue;
                }
                string name = GoodsModel.GetGoodsName(e.TypeId);
                if (string.IsNullOrEmpty(name)) sb.Append("物品 ").Append(e.TypeId); // 降级:config_goods 缺/未加载
                else sb.Append(name);                                                  // 真实物品名(淬魂原石 等)
                sb.Append(" ×").Append(e.Count);
            }
            return sb.ToString();
        }

        private static void AppendSpecialGoods(List<Entry> result, string text, int career)
        {
            ErlangTerm root = ErlangParser.Parse(text);
            if (root?.Items == null) return;
            foreach (ErlangTerm t in root.Items)
            {
                if (t?.Items == null || t.Items.Count < 3) continue;
                int c = t.Get<int>(0);
                if (c != career && c != 0) continue; // 职业过滤:本职业 + 通用(career==0)
                int typeId = t.Get<int>(1);
                long count = t.Get<long>(2);
                result.Add(new Entry(typeId, count, typeId == 0));
            }
        }

        private static void AppendAward(List<Entry> result, string text)
        {
            ErlangTerm root = ErlangParser.Parse(text);
            if (root?.Items == null) return;
            foreach (ErlangTerm t in root.Items)
            {
                if (t?.Items == null || t.Items.Count < 4) continue;
                int typeId = t.Get<int>(2);
                long count = t.Get<long>(3);
                result.Add(new Entry(typeId, count, false));
            }
        }
    }
}
