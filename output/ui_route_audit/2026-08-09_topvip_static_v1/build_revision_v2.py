#!/usr/bin/env python3
"""Build the corrected schema-6 TopVip manifest/results without mutating v1."""

from __future__ import annotations

import copy
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
OUT = ROOT / "revision-v2"


def control(control_id: str, kind: str, child: str) -> dict:
    return {"id": control_id, "kind": kind, "child": child}


def new_node(node_id: str, parent: str, node_type: str = "read", risk: str = "read-only", *, note: str = "", controls: list[dict] | None = None) -> dict:
    value = {"id": node_id, "parent": parent, "type": node_type, "risk": risk}
    if note:
        value["note"] = note
    if controls is not None:
        value["control_inventory"] = controls
    return value


def blocked(node_id: str, reason: str) -> dict:
    return {"id": node_id, "status": "blocked", "blocked_reason": reason}


def main() -> None:
    manifest = json.loads((ROOT / "route-manifest.json").read_text(encoding="utf-8"))
    results = json.loads((ROOT / "static-results.json").read_text(encoding="utf-8"))
    nodes = manifest["nodes"]
    by_id = {item["id"]: item for item in nodes}
    result_by_id = {item["id"]: item for item in results["nodes"]}

    manifest["route"] = "mainui.topvip.revision-v2"
    manifest["baseline"] = copy.deepcopy(manifest.get("baseline", {}))
    manifest["baseline"].update({
        "revision": 2,
        "supersedes": "../route-ledger.json",
        "reconciliation": "adds entry/tab condition leaves, AlertTypeTwo controls, shared skill-point popup, and ShopBulkPurchase/Calculator branches",
        "config_snapshot": "6 skill stages x 8 skill tasks; 13 currency tasks; 5 permanent daily rewards"
    })

    root = by_id["topvip.route"]
    root["control_inventory"].extend([
        control("entry_denied_message", "conditional-message", "topvip.entry_denied"),
        control("tab_main_red", "conditional-red-dot", "topvip.tab_red.main"),
        control("tab_skill_red", "conditional-red-dot", "topvip.tab_red.skill"),
        control("tab_task_red", "conditional-red-dot", "topvip.tab_red.task"),
        control("tab_shop_red", "conditional-red-dot", "topvip.tab_red.shop"),
    ])
    additions = [
        new_node("topvip.entry_denied", "topvip.route", note="OPEN_TOP_VIP_BASE_VIEW below VIP4 shows denial message"),
        new_node("topvip.tab_red.main", "topvip.route", note="daily reward red"),
        new_node("topvip.tab_red.skill", "topvip.route", note="skill task red"),
        new_node("topvip.tab_red.task", "topvip.route", note="currency task red"),
        new_node("topvip.tab_red.shop", "topvip.route", note="shop affordable/quota red"),
    ]

    main_page = by_id["topvip.main"]
    for item in main_page["control_inventory"]:
        if item["child"] == "topvip.main.experience":
            item["child"] = "topvip.main.experience_confirm"
    old_experience = by_id.pop("topvip.main.experience")
    nodes.remove(old_experience)
    old_experience_result = result_by_id.pop("topvip.main.experience")
    results["nodes"].remove(old_experience_result)
    additions.extend([
        new_node("topvip.main.experience_confirm", "topvip.main", "page", controls=[
            control("content", "confirmation-text", "topvip.main.experience_confirm.content"),
            control("ok_btn", "transaction-button", "topvip.main.experience_confirm.confirm"),
            control("cancel_btn", "return", "topvip.main.experience_confirm.cancel"),
            control("close_btn", "return", "topvip.main.experience_confirm.close"),
            control("background", "return", "topvip.main.experience_confirm.background_close"),
        ]),
        new_node("topvip.main.experience_confirm.content", "topvip.main.experience_confirm"),
        new_node("topvip.main.experience_confirm.confirm", "topvip.main.experience_confirm", "transaction", "destructive-write", note="45106"),
        new_node("topvip.main.experience_confirm.cancel", "topvip.main.experience_confirm", "return"),
        new_node("topvip.main.experience_confirm.close", "topvip.main.experience_confirm", "return"),
        new_node("topvip.main.experience_confirm.background_close", "topvip.main.experience_confirm", "return"),
    ])

    promotion = by_id["topvip.main.promotion"]
    for item in promotion["control_inventory"]:
        if item["child"] == "topvip.promotion.buy":
            item["child"] = "topvip.promotion.buy_confirm"
    old_buy = by_id.pop("topvip.promotion.buy")
    nodes.remove(old_buy)
    old_buy_result = result_by_id.pop("topvip.promotion.buy")
    results["nodes"].remove(old_buy_result)
    additions.extend([
        new_node("topvip.promotion.buy_confirm", "topvip.main.promotion", "page", controls=[
            control("content", "confirmation-text", "topvip.promotion.buy_confirm.content"),
            control("ok_btn", "transaction-button", "topvip.promotion.buy_confirm.confirm"),
            control("cancel_btn", "return", "topvip.promotion.buy_confirm.cancel"),
            control("close_btn", "return", "topvip.promotion.buy_confirm.close"),
            control("background", "return", "topvip.promotion.buy_confirm.background_close"),
        ]),
        new_node("topvip.promotion.buy_confirm.content", "topvip.promotion.buy_confirm"),
        new_node("topvip.promotion.buy_confirm.confirm", "topvip.promotion.buy_confirm", "transaction", "destructive-write", note="45107"),
        new_node("topvip.promotion.buy_confirm.cancel", "topvip.promotion.buy_confirm", "return"),
        new_node("topvip.promotion.buy_confirm.close", "topvip.promotion.buy_confirm", "return"),
        new_node("topvip.promotion.buy_confirm.background_close", "topvip.promotion.buy_confirm", "return"),
    ])

    skill = by_id["topvip.skill"]
    skill["control_inventory"].append(control("point_popup", "conditional-popup", "topvip.skill.point_popup"))
    additions.extend([
        new_node("topvip.skill.point_popup", "topvip.skill", "page", controls=[
            control("attribute_lb", "config-text", "topvip.skill.point_popup.content"),
            control("tip_gp", "positioned-panel", "topvip.skill.point_popup.position"),
            control("bg", "return", "topvip.skill.point_popup.close"),
        ]),
        new_node("topvip.skill.point_popup.content", "topvip.skill.point_popup", note="8 stage/sub-stage attribute variants"),
        new_node("topvip.skill.point_popup.position", "topvip.skill.point_popup", note="opens relative to clicked point with y offset -40"),
        new_node("topvip.skill.point_popup.close", "topvip.skill.point_popup", "return"),
    ])
    by_id["topvip.skill.tasks"]["note"] = "48 config rows: 6 stages x 8; each row has state/go/45103/reward leaves"
    by_id["topvip.task.list"]["note"] = "13 config rows (101-113) with level-conditional visibility and 2-4 reward cells"
    by_id["topvip.main.daily_rewards"]["note"] = "permanent VIP daily reward config contains 5 reward cells"

    bulk = by_id["topvip.shop.list.bulk_buy"]
    bulk.update({
        "type": "page",
        "risk": "read-only",
        "note": "shared ShopBulkPurchaseView; route is enumerated but popup remains outside writable island",
        "control_inventory": [
            control("goods_icon", "shared-item", "topvip.shop.bulk.goods"),
            control("cost_icon", "shared-item", "topvip.shop.bulk.cost"),
            control("limit_and_price", "state", "topvip.shop.bulk.state"),
            control("minus_ten", "local-write", "topvip.shop.bulk.minus10"),
            control("minus_one", "local-write", "topvip.shop.bulk.minus1"),
            control("cur_show_num", "calculator-popup", "topvip.shop.bulk.calculator"),
            control("add_one", "local-write", "topvip.shop.bulk.plus1"),
            control("add_ten", "local-write", "topvip.shop.bulk.plus10"),
            control("confirmBtn", "conditional-transaction", "topvip.shop.bulk.confirm_routes"),
            control("cancleBtn", "return", "topvip.shop.bulk.cancel"),
            control("closeBtn", "return", "topvip.shop.bulk.close"),
            control("background", "return", "topvip.shop.bulk.background_close"),
        ]
    })
    old_bulk_result = result_by_id.pop("topvip.shop.list.bulk_buy")
    results["nodes"].remove(old_bulk_result)
    additions.extend([
        new_node("topvip.shop.bulk.goods", "topvip.shop.list.bulk_buy", "navigation", note="BaseAwardItem goods detail"),
        new_node("topvip.shop.bulk.cost", "topvip.shop.list.bulk_buy", "navigation", note="BaseAwardItem cost detail"),
        new_node("topvip.shop.bulk.state", "topvip.shop.list.bulk_buy", note="quota, current quantity, total price, enough/insufficient color"),
        new_node("topvip.shop.bulk.minus10", "topvip.shop.list.bulk_buy", "reversible-write", "reversible-write"),
        new_node("topvip.shop.bulk.minus1", "topvip.shop.list.bulk_buy", "reversible-write", "reversible-write"),
        new_node("topvip.shop.bulk.plus1", "topvip.shop.list.bulk_buy", "reversible-write", "reversible-write"),
        new_node("topvip.shop.bulk.plus10", "topvip.shop.list.bulk_buy", "reversible-write", "reversible-write"),
        new_node("topvip.shop.bulk.calculator", "topvip.shop.list.bulk_buy", "page", controls=[
            control("key_0_9", "numeric-key-matrix", "topvip.shop.bulk.calculator.digits"),
            control("key_11", "backspace", "topvip.shop.bulk.calculator.back"),
            control("key_12", "return", "topvip.shop.bulk.calculator.confirm"),
            control("click_bg", "return", "topvip.shop.bulk.calculator.background_close"),
        ]),
        new_node("topvip.shop.bulk.calculator.digits", "topvip.shop.bulk.calculator", "reversible-write", "reversible-write", note="keys 0-9 and max-count clamp"),
        new_node("topvip.shop.bulk.calculator.back", "topvip.shop.bulk.calculator", "reversible-write", "reversible-write"),
        new_node("topvip.shop.bulk.calculator.confirm", "topvip.shop.bulk.calculator", "return"),
        new_node("topvip.shop.bulk.calculator.background_close", "topvip.shop.bulk.calculator", "return"),
        new_node("topvip.shop.bulk.confirm_routes", "topvip.shop.list.bulk_buy", "page", controls=[
            control("direct_15302", "transaction-branch", "topvip.shop.bulk.confirm.direct"),
            control("bound_currency_substitute", "conditional-alert", "topvip.shop.bulk.confirm.substitute"),
            control("quick_use_overflow", "conditional-alert", "topvip.shop.bulk.confirm.quickuse"),
            control("diamond_insufficient", "conditional-navigation", "topvip.shop.bulk.confirm.recharge"),
            control("other_currency_insufficient", "conditional-message", "topvip.shop.bulk.confirm.insufficient"),
        ]),
        new_node("topvip.shop.bulk.confirm.direct", "topvip.shop.bulk.confirm_routes", "transaction", "destructive-write", note="15302 key_id + quantity"),
        new_node("topvip.shop.bulk.confirm.substitute", "topvip.shop.bulk.confirm_routes", "page", controls=[
            control("content", "confirmation-text", "topvip.shop.bulk.substitute.content"),
            control("check", "checkbox", "topvip.shop.bulk.substitute.check"),
            control("ok_btn", "transaction-button", "topvip.shop.bulk.substitute.confirm"),
            control("cancel_btn", "return", "topvip.shop.bulk.substitute.cancel"),
            control("close_btn", "return", "topvip.shop.bulk.substitute.close"),
            control("background", "return", "topvip.shop.bulk.substitute.background_close"),
        ]),
        new_node("topvip.shop.bulk.substitute.content", "topvip.shop.bulk.confirm.substitute"),
        new_node("topvip.shop.bulk.substitute.check", "topvip.shop.bulk.confirm.substitute", "reversible-write", "reversible-write"),
        new_node("topvip.shop.bulk.substitute.confirm", "topvip.shop.bulk.confirm.substitute", "transaction", "destructive-write", note="15302 after bound-currency substitution"),
        new_node("topvip.shop.bulk.substitute.cancel", "topvip.shop.bulk.confirm.substitute", "return"),
        new_node("topvip.shop.bulk.substitute.close", "topvip.shop.bulk.confirm.substitute", "return"),
        new_node("topvip.shop.bulk.substitute.background_close", "topvip.shop.bulk.confirm.substitute", "return"),
        new_node("topvip.shop.bulk.confirm.quickuse", "topvip.shop.bulk.confirm_routes", "page", controls=[
            control("content", "confirmation-text", "topvip.shop.bulk.quickuse.content"),
            control("ok_btn", "transaction-button", "topvip.shop.bulk.quickuse.confirm"),
            control("cancel_btn", "return", "topvip.shop.bulk.quickuse.cancel"),
            control("close_btn", "return", "topvip.shop.bulk.quickuse.close"),
            control("background", "return", "topvip.shop.bulk.quickuse.background_close"),
        ]),
        new_node("topvip.shop.bulk.quickuse.content", "topvip.shop.bulk.confirm.quickuse"),
        new_node("topvip.shop.bulk.quickuse.confirm", "topvip.shop.bulk.confirm.quickuse", "transaction", "destructive-write", note="15302 quick-use branch"),
        new_node("topvip.shop.bulk.quickuse.cancel", "topvip.shop.bulk.confirm.quickuse", "return"),
        new_node("topvip.shop.bulk.quickuse.close", "topvip.shop.bulk.confirm.quickuse", "return"),
        new_node("topvip.shop.bulk.quickuse.background_close", "topvip.shop.bulk.confirm.quickuse", "return"),
        new_node("topvip.shop.bulk.confirm.recharge", "topvip.shop.bulk.confirm_routes", "navigation", "destructive-write", note="NotEnougnDiamond / recharge route"),
        new_node("topvip.shop.bulk.confirm.insufficient", "topvip.shop.bulk.confirm_routes", note="insufficient-currency message"),
        new_node("topvip.shop.bulk.cancel", "topvip.shop.list.bulk_buy", "return"),
        new_node("topvip.shop.bulk.close", "topvip.shop.list.bulk_buy", "return", note="shared popup uses dedicated CLOSE sound"),
        new_node("topvip.shop.bulk.background_close", "topvip.shop.list.bulk_buy", "return"),
    ])

    nodes.extend(additions)
    existing_ids = {item["id"] for item in results["nodes"]}
    parents = {item.get("parent") for item in nodes if item.get("parent")}
    special_reason = {
        "topvip.main.experience_confirm.confirm": "本轮未点击；45106 体验购买是 R601 hard-negative，禁止实现孤立协议",
        "topvip.promotion.buy_confirm.confirm": "本轮未点击；45107 永久购买是 R601 hard-negative，禁止实现孤立协议",
        "topvip.shop.bulk.confirm.direct": "本轮未点击；15302 批量购买未授权",
        "topvip.shop.bulk.substitute.confirm": "本轮未点击；绑元替代确认后仍会发送 15302，未授权",
        "topvip.shop.bulk.quickuse.confirm": "本轮未点击；快速使用分支仍会发送 15302，未授权",
        "topvip.shop.bulk.confirm.recharge": "本轮未点击；充值导航未授权",
    }
    for item in nodes:
        node_id = item["id"]
        if node_id in parents or node_id in existing_ids:
            continue
        if node_id in special_reason:
            reason = special_reason[node_id]
        elif item["type"] == "transaction":
            reason = "本轮未点击；交易叶未授权，且所在页面/弹窗缺完整 Unity 实现"
        elif item["risk"] == "destructive-write":
            reason = "本轮未点击；充值/购买导航未授权"
        else:
            reason = "完整 TopVip 页面/弹窗 Prefab 与业务 View 缺失；按本轮约束不触发转换或运行"
        results["nodes"].append(blocked(node_id, reason))

    # Make every existing transaction/recharge blocker explicitly state that no click occurred.
    refreshed_by_id = {item["id"]: item for item in nodes}
    for item in results["nodes"]:
        spec = refreshed_by_id[item["id"]]
        if (spec["type"] == "transaction" or spec["risk"] == "destructive-write") and "未点击" not in item["blocked_reason"]:
            item["blocked_reason"] = "本轮未点击；" + item["blocked_reason"]

    results["updated_at"] = "2026-08-09T15:48:00+08:00"
    results["summary_details"] = {
        "completion_level": "static inventory revision 2",
        "supersedes": "../route-ledger.json",
        "topology_corrections": "entry/tab conditions; two AlertTypeTwo dialogs; skill point popup; ShopBulkPurchase and Calculator branches",
        "runtime": "not run by instruction",
        "transactions": "every transaction/recharge/purchase leaf explicitly blocked and not clicked",
        "convert_module": "blocked: requires real legacy runtime snapshots, Unity MCP bake/backfill, Addressables registration, Unity compile/runtime diff"
    }

    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "route-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (OUT / "static-results.json").write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
