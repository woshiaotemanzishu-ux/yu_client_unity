import json
from pathlib import Path


OUT = Path(__file__).resolve().parent
V3 = OUT.parent / "2026-08-09_vip_main_revision_v3"
OLD_PREFIX = "mainui.vip.v3"
NEW_PREFIX = "mainui.vip.v4"
OLD_STATIC = "output/ui_route_audit/2026-08-09_vip_main_revision_v3/static-audit.md"
NEW_STATIC = "output/ui_route_audit/2026-08-09_vip_main_revision_v4/static-audit.md"


def rename(value):
    if isinstance(value, str):
        return value.replace(OLD_PREFIX, NEW_PREFIX).replace(OLD_STATIC, NEW_STATIC)
    if isinstance(value, list):
        return [rename(item) for item in value]
    if isinstance(value, dict):
        return {key: rename(item) for key, item in value.items()}
    return value


def main():
    old_manifest = json.loads((V3 / "route-manifest.json").read_text(encoding="utf-8"))
    old_results = json.loads((V3 / "results-static.json").read_text(encoding="utf-8"))
    manifest = rename(old_manifest)
    results = rename(old_results)

    # v4 is evidence-only: after route-prefix normalization, node topology must be byte-equivalent as JSON.
    assert manifest["nodes"] == rename(old_manifest["nodes"])
    assert len(manifest["nodes"]) == 1347
    assert len(results["nodes"]) == 1013

    manifest["route"] = NEW_PREFIX
    baseline = manifest["baseline"]
    baseline["manifest_revision"] = 4
    baseline["supersedes"] = "output/ui_route_audit/2026-08-09_vip_main_revision_v3/route-ledger.json"
    baseline["revision_reason"] = (
        "v3 节点拓扑正确，但配置证据误绑定 cdn/assets 原始副本；v4 仅重绑老 H5 实际 "
        "ResUrl→cdn/resource 与 config.zip PRELOAD 消费链，节点、父子、类型、风险和控件清单零变化。"
    )
    baseline["authority"] = (
        "当前老 H5 由 ResUrl/URL.formatURL 消费 cdn/resource；LoadGameConfig 优先读取 Config.PRELOAD，"
        "未预载项才读取 GameResPath 的 resource/config/client|server 散文件。真实运行表现仍是最终权威。"
    )
    baseline["config_evidence"] = [
        {"path": "E:/GitProject/yu_client/cdn/resource/config/client/ClientVipPrivilege.json",
         "sha256": "c5cef232b28faf29b536a2ef18cc103ed19c04c9490d061cace8c3a2943c0dc1",
         "consumer": "VipPrivilegeCardView/VipRuleView; not present in config.zip PRELOAD"},
        {"path": "E:/GitProject/yu_client/cdn/resource/config/server/config_vip_card.json",
         "sha256": "0f17f7a6da10828bedbceac7336c93c39fb96579db374155cff9c8682058f73b",
         "consumer": "VipPrivilegeCardView; not present in config.zip PRELOAD"},
        {"path": "E:/GitProject/yu_client/cdn/resource/config/server/config_vip_config.json",
         "sha256": "dfdb45285cfe664c05badcb0c250a5861c2d581be5f8939057596401066fbda6",
         "consumer": "VipPrivilegeShowView; not present in config.zip PRELOAD"},
        {"path": "E:/GitProject/yu_client/cdn/resource/config/client/ClientVipWelfare.json",
         "sha256": "4f5ccb17d2e3877a2271624ca2836a17bf5b51d85c049abacd6082ef4c8d9182",
         "consumer": "VipPrivilegeShowView; not present in config.zip PRELOAD"},
        {"path": "E:/GitProject/yu_client/cdn/resource/config.zip",
         "sha256": "031984617dbcf27128265961b76014b7a4e68c7d047de46e6448ae4fcf2b3ac9",
         "consumer": "ResManager.LoadConfigZip → Config.PRELOAD_CLIENT_CONFIG/PRELOAD_SERVER_CONFIG"},
        {"path": "Assets/GameRes/resource/config/client/clientvipwelfare.json",
         "sha256": "4f5ccb17d2e3877a2271624ca2836a17bf5b51d85c049abacd6082ef4c8d9182",
         "consumer": "Unity mirrored read-only welfare presentation config"},
    ]
    baseline["preload_config_evidence"] = {
        "container": "E:/GitProject/yu_client/cdn/resource/config.zip",
        "embedded_entry": "config.json",
        "embedded_entry_sha256": "aeea254274a99f9b092328d476bcae72b78fdbfe119700144e66ab4d0acca43a",
        "note": "内嵌对象没有独立文件路径，以下为按 key 排序、紧凑 UTF-8 JSON 的 canonical SHA-256。",
        "objects": [
            {"name": "config_recharge_product", "entries": 95,
             "canonical_sha256": "1dbb6c0ca8db235a741f858182d8a97728d1fcd9d05f93f015de23af754ff264",
             "type1_candidates": [2, 3, 4, 5, 6, 7, 8], "type2_candidates": []},
            {"name": "config_recharge_return", "entries": 16,
             "canonical_sha256": "c6a13d4f7902edc8660f91e1dbcaa127c1e63a503c66ed980add5f89b11328ea"},
            {"name": "ClientRechargeShow", "entries": 11,
             "canonical_sha256": "e5087fd768de584f34ed2ee192d114e301819000eccb5ce918f3f53a0f12cdbb"},
        ],
        "not_preloaded": ["ClientVipPrivilege", "config_vip_card", "config_vip_config", "ClientVipWelfare"],
    }
    loading_chain = [
        "E:/GitProject/yu_client/h5/src/util/GameResPath.ts",
        "E:/GitProject/yu_client/h5/src/GameScriptModule.ts",
        "E:/GitProject/yu_client/h5/src/common/ResManager.ts",
        "E:/GitProject/yu_client/h5/src/common/Config.ts",
        "E:/GitProject/yu_client/h5/laya2cdn.bat",
    ]
    baseline["loading_chain_sources"] = loading_chain
    for source in loading_chain:
        if source not in baseline["legacy_sources"]:
            baseline["legacy_sources"].append(source)

    ids = [node["id"] for node in manifest["nodes"]]
    assert len(ids) == len(set(ids))
    known = set(ids)
    for node in manifest["nodes"]:
        if node["parent"] is not None:
            assert node["parent"] in known
        for item in node.get("control_inventory", []):
            assert item["child"] in known
    parent_ids = {node["parent"] for node in manifest["nodes"] if node["parent"] is not None}
    leaves = {node["id"] for node in manifest["nodes"] if node["id"] not in parent_ids}
    assert leaves == {node["id"] for node in results["nodes"]}
    assert all(node["status"] in ("blocked", "needs-runtime-verify") for node in results["nodes"])
    assert all(isinstance(node.get("applicable_gates"), list) for node in results["nodes"])

    (OUT / "route-manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (OUT / "results-static.json").write_text(
        json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"nodes": len(ids), "leaves": len(leaves),
                      "pages": len(ids) - len(leaves), "topology_delta_from_v3": 0}, ensure_ascii=False))


if __name__ == "__main__":
    main()
