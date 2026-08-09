from __future__ import annotations

import hashlib
import json
import subprocess
from pathlib import Path


REPO = Path(r"E:\GitProject\yu_client_unity")
OLD = Path(r"E:\GitProject\yu_client")
OUT = REPO / "output/ui_route_audit/2026-08-09_parallel_ui_refine/sevenday"
ROUTE = "mainui.activity.sevenday"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def file_ref(path: Path) -> dict[str, object]:
    return {
        "path": str(path),
        "exists": path.is_file(),
        "sha256": sha256(path) if path.is_file() else None,
        "bytes": path.stat().st_size if path.is_file() else None,
    }


def write_json(name: str, value: object) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / name).write_text(
        json.dumps(value, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def node(
    node_id: str,
    parent: str | None,
    kind: str,
    risk: str = "read-only",
    controls: list[dict[str, str]] | None = None,
) -> dict[str, object]:
    value: dict[str, object] = {
        "id": node_id,
        "parent": parent,
        "type": kind,
        "risk": risk,
    }
    if controls is not None:
        value["control_inventory"] = controls
    return value


def control(control_id: str, kind: str, child: str) -> dict[str, str]:
    return {"id": control_id, "kind": kind, "child": child}


def build_manifest() -> dict[str, object]:
    nodes: list[dict[str, object]] = []
    root_children = [
        ("icon.175", "activity-icon", "navigation"),
        ("icon.175_at_8", "activity-icon", "navigation"),
        ("icon.175_1", "activity-icon", "navigation"),
        ("snapshot.open", "protocol-snapshot", "read"),
        ("snapshot.merge", "protocol-snapshot", "read"),
        ("refresh.level", "condition-refresh", "read"),
        ("refresh.task", "condition-refresh", "read"),
        ("icon.lifecycle", "conditional-visibility", "read"),
        ("icon.red-dot", "red-dot", "read"),
    ]
    nodes.append(
        node(
            ROUTE,
            None,
            "page",
            controls=[control(name, control_kind, f"{ROUTE}.{name}") for name, control_kind, _ in root_children],
        )
    )
    for suffix, _, kind in root_children:
        nodes.append(node(f"{ROUTE}.{suffix}", ROUTE, kind))

    pages = [
        ("open", "icon.175", "SevenDayView", "17501", list(range(1, 8))),
        ("eight", "icon.175_at_8", "SevenEightDayView", "17501", list(range(8, 15))),
        ("merge", "icon.175_1", "SevenMergeView", "17503", list(range(1, 8))),
    ]
    for page_key, nav_key, view_name, claim_cmd, days in pages:
        page_id = f"{ROUTE}.page.{page_key}"
        nav_id = f"{ROUTE}.{nav_key}"
        direct: list[tuple[str, str, str]] = []
        direct.extend((f"tab.day-{day}", "day-tab", "tab") for day in days)
        direct.extend(
            [
                ("dynamic-presentation", "visual-state", "read"),
                ("reward-list", "scroll-list", "read"),
                ("reward-detail.slot-1", "list-item", "navigation"),
                ("reward-detail.slot-2", "list-item", "navigation"),
                ("reward-detail.slot-3", "list-item", "navigation"),
                ("reward-detail.slot-4", "list-item", "navigation"),
                ("claim.unavailable", "claim-button-state", "read"),
                ("claim.available", "claim-button", "transaction"),
                ("claim.received", "claim-button-state", "read"),
                ("red-dot", "red-dot", "read"),
                ("close", "close-button", "return"),
            ]
        )
        nodes.append(
            node(
                page_id,
                nav_id,
                "page",
                controls=[control(name, control_kind, f"{page_id}.{name}") for name, control_kind, _ in direct],
            )
        )
        for suffix, _, kind in direct:
            risk = "destructive-write" if suffix == "claim.available" else "read-only"
            nodes.append(node(f"{page_id}.{suffix}", page_id, kind, risk=risk))

        claim_id = f"{page_id}.claim.available"
        claim_children = [
            ("protocol", "destructive-write"),
            ("immediate-refresh", "read"),
            ("success-popup", "navigation"),
            ("reopen", "read"),
        ]
        for suffix, kind in claim_children:
            nodes.append(node(f"{claim_id}.{suffix}", claim_id, kind, risk="destructive-write"))

    return {"route": ROUTE, "nodes": nodes}


STATIC_REF = "output/ui_route_audit/2026-08-09_parallel_ui_refine/sevenday/static-reconciliation.json"
COMPONENT_REF = "output/ui_route_audit/2026-08-09_parallel_ui_refine/sevenday/component-dependencies.json"


def build_results(manifest: dict[str, object]) -> dict[str, object]:
    results: list[dict[str, object]] = []
    root_baseline = {
        f"{ROUTE}.snapshot.open": "老端17500与Unity现有只读解析/缓存静态一致；本轮未运行真实客户端。",
        f"{ROUTE}.snapshot.merge": "老端17502与Unity现有只读解析/缓存静态一致；本轮未运行真实客户端。",
        f"{ROUTE}.refresh.level": "等级真变化重拉17500/17502的静态链已存在；本轮未运行。",
        f"{ROUTE}.refresh.task": "普通七日任务门槛跨越只重拉17500的静态链已存在；本轮未运行。",
        f"{ROUTE}.icon.lifecycle": "三入口互斥、明天可领文案与开放门仅有源码/配置对照，未做运行验证。",
    }
    for node_id, note in root_baseline.items():
        results.append({"id": node_id, "status": "baseline-only", "note": note, "evidence": [STATIC_REF]})
    results.append(
        {
            "id": f"{ROUTE}.icon.red-dot",
            "status": "needs-runtime-verify",
            "note": "增量补齐status=1入口红点派生，并在删图标前清理共享红点缓存。",
            "runtime_gap": "禁止启动Unity/浏览器；尚未验证175、175@8、175_1三入口的显示、切换、跨天与重开红点。",
            "evidence": [STATIC_REF],
        }
    )

    page_views = {"open": "SevenDayView", "eight": "SevenEightDayView", "merge": "SevenMergeView"}
    for item in manifest["nodes"]:
        node_id = str(item["id"])
        if ".page." not in node_id:
            continue
        if node_id.endswith(".claim.available.protocol"):
            cmd = "17503" if ".page.merge." in node_id else "17501"
            results.append(
                {
                    "id": node_id,
                    "status": "blocked",
                    "blocked_reason": f"{cmd}是真实背包/邮件发奖并持久化领取态的hard-negative；本轮无写事务授权，且三页/配置/单飞/错误/奖励链未闭环。",
                    "note": "禁止添加常量、sender、handler、乐观领取态、本地发奖或盲重试。",
                    "evidence": [STATIC_REF],
                }
            )
            continue
        if any(node_id.endswith(suffix) for suffix in (".claim.available.immediate-refresh", ".claim.available.success-popup", ".claim.available.reopen")):
            results.append(
                {
                    "id": node_id,
                    "status": "blocked",
                    "blocked_reason": "只能在权威领取成功链后验证；本轮禁止真实领取，且17501/17503仍为hard-negative。",
                    "evidence": [STATIC_REF, COMPONENT_REF],
                }
            )
            continue
        if node_id.endswith(".claim.available"):
            continue
        if any(node_id == f"{ROUTE}.page.{key}" for key in page_views):
            continue
        view = next((value for key, value in page_views.items() if f".page.{key}." in node_id), "SevenDayView")
        results.append(
            {
                "id": node_id,
                "status": "defect",
                "note": f"Unity缺少{view}可编辑Prefab/View/Bind/路由/配置消费者，静态无法承载该叶；真实像素、滚动、特效、cold/warm均未运行。",
                "evidence": [STATIC_REF, COMPONENT_REF],
            }
        )
    return {
        "updated_at": "2026-08-09T00:00:00+08:00",
        "summary_details": {
            "runtime_policy": "no Unity, no browser, no transaction, no build",
            "conversion": "blocked-before-convert: no legacy runtime snapshot and Unity execution forbidden",
        },
        "nodes": results,
    }


def build_reconciliation() -> dict[str, object]:
    old_sources = [
        OLD / "h5/src/commonController/SevenDayController.ts",
        OLD / "h5/src/commonModel/SevenDayModel.ts",
        OLD / "h5/src/sevenDay/SevenDayView.ts",
        OLD / "h5/src/sevenDay/SevenDayItem.ts",
        OLD / "h5/src/sevenDay/SevenEightDayView.ts",
        OLD / "h5/src/sevenDay/SevenEightDayItem.ts",
        OLD / "h5/src/sevenMergeDay/SevenMergeView.ts",
        OLD / "h5/src/sevenMergeDay/SevenMergeItem.ts",
        OLD / "h5/libs/proto175.d.ts",
    ]
    old_scenes = [
        OLD / "cdn/resource/game/sevenDay/SevenDayView.scene",
        OLD / "cdn/resource/game/sevenDay/SevenDayItem.scene",
        OLD / "cdn/resource/game/sevenDay/SevenEightDayView.scene",
        OLD / "cdn/resource/game/sevenDay/SevenEightDayItem.scene",
        OLD / "cdn/resource/game/sevenMergeDay/SevenMergeView.scene",
        OLD / "cdn/resource/game/sevenMergeDay/SevenMergeItem.scene",
    ]
    old_configs = [
        OLD / "cdn/resource/config/server/config_login_reward_day.json",
        OLD / "cdn/resource/config/server/config_login_merge_reward_day.json",
    ]
    unity_sources = [
        REPO / "Assets/Scripts/Module/Core/SevenDay/SevenDayController.cs",
        REPO / "Assets/Scripts/Module/Core/SevenDay/SevenDayModel.cs",
    ]
    return {
        "route": ROUTE,
        "audit_date": "2026-08-09",
        "starting_dirty": {
            "scope": [
                "Assets/Scripts/Module/Core/SevenDay",
                "Assets/Prefabs/UI/SevenDay",
                "output/ui_route_audit/2026-08-09_parallel_ui_refine/sevenday",
            ],
            "entries": [],
            "conclusion": "起始目标岛干净；本轮两份SevenDay C#差异可归属本代理，未发现并发同文件改动。",
        },
        "legacy_runtime": {
            "status": "not-run",
            "reason": "本轮明确禁止浏览器/Computer Use，且output中未发现可绑定到当前源码/账号/viewport的SevenDay既有真实运行证据。",
            "gates": [
                "old-client-same-account",
                "two-viewports",
                "pixel-overlay-diff",
                "scroll-and-last-item",
                "effect-two-frames",
                "cold-warm",
                "immediate-refresh",
                "close-reopen",
                "return-chain-runtime",
            ],
        },
        "legacy_source": {
            "files": [file_ref(path) for path in old_sources],
            "scenes": [file_ref(path) for path in old_scenes],
            "configs": [file_ref(path) for path in old_configs],
            "facts": {
                "entry_icons": ["175", "175@8", "175_1"],
                "views": ["SevenDayView", "SevenEightDayView", "SevenMergeView"],
                "snapshot_protocols": ["17500", "17502"],
                "claim_protocols": ["17501", "17503"],
                "day_tabs": {"SevenDayView": [1, 2, 3, 4, 5, 6, 7], "SevenEightDayView": [8, 9, 10, 11, 12, 13, 14], "SevenMergeView": [1, 2, 3, 4, 5, 6, 7]},
                "claim_states": {"0": "不可领提示", "1": "权威请求", "2": "已领取提示"},
                "reward_slots_rendered": 4,
                "post_success": ["status=2", "页面即时刷新", "入口/页签红点刷新", "CongratulationObtainView"],
                "presentation": {
                    "SevenDayView": "按日配置显示图片/模型/UI特效，另有ui_fazhen_05",
                    "SevenEightDayView": "按8..14日显示图片/模型与动态标题日签",
                    "SevenMergeView": "按merge_wlv选择奖励，当前配置show_type=1图片，离散弧形页签",
                },
            },
        },
        "unity_static": {
            "head": subprocess.run(["git", "rev-parse", "HEAD"], cwd=REPO, check=True, capture_output=True, text=True).stdout.strip(),
            "files": [file_ref(path) for path in unity_sources],
            "prefab_search": {"matches": [], "conclusion": "无SevenDay/SevenEightDay/SevenMerge可编辑Prefab"},
            "view_bind_search": {"matches": [], "conclusion": "无对应Unity View/Bind"},
            "config_search": {"matches": [], "conclusion": "Unity缺config_login_reward_day与config_login_merge_reward_day"},
            "router_search": {"matches": [], "conclusion": "MainUIRouter无175/175@8/175_1打开映射，点击会落通用未实现占位"},
            "implemented": [
                "17500/17502注册、解析与活动图标互斥",
                "等级真变化、跨天、普通七日任务门槛重拉",
                "本轮增量：按status=1派生三入口红点并在删图标前清共享缓存",
            ],
            "hard_negative": ["17501", "17503"],
        },
        "reconciliation": {
            "static_matches": ["17500/17502只读线", "入口键与互斥规则", "175@8明天可领条件", "等级/任务刷新边界"],
            "static_fix": "入口红点消费缺失；SevenDay私有Model/Controller已做最小增量修复。",
            "defects": ["三套页面/Prefab/View/Bind缺失", "两张权威奖励配置缺失", "专属图片/模型/UI特效未迁", "三入口MainUI路由缺失", "列表/详情/按钮/页签/关闭链无Unity消费者"],
            "blocked": ["convert-module缺老端真实运行快照且禁止Unity烘焙", "17501/17503领取为未授权真实发奖事务", "成功弹窗、即时刷新和关闭重开只能在权威成功后验证"],
        },
    }


def build_components() -> dict[str, object]:
    return {
        "route": ROUTE,
        "components": [
            {"name": "ActivityIconManager", "side": "Unity", "owner": "MainUI shared", "usage": "175/175@8/175_1入口、红点、文字", "write_policy": "forbidden shared island; SevenDay only calls public API", "status": "static consumer present"},
            {"name": "MainUIRouter", "side": "Unity", "owner": "MainUI shared", "usage": "入口跳转", "write_policy": "forbidden shared island", "status": "blocker: no SevenDay routes"},
            {"name": "EquipmentItem", "side": "legacy", "owner": "Common shared", "usage": "最多4个奖励格与物品详情", "write_policy": "forbidden shared island", "status": "Unity SevenDay consumer absent"},
            {"name": "CongratulationObtainView", "side": "legacy", "owner": "Common shared", "usage": "17501/17503成功奖励展示", "write_policy": "forbidden shared island", "status": "blocked behind destructive claim"},
            {"name": "UIEffect/ui_fazhen_05", "side": "legacy", "owner": "SevenDay page", "usage": "普通七日展示底部法阵", "write_policy": "dedicated resource allowed only after conversion evidence", "status": "not migrated; two-frame runtime evidence unavailable"},
            {"name": "per-day image/model/effect presentation", "side": "legacy", "owner": "SevenDay dedicated", "usage": "三页动态展示", "write_policy": "dedicated closure only", "status": "config/resource consumer absent"},
        ],
        "shared_component_state_matrix": {
            "EquipmentItem": ["slot1", "slot2", "slot3", "slot4", "click-detail", "quality/effect", "insufficient-empty-not-applicable"],
            "ActivityIconManager": ["175 visible/hidden/red", "175@8 visible/hidden/red/tomorrow-text", "175_1 visible/hidden/red", "mutual exclusion", "delete/recreate cache"],
            "dynamic_presentation": ["image", "model", "effect", "hide-before-switch", "close cleanup", "reopen"],
        },
        "runtime_sampling": {"status": "not-run", "reason": "本轮禁止Unity/浏览器；未修改共享组件，不能伪造代表消费者证据。"},
    }


def main() -> None:
    manifest = build_manifest()
    write_json("route-manifest.json", manifest)
    write_json("static-reconciliation.json", build_reconciliation())
    write_json("component-dependencies.json", build_components())
    write_json("route-results.json", build_results(manifest))
    write_json(
        "starting-dirty.json",
        {
            "captured_before_writes": True,
            "entries": [],
            "scopes": ["Assets/Scripts/Module/Core/SevenDay", "Assets/Prefabs/UI/SevenDay", "output/ui_route_audit/2026-08-09_parallel_ui_refine/sevenday"],
            "note": "目标岛起始无tracked/untracked dirty；全仓其他并发dirty未触碰。",
        },
    )
    print(f"generated {len(manifest['nodes'])} manifest nodes")


if __name__ == "__main__":
    main()
