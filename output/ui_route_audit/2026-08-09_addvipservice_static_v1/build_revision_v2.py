#!/usr/bin/env python3
"""Build the corrected schema-6 AddVipService manifest/results without mutating v1."""

from __future__ import annotations

import copy
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
OUT = ROOT / "revision-v2"


def control(control_id: str, kind: str, child: str) -> dict:
    return {"id": control_id, "kind": kind, "child": child}


def node(node_id: str, parent: str, node_type: str = "read", risk: str = "read-only", *, note: str = "", controls: list[dict] | None = None) -> dict:
    value = {"id": node_id, "parent": parent, "type": node_type, "risk": risk}
    if note:
        value["note"] = note
    if controls is not None:
        value["control_inventory"] = controls
    return value


def blocked(node_id: str, reason: str) -> dict:
    return {"id": node_id, "status": "blocked", "blocked_reason": reason}


def remove_leaf(nodes: list[dict], results: list[dict], node_id: str) -> None:
    nodes[:] = [item for item in nodes if item["id"] != node_id]
    results[:] = [item for item in results if item["id"] != node_id]


def main() -> None:
    manifest = json.loads((ROOT / "route-manifest.json").read_text(encoding="utf-8"))
    result_doc = json.loads((ROOT / "static-results.json").read_text(encoding="utf-8"))
    nodes = manifest["nodes"]
    results = result_doc["nodes"]
    by_id = {item["id"]: item for item in nodes}

    manifest["route"] = "mainui.addvipservice.revision-v2"
    manifest["baseline"] = copy.deepcopy(manifest.get("baseline", {}))
    manifest["baseline"].update({
        "revision": 2,
        "supersedes": "../route-ledger.json",
        "reconciliation": "adds alpha/first-charge/channel/level/open-day entry gates and exact 0/3 reward variants",
        "config_snapshot": "ClientAddVipService rows 1 and 2; 14 top-level channels; row1 open_lv 90/reward 0; row2 open_lv 150/reward 3"
    })

    # Expand the previously collapsed entry-condition leaf into the actual gate chain.
    remove_leaf(nodes, results, "addvip.entry_condition")
    additions = [
        node("addvip.entry_condition", "addvip.route", "page", controls=[
            control("non_alpha", "condition", "addvip.entry.non_alpha"),
            control("first_recharge_15908", "condition", "addvip.entry.first_recharge"),
            control("channel_tem", "condition", "addvip.entry.channel"),
            control("channel_row_1", "conditional-variant", "addvip.entry.variant1"),
            control("channel_row_2", "conditional-variant", "addvip.entry.variant2"),
            control("open_level_day", "condition", "addvip.entry.level_day"),
        ]),
        node("addvip.entry.non_alpha", "addvip.entry_condition", note="PlatformManager.is_alpha must be false"),
        node("addvip.entry.first_recharge", "addvip.entry_condition", note="15908 is_buy == 1 latches IsShowAddVipServiceIcon"),
        node("addvip.entry.channel", "addvip.entry_condition", note="plat_name must match one of 14 top-level tem entries"),
        node("addvip.entry.variant1", "addvip.entry_condition", note="13 channels: displayed icon_name 114, open_lv 90, open_day 0"),
        node("addvip.entry.variant2", "addvip.entry_condition", note="yy_suyou: displayed icon_name 117, open_lv 150, open_day 0 while logical icon type remains 114"),
        node("addvip.entry.level_day", "addvip.entry_condition", note="ActivityIconManager applies the selected row open_lv/open_day gate"),
    ]

    # Expand config selection into both actual rows plus the unmatched branch.
    remove_leaf(nodes, results, "addvip.config_selection")
    additions.extend([
        node("addvip.config_selection", "addvip.route", "page", controls=[
            control("row_1", "conditional-config", "addvip.config.row1"),
            control("row_2", "conditional-config", "addvip.config.row2"),
            control("unmatched", "conditional-empty", "addvip.config.unmatched"),
        ]),
        node("addvip.config.row1", "addvip.config_selection", note="ui_gz_title + image001 + row1 des/des1 + zero rewards"),
        node("addvip.config.row2", "addvip.config_selection", note="ui_gz_title2 + image002 + row2 des/des1 + three rewards"),
        node("addvip.config.unmatched", "addvip.config_selection", note="LoadSuccess returns without filling dynamic content"),
    ])

    # Replace wildcard reward leaves with the exact config snapshot.
    rewards = by_id["addvip.rewards"]
    rewards["control_inventory"] = [
        control("row1_empty", "conditional-empty-list", "addvip.rewards.row1_empty"),
        control("row2_reward_0", "shared-item-navigation", "addvip.rewards.row2.item0"),
        control("row2_reward_1", "shared-item-navigation", "addvip.rewards.row2.item1"),
        control("row2_reward_2", "shared-item-navigation", "addvip.rewards.row2.item2"),
        control("reward_layout", "layout", "addvip.rewards.layout"),
    ]
    remove_leaf(nodes, results, "addvip.rewards.data")
    remove_leaf(nodes, results, "addvip.rewards.item_detail")
    additions.extend([
        node("addvip.rewards.row1_empty", "addvip.rewards", note="row1 reward=[]; Content must stay empty without stale clones"),
        node("addvip.rewards.row2.item0", "addvip.rewards", "navigation", note="mapped reward [0,35,200]; exact EquipmentItem detail/return"),
        node("addvip.rewards.row2.item1", "addvip.rewards", "navigation", note="mapped reward [0,31,200000]; exact EquipmentItem detail/return"),
        node("addvip.rewards.row2.item2", "addvip.rewards", "navigation", note="mapped reward [0,37020002,1]; exact EquipmentItem detail/return"),
    ])

    nodes.extend(additions)
    existing_ids = {item["id"] for item in results}
    parents = {item.get("parent") for item in nodes if item.get("parent")}
    for item in nodes:
        node_id = item["id"]
        if node_id in parents or node_id in existing_ids:
            continue
        if node_id.startswith("addvip.entry"):
            reason = "ClientAddVipService 配置/白名单消费链未迁移，入口条件无法在当前 Unity 路线真实成立"
        elif node_id.startswith("addvip.config"):
            reason = "ClientAddVipService 未迁移，渠道配置分支无 Unity 业务消费者"
        else:
            reason = "AddVipServiceView 业务脚本与配置驱动克隆链缺失，无法运行核对"
        results.append(blocked(node_id, reason))

    result_doc["updated_at"] = "2026-08-09T15:48:00+08:00"
    result_doc["summary_details"] = {
        "completion_level": "static inventory revision 2",
        "supersedes": "../route-ledger.json",
        "topology_corrections": "entry gate chain and exact two-channel/zero-or-three-reward variants",
        "runtime": "not run by instruction",
        "transactions": "no recharge/claim/purchase transaction exists on this page",
        "fix_view": "blocked by missing business View and prohibited ClientConfigSync/MainUI integration scope; existing Prefab preserved"
    }

    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "route-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (OUT / "static-results.json").write_text(json.dumps(result_doc, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
