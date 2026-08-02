using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Game;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 跨天/整点事件源(自动循环 轮20 ServerClock)实证:纯逻辑用例,不建 Stage/不渲染。
    ///
    /// 本轮的核心事实:老端 DAY_CHANGE/HOUR_REFRESH **不是本地 ticker 驱动**,而是服务端在 00:00:01 与
    /// 04:00:01 对每个在线玩家单播 10201 驱动(yu_server\src\server\mod_server_cast.erl:599/612;
    /// 老端 TryFireEvent 全仓唯一调用者是 ServerTimeModel.ts:39 的 InitServerTime)。所以本用例只要按
    /// 服务端 pt_102.erl:65-79 的权威字节序拼 10201 合成包、按时间线依次喂给 GameStartController.On10201,
    /// 就能完全确定性地驱动跨天判定——**不依赖真实等待,也不依赖 MonoBehaviour.Update**(轮1 教训)。
    ///
    /// 断言分段:
    ///   A 首包只写基线不触发(镜像老端 lastDay/lastHour 首次为空时短路,ServerTimeModel.ts:49/59);
    ///   B 跨过 0 点 → DAY_CHANGE 恰好 1 次;
    ///   C **裁决1 订正核心**:lastHour==0 时再到 4 点,本端 HOUR_REFRESH **必须触发**——老端
    ///     ServerTimeModel.ts:59 `if (this.lastHour && ...)` 用 truthy 判定,0 点把 lastHour 置 0(falsy)
    ///     会把整条 if 短路掉,导致跨夜在线玩家 4 点刷新永不触发;本端用 _hasLastHour 显式布尔订正。
    ///     这一段挂了就说明订正被回退成了老端 bug;
    ///   D 非 4 点整点不触发;E 同一小时重复收包不重复触发;F 同一包内 DAY+HOUR 可同时触发;
    ///   G 时区(裁决2):判 4 点用服务器时区 UTC+8,不是设备本地时区,也不是裸 UTC;
    ///   H **裁决5**:10000 只对时、只落 open_time,**绝不触发**任何跨天事件(老端 LoginController.ts:275-276
    ///     同一位置也不调 TryFireEvent);
    ///   I **ErlangParser 护栏**(裁决7 连带):喂 JSON 串不得死循环——⚠若护栏被回退,本段会挂死到
    ///     CliVerify 超时(EXIT 2)而非返回失败码,看到 serverclock 段超时优先查 ErlangParser.ParseList;
    ///     同段回归:合法 Erlang term 仍要正常解析(护栏不能误伤正常路径);
    ///   J config_key_value 落地 + key1 JSON 形态;K 41708 奖励明细解析(老端此路产出 1000 个空串垃圾对象,
    ///     本端订正为 JSON 解析);L 10205 负约束注册矩阵。
    ///
    /// 日志前缀统一 "CLIVERIFY serverclock"。独立文件复用 CliVerify.Pkt,不改 CliVerify.cs 本体。
    /// </summary>
    public static class ServerClockCase
    {
        private const BindingFlags FI = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags FS = BindingFlags.NonPublic | BindingFlags.Static;

        private static int _dayCount;
        private static int _hourCount;
        private static int _hourArg;
        private static int _refreshCount;

        private static void OnDay() { _dayCount++; }
        private static void OnHour(int h) { _hourCount++; _hourArg = h; }
        private static void OnRefresh() { _refreshCount++; }

        private static void ResetCounters()
        {
            _dayCount = 0;
            _hourCount = 0;
            _hourArg = -1;
            _refreshCount = 0;
        }

        /// <summary>把"服务器墙钟"的年月日时分换算成 unix 毫秒(10201 的 ServerTime 字段是毫秒,
        /// yu_server\src\game\lib_game.erl:79 utime:longunixtime();毫秒语义见 utime.erl:96-98)。</summary>
        private static long AtServerLocal(int y, int mo, int d, int h, int mi)
        {
            return new DateTimeOffset(y, mo, d, h, mi, 0,
                TimeSpan.FromHours(TimeUtil.SERVER_ZONE_HOURS)).ToUnixTimeMilliseconds();
        }

        /// <summary>10201 合成包,严格按 pt_102.erl:65-79:
        /// &lt;&lt;OpenTime:32, MergeTime:32, MergeStartTime:32, MergeCount:32, ServerTime:64&gt;&gt; 共 24 字节。</summary>
        private static byte[] Pkt10201(long openSec, long mergeSec, long mergeStartSec, int mergeCount, long serverMs)
        {
            return new CliVerify.Pkt().I(openSec).I(mergeSec).I(mergeStartSec).I(mergeCount).L(serverMs).Bytes();
        }

        /// <summary>10000 合成包,严格按 pt_100.erl:75-82:
        /// &lt;&lt;Career:8, Time:64, OpenTime:32, N:16, RegPlayerNum:32&gt;&gt; + N 个角色(此处 N=0)。
        /// 注意 wire 上 N:16 排在 RegPlayerNum:32 **之前**(老端 ReadFmt("clihi") 印证)。</summary>
        private static byte[] Pkt10000(long serverMs, long openSec)
        {
            return new CliVerify.Pkt().C(1).L(serverMs).I(openSec).H(0).I(0).Bytes();
        }

        public static async Task<int> Run()
        {
            int fail = 0;

            GameStartController gsc = GameStartController.Instance;
            bool gameStartWasInitialized = gsc.IsInitialized;
            gsc.Init();
            MethodInfo m10201 = gsc.GetType().GetMethod("On10201", FI);
            MethodInfo m10205 = gsc.GetType().GetMethod("On10205", FI);
            object lc = Shenxiao.Module.Core.Login.LoginController.Instance;
            MethodInfo m10000 = lc.GetType().GetMethod("OnAccountLogin", FI);
            object wc = Shenxiao.Module.Core.Welfare.WelfareController.Instance;
            MethodInfo mParseGift = wc.GetType().GetMethod("ParseDownloadGiftReward", FS);
            if (m10201 == null || m10205 != null || m10000 == null || mParseGift == null)
            {
                Debug.LogError("CLIVERIFY serverclock handler missing(reflection): On10201="
                    + (m10201 != null) + " On10205Absent=" + (m10205 == null)
                    + " OnAccountLogin=" + (m10000 != null)
                    + " ParseDownloadGiftReward=" + (mParseGift != null));
                if (!gameStartWasInitialized) gsc.Dispose();
                return 3;
            }

            void Feed10201(long openSec, long mergeSec, long mergeStartSec, int mergeCount, long serverMs)
            {
                byte[] pkt = Pkt10201(openSec, mergeSec, mergeStartSec, mergeCount, serverMs);
                m10201.Invoke(gsc, new object[] { new NetReader(pkt, 0, pkt.Length) });
            }

            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnDay);
            EventDispatcher.On<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnHour);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnRefresh);
            try
            {
                ServerTimeModel.Reset();
                ResetCounters();

                // 开服 0 点 = 2026-07-01 00:00(服务器墙钟)。open_time 语义见 record.hrl:36「开服当天0点」。
                long openSec = AtServerLocal(2026, 7, 1, 0, 0) / 1000L;
                const long MERGE_SEC = 0L;        // 未合服
                const long MERGE_START_SEC = 0L;
                const int MERGE_COUNT = 3;

                // ---- A 首包:只写基线,不触发 DAY/HOUR;但 REFRESH 每包必发 ----
                Feed10201(openSec, MERGE_SEC, MERGE_START_SEC, MERGE_COUNT, AtServerLocal(2026, 7, 10, 23, 0));
                bool aOk = _dayCount == 0 && _hourCount == 0 && _refreshCount == 1
                           && ServerTimeModel.OpenTime == openSec
                           && ServerTimeModel.MergeTime == MERGE_SEC
                           && ServerTimeModel.MergeStartTime == MERGE_START_SEC
                           && ServerTimeModel.MergeCount == MERGE_COUNT;
                if (!aOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock A 首包基线失败 day=" + _dayCount + " hour=" + _hourCount
                        + " refresh=" + _refreshCount + " open=" + ServerTimeModel.OpenTime + "/" + openSec
                        + " mergeTime=" + ServerTimeModel.MergeTime + " mergeStart=" + ServerTimeModel.MergeStartTime
                        + " mergeCount=" + ServerTimeModel.MergeCount);
                }

                // ---- B 跨 0 点:DAY_CHANGE 恰 1 次;lastHour 23→0 不该命中 4 点 ----
                ResetCounters();
                Feed10201(openSec, MERGE_SEC, MERGE_START_SEC, MERGE_COUNT, AtServerLocal(2026, 7, 11, 0, 30));
                bool bOk = _dayCount == 1 && _hourCount == 0 && _refreshCount == 1;
                if (!bOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock B 跨天失败 day=" + _dayCount + "(期望1) hour="
                        + _hourCount + "(期望0) refresh=" + _refreshCount);
                }

                // ---- C 裁决1 订正核心:lastHour==0 再到 4 点,必须触发 HOUR_REFRESH(老端此处不触发)----
                ResetCounters();
                Feed10201(openSec, MERGE_SEC, MERGE_START_SEC, MERGE_COUNT, AtServerLocal(2026, 7, 11, 4, 0));
                bool cOk = _hourCount == 1 && _hourArg == 4 && _dayCount == 0;
                if (!cOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock C 裁决1订正失败(lastHour=0→4点必须触发,老端 truthy bug 已订正)"
                        + " hour=" + _hourCount + "(期望1) hourArg=" + _hourArg + "(期望4) day=" + _dayCount + "(期望0)");
                }

                // ---- D 非 4 点整点不触发 ----
                ResetCounters();
                Feed10201(openSec, MERGE_SEC, MERGE_START_SEC, MERGE_COUNT, AtServerLocal(2026, 7, 11, 5, 0));
                bool dOk = _hourCount == 0 && _dayCount == 0;
                if (!dOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock D 非4点误触发 hour=" + _hourCount + " day=" + _dayCount);
                }

                // ---- E 同一小时重复收包不重复触发(镜像老端 ServerTimeModel.ts:56-57 早退)----
                ResetCounters();
                Feed10201(openSec, MERGE_SEC, MERGE_START_SEC, MERGE_COUNT, AtServerLocal(2026, 7, 11, 5, 40));
                bool eOk = _hourCount == 0 && _dayCount == 0 && _refreshCount == 1;
                if (!eOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock E 同小时重复触发 hour=" + _hourCount + " day=" + _dayCount);
                }

                // ---- F 同一包内 DAY + HOUR 同时触发(次日 4 点)----
                ResetCounters();
                Feed10201(openSec, MERGE_SEC, MERGE_START_SEC, MERGE_COUNT, AtServerLocal(2026, 7, 12, 4, 0));
                bool fOk = _dayCount == 1 && _hourCount == 1 && _hourArg == 4;
                if (!fOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock F 次日4点双触发失败 day=" + _dayCount + "(期望1) hour="
                        + _hourCount + "(期望1) hourArg=" + _hourArg);
                }

                // ---- G 裁决2 时区:判 4 点走服务器时区(UTC+8),不是裸 UTC、不是设备本地时区 ----
                DateTime utc = TimeUtil.NowUtc();
                DateTime srv = TimeUtil.NowServerLocal();
                bool gOk = TimeUtil.SERVER_ZONE_HOURS == 8
                           && Math.Abs((srv - utc).TotalHours - 8.0) < 0.01
                           && srv.Hour == 4; // 刚喂的是服务器墙钟 04:00
                if (!gOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock G 时区失败 zone=" + TimeUtil.SERVER_ZONE_HOURS
                        + " utcHour=" + utc.Hour + " srvHour=" + srv.Hour + "(期望4) diff="
                        + (srv - utc).TotalHours);
                }

                // ---- H 裁决5:10000 只对时 + 只落 open_time,绝不触发跨天事件 ----
                ResetCounters();
                long newOpenSec = AtServerLocal(2026, 6, 20, 0, 0) / 1000L; // 故意换个 open_time
                long farMs = AtServerLocal(2026, 8, 1, 4, 0);               // 故意跳到远期 + 恰好 4 点
                byte[] p10000 = Pkt10000(farMs, newOpenSec);
                m10000.Invoke(lc, new object[] { new NetReader(p10000, 0, p10000.Length) });
                bool hOk = _dayCount == 0 && _hourCount == 0 && _refreshCount == 0
                           && ServerTimeModel.OpenTime == newOpenSec
                           && ServerTimeModel.MergeCount == MERGE_COUNT   // 另三项原样保留,不被 10000 清掉
                           && Math.Abs(TimeUtil.NowMs() - farMs) < 5000;  // 确实对了时
                if (!hOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock H 裁决5失败(10000 应只对时不触发事件) day=" + _dayCount
                        + " hour=" + _hourCount + " refresh=" + _refreshCount + " open=" + ServerTimeModel.OpenTime
                        + "/" + newOpenSec + " mergeCount=" + ServerTimeModel.MergeCount
                        + " nowMs-far=" + (TimeUtil.NowMs() - farMs));
                }

                // ---- I ErlangParser 护栏:喂 JSON 不死循环 + 合法 Erlang term 不误伤 ----
                // ⚠护栏若被回退,下一行会无限循环,本段表现为"整个 serverclock 用例超时",不是返回失败码。
                ErlangTerm jsonFed = ErlangParser.Parse("[{\"0\":0,\"1\":35,\"2\":200}]");
                ErlangTerm realTerm = ErlangParser.Parse("[{0,1102015063,1}]");
                bool iOk = jsonFed != null
                           && realTerm != null && realTerm.Items != null && realTerm.Items.Count == 1
                           && realTerm.Items[0].Items != null && realTerm.Items[0].Items.Count == 3
                           && realTerm.Items[0].Get<int>(0) == 0
                           && realTerm.Items[0].Get<long>(1) == 1102015063L
                           && realTerm.Items[0].Get<int>(2) == 1;
                if (!iOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock I ErlangParser 失败 jsonFed=" + (jsonFed != null)
                        + " realTermItems=" + (realTerm?.Items?.Count ?? -1));
                }

                // ---- J config_key_value 落地 + key1 形态 ----
                await KeyValueConfigs.EnsureLoaded();
                string key1 = KeyValueConfigs.GetRaw(1);
                bool jParsed = false;
                int jArrN = -1;
                try
                {
                    if (JToken.Parse(key1 ?? string.Empty) is JArray arr) { jParsed = true; jArrN = arr.Count; }
                }
                catch (Exception) { jParsed = false; }
                bool jOk = KeyValueConfigs.IsLoaded && KeyValueConfigs.Count == 59
                           && !string.IsNullOrEmpty(key1) && jParsed && jArrN == 3
                           && KeyValueConfigs.GetName(1) == "下载礼包奖励";
                if (!jOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock J config_key_value 失败 loaded=" + KeyValueConfigs.IsLoaded
                        + " count=" + KeyValueConfigs.Count + "(期望59) key1Parsed=" + jParsed + " arrN=" + jArrN
                        + "(期望3) name1=" + KeyValueConfigs.GetName(1));
                }

                // ---- K 41708 奖励明细:按 config_key_value[1] 解出三条(老端此路产出 1000 个空串垃圾)----
                var gift = mParseGift.Invoke(null, null) as List<(int style, int typeId, long count)>;
                bool kOk = gift != null && gift.Count == 3
                           && gift[0].style == 0 && gift[0].typeId == 35 && gift[0].count == 200L
                           && gift[1].typeId == 16020002 && gift[1].count == 1L
                           && gift[2].typeId == 17020002 && gift[2].count == 1L;
                if (!kOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock K 41708 明细失败 n=" + (gift?.Count ?? -1) + "(期望3) "
                        + (gift != null && gift.Count > 0
                            ? "[0]=" + gift[0].style + "/" + gift[0].typeId + "/" + gift[0].count
                            : "(空)"));
                }

                // ---- L R541 纠偏:10205 晚于 KfStage 原负约束的增量必须保持无 handler/无注册 ----
                var handlers = typeof(NetManager).GetField("_handlers", FS)?.GetValue(null) as System.Collections.IDictionary;
                bool lOk = handlers != null && !handlers.Contains(10205) && m10205 == null;
                if (!lOk)
                {
                    fail++;
                    Debug.LogError("CLIVERIFY serverclock L 10205 负约束失败 registered="
                        + (handlers != null && handlers.Contains(10205)) + " handlerAbsent=" + (m10205 == null));
                }

                Debug.Log("CLIVERIFY serverclock A=" + aOk + " B=" + bOk + " C=" + cOk + " D=" + dOk
                    + " E=" + eOk + " F=" + fOk + " G=" + gOk + " H=" + hOk + " I=" + iOk + " J=" + jOk
                    + " K=" + kOk + " L=" + lOk + " fail=" + fail);
            }
            finally
            {
                EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnDay);
                EventDispatcher.Off<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnHour);
                EventDispatcher.Off(GlobalEvent.EVT_SERVER_TIME_REFRESH, OnRefresh);
                ServerTimeModel.Reset();
                // ⚠必须把全局服务器时钟还原,否则本用例会把 TimeUtil 的钟留在 2026-08-01(H 段喂的远期时间戳),
                // 污染同一次 RenderAll 里后续用例的时间判定。本轮实测教训:DailyHubCase 的 15718 期望值就是因为
                // "期望用设备钟 DateTime.UtcNow、生产用服务端同步钟 TimeUtil.NowUtc()"两钟分叉而假失败
                // ——跨用例时钟污染是这类假失败的放大器,谁操纵了全局时钟谁负责还原(ChatCase 同款纪律)。
                TimeUtil.SyncServerTime(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                if (!gameStartWasInitialized) gsc.Dispose();
            }

            return fail == 0 ? 0 : 1;
        }
    }
}
