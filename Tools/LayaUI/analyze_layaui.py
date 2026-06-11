#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""LayaUI 归属分析器。

扫描 yu_client 的 h5/src TS 源码与 cdn 运行时 scene JSON,产出 ui_manifest.json,
为 Unity Editor 端的 LayaUI 转换器(Assets/Editor/LayaUI/)提供粒度决策。

判定规则(注意:这个项目里基类不可靠,achvView 这种主界面也继承 BaseItem1,
所以以「引用拓扑 + layer_value 信号」为准):

  window(BaseView1 链 / 设置过 this.layer_value)      -> view-prefab(独立)
  component 被恰好 1 个 UI 类引用                      -> inline(内联进宿主 __Templates)
  component 被 >=2 个 UI 类引用                        -> shared-prefab(共享,嵌套进宿主)
  component 只被控制器等非 UI 文件引用                 -> standalone-prefab(独立,控制器直开)
  component / scene 无任何代码引用                     -> dead-flag(默认不转换,报告列出)
  *Skin / *Exml 等无类绑定的换皮变体                   -> variant-unused(默认不转换)
  其余无类绑定的孤儿 scene                             -> orphan-flag(默认不转换)

用法:
  python3 Tools/LayaUI/analyze_layaui.py [yu_client根目录]
  缺省 yu_client 根目录取本仓库同级的 ../yu_client。
