"""Independent, output-only static validator for the Market schema-6 route.

This script is intentionally read-only.  It validates the frozen manifest,
applied ledger, current old-client config cardinalities and the allowed Unity
file island without launching Unity or executing any market protocol.
"""

from __future__ import annotations

import hashlib
import json
from collections import Counter
from pathlib import Path


HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
OLD_CLIENT = Path("E:/GitProject/yu_client")

MANIFEST_PATH = HERE / "route-manifest-v2.json"
RESULTS_PATH = HERE / "results-static-v2.json"
LEDGER_PATH = HERE / "route-ledger-v2.json"


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


manifest = load(MANIFEST_PATH)
results = load(RESULTS_PATH)
ledger = load(LEDGER_PATH)

assert manifest["route"] == "mainui.market"
assert ledger["schema"] == 6
assert ledger["route"] == manifest["route"]

manifest_hash = hashlib.sha256(MANIFEST_PATH.read_bytes()).hexdigest()
assert ledger["manifest_source"]["sha256"] == manifest_hash
assert ledger["manifest_source"]["path"].replace("\\", "/").endswith(
    "output/ui_route_audit/2026-08-09_market/route-manifest-v2.json"
)

manifest_nodes = manifest["nodes"]
ledger_nodes = ledger["nodes"]
manifest_by_id = {node["id"]: node for node in manifest_nodes}
ledger_by_id = {node["id"]: node for node in ledger_nodes}
assert len(manifest_nodes) == len(manifest_by_id) == 376
assert len(ledger_nodes) == len(ledger_by_id) == 376
assert set(manifest_by_id) == set(ledger_by_id)

children: dict[str, list[str]] = {node_id: [] for node_id in manifest_by_id}
roots = []
for node in manifest_nodes:
    parent = node.get("parent")
    if parent is None:
        roots.append(node["id"])
    else:
        assert parent in manifest_by_id, (node["id"], parent)
        children[parent].append(node["id"])
assert roots == ["mainui.market"]

for node_id, source in manifest_by_id.items():
    applied = ledger_by_id[node_id]
    for key in ("parent", "type", "risk", "control_inventory"):
        assert applied.get(key) == source.get(key), (node_id, key)

    inventory = source.get("control_inventory") or []
    inventory_children = [item["child"] for item in inventory]
    assert len(inventory_children) == len(set(inventory_children)), node_id
    assert set(inventory_children) == set(children[node_id]), node_id

leaf_ids = {node_id for node_id, value in children.items() if not value}
result_by_id = {node["id"]: node for node in results["nodes"]}
assert len(leaf_ids) == len(result_by_id) == 335
assert leaf_ids == set(result_by_id)

result_statuses = Counter(node["status"] for node in results["nodes"])
assert result_statuses == {"blocked": 320, "needs-runtime-verify": 15}
ledger_statuses = Counter(node["status"] for node in ledger_nodes)
assert ledger_statuses == {"blocked": 361, "needs-runtime-verify": 15}
assert ledger["summary"] == {
    "total": 376,
    "status_counts": {"blocked": 361, "needs-runtime-verify": 15},
}
assert "done" not in ledger_statuses

for leaf_id, result in result_by_id.items():
    applied = ledger_by_id[leaf_id]
    assert result["status"] == applied["status"]
    assert result["status"] in {"blocked", "needs-runtime-verify"}
    if manifest_by_id[leaf_id]["risk"] == "destructive-write":
        assert result["status"] == "blocked", leaf_id

write_protocols = {"15106", "15108", "15111", "15115", "15116", "15117", "15122"}
read_protocols = {"15100", "15101", "15102", "15109", "15112", "15114", "15118", "15119", "15120", "15121"}
for command in write_protocols:
    assert result_by_id[f"mainui.market.protocols.p{command}"]["status"] == "blocked"
for command in read_protocols:
    assert result_by_id[f"mainui.market.protocols.p{command}"]["status"] == "needs-runtime-verify"
assert result_by_id["mainui.market.protocols.exclusions"]["status"] == "needs-runtime-verify"

sell_types = load(
    OLD_CLIENT / "cdn/resource/config/server/config_goods_sell_type.json"
)
sell_subtypes = load(
    OLD_CLIENT / "cdn/resource/config/server/config_goods_sell_subtype.json"
)
active_types = {int(key) for key in sell_types if 1 <= int(key) <= 8}
active_subtypes = [
    value for value in sell_subtypes.values() if int(value["type"]) in active_types
]
buy_subtypes = len(active_subtypes)
plz_subtypes = sum(int(value["subtype"]) != 0 for value in active_subtypes)
counts = manifest["baseline"]["static_counts"]
assert len(active_types) == counts["top_categories"] == 8
assert buy_subtypes == counts["buy_subtype_cells"] == 69
assert plz_subtypes == counts["plz_subtype_cells"] == 61
assert counts["visible_primary_tabs"] == 3
assert counts["quality_options"] == 6
assert counts["stage_options"] == 17
assert counts["star_options"] == 6

market_cs_dir = REPO / "Assets/Scripts/Module/Core/Market"
assert sorted(path.name for path in market_cs_dir.glob("*.cs")) == [
    "MarketController.cs",
    "MarketModel.cs",
]

market_prefab_dir = REPO / "Assets/Prefabs/UI/Market"
market_prefabs = sorted(market_prefab_dir.glob("*.prefab"))
assert [path.name for path in market_prefabs] == ["MarketPlzShowItem.prefab"]
prefab_text = market_prefabs[0].read_text(encoding="utf-8-sig")
assert "Shenxiao.Generated.UI.Market.MarketPlzShowItemBind" in prefab_text
assert "EquipmentItem" in prefab_text

chat_prefab = REPO / "Assets/Prefabs/UI/Chat/ChatModule.prefab"
assert chat_prefab.is_file()
assert "_tpl_MarketPlzShowItem" in chat_prefab.read_text(encoding="utf-8-sig")

print(
    "STATIC_ASSERTIONS PASS "
    f"nodes={len(manifest_nodes)} leaves={len(leaf_ids)} "
    f"blocked={ledger_statuses['blocked']} "
    f"needs={ledger_statuses['needs-runtime-verify']} "
    f"prefabs={len(market_prefabs)} marketCs=2 categories={len(active_types)} "
    f"buySubtypes={buy_subtypes} plzSubtypes={plz_subtypes} "
    f"writeProtocols={len(write_protocols)}"
)
