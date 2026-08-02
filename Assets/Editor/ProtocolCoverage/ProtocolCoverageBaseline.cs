using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Shenxiao.Editor.ProtocolCoverage
{
    /// <summary>baseline.json 的族级条目。status 三态:
    ///   done             = 断言 C 生效,该族「活缺口」必须 ⊆ killlist(每条带 evidence),否则红。
    ///   legacy_unverified = 历史 [x] 包仍有未落入有效killlist/hard-negative的缺口,报告列出并逐包消化。
    ///   pending           = 从未被任何包声明过 done,C 不对它生效(纯观察)。
    /// status 是人工策展字段——工具产出的 baseline.next.json 原样保留正式基线的 status/statusNote，
    /// 并把「零活缺口或全部缺口均有 evidence-killlist」算出的二态建议另写入 suggestedStatus。
    /// legacy_unverified 的判定与正式状态变更必须人工核对后才落进 baseline.json；当全部缺口已被
    /// killlist/hard-negative分类且至少一项仍为hard-negative时，应转pending而不是done
    /// (裁决5:Case 绝不自动覆盖正式 baseline)。</summary>
    public sealed class FamilyBaseline
    {
        [JsonProperty("prefix")] public int Prefix;
        [JsonProperty("unityRegistered")] public int UnityRegistered;
        [JsonProperty("liveGap")] public int LiveGap;
        /// <summary>候选基线的逐号活缺口，便于后续审计直接选号；历史正式baseline缺此字段也可兼容读取。</summary>
        [JsonProperty("liveGapCmds")] public List<int> LiveGapCmds = new List<int>();
        [JsonProperty("status")] public string Status = "pending";
        /// <summary>人工策展说明。正式baseline已有字段此前被模型静默丢弃，候选文件必须原样保留。</summary>
        [JsonProperty("statusNote", NullValueHandling = NullValueHandling.Ignore)] public string StatusNote;
        /// <summary>候选文件的机器建议，不覆盖人工status；正式baseline可不含此字段。</summary>
        [JsonProperty("suggestedStatus", NullValueHandling = NullValueHandling.Ignore)] public string SuggestedStatus;
    }

    public sealed class CoverageBaseline
    {
        [JsonProperty("generatedAt")] public string GeneratedAt;
        [JsonProperty("denominatorNote")] public string DenominatorNote;
        [JsonProperty("totalUnityRegistered")] public int TotalUnityRegistered;
        [JsonProperty("totalClientProtocolDefined")] public int TotalClientProtocolDefined;
        [JsonProperty("totalLiveDefined")] public int TotalLiveDefined;
        [JsonProperty("totalLiveGap")] public int TotalLiveGap;
        [JsonProperty("totalDeadGap")] public int TotalDeadGap;
        [JsonProperty("liveCoveragePercent")] public double LiveCoveragePercent;
        [JsonProperty("errorExitUnregisteredCount")] public int ErrorExitUnregisteredCount;
        /// <summary>冻结时未注册的族错误出口逐号清单(升序),与计数共同防止门槛被调大或换号。</summary>
        [JsonProperty("errorExitUnregisteredCmds")] public List<int> ErrorExitUnregisteredCmds = new List<int>();
        /// <summary>本次快照的 Unity 已注册号全集(升序),供断言 A 在总量倒退时精确打印「消失的号」用。</summary>
        [JsonProperty("registeredCmds")] public List<int> RegisteredCmds = new List<int>();
        [JsonProperty("families")] public List<FamilyBaseline> Families = new List<FamilyBaseline>();

        public FamilyBaseline FindFamily(int prefix) => Families.FirstOrDefault(f => f.Prefix == prefix);
    }

    /// <summary>killlist.json 一条 = 一个被判定「不必登记」的协议号(死号 / 团队裁决跳过)。
    /// evidence 强制填(文件:行号,或明确的裁决出处),这是整个防复发机制的支点——
    /// 没有它,"判死"就只是嘴说,C 段就没法机检。clientMode 默认 absent；只有 C2S 真实可达、
    /// 但 S2C handler 判死的半死号才能显式标 send_only 并保留 Proto 发送常量。</summary>
    public sealed class KillEntry
    {
        public const string CLIENT_MODE_ABSENT = "absent";
        public const string CLIENT_MODE_SEND_ONLY = "send_only";

        [JsonProperty("cmd")] public int Cmd;
        [JsonProperty("reason")] public string Reason; // dead_empty_body | dead_no_old_handler | skip_decision | legacy_unverified_seed
        [JsonProperty("evidence")] public string Evidence; // "文件:行号" 或裁决出处
        [JsonProperty("note")] public string Note;
        [JsonProperty("clientMode")] public string ClientMode = CLIENT_MODE_ABSENT;
    }

    /// <summary>当前产品边界明确禁止出现在 Unity 协议层的协议号。
    /// 与 killlist 不同：这些号可以是服务端真实活协议或延期事务，仍保留在活缺口分母；
    /// 本表只防止在原负约束尚未被明确改写时重新加入常量或注册。</summary>
    public sealed class HardNegativeConstraintEntry
    {
        [JsonProperty("cmd")] public int Cmd;
        [JsonProperty("rule")] public string Rule;
        [JsonProperty("evidence")] public string Evidence;
        [JsonProperty("note")] public string Note;
    }

    public static class ProtocolCoverageBaseline
    {
        public static CoverageBaseline LoadBaseline()
        {
            string path = ProtocolCoverageSettings.BASELINE_PATH;
            if (!File.Exists(path)) return null;
            return JsonConvert.DeserializeObject<CoverageBaseline>(File.ReadAllText(path));
        }

        public static List<KillEntry> LoadKilllist()
        {
            string path = ProtocolCoverageSettings.KILLLIST_PATH;
            if (!File.Exists(path)) return new List<KillEntry>();
            List<KillEntry> list = JsonConvert.DeserializeObject<List<KillEntry>>(File.ReadAllText(path));
            return list ?? new List<KillEntry>();
        }

        public static List<HardNegativeConstraintEntry> LoadHardNegativeConstraints()
        {
            string path = ProtocolCoverageSettings.HARD_NEGATIVE_CONSTRAINTS_PATH;
            if (!File.Exists(path)) return new List<HardNegativeConstraintEntry>();
            List<HardNegativeConstraintEntry> list =
                JsonConvert.DeserializeObject<List<HardNegativeConstraintEntry>>(File.ReadAllText(path));
            return list ?? new List<HardNegativeConstraintEntry>();
        }

        /// <summary>从本次扫描生成候选基线。正式基线的人工 status/statusNote 原样保留；
        /// suggestedStatus 只按当前逐号缺口是否全部由带 evidence 的 killlist 治理机械计算，
        /// 供人工核对而不替代策展裁决。</summary>
        public static CoverageBaseline BuildCandidate(
            ProtocolCoverageScanner.ScanResult scan,
            string denominatorNote,
            CoverageBaseline curatedBaseline,
            IReadOnlyCollection<KillEntry> killlist)
        {
            var baseline = new CoverageBaseline
            {
                GeneratedAt = scan.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                DenominatorNote = denominatorNote,
                TotalUnityRegistered = scan.UnityRegistered.Count,
                TotalClientProtocolDefined = scan.ClientProtocolDefined.Count,
                TotalLiveDefined = scan.LiveDefinedSet().Count,
                TotalLiveGap = scan.LiveGap().Count,
                TotalDeadGap = scan.DeadGap().Count,
            };
            baseline.LiveCoveragePercent = baseline.TotalLiveDefined == 0
                ? 0.0
                : System.Math.Round(100.0 * (baseline.TotalLiveDefined - baseline.TotalLiveGap) / baseline.TotalLiveDefined, 1);

            HashSet<int> unregisteredErrorExits = new HashSet<int>(scan.ErrorExitCandidates);
            unregisteredErrorExits.ExceptWith(scan.UnityRegistered);
            baseline.ErrorExitUnregisteredCount = unregisteredErrorExits.Count;
            baseline.ErrorExitUnregisteredCmds = unregisteredErrorExits.OrderBy(c => c).ToList();

            baseline.RegisteredCmds = scan.UnityRegistered.OrderBy(c => c).ToList();
            var validKillSet = new HashSet<int>((killlist ?? new List<KillEntry>())
                .Where(k => !string.IsNullOrWhiteSpace(k.Evidence))
                .Select(k => k.Cmd));

            foreach (ProtocolCoverageScanner.FamilyStat fs in scan.BuildFamilyTable())
            {
                FamilyBaseline curated = curatedBaseline?.FindFamily(fs.Prefix);
                string suggestedStatus = fs.LiveGapCmds.All(validKillSet.Contains) ? "done" : "pending";
                baseline.Families.Add(new FamilyBaseline
                {
                    Prefix = fs.Prefix,
                    UnityRegistered = fs.UnityRegistered,
                    LiveGap = fs.LiveGap,
                    LiveGapCmds = fs.LiveGapCmds.ToList(),
                    Status = curated?.Status ?? suggestedStatus,
                    StatusNote = curated?.StatusNote,
                    SuggestedStatus = suggestedStatus,
                });
            }

            return baseline;
        }

        /// <summary>裁决5:Case 绝不自动覆盖 baseline.json,只写候选文件(Reports/ 已整体 gitignore,
        /// 不会污染工作区)。基线上调必须人确认后手动覆盖 Schemas/ProtocolCoverage/baseline.json。</summary>
        public static string WriteBaselineNext(CoverageBaseline baseline)
        {
            string dir = ProtocolCoverageSettings.REPORT_ROOT;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "baseline.next.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(baseline, Formatting.Indented));
            return path;
        }
    }
}
