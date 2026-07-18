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

# 根锚定语义静态提取(tsChain / isCenter / rootLayout)——————————————
# 老端 view 的根节点锚定语义大部分写在【TS 运行时基类】里,而不是 .scene 数据里:
#   BaseView1.ts        OnLoadCompleted 里 if (is_center) centerX = centerY = 0
#   BaseWindowComponent.ts  load_callback 里 display_obj.bottom = 0 / centerX = 0
#   另有约 45 个业务子类在 LoadSuccess / load_callback 里自己覆写 display_obj.{left,right,...}
# 转换器只读 .scene 的 props,于是把这层压成「有显式锚就算,否则无条件居中」的二值。
# 这里把三层语义在转换期静态提取出来,喂给 Unity 侧已有的 LayaRectMath。
#
# 提取口径(踩过的坑都在这):
#   1. 必须先剥注释:全项目有 23 行被注释掉的 this.is_center,以及
#      BaseWindowComponent.ts 里注释掉的 percentHeight,直接跑正则会吃进来。
#   2. is_center 必须带 \b 和 this. 前缀,否则误吞 is_center_x / is_center_y 与局部同名参数。
#   3. display_obj 必须限定 this/self 接收者,否则会吞掉 item.display_obj.left 这类
#      「子 item 的定位」,那不是根锚。
#   4. 只认构造期(constructor / LoadSuccess / load_callback / InitView / OnLoadCompleted)
#      里、且不嵌在任何 if / 回调内的赋值。open_callback / AfterOpen / OpenAnimation / SetData
#      里的赋值是运行时开关或补间动画起始值,静态折叠会锁死动画。
#   5. scaleX / scaleY 整体不提取:全项目 50 余处 scale 赋值绝大多数是开场动画起始值
#      (BaseView1 的 0.825、CongratulationObtainView 的 0.5),折叠会让界面整体缩水。
#   6. 刘海高度一律折算成 0 并打 safeAreaTop 标记,绝不烤成 60px——老端
#      Util.GetLiuhaiHeight() 是硬编码 60 + 静态缓存永不更新(老端 bug),
#      Unity 侧已有 SafeAreaRoot,烤进去会叠成双倍内缩。

RE_IS_CENTER = re.compile(r"\bthis\.is_center\s*=\s*([A-Za-z0-9_$.]+)")
_ANCHOR_KEYS = "left|right|top|bottom|centerX|centerY|scaleX|scaleY"
RE_ROOT_ANCHOR = re.compile(r"(?:this|self)\.display_obj\.(%s)\s*=" % _ANCHOR_KEYS)
# 链式赋值 a = b = c = 0:把整串 LHS 一次吃掉,余下的才是 RHS
RE_ROOT_CHAIN = re.compile(
    r"^\s*((?:(?:this|self)\.display_obj\.(?:%s)\s*=\s*)+)(.*?)\s*;?\s*$" % _ANCHOR_KEYS, re.S)
# 刘海 5 种写法:Util.GetLiuhaiHeight() / ...|| 0 / ...+625 / 局部变量 liu_hai_height / liuhai_height
RE_LIUHAI = re.compile(r"Util\.GetLiuhaiHeight\s*\(\s*\)|\bliu_?hai_height\b")
RE_NUM = re.compile(r"^-?\d+(?:\.\d+)?$")

# 作用域头部识别:this.xxx = function(){ / this.xxx = () => { / xxx = () => { / public Xxx(){
RE_ASSIGN_FUNC = re.compile(r"(?:this|self)\.(\w+)\s*=\s*(?:function\s*)?\([^()]*\)\s*(?:=>)?\s*$")
RE_ARROW_FIELD = re.compile(r"(\w+)\s*=\s*(?:\([^()]*\)|\w+)\s*=>\s*$")
RE_METHOD_HEAD = re.compile(r"(\w+)\s*\([^()]*\)\s*(?::\s*[\w<>\[\],.\s]+)?\s*$")
_BLOCK_KEYWORDS = {"if", "else", "for", "while", "switch", "catch", "do", "try",
                   "function", "return", "typeof", "in", "of"}
