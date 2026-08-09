using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: CompositeStaticAudit <repo-root>");
    return 2;
}

string root = Path.GetFullPath(args[0]);
string output = Path.Combine(root, "output", "ui_route_audit", "2026-08-09_composite");
string legacyRoot = @"E:\GitProject\yu_client";
string flowPath = Path.Combine(root, "Assets", "Scripts", "Module", "Core", "Composite", "CompositeFlow.cs");
string manifestPath = Path.Combine(output, "route-manifest.json");
string resultsPath = Path.Combine(output, "static-results.json");
string modulePrefabPath = Path.Combine(root, "Assets", "Prefabs", "UI", "Composite", "CompositeModule.prefab");
string menuPath = Path.Combine(legacyRoot, "cdn", "resource", "config", "server", "config_compose_menu.json");
string legacyViewPath = Path.Combine(legacyRoot, "h5", "src", "composite", "CompositeView.ts");

var errors = new List<string>();
void Check(bool condition, string message)
{
    if (!condition) errors.Add(message);
}

string[] labels =
{
    "道具合成", "熔魄铸魂", "影骸战衣合成", "遗骸铸合", "九霄冥饰合成",
    "天骨铸灵", "启示铠铸", "灵宠炼合", "御府铸仪", "天殒铸灵"
};
string[] views =
{
    "CompositeGoodsView", "CompositeRuneView", "CompositeHolySealView", "GodBeastCompositeView",
    "CompositeUnrealView", "GodBefallCompositeView", "CompositeRevelationView", "CompositeGuardView",
    "GodCourtComView", "CompositeLonglangView"
};

string flow = File.ReadAllText(flowPath);
string legacyView = File.ReadAllText(legacyViewPath);
foreach (string label in labels)
{
    Check(flow.Contains("\"" + label + "\"", StringComparison.Ordinal), "CompositeFlow missing label: " + label);
    Check(legacyView.Contains("label: \"" + label + "\"", StringComparison.Ordinal), "Legacy CompositeView missing label: " + label);
}
Check(flow.Contains("Label = TabLabels[i]", StringComparison.Ordinal), "TabSpec.Label is not assigned from TabLabels");

