using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.EditorTools.Packaging
{
    /// <summary>
    /// 给 Remote_* 组的每个条目打恰好一个 pack_ 前缀标签,配合 PackTogetherByLabel 把
    /// 6 个巨型组拆成"目录粒度"的中等 bundle(改一个文件只重下它所在的目录包)。
    /// 规则:按 address 前缀取固定深度目录当"打包单元";源文件累计 &lt; 512KB 的小单元向父目录合并。
    /// pack_ 标签只归本工具管:手动加/删 pack_ 标签会改写全局 bundle 布局造成 hash 大漂移。
    /// </summary>
    public static class PackLabeler
    {
        public const string LabelPrefix = "pack_";
        private const long MergeThresholdBytes = 512 * 1024;
        private const long TargetMergedBytes = 2 * 1024 * 1024; // 合并桶目标体积(源文件口径)
        private const long WarnUnitBytes = 100L * 1024 * 1024; // 源文件口径,LZ4 后通常显著小于此

        /// <summary>为所有 Remote_* 条目分配 pack_ 标签,返回打包单元数。由『Addressable 自动分组』调用。</summary>
        public static int AssignAll(AddressableAssetSettings settings)
        {
            var entries = CollectRemoteEntries(settings);
            if (entries.Count == 0) return 0;

            var unitOf = new Dictionary<AddressableAssetEntry, string>(entries.Count);
            var sizeOf = new Dictionary<AddressableAssetEntry, long>(entries.Count);
            foreach (var e in entries)
            {
                unitOf[e] = InitialUnit(e.address);
                sizeOf[e] = SourceSize(e.AssetPath);
            }

            // 小单元合并(有界迭代;阈值按源文件体积,是压缩后体积的保守代理)。
            // ⚠合并必须有界:旧版无脑并父,曾把 2036 个小特效卷成一个 54MB 巨包/职业动作卷成 126MB——
            // 巨包被任意一个成员钉住整包驻堆(WebGL wasm 堆),与整图打包同病。现在父目录下小单元
            // 总量超过目标体积时,按稳定哈希切成 ~TargetMergedBytes 的桶(桶名 parent/m{i},不与真实目录冲突)。
            for (int pass = 0; pass < 4; pass++)
            {
                var unitSize = new Dictionary<string, long>();
                foreach (var e in entries)
                {
                    unitSize.TryGetValue(unitOf[e], out var s);
                    unitSize[unitOf[e]] = s + sizeOf[e];
                }

                var smallByParent = new Dictionary<string, List<string>>();
                foreach (var kv in unitSize)
                {
                    if (kv.Value >= MergeThresholdBytes || Depth(kv.Key) <= 1) continue;
                    string parent = Parent(kv.Key);
                    if (!smallByParent.TryGetValue(parent, out var list)) smallByParent[parent] = list = new List<string>();
                    list.Add(kv.Key);
                }
                if (smallByParent.Count == 0) break;

                var mapTo = new Dictionary<string, string>();
                foreach (var kv in smallByParent)
                {
                    // ⚠依赖连坐红线:Addressables 依赖闭包是【包粒度】——prefab 混桶后,同桶任意 prefab 的
                    // 纹理依赖全部连坐(实测登录 prefab 混桶把 mainui/firstblood 等 20MB 无关模块拽进启动下载)。
                    // prefab 族小单元一律保持独立小包,不混桶也不并父;叶子资产(纹理/图标/瓦片/clip)桶化安全。
                    if (kv.Key.StartsWith("prefabs/", StringComparison.Ordinal)) continue;
                    long total = 0;
                    foreach (string unit in kv.Value) total += unitSize[unit];
                    int buckets = (int)Math.Min(64, Math.Max(1, total / TargetMergedBytes));
                    foreach (string unit in kv.Value)
                        mapTo[unit] = buckets <= 1 ? kv.Key : kv.Key + "/m" + (Fnv1a(unit) % (uint)buckets);
                }

                foreach (var e in entries)
                {
                    if (mapTo.TryGetValue(unitOf[e], out string to)) unitOf[e] = to;
                }
            }

            // 应用:每条目恰好一个 pack_ 标签
            var usedLabels = new HashSet<string>();
            foreach (var e in entries)
            {
                string label = LabelPrefix + unitOf[e].Replace('/', '_');
                usedLabels.Add(label);

                foreach (var stale in e.labels.Where(l => l.StartsWith(LabelPrefix, StringComparison.Ordinal) && l != label).ToList())
                    e.SetLabel(stale, false, false, false);
                if (!e.labels.Contains(label))
                    e.SetLabel(label, true, true, false);
            }

            // 清掉全局标签表里已无人使用的 pack_ 标签
            foreach (var stale in settings.GetLabels()
                         .Where(l => l.StartsWith(LabelPrefix, StringComparison.Ordinal) && !usedLabels.Contains(l)).ToList())
                settings.RemoveLabel(stale, false);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            return usedLabels.Count;
        }

        /// <summary>校验:每个 Remote 条目恰好一个 pack_ 标签;输出单元数/体积分布/超大单元警告。</summary>
        public static bool Validate(AddressableAssetSettings settings, out string report)
        {
            var entries = CollectRemoteEntries(settings);
            var sb = new System.Text.StringBuilder();
            int bad = 0;
            var unitSize = new Dictionary<string, long>();
            var unitCount = new Dictionary<string, int>();

            foreach (var e in entries)
            {
                var packs = e.labels.Where(l => l.StartsWith(LabelPrefix, StringComparison.Ordinal)).ToList();
                if (packs.Count != 1)
                {
                    if (bad++ < 20) sb.AppendLine($"  ✗ {e.address}: pack_ 标签数 {packs.Count}(要求恰好 1),先跑『Addressable 自动分组』");
                    continue;
                }
                long size = SourceSize(e.AssetPath);
                unitSize.TryGetValue(packs[0], out var s); unitSize[packs[0]] = s + size;
                unitCount.TryGetValue(packs[0], out var c); unitCount[packs[0]] = c + 1;
            }

            var over = unitSize.Where(kv => kv.Value > WarnUnitBytes).OrderByDescending(kv => kv.Value).ToList();
            sb.Insert(0, $"[PackLabeler] 校验: 条目 {entries.Count}, 打包单元 {unitSize.Count}, 标签异常 {bad}, 超 {WarnUnitBytes / 1024 / 1024}MB 单元 {over.Count}\n");
            foreach (var kv in unitSize.OrderByDescending(kv => kv.Value).Take(20))
                sb.AppendLine($"  {kv.Value / 1024 / 1024,6} MB  {unitCount[kv.Key],5} 条  {kv.Key}");
            foreach (var kv in over.Take(10))
                sb.AppendLine($"  ⚠ 超大单元 {kv.Key} = {kv.Value / 1024 / 1024} MB(考虑加深该前缀的单元深度)");

            report = sb.ToString();
            return bad == 0 && entries.Count > 0;
        }

        [UnityEditor.MenuItem("神霄/打包/通用/分组与拆包校验", priority = 200)]
        public static void ValidateMenu()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) { Debug.LogError("[PackLabeler] Addressable settings not found"); return; }
            bool ok = Validate(settings, out var report);
            if (ok) Debug.Log(report + "校验通过,可以构建。");
            else Debug.LogError(report + "校验未通过。");
        }

        private static List<AddressableAssetEntry> CollectRemoteEntries(AddressableAssetSettings settings)
        {
            var list = new List<AddressableAssetEntry>();
            foreach (var group in settings.groups)
            {
                if (group == null || !group.Name.StartsWith("Remote_", StringComparison.Ordinal)) continue;
                list.AddRange(group.entries);
            }
            return list;
        }

        /// <summary>
        /// address → 初始打包单元(目录粒度)。深度规则按资源族设计:
        /// 地图按 mapId、game 按模块、object 按模型/动作族、effect 按特效目录、UI prefab 按模块。
        /// </summary>
        private static string InitialUnit(string address)
        {
            var seg = address.Split('/');
            int depth;
            if (address.StartsWith("resource/game/scene/map/", StringComparison.Ordinal))
            {
                // 瓦片按 12×12 块分包:整图一包(map_10000 实测 43.8MB)会让"加载 1 瓦=整包驻堆",
                // 且新图首访要整包下载。块粒度后视野最多钉住 ~4 块(约 2MB/块),下载也按需分块。
                // 只有 tile/ 下文件名恰为 4 位行列号(rrcc)的算瓦片;整图低清底图({resId}.jpg,
                // 可能同为 4 位数字,如 8001)与寻路 bytes 留在图目录单元(进图必载的"图核"小包)。
                if (seg.Length >= 7 && seg[5] == "tile" && seg[6].Length == 4 && seg[6] != seg[4]
                    && int.TryParse(seg[6], out int rowCol))
                {
                    int blockRow = (rowCol / 100 - 1) / 12;
                    int blockCol = (rowCol % 100 - 1) / 12;
                    return string.Join("/", seg.Take(6)) + "/b" + blockRow + "_" + blockCol;
                }
                depth = 5;
            }
            else if (address.StartsWith("resource/game/skillicon/", StringComparison.Ordinal))
            {
                // 687 张技能图标共 21MB:目录粒度=用 1 张钉 21MB。每文件一单元,由合并桶切 ~2MB。
                // ⚠必须放在 resource/game/ 通配之前,否则永远匹配不到。
                return address;
            }
            else if (address.StartsWith("resource/game/", StringComparison.Ordinal)) depth = 3;
            else if (address.StartsWith("resource/", StringComparison.Ordinal)) depth = 2;
            else if (address.StartsWith("object/", StringComparison.Ordinal) && seg.Length > 2 && seg[2] == "action")
            {
                // 每个动作 clip 一个单元:职业动作目录 96~126MB,主角只用 idle/run 却钉住整包驻堆。
                // 大 clip(≥512KB)独立成包,小 clip 由上面的合并桶收编成 ~2MB 桶。
                return address;
            }
            else if (address.StartsWith("object/", StringComparison.Ordinal)) depth = 3;
            else if (address.StartsWith("effect/textures/", StringComparison.Ordinal))
            {
                // 共享特效贴图(依赖拉包):目录粒度=任意特效钉 14~17MB。每文件一单元,由合并桶切 ~2MB。
                return address;
            }
            else if (address.StartsWith("effect/objs/", StringComparison.Ordinal)) depth = 4;
            else if (address.StartsWith("effect/", StringComparison.Ordinal)) depth = 3;
            else if (address.StartsWith("prefabs/ui/", StringComparison.Ordinal)) depth = 3;
            else if (address.StartsWith("fonts/", StringComparison.Ordinal)) depth = 1;
            else if (address.StartsWith("comp/", StringComparison.Ordinal)) depth = 1;
            else depth = 2;

            int take = Math.Min(depth, Math.Max(1, seg.Length - 1)); // 不把文件名段算进单元
            return string.Join("/", seg.Take(take));
        }

        private static int Depth(string unit) => unit.Count(c => c == '/') + 1;

        private static string Parent(string unit)
        {
            int idx = unit.LastIndexOf('/');
            return idx > 0 ? unit.Substring(0, idx) : unit;
        }

        /// <summary>稳定字符串哈希(FNV-1a):桶分配跨构建、跨进程一致,避免布局漂移。</summary>
        private static uint Fnv1a(string s)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= 16777619;
                }
                return hash;
            }
        }

        private static long SourceSize(string assetPath)
        {
            try
            {
                if (string.IsNullOrEmpty(assetPath)) return 0;
                var fi = new FileInfo(assetPath);
                return fi.Exists ? fi.Length : 0;
            }
            catch { return 0; }
        }
    }
}
