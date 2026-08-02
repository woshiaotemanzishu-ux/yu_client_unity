using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shenxiao.Editor.ProtocolCoverage;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 协议覆盖率核验器(PG 包,第21轮/R547):六段断言 A-F,照 CliVerify.cs 既有惯例,日志前缀
    /// "CLIVERIFY protocolcoverage"。纯静态分析 + 一次运行时反射,不建 Stage/不渲染。
    ///
    ///   A 总量防倒退:Unity 运行时已注册数 &gt;= baseline;降了或有具体号消失都算红,打印消失的号。
    ///   B 家族防倒退:逐前缀(协议号/100)已注册数不许降。
    ///   C 完工家族零未申报(防虚假完工正主):baseline 里 status=="done" 的家族,其「活缺口」必须
    ///     整体落在 killlist.json 里(且每条 killlist 记录必须带非空 evidence,否则不算数)。
    ///     status=="legacy_unverified" 的家族缺口只进报告,不挂红(裁决3)。
    ///   D 双注册检查:静态扫描 Unity C# 源码(运行时字典是覆盖语义,同号注册两次看不出来)。
    ///   E 族错误出口专项:老端「函数体除 Util.ErrorCodeShow 外无其它副作用」的协议号
    ///     (裁决7 收紧规则)里,未在 Unity 注册的数量不得比 baseline 记录的多(非回归,不强求清零——
    ///     清零是其它包的活,这里只守住不再变多)。
    ///   F 硬负约束防复发:hard_negative_constraints.json 中的协议号不得重新出现为运行时注册、
    ///     源码静态注册或 Proto 常量；清单缺失、重复或无 evidence 同样挂红。它不改变活缺口与
    ///     killlist 口径，只把 AGENTS 的现行禁止边界变成机器门禁。
    ///
    /// 收尾落 Reports/ProtocolCoverage/coverage_&lt;date&gt;.md + baseline.next.json(裁决5:
    /// 绝不自动覆盖 Schemas/ProtocolCoverage/baseline.json,基线上调必须人工确认后手动覆盖)。
    /// </summary>
    public static class ProtocolCoverageCase
    {
        public static Task<int> Run()
        {
            try
            {
                ProtocolCoverageScanner.ScanResult scan = ProtocolCoverageScanner.Scan();
                if (Environment.GetCommandLineArgs().Contains("-pgDumpDebug"))
                {
                    DumpDebugOldAll(scan);
                }
                CoverageBaseline baseline = ProtocolCoverageBaseline.LoadBaseline();
                List<KillEntry> killlist = ProtocolCoverageBaseline.LoadKilllist();
                List<HardNegativeConstraintEntry> hardNegativeConstraints =
                    ProtocolCoverageBaseline.LoadHardNegativeConstraints();

                var outcome = new AssertionOutcome();
                AssertA(scan, baseline, outcome);
                AssertB(scan, baseline, outcome);
                AssertC(scan, baseline, killlist, outcome);
                AssertD(scan, outcome);
                AssertE(scan, baseline, outcome);
                AssertF(scan, hardNegativeConstraints, outcome);

                CoverageBaseline candidate = ProtocolCoverageBaseline.BuildCandidate(scan, ProtocolCoverageReport.DenominatorNote(scan));
                string nextPath = ProtocolCoverageBaseline.WriteBaselineNext(candidate);
                string md = ProtocolCoverageReport.BuildMarkdown(scan, baseline ?? candidate, killlist, outcome);
                string reportPath = ProtocolCoverageReport.WriteReport(md);

                int oldAliveCount = scan.OldActiveKeys().Count;
                int oldDeadInAllCount = scan.OldAll.Count - oldAliveCount;
                Debug.Log("CLIVERIFY protocolcoverage registered=" + scan.UnityRegistered.Count
                    + " clientProtocolDefined=" + scan.ClientProtocolDefined.Count
                    + " liveDefined=" + scan.LiveDefinedSet().Count
                    + " liveGap=" + scan.LiveGap().Count
                    + " deadGap=" + scan.DeadGap().Count
                    + " handwrittenExtra=" + scan.HandwrittenExtra().Count
                    + " oldAllRaw=" + scan.OldAll.Count + " oldAlive=" + oldAliveCount + " oldDead=" + oldDeadInAllCount
                    + " oldCommentedOnlyCmds=" + scan.OldCommentedOnly.Count
                    + " hardNegativeConstraints=" + hardNegativeConstraints.Count
                    + " report=" + reportPath + " baselineNext=" + nextPath);
                foreach (string line in outcome.Lines) Debug.Log("CLIVERIFY protocolcoverage " + line);
                Debug.Log("CLIVERIFY protocolcoverage VERDICT pass=" + outcome.Pass);

                return Task.FromResult(outcome.Pass ? 0 : 3);
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY protocolcoverage EXCEPTION " + e);
                return Task.FromResult(1);
            }
        }

        /// <summary>独立 batchmode 入口(不依赖 CliVerify.cs 收编):
        ///   Unity.exe -batchmode -projectPath . -executeMethod Shenxiao.EditorTools.ProtocolCoverageCase.RunBatch
        /// 便于本包单跑/CI 单跑;主控收口时会按既有惯例把 Run() 接进 CliVerify.RenderAll。</summary>
        public static void RunBatch()
        {
            int code;
            try
            {
                Task<int> task = Run();
                code = task.Result;
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY protocolcoverage EXCEPTION " + e);
                code = 1;
            }
            Debug.Log("CLIVERIFY EXIT " + code);
            EditorApplication.Exit(code);
        }

        /// <summary>一次性诊断:把 OldAll 全量(cmd/alive/file:line)吐到 Reports/,便于跟 round20 python
        /// 脚本的 old_reg 结果做逐号 diff(见本轮排查 liveDefined 数字对不上的过程)。-pgDumpDebug 才触发,
        /// 不影响正常跑法。</summary>
        private static void DumpDebugOldAll(ProtocolCoverageScanner.ScanResult scan)
        {
            string dir = ProtocolCoverageSettings.REPORT_ROOT;
            System.IO.Directory.CreateDirectory(dir);
            var sb = new System.Text.StringBuilder();
            foreach (var kv in scan.OldAll.OrderBy(k => k.Key))
            {
                sb.AppendLine(kv.Key + "\t" + kv.Value.Alive + "\t" + kv.Value.File + ":" + kv.Value.Line + "\t" + kv.Value.DeadReason);
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "debug_oldall.txt"), sb.ToString());
        }

        private static void AssertA(ProtocolCoverageScanner.ScanResult scan, CoverageBaseline baseline, AssertionOutcome outcome)
        {
            int cur = scan.UnityRegistered.Count;
            if (baseline == null)
            {
                outcome.Add("A总量防倒退", true, "无 baseline.json,首次运行不挂红,当前注册=" + cur);
                return;
            }

            int prev = baseline.TotalUnityRegistered;
            List<int> missing = baseline.RegisteredCmds
                .Where(c => !scan.UnityRegistered.Contains(c))
                .OrderBy(c => c)
                .ToList();
            bool pass = cur >= prev && missing.Count == 0;
            string detail = "当前" + cur + " vs baseline" + prev;
            if (missing.Count > 0)
            {
                detail += ";消失的号(" + missing.Count + "):" + string.Join(",", missing.Take(50)) + (missing.Count > 50 ? "..." : "");
            }
            outcome.Add("A总量防倒退", pass, detail);
        }

        private static void AssertB(ProtocolCoverageScanner.ScanResult scan, CoverageBaseline baseline, AssertionOutcome outcome)
        {
            if (baseline == null)
            {
                outcome.Add("B家族防倒退", true, "无 baseline.json,首次运行不挂红");
                return;
            }

            Dictionary<int, int> curFamilies = scan.BuildFamilyTable().ToDictionary(f => f.Prefix, f => f.UnityRegistered);
            var regressed = new List<string>();
            foreach (FamilyBaseline fb in baseline.Families)
            {
                int cur = curFamilies.TryGetValue(fb.Prefix, out int v) ? v : 0;
                if (cur < fb.UnityRegistered) regressed.Add(fb.Prefix + ":" + cur + "<" + fb.UnityRegistered);
            }
            outcome.Add("B家族防倒退", regressed.Count == 0, regressed.Count == 0 ? "全部家族未降" : string.Join(";", regressed));
        }

        private static void AssertC(
            ProtocolCoverageScanner.ScanResult scan,
            CoverageBaseline baseline,
            List<KillEntry> killlist,
            AssertionOutcome outcome)
        {
            if (baseline == null)
            {
                outcome.Add("C完工家族零未申报", true, "无 baseline.json,首次运行不挂红");
                return;
            }

            var validKillSet = new HashSet<int>(
                killlist.Where(k => !string.IsNullOrWhiteSpace(k.Evidence)).Select(k => k.Cmd));
            var badKillEntries = killlist.Where(k => string.IsNullOrWhiteSpace(k.Evidence)).Select(k => k.Cmd).ToList();

            var offenders = new List<string>();
            foreach (FamilyBaseline fb in baseline.Families.Where(f => f.Status == "done"))
            {
                List<int> unkilled = scan.LiveGap()
                    .Where(c => ProtocolCoverageScanner.ScanResult.Family(c) == fb.Prefix && !validKillSet.Contains(c))
                    .OrderBy(c => c)
                    .ToList();
                if (unkilled.Count > 0)
                {
                    offenders.Add(fb.Prefix + ":未申报" + unkilled.Count + "(" + string.Join(",", unkilled) + ")");
                }
            }

            bool pass = offenders.Count == 0 && badKillEntries.Count == 0;
            string detail = pass
                ? "全部 done 家族的活缺口均在 killlist(含 evidence)内"
                : string.Join("; ", offenders) + (badKillEntries.Count > 0
                    ? (offenders.Count > 0 ? "; " : "") + "killlist 缺 evidence 的号(不算数):" + string.Join(",", badKillEntries)
                    : "");
            outcome.Add("C完工家族零未申报", pass, detail);
        }

        private static void AssertD(ProtocolCoverageScanner.ScanResult scan, AssertionOutcome outcome)
        {
            Dictionary<int, List<ProtocolCoverageScanner.DuplicateSite>> dups = scan.DuplicateRegistrations();
            bool pass = dups.Count == 0;
            string detail = pass
                ? "无双注册"
                : string.Join("; ", dups.Select(kv =>
                    kv.Key + "@" + string.Join(",", kv.Value.Select(s => s.File + ":" + s.Line))));
            outcome.Add("D双注册检查", pass, detail);
        }

        private static void AssertE(ProtocolCoverageScanner.ScanResult scan, CoverageBaseline baseline, AssertionOutcome outcome)
        {
            var unregistered = new HashSet<int>(scan.ErrorExitCandidates);
            unregistered.ExceptWith(scan.UnityRegistered);

            int baselineCount = baseline?.ErrorExitUnregisteredCount ?? unregistered.Count;
            bool firstRun = baseline == null;
            bool pass = firstRun || unregistered.Count <= baselineCount;
            string detail = "当前" + unregistered.Count + (firstRun ? "(无baseline,首次运行不挂红)" : " vs baseline" + baselineCount);
            if (unregistered.Count > 0) detail += ";号=" + string.Join(",", unregistered.OrderBy(x => x));
            outcome.Add("E族错误出口未注册非回归", pass, detail);
        }

        private static void AssertF(
            ProtocolCoverageScanner.ScanResult scan,
            List<HardNegativeConstraintEntry> constraints,
            AssertionOutcome outcome)
        {
            var malformed = constraints
                .Where(c => c.Cmd <= 0 || string.IsNullOrWhiteSpace(c.Rule) || string.IsNullOrWhiteSpace(c.Evidence))
                .Select(c => c.Cmd)
                .OrderBy(c => c)
                .ToList();
            var duplicates = constraints
                .GroupBy(c => c.Cmd)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .OrderBy(c => c)
                .ToList();
            var violations = new List<string>();

            foreach (HardNegativeConstraintEntry constraint in constraints
                .GroupBy(c => c.Cmd)
                .Select(g => g.First())
                .OrderBy(c => c.Cmd))
            {
                var locations = new List<string>();
                if (scan.UnityRegistered.Contains(constraint.Cmd)) locations.Add("runtime");
                if (scan.UnityProtocolConstants.Contains(constraint.Cmd)) locations.Add("ProtoConst");
                if (scan.UnityStaticSites.TryGetValue(
                    constraint.Cmd,
                    out List<ProtocolCoverageScanner.DuplicateSite> sites))
                {
                    locations.Add("static@" + string.Join(",", sites.Select(s => s.File + ":" + s.Line)));
                }

                if (locations.Count > 0)
                {
                    violations.Add(constraint.Cmd + "[" + string.Join("+", locations) + "]");
                }
            }

            bool pass = constraints.Count > 0
                && malformed.Count == 0
                && duplicates.Count == 0
                && violations.Count == 0;
            var details = new List<string>();
            if (constraints.Count == 0) details.Add("清单缺失或为空");
            if (malformed.Count > 0) details.Add("字段/evidence不完整:" + string.Join(",", malformed));
            if (duplicates.Count > 0) details.Add("重复协议号:" + string.Join(",", duplicates));
            if (violations.Count > 0) details.Add("违规出现:" + string.Join(";", violations));
            if (pass) details.Add(constraints.Count + "条约束均无常量或注册");
            outcome.Add("F硬负约束防复发", pass, string.Join(";", details));
        }
    }
}