using JsonDocument manifestDoc = JsonDocument.Parse(File.ReadAllText(manifestPath));
using JsonDocument resultsDoc = JsonDocument.Parse(File.ReadAllText(resultsPath));
JsonElement nodeArray = manifestDoc.RootElement.GetProperty("nodes");
JsonElement resultArray = resultsDoc.RootElement.GetProperty("nodes");
var nodeById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
foreach (JsonElement node in nodeArray.EnumerateArray())
{
    string id = node.GetProperty("id").GetString()!;
    Check(nodeById.TryAdd(id, node), "duplicate manifest node: " + id);
}
var resultById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
foreach (JsonElement result in resultArray.EnumerateArray())
{
    string id = result.GetProperty("id").GetString()!;
    Check(resultById.TryAdd(id, result), "duplicate result node: " + id);
}
foreach ((string id, JsonElement node) in nodeById)
{
    string type = node.GetProperty("type").GetString()!;
    bool isPage = type == "page";
    if (!isPage)
    {
        Check(resultById.ContainsKey(id), "leaf missing explicit result: " + id);
    }
    if (isPage && node.TryGetProperty("control_inventory", out JsonElement inventory))
    {
        var direct = nodeById.Values
            .Where(x => x.TryGetProperty("parent", out JsonElement p) && p.GetString() == id)
            .Select(x => x.GetProperty("id").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var mapped = inventory.EnumerateArray()
            .Select(x => x.GetProperty("child").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Check(direct.SequenceEqual(mapped), "control_inventory mismatch: " + id);
    }
}
foreach ((string id, JsonElement result) in resultById)
{
    string status = result.GetProperty("status").GetString()!;
    Check(status is "blocked" or "needs-runtime-verify", "unexpected static result status: " + id + "=" + status);
    if (nodeById[id].GetProperty("type").GetString() == "transaction")
    {
        Check(status == "blocked", "transaction is not blocked: " + id);
    }
}
Check(resultById["mainui.composite.goods.compose"].GetProperty("blocked_reason").GetString()!.Contains("15028"), "goods compose missing 15028 hard-negative reason");
Check(resultById["mainui.composite.guard.compose"].GetProperty("blocked_reason").GetString()!.Contains("15028"), "guard compose missing 15028 hard-negative reason");

string modulePrefab = File.ReadAllText(modulePrefabPath);
var prefabPresence = new Dictionary<string, object>(StringComparer.Ordinal);
foreach (string view in views)
{
    bool inModule = Regex.IsMatch(modulePrefab, "^  m_Name: " + Regex.Escape(view) + "$", RegexOptions.Multiline);
    string standalone = Path.Combine(root, "Assets", "Prefabs", "UI", "Composite", view + ".prefab");
    bool standaloneExists = File.Exists(standalone);
    string business = Path.Combine(root, "Assets", "Scripts", "Module", "Core", "Composite", "Views", view + ".cs");
    prefabPresence[view] = new { in_module = inModule, standalone = standaloneExists, business_view = File.Exists(business) };
}
Check(((dynamic)prefabPresence["GodBeastCompositeView"]).in_module == false, "GodBeastCompositeView unexpectedly present in module");
Check(((dynamic)prefabPresence["CompositeRevelationView"]).in_module == false, "CompositeRevelationView unexpectedly present in module");
Check(((dynamic)prefabPresence["CompositeRuneView"]).standalone == true, "CompositeRuneView standalone prefab missing");

using JsonDocument menuDoc = JsonDocument.Parse(File.ReadAllText(menuPath));
JsonElement menu = menuDoc.RootElement;
int[] roots = { 2000, 3000, 80, 6000, 5000, 8000, 72, 26000, 78, 7700 };
var rootNames = new[] { "goods", "rune", "holy_seal", "god_beast", "unreal", "god_befall", "revelation", "guard", "god_court", "longlang" };
int[] ParseList(JsonElement entry)
{
    string raw = entry.GetProperty("list").GetString() ?? "[]";
    return Regex.Matches(raw, @"\d+").Select(x => int.Parse(x.Value)).ToArray();
}
int CountLeaves(int id, HashSet<int> stack)
{
    if (!menu.TryGetProperty(id.ToString(), out JsonElement entry)) return 1;
    if (!stack.Add(id)) return 1;
    int[] children = ParseList(entry);
    if (entry.GetProperty("is_bottom").GetInt32() == 0)
    {
        stack.Remove(id);
        return children.Length;
    }
    int total = children.Sum(child => CountLeaves(child, stack));
    stack.Remove(id);
    return total;
}
var menuSummary = new Dictionary<string, object>(StringComparer.Ordinal);
for (int i = 0; i < roots.Length; i++)
{
    JsonElement entry = menu.GetProperty(roots[i].ToString());
    menuSummary[rootNames[i]] = new
    {
        root_id = roots[i],
        open_lv = entry.GetProperty("open_lv").GetInt32(),
        direct_children = ParseList(entry).Length,
        terminal_rules = CountLeaves(roots[i], new HashSet<int>())
    };
}

string Sha(string path)
{
    using var stream = File.OpenRead(path);
    return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
}
var report = new
{
    verdict = errors.Count == 0 ? "pass" : "fail",
    errors,
    manifest = new
    {
        node_count = nodeById.Count,
        leaf_count = nodeById.Values.Count(x => x.GetProperty("type").GetString() != "page"),
        result_count = resultById.Count,
        blocked = resultById.Values.Count(x => x.GetProperty("status").GetString() == "blocked"),
        needs_runtime_verify = resultById.Values.Count(x => x.GetProperty("status").GetString() == "needs-runtime-verify")
    },
    labels,
    prefab_presence = prefabPresence,
    menu_summary = menuSummary,
    source_hashes = new Dictionary<string, string>
    {
        ["CompositeFlow.cs"] = Sha(flowPath),
        ["CompositeModule.prefab"] = Sha(modulePrefabPath),
        ["legacy CompositeView.ts"] = Sha(legacyViewPath),
        ["config_compose_menu.json"] = Sha(menuPath),
        ["route-manifest.json"] = Sha(manifestPath),
        ["static-results.json"] = Sha(resultsPath)
    }
};
string reportPath = Path.Combine(output, "static-verification.json");
File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
Console.WriteLine("VERDICT pass={0} nodes={1} leaves={2} blocked={3} needs-runtime-verify={4}",
    errors.Count == 0,
    nodeById.Count,
    resultById.Count,
    resultById.Values.Count(x => x.GetProperty("status").GetString() == "blocked"),
    resultById.Values.Count(x => x.GetProperty("status").GetString() == "needs-runtime-verify"));
foreach (string error in errors) Console.Error.WriteLine(error);
return errors.Count == 0 ? 0 : 1;
