# UI 运行时比对(diff oracle)

把「老端长什么样」和「Unity 转出来长什么样」各自 dump 成一棵运行时节点树,
逐节点比对,输出**缺失 / 偏移 / 换错图 / 大小不对**的清单——
把"一屏屏用眼睛找"变成"机器告诉你第几个节点偏了多少、哪几个没了"。

为什么是运行时比对:转换器吃的是静态 `.scene/.json`,看不到**运行时加载/摆位**的东西
(特效、动态 UI),所以静态比对抓不到那些 bug。两边都在**运行时**抓,才是真相对真相。

## 一、产两份快照

### Laya 侧(真相)→ `page_snapshot_*.json`
1. 开 electron 工具(`tools/yu-resource-tool`)连上老客户端,导航到目标页(如登录创角)。
2. 用快照功能导出该视图(底层是 `pageSnapshot.js / __sxExportPageSnapshots__`),
   得到 `page_snapshot_<页名>_<时间>.json`。
   - 节点的 `globalBounds` 是 Laya 自己算好的屏幕矩形(左上角原点、Y 向下),
     已经把运行时加载/摆位的结果烤进去了。

> 注意:`pageSnapshot.js` 的 `readGlobalBounds` 已改成取四角包围盒(支持旋转节点)。
> 改完要让工具生效——`npm run dev` 或重新 `build` 一次 electron 工具。

### Unity 侧(结果)→ `ui_dump.json`
1. **先把 GameView 分辨率设成与老端一致的设计分辨率 720×1280**(同宽高比)。
   否则逐轴归一化会把一个轴压扁,`OFFSET/SIZE` 会误报(工具会在宽高比不一致时告警)。
2. Play 跑到目标页 → 菜单 `神霄/调试/UI运行态/截图+节点Dump`。
3. 得到 `output/runtime_unity/<时间>/ui_dump.json`,每个节点带 `screenRect`
   (左上角原点、Y 向下,与 Laya `globalBounds` 同坐标系)。

> `screenRect` 只在 **Play 模式**输出(非 Play 时 `Screen.height` 不是 GameView 尺寸,Y 翻转会错)。
> 每个节点的投影相机按其所属 Canvas 求值(支持嵌套 3D / overrideSorting 子画布)。

## 二、跑比对

```bash
python ui_runtime_diff.py \
  --laya  page_snapshot_LoginCreateRoleView_xxx.json \
  --unity output/runtime_unity/xxxx/ui_dump.json \
  --view  LoginCreateRoleView \
  --out   report.json
```

参数:
- `--view`     要比的视图名(默认取快照里第一个)。
- `--pos-thresh`  位置阈值(归一化,默认 0.01 = 屏幕的 1%)。
- `--size-thresh` 尺寸比例阈值(默认 0.10 = ±10%)。
- `--include-invisible` 把老端不可见节点也纳入 MISSING。

自检(不需要真实快照,验证工具本身):
```bash
python ui_runtime_diff.py --selftest
```

## 三、读报告

| 严重度 | 含义 | 对应你的痛点 |
|---|---|---|
| `MISSING`  | 老端可见/有视觉内容,但 Unity 没有对应节点(含**无名特效**) | "少了" |
| `RESOURCE` | 贴图/skin 与 Unity sprite 对不上 | 换错图 / 没映射 |
| `OFFSET`   | 位置偏移 > 阈值,给 `±dx,±dy`(老端设计像素) | "位置不对" |
| `SIZE`     | 尺寸比例偏离 > 阈值 | 大了 / 小了 |
| `HIDDEN`   | 老端可见但 Unity 节点 inactive | |
| `EXTRA`    | Unity 有、老端没有;与已匹配节点重名时必报(疑似重复/错位) | |

对齐方式:按**节点名 + 最长公共路径后缀**(Laya 节点名 1:1 进了 prefab;
后缀匹配能区分不同子视图里的同名节点,如两个 `_img_tips`)。无名但有贴图/文字的
节点(特效)按资源/文字 + 距离几何对齐。

## 四、固定流程(每页一轮,直到清单见底)

```
转换该页  →  两边各 dump 一份运行时快照  →  跑 diff  →
按清单修(MISSING/RESOURCE 优先,再 OFFSET/SIZE)  →  重转  →  重新 diff  →  清零
```

## 已知前提 / 限制
- 两边必须**同宽高比**(建议 Unity GameView = 720×1280)。不一致时只有 MISSING/RESOURCE 可信,
  OFFSET/SIZE 会告警且不可信。
- 零尺寸的纯容器节点不做几何比对(只看存在性/资源)。
- `RESOURCE` 依赖 Unity sprite 名 == 源 png basename(散图管线成立;图集回退也保留同名)。
