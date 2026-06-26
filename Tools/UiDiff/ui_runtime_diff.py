#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
ui_runtime_diff.py — Laya 运行时真相 vs Unity 运行时结果 的逐节点比对工具(diff oracle)

输入两份运行时快照:
  --laya   page_snapshot_*.json   由原版 Laya 客户端 electron 工具导出
                                   (pageSnapshot.js / __sxExportPageSnapshots__)
  --unity  ui_dump.json           由 Unity RuntimeUiCaptureTool 导出
                                   (神霄/调试/UI运行态/截图+节点Dump)

按「节点名 + 层级」对齐两棵树,在归一化屏幕空间(各自除以自己的 stage/screen,
左上角原点、Y 向下)里比对,输出排序后的差异清单:

  MISSING   老端可见、Unity 没有            → "少了"
  RESOURCE  贴图/skin 与 Unity sprite 对不上 → 换错图 / 没映射
  OFFSET    位置偏移 > 阈值                  → "位置不对",给 dx,dy
  SIZE      尺寸比例偏离 > 阈值              → 大了 / 小了
  HIDDEN    老端可见但 Unity 节点 inactive
  EXTRA     Unity 有、老端没有(一般是转换器加的壳,信息性)

用法:
  python ui_runtime_diff.py --laya page_snapshot_xxx.json --unity ui_dump.json \
         --view LoginCreateRoleView --out report.json
  python ui_runtime_diff.py --selftest        # 用内置 fixture 自检
