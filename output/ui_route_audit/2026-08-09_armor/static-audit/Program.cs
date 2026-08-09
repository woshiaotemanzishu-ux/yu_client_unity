using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: ArmorStaticAudit <repo-root>");
    return 2;
}

string root = Path.GetFullPath(args[0]);
string output = Path.Combine(root, "output", "ui_route_audit", "2026-08-09_armor");
string legacy = @"E:\GitProject\yu_client";
string legacyViewPath = Path.Combine(legacy, "h5", "src", "equipArmor", "EquipArmorView.ts");
string legacyItemPath = Path.Combine(legacy, "h5", "src", "equipArmor", "ArmorItem.ts");
string legacyAttrPath = Path.Combine(legacy, "h5", "src", "equipArmor", "ArmorAttrView.ts");
string armorControllerPath = Path.Combine(root, "Assets", "Scripts", "Module", "Core", "Armor", "ArmorController.cs");
string armorModelPath = Path.Combine(root, "Assets", "Scripts", "Module", "Core", "Armor", "ArmorModel.cs");
string armorConfigsPath = Path.Combine(root, "Assets", "Scripts", "Module", "Core", "Armor", "ArmorConfigs.cs");
string unityViewPath = Path.Combine(root, "Assets", "Scripts", "Module", "Core", "Equip", "Views", "EquipArmorView.cs");
string attrViewPath = Path.Combine(root, "Assets", "Scripts", "Module", "Core", "Equip", "Views", "ArmorAttrView.cs");
string equipFlowPath = Path.Combine(root, "Assets", "Scripts", "Module", "Core", "Equip", "EquipFlow.cs");
string prefabPath = Path.Combine(root, "Assets", "Prefabs", "UI", "EquipArmor", "EquipArmorModule.prefab");
string attrPrefabPath = Path.Combine(root, "Assets", "Prefabs", "UI", "EquipArmor", "ArmorAttrItem.prefab");
string manifestPath = Path.Combine(output, "route-manifest.json");
string resultsPath = Path.Combine(output, "results-static.json");
string equipmentPath = Path.Combine(root, "Assets", "GameRes", "resource", "config", "server", "config_armour_equipment.json");
string suitPath = Path.Combine(root, "Assets", "GameRes", "resource", "config", "server", "config_armour_suit.json");
string kvPath = Path.Combine(root, "Assets", "GameRes", "resource", "config", "server", "config_armour_kv.json");

var errors = new List<string>();
void Check(bool condition, string message) { if (!condition) errors.Add(message); }
string Read(string path) => File.ReadAllText(path);

string legacyView = Read(legacyViewPath);
string legacyItem = Read(legacyItemPath);
string legacyAttr = Read(legacyAttrPath);
string controller = Read(armorControllerPath);
string model = Read(armorModelPath);
string configs = Read(armorConfigsPath);
string unityView = Read(unityViewPath);
string attrView = Read(attrViewPath);
string equipFlow = Read(equipFlowPath);
string prefab = Read(prefabPath);

foreach (string token in new[]
{
    "_btn_all", "_btn_make", "leftBtn", "rightBtn", "_gp_tabs", "_gp_equips", "_gp_mat",
    "_gp_curr", "_gp_attr", "_gp_attr2", "_img_red", "_img_red1", "_img_red2"
})
    Check(legacyView.Contains(token, StringComparison.Ordinal), "legacy view missing control: " + token);
Check(legacyView.Contains("PlayBigEffect(\"ui_dazaochengong\", { x: -0.85, y: 0.55 }, 1.5)", StringComparison.Ordinal), "legacy success effect call changed");
Check(legacyItem.Contains("this.item.SetShowTips(false)", StringComparison.Ordinal), "legacy position item no-tip semantic missing");
Check(legacyAttr.Contains("click_bg_toClose = true", StringComparison.Ordinal), "legacy attr popup background close semantic missing");