# 构造期方法白名单:只有这些方法体里的字面量赋值可以静态折叠
CTOR_PERIOD_METHODS = {"constructor", "LoadSuccess", "load_callback", "InitView", "OnLoadCompleted"}
# 本批次实际写出的键(scaleX/scaleY 见上文第 5 条,不提取)
LAYOUT_KEYS = ("left", "right", "top", "bottom", "centerX", "centerY")
# 按轴分组:同轴内老端 Laya 的优先级是 centerX > left > right / centerY > top > bottom
AXIS_OF = {"left": "x", "right": "x", "centerX": "x",
           "top": "y", "bottom": "y", "centerY": "y"}


def strip_comments(text, blank_strings=True):
    """把 // 与 /* */ 替换成等长空格(保留行列号与全文长度,便于按原偏移切片)。

    blank_strings=True 时同时把字符串/模板串的【内容】也刷成空格(保留引号本身),
    这样后续的花括号配对不会被串里的 { } 带偏,也不会误匹配串里的代码文本。
    正则字面量按「前一个非空字符」启发式判定,避免 /.../ 里的 // 被当成注释。
    """
    out = list(text)
    n = len(text)
    i = 0
    prev_sig = ""  # 上一个有效非空字符,用于区分「除号」与「正则起始」
    while i < n:
        c = text[i]
        if c == "/" and i + 1 < n:
            nxt = text[i + 1]
            if nxt == "/":
                while i < n and text[i] != "\n":
                    out[i] = " "
                    i += 1
                continue
            if nxt == "*":
                while i < n and not (text[i] == "*" and i + 1 < n and text[i + 1] == "/"):
                    if text[i] != "\n":
                        out[i] = " "
                    i += 1
                for j in range(i, min(i + 2, n)):
                    out[j] = " "
                i += 2
                continue
            if prev_sig in ("", "(", ",", "=", ":", "[", "!", "&", "|", "?", "{", "}", ";", "+", "-", "*", "%", "<", ">", "~", "^"):
                # 正则字面量:整体跳过(不改写),防止里面的 // 被误认成注释
                i += 1
                while i < n and text[i] != "\n":
                    if text[i] == "\\":
                        i += 2
                        continue
                    if text[i] == "/":
                        i += 1
                        break
                    i += 1
                prev_sig = "/"
                continue
        if c in ("'", '"', "`"):
            quote = c
            i += 1
            while i < n:
                ch = text[i]
                if ch == "\\":
                    if blank_strings:
                        out[i] = " "
                        if i + 1 < n and text[i + 1] != "\n":
                            out[i + 1] = " "
                    i += 2
                    continue
                if ch == quote:
                    break
                if blank_strings and ch != "\n":
                    out[i] = " "
                i += 1
            i += 1
            prev_sig = quote
            continue
        if not c.isspace():
            prev_sig = c
        i += 1
    return "".join(out)


def _classify_header(header):
    """花括号前的头部文本 -> 函数/方法名;普通语句块(if/else/对象字面量等)返回 None。"""
    h = header.rstrip()
    if not h:
        return None
    m = RE_ASSIGN_FUNC.search(h)
    if m:
        return m.group(1)
    m = RE_ARROW_FIELD.search(h)
    if m:
        return m.group(1)
    m = RE_METHOD_HEAD.search(h)
    if m and m.group(1) not in _BLOCK_KEYWORDS:
        return m.group(1)
    return None


def build_scopes(sbody):
    """扫一遍剥过注释的类体,返回 [(开始偏移, 结束偏移, 函数名或 None)]。

    用花括号配对而不是缩进:全项目同时存在 `LoadSuccess = () => {`(类字段箭头)与
    顶格不缩进的 `public constructor(){`,缩进锚定两种都会漏。
    """
    scopes = []
    stack = []
    prev = 0  # 上一个语句边界,用于截出花括号前的头部文本
    for i, c in enumerate(sbody):
        if c == "{":
            stack.append((i, _classify_header(sbody[prev:i])))
            prev = i + 1
        elif c == "}":
            if stack:
                st, nm = stack.pop()
                scopes.append((st, i, nm))
            prev = i + 1
        elif c in ";\n":
            prev = i + 1
    for st, nm in stack:  # 类体被下一个 class 截断导致未闭合的,按到结尾算
        scopes.append((st, len(sbody), nm))
    scopes.sort()
    return scopes


