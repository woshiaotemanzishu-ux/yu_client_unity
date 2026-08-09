#!/usr/bin/env python3
"""Generate the corrected Team v4 topology from preserved v3."""

from __future__ import annotations

import json
from pathlib import Path


HERE = Path(__file__).resolve().parent
V3 = HERE.parent / "2026-08-09_team_static_v3" / "route-manifest.json"
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
    result = {"id": node_id, "parent": parent, "type": node_type, "risk": risk}
    if inventory is not None:
        result["control_inventory"] = inventory
    return result


def main() -> None:
    manifest = json.loads(V3.read_text(encoding="utf-8"))
    if manifest.get("route") != "mainui.team" or len(manifest.get("nodes", [])) != 132:
        raise SystemExit("unexpected Team v3 source topology")

    manifest["baseline"]["topology_revision"] = 4
    manifest["baseline"]["notes"] = (
        "v4 修正 v3 对 TeamInviteView.GetAreaScenePlayer 的错误场景变化定时器解释："
        "先查询当前 scene；当前 scene 为野外时再通过 0.1s timer 遍历 GetAllFieldScene，"
        "跳过当前 scene 并逐个查询其他野外 scene，遍历结束及 Remove 均 ClearTimer；"
        "未启动 Unity/浏览器，未执行账号写事务。"
    )

    nodes = manifest["nodes"]
    by_id = {item["id"]: item for item in nodes}

    def require(node_id: str) -> dict:
        return by_id[node_id]

    def remove(*node_ids: str) -> None:
        for node_id in node_ids:
            item = by_id.pop(node_id)
            nodes.remove(item)

    def add(item: dict) -> None:
        if item["id"] in by_id:
            raise ValueError(f"duplicate v4 node: {item['id']}")
        nodes.append(item)
        by_id[item["id"]] = item

    query_id = "mainui.team.view.invite.nearby.query"
    remove(
        f"{query_id}.scene-change-timer",
        f"{query_id}.request-24053",
        f"{query_id}.destroy-clear-timer",
    )

    query = require(query_id)
    query["control_inventory"] = [
        control("current-scene-request-24053", "query", f"{query_id}.current-scene-request-24053"),
        control("field-scene-fanout", "conditional-query", f"{query_id}.field-scene-fanout"),
        control("remove-clear-timer", "lifecycle-state", f"{query_id}.remove-clear-timer"),
    ]

    fanout_id = f"{query_id}.field-scene-fanout"
    traversal_id = f"{fanout_id}.traversal"
    add(node(f"{query_id}.current-scene-request-24053", query_id))
    add(node(
        fanout_id,
        query_id,
        "page",
        inventory=[
            control("current-scene-is-field", "condition", f"{fanout_id}.current-scene-is-field"),
            control("clear-existing-timer", "lifecycle-state", f"{fanout_id}.clear-existing-timer"),
            control("period-timer-0.1s", "lifecycle-state", f"{fanout_id}.period-timer-0.1s"),
            control("traversal", "conditional-list", traversal_id),
        ],
    ))
    add(node(f"{fanout_id}.current-scene-is-field", fanout_id))
    add(node(f"{fanout_id}.clear-existing-timer", fanout_id))
    add(node(f"{fanout_id}.period-timer-0.1s", fanout_id))
    add(node(
        traversal_id,
        fanout_id,
        "page",
        inventory=[
            control("get-all-field-scenes", "list-source", f"{traversal_id}.get-all-field-scenes"),
            control("skip-current-scene", "condition", f"{traversal_id}.skip-current-scene"),
            control(
                "request-each-other-scene-24053",
                "query",
                f"{traversal_id}.request-each-other-scene-24053",
            ),
            control(
                "complete-clear-timer",
                "lifecycle-state",
                f"{traversal_id}.complete-clear-timer",
            ),
        ],
    ))
    add(node(f"{traversal_id}.get-all-field-scenes", traversal_id))
    add(node(f"{traversal_id}.skip-current-scene", traversal_id))
    add(node(f"{traversal_id}.request-each-other-scene-24053", traversal_id))
    add(node(f"{traversal_id}.complete-clear-timer", traversal_id))
    add(node(f"{query_id}.remove-clear-timer", query_id))

    parent_ids = {item["parent"] for item in nodes if item.get("parent")}
    leaves = [item for item in nodes if item["id"] not in parent_ids]
    if len(nodes) != 140 or len(leaves) != 111:
        raise SystemExit(f"unexpected v4 topology: nodes={len(nodes)} leaves={len(leaves)}")

    OUT.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )


if __name__ == "__main__":
    main()