Check(controller.Contains("UserMsgAdapter.Encode(Proto.ARMOR_MAKE, \"ccc\"", StringComparison.Ordinal), "14402 request wire is not ccc");
Check(controller.Contains("bool applied = ArmorModel.Instance.ApplyMakeResult(code, delta)", StringComparison.Ordinal), "14402 result is not delegated to authoritative model");
Check(model.Contains("if (code != 1) return false;", StringComparison.Ordinal), "failed 14402 may mutate model");
Check(configs.Contains("if (!cost.IsArmorState) real.Add(cost);", StringComparison.Ordinal), "armor-state cost is not excluded from real bag costs");
Check(configs.Contains("MissingPreviousStage", StringComparison.Ordinal), "previous-stage gate missing");

foreach (string name in new[] { "EquipArmorView", "ArmorAttrView", "ArmorItem", "ArmorTabItem" })
    Check(prefab.Contains("  m_Name: " + name, StringComparison.Ordinal), "prefab missing node: " + name);
foreach (string component in new[]
{
    "Shenxiao.Module.Core::Shenxiao.Module.Core.Equip.EquipArmorView",
    "Shenxiao.Module.Core::Shenxiao.Module.Core.Equip.ArmorAttrView",
    "UnityEngine.UI::UnityEngine.UI.ScrollRect",
    "UnityEngine.UI::UnityEngine.UI.RectMask2D"
})
    Check(prefab.Contains(component, StringComparison.Ordinal), "prefab missing component: " + component);
Check(File.Exists(attrPrefabPath), "shared ArmorAttrItem prefab missing");

using JsonDocument equipment = JsonDocument.Parse(Read(equipmentPath));
using JsonDocument suit = JsonDocument.Parse(Read(suitPath));
using JsonDocument kv = JsonDocument.Parse(Read(kvPath));
Check(equipment.RootElement.EnumerateObject().Count() == 90, "equipment config row count != 90");
Check(suit.RootElement.EnumerateObject().Count() == 18, "suit config row count != 18");
Check(kv.RootElement.EnumerateObject().Count() == 2, "kv config row count != 2");
var levels = suit.RootElement.EnumerateObject()
    .Select(x => x.Value.GetProperty("open_lv").GetInt32()).Distinct().OrderBy(x => x).ToArray();
Check(levels.SequenceEqual(new[] { 450, 470, 490, 520, 550, 580, 610, 640, 670 }), "suit open levels changed");