def parse_anchor_rhs(rhs):
    """赋值右值 -> (数值, 是否刘海折算);不可静态提取返回 None。

    LoginEnterView.ts 的 `left = right = bottom = this.display_obj.top` 末端是【读】
    未赋值的 top(老端 bug),会落到这里返回 None,不提取。
    """
    s = rhs.strip().rstrip(";").strip()
    if RE_NUM.match(s):
        return float(s), False
    if RE_LIUHAI.search(s):
        # 扣掉刘海项、保留常数项:GetLiuhaiHeight()+625 -> 625 且打标记
        rest = RE_LIUHAI.sub("", s)
        rest = re.sub(r"\|\|\s*0", "", rest)
        rest = rest.replace("(", "").replace(")", "").strip()
        if rest == "":
            return 0.0, True
        m = re.match(r"^([+-])\s*(\d+(?:\.\d+)?)$", rest)
        if m:
            v = float(m.group(2))
            return (v if m.group(1) == "+" else -v), True
        return None
    return None


def _num(v):
    """整数就写成 int,避免 manifest 里出现 0.0 这种噪声。"""
    return int(v) if float(v).is_integer() else v


def extract_is_center_own(sbody):
    """本类体内 this.is_center 的字面量赋值。

    同一类里赋了不一致的字面量(7 个 tooltip:构造期 true、SetData 里按参数切 false),
    或赋的是非字面量(GuildFightMapView = this.click_bg_toClose)-> 'dynamic'。
    """
    vals = set()
    for m in RE_IS_CENTER.finditer(sbody):
        raw = m.group(1)
        if raw == "true":
            vals.add(True)
        elif raw == "false":
            vals.add(False)
        else:
            return "dynamic"
    if not vals:
        return None
    if len(vals) > 1:
        return "dynamic"
    return vals.pop()


def extract_root_layout_own(sbody, scopes):
    """本类体内构造期的 display_obj 根锚字面量赋值。

    返回 (layout, suspects):
      layout   {键: (数值, 是否刘海折算)}
      suspects 提取时被丢弃的原因串,供人工核对
    """
    layout = {}
    suspects = []
    done = set()
    for m in RE_ROOT_ANCHOR.finditer(sbody):
        # 回溯到语句起点,把链式赋值整条吃掉(同一条语句只处理一次)
        start = max(sbody.rfind(ch, 0, m.start()) for ch in ";\n{}") + 1
        if start in done:
            continue
        done.add(start)
        end = len(sbody)
        for ch in ";\n":
            p = sbody.find(ch, m.start())
            if p != -1:
                end = min(end, p)
        stmt = sbody[start:end]
        cm = RE_ROOT_CHAIN.match(stmt)
        if not cm:
            suspects.append("语句解析失败: %s" % stmt.strip()[:80])
            continue
        keys = re.findall(r"display_obj\.(%s)" % _ANCHOR_KEYS, cm.group(1))
        # 作用域归属:必须在构造期方法体内,且不嵌在 if / 回调里
        containing = [s for s in scopes if s[0] < start < s[1]]
        named = [s for s in containing if s[2]]
        method = named[-1][2] if named else None
        if method not in CTOR_PERIOD_METHODS:
            suspects.append("非构造期(%s): %s" % (method or "?", ",".join(keys)))
            continue
        depth_after = sum(1 for s in containing if s[0] > named[-1][0])
        if depth_after:
            # 埋在 if / 事件回调里的,是运行时开关(收展动画、显隐切换),不能折叠
            suspects.append("嵌套分支内(%s): %s" % (method, ",".join(keys)))
            continue
        parsed = parse_anchor_rhs(cm.group(2))
        if parsed is None:
            suspects.append("右值非字面量(%s): %s" % (",".join(keys), cm.group(2).strip()[:60]))
            continue
        val, is_safe = parsed
        for k in keys:
            if k in ("scaleX", "scaleY"):
                continue  # 见文件头第 5 条:scale 绝大多数是动画起始值
            layout[k] = (val, is_safe)
    return layout, suspects


