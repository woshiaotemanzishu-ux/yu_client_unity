using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Shenxiao.Editor.ProtocolCoverage
{
    /// <summary>coverage_&lt;date&gt;.md 报告生成。裁决2:对外只报活口径(B),全集口径(A,含 951 个
    /// 死号做分母)禁止对外报,这里报告抬头必须写清楚分母是什么——不给使用者留下"哪个百分比可以引用"的歧义。</summary>
    public static class ProtocolCoverageReport
    {
        public static string DenominatorNote(ProtocolCoverageScanner.ScanResult scan)
        {
            int liveDefined = scan.LiveDefinedSet().Count;
            int liveCovered = liveDefined - scan.LiveGap().Count;
            int handwritten = scan.HandwrittenExtra().Count;
            double pctB = liveDefined == 0 ? 0 : Math.Round(100.0 * liveCovered / liveDefined, 1);
            double pctC = (liveDefined + handwritten) == 0
                ? 0
                : Math.Round(100.0 * (liveCovered + handwritten) / (liveDefined + handwritten), 1);
            double pctA = scan.ClientProtocolDefined.Count == 0
                ? 0
                : Math.Round(100.0 * scan.UnityRegistered.Count / scan.ClientProtocolDefined.Count, 1);
            return $"对外只报口径B=活口径:Unity已覆盖{liveCovered}/活协议全集{liveDefined}={pctB}%" +
                   $"(脚注口径C=含手写:{liveCovered + handwritten}/{liveDefined + handwritten}={pctC}%;" +
                   $"口径A=全集口径{scan.UnityRegistered.Count}/{scan.ClientProtocolDefined.Count}={pctA}% 分母含死号,禁止对外报)";
        }

        public static string BuildMarkdown(
            ProtocolCoverageScanner.ScanResult scan,
            CoverageBaseline baseline,
            List<KillEntry> killlist,
            AssertionOutcome assertions)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 协议覆盖率核验报告 " + scan.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();
            sb.AppendLine("> " + DenominatorNote(scan));
            sb.AppendLine();
            sb.AppendLine("## 总量");
            sb.AppendLine();
            sb.AppendLine("| 指标 | 数值 |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| Unity 已注册(运行时真值) | {scan.UnityRegistered.Count} |");
            sb.AppendLine($"| ClientProtocol.json 全集 | {scan.ClientProtocolDefined.Count} |");
            sb.AppendLine($"| 老端活协议全集(定义∩老端活handler) | {scan.LiveDefinedSet().Count} |");
            sb.AppendLine($"| 活缺口(需要接) | {scan.LiveGap().Count} |");
            sb.AppendLine($"| 死号(粗口径) | {scan.DeadGap().Count} |");
            sb.AppendLine($"| 手写号(不在CP.json,场景/战斗) | {scan.HandwrittenExtra().Count} |");
            sb.AppendLine($"| 族错误出口候选未注册数 | {baseline.ErrorExitUnregisteredCount} |");
            if (baseline.TotalUnityRegistered > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"baseline 对比:注册 {scan.UnityRegistered.Count} vs baseline {baseline.TotalUnityRegistered}" +
                               $"(delta {scan.UnityRegistered.Count - baseline.TotalUnityRegistered:+0;-0;0})");
            }

            sb.AppendLine();
            sb.AppendLine("## 断言结果 A-G");
            sb.AppendLine();
            foreach (string line in assertions.Lines) sb.AppendLine("- " + line);

            sb.AppendLine();
            sb.AppendLine("## 家族表(前缀 = 协议号/100)");
            sb.AppendLine();
            sb.AppendLine("| 前缀 | Unity已注册 | 活缺口 | 死号 | 服务端路由 | baseline状态 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (ProtocolCoverageScanner.FamilyStat fs in scan.BuildFamilyTable().OrderByDescending(f => f.LiveGap))
            {
                FamilyBaseline fb = baseline.FindFamily(fs.Prefix);
                string status = fb?.Status ?? "(未入baseline)";
                string route = fs.ServerRouteTarget != null ? fs.ServerRouteTarget + "/" + fs.ServerRouteStatus : "NoRoute";
                sb.AppendLine($"| {fs.Prefix} | {fs.UnityRegistered} | {fs.LiveGap} | {fs.DeadGap} | {route} | {status} |");
            }

            sb.AppendLine();
            sb.AppendLine("## legacy_unverified 家族的未申报活缺口(报告级,不挂红,见裁决3)");
            sb.AppendLine();
            var killSet = new HashSet<int>(killlist.Select(k => k.Cmd));
            bool anyLegacy = false;
            foreach (FamilyBaseline fb in baseline.Families.Where(f => f.Status == "legacy_unverified"))
            {
                List<int> gapCmds = scan.LiveGap().Where(c => ProtocolCoverageScanner.ScanResult.Family(c) == fb.Prefix).OrderBy(c => c).ToList();
                List<int> unkilled = gapCmds.Where(c => !killSet.Contains(c)).ToList();
                if (unkilled.Count == 0) continue;
                anyLegacy = true;
                sb.AppendLine($"- 家族 {fb.Prefix}:未申报 {unkilled.Count} 个 -> {string.Join(",", unkilled)}");
            }
            if (!anyLegacy) sb.AppendLine("- (无)");

            return sb.ToString();
        }

        public static string WriteReport(string markdown)
        {
            string dir = ProtocolCoverageSettings.REPORT_ROOT;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "coverage_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".md");
            File.WriteAllText(path, markdown);
            return path;
        }
    }

    /// <summary>七段断言的人类可读结果行 + 总体是否通过。</summary>
    public sealed class AssertionOutcome
    {
        public readonly List<string> Lines = new List<string>();
        public bool Pass = true;

        public void Add(string tag, bool pass, string detail)
        {
            Lines.Add($"[{(pass ? "PASS" : "FAIL")}] {tag}: {detail}");
            if (!pass) Pass = false;
        }
    }
}