using JsonDocument manifest = JsonDocument.Parse(Read(manifestPath));
using JsonDocument results = JsonDocument.Parse(Read(resultsPath));
var nodeById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
foreach (JsonElement node in manifest.RootElement.GetProperty("nodes").EnumerateArray())
{
    string id = node.GetProperty("id").GetString()!;
    Check(nodeById.TryAdd(id, node), "duplicate node: " + id);
}
var resultById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
foreach (JsonElement result in results.RootElement.GetProperty("nodes").EnumerateArray())
{
    string id = result.GetProperty("id").GetString()!;
    Check(resultById.TryAdd(id, result), "duplicate result: " + id);
}
foreach ((string id, JsonElement node) in nodeById)
{
    string type = node.GetProperty("type").GetString()!;
    if (type != "page") Check(resultById.ContainsKey(id), "leaf missing explicit result: " + id);
    if (type == "page" && node.TryGetProperty("control_inventory", out JsonElement inventory))
    {
        string[] direct = nodeById.Values
            .Where(x => x.TryGetProperty("parent", out JsonElement p) && p.GetString() == id)
            .Select(x => x.GetProperty("id").GetString()!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        string[] mapped = inventory.EnumerateArray().Select(x => x.GetProperty("child").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Check(direct.SequenceEqual(mapped), "control inventory mismatch: " + id);
    }
}
foreach ((string id, JsonElement result) in resultById)
{
    string status = result.GetProperty("status").GetString()!;
    Check(status is "blocked" or "needs-runtime-verify", "unexpected result status: " + id + "=" + status);
    if (nodeById[id].GetProperty("type").GetString() == "transaction") Check(status == "blocked", "transaction not blocked: " + id);
}

// These are deliberate cross-island blockers, not production changes in this route.
Check(!unityView.Contains("ui_dazaochengong", StringComparison.Ordinal), "success effect blocker unexpectedly stale");
Check(!unityView.Contains("SetClickCallBack", StringComparison.Ordinal), "position item click blocker unexpectedly stale");
Check(!unityView.Contains("SetGray", StringComparison.Ordinal), "material/position gray blocker unexpectedly stale");
Check(!unityView.Contains("神创", StringComparison.Ordinal), "locked-stage copy blocker unexpectedly stale");
Check(attrView.Contains("UIUtil.AddClick(_img_bg, Hide)", StringComparison.Ordinal), "attr popup close blocker unexpectedly stale");
Check(!equipFlow.Contains("Label =", StringComparison.Ordinal), "Equip tab label blocker unexpectedly stale");
foreach (string id in new[]
{
    "mainui.equip.armor.entry", "mainui.equip.armor.stages.locked", "mainui.equip.armor.types.skin",
    "mainui.equip.armor.positions.state", "mainui.equip.armor.materials.quantity",
    "mainui.equip.armor.make.effect", "mainui.equip.armor.total-attrs.close", "mainui.equip.armor.root-red"
})
    Check(resultById[id].GetProperty("status").GetString() == "blocked", "known cross-island defect not blocked: " + id);

string Sha(string path)
{
    using FileStream stream = File.OpenRead(path);
    return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
}
var report = new
{
    verdict = errors.Count == 0 ? "pass" : "fail",
    errors,
    manifest = new
    {
        node_count = nodeById.Count,
        leaf_count = resultById.Count,
        blocked = resultById.Values.Count(x => x.GetProperty("status").GetString() == "blocked"),
        needs_runtime_verify = resultById.Values.Count(x => x.GetProperty("status").GetString() == "needs-runtime-verify"),
    },
    configs = new { equipment_rows = 90, suit_rows = 18, kv_rows = 2, open_levels = levels },
    prefab = new { module = "Assets/Prefabs/UI/EquipArmor/EquipArmorModule.prefab", shared_attr_item = "Assets/Prefabs/UI/EquipArmor/ArmorAttrItem.prefab" },
    known_cross_island_blockers = new[]
    {
        "EquipFlow tab label", "stage locked copy/selected skin", "type selected skin",
        "position BaseAward click interception and gray", "material insufficient gray",
        "success ui_dazaochengong effect", "ArmorAttrView mask close identity", "MainUI root red dot",
    },
    source_hashes = new Dictionary<string, string>
    {
        ["ArmorController.cs"] = Sha(armorControllerPath),
        ["ArmorModel.cs"] = Sha(armorModelPath),
        ["ArmorConfigs.cs"] = Sha(armorConfigsPath),
        ["EquipArmorModule.prefab"] = Sha(prefabPath),
        ["legacy EquipArmorView.ts"] = Sha(legacyViewPath),
        ["route-manifest.json"] = Sha(manifestPath),
        ["results-static.json"] = Sha(resultsPath),
    },
};
File.WriteAllText(Path.Combine(output, "static-verification.json"),
    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
Console.WriteLine("VERDICT pass={0} nodes={1} leaves={2} blocked={3} needs-runtime-verify={4}",
    errors.Count == 0, nodeById.Count, resultById.Count,
    resultById.Values.Count(x => x.GetProperty("status").GetString() == "blocked"),
    resultById.Values.Count(x => x.GetProperty("status").GetString() == "needs-runtime-verify"));
foreach (string error in errors) Console.Error.WriteLine(error);
return errors.Count == 0 ? 0 : 1;