def build_ts_chain(name, classes):
    """从自身类到根基类的完整继承链。

    链尾是 Laya 的外部类(HashObject / Box 等):它不在 classes 里,append 后终止。
    """
    chain = []
    seen = set()
    cur = name
    while cur and cur not in seen:
        seen.add(cur)
        chain.append(cur)
        info = classes.get(cur)
        if info is None:
            break  # 外部/Laya 类,已收进链尾
        cur = info["base"]
    return chain


def resolve_is_center(chain, classes):
    """沿链求 is_center 生效值:最派生的显式赋值胜;链上没人赋过返回 None。

    注意 BaseView1 的 `protected is_center: boolean = false` 是字段声明不是 this. 赋值,
    不会被当成显式赋值——所以「链上无赋值」= 老端默认 false = 不居中。
    """
    for cname in chain:
        info = classes.get(cname)
        if info is not None and info.get("isCenterOwn") is not None:
            return info["isCenterOwn"]
    return None


def resolve_root_layout(chain, classes):
    """沿链从根基类往自身叠加,得到 display_obj 上最终的属性集合。

    合并语义是【累加并集、同键子类胜】,不是按轴替换:老端运行时基类与子类的赋值
    落在同一个 display_obj 上,BaseWindowComponent 给的 bottom=0 不会因为子类
    又给了 top 就消失(DiamondFightBattleView 正是 top 与 bottom 同时存在 -> 纵向拉伸)。
    """
    vals, src, safe = {}, {}, {}
    for cname in reversed(chain):  # 根基类 -> 自身
        info = classes.get(cname)
        if info is None:
            continue
        for k, (v, is_safe) in (info.get("rootLayoutOwn") or {}).items():
            vals[k], src[k], safe[k] = v, cname, is_safe
    if not vals:
        return None
    out = {}
    for k in LAYOUT_KEYS:
        if k in vals:
            out[k] = _num(vals[k])
    if not out:
        return None
    if any(safe.get(k) for k in out):
        out["safeAreaTop"] = True
    out["_from"] = {k: src[k] for k in out if not k.startswith("_") and k != "safeAreaTop"}
    return out

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
            # 剥注释版全文:长度/偏移与原文逐字符对齐,可用同一组 match 偏移切片。
            # 只给根锚定提取用,原有字段(base_file / bakedSkins / thisRefs)仍走原文,行为不变。
            stext = strip_comments(text)
            matches = list(RE_CLASS.finditer(text))
            for i, m in enumerate(matches):
                name, base = m.group(1), m.group(2).split(".")[-1]
                body_end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
                body = text[m.start():body_end]
                sbody = stext[m.start():body_end]
                bf = RE_BASE_FILE.search(body)
                lf = RE_LAYOUT_FILE.search(body)
                if name in classes:
                    # 重名类:保留先发现的,记录冲突
                    classes[name].setdefault("dup_files", []).append(path)
                    continue
                own_layout, layout_suspects = extract_root_layout_own(sbody, build_scopes(sbody))
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
                    # 根锚定语义(本类自己写的那一层,继承合并在 resolve_* 里做)
                    "isCenterOwn": extract_is_center_own(sbody),
                    "rootLayoutOwn": own_layout,
                    "rootLayoutSuspect": layout_suspects,
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
    # 同一个 scene 被多个类共用(common/BaseWindowSkin 被 80+ 个业务窗共用,
    # holySeal/HolySealAttrView 等被 4 个类共用)。tsClass 只能记一个,
    # 这里把全部共用者收下来做根锚定的冲突检测,不让后来者被静默吞掉。
    scene_classes_all = defaultdict(list)
    for name, info in classes.items():
        if info["module"] and info["layout"]:
            k = "%s/%s" % (info["module"], info["layout"])
            scene_class.setdefault(k, name)
            scene_classes_all[k].append(name)

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
        # 根锚定语义:以 scene 为索引反查 TS(不是拿 TS 类正推,老端 view 类比 scene 多,
        # 正推会给不存在的 scene 键写配置)
        chain = build_ts_chain(cls, classes) if cls else None
        sc["tsChain"] = chain
        sc["isCenter"] = resolve_is_center(chain, classes) if chain else None
        sc["rootLayout"] = resolve_root_layout(chain, classes) if chain else None
        peers = scene_classes_all.get(key, ())
        if len(peers) > 1:
            # 共用者算出来不一致的,列进 rootLayoutConflict 交人工裁决,
            # 转换器看到这个字段应保持现状而不是采信 tsClass 那一个
            sigs = set()
            for p in peers:
                pc = build_ts_chain(p, classes)
                rl = resolve_root_layout(pc, classes)
                if rl:  # 比数值不比来源:同值但由不同类贡献不算冲突
                    rl = {k: v for k, v in rl.items() if k != "_from"}
                sigs.add((json.dumps(rl, sort_keys=True), resolve_is_center(pc, classes)))
            if len(sigs) > 1:
                sc["rootLayoutConflict"] = sorted(peers)
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
    report_root_layout(scenes)


