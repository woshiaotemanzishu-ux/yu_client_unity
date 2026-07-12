using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 排行榜(自动循环 轮12 #12;纯数据层轮)实证:反射喂 RankController 私有 handler(模板
    /// PetTrainCase+DailyHubCase,纯逻辑段——本轮 UI prefab 全套不存在,不写渲染段,见类注释)。
    ///
    /// 断言覆盖:
    ///   · config_ranking:show==1 过滤 + sortid 升序(15条→11可见,对标老端 spiritTabDataList())+
    ///     战力榜(200)真实数值(rank_max=100/rank_limit=300000)+ 结社(100)show=0 隐藏。
    ///   · config_medal 导表(仅访问器,不解析勋章渲染)。
    ///   · 22100 防御 recv(服务端从未调用此号,r12_server §存活判定)——不炸+显码。
    ///   · Guard:Start≤0/Len≤0 本地拦截不发包(NetManager"send while disconnected"日志缺席佐证未触发发送;
    ///     合法参数则日志出现佐证确实尝试发送,对标 TeamCase 24008/24047 同款断言写法)。
    ///   · **22101 config 驱动分页续拉(轮12 blocker 修复)**:服务端 lib_common_rank_mod.erl 正常分支
    ///     (:1220)与越界分支(:1190)的 Sum 字段位置都传请求 Len(恒为回声,非真实总数)——喂的响应包形按
    ///     真实服务端形态构造(sum 恒等于该次请求的 len),续拉断言锚定 RankConfigs.RankMax:
    ///       - 战力榜(rank_max=100,20 整除)拉满 5 页共 100 条,每页收到后自动续发下一页,第 5 页收满后
    ///         停止 + MarkComplete,且每页存档的 Sum 均等于该页请求 len(证明确实只是回声,未被当总数用)。
    ///       - 等级榜(rank_max=50,20 不整除)拉 20+20+10 三页:末页 len=10 比前两页的 20 更小——旧的
    ///         "sum 只增不减"防御会因新 sum(10)&lt;基线(20)而误判越界、丢弃整页数据;新实现按 ConfiguredMax
    ///         判断,验证末页 10 条被正常采信入库、总数收满 50、正确终止。
    ///   · 服务端回空列表 → 立即终止(不判 needMore)。
    ///   · 未知 rank_type(不在配置表内)兜底单页终止不炸(RankConfigs.GetByType 查不到时 ConfiguredMax
    ///     默认 0,首页收到即因 received≥ConfiguredMax 终止,不臆造总数、不续发、不死循环)。
    /// 严禁实现:22102/22103/22104(服务端 handle 整段注释,彻底不可达)/22105(老端自己未注册)——
    /// 本用例不测试这四个号,RankController 本身也未注册它们(规格§0/纪律5)。
    /// </summary>
    public static class RankCase
    {
        public static async Task<int> Run()
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Rank.RankConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Rank.RankConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY rank FAIL RankConfigs not loaded");
                    return 3;
                }

                // ---- A. config_ranking:show==1 过滤 + sortid 升序(15条→11可见) ----
                List<Shenxiao.Module.Core.Rank.RankConfigs.RankTypeCfg> visible =
                    Shenxiao.Module.Core.Rank.RankConfigs.GetVisibleSorted();
                bool visCountOk = visible.Count == 11;
                bool visOrderOk = visCountOk
                    && visible[0].Type == Shenxiao.Module.Core.Rank.RankModel.TYPE_FIGHT
                    && visible[visible.Count - 1].Type == Shenxiao.Module.Core.Rank.RankModel.TYPE_ONHOOK;
                Shenxiao.Module.Core.Rank.RankConfigs.RankTypeCfg fight =
                    Shenxiao.Module.Core.Rank.RankConfigs.GetByType(Shenxiao.Module.Core.Rank.RankModel.TYPE_FIGHT);
                bool fightCfgOk = fight != null && fight.RankMax == 100 && fight.RankLimit == 300000
                    && fight.Show == 1 && fight.SortId == 1;
                Shenxiao.Module.Core.Rank.RankConfigs.RankTypeCfg guild =
                    Shenxiao.Module.Core.Rank.RankConfigs.GetByType(Shenxiao.Module.Core.Rank.RankModel.TYPE_GUILD);
                bool guildHiddenOk = guild != null && guild.Show == 0; // 半死:config show:0 隐藏(r12_oldrank §半死分析)
                bool medalLoadedOk = Shenxiao.Module.Core.Rank.RankConfigs.MedalCount > 100;
                Debug.Log("CLIVERIFY rank config visCount=" + visible.Count + " visOrder=" + visOrderOk
                    + " fightCfg=" + fightCfgOk + " guildHidden=" + guildHiddenOk
                    + " medalCount=" + Shenxiao.Module.Core.Rank.RankConfigs.MedalCount);

                object ctrl = Shenxiao.Module.Core.Rank.RankController.Instance;
                const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo H(string name)
                {
                    MethodInfo m = ctrl.GetType().GetMethod(name, F);
                    if (m == null) Debug.LogError("CLIVERIFY rank handler missing: " + name);
                    return m;
                }
                MethodInfo m22100 = H("On22100"), m22101 = H("On22101");
                if (m22100 == null || m22101 == null) return 3;
                void Feed(MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.Rank.RankModel model = Shenxiao.Module.Core.Rank.RankModel.Instance;
                model.Clear();

                // ---- B. 22100 防御 recv(服务端从未发过此号,仍不炸+显码) ----
                logs.Clear();
                bool err22100NoThrow = true;
                try { Feed(m22100, new CliVerify.Pkt().I(2210001).Bytes()); }
                catch (Exception e) { err22100NoThrow = false; Debug.LogError("CLIVERIFY rank 22100 threw: " + e); }
                bool err22100Ok = err22100NoThrow && logs.Exists(l => l.Contains("22100 错误码壳"));
                Debug.Log("CLIVERIFY rank 22100 noThrow=" + err22100NoThrow + " ok=" + err22100Ok);

                // ---- C. Guard:Start≤0/Len≤0 本地拦截不发包(不触发 NetManager 发送尝试) ----
                logs.Clear();
                Shenxiao.Module.Core.Rank.RankController.Instance.RequestRank(
                    Shenxiao.Module.Core.Rank.RankModel.TYPE_FIGHT, 0, 20); // Start≤0
                Shenxiao.Module.Core.Rank.RankController.Instance.RequestRank(
                    Shenxiao.Module.Core.Rank.RankModel.TYPE_FIGHT, 1, 0);  // Len≤0
                bool guardBlockedOk = !logs.Exists(l => l.Contains("proto=" + Shenxiao.Framework.Net.Proto.RANK_QUERY));
                bool guardLogOk = logs.Exists(l => l.Contains("RequestRank 本地拦截非法分页"));
                logs.Clear();
                Shenxiao.Module.Core.Rank.RankController.Instance.RequestRank(
                    Shenxiao.Module.Core.Rank.RankModel.TYPE_FIGHT, 1, 20); // 合法
                bool guardPassOk = logs.Exists(l => l.Contains("proto=" + Shenxiao.Framework.Net.Proto.RANK_QUERY));
                Debug.Log("CLIVERIFY rank guard blocked=" + guardBlockedOk + " blockedLog=" + guardLogOk
                    + " passThrough=" + guardPassOk);

                // ---- D. 22101 config 驱动分页(FIGHT,rank_max=100,20 整除→整拉 5 页)----
                // 真实服务端形态:每页 sum 恒等于该次请求的 len(=20,非真实总数)——RequestRankFirstPage
                // 触发 BeginQuery 把 ConfiguredMax 锁为 RankConfigs.GetByType(FIGHT).RankMax=100。
                model.Clear();
                logs.Clear();
                Shenxiao.Module.Core.Rank.RankController.Instance.RequestRankFirstPage(
                    Shenxiao.Module.Core.Rank.RankModel.TYPE_FIGHT);
                bool firstPageSentOk = logs.Exists(l => l.Contains("proto=" + Shenxiao.Framework.Net.Proto.RANK_QUERY));

                Shenxiao.Module.Core.Rank.RankModel.RankTypeData dataFight = null;
                int fightReceived = 0;
                bool fightMidPagesOk = true;
                bool fightLastPageOk = false;
                bool selVal64Ok = false;
                for (int page = 1; page <= 5; page++)
                {
                    int start = fightReceived + 1;
                    const int len = 20;
                    logs.Clear();
                    Feed(m22101, BuildRankPage(Shenxiao.Module.Core.Rank.RankModel.TYPE_FIGHT,
                        start, len, 7, 10000000000L, 3, len, start));
                    fightReceived += len;
                    dataFight = model.GetData(Shenxiao.Module.Core.Rank.RankModel.TYPE_FIGHT);
                    bool receivedOk = dataFight != null && dataFight.Items.Count == fightReceived;
                    bool sumEchoOk = dataFight != null && dataFight.Sum == len; // 每页存档 Sum 恒等于该页请求 len(证回声非总数)
                    if (page == 1)
                    {
                        selVal64Ok = dataFight != null && dataFight.SelVal > int.MaxValue; // 尾哨兵证64位而非32位截断
                    }
                    if (page < 5)
                    {
                        bool resendOk = logs.Exists(l => l.Contains("proto=" + Shenxiao.Framework.Net.Proto.RANK_QUERY));
                        fightMidPagesOk &= receivedOk && sumEchoOk && resendOk && !dataFight.Complete;
                    }
                    else
                    {
                        bool noMoreResendOk = !logs.Exists(l => l.Contains("proto=" + Shenxiao.Framework.Net.Proto.RANK_QUERY));
                        fightLastPageOk = receivedOk && sumEchoOk && noMoreResendOk && dataFight.Complete;
                    }
                }
                bool fightTotalOk = fightReceived == 100 && dataFight != null && dataFight.Items.Count == 100;
                bool tailSentinelOk = dataFight != null && dataFight.Items.Count == 100
                    && dataFight.Items[99].Rank == 100 && dataFight.Items[99].Figure != null
                    && dataFight.Items[99].Figure.name == "r100";
                Debug.Log("CLIVERIFY rank 22101 FIGHT firstSent=" + firstPageSentOk + " total=" + fightReceived
                    + " midPagesOk=" + fightMidPagesOk + " lastPageOk=" + fightLastPageOk
                    + " selVal64=" + selVal64Ok + " tailSentinel=" + tailSentinelOk);

                // ---- E. 末页短 len 不被误杀(LEVEL,rank_max=50,20 不整除→20+20+10 三页)----
                // 旧实现"sum 只增不减"防御会把末页 sum(10)&lt;前两页基线(20) 误判越界、丢弃整页——新实现按
                // ConfiguredMax(50) 判断,验证 10 条正常入库、总数收满 50、正确终止,不受 sum 回落影响。
                logs.Clear();
                Shenxiao.Module.Core.Rank.RankController.Instance.RequestRankFirstPage(
                    Shenxiao.Module.Core.Rank.RankModel.TYPE_LEVEL);
                Feed(m22101, BuildRankPage(Shenxiao.Module.Core.Rank.RankModel.TYPE_LEVEL, 1, 20, 0, 0L, 0, 20, 1));
                Feed(m22101, BuildRankPage(Shenxiao.Module.Core.Rank.RankModel.TYPE_LEVEL, 21, 20, 0, 0L, 0, 20, 21));
                logs.Clear();
                Feed(m22101, BuildRankPage(Shenxiao.Module.Core.Rank.RankModel.TYPE_LEVEL, 41, 10, 0, 0L, 0, 10, 41));
                Shenxiao.Module.Core.Rank.RankModel.RankTypeData dataLevel =
                    model.GetData(Shenxiao.Module.Core.Rank.RankModel.TYPE_LEVEL);
                bool shortTailNotDroppedOk = dataLevel != null && dataLevel.Items.Count == 50; // 末页10条未被丢弃
                bool shortTailSumEchoOk = dataLevel != null && dataLevel.Sum == 10; // 存档的sum就是该页len(比前面小,纯回声)
                bool shortTailCompleteOk = dataLevel != null && dataLevel.Complete;
                bool shortTailNoResendOk = !logs.Exists(l => l.Contains("proto=" + Shenxiao.Framework.Net.Proto.RANK_QUERY));
                Debug.Log("CLIVERIFY rank 22101 LEVEL total=" + (dataLevel?.Items.Count ?? -1)
                    + " notDropped=" + shortTailNotDroppedOk + " sumEcho=" + shortTailSumEchoOk
                    + " complete=" + shortTailCompleteOk + " noResend=" + shortTailNoResendOk);

                // ---- F. 服务端回空列表 → 立即终止(不判 needMore,不炸) ----
                logs.Clear();
                Feed(m22101, new CliVerify.Pkt()
                    .I(Shenxiao.Module.Core.Rank.RankModel.TYPE_EQUIP).I(1).I(20).I(0).L(0L).I(0).I(0).H(0)
                    .Bytes());
                Shenxiao.Module.Core.Rank.RankModel.RankTypeData dataEquip =
                    model.GetData(Shenxiao.Module.Core.Rank.RankModel.TYPE_EQUIP);
                bool emptyStopOk = dataEquip != null && dataEquip.Items.Count == 0 && dataEquip.Complete
                    && !logs.Exists(l => l.Contains("proto=" + Shenxiao.Framework.Net.Proto.RANK_QUERY));
                Debug.Log("CLIVERIFY rank empty list stop=" + emptyStopOk);

                // ---- G. 未知 rank_type(不在配置表内)兜底单页终止不炸(ConfiguredMax 查不到默认0,首页收到
                // 即因 received≥ConfiguredMax 终止,不臆造总数、不续发) ----
                const int unknownType = 99999;
                logs.Clear();
                bool unknownNoThrow = true;
                try
                {
                    Feed(m22101, new CliVerify.Pkt()
                        .I(unknownType).I(1).I(20).I(0).L(0L).I(0).I(1).H(1)
                            .L(9201).I(0).AppendMinimalFigure("庚").L(123456L).L(654321L).I(0).I(0).I(1)
                        .Bytes());
                }
                catch (Exception e) { unknownNoThrow = false; Debug.LogError("CLIVERIFY rank unknown type threw: " + e); }
                Shenxiao.Module.Core.Rank.RankModel.RankTypeData dataUnknown = model.GetData(unknownType);
                bool unknownOk = unknownNoThrow && dataUnknown != null && dataUnknown.Items.Count == 1
                    && dataUnknown.Items[0].FirstValue == 654321L && dataUnknown.Complete
                    && !logs.Exists(l => l.Contains("proto=" + Shenxiao.Framework.Net.Proto.RANK_QUERY));
                Debug.Log("CLIVERIFY rank unknown type noThrow=" + unknownNoThrow + " ok=" + unknownOk);

                bool pass = visCountOk && visOrderOk && fightCfgOk && guildHiddenOk && medalLoadedOk
                    && err22100NoThrow && err22100Ok
                    && guardBlockedOk && guardLogOk && guardPassOk
                    && firstPageSentOk && fightMidPagesOk && fightLastPageOk && fightTotalOk
                    && selVal64Ok && tailSentinelOk
                    && shortTailNotDroppedOk && shortTailSumEchoOk && shortTailCompleteOk && shortTailNoResendOk
                    && emptyStopOk
                    && unknownNoThrow && unknownOk;

                Debug.Log("CLIVERIFY rank VERDICT pass=" + pass);
                model.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }

        /// <summary>按 FigureProto.SCHEMA 字段序逐项写一个全零/空的最小 Figure 块(与 ChatCase/FriendMailCase/
        /// TeamCase 的 AppendMinimalFigure 逐字节相同,独立文件各自持有一份,避免跨用例文件耦合)。
        /// 改 SCHEMA 顺序时所有副本必须同步。</summary>
        private static CliVerify.Pkt AppendMinimalFigure(this CliVerify.Pkt p, string name)
        {
            return p
                .S(name)  // name
                .C(0)     // sex
                .C(0)     // realm
                .C(0)     // career
                .H(0)     // level
                .C(0)     // GM
                .C(0)     // vip_flag
                .C(0)     // is_hide_vip
                .C(0)     // touxian
                .H(0)     // level_model_list count
                .H(0)     // fashion_model_list count
                .S("")    // picture
                .I(0)     // prcture_ver
                .L(0)     // guild_id
                .S("")    // guild_name
                .C(0)     // position
                .S("")    // position_name
                .I(0)     // dsgt_id
                .I(0)     // liveness_id
                .C(0)     // turn
                .C(0)     // turn_stage
                .C(0)     // grade_id
                .C(0)     // is_marriage
                .L(0)     // marriage_id
                .S("")    // marriage_name
                .I(0)     // escort_state
                .I(0)     // block_id
                .I(0)     // house_id
                .H(0)     // house_lv
                .H(0)     // figure_list count
                .H(0)     // figure_ride_list count
                .H(0)     // achv_lv
                .H(0)     // medal_id
                .I(0)     // fazhen_id
                .H(0)     // dress_list count
                .I(0)     // god_id
                .I(0)     // revelation_suit
                .I(0)     // demon_id
                .C(0)     // supreme_vip
                .I(0)     // title_id
                .C(0)     // mask_id
                .C(0)     // seaCamp
                .C(0)     // brick_id
                .C(0)     // dummy_type
                .C(0)     // suit_fashion_id
                .C(0);    // collect_state
        }

        /// <summary>按真实服务端形态(lib_common_rank_mod.erl:1220/1190)构造一页 22101 响应:Sum 字段位置
        /// 恒填 <paramref name="len"/>(请求 Len 的回声,非真实总数)——本轮 blocker 修复的核心断言依据即
        /// "sum 恒等于 len,不可当总数/续拉信号消费"。<paramref name="itemCount"/> 条 rank_list,Rank 从
        /// <paramref name="firstRank"/> 起连续编号,Figure.name="r"+rank 便于尾哨兵核对。</summary>
        private static byte[] BuildRankPage(int rankType, int start, int len, int roleRank, long selVal,
            int selSecVal, int itemCount, int firstRank)
        {
            CliVerify.Pkt p = new CliVerify.Pkt()
                .I(rankType).I(start).I(len).I(roleRank).L(selVal).I(selSecVal).I(len).H(itemCount);
            for (int i = 0; i < itemCount; i++)
            {
                int rank = firstRank + i;
                p = p.L(9000L + rank).I(0).AppendMinimalFigure("r" + rank)
                    .L(1000000000L - rank).L(500000L - rank).I(0).I(0).I(rank);
            }
            return p.Bytes();
        }
    }
}
