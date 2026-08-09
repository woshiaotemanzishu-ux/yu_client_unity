#!/usr/bin/env python3
"""Generate Team route topology v3 from the preserved v2 manifest.

The v1/v2 manifests and ledgers are immutable.  This helper only writes the
new v3 manifest before route_ledger.py init creates the schema-6 ledger.
"""

from __future__ import annotations

import json
from pathlib import Path


HERE = Path(__file__).resolve().parent
V2 = HERE.parent / "2026-08-09_team_static_v2" / "route-manifest.json"
OUT = HERE / "route-manifest.json"


def control(control_id: str, kind: str, child: str) -> dict:
    return {"id": control_id, "kind": kind, "child": child}


def node(
    node_id: str,
    parent: str,
    node_type: str = "read",
    risk: str = "read-only",
    inventory: list[dict] | None = None,
) -> dict:
    value = {"id": node_id, "parent": parent, "type": node_type, "risk": risk}
    if inventory is not None:
        value["control_inventory"] = inventory
    return value


def main() -> None:
    manifest = json.loads(V2.read_text(encoding="utf-8"))
    if manifest.get("route") != "mainui.team" or len(manifest.get("nodes", [])) != 127:
        raise SystemExit("unexpected v2 Team topology")

    manifest["baseline"]["topology_revision"] = 3
    manifest["baseline"]["notes"] = (
        "老端源码、Laya JSON、Unity Prefab/Bind、Generated wire 与服务端只读交叉盘点；"
        "v3 补齐附近邀请的场景变化定时刷新、24053 重拉和销毁清理生命周期，"
        "并把更改目标确认拆为已有队伍 24017 与无队伍本地成功回调两条互斥叶；"
        "未启动 Unity/浏览器，未执行账号写事务。"
    )

    nodes = manifest["nodes"]
    by_id = {item["id"]: item for item in nodes}

    def require(node_id: str) -> dict:
        return by_id[node_id]

    def add(item: dict) -> None:
        if item["id"] in by_id:
            raise ValueError(f"duplicate v3 node: {item['id']}")
        nodes.append(item)
        by_id[item["id"]] = item

    # TeamInviteView Nearby tab owns a lifecycle refresh group: it detects scene
    # changes on a timer, re-pulls 24053, and clears the timer on destroy.
    nearby = require("mainui.team.view.invite.nearby")
    query_control = next(
        item for item in nearby["control_inventory"]
        if item["child"] == "mainui.team.view.invite.nearby.query"
    )
    query_control["id"] = "scene-refresh-lifecycle"
    query_control["kind"] = "lifecycle-query"

    nearby_query = require("mainui.team.view.invite.nearby.query")
    nearby_query["type"] = "page"
    nearby_query["control_inventory"] = [
        control(
            "scene-change-timer",
            "lifecycle-state",
            "mainui.team.view.invite.nearby.query.scene-change-timer",
        ),
        control(
            "request-24053",
            "query",
            "mainui.team.view.invite.nearby.query.request-24053",
        ),
        control(
            "destroy-clear-timer",
            "lifecycle-state",
            "mainui.team.view.invite.nearby.query.destroy-clear-timer",
        ),
    ]
    add(node(
        "mainui.team.view.invite.nearby.query.scene-change-timer",
        "mainui.team.view.invite.nearby.query",
    ))
    add(node(
        "mainui.team.view.invite.nearby.query.request-24053",
        "mainui.team.view.invite.nearby.query",
    ))
    add(node(
        "mainui.team.view.invite.nearby.query.destroy-clear-timer",
        "mainui.team.view.invite.nearby.query",
    ))

    # Confirm is not one transaction: the old client has two mutually exclusive
    # branches based on whether a team already exists.
    confirm = require("mainui.team.view.change-target.confirm")
    confirm["type"] = "page"
    confirm["control_inventory"] = [
        control(
            "existing-team-request-24017",
            "conditional-transaction",
            "mainui.team.view.change-target.confirm.existing-team-24017",
        ),
        control(
            "no-team-local-success",
            "conditional-transaction",
            "mainui.team.view.change-target.confirm.no-team-change-target-success",
        ),
    ]
    add(node(
        "mainui.team.view.change-target.confirm.existing-team-24017",
        "mainui.team.view.change-target.confirm",
        "transaction",
        "destructive-write",
    ))
    add(node(
        "mainui.team.view.change-target.confirm.no-team-change-target-success",
        "mainui.team.view.change-target.confirm",
        "transaction",
        "destructive-write",
    ))

    parent_ids = {item["parent"] for item in nodes if item.get("parent")}
    leaves = [item for item in nodes if item["id"] not in parent_ids]
    if len(nodes) != 132 or len(leaves) != 105:
        raise SystemExit(f"unexpected v3 topology: nodes={len(nodes)} leaves={len(leaves)}")

    OUT.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )


if __name__ == "__main__":
    main()
