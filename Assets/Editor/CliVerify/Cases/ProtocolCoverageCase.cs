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
    /// 协议覆盖率核验器(PG 包,第21轮/R547-R564):九段断言 A-I,照 CliVerify.cs 既有惯例,日志前缀
    /// "CLIVERIFY protocolcoverage"。纯静态分析 + 一次运行时反射,不建 Stage/不渲染。
    ///
    ///   A 总量防倒退:Unity 运行时已注册数 &gt;= baseline；baseline.registeredCmds 必须与历史总量
    ///     等长、五位、唯一且严格升序；顶层总量必须能分解为活覆盖/活缺口/非活已注册/死缺口/手写，
    ///     denominatorNote 必须由该冻结分区精确生成。具体号消失默认算红，只有带 evidence 的
    ///     killlist/硬负约束裁决可按号豁免，且打印全部消失号。
    ///   B 家族防倒退:冻结家族unityRegistered必须与registeredCmds逐前缀分组精确一致；各家族历史
    ///     liveGap 必须非负且汇总精确等于顶层 totalLiveGap，顶层值不得越过 totalLiveDefined；冻结的
    ///     liveCoveragePercent 必须等于两项总量按一位小数推导的百分比；当前逐前缀已注册数不许降，
    ///     同一证据裁决号仅补偿所属家族。
    ///   C 完工家族零未申报(防虚假完工正主):baseline 里 status=="done" 的家族,其「活缺口」必须
    ///     整体落在 killlist.json 里(且每条 killlist 记录必须带非空 evidence,否则不算数)。
    ///     status=="legacy_unverified" 的家族缺口只进报告,不挂红(裁决3)。
    ///   D 双注册检查:静态扫描 Unity C# 源码(运行时字典是覆盖语义,同号注册两次看不出来)。
    ///   E 族错误出口专项:老端「函数体除 Util.ErrorCodeShow 外无其它副作用」的协议号
    ///     (裁决7 收紧规则)里,未在 Unity 注册的数量不得比 baseline 记录的多(非回归,不强求清零——
    ///     清零是其它包的活,这里只守住不再变多)；冻结逐号清单必须完整，当前或按历史注册清单
    ///     重建出的候选不得出现清单外新号。
    ///   F 硬负约束防复发:hard_negative_constraints.json 中的五位协议号不得重新出现为运行时注册、
    ///     源码静态注册、Proto 常量、具名发送或五位数字直发，也不得与killlist重叠；每号必须仍属于当前liveGap，
    ///     清单缺失、重复、陈旧或无 evidence 同样挂红。它不改变活缺口与killlist口径，只把 AGENTS 的现行禁止
    ///     边界变成机器门禁。
    ///   G killlist 防复活:killlist 必须非空且每条为五位cmd并带reason/evidence；其中协议号不得拥有
    ///     运行时 handler。只有显式 clientMode=send_only 的 C2S 单向操作允许且必须保留 Proto 常量及
    ///     生产 `Send*(Proto.X,...)` 直接引用，其余死号不得残留常量或发送引用；模式非法或清单重复也挂红。
    ///   H baseline状态收口:当前零活缺口、或剩余活缺口已全部由带evidence的killlist治理的家族，
    ///     正式baseline必须人工标为done；家族重复、未知status、漏入当前家族，或仍有活缺口的done族
    ///     缺少statusNote同样挂红。
    ///   I 候选基线完整性:baseline.next 的顶层机器计数、注册/错误出口逐号清单与家族机器字段必须
    ///     精确等于当前扫描；正式基线家族不得静默丢失，人工status/statusNote必须逐族原样保留，
    ///     suggestedStatus必须与当前liveGap/有效killlist机械判断一致。
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
                AssertA(scan, baseline, killlist, hardNegativeConstraints, outcome);
                AssertB(scan, baseline, killlist, hardNegativeConstraints, outcome);
                AssertC(scan, baseline, killlist, outcome);
                AssertD(scan, outcome);
                AssertE(scan, baseline, outcome);
                AssertF(scan, hardNegativeConstraints, killlist, outcome);
                AssertG(scan, killlist, outcome);
                AssertH(scan, baseline, killlist, outcome);

                CoverageBaseline candidate = ProtocolCoverageBaseline.BuildCandidate(
                    scan,
                    ProtocolCoverageReport.DenominatorNote(scan),
                    baseline,
                    killlist);
                AssertI(scan, baseline, candidate, killlist, outcome);
                string nextPath = ProtocolCoverageBaseline.WriteBaselineNext(candidate);
                string md = ProtocolCoverageReport.BuildMarkdown(
                    scan,
                    baseline ?? candidate,
                    killlist,
                    hardNegativeConstraints,
                    outcome);
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

        private static void AssertA(
            ProtocolCoverageScanner.ScanResult scan,
            CoverageBaseline baseline,
            List<KillEntry> killlist,
            List<HardNegativeConstraintEntry> hardNegativeConstraints,
            AssertionOutcome outcome)
        {
            int cur = scan.UnityRegistered.Count;
            if (baseline == null)
            {
                outcome.Add("A总量防倒退", true, "无 baseline.json,首次运行不挂红,当前注册=" + cur);
                return;
            }

            int prev = baseline.TotalUnityRegistered;
            List<int> baselineCmds = baseline.RegisteredCmds ?? new List<int>();
            List<int> duplicateBaselineCmds = baselineCmds
                .GroupBy(c => c)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .OrderBy(c => c)
                .ToList();
            List<int> invalidBaselineCmds = baselineCmds
                .Where(c => c < 10000 || c > 99999)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            bool strictlyIncreasing = baselineCmds
                .Zip(baselineCmds.Skip(1), (left, right) => left < right)
                .All(inOrder => inOrder);
            bool baselineCmdsValid = baselineCmds.Count == prev
                && duplicateBaselineCmds.Count == 0
                && invalidBaselineCmds.Count == 0
                && strictlyIncreasing;
            long historicalLiveCovered = (long)baseline.TotalLiveDefined - baseline.TotalLiveGap;
            long historicalDefinedRegistered = (long)baseline.TotalClientProtocolDefined
                - baseline.TotalLiveGap - baseline.TotalDeadGap;
            long historicalNonLiveRegistered = (long)baseline.TotalClientProtocolDefined
                - baseline.TotalLiveDefined - baseline.TotalDeadGap;
            long historicalHandwritten = (long)baseline.TotalUnityRegistered - historicalDefinedRegistered;
            bool baselinePartitionValid = baseline.TotalUnityRegistered >= 0
                && baseline.TotalClientProtocolDefined >= 0
                && baseline.TotalLiveDefined >= 0
                && baseline.TotalLiveDefined <= baseline.TotalClientProtocolDefined
                && baseline.TotalLiveGap >= 0
                && baseline.TotalLiveGap <= baseline.TotalLiveDefined
                && baseline.TotalDeadGap >= 0
                && baseline.TotalDeadGap <= (long)baseline.TotalClientProtocolDefined - baseline.TotalLiveDefined
                && historicalDefinedRegistered >= 0
                && historicalDefinedRegistered <= baseline.TotalUnityRegistered;
            string expectedDenominatorNote = baselinePartitionValid
                ? ProtocolCoverageReport.DenominatorNote(
                    baseline.TotalUnityRegistered,
                    baseline.TotalClientProtocolDefined,
                    baseline.TotalLiveDefined,
                    baseline.TotalLiveGap,
                    (int)historicalHandwritten)
                : string.Empty;
            bool denominatorNoteValid = baselinePartitionValid
                && string.Equals(baseline.DenominatorNote, expectedDenominatorNote, StringComparison.Ordinal);
            HashSet<int> sanctionedRemovals = BuildSanctionedRemovalSet(killlist, hardNegativeConstraints);
            List<int> allMissing = baselineCmds
                .Where(c => !scan.UnityRegistered.Contains(c))
                .OrderBy(c => c)
                .ToList();
            List<int> missing = allMissing.Where(c => !sanctionedRemovals.Contains(c)).ToList();
            List<int> sanctionedMissing = allMissing.Where(sanctionedRemovals.Contains).ToList();
            bool pass = baselineCmdsValid
                && baselinePartitionValid
                && denominatorNoteValid
                && cur >= prev
                && missing.Count == 0;
            string detail = "当前" + cur + " vs baseline" + prev;
            if (baselineCmds.Count != prev)
                detail += ";registeredCmds计数" + baselineCmds.Count + "!=历史总量" + prev;
            if (duplicateBaselineCmds.Count > 0)
                detail += ";registeredCmds重复:" + string.Join(",", duplicateBaselineCmds);
            if (invalidBaselineCmds.Count > 0)
                detail += ";registeredCmds非五位:" + string.Join(",", invalidBaselineCmds);
            if (!strictlyIncreasing) detail += ";registeredCmds非严格升序";
            if (!baselinePartitionValid)
            {
                detail += ";baseline冻结分区非法:client=" + baseline.TotalClientProtocolDefined
                    + ",live=" + baseline.TotalLiveDefined
                    + ",liveGap=" + baseline.TotalLiveGap
                    + ",deadGap=" + baseline.TotalDeadGap
                    + ",definedRegistered=" + historicalDefinedRegistered
                    + ",handwritten=" + historicalHandwritten;
            }
            else if (!denominatorNoteValid)
            {
                detail += ";denominatorNote与冻结总量不一致";
            }
            if (missing.Count > 0)
            {
                detail += ";消失的号(" + missing.Count + "):" + string.Join(",", missing.Take(50)) + (missing.Count > 50 ? "..." : "");
            }
            if (sanctionedMissing.Count > 0)
            {
                detail += ";有证据裁决移除(" + sanctionedMissing.Count + "):" + string.Join(",", sanctionedMissing);
            }
            if (baselineCmdsValid) detail += ";baseline注册号清单" + baselineCmds.Count + "条完整";
            if (denominatorNoteValid)
            {
                detail += ";历史分区=live覆盖" + historicalLiveCovered
                    + "+liveGap" + baseline.TotalLiveGap
                    + "+非活已注册" + historicalNonLiveRegistered
                    + "+deadGap" + baseline.TotalDeadGap
                    + ";手写" + historicalHandwritten + ";denominatorNote一致";
            }
            outcome.Add("A总量防倒退", pass, detail);
        }

        private static void AssertB(
            ProtocolCoverageScanner.ScanResult scan,
            CoverageBaseline baseline,
            List<KillEntry> killlist,
            List<HardNegativeConstraintEntry> hardNegativeConstraints,
            AssertionOutcome outcome)
        {
            if (baseline == null)
            {
                outcome.Add("B家族防倒退", true, "无 baseline.json,首次运行不挂红");
                return;
            }

            Dictionary<int, int> curFamilies = scan.BuildFamilyTable().ToDictionary(f => f.Prefix, f => f.UnityRegistered);
            List<int> baselineCmds = baseline.RegisteredCmds ?? new List<int>();
            Dictionary<int, int> baselineManifestCounts = baselineCmds
                .GroupBy(ProtocolCoverageScanner.ScanResult.Family)
                .ToDictionary(g => g.Key, g => g.Count());
            var baselinePrefixes = new HashSet<int>(baseline.Families.Select(f => f.Prefix));
            var baselineFamilyMismatches = new List<string>();
            foreach (FamilyBaseline fb in baseline.Families)
            {
                int manifestCount = baselineManifestCounts.TryGetValue(fb.Prefix, out int count) ? count : 0;
                if (fb.UnityRegistered != manifestCount)
                {
                    baselineFamilyMismatches.Add(fb.Prefix + ":族计数" + fb.UnityRegistered
                        + "!=逐号清单" + manifestCount);
                }
            }
            List<int> manifestFamiliesMissingBaseline = baselineManifestCounts.Keys
                .Where(prefix => !baselinePrefixes.Contains(prefix))
                .OrderBy(prefix => prefix)
                .ToList();
            List<int> negativeFamilyLiveGaps = baseline.Families
                .Where(f => f.LiveGap < 0)
                .Select(f => f.Prefix)
                .OrderBy(prefix => prefix)
                .ToList();
            int familyLiveGapSum = baseline.Families.Sum(f => f.LiveGap);
            bool totalLiveGapValid = baseline.TotalLiveGap >= 0
                && baseline.TotalLiveGap <= baseline.TotalLiveDefined;
            double expectedLiveCoveragePercent = baseline.TotalLiveDefined == 0
                ? 0.0
                : Math.Round(100.0 * (baseline.TotalLiveDefined - baseline.TotalLiveGap)
                    / baseline.TotalLiveDefined, 1);
            bool liveCoveragePercentValid = !double.IsNaN(baseline.LiveCoveragePercent)
                && !double.IsInfinity(baseline.LiveCoveragePercent)
                && Math.Abs(baseline.LiveCoveragePercent - expectedLiveCoveragePercent) < 0.0001;
            HashSet<int> sanctionedRemovals = BuildSanctionedRemovalSet(killlist, hardNegativeConstraints);
            var sanctionedBaselineMissingByFamily = baselineCmds
                .Where(c => sanctionedRemovals.Contains(c) && !scan.UnityRegistered.Contains(c))
                .GroupBy(ProtocolCoverageScanner.ScanResult.Family)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c).ToList());
            var regressed = new List<string>();
            var compensated = new List<string>();
            foreach (FamilyBaseline fb in baseline.Families)
            {
                int cur = curFamilies.TryGetValue(fb.Prefix, out int v) ? v : 0;
                List<int> sanctioned = sanctionedBaselineMissingByFamily.TryGetValue(fb.Prefix, out List<int> cmds)
                    ? cmds
                    : new List<int>();
                int effective = cur + sanctioned.Count;
                if (effective < fb.UnityRegistered)
                {
                    regressed.Add(fb.Prefix + ":" + cur + "+裁决" + sanctioned.Count + "<" + fb.UnityRegistered);
                }
                else if (sanctioned.Count > 0)
                {
                    compensated.Add(fb.Prefix + "[" + string.Join(",", sanctioned) + "]");
                }
            }
            bool baselineFamiliesValid = baselineFamilyMismatches.Count == 0
                && manifestFamiliesMissingBaseline.Count == 0
                && negativeFamilyLiveGaps.Count == 0
                && totalLiveGapValid
                && familyLiveGapSum == baseline.TotalLiveGap
                && liveCoveragePercentValid;
            bool pass = baselineFamiliesValid && regressed.Count == 0;
            var details = new List<string>();
            if (baselineFamilyMismatches.Count > 0)
                details.Add("baseline家族计数不一致:" + string.Join(";", baselineFamilyMismatches));
            if (manifestFamiliesMissingBaseline.Count > 0)
                details.Add("逐号清单家族未入baseline:" + string.Join(",", manifestFamiliesMissingBaseline));
            if (negativeFamilyLiveGaps.Count > 0)
                details.Add("baseline家族liveGap为负:" + string.Join(",", negativeFamilyLiveGaps));
            if (!totalLiveGapValid)
                details.Add("baseline历史liveGap总量非法:" + baseline.TotalLiveGap
                    + "/liveDefined=" + baseline.TotalLiveDefined);
            if (familyLiveGapSum != baseline.TotalLiveGap)
                details.Add("baseline家族liveGap汇总" + familyLiveGapSum
                    + "!=历史总量" + baseline.TotalLiveGap);
            if (!liveCoveragePercentValid)
                details.Add("baseline历史覆盖率" + baseline.LiveCoveragePercent
                    + "!=总量推导" + expectedLiveCoveragePercent);
            if (regressed.Count > 0) details.Add(string.Join(";", regressed));
            if (pass) details.Add("baseline家族冻结计数与" + baselineCmds.Count + "条逐号清单一致;"
                + "历史liveGap分项/总量" + familyLiveGapSum + "一致;覆盖率"
                + expectedLiveCoveragePercent + "%;全部家族未降");
            string detail = string.Join(";", details);
            if (compensated.Count > 0) detail += ";有证据裁决移除=" + string.Join(";", compensated);
            outcome.Add("B家族防倒退", pass, detail);
        }

        private static HashSet<int> BuildSanctionedRemovalSet(
            IEnumerable<KillEntry> killlist,
            IEnumerable<HardNegativeConstraintEntry> hardNegativeConstraints)
        {
            var result = new HashSet<int>(
                killlist.Where(k => !string.IsNullOrWhiteSpace(k.Evidence)).Select(k => k.Cmd));
            result.UnionWith(hardNegativeConstraints
                .Where(k => !string.IsNullOrWhiteSpace(k.Evidence))
                .Select(k => k.Cmd));
            return result;
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
            List<int> historicalManifest = baseline?.ErrorExitUnregisteredCmds ?? new List<int>();
            List<int> duplicateManifestCmds = historicalManifest
                .GroupBy(c => c)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .OrderBy(c => c)
                .ToList();
            List<int> invalidManifestCmds = historicalManifest
                .Where(c => c < 10000 || c > 99999)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            bool strictlyIncreasing = historicalManifest
                .Zip(historicalManifest.Skip(1), (left, right) => left < right)
                .All(inOrder => inOrder);
            bool manifestValid = firstRun || (baselineCount >= 0
                && historicalManifest.Count == baselineCount
                && duplicateManifestCmds.Count == 0
                && invalidManifestCmds.Count == 0
                && strictlyIncreasing);
            var historicalManifestSet = new HashSet<int>(historicalManifest);
            List<int> currentOutsideManifest = unregistered
                .Where(c => !historicalManifestSet.Contains(c))
                .OrderBy(c => c)
                .ToList();
            var historicalUnregistered = new HashSet<int>(scan.ErrorExitCandidates);
            historicalUnregistered.ExceptWith(baseline?.RegisteredCmds ?? new List<int>());
            List<int> rebuiltOutsideManifest = historicalUnregistered
                .Where(c => !historicalManifestSet.Contains(c))
                .OrderBy(c => c)
                .ToList();
            List<int> historicalNowAbsent = historicalManifest
                .Where(c => !historicalUnregistered.Contains(c))
                .OrderBy(c => c)
                .ToList();
            bool pass = firstRun || (manifestValid
                && unregistered.Count <= baselineCount
                && currentOutsideManifest.Count == 0
                && rebuiltOutsideManifest.Count == 0);
            string detail = "当前" + unregistered.Count + (firstRun ? "(无baseline,首次运行不挂红)" : " vs baseline" + baselineCount);
            if (unregistered.Count > 0) detail += ";号=" + string.Join(",", unregistered.OrderBy(x => x));
            if (!firstRun && historicalManifest.Count != baselineCount)
                detail += ";历史清单计数" + historicalManifest.Count + "!=baseline" + baselineCount;
            if (duplicateManifestCmds.Count > 0)
                detail += ";历史清单重复:" + string.Join(",", duplicateManifestCmds);
            if (invalidManifestCmds.Count > 0)
                detail += ";历史清单非五位:" + string.Join(",", invalidManifestCmds);
            if (!firstRun && !strictlyIncreasing) detail += ";历史清单非严格升序";
            if (!firstRun && currentOutsideManifest.Count > 0)
                detail += ";当前新增未治理:" + string.Join(",", currentOutsideManifest);
            if (!firstRun && rebuiltOutsideManifest.Count > 0)
                detail += ";按baseline注册清单重建新增:" + string.Join(",", rebuiltOutsideManifest);
            if (!firstRun) detail += manifestValid
                ? ";历史逐号清单" + historicalManifest.Count + "条完整;按baseline注册清单重建="
                    + historicalUnregistered.Count
                : ";历史逐号清单项数" + historicalManifest.Count + ";按baseline注册清单重建="
                    + historicalUnregistered.Count;
            if (!firstRun && historicalNowAbsent.Count > 0)
                detail += ";历史候选当前已不再识别:" + string.Join(",", historicalNowAbsent);
            outcome.Add("E族错误出口未注册非回归", pass, detail);
        }

        private static void AssertF(
            ProtocolCoverageScanner.ScanResult scan,
            List<HardNegativeConstraintEntry> constraints,
            List<KillEntry> killlist,
            AssertionOutcome outcome)
        {
            var malformed = constraints
                .Where(c => c.Cmd < 10000
                    || c.Cmd > 99999
                    || string.IsNullOrWhiteSpace(c.Rule)
                    || string.IsNullOrWhiteSpace(c.Evidence))
                .Select(c => c.Cmd)
                .OrderBy(c => c)
                .ToList();
            var duplicates = constraints
                .GroupBy(c => c.Cmd)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .OrderBy(c => c)
                .ToList();
            var killSet = new HashSet<int>(killlist.Select(k => k.Cmd));
            List<int> killlistOverlaps = constraints
                .Select(c => c.Cmd)
                .Distinct()
                .Where(killSet.Contains)
                .OrderBy(c => c)
                .ToList();
            HashSet<int> liveGap = scan.LiveGap();
            List<int> outsideLiveGap = constraints
                .Select(c => c.Cmd)
                .Distinct()
                .Where(c => !liveGap.Contains(c))
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
                if (scan.UnityStaticSendSites.TryGetValue(
                    constraint.Cmd,
                    out List<ProtocolCoverageScanner.DuplicateSite> sendSites))
                {
                    locations.Add("send@" + string.Join(",", sendSites.Select(s => s.File + ":" + s.Line)));
                }
                if (scan.UnityStaticLiteralSendSites.TryGetValue(
                    constraint.Cmd,
                    out List<ProtocolCoverageScanner.DuplicateSite> literalSendSites))
                {
                    locations.Add("literalSend@" + string.Join(",", literalSendSites.Select(s => s.File + ":" + s.Line)));
                }

                if (locations.Count > 0)
                {
                    violations.Add(constraint.Cmd + "[" + string.Join("+", locations) + "]");
                }
            }

            bool pass = constraints.Count > 0
                && malformed.Count == 0
                && duplicates.Count == 0
                && killlistOverlaps.Count == 0
                && outsideLiveGap.Count == 0
                && violations.Count == 0;
            var details = new List<string>();
            if (constraints.Count == 0) details.Add("清单缺失或为空");
            if (malformed.Count > 0) details.Add("cmd/rule/evidence不完整:" + string.Join(",", malformed));
            if (duplicates.Count > 0) details.Add("重复协议号:" + string.Join(",", duplicates));
            if (killlistOverlaps.Count > 0) details.Add("与killlist重叠:" + string.Join(",", killlistOverlaps));
            if (outsideLiveGap.Count > 0) details.Add("不在当前liveGap:" + string.Join(",", outsideLiveGap));
            if (violations.Count > 0) details.Add("违规出现:" + string.Join(";", violations));
            if (pass) details.Add(constraints.Count + "条约束均属于当前liveGap、无常量/注册/发送入口且与"
                + killlist.Count + "条killlist零交集");
            outcome.Add("F硬负约束防复发", pass, string.Join(";", details));
        }

        private static void AssertG(
            ProtocolCoverageScanner.ScanResult scan,
            List<KillEntry> killlist,
            AssertionOutcome outcome)
        {
            List<int> malformed = killlist
                .Where(k => k.Cmd < 10000
                    || k.Cmd > 99999
                    || string.IsNullOrWhiteSpace(k.Reason)
                    || string.IsNullOrWhiteSpace(k.Evidence))
                .Select(k => k.Cmd)
                .OrderBy(c => c)
                .ToList();
            List<int> duplicates = killlist
                .GroupBy(k => k.Cmd)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .OrderBy(c => c)
                .ToList();
            List<int> registeredKill = killlist
                .Select(k => k.Cmd)
                .Distinct()
                .Where(scan.UnityRegistered.Contains)
                .OrderBy(c => c)
                .ToList();
            List<int> invalidModes = killlist
                .Where(k => k.ClientMode != KillEntry.CLIENT_MODE_ABSENT
                    && k.ClientMode != KillEntry.CLIENT_MODE_SEND_ONLY)
                .Select(k => k.Cmd)
                .OrderBy(c => c)
                .ToList();
            List<int> forbiddenConstants = killlist
                .Where(k => k.ClientMode == KillEntry.CLIENT_MODE_ABSENT
                    && scan.UnityProtocolConstants.Contains(k.Cmd))
                .Select(k => k.Cmd)
                .OrderBy(c => c)
                .ToList();
            List<int> missingSendOnlyConstants = killlist
                .Where(k => k.ClientMode == KillEntry.CLIENT_MODE_SEND_ONLY
                    && !scan.UnityProtocolConstants.Contains(k.Cmd))
                .Select(k => k.Cmd)
                .OrderBy(c => c)
                .ToList();
            List<int> forbiddenSendReferences = killlist
                .Where(k => k.ClientMode == KillEntry.CLIENT_MODE_ABSENT
                    && (scan.UnityStaticSendSites.ContainsKey(k.Cmd)
                        || scan.UnityStaticLiteralSendSites.ContainsKey(k.Cmd)))
                .Select(k => k.Cmd)
                .OrderBy(c => c)
                .ToList();
            List<int> missingSendOnlyReferences = killlist
                .Where(k => k.ClientMode == KillEntry.CLIENT_MODE_SEND_ONLY
                    && !scan.UnityStaticSendSites.ContainsKey(k.Cmd))
                .Select(k => k.Cmd)
                .OrderBy(c => c)
                .ToList();
            int sendOnlyCount = killlist.Count(k => k.ClientMode == KillEntry.CLIENT_MODE_SEND_ONLY);

            bool pass = killlist.Count > 0
                && malformed.Count == 0
                && duplicates.Count == 0
                && registeredKill.Count == 0
                && invalidModes.Count == 0
                && forbiddenConstants.Count == 0
                && missingSendOnlyConstants.Count == 0
                && forbiddenSendReferences.Count == 0
                && missingSendOnlyReferences.Count == 0;
            var details = new List<string>();
            if (killlist.Count == 0) details.Add("清单缺失或为空");
            if (malformed.Count > 0) details.Add("cmd/reason/evidence不完整:" + string.Join(",", malformed));
            if (duplicates.Count > 0) details.Add("重复协议号:" + string.Join(",", duplicates));
            if (registeredKill.Count > 0) details.Add("死号仍有运行时handler:" + string.Join(",", registeredKill));
            if (invalidModes.Count > 0) details.Add("clientMode非法:" + string.Join(",", invalidModes));
            if (forbiddenConstants.Count > 0) details.Add("absent死号仍有Proto常量:" + string.Join(",", forbiddenConstants));
            if (missingSendOnlyConstants.Count > 0) details.Add("send_only缺Proto常量:" + string.Join(",", missingSendOnlyConstants));
            if (forbiddenSendReferences.Count > 0) details.Add("absent死号仍有生产发送引用:" + string.Join(",", forbiddenSendReferences));
            if (missingSendOnlyReferences.Count > 0) details.Add("send_only缺生产发送引用:" + string.Join(",", missingSendOnlyReferences));
            if (pass) details.Add(killlist.Count + "条killlist字段/evidence完整且与运行时注册零交集;send_only=" + sendOnlyCount
                + ";发送引用=" + sendOnlyCount + ";数字直发=" + scan.UnityStaticLiteralSendSites.Count);
            outcome.Add("G死号防复活", pass, string.Join(";", details));
        }

        private static void AssertH(
            ProtocolCoverageScanner.ScanResult scan,
            CoverageBaseline baseline,
            List<KillEntry> killlist,
            AssertionOutcome outcome)
        {
            if (baseline == null)
            {
                outcome.Add("Hbaseline状态收口", true, "无 baseline.json,首次运行不挂红");
                return;
            }

            var validStatuses = new HashSet<string> { "done", "pending", "legacy_unverified" };
            List<int> duplicatePrefixes = baseline.Families
                .GroupBy(f => f.Prefix)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .OrderBy(p => p)
                .ToList();
            List<int> invalidStatuses = baseline.Families
                .Where(f => !validStatuses.Contains(f.Status))
                .Select(f => f.Prefix)
                .OrderBy(p => p)
                .ToList();
            var baselinePrefixes = new HashSet<int>(baseline.Families.Select(f => f.Prefix));
            List<int> missingFamilies = scan.BuildFamilyTable()
                .Select(f => f.Prefix)
                .Where(p => !baselinePrefixes.Contains(p))
                .OrderBy(p => p)
                .ToList();
            var validKillSet = new HashSet<int>(killlist
                .Where(k => !string.IsNullOrWhiteSpace(k.Evidence))
                .Select(k => k.Cmd));
            HashSet<int> liveGap = scan.LiveGap();
            List<int> doneWithGapMissingNote = baseline.Families
                .Where(f => f.Status == "done" && string.IsNullOrWhiteSpace(f.StatusNote))
                .Where(f => liveGap.Any(c => ProtocolCoverageScanner.ScanResult.Family(c) == f.Prefix))
                .Select(f => f.Prefix)
                .Distinct()
                .OrderBy(p => p)
                .ToList();
            var notClosed = new List<string>();
            foreach (FamilyBaseline family in baseline.Families.Where(f => f.Status != "done"))
            {
                List<int> gaps = liveGap
                    .Where(c => ProtocolCoverageScanner.ScanResult.Family(c) == family.Prefix)
                    .OrderBy(c => c)
                    .ToList();
                List<int> ungoverned = gaps.Where(c => !validKillSet.Contains(c)).ToList();
                if (ungoverned.Count == 0)
                {
                    notClosed.Add(family.Prefix + (gaps.Count == 0
                        ? "[零活缺口]"
                        : "[全killlist:" + string.Join(",", gaps) + "]"));
                }
            }

            bool pass = duplicatePrefixes.Count == 0
                && invalidStatuses.Count == 0
                && missingFamilies.Count == 0
                && doneWithGapMissingNote.Count == 0
                && notClosed.Count == 0;
            var details = new List<string>();
            if (duplicatePrefixes.Count > 0) details.Add("baseline家族重复:" + string.Join(",", duplicatePrefixes));
            if (invalidStatuses.Count > 0) details.Add("status非法:" + string.Join(",", invalidStatuses));
            if (missingFamilies.Count > 0) details.Add("当前家族未入baseline:" + string.Join(",", missingFamilies));
            if (doneWithGapMissingNote.Count > 0)
                details.Add("带活缺口done族缺statusNote:" + string.Join(",", doneWithGapMissingNote));
            if (notClosed.Count > 0) details.Add("已完整治理但未done:" + string.Join(";", notClosed));
            if (pass) details.Add("baseline家族唯一且带活缺口done族均有statusNote;"
                + "无零缺口/全killlist仍滞留pending或legacy_unverified的家族");
            outcome.Add("Hbaseline状态收口", pass, string.Join(";", details));
        }

        private static void AssertI(
            ProtocolCoverageScanner.ScanResult scan,
            CoverageBaseline curatedBaseline,
            CoverageBaseline candidate,
            List<KillEntry> killlist,
            AssertionOutcome outcome)
        {
            if (candidate == null)
            {
                outcome.Add("I候选基线完整性", false, "BuildCandidate返回null");
                return;
            }

            var topMismatches = new List<string>();
            string expectedGeneratedAt = scan.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss");
            string expectedDenominatorNote = ProtocolCoverageReport.DenominatorNote(scan);
            int expectedLiveDefined = scan.LiveDefinedSet().Count;
            int expectedLiveGap = scan.LiveGap().Count;
            double expectedCoverage = expectedLiveDefined == 0
                ? 0.0
                : Math.Round(100.0 * (expectedLiveDefined - expectedLiveGap) / expectedLiveDefined, 1);
            if (!string.Equals(candidate.GeneratedAt, expectedGeneratedAt, StringComparison.Ordinal))
                topMismatches.Add("generatedAt");
            if (!string.Equals(candidate.DenominatorNote, expectedDenominatorNote, StringComparison.Ordinal))
                topMismatches.Add("denominatorNote");
            if (candidate.TotalUnityRegistered != scan.UnityRegistered.Count)
                topMismatches.Add("totalUnityRegistered");
            if (candidate.TotalClientProtocolDefined != scan.ClientProtocolDefined.Count)
                topMismatches.Add("totalClientProtocolDefined");
            if (candidate.TotalLiveDefined != expectedLiveDefined) topMismatches.Add("totalLiveDefined");
            if (candidate.TotalLiveGap != expectedLiveGap) topMismatches.Add("totalLiveGap");
            if (candidate.TotalDeadGap != scan.DeadGap().Count) topMismatches.Add("totalDeadGap");
            if (double.IsNaN(candidate.LiveCoveragePercent)
                || double.IsInfinity(candidate.LiveCoveragePercent)
                || Math.Abs(candidate.LiveCoveragePercent - expectedCoverage) >= 0.0001)
                topMismatches.Add("liveCoveragePercent");

            List<int> expectedRegistered = scan.UnityRegistered.OrderBy(c => c).ToList();
            List<int> candidateRegistered = candidate.RegisteredCmds ?? new List<int>();
            bool registeredManifestValid = candidateRegistered.SequenceEqual(expectedRegistered);
            var expectedErrorExits = new HashSet<int>(scan.ErrorExitCandidates);
            expectedErrorExits.ExceptWith(scan.UnityRegistered);
            List<int> expectedErrorManifest = expectedErrorExits.OrderBy(c => c).ToList();
            List<int> candidateErrorManifest = candidate.ErrorExitUnregisteredCmds ?? new List<int>();
            bool errorManifestValid = candidate.ErrorExitUnregisteredCount == expectedErrorManifest.Count
                && candidateErrorManifest.SequenceEqual(expectedErrorManifest);

            List<ProtocolCoverageScanner.FamilyStat> expectedFamilies = scan.BuildFamilyTable();
            List<FamilyBaseline> candidateFamilies = candidate.Families ?? new List<FamilyBaseline>();
            Dictionary<int, List<FamilyBaseline>> candidateGroups = candidateFamilies
                .GroupBy(f => f.Prefix)
                .ToDictionary(g => g.Key, g => g.ToList());
            var expectedPrefixes = new HashSet<int>(expectedFamilies.Select(f => f.Prefix));
            List<int> duplicateCandidatePrefixes = candidateGroups
                .Where(kv => kv.Value.Count > 1)
                .Select(kv => kv.Key)
                .OrderBy(p => p)
                .ToList();
            List<int> missingCandidateFamilies = expectedPrefixes
                .Where(p => !candidateGroups.ContainsKey(p))
                .OrderBy(p => p)
                .ToList();
            List<int> extraCandidateFamilies = candidateGroups.Keys
                .Where(p => !expectedPrefixes.Contains(p))
                .OrderBy(p => p)
                .ToList();
            List<int> droppedCuratedFamilies = (curatedBaseline?.Families ?? new List<FamilyBaseline>())
                .Select(f => f.Prefix)
                .Distinct()
                .Where(p => !candidateGroups.ContainsKey(p))
                .OrderBy(p => p)
                .ToList();

            var validKillSet = new HashSet<int>((killlist ?? new List<KillEntry>())
                .Where(k => !string.IsNullOrWhiteSpace(k.Evidence))
                .Select(k => k.Cmd));
            var familyMachineMismatches = new List<int>();
            var suggestedStatusMismatches = new List<int>();
            var curatedFieldMismatches = new List<int>();
            int curatedPreservedCount = 0;
            foreach (ProtocolCoverageScanner.FamilyStat expected in expectedFamilies)
            {
                if (!candidateGroups.TryGetValue(expected.Prefix, out List<FamilyBaseline> matches)
                    || matches.Count != 1)
                {
                    continue;
                }

                FamilyBaseline actual = matches[0];
                if (actual.UnityRegistered != expected.UnityRegistered
                    || actual.LiveGap != expected.LiveGap
                    || !(actual.LiveGapCmds ?? new List<int>()).SequenceEqual(expected.LiveGapCmds))
                {
                    familyMachineMismatches.Add(expected.Prefix);
                }

                string expectedSuggestedStatus = expected.LiveGapCmds.All(validKillSet.Contains)
                    ? "done"
                    : "pending";
                if (!string.Equals(actual.SuggestedStatus, expectedSuggestedStatus, StringComparison.Ordinal))
                {
                    suggestedStatusMismatches.Add(expected.Prefix);
                }

                FamilyBaseline curated = curatedBaseline?.FindFamily(expected.Prefix);
                string expectedStatus = curated?.Status ?? expectedSuggestedStatus;
                string expectedStatusNote = curated?.StatusNote;
                if (!string.Equals(actual.Status, expectedStatus, StringComparison.Ordinal)
                    || !string.Equals(actual.StatusNote, expectedStatusNote, StringComparison.Ordinal))
                {
                    curatedFieldMismatches.Add(expected.Prefix);
                }
                else if (curated != null)
                {
                    curatedPreservedCount++;
                }
            }

            bool pass = topMismatches.Count == 0
                && registeredManifestValid
                && errorManifestValid
                && duplicateCandidatePrefixes.Count == 0
                && missingCandidateFamilies.Count == 0
                && extraCandidateFamilies.Count == 0
                && droppedCuratedFamilies.Count == 0
                && familyMachineMismatches.Count == 0
                && suggestedStatusMismatches.Count == 0
                && curatedFieldMismatches.Count == 0;
            var details = new List<string>();
            if (topMismatches.Count > 0) details.Add("顶层机器字段不一致:" + string.Join(",", topMismatches));
            if (!registeredManifestValid) details.Add("registeredCmds未精确反映当前扫描");
            if (!errorManifestValid) details.Add("错误出口计数/逐号清单未精确反映当前扫描");
            if (duplicateCandidatePrefixes.Count > 0)
                details.Add("候选家族重复:" + string.Join(",", duplicateCandidatePrefixes));
            if (missingCandidateFamilies.Count > 0)
                details.Add("候选漏当前家族:" + string.Join(",", missingCandidateFamilies));
            if (extraCandidateFamilies.Count > 0)
                details.Add("候选多余家族:" + string.Join(",", extraCandidateFamilies));
            if (droppedCuratedFamilies.Count > 0)
                details.Add("候选丢正式策展家族:" + string.Join(",", droppedCuratedFamilies));
            if (familyMachineMismatches.Count > 0)
                details.Add("家族机器字段不一致:" + string.Join(",", familyMachineMismatches));
            if (suggestedStatusMismatches.Count > 0)
                details.Add("suggestedStatus不一致:" + string.Join(",", suggestedStatusMismatches));
            if (curatedFieldMismatches.Count > 0)
                details.Add("人工status/statusNote未保留:" + string.Join(",", curatedFieldMismatches));
            if (pass)
            {
                details.Add("候选顶层/" + expectedRegistered.Count + "注册号/" + expectedErrorManifest.Count
                    + "错误出口/" + expectedFamilies.Count + "家族机器字段准确;人工策展字段保留"
                    + curatedPreservedCount + "族;suggestedStatus一致");
            }
            outcome.Add("I候选基线完整性", pass, string.Join(";", details));
        }
    }
}
