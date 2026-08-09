#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
from collections import Counter
from pathlib import Path


REPO = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


expected_hashes = {
    REPO / "Assets/Scripts/Module/Core/Compete/CompeteController.cs": "f59e2c5ec211c56e84f7c3df59a0818b1cf17e2119ac8079b33dfe37b989cbc6",
    REPO / "Assets/Scripts/Module/Core/Compete/CompeteModel.cs": "76a1835213ffa78dde5186870956c3c36f426944f145e64e39f4f17fd722e66b",
    REPO / "Assets/Prefabs/UI/Competelist/CompetelistModule.prefab": "44e7fe011d7f41469f33fe4a0f7b97848f006d2a3aca0da54933afa6fbed2388",
}
for path, expected in expected_hashes.items():
    actual = sha256(path)
    assert actual == expected, f"target changed during audit: {path} {actual} != {expected}"

manifest = json.loads((HERE / "route-manifest.json").read_text(encoding="utf-8"))
ledger = json.loads((HERE / "route-ledger.json").read_text(encoding="utf-8"))
results = json.loads((HERE / "results-static.json").read_text(encoding="utf-8"))
dependencies = json.loads((HERE / "component-dependencies.json").read_text(encoding="utf-8"))
nvr = json.loads((HERE / "nvr-matrix.json").read_text(encoding="utf-8"))

assert manifest["route"] == "mainui.compete.338"
assert ledger["schema"] == 6
assert len(manifest["nodes"]) == 49
parent_ids = {node.get("parent") for node in manifest["nodes"] if node.get("parent")}
leaf_ids = {node["id"] for node in manifest["nodes"] if node["id"] not in parent_ids}
assert len(leaf_ids) == 40
assert {node["id"] for node in results["nodes"]} == leaf_ids
assert Counter(node["status"] for node in ledger["nodes"]) == Counter(
    {"baseline-only": 5, "blocked": 24, "defect": 20}
)

prefab = (REPO / "Assets/Prefabs/UI/Competelist/CompetelistModule.prefab").read_text(encoding="utf-8")
expected_components = {
    "UnityEngine.UI::UnityEngine.UI.ScrollRect": 4,
    "UnityEngine.UI::UnityEngine.UI.RectMask2D": 4,
    "UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup": 7,
    "UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup": 0,
    "UnityEngine.UI::UnityEngine.UI.GridLayoutGroup": 0,
    "UnityEngine.UI::UnityEngine.UI.ContentSizeFitter": 0,
    "Shenxiao.Generated.UI.Competelist.CompetelistViewBind": 1,
    "Shenxiao.Generated.UI.Competelist.CompetelistRewardViewBind": 1,
    "Shenxiao.Generated.UI.Competelist.CompetelistIntegralItemBind": 1,
    "Shenxiao.Generated.UI.Competelist.CompetelistRankItemBind": 1,
    "Shenxiao.Generated.UI.Competelist.CompetelistRewaedItemBind": 1,
}
for marker, expected in expected_components.items():
    actual = prefab.count(marker)
    assert actual == expected, f"prefab marker {marker}: {actual} != {expected}"

missing_configs = [
    "Assets/GameRes/resource/config/server/config_race_act_info.json",
    "Assets/GameRes/resource/config/server/config_race_act_stage_reward.json",
    "Assets/GameRes/resource/config/server/config_race_act_rank_reward.json",
    "Assets/GameRes/resource/config/server/config_race_act_reward.json",
    "Assets/GameRes/resource/config/client/ClientConfigRaceActRewardShow.json",
    "Assets/GameRes/resource/config/client/ClientCompetelistSkill.json",
]
assert all(not (REPO / relative).exists() for relative in missing_configs)

assert len(dependencies["components"]) == 7
assert all(item["status"] in {"defect", "blocked"} for item in dependencies["components"])
assert len(nvr["checks"]) == 16
assert all(item["status"] in {"blocked", "needs-runtime-verify"} for item in nvr["checks"])

print("compete_static_validation=PASS")
print("manifest_nodes=49 leaf_nodes=40")
print("ledger_status=baseline-only:5,blocked:24,defect:20")
print("production_hashes=stable")
print("runtime=NVR/blocked")