"""
import json
import os
import re
import sys
import time
from collections import defaultdict

UNITY_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DEFAULT_CLIENT = os.path.normpath(os.path.join(UNITY_ROOT, "..", "yu_client"))

RE_CLASS = re.compile(r"^(?:export\s+)?class\s+(\w+)\s+extends\s+([\w\.]+)", re.M)
RE_BASE_FILE = re.compile(r"""(?:this\.)?base_file\s*=\s*["']([^"']+)["']""")
RE_LAYOUT_FILE = re.compile(r"""(?:this\.)?layout_file\s*=\s*["']([^"']+)["']""")
RE_LAYER = re.compile(r"this\.layer_value\s*=")

# 运行时皮肤静态烘焙 —————————————————————————————————————————————
# 规律(全部来自 ResManager.ts / GameResPath.ts,改这两个文件时要同步):
#   1. GameResPath 的简单方法都是单行模板串 return `...${arg}...`,
#      直接解析 GameResPath.ts 自动推导「方法名 -> 路径模板」,不硬编码。
#   2. SetTexture / SetOutsideImageSprite(内部调 SetTexture)运行时会把路径里
#      第一个 /texture/ 替换成 /other/(移植后遗症),烘焙路径必须复刻;
#      SetImageSpriteTrans / 直接 .skin= 不做替换。
#   3. SetImageSprite(this, img, ab, res):ab 去掉最后一个 _ 后缀,
#      拼 resource/game/{ab}/texture/{res}.png。
#   4. 支持 let/const/var x = this._img_xxx 的别名再赋图(全项目 ~800 处)。
# 同一节点首写胜。解析不了的(变量/模板串/三元)留给报告的「运行时赋值」清单。

RE_ALIAS = re.compile(r"(?:let|const|var)\s+(\w+)\s*=\s*this\.(\w+)\s*[;\n]")
# 局部字符串常量:let bg_url = "resource/..." 再传给 SetTexture(LoginBgView 等的写法)
RE_STRCONST = re.compile(r"""(?:let|const|var)\s+(\w+)\s*=\s*["'](resource/[^"']+)["']""")
# 模板串常量/实参:`resource/...${id}.jpg` —— ${...} 当通配符 glob 资源目录,取第一张当编辑器默认
RE_STRCONST_TPL = re.compile(r"(?:let|const|var)\s+(\w+)\s*=\s*`(resource/[^`]+)`")
_LIT = r"""["']([^"']+)["']"""
_GRP_CALL = r"""GameResPath\.(\w+)\(\s*((?:["'][^"']*["']\s*,\s*)*["'][^"']*["'])\s*\)"""
# target: this.node 或 别名
_TARGET = r"(?:this\.(\w+)|(\w+))"

RE_SKIN_ASSIGN_LIT = re.compile(r"this\.(\w+)\.skin\s*=\s*" + _LIT)
RE_SKIN_ASSIGN_GRP = re.compile(r"this\.(\w+)\.skin\s*=\s*" + _GRP_CALL)
RE_TEX_LIT = re.compile(r"(SetTexture|SetOutsideImageSprite|SetImageSpriteTrans)\(\s*this\s*,\s*" + _TARGET + r"\s*,\s*" + _LIT)
RE_TEX_GRP = re.compile(r"(SetTexture|SetOutsideImageSprite|SetImageSpriteTrans)\(\s*this\s*,\s*" + _TARGET + r"\s*,\s*" + _GRP_CALL)
RE_TEX_VAR = re.compile(r"(SetTexture|SetOutsideImageSprite|SetImageSpriteTrans)\(\s*this\s*,\s*" + _TARGET + r"\s*,\s*(\w+)\s*[,)]")
RE_TEX_TPL = re.compile(r"(SetTexture|SetOutsideImageSprite|SetImageSpriteTrans)\(\s*this\s*,\s*" + _TARGET + r"\s*,\s*`(resource/[^`]+)`")

_CLIENT_ROOT = None  # main() 里设置,供模板串 glob 用


def resolve_template_path(tpl):
    """`resource/...${id}.jpg` -> 资源目录里 glob 第一张匹配;无 ${} 即原样返回。"""
    pattern = re.sub(r"\$\{[^}]*\}", "*", tpl)
    if "*" not in pattern:
        return pattern
    if _CLIENT_ROOT is None:
        return None
    import glob as _glob
    for base in (os.path.join(_CLIENT_ROOT, "h5", "laya", "assets"),
                 os.path.join(_CLIENT_ROOT, "cdn")):
        matches = sorted(_glob.glob(os.path.join(base, pattern.replace("/", os.sep))))
        for m in matches:
            if m.lower().endswith((".png", ".jpg")):
                return os.path.relpath(m, base).replace(os.sep, "/")
    return None
RE_IMGSPRITE = re.compile(r"SetImageSprite\(\s*this\s*,\s*" + _TARGET + r"\s*,\s*" + _LIT + r"\s*,\s*" + _LIT)

RE_GRP_METHOD = re.compile(
    r"public static (\w+)\(([^)]*)\)\s*\{\s*return\s+`([^`]+)`", re.S)

_gameres_templates = {}


def load_gameres_templates(src_root):
    """解析 GameResPath.ts,推导「方法名 -> (参数名列表, 模板)」。只收单行模板方法。"""
    path = os.path.join(src_root, "util", "GameResPath.ts")
    if not os.path.exists(path):
        return
    text = open(path, encoding="utf-8", errors="replace").read()
    for m in RE_GRP_METHOD.finditer(text):
        fn, params, tpl = m.group(1), m.group(2), m.group(3)
        names = [p.split(":")[0].strip() for p in params.split(",") if p.strip()]
        _gameres_templates[fn] = (names, tpl)


def resolve_gameres(fn, raw_args):
    """GameResPath.Fn("a","b") -> 路径;含未替换占位符则放弃。"""
    info = _gameres_templates.get(fn)
    if info is None:
        return None
    names, tpl = info
    args = re.findall(r"""["']([^"']*)["']""", raw_args)
    out = tpl
    for name, val in zip(names, args):
        out = out.replace("${%s}" % name, val)
    return None if "${" in out else out


def _texture_to_other(path):
    return path.replace("/texture/", "/other/", 1)


def extract_baked_skins(body):
    aliases = {a: node for a, node in RE_ALIAS.findall(body)}
    strconsts = {a: path for a, path in RE_STRCONST.findall(body)}
    for a, tpl in RE_STRCONST_TPL.findall(body):
        resolved = resolve_template_path(_texture_to_other(tpl))
        if resolved:
            strconsts.setdefault(a, resolved)

    def target_node(this_node, alias):
        return this_node if this_node else aliases.get(alias)

    baked = {}

    def put(node, path):
        if node and path:
            baked.setdefault(node, path)

    for m in RE_SKIN_ASSIGN_LIT.finditer(body):
        put(m.group(1), m.group(2))
    for m in RE_SKIN_ASSIGN_GRP.finditer(body):
        put(m.group(1), resolve_gameres(m.group(2), m.group(3)))
    for m in RE_TEX_LIT.finditer(body):
        fn, path = m.group(1), m.group(4)
        if fn != "SetImageSpriteTrans":
            path = _texture_to_other(path)
        put(target_node(m.group(2), m.group(3)), path)
    for m in RE_TEX_GRP.finditer(body):
        fn = m.group(1)
        path = resolve_gameres(m.group(4), m.group(5))
        if path and fn != "SetImageSpriteTrans":
            path = _texture_to_other(path)
        put(target_node(m.group(2), m.group(3)), path)
    for m in RE_IMGSPRITE.finditer(body):
        ab, res = m.group(3), m.group(4)
        if "_" in ab[1:]:
            ab = ab[:ab.rindex("_")]
        put(target_node(m.group(1), m.group(2)), "resource/game/%s/texture/%s.png" % (ab, res))
    for m in RE_TEX_VAR.finditer(body):
        fn = m.group(1)
        path = strconsts.get(m.group(4))
        if path and fn != "SetImageSpriteTrans":
            path = _texture_to_other(path)
        put(target_node(m.group(2), m.group(3)), path)
    for m in RE_TEX_TPL.finditer(body):
        fn, tpl = m.group(1), m.group(4)
        if fn != "SetImageSpriteTrans":
            tpl = _texture_to_other(tpl)
        put(target_node(m.group(2), m.group(3)), resolve_template_path(tpl))
    return baked

# 窗口基类(继承到这里的必然是独立窗口)
VIEW_BASES = {"BaseView1", "BaseView", "BaseSubView"}

SKIN_PROP_KEYS = ("skin", "texture", "vScrollBarSkin", "hScrollBarSkin", "sceneBg")


def scan_ts_classes(src_root):
    """返回 classes: name -> dict(file, base, module, layout)。"""
    classes = {}
    file_text = {}
    for dirpath, _dirnames, filenames in os.walk(src_root):
        for fn in filenames:
            if not fn.endswith(".ts"):
                continue
            path = os.path.join(dirpath, fn)
            try:
                with open(path, encoding="utf-8", errors="replace") as f:
                    text = f.read()
            except OSError:
                continue
            file_text[path] = text
            matches = list(RE_CLASS.finditer(text))
            for i, m in enumerate(matches):
                name, base = m.group(1), m.group(2).split(".")[-1]
                body_end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
                body = text[m.start():body_end]
                bf = RE_BASE_FILE.search(body)
                lf = RE_LAYOUT_FILE.search(body)
                if name in classes:
                    # 重名类:保留先发现的,记录冲突
                    classes[name].setdefault("dup_files", []).append(path)
                    continue
                classes[name] = {
                    "file": os.path.relpath(path, src_root),
                    "base": base,
                    "module": bf.group(1) if bf else None,
                    "layout": lf.group(1) if lf else None,
                    "hasLayer": bool(RE_LAYER.search(body)),
                    "bakedSkins": extract_baked_skins(body),
                    # 类体里 this.xxx 引用集合:与 scene 节点名求交得 codeNodes,
                    # Bind 收集 = "_" 前缀节点 ∪ codeNodes(老界面如 LoginView 的
                    # account/loginBtn 不带下划线,只靠前缀会漏)
                    "thisRefs": set(re.findall(r"this\.(\w+)\b", body)),
                }
    return classes, file_text


def is_window(name, classes):
    """窗口判定:继承链到 BaseView1 系,或类体里设置过 layer_value。"""
    seen = set()
    cur = name
    while cur and cur not in seen:
        seen.add(cur)
        if cur in VIEW_BASES:
            return True
        info = classes.get(cur)
        if info is None:
            return False
        if info.get("hasLayer"):
            return True
        cur = info["base"]
    return False


def build_usage(classes, file_text, src_root):
    """UI 类 -> (引用它的其他 UI 类集合, 引用它的非 UI 文件集合)。

    同文件里共同声明的 UI 类也算宿主(item 常和宿主 view 写在一个文件里)。
    """
    ui_in_file = defaultdict(list)  # 文件 -> 该文件声明的 UI 类
    for name, info in classes.items():
        if info["isUI"]:
            ui_in_file[info["file"]].append(name)

    owners = defaultdict(set)
    other_refs = defaultdict(set)
    word_re_cache = {}
    ui_names = [n for n, i in classes.items() if i["isUI"]]
    for path, text in file_text.items():
        rel = os.path.relpath(path, src_root)
        file_ui = ui_in_file.get(rel, ())
        for cls in ui_names:
            if cls not in text:
                continue
            wr = word_re_cache.get(cls)
            if wr is None:
                wr = re.compile(r"\b%s\b" % re.escape(cls))
                word_re_cache[cls] = wr
            if not wr.search(text):
                continue
            hosts = [u for u in file_ui if u != cls]
            if hosts:
                owners[cls].update(hosts)
            elif classes[cls]["file"] != rel:
                other_refs[cls].add(rel)
    return owners, other_refs


def walk_scene(node, skins, types, names):
    t = node.get("type")
    if t:
        types[t] = types.get(t, 0) + 1
    props = node.get("props", {})
    n = props.get("name")
    if isinstance(n, str) and n:
        names.add(n)
    for k in SKIN_PROP_KEYS:
        v = props.get(k)
        if isinstance(v, str) and v:
            skins.add(v)
    for c in node.get("child", ()):  # 运行时 json 的子节点字段
        walk_scene(c, skins, types, names)
    for c in props.get("child", ()):  # 个别 Label 嵌套
        if isinstance(c, dict):
            walk_scene(c, skins, types, names)


def classify_skin(skin, client_root, atlas_index):
    """loose / cdn / atlas / comp / missing"""
    if skin.startswith("comp/"):
        loose = os.path.join(client_root, "h5", "laya", "assets", skin)
        return "comp" if os.path.exists(loose) else "missing"
    loose = os.path.join(client_root, "h5", "laya", "assets", skin)
    if os.path.exists(loose):
        return "loose"
    if os.path.exists(os.path.join(client_root, "cdn", skin)):
        return "cdn"  # 镜像缺、cdn 有(散图兜底源)
    m = re.match(r"resource/game/([^/]+)/texture/(.+)$", skin)
    if m:
        frames = atlas_index.get("%s/texture.atlas" % m.group(1), {}).get("frames", {})
        if m.group(2) in frames:
            return "atlas"
    return "missing"


def pascal(s):
    return "".join(p[:1].upper() + p[1:] for p in re.split(r"[_\-]+", s) if p)


def main():
    client_root = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_CLIENT
    src_root = os.path.join(client_root, "h5", "src")
    cdn_game = os.path.join(client_root, "cdn", "resource", "game")
    if not os.path.isdir(src_root) or not os.path.isdir(cdn_game):
        sys.exit("yu_client 路径不对: %s (需要 h5/src 与 cdn/resource/game)" % client_root)

    global _CLIENT_ROOT
    _CLIENT_ROOT = client_root

    print("[1/4] 扫描 TS 类 ...")
    load_gameres_templates(src_root)
    print("   GameResPath 模板方法 %d 个(自动推导)" % len(_gameres_templates))
    classes, file_text = scan_ts_classes(src_root)
    for name, info in classes.items():
        info["isUI"] = bool(info["module"] and info["layout"])
        info["kind"] = "window" if (info["isUI"] and is_window(name, classes)) else (
            "component" if info["isUI"] else "non-ui")
    n_win = sum(1 for i in classes.values() if i["kind"] == "window")
    n_comp = sum(1 for i in classes.values() if i["kind"] == "component")
    print("   类总数 %d (window=%d, component=%d, non-ui=%d)"
          % (len(classes), n_win, n_comp, len(classes) - n_win - n_comp))

    print("[2/4] 引用归属分析 ...")
    owners, other_refs = build_usage(classes, file_text, src_root)

    print("[3/4] 扫描 cdn 运行时 scene JSON ...")
    uicfg_path = os.path.join(client_root, "cdn", "resource", "UIConfig.json")
    atlas_index = {}
    if os.path.exists(uicfg_path):
        with open(uicfg_path, encoding="utf-8") as f:
            atlas_index = json.load(f)

    # scene key("module/Name") -> 类
    scene_class = {}
    for name, info in classes.items():
        if info["module"] and info["layout"]:
            scene_class.setdefault("%s/%s" % (info["module"], info["layout"]), name)

    scenes = {}
    for dirpath, _d, filenames in os.walk(cdn_game):
        for fn in filenames:
            if not fn.endswith(".json"):
                continue
            scene_file = os.path.join(dirpath, fn[:-5] + ".scene")
            if not os.path.exists(scene_file):
                continue  # 不是 scene 的配套 json
            jpath = os.path.join(dirpath, fn)
            rel = os.path.relpath(jpath, cdn_game).replace(os.sep, "/")
            module = rel.split("/")[0]
            name = fn[:-5]
            key = "%s/%s" % (module, name)
            try:
                with open(jpath, encoding="utf-8") as f:
                    data = json.load(f)
            except (OSError, ValueError) as e:
                scenes[key] = {"error": str(e)}
                continue
            skins, types, names = set(), {}, set()
            walk_scene(data, skins, types, names)
            props = data.get("props", {})
            scenes[key] = {
                "module": module,
                "name": name,
                "json": "cdn/resource/game/%s" % rel,
                "width": props.get("width"),
                "height": props.get("height"),
                "nodeTypes": types,
                "skins": sorted(skins),
                "nodeNames": sorted(names),
            }

    print("   scene 总数 %d" % len(scenes))

    print("[4/4] 粒度决策 ...")
    default_skins = {}
    default_skins_path = os.path.join(UNITY_ROOT, "Schemas", "LayaUI", "ui_default_skins.json")
    if os.path.exists(default_skins_path):
        with open(default_skins_path, encoding="utf-8") as f:
            default_skins = json.load(f)
        print("   手工默认图: %d 个 scene" % len(default_skins))
    counts = defaultdict(int)
    for key, sc in scenes.items():
        if "error" in sc:
            continue
        cls = scene_class.get(key)
        sc["tsClass"] = cls
        sc["kind"] = classes[cls]["kind"] if cls else "orphan"
        sc["bakedSkins"] = dict(classes[cls]["bakedSkins"]) if cls else {}
        # 手工默认表强制覆盖自动烘焙(用于纠正分支选择,如选服背景的普通/位置模式分支)
        for node, path in default_skins.get(key, {}).items():
            sc["bakedSkins"][node] = path
        # 被代码引用的非下划线节点(Bind 收集 = "_"前缀 ∪ codeNodes)
        node_names = set(sc.pop("nodeNames", []) or [])
        if cls:
            sc["codeNodes"] = sorted(n for n in (classes[cls]["thisRefs"] & node_names)
                                     if not n.startswith("_"))
        else:
            sc["codeNodes"] = []
        own = sorted(owners.get(cls, ())) if cls else []
        sc["ownerClasses"] = own
        sc["otherRefFiles"] = sorted(other_refs.get(cls, ())) if cls else []
        if cls is None:
            base = re.sub(r"(Skin\d*|Exml)$", "", sc["name"])
            if base != sc["name"]:
                sc["decision"] = "variant-unused"
                sc["variantOf"] = "%s/%s" % (sc["module"], base)
            else:
                sc["decision"] = "orphan-flag"
        elif sc["kind"] == "window":
            sc["decision"] = "view-prefab"
        elif len(own) == 1:
            sc["decision"] = "inline"
            sc["inlineHost"] = own[0]
        elif len(own) > 1:
            sc["decision"] = "shared-prefab"
        elif sc["otherRefFiles"]:
            sc["decision"] = "standalone-prefab"  # 控制器直开的组件
        else:
            sc["decision"] = "dead-flag"  # 代码无任何引用
        # 皮肤来源
        skin_src = {}
        for s in sc.get("skins", ()):
            skin_src[s] = classify_skin(s, client_root, atlas_index)
        sc["skinSource"] = skin_src
        sc["missingSkins"] = sorted(s for s, v in skin_src.items() if v == "missing")

    # inline 链防环:沿 inlineHost 上溯,遇到环或断链则降级为 standalone-prefab
    class_scene = {v: k for k, v in scene_class.items()}
    for key, sc in scenes.items():
        if sc.get("decision") != "inline":
            continue
        seen, cur, ok = {key}, sc, True
        while cur.get("decision") == "inline":
            host_key = class_scene.get(cur["inlineHost"])
            if host_key is None or host_key in seen or host_key not in scenes:
                ok = False
                break
            seen.add(host_key)
            cur = scenes[host_key]
        if not ok:
            sc["decision"] = "standalone-prefab"
            sc["notes"] = "inline 链成环或宿主缺 scene,降级独立 prefab"
            sc.pop("inlineHost", None)

    # 反向:每个宿主内联哪些 item
    inline_of = defaultdict(list)
    for key, sc in scenes.items():
        if sc.get("decision") == "inline":
            inline_of[class_scene[sc["inlineHost"]]].append(key)
    for host, items in inline_of.items():
        if host in scenes:
            scenes[host]["inlineItems"] = sorted(items)
    for key, sc in scenes.items():
        counts[sc.get("decision", "error")] += 1

    manifest = {
        "version": 1,
        "generatedAt": time.strftime("%Y-%m-%d %H:%M:%S"),
        "designWidth": 720,
        "designHeight": 1280,
        "moduleDirCase": {m: pascal(m) for m in sorted({s["module"] for s in scenes.values() if "module" in s})},
        "summary": dict(counts),
        "scenes": scenes,
    }
    out = os.path.join(UNITY_ROOT, "Schemas", "LayaUI", "ui_manifest.json")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=1, sort_keys=True)
    print("已写出 %s" % out)
    print("决策统计:", dict(counts))
    missing_total = sum(len(s.get("missingSkins", ())) for s in scenes.values())
    print("缺图引用合计: %d (详见 manifest 各 scene 的 missingSkins)" % missing_total)


if __name__ == "__main__":
    main()
