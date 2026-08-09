using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

static class Audit
{
    private static readonly List<string> Failures = new();

    private static void Check(bool condition, string label)
    {
        Console.WriteLine($"[{(condition ? "PASS" : "FAIL")}] {label}");
        if (!condition) Failures.Add(label);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) &&
                Directory.Exists(Path.Combine(current.FullName, "Assets")))
                return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Cannot locate repository root.");
    }

    private static Dictionary<string, (string Name, int Enabled)> ReadScrollRects(string yaml)
    {
        var names = new Dictionary<string, string>();
        foreach (Match match in Regex.Matches(yaml,
                     @"(?ms)^--- !u!1 &(?<id>-?\d+)\r?\nGameObject:\r?\n(?<body>.*?)(?=^--- !u!|\z)"))
        {
            var name = Regex.Match(match.Groups["body"].Value, @"(?m)^  m_Name: (?<name>.*)$");
            if (name.Success) names[match.Groups["id"].Value] = name.Groups["name"].Value.Trim();
        }

        var result = new Dictionary<string, (string, int)>();
        foreach (Match match in Regex.Matches(yaml,
                     @"(?ms)^--- !u!114 &(?<id>-?\d+)\r?\nMonoBehaviour:\r?\n(?<body>.*?)(?=^--- !u!|\z)"))
        {
            var body = match.Groups["body"].Value;
            if (!body.Contains("guid: 1aa08ab6e0800fa44ae55d278d1423e3", StringComparison.Ordinal)) continue;
            var gameObject = Regex.Match(body, @"(?m)^  m_GameObject: \{fileID: (?<id>-?\d+)\}");
            var enabled = Regex.Match(body, @"(?m)^  m_Enabled: (?<value>[01])$");
            if (!gameObject.Success || !enabled.Success) continue;
            names.TryGetValue(gameObject.Groups["id"].Value, out var name);
            result[match.Groups["id"].Value] = (name ?? "<unnamed>", int.Parse(enabled.Groups["value"].Value));
        }
        return result;
    }

    private static void CheckTransformReferences(string yaml)
    {
        var transforms = Regex.Matches(yaml, @"(?m)^--- !u!224 &(?<id>-?\d+)(?: stripped)?$")
            .Select(m => m.Groups["id"].Value).ToHashSet(StringComparer.Ordinal);
        var missing = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match block in Regex.Matches(yaml,
                     @"(?ms)^--- !u!224 &-?\d+[^\r\n]*\r?\nRectTransform:\r?\n(?<body>.*?)(?=^--- !u!|\z)"))
        {
            var children = Regex.Match(block.Groups["body"].Value,
                @"(?ms)^  m_Children:\r?\n(?<list>(?:  - \{fileID: -?\d+\}\r?\n)*)");
            if (!children.Success) continue;
            foreach (Match child in Regex.Matches(children.Groups["list"].Value, @"fileID: (?<id>-?\d+)"))
            {
                var id = child.Groups["id"].Value;
                if (id != "0" && !transforms.Contains(id)) missing.Add(id);
            }
        }
        Check(missing.Count == 0, $"all RectTransform m_Children references resolve (missing={missing.Count})");
    }

    public static int Main()
    {
        var root = FindRepositoryRoot();
        var prefabPath = Path.Combine(root, "Assets", "Prefabs", "UI", "GodBeast", "GodBeastModule.prefab");
        var controllerPath = Path.Combine(root, "Assets", "Scripts", "Module", "Core", "GodBeast", "GodBeastController.cs");
        var output = Path.Combine(root, "output", "ui_route_audit", "2026-08-09_god_beast");
        var manifestPath = Path.Combine(output, "route-manifest.json");
        var ledgerPath = Path.Combine(output, "route-ledger.json");

        var yaml = File.ReadAllText(prefabPath);
        Check(yaml.StartsWith("%YAML 1.1", StringComparison.Ordinal), "Prefab YAML header preserved");
        foreach (var rootName in new[] { "GodBeastBagView", "GodBeastComView", "GodBeastSelectView", "GodBeastSkillView", "GodBeastStrView", "GodBeastStrenView", "GodBeastTipsView" })
            Check(yaml.Contains($"m_Name: {rootName}", StringComparison.Ordinal), $"Prefab contains {rootName}");
        Check(!yaml.Contains("m_Name: GodBeastView\r", StringComparison.Ordinal) &&
              !yaml.Contains("m_Name: GodBeastView\n", StringComparison.Ordinal), "Prefab intentionally lacks main GodBeastView (recorded blocker)");

        var scrollRects = ReadScrollRects(yaml);
        foreach (var id in new[] { "161477009195818581", "4885670100450534145", "1024554220334860621" })
            Check(scrollRects.TryGetValue(id, out var state) && state.Enabled == 0, $"duplicate outer ScrollRect {id} disabled");
        foreach (var id in new[] { "6047304782812189136", "6539117512329816510", "1956567193287015445" })
            Check(scrollRects.TryGetValue(id, out var state) && state.Enabled == 1, $"runtime list ScrollRect {id} remains enabled");

        Check(!yaml.Contains("m_text: aaa", StringComparison.Ordinal), "tipsLb conversion placeholder removed");
        Check(!Regex.IsMatch(yaml, @"(?m)^  m_text: aa$"), "attribute conversion placeholders removed");
        Check(!yaml.Contains("m_text: htmlText", StringComparison.Ordinal), "level conversion placeholder removed");
        Check(yaml.Contains(@"m_text: ""\u9057\u9AB8\u9884\u89C8""", StringComparison.Ordinal), "compose preview title matches current legacy runtime text");
        Check(!yaml.Contains(@"m_text: ""\u9057\u9AB8\u88C5\u9884\u89C8""", StringComparison.Ordinal), "stale compose preview title absent");
        CheckTransformReferences(yaml);

        var controller = File.ReadAllText(controllerPath);
        foreach (var symbol in new[] { "GODBEAST_ERROR", "GODBEAST_OVERVIEW", "GODBEAST_UPDATE", "GODBEAST_STRENGTH_PREVIEW", "GODBEAST_ATTRIBUTE_POWER" })
            Check(controller.Contains($"RegisterProtocal(Proto.{symbol}", StringComparison.Ordinal), $"read-side controller registers {symbol}");
        foreach (var command in new[] { "17303", "17304", "17305", "17306", "17307", "17310", "17311", "17312" })
            Check(!controller.Contains($"On{command}", StringComparison.Ordinal), $"write-side handler {command} remains absent and blocked");

        var configRoot = Path.Combine(root, "Assets", "GameRes", "resource", "config");
        foreach (var name in new[] { "config_eudemons_item.json", "config_eudemons_equip_pos.json", "config_eudemons_equip_attr.json", "config_eudemons_strength.json", "config_eudemons_compose.json", "config_eudemons_cfg.json", "ConfigGodBeast.json" })
            Check(!Directory.EnumerateFiles(configRoot, name, SearchOption.AllDirectories).Any(), $"missing required config remains explicit blocker: {name}");

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        using var ledger = JsonDocument.Parse(File.ReadAllText(ledgerPath));
        Check(ledger.RootElement.GetProperty("schema").GetInt32() == 6, "route ledger schema is 6");
        Check(manifest.RootElement.GetProperty("route").GetString() == "mainui.treasure.god-beast", "manifest route identity");
        var manifestNodes = manifest.RootElement.GetProperty("nodes");
        var ledgerNodes = ledger.RootElement.GetProperty("nodes");
        Check(manifestNodes.GetArrayLength() == 168 && ledgerNodes.GetArrayLength() == 168, "manifest and ledger contain 168 nodes");
        var statuses = ledgerNodes.EnumerateArray().GroupBy(n => n.GetProperty("status").GetString() ?? "<null>")
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        Check(statuses.GetValueOrDefault("blocked") == 161, "ledger blocked count is 161");
        Check(statuses.GetValueOrDefault("needs-runtime-verify") == 7, "ledger runtime-verification count is 7");
        Check(!statuses.ContainsKey("done") && statuses.Values.Sum() == 168, "no static result is misrepresented as done");
        var expectedManifestHash = ledger.RootElement.GetProperty("manifest_source").GetProperty("sha256").GetString();
        var actualManifestHash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(manifestPath)));
        Check(string.Equals(expectedManifestHash, actualManifestHash, StringComparison.Ordinal), "ledger manifest_source SHA-256 matches manifest bytes");

        Console.WriteLine($"summary failures={Failures.Count}");
        if (Failures.Count == 0) return 0;
        foreach (var failure in Failures) Console.Error.WriteLine($"FAILED: {failure}");
        return 1;
    }
}
