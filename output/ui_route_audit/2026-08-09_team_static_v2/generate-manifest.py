#!/usr/bin/env python3
"""Generate the corrected Team route topology from the preserved v1 ledger source.

The v1 manifest/ledger remain immutable.  This script only creates the v2
manifest before route_ledger.py init is called for the new topology.
"""

from __future__ import annotations

import json
from pathlib import Path


HERE = Path(__file__).resolve().parent
V1 = HERE.parent / "2026-08-09_team_static_v1" / "route-manifest.json"
OUT = HERE / "route-manifest.json"


def control(control_id: str, kind: str, child: str) -> dict:
    return {"id": control_id, "kind": kind, "child": child}


def node(node_id: str, parent: str, node_type: str = "read", risk: str = "read-only", inventory=None) -> dict:
    value = {"id": node_id, "parent": parent, "type": node_type, "risk": risk}
    if inventory is not None:
        value["control_inventory"] = inventory
    return value


def main() -> None:
    manifest = json.loads(V1.read_text(encoding="utf-8"))
    manifest["baseline"]["topology_revision"] = 2
    manifest["baseline"]["notes"] = (
        "老端源码、Laya JSON、Unity Prefab/Bind、Generated wire 与服务端只读交叉盘点；"
        "v2 补齐打开查询、条件按钮组、数字键盘/校验、列表项状态、Sentient Alert 分支及复扫漏项；"
        "未采集运行截图。"
    )

    nodes = manifest["nodes"]
    by_id = {item["id"]: item for item in nodes}

    def require(node_id: str) -> dict:
        return by_id[node_id]

    def add(item: dict) -> None:
        if item["id"] in by_id:
            raise ValueError(f"duplicate v2 node: {item['id']}")
        nodes.append(item)
        by_id[item["id"]] = item

    def remove(*node_ids: str) -> None:
        for node_id in node_ids:
            item = by_id.pop(node_id)
            nodes.remove(item)

    # TeamView: load-time 24010, background close, label/state matrices,
    # non-leader feedback, world-shout countdown and conditional auto-create.
    view = require("mainui.team.view")
    view["control_inventory"] = [
        control("initial-query-24010", "query", "mainui.team.view.initial-query-24010"),
        control("close", "button", "mainui.team.view.close"),
        control("background-close", "background", "mainui.team.view.background-close"),
        control("tips-btn", "button", "mainui.team.view.small-desc"),
        control("target-btn", "button", "mainui.team.view.change-target"),
        control("target-summary", "dynamic-text", "mainui.team.view.target-summary"),
        control("team-no-team-button-groups", "conditional-state", "mainui.team.view.button-groups"),
        control("non-leader-prompts", "conditional-state", "mainui.team.view.non-leader-prompts"),
        control("create-btn", "button", "mainui.team.view.create"),
        control("auto-create-on-open", "conditional-transaction", "mainui.team.view.auto-create-on-open"),
        control("hall-scroll", "list", "mainui.team.view.hall"),
        control("team-scroll", "list", "mainui.team.view.members"),
        control("invite-btn", "button", "mainui.team.view.invite"),
        control("world-btn", "button", "mainui.team.view.world-shout"),
        control("world-shout-state", "dynamic-state", "mainui.team.view.world-shout-state"),
        control("apply-list-btn", "button", "mainui.team.view.apply"),
        control("apply-red-dot", "conditional-state", "mainui.team.view.apply-red-dot"),
        control("empty-state", "conditional-state", "mainui.team.view.empty-state"),
    ]
    add(node("mainui.team.view.initial-query-24010", "mainui.team.view"))
    add(node("mainui.team.view.background-close", "mainui.team.view", "return"))
    add(node("mainui.team.view.target-summary", "mainui.team.view"))
    add(node(
        "mainui.team.view.button-groups",
        "mainui.team.view",
        "page",
        inventory=[
            control("with-team", "conditional-state", "mainui.team.view.button-groups.with-team"),
            control("without-team", "conditional-state", "mainui.team.view.button-groups.without-team"),
        ],
    ))
    add(node("mainui.team.view.button-groups.with-team", "mainui.team.view.button-groups"))
    add(node("mainui.team.view.button-groups.without-team", "mainui.team.view.button-groups"))
    add(node(
        "mainui.team.view.non-leader-prompts",
        "mainui.team.view",
        "page",
        inventory=[
            control("target", "conditional-message", "mainui.team.view.non-leader-prompts.target"),
            control("world-shout", "conditional-message", "mainui.team.view.non-leader-prompts.world-shout"),
            control("apply-list", "conditional-message", "mainui.team.view.non-leader-prompts.apply-list"),
        ],
    ))
    add(node("mainui.team.view.non-leader-prompts.target", "mainui.team.view.non-leader-prompts"))
    add(node("mainui.team.view.non-leader-prompts.world-shout", "mainui.team.view.non-leader-prompts"))
    add(node("mainui.team.view.non-leader-prompts.apply-list", "mainui.team.view.non-leader-prompts"))
    add(node("mainui.team.view.auto-create-on-open", "mainui.team.view", "transaction", "destructive-write"))
    add(node(
        "mainui.team.view.world-shout-state",
        "mainui.team.view",
        "page",
        inventory=[
            control("countdown-text", "dynamic-text", "mainui.team.view.world-shout-state.countdown-text"),
            control("cooldown-click", "conditional-message", "mainui.team.view.world-shout-state.cooldown-click"),
            control("expiry-reset", "conditional-state", "mainui.team.view.world-shout-state.expiry-reset"),
        ],
    ))
    add(node("mainui.team.view.world-shout-state.countdown-text", "mainui.team.view.world-shout-state"))
    add(node("mainui.team.view.world-shout-state.cooldown-click", "mainui.team.view.world-shout-state"))
    add(node("mainui.team.view.world-shout-state.expiry-reset", "mainui.team.view.world-shout-state"))

    # Team member rows have a leader/self matrix controlling quit/kick buttons.
    members = require("mainui.team.view.members")
    members["control_inventory"].append(
        control("row-action-visibility", "conditional-state", "mainui.team.view.members.action-visibility")
    )
    add(node("mainui.team.view.members.action-visibility", "mainui.team.view.members"))

    # Invite tab items and each data page's actual row renderer were missing.
    invite = require("mainui.team.view.invite")
    invite["control_inventory"].insert(
        1, control("tab-strip", "list", "mainui.team.view.invite.tab-strip")
    )
    add(node(
        "mainui.team.view.invite.tab-strip",
        "mainui.team.view.invite",
        "page",
        inventory=[
            control("row-render", "list-item", "mainui.team.view.invite.tab-strip.row-render"),
            control("selected-state", "conditional-state", "mainui.team.view.invite.tab-strip.selected-state"),
        ],
    ))
    add(node("mainui.team.view.invite.tab-strip.row-render", "mainui.team.view.invite.tab-strip"))
    add(node("mainui.team.view.invite.tab-strip.selected-state", "mainui.team.view.invite.tab-strip"))
    for suffix in ("nearby", "friend", "guild"):
        page_id = f"mainui.team.view.invite.{suffix}"
        require(page_id)["control_inventory"].insert(
            2, control("row-render", "list-item", f"{page_id}.row-render")
        )
        add(node(f"{page_id}.row-render", page_id))

    # ApplyView queries on every open and mirrors team_info.join_type to the check image.
    apply_page = require("mainui.team.view.apply")
    apply_page["control_inventory"].insert(
        0, control("open-query-24047", "query", "mainui.team.view.apply.open-query-24047")
    )
    apply_page["control_inventory"].insert(
        3, control("join-type-check-state", "conditional-state", "mainui.team.view.apply.join-type-check-state")
    )
    add(node("mainui.team.view.apply.open-query-24047", "mainui.team.view.apply"))
    add(node("mainui.team.view.apply.join-type-check-state", "mainui.team.view.apply"))

    # Closing/destroying the invitation popup clears its model list and emits refresh.
    be_invited = require("mainui.team.be-invited")
    be_invited["control_inventory"].append(
        control("destroy-reset-list", "lifecycle-state", "mainui.team.be-invited.reset-list")
    )
    add(node("mainui.team.be-invited.reset-list", "mainui.team.be-invited", "reversible-write", "reversible-write"))

    # Change-target: explicit list item/state topology plus both number controls,
    # calculator lifecycle and all four validation-failure branches.
    remove(
        "mainui.team.view.change-target.list",
        "mainui.team.view.change-target.select",
        "mainui.team.view.change-target.min-level",
        "mainui.team.view.change-target.max-level",
    )
    change_target = require("mainui.team.view.change-target")
    change_target["control_inventory"] = [
        control("close", "button", "mainui.team.view.change-target.close"),
        control("target-list", "list", "mainui.team.view.change-target.list"),
        control("down-level", "button", "mainui.team.view.change-target.down-level-click"),
        control("up-level", "button", "mainui.team.view.change-target.up-level-click"),
        control("calculator-popup", "popup", "mainui.team.view.change-target.calculator"),
        control("confirm", "button", "mainui.team.view.change-target.confirm"),
    ]
    add(node(
        "mainui.team.view.change-target.list",
        "mainui.team.view.change-target",
        "page",
        inventory=[
            control("role-level-filter", "conditional-list", "mainui.team.view.change-target.list.role-level-filter"),
            control("scroll", "scroll", "mainui.team.view.change-target.list.scroll"),
            control("row-render", "list-item", "mainui.team.view.change-target.list.row-render"),
            control("selected-state", "conditional-state", "mainui.team.view.change-target.list.selected-state"),
        ],
    ))
    add(node("mainui.team.view.change-target.list.role-level-filter", "mainui.team.view.change-target.list"))
    add(node("mainui.team.view.change-target.list.scroll", "mainui.team.view.change-target.list"))
    add(node("mainui.team.view.change-target.list.row-render", "mainui.team.view.change-target.list"))
    add(node("mainui.team.view.change-target.list.selected-state", "mainui.team.view.change-target.list"))
    add(node("mainui.team.view.change-target.down-level-click", "mainui.team.view.change-target", "navigation"))
    add(node("mainui.team.view.change-target.up-level-click", "mainui.team.view.change-target", "navigation"))
    add(node(
        "mainui.team.view.change-target.calculator",
        "mainui.team.view.change-target",
        "page",
        inventory=[
            control("open-event", "popup-open", "mainui.team.view.change-target.calculator.open-event"),
            control("value-change", "input", "mainui.team.view.change-target.calculator.value-change"),
            control("close-callback", "popup-close", "mainui.team.view.change-target.calculator.close-callback"),
            control("validation", "conditional-state", "mainui.team.view.change-target.calculator.validation"),
        ],
    ))
    add(node("mainui.team.view.change-target.calculator.open-event", "mainui.team.view.change-target.calculator", "navigation"))
    add(node("mainui.team.view.change-target.calculator.value-change", "mainui.team.view.change-target.calculator", "reversible-write", "reversible-write"))
    add(node("mainui.team.view.change-target.calculator.close-callback", "mainui.team.view.change-target.calculator", "reversible-write", "reversible-write"))
    add(node(
        "mainui.team.view.change-target.calculator.validation",
        "mainui.team.view.change-target.calculator",
        "page",
        inventory=[
            control("min-below-config", "failure-state", "mainui.team.view.change-target.calculator.validation.min-below-config"),
            control("min-above-max", "failure-state", "mainui.team.view.change-target.calculator.validation.min-above-config-or-current-max"),
            control("max-above-config", "failure-state", "mainui.team.view.change-target.calculator.validation.max-above-config"),
            control("max-below-min", "failure-state", "mainui.team.view.change-target.calculator.validation.max-below-config-or-current-min"),
        ],
    ))
    for suffix in (
        "min-below-config",
        "min-above-config-or-current-max",
        "max-above-config",
        "max-below-config-or-current-min",
    ):
        add(node(
            f"mainui.team.view.change-target.calculator.validation.{suffix}",
            "mainui.team.view.change-target.calculator.validation",
            "reversible-write",
            "reversible-write",
        ))

    # MatchView Sentient full-team Alert is a popup with two controls.  Confirm
    # has mutually exclusive in-scene navigation and out-of-scene 24108 leaves.
    remove("mainui.team.match.sentient-transition")
    match = require("mainui.team.match")
    match["control_inventory"][-1] = control(
        "sentient-full-team-alert", "conditional-popup", "mainui.team.match.sentient-alert"
    )
    add(node(
        "mainui.team.match.sentient-alert",
        "mainui.team.match",
        "page",
        inventory=[
            control("open", "conditional-state", "mainui.team.match.sentient-alert.open"),
            control("auto-cancel-match", "conditional-transaction", "mainui.team.match.sentient-alert.auto-cancel-match"),
            control("confirm", "button", "mainui.team.match.sentient-alert.confirm"),
            control("cancel", "button", "mainui.team.match.sentient-alert.cancel"),
        ],
    ))
    add(node("mainui.team.match.sentient-alert.open", "mainui.team.match.sentient-alert"))
    add(node("mainui.team.match.sentient-alert.auto-cancel-match", "mainui.team.match.sentient-alert", "transaction", "destructive-write"))
    add(node(
        "mainui.team.match.sentient-alert.confirm",
        "mainui.team.match.sentient-alert",
        "page",
        inventory=[
            control("in-scene-find-way", "navigation", "mainui.team.match.sentient-alert.confirm.find-way"),
            control("off-scene-24108", "transaction", "mainui.team.match.sentient-alert.confirm.request-24108"),
        ],
    ))
    add(node("mainui.team.match.sentient-alert.confirm.find-way", "mainui.team.match.sentient-alert.confirm", "transaction", "destructive-write"))
    add(node("mainui.team.match.sentient-alert.confirm.request-24108", "mainui.team.match.sentient-alert.confirm", "transaction", "destructive-write"))
    add(node("mainui.team.match.sentient-alert.cancel", "mainui.team.match.sentient-alert", "return"))

    OUT.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
