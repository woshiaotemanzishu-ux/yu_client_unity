#!/usr/bin/env python3
"""Generate this static audit batch's compact leaf results.

This route-local helper may only write route-results.json.  The official
schema-6 ledger remains exclusively owned by route_ledger.py apply.
"""

from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
MANIFEST = ROOT / "route-manifest.json"
RESULTS = ROOT / "route-results.json"

RUNTIME_LEAVES = {
    "mainui.team.view.hall.row-render": (
        "TeamHallItem 已静态接管 24012 大厅快照的纯展示字段，并保持申请按钮和头像菜单无新增写事务绑定；"
        "仍缺真实列表宿主、Prefab GraphicRaycaster、异步头像、field-scene 附近语义、两档 viewport，"
        "以及老 H5/Unity Web 同账号同路径复验。"
    ),
}

DEAD_24011_LEAVES = {
    "mainui.team.view.hall.avatar-menu-dead",
    "mainui.team.view.members.avatar-menu-dead",
    "mainui.team.dead-24011",
}


def blocked_reason(node: dict) -> str:
    node_id = node["id"]
    if node_id in DEAD_24011_LEAVES:
        return (
            "24011 是已证实的死客户端流：老端 ShowPlayerMenu 为空/队员头像点击未绑定，"
            "当前服务端领队事务虽存活也不得从 Unity 新增入口；保持 killlist 负约束。"
        )
    if node_id == "mainui.team.dead-24042":
        return (
            "24042 是已证实的死客户端读流：服务端查询存在，但老端无发送点且回包仅解码丢弃；"
            "不得以协议存在为由新增 Unity 入口。"
        )
    if node_id == "mainui.team.prefab-resource-closure":
        return (
            "Team 页面级 Prefab 全部缺失，Unity team 资源闭包仅有 atlas 与 3 张纹理，"
            "不足以静态完成 15 个老端页面/列表项；本轮文件岛也不允许补写资源或启动 Unity 转换。"
        )
    if node_id == "mainui.team.sound":
        return (
            "老端 Team 路线未形成可由当前缺页静态验证的页面专属声音消费闭包；"
            "禁止用通用点击音或资源存在伪装完成，需页面落地后的真实运行时触发/关闭复验。"
        )
    if node.get("risk") != "read-only":
        return (
            "该叶会改变组队/匹配/申请/邀请/投票或筛选状态；用户要求所有真实写事务只枚举，"
            "本轮无账号写授权且页面 Prefab 缺失，因此未新增可执行点击绑定、未发协议。"
        )
    return (
        "对应 Team 页面级 Prefab 尚不存在；convert-module 需要 Unity 落地与老端/Unity 运行快照，"
        "而本轮明确禁止启动 Unity、浏览器和前台程序，故只能保留静态枚举，不能冒充真实 Web 通过。"
    )


def main() -> None:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    parent_ids = {node["parent"] for node in manifest["nodes"] if node.get("parent")}
    leaves = [node for node in manifest["nodes"] if node["id"] not in parent_ids]

    results = []
    for node in leaves:
        node_id = node["id"]
        if node_id in RUNTIME_LEAVES:
            results.append(
                {
                    "id": node_id,
                    "status": "needs-runtime-verify",
                    "runtime_gap": RUNTIME_LEAVES[node_id],
                    "note": "静态只读展示增量已落地；无点击发包绑定。",
                }
            )
        else:
            results.append(
                {
                    "id": node_id,
                    "status": "blocked",
                    "blocked_reason": blocked_reason(node),
                }
            )

    if len(results) != 68:
        raise SystemExit(f"unexpected Team leaf count: {len(results)}")
    if sum(item["status"] == "needs-runtime-verify" for item in results) != 1:
        raise SystemExit("expected exactly one runtime-verification leaf")

    RESULTS.write_text(
        json.dumps({"nodes": results}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )


if __name__ == "__main__":
    main()