"""

import argparse
import json
import os
import sys
from collections import defaultdict


# --------------------------------------------------------------------------- #
# 解析:把两边的节点树拍平成统一的 Node 列表
# --------------------------------------------------------------------------- #

class Node:
    __slots__ = ("name", "type", "name_path", "rect", "resources",
                 "visible", "text", "active", "raw")

    def __init__(self, name, type_, name_path, rect, resources,
                 visible, text, active, raw):
        self.name = name
        self.type = type_
        self.name_path = name_path      # tuple[str]  从视图根到自身
        self.rect = rect                # (x, y, w, h) 屏幕像素, 左上角原点, Y 向下; 或 None
        self.resources = resources      # list[str]   贴图 basename(无扩展名), 已小写
        self.visible = visible          # bool
        self.text = text                # str | None
        self.active = active            # bool   (Unity 侧 activeInHierarchy)
        self.raw = raw

    def depth(self):
        return len(self.name_path)


def _basename_noext(url):
    if not url:
        return None
    tail = url.replace("\\", "/").split("/")[-1]
    tail = tail.split("?")[0].split("#")[0]
    dot = tail.rfind(".")
    if dot > 0:
        tail = tail[:dot]
    return tail.lower() or None


# ----- Laya 侧 (page_snapshot) -------------------------------------------- #

def load_laya(path, view_name=None):
    with open(path, "r", encoding="utf-8-sig") as f:
        data = json.load(f)

    stage = data.get("stage") or {}
    stage_w = float(stage.get("width") or 0) or None
    stage_h = float(stage.get("height") or 0) or None

    views = data.get("views") or []
    if not views:
        raise ValueError("Laya 快照里没有 views(导出时可能没选中页面)")

    chosen = None
    if view_name:
        for v in views:
            meta = v.get("meta") or {}
            if view_name in (meta.get("name"), meta.get("rawName"),
                             meta.get("layoutFile")):
                chosen = v
                break
        if chosen is None:
            names = [(v.get("meta") or {}).get("name") for v in views]
            raise ValueError("Laya 快照里找不到视图 %r;可选:%s" % (view_name, names))
    else:
        chosen = views[0]

    meta = chosen.get("meta") or {}
    # 视图自身宽高可作为 stage 兜底(全屏视图时与 stage 一致)
    if not stage_w:
        stage_w = float(meta.get("width") or 0) or None
    if not stage_h:
        stage_h = float(meta.get("height") or 0) or None

    nodes = []

    def walk(n, ancestors):
        name = n.get("name") or ""
        name_path = ancestors + (name,)
        gb = n.get("globalBounds")
        rect = None
        if isinstance(gb, dict):
            rect = (float(gb.get("x", 0)), float(gb.get("y", 0)),
                    float(gb.get("width", 0)), float(gb.get("height", 0)))
        res = []
        for r in (n.get("resources") or []):
            b = _basename_noext(r.get("url"))
            if b:
                res.append(b)
        # skin 字段再补一次(serializeNode 顶层也存了 cleanUrl 后的 skin)
        b = _basename_noext(n.get("skin"))
        if b and b not in res:
            res.append(b)
        # 组件根(View/Item)的 skin 往往是它自己的 .json 路径,basename == 节点名,
        # 不是真贴图,过滤掉避免误报 RESOURCE。
        nm_low = (name or "").lower()
        res = [b for b in res if b != nm_low]
        text = None
        tp = n.get("textProps")
        if isinstance(tp, dict) and tp.get("text"):
            text = str(tp.get("text"))
        visible = bool(n.get("effectiveVisible", n.get("visible", True)))
        nodes.append(Node(
            name=name, type_=n.get("type") or "", name_path=name_path,
            rect=rect, resources=res, visible=visible, text=text,
            active=visible, raw=n))
        for c in (n.get("children") or []):
            walk(c, name_path)

    tree = chosen.get("nodeTree")
    if not tree:
        raise ValueError("视图 %r 没有 nodeTree" % meta.get("name"))
    walk(tree, ())

    return {
        "side": "laya",
        "view": meta.get("name"),
        "stage": (stage_w, stage_h),
        "nodes": nodes,
    }


# ----- Unity 侧 (ui_dump) -------------------------------------------------- #

def load_unity(path, view_name=None):
    with open(path, "r", encoding="utf-8-sig") as f:
        data = json.load(f)

    screen = data.get("screen") or [0, 0]
    screen_w = float(screen[0] or 0) or None
    screen_h = float(screen[1] or 0) or None

    nodes = []

    def walk(n, ancestors):
        name = n.get("name") or ""
        while name.endswith("(Clone)"):   # Unity Instantiate 后缀,Laya 侧没有,去掉才对得上
            name = name[:-len("(Clone)")]
        name_path = ancestors + (name,)
        rect = None
        sr = n.get("screenRect")  # [x, y, w, h] 左上角原点, 由增强后的 capture 写入
        if isinstance(sr, (list, tuple)) and len(sr) == 4:
            rect = (float(sr[0]), float(sr[1]), float(sr[2]), float(sr[3]))
        res = []
        text = None
        g = n.get("graphic")
        if isinstance(g, dict):
            b = _basename_noext(g.get("sprite") or g.get("texture"))
            if b:
                res.append(b)
            if g.get("text"):
                text = str(g.get("text"))
        active = bool(n.get("activeInHierarchy", True))
        nodes.append(Node(
            name=name, type_=(g or {}).get("type") or "", name_path=name_path,
            rect=rect, resources=res, visible=active, text=text,
            active=active, raw=n))
        for c in (n.get("children") or []):
            walk(c, name_path)

    for root in (data.get("rootCanvases") or []):
        walk(root, ())

    # 指定了视图就只比该视图子树:Unity dump 往往是整棵 UI(含其它子视图),
    # 不裁剪会把别的视图全算成 EXTRA。按名字找到视图根,只留其子树(保留完整 name_path)。
    if view_name:
        scope = next((nd.name_path for nd in nodes if nd.name == view_name), None)
        if scope:
            n = len(scope)
            nodes = [nd for nd in nodes if nd.name_path[:n] == scope]

    return {
        "side": "unity",
        "view": view_name,
        "stage": (screen_w, screen_h),
        "nodes": nodes,
    }


# --------------------------------------------------------------------------- #
# 归一化 + 对齐 + 比对
# --------------------------------------------------------------------------- #

def compute_frames(laya, unity):
    """把两端原生坐标映射到统一的「设计空间」再归一化。
    设计分辨率取 Unity screen(要求 Unity 在设计分辨率下抓 dump);
    Laya stage 往往 ≠ 设计(electron 窗口大小可变),按等比适配(SHOWALL/contain)
    + 居中留边映射回设计空间——否则不同宽高比下非居中节点会被误判 OFFSET/SIZE。
    返回 (lframe, uframe, design)。每个 frame = {s, offx, offy, dw, dh}。"""
    dw, dh = unity["stage"]
    if not dw or not dh:
        dw, dh = laya["stage"]            # 兜底
    lsw, lsh = laya["stage"]
    if lsw and lsh and dw and dh:
        s_l = min(lsw / dw, lsh / dh)     # contain 等比适配
    else:
        s_l = 1.0
    s_l = s_l or 1.0
    lframe = {"s": s_l,
              "offx": ((lsw or 0) - dw * s_l) / 2.0,
              "offy": ((lsh or 0) - dh * s_l) / 2.0,
              "dw": dw, "dh": dh}
    uframe = {"s": 1.0, "offx": 0.0, "offy": 0.0, "dw": dw, "dh": dh}
    return lframe, uframe, (dw, dh)


def norm_center(rect, frame):
    if rect is None or not frame or not frame["dw"] or not frame["dh"]:
        return None
    x, y, w, h = rect
    s = frame["s"] or 1.0
    cx = ((x + w / 2.0) - frame["offx"]) / s
    cy = ((y + h / 2.0) - frame["offy"]) / s
    return (cx / frame["dw"], cy / frame["dh"])


def norm_size(rect, frame):
    if rect is None or not frame or not frame["dw"] or not frame["dh"]:
        return None
    _, _, w, h = rect
    s = frame["s"] or 1.0
    return ((w / s) / frame["dw"], (h / s) / frame["dh"])


def _common_suffix_len(a, b):
    """两个 name_path(从视图根到自身的名字元组)末尾连续相同的长度。
    Unity 树比 Laya 多出 UIRoot/Canvas/视图根 等壳层,所以比后缀而非全路径;
    后缀越长越是同一个节点,天然区分不同子视图里的同名节点。"""
    n = 0
    i, j = len(a) - 1, len(b) - 1
    while i >= 0 and j >= 0 and a[i] == b[j]:
        n += 1
        i -= 1
        j -= 1
    return n


def _proximity(lc, uc):
    if lc is None or uc is None:
        return 9e9
    return (uc[0] - lc[0]) ** 2 + (uc[1] - lc[1]) ** 2


def align(laya, unity, lframe, uframe):
    """对齐两棵树。返回 (pairs, missing, extra)。
    具名节点: 按 名字 + 最长公共路径后缀 对齐(同名跨子视图不会错配),
              后缀相同再用归一化距离兜底。
    无名但有视觉内容(常见于特效): 按 资源 basename / 文字 + 距离 几何对齐。"""
    unodes = unity["nodes"]
    used = set()

    by_name = defaultdict(list)
    for un in unodes:
        if un.name:
            by_name[un.name].append(un)

    pairs = []          # (laya_node, unity_node)
    missing = []        # laya 节点无 Unity 对应

    # ① 具名节点
    for ln in laya["nodes"]:
        if not ln.name:
            continue
        cands = [u for u in by_name.get(ln.name, []) if id(u) not in used]
        if not cands:
            missing.append(ln)
            continue
        lc = norm_center(ln.rect, lframe)
        cands.sort(
            key=lambda u: (_common_suffix_len(ln.name_path, u.name_path),
                           -_proximity(lc, norm_center(u.rect, uframe))),
            reverse=True)
        match = cands[0]
        used.add(id(match))
        pairs.append((ln, match))

    # ② 无名但有贴图/文字的节点(特效往往无名,不能直接丢)
    for ln in laya["nodes"]:
        if ln.name or not (ln.resources or ln.text):
            continue
        lc = norm_center(ln.rect, lframe)
        best, best_key = None, None
        for u in unodes:
            if id(u) in used:
                continue
            res_hit = bool(ln.resources and (set(ln.resources) & set(u.resources)))
            txt_hit = bool(ln.text and u.text and ln.text == u.text)
            if not (res_hit or txt_hit):
                continue
            key = _proximity(lc, norm_center(u.rect, uframe))
            if best is None or key < best_key:
                best, best_key = u, key
        if best is not None:
            used.add(id(best))
            pairs.append((ln, best))
        else:
            missing.append(ln)   # 丢失的无名特效也能被发现

    extra = [u for u in unodes if u.name and id(u) not in used]
    return pairs, missing, extra


SEVERITY_ORDER = {"MISSING": 0, "RESOURCE": 1, "OFFSET": 2, "SIZE": 3,
                  "HIDDEN": 4, "EXTRA": 5}


def diff(laya, unity, pos_thresh=0.01, size_thresh=0.10,
         include_invisible=False):
    lframe, uframe, design = compute_frames(laya, unity)
    pairs, missing, extra = align(laya, unity, lframe, uframe)
    findings = []
    view_root = laya.get("view")   # 视图根节点会拉伸贴合 stage,不做几何比对

    def path_str(n):
        return "/".join([p for p in n.name_path if p])

    for ln in missing:
        if not include_invisible and not ln.visible:
            continue
        res = ",".join(ln.resources) if ln.resources else ""
        if ln.name:
            detail = "老端可见但 Unity 树里没有对应节点"
        else:
            detail = "老端无名但有视觉内容(疑似特效),Unity 无对应节点"
        if res:
            detail += "  贴图=" + res
        findings.append({
            "severity": "MISSING", "node": ln.name or "(无名)", "type": ln.type,
            "path": path_str(ln), "res": res, "detail": detail,
        })

    for ln, un in pairs:
        # HIDDEN
        if ln.visible and not un.active:
            findings.append({
                "severity": "HIDDEN", "node": ln.name, "type": ln.type,
                "path": path_str(ln),
                "detail": "老端可见,Unity 对应节点 inactive",
            })

        # RESOURCE — 仅当老端确有贴图时才校验
        if ln.resources:
            if un.resources:
                if not (set(ln.resources) & set(un.resources)):
                    findings.append({
                        "severity": "RESOURCE", "node": ln.name,
                        "type": ln.type, "path": path_str(ln),
                        "detail": "贴图对不上  laya=%s  unity=%s" % (
                            ",".join(ln.resources), ",".join(un.resources)),
                    })
            else:
                findings.append({
                    "severity": "RESOURCE", "node": ln.name, "type": ln.type,
                    "path": path_str(ln),
                    "detail": "老端有贴图 %s,Unity 节点没有 sprite" % (
                        ",".join(ln.resources)),
                })

        # OFFSET / SIZE — 需要两边都有 rect;视图根拉伸贴合 stage,跳过
        is_root = bool(view_root) and ln.name == view_root
        lc, uc = norm_center(ln.rect, lframe), norm_center(un.rect, uframe)
        if lc and uc and not is_root:
            dx, dy = uc[0] - lc[0], uc[1] - lc[1]
            dist = (dx * dx + dy * dy) ** 0.5
            if dist > pos_thresh:
                px_x = dx * (design[0] or 0)
                px_y = dy * (design[1] or 0)
                findings.append({
                    "severity": "OFFSET", "node": ln.name, "type": ln.type,
                    "path": path_str(ln),
                    "norm_dist": round(dist, 4),
                    "detail": "偏移 ~(%+.0f, %+.0f)px(老端设计像素)  (归一化 %.3f)" % (
                        px_x, px_y, dist),
                })
        ls, us = norm_size(ln.rect, lframe), norm_size(un.rect, uframe)
        if ls and us and ls[0] > 0 and ls[1] > 0 and not is_root:
            rw = us[0] / ls[0] if ls[0] else 1.0
            rh = us[1] / ls[1] if ls[1] else 1.0
            if abs(rw - 1.0) > size_thresh or abs(rh - 1.0) > size_thresh:
                findings.append({
                    "severity": "SIZE", "node": ln.name, "type": ln.type,
                    "path": path_str(ln),
                    "detail": "尺寸比例  w×%.2f  h×%.2f" % (rw, rh),
                })

    matched_names = {ln.name for ln, _ in pairs if ln.name}
    for un in extra:
        is_dup = un.name in matched_names    # 与已匹配节点重名 = 真重复/错位,必报
        if un.resources or un.text or is_dup:
            findings.append({
                "severity": "EXTRA", "node": un.name, "type": un.type,
                "path": path_str(un),
                "detail": ("Unity 独有,且与已匹配节点重名(疑似重复/错位节点)"
                           if is_dup else "Unity 独有(老端无同名节点)"),
            })

    findings.sort(key=lambda f: (SEVERITY_ORDER.get(f["severity"], 9),
                                 -f.get("norm_dist", 0)))
    return findings, pairs, missing, extra


# --------------------------------------------------------------------------- #
# 报告
# --------------------------------------------------------------------------- #

def report(findings, laya, unity, pairs):
    counts = defaultdict(int)
    for f in findings:
        counts[f["severity"]] += 1
    lines = []
    lines.append("=" * 72)
    lines.append("UI 运行时比对  laya[%s]  vs  unity" % (laya.get("view") or "?"))
    lines.append("  laya stage = %s   unity screen = %s" % (
        laya["stage"], unity["stage"]))
    lines.append("  对齐节点 %d   老端节点 %d   Unity 节点 %d" % (
        len(pairs), len(laya["nodes"]), len(unity["nodes"])))
    lines.append("  差异: " + "  ".join(
        "%s=%d" % (k, counts[k]) for k in SEVERITY_ORDER if counts[k]) or "  无")
    lines.append("=" * 72)

    # 打印 Laya stage → 设计空间 的适配参数,便于核对(假设 SHOWALL 等比适配)
    lframe, _uf, design = compute_frames(laya, unity)
    if design[0] and design[1]:
        lines.append("  设计空间 %g×%g(取自 Unity screen);Laya 等比适配 scale=%.3f, "
                     "居中留边 X=%.0f Y=%.0f" % (
                         design[0], design[1], lframe["s"],
                         lframe["offx"], lframe["offy"]))
        if (lframe["s"] <= 0 or abs(lframe["offx"]) > design[0]
                or abs(lframe["offy"]) > design[1]):
            lines.append("  ⚠ 适配参数异常,OFFSET/SIZE 可能不可信;"
                         "请确认 Unity 在设计分辨率(如 720×1280)下抓的 dump")

    cur = None
    for f in findings:
        if f["severity"] != cur:
            cur = f["severity"]
            lines.append("")
            lines.append("【%s】" % cur)
        lines.append("  %-22s %-10s %s" % (
            f["node"] or "(无名)", f.get("type", ""), f["detail"]))
        if f.get("path"):
            lines.append("      @ %s" % f["path"])
    if not findings:
        lines.append("")
        lines.append("  ✅ 无差异")
    return "\n".join(lines)


# --------------------------------------------------------------------------- #
# 自检 (无需真实运行时快照)
# --------------------------------------------------------------------------- #

def _selftest():
    here = os.path.dirname(os.path.abspath(__file__))
    fx = os.path.join(here, "fixtures")
    laya = load_laya(os.path.join(fx, "laya_create_role.sample.json"))
    unity = load_unity(os.path.join(fx, "unity_create_role.sample.json"))
    findings, pairs, missing, extra = diff(laya, unity)
    print(report(findings, laya, unity, pairs))

    got = {(f["severity"], f["node"]) for f in findings}
    expect = {
        ("MISSING", "_img_random"),     # Unity 删掉了这个节点
        ("OFFSET", "_img_enter"),       # Unity 把它 +40px
        ("RESOURCE", "_img_bg"),        # Unity 换错了 sprite
        ("SIZE", "_img_tips"),          # Unity 放大了 1.3x
    }
    ok = expect <= got

    # 丢失的无名特效(boom)应浮现为 MISSING
    eff_ok = any(f["severity"] == "MISSING" and "boom" in (f.get("res") or "")
                 for f in findings)
    # 重名节点(两个 _lab 分属 _sub_a/_sub_b)不应错配出幻影 OFFSET/RESOURCE
    lab_clean = not any(f["node"] == "_lab" and
                        f["severity"] in ("OFFSET", "RESOURCE") for f in findings)
    # 分辨率无关性:把 Unity 整体放大 2x(1440×2560),结论应完全不变
    u2 = {"side": "unity", "view": unity["view"], "stage": (1440.0, 2560.0),
          "nodes": []}
    for nd in unity["nodes"]:
        r = tuple(v * 2 for v in nd.rect) if nd.rect else None
        u2["nodes"].append(Node(nd.name, nd.type, nd.name_path, r, nd.resources,
                                nd.visible, nd.text, nd.active, nd.raw))
    findings2, _, _, _ = diff(laya, u2)
    got2 = {(f["severity"], f["node"]) for f in findings2}
    res_ok = got2 == got

    all_ok = ok and eff_ok and lab_clean and res_ok
    print("\nSELF-TEST:", "PASS ✅" if all_ok else "FAIL ❌")
    if not all_ok:
        if not ok:
            print("  缺少预期结论:", expect - got)
        if not eff_ok:
            print("  丢失的无名特效未浮现为 MISSING")
        if not lab_clean:
            print("  重名 _lab 产生了幻影 OFFSET/RESOURCE(跨子视图错配)")
        if not res_ok:
            print("  Unity 放大 2x 后结论变了(frame 映射非分辨率无关):", got2 ^ got)
    return 0 if all_ok else 1


def main():
    # Windows 控制台默认 GBK,中文/emoji 会报 UnicodeEncodeError → 强制 UTF-8
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8")
        except Exception:
            pass

    ap = argparse.ArgumentParser(description="Laya vs Unity 运行时 UI 比对")
    ap.add_argument("--laya", help="page_snapshot_*.json")
    ap.add_argument("--unity", help="ui_dump.json")
    ap.add_argument("--view", help="要比对的视图名(默认取快照里第一个)")
    ap.add_argument("--out", help="把差异写成 json")
    ap.add_argument("--pos-thresh", type=float, default=0.01,
                    help="位置阈值(归一化, 默认 0.01 = 屏幕的 1%%)")
    ap.add_argument("--size-thresh", type=float, default=0.10,
                    help="尺寸比例阈值(默认 0.10 = ±10%%)")
    ap.add_argument("--include-invisible", action="store_true",
                    help="把老端不可见节点也纳入 MISSING")
    ap.add_argument("--selftest", action="store_true", help="用内置 fixture 自检")
    args = ap.parse_args()

    if args.selftest:
        return _selftest()
    if not args.laya or not args.unity:
        ap.error("需要 --laya 和 --unity(或 --selftest)")

    laya = load_laya(args.laya, args.view)
    unity = load_unity(args.unity, args.view)
    findings, pairs, missing, extra = diff(
        laya, unity, args.pos_thresh, args.size_thresh, args.include_invisible)
    print(report(findings, laya, unity, pairs))
    if args.out:
        with open(args.out, "w", encoding="utf-8") as f:
            json.dump({"view": laya.get("view"), "findings": findings},
                      f, ensure_ascii=False, indent=2)
        print("\n→ %s" % args.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
