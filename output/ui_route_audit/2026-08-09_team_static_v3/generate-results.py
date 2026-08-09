#!/usr/bin/env python3
"""Generate blocked-only leaf results for the Team v3 static audit."""

from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
MANIFEST = ROOT / "route-manifest.json"
RESULTS = ROOT / "route-results.json"

DEAD_24011_LEAVES = {
    "mainui.team.view.hall.avatar-menu-dead",
    "mainui.team.view.members.avatar-menu-dead",
    "mainui.team.dead-24011",
}


def blocked_reason(item: dict) -> str:
    node_id = item["id"]
    if node_id == "mainui.team.view.hall.row-render":
        return (
            "24012 纯展示字段、列表复用清理与同场景/双方野外的附近语义已在 Team 岛静态实现；"
            "但 CustomHeadItem 的自定义头像与头像框消费位于 Common 跨文件岛，当前缺少可验证闭包，"
            "且未执行 Unity/真实 Web 的列表复用、两档 viewport 与像素验收，因此组合 row-render 必须保持 blocked。"
        )
    if node_id in DEAD_24011_LEAVES:
        return (
            "24011 是已证实的死客户端流：老端 ShowPlayerMenu 为空/队员头像点击未绑定，"
            "不得因服务端领队事务仍存活而从 Unity 新增入口；保持 killlist 负约束。"
        )
    if node_id == "mainui.team.dead-24042":
        return (
            "24042 是已证实的死客户端读流：服务端查询存在，但老端无发送点且回包仅解码丢弃；"
            "不得以协议存在为由新增 Unity 入口。"
        )
    if node_id == "mainui.team.prefab-resource-closure":
        return (
            "Team 页面级 Prefab 仍缺失，当前可编辑资源闭包不足以静态完成老端全部页面/列表项；"
            "本轮禁止启动 Unity 执行 convert-module，故保持 blocked。"
        )
    if node_id == "mainui.team.sound":
        return (
            "尚无页面级运行实例核对页面专属声音触发、成功回包时机及关闭生命周期；"
            "通用点击音或资源存在不能替代真实消费证据。"
        )
    if node_id.startswith("mainui.team.view.invite.nearby.query."):
        return (
            "老端 TeamInviteView 的场景变化定时器、24053 重拉与 destroy ClearTimer 生命周期已枚举；"
            "Unity 页面级 Prefab/View 尚未落地且本轮禁止 Unity/浏览器，不能用静态源码冒充运行生命周期通过。"
        )
    if node_id.startswith("mainui.team.view.change-target.confirm."):
        return (
            "老端已有队伍发送 24017、无队伍调用 ChangeTargetSuccess(params) 的互斥确认分支已分别枚举；"
            "两条都会改变组队/筛选流程状态，用户只授权盘点，未授权账号写事务或新增可执行绑定。"
        )
    if item.get("risk") != "read-only":
        return (
            "该叶会改变组队、匹配、申请、邀请、投票、筛选输入或外部导航状态；"
            "用户要求所有真实写事务只枚举，故未新增可执行点击绑定、未发协议。"
        )
    return (
        "对应 Team 页面级 Prefab/View 或真实运行状态尚不具备；convert-module 需要 Unity 落地与老端/Unity 运行快照，"
        "而本轮禁止启动 Unity、浏览器和前台程序，故只能保留静态枚举，不能冒充真实 Web 通过。"
    )


def main() -> None:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    parent_ids = {item["parent"] for item in manifest["nodes"] if item.get("parent")}
    leaves = [item for item in manifest["nodes"] if item["id"] not in parent_ids]
    results = [
        {
            "id": item["id"],
            "status": "blocked",
            "blocked_reason": blocked_reason(item),
        }
        for item in leaves
    ]
    if len(results) != 105:
        raise SystemExit(f"unexpected Team v3 leaf count: {len(results)}")
    if any(item["status"] != "blocked" for item in results):
        raise SystemExit("v3 must contain blocked-only results")

    RESULTS.write_text(
        json.dumps({"nodes": results}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )


if __name__ == "__main__":
    main()
