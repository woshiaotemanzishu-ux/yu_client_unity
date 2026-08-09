import json
from pathlib import Path


OUT = Path(__file__).resolve().parent
V2 = OUT.parent / "2026-08-09_vip_main_revision_v2"
OLD_PREFIX = "mainui.vip.v2"
NEW_PREFIX = "mainui.vip.v3"
OLD_STATIC = "output/ui_route_audit/2026-08-09_vip_main_revision_v2/static-audit.md"
NEW_STATIC = "output/ui_route_audit/2026-08-09_vip_main_revision_v3/static-audit.md"
TYPE2_PREFIX = OLD_PREFIX + ".recharge.candidates.type2-product"


def rename(value):
    if isinstance(value, str):
        return value.replace(OLD_PREFIX, NEW_PREFIX).replace(OLD_STATIC, NEW_STATIC)
    if isinstance(value, list):
        return [rename(item) for item in value]
    if isinstance(value, dict):
        return {key: rename(item) for key, item in value.items()}
    return value


def main():
    manifest = json.loads((V2 / "route-manifest.json").read_text(encoding="utf-8"))
    results = json.loads((V2 / "results-static.json").read_text(encoding="utf-8"))

    manifest["nodes"] = [node for node in manifest["nodes"] if not node["id"].startswith(TYPE2_PREFIX)]
    candidate_page = next(node for node in manifest["nodes"]
                          if node["id"] == OLD_PREFIX + ".recharge.candidates")
    candidate_page["control_inventory"] = [item for item in candidate_page["control_inventory"]
                                           if not item["child"].startswith(TYPE2_PREFIX)]
    candidate_page["note"] = (
        "权威 config_recharge_product SHA e521... 仅有 7 个 type1 候选 product_id=2..8；"
        "显示集合仍必须与 15800 有序快照取交集。type2 数量未知，只存在于 15901 动态模板。"
    )

    results["nodes"] = [node for node in results["nodes"] if not node["id"].startswith(TYPE2_PREFIX)]
    manifest = rename(manifest)
    results = rename(results)
    manifest["route"] = NEW_PREFIX
    baseline = manifest["baseline"]
    baseline["manifest_revision"] = 3
    baseline["supersedes"] = "output/ui_route_audit/2026-08-09_vip_main_revision_v2/route-ledger.json"
    frozen = baseline["frozen_inventory"]
    frozen["recharge_type1_candidates"] = 7
    frozen.pop("recharge_type1_type2_candidates", None)
    frozen["recharge_candidate_ids"] = [2, 3, 4, 5, 6, 7, 8]
    frozen["recharge_type2_candidates"] = "dynamic-only via 15901; no static count fabricated"
    frozen["recharge_visibility"] = "7 type1 candidates intersect ordered 15800 snapshot"
    baseline["revision_reason"] = (
        "v2 错把不存在于权威 config_recharge_product(e521...) 的 7 个 type2 候选展开为静态拓扑；"
        "v3 删除这些无证据分支，15901 继续只冻结动态模板。"
    )

    ids = [node["id"] for node in manifest["nodes"]]
    assert len(ids) == len(set(ids))
    assert not any(".candidates.type2-product" in node_id for node_id in ids)
    assert sum(node["parent"] == NEW_PREFIX + ".recharge.candidates" and
               ".candidates.type1-product" in node["id"] for node in manifest["nodes"]) == 7
    known = set(ids)
    for node in manifest["nodes"]:
        if node["parent"] is not None:
            assert node["parent"] in known, (node["id"], node["parent"])
        for item in node.get("control_inventory", []):
            assert item["child"] in known, (node["id"], item["child"])

    parent_ids = {node["parent"] for node in manifest["nodes"] if node["parent"] is not None}
    leaves = {node["id"] for node in manifest["nodes"] if node["id"] not in parent_ids}
    result_ids = {node["id"] for node in results["nodes"]}
    assert leaves == result_ids, (len(leaves), len(result_ids), sorted(leaves ^ result_ids)[:5])
    assert all(isinstance(node.get("applicable_gates"), list) for node in results["nodes"])
    assert all(node["status"] in ("blocked", "needs-runtime-verify") for node in results["nodes"])

    (OUT / "route-manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (OUT / "results-static.json").write_text(
        json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"nodes": len(ids), "leaves": len(leaves),
                      "pages": len(ids) - len(leaves)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
