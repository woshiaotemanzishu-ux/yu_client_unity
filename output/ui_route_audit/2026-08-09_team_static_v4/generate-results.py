#!/usr/bin/env python3
"""Generate blocked-only Team v4 leaf results."""

from __future__ import annotations

import json
from pathlib import Path


HERE = Path(__file__).resolve().parent
MANIFEST = HERE / "route-manifest.json"
OUT = HERE / "route-results.json"

DEAD_24011 = {
    "mainui.team.view.hall.avatar-menu-dead",
    "mainui.team.view.members.avatar-menu-dead",
    "mainui.team.dead-24011",
}


def reason(item: dict) -> str:
    node_id = item["id"]
    if node_id == "mainui.team.view.hall.row-render":
        return (
            "24012 展示字段、复用清理与双方野外附近语义已静态实现；但 CustomHeadItem 自定义头像/头像框"
            "位于 Common 跨文件岛，且缺 Unity/真实 Web 列表复用与像素证据，组合行保持 blocked。"
        )
    if node_id in DEAD_24011:
        return "24011 为已证实的死客户端流，老端 ShowPlayerMenu 为空；不得新增 Unity 入口。"
    if node_id == "mainui.team.dead-24042":
        return "24042 无老端发送点且回包仅解码丢弃，保持死读流边界。"
    if node_id.startswith("mainui.team.view.invite.nearby.query."):
        return (
            "老端当前 scene 首查、野外条件、0.1s timer、GetAllFieldScene 遍历、跳过当前 scene、"
            "逐个 24053 及遍历/Remove 清理已静态枚举；Unity TeamInviteView 未落地且本轮禁止运行验证。"
        )
    if node_id == "mainui.team.prefab-resource-closure":
        return "Team 页面级 Prefab 大量缺失，convert-module 依赖本轮禁止的 Unity 与运行快照。"
    if node_id == "mainui.team.sound":
        return "缺少页面级真实运行实例核对专属声音消费与关闭生命周期。"
    if item.get("risk") != "read-only":
        return "该叶会改变组队或导航状态；用户要求真实写事务只枚举，未新增绑定或发包。"
    return (
        "对应 Team 页面级 Prefab/View 或真实状态未落地；本轮禁止 Unity、浏览器和前台程序，"
        "静态源码不能冒充真实 Web 通过。"
    )


def main() -> None:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    parent_ids = {item["parent"] for item in manifest["nodes"] if item.get("parent")}
    leaves = [item for item in manifest["nodes"] if item["id"] not in parent_ids]
    results = [
        {"id": item["id"], "status": "blocked", "blocked_reason": reason(item)}
        for item in leaves
    ]
    if len(results) != 111 or any(item["status"] != "blocked" for item in results):
        raise SystemExit("unexpected Team v4 blocked leaf set")
    OUT.write_text(
        json.dumps({"nodes": results}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )


if __name__ == "__main__":
    main()