def report_root_layout(scenes):
    """根锚定语义提取的自检与统计。

    自检基线:ui_root_layouts.json 里 7 条手工配置应能被 TS 提取逐字段复现
    (另 3 条是 x/y/scale,不来自 display_obj 赋值,必须保留人工表)。
    正则或作用域归属一旦退化,这里会立刻炸出来,比事后看 prefab diff 便宜得多。
    """
    n_chain = n_layout = n_conflict = 0
    by_center = defaultdict(int)
    fallback = defaultdict(int)  # 既无 rootLayout 又非居中的(反向错候选),按 decision 分
    safe_area = 0
    for key, sc in scenes.items():
        if "error" in sc:
            continue
        if sc.get("tsChain"):
            n_chain += 1
        rl = sc.get("rootLayout")
        if rl:
            n_layout += 1
            if rl.get("safeAreaTop"):
                safe_area += 1
        if sc.get("rootLayoutConflict"):
            n_conflict += 1
        if sc.get("tsClass"):
            by_center[sc.get("isCenter")] += 1
            if not rl and sc.get("isCenter") is not True:
                fallback[sc.get("decision", "?")] += 1
    print("─ 根锚定语义提取 ─")
    print("  拿到 tsChain: %d / %d 个 scene 条目" % (n_chain, len(scenes)))
    print("  拿到非空 rootLayout: %d (其中刘海折算 safeAreaTop: %d)" % (n_layout, safe_area))
    print("  isCenter 生效值分布(有 tsClass 的条目):", dict(by_center))
    print("  既无 rootLayout 又未生效居中(反向错候选,按 decision 分):", dict(fallback))
    print("  共用 scene 且根锚定冲突(需人工裁决): %d" % n_conflict)
    # 自检:这 7 条必须与 Schemas/LayaUI/ui_root_layouts.json 的手工值逐字段一致
    baseline = {
        "common/BaseWindowSkin": {"centerX": 0, "bottom": 0},
        "mainUI/MainUITopView": {"centerX": 0, "top": 0},
        "mainUI/MainUISkillView": {"centerX": 0, "bottom": 254},
        "mainUI/MainUIChatView": {"centerX": 0, "bottom": 0},
        "mainUI/MainUISecondaryView": {"left": 0, "right": 0, "bottom": 290},
        "mainUI/MainUITaskTeamView": {"left": 10, "bottom": 380},
        "mainUI/MainUIDownView": {"centerX": 0, "bottom": 0},
    }
    bad = []
    for key, want in baseline.items():
        got = scenes.get(key, {}).get("rootLayout") or {}
        got = {k: v for k, v in got.items() if k in LAYOUT_KEYS}  # 严格:多提取出键也算退化
        if got != want:
            bad.append("%s 期望 %s 实得 %s" % (key, want, got))
    if bad:
        print("  ⚠ 自检未通过(提取逻辑可能退化):")
        for b in bad:
            print("    " + b)
    else:
        print("  自检通过:%d 条手工基线全部逐字段复现" % len(baseline))


if __name__ == "__main__":
    main()
