# 老端图片字体全局迁移（2026-08-05）

## 1. 事实源与盘点口径

- Electron 资源工具“位图字体”的默认目录是 `yu_client/cdn/resource/font`，H5 镜像目录仅作辅证；当前运行资源直接位于 CDN 树，不存在本轮应改的 `ver/N` 子目录。
- 当前 CDN 目录共有 66 份 `.fnt`、67 张 `.png`。多出的 `festival_level_0.png` 与 `festival_level.png` 内容相同，是历史别名，不是第 67 套字体；`rune_num` 的合法 page 文件本来就叫 `rune_num_0.png`。
- 老端现行 `h5/src` 有 91 行可执行的字面量 `LoadFont` 调用，另有 5 行动态调用：`FightingShowSmallItem.Font_name`、战斗 `FightFontType→font` 四处。动态索引 `_lab_multiple_${i}` 需要展开为 1～5 五个节点。
- `.scene` 中还有静态 `font` 属性。最终绑定以“scene 初值 + TS 运行时覆写”为准，TS 后写覆盖 scene，不能只扫任一侧。
- 合并 scene、可执行 TS 与动态战斗映射后，66 套中 55 套当前有运行引用；其余 11 套（`font_1/rune_num/rune_times/scene_add/scene_baby/scene_baoji_mainrole/scene_baoji_other/scene_damage_other/scene_pet/scene_talisman/view_fight_up_2`）只作为小体积历史素材同步，不强行制造业务入口。
- 单张图片美术字的现行独立链是 `resource/game/skillName/{skillId}.png`：目录 41 张，其中 `config_key_value[20001]` 当前白名单 34 个且都有对应图片。`common/texture/com_fight_label*` 已由现有 `FightingShowSmallItem` 正确使用，不重复造链。
- `resource/game/font/fight_small.*` 是历史副本；现行 `GameResPath.GetFont` 固定走 `resource/font`，因此不作为第二份运行资源迁入。

## 2. Unity 落地规则

- 66 套 FNT/PNG 全量同步到 `Assets/GameRes/Fonts/Bitmap`，由 `BitmapFontAssetBuilder` 生成同名静态 `TMP_FontAsset`，Shader 固定为 `TextMeshPro/Bitmap Custom Atlas`。PNG 已烤颜色，使用端顶点色必须为白色。
- 项目原本没有 `Resources/TMP Settings.asset`，批处理和运行时首次给动态创建的 TMP 组件赋字体会空引用；同步工具会幂等创建该设置并沿用现有 `DFPYuanW7/FZYHJW` 主字体与后备字体，不改普通正文的视觉选择。
- FNT 中约一批 `page file` 保留旧导出名；当 page 文件不存在时按 Electron 工具相同口径回退到 `<fontName>.png`。`id=-1` 是缺字占位，不写入 TextCore 字符表。
- Addressables 只注册最终 `.asset` 地址 `fonts/bitmap/<name>`；`.fnt` 和 atlas `.png` 作为字体资产依赖打包。三者若都注册，会产生三个完全相同的无扩展名地址并让类型解析不确定。
- 技能名字图强制导入为无 mipmap、无压缩、Clamp 的单 Sprite，地址为 `resource/game/skillname/<skillId>`。
- 已存在的人工 Prefab 由 `BitmapFontPrefabUpgrader` 增量改字体引用，不重转页面；后续首次转换的页面由 `LayaSceneConverter.BuildLabel` 直接识别 `props.font` 并绑定位图字体。
- 菜单入口：`神霄/资源/同步并应用老端位图字体`。它按“同步源文件→生成字体→扫描 scene/TS→增量改 Prefab→Addressables 分组→输出清单”顺序幂等执行；Addressables 只更新 `Remote_Fonts` 与 `Remote_resource/skillName`，不重排 `effect/object` 等并行流程正在使用的组。

## 3. 战斗图片字语义

- 20001 伤害飘字不再用普通 TMP 中文前缀或颜色近似。十套 `fight_font_*` 预热完成后才显示；首包早到会排队，不允许回退纯文字。
- 老端 `a/b/c` 是 atlas 中的完整美术字形，不是要显示的拉丁字母：主角攻击特殊态用 `a+damage`，主角被击特殊态用 `b+damage`，普通数字直接用对应攻/受击字体。
- `damage==0` 只有闪避显示图形字 `a`；免疫、无伤害帧静默。暂停或打开大窗口时不显示战斗飘字。
- 20001 的 `attack_trigger_skill_list` 与 20028 均按 `config_key_value[20001]` 白名单加载技能名字图，使用老端 `FightFontFiveAni` 的 `0.75→2→1→停留→淡出`演出；不再把技能名写成普通文字。
- `FightingShowSmallItem` 使用 `num_new`；`FightingUpItem` style 1/2 分别使用 `num_new_green`、`view_fight_up`；`FightingUpView` 继续使用 `fight_up/fight_up2`，但 atlas 已更新为当前 CDN 真值。

## 4. 机器验收与证据

执行：

```powershell
Unity.exe -batchmode -projectPath <worktree> `
  -executeMethod Shenxiao.EditorTools.BitmapFontCase.RunBatch `
  -logFile Temp/bitmap-font-case.log
```

`BitmapFontCase` 同一轮检查：66 份 FNT 与 66 个 TMP 字体一一对应、glyph/atlas/material 完整、十套战斗字体的数字及 `a/b/c` 图形字、41 张单 Sprite、34 个白名单图片、Addressable 唯一入口、公共战力 Prefab 序列化绑定，并输出三张全字体接触表、一张战斗映射表和一张技能名字图表。视觉验收必须使用真实图形设备，禁止加 `-nographics`；每张图还会统计非背景像素，避免“有 RT/网格但没出帧”的假 PASS。最后再跑第二次完整同步，要求 `copied/changedPrefabs/changedLabels` 全为 0。

最终结果：发现 106 条 `view/node/font` 绑定，其中 64 条命中当前已迁移页面并落到 33 个实际 Prefab；其余 42 条属于尚无对应可编辑 Prefab 的老端页面，保留在清单中并由后续首次转换自动应用。最终 Unity `CLIVERIFY bitmap-font PASS`，五张证据图的非背景像素依次为 `151367/164029/207607/57744/266633`，二次同步为 `copied=0, changedPrefabs=0, changedLabels=0`。

证据目录：`output/ui_route_audit/2026-08-05_bitmap-fonts/`；其中 `bitmap-font-inventory.json` 保存 scene/TS 发现的每个 `view/node/font` 及当前 Prefab 是否命中，`evidence/bitmap-font-contact-sheet-*.png` 保存真实 TMP Bitmap Shader 渲染结果，`route-ledger.json` 最终为 7/7 节点 `done`。

## 5. 2026-08-06 用户复查回卷：改名绑定遗漏与战斗字号

用户真实运行截图证明上一轮“64 条命中、其余都是未迁移页面”的结论不完整，本节覆盖该部分结论，但保留 2026-08-05 的原始数字作为历史证据。

- **主界面真实遗漏**：`MainUITopView._lb_fighting` 在人工 Prefab 中已改名为 `CombatPowerLabel`，`MainUITaskTeamView._lb_open_awaken_progress` 已改名为 `AwakenProgressLabel`。旧升级器只比较 `GameObject.name`，因此即使 View 的序列化 Bind 字段仍精确指向目标 TMP，也会被判为未命中。现改为优先解析 View 根组件的序列化字段，再以同名节点兜底；对应 Prefab 分别绑定 `num_new` 与 `temple_awaken_font`，彩色 atlas 保持白色顶点色。
- **清单误报**：`ElimMainView._lb_round` 虽在 `.scene` 残留 `font=elim_fnt_1`，节点类型和现行 TS 都证明它是 `Image`，运行时通过 `SetImageSprite(..., elim_numN)` 换独立轮数图。工具现排除这类 Image 字段，不再把图片链计为漏字体。`FightingUpView._lb_fight/_lb_add_fight` 已正确绑定 `fight_up/fight_up2`，此前同样只是节点改名造成的假阴性。
- **纠正后的盘点口径**：删除 1 条 Image 误报后共 105 条文字绑定；序列化 Bind 解析补回 4 条现行 Prefab 命中，当前应为 68 条命中、37 条尚无可编辑 Prefab。后续专项输出改到新的不可变目录 `output/ui_route_audit/2026-08-06_bitmap-font-remediation/`，不覆盖 2026-08-05 证据。
- **战斗飘字变小根因**：十套 `fight_font_*` 的 FNT `info size=36`，但 glyph 槽高为 100。老端 `FightFont` 把 Label 与 `BitmapFont.fontSize` 都设为 36，`autoScaleSize=true` 时等价于按图集 1:1 绘制；Unity 字体构建器按 glyph 槽高生成 `faceInfo.pointSize=100`，运行时却仍写 `fontSize=36`，于是又缩成 `36/100`。现由战斗消费者把老端 36 基准换算到字体原生 `pointSize`。
- **固定容器二次缺陷**：首次只把容器从 `320×60` 扩成 `480×120` 并设置 `Overflow`，字体接触表能完整出帧，但用户真实战斗确认运行时池对象仍会被截断。`Overflow` 不会替 Graphic 自动扩大本次文本的网格边界，Crit 首段还会把整个对象放到 2 倍。现改为每次池对象复用时按 `GetPreferredValues` 动态扩容（最低 `480×180`）、四周保留 `48/24` 透明边距、开启 `extraPadding` 并在首帧 `ForceMeshUpdate`；专项同时以普通长数字和 `a+长数字` 断言 `textBounds` 完整位于 Rect 内。
- **禁止全局改指标**：66 套 FNT 的 `glyphHeight/infoSize` 比例差异很大，且存在历史异常 info 值；不能因为战斗字体是 `100/36` 就把 `BitmapFontAssetBuilder` 的 face 指标统一改成 info size。主界面战力 `num_new` 的老端 `autoScaleSize=false`，按原生 22px；觉醒进度则按老端 Label 15 / BitmapFont 14 的比例换算为 `22×15/14≈23.57px`。尺寸必须逐消费链还原。
- **定向复验入口**：本次禁止再调用会同步/重建全部字体的 `RunBatch`。编辑器内使用菜单 `神霄/验证/位图字体复查专项（定向）`；关闭编辑器后可用 `-executeMethod Shenxiao.EditorTools.BitmapFontCase.RunRemediationBatch`。该入口只读两个 HUD Prefab、`FightingUpView` 与十套战斗字体，只向新证据目录写 PNG/JSON，不同步源文件、不重建 66 套字体、不改 Addressables、不扫描全部 Prefab。每次最终截图必须换不可变目录，本轮修正证据位于 `evidence-r2/`。
- **验证状态**：`Shenxiao.Module.Core.csproj` 与 `Shenxiao.Editor.csproj` 离线编译均为 0 error；当前 Unity 真实图形设备 `CLIVERIFY bitmap-font-remediation PASS`，两张图分别有 `19658/133181` 个非背景像素，长普通数字与 `a` 暴击前缀 bounds 断言通过。用户第一次真实战斗已把固定 `480×120` 方案回卷为“截断”；动态 Rect 修正仍等待用户第二次实际战斗确认，因此父路线继续 `pending`，不能仅凭专项 PASS 标最终 `done`。

## 6. 2026-08-06 第二次用户复查：TMP 路线作废与窗口生命周期

用户第二次真实战斗截图再次证明动态 Rect 方案仍是假修复：数字的 TMP `textBounds` 虽在自身 Rect 内，画面仍与老端 FNT 切片不一致；同时复现“战斗中打开角色面板再关闭，之后所有飘字永久消失”。第 5 节关于战斗字依靠 TMP preferred bounds 修复的结论到此作废，HUD 等普通位图 TMP 消费者不受影响。

- **旧端真实绘制方式**：`Laya.BitmapFont.parseFont` 按 FNT 的 `x/y/width/height/xoffset/yoffset/xadvance` 创建每个字符纹理，`BitmapFont._drawText` 再逐字 `drawImage`；它不使用 FNT `common.lineHeight/base` 做通用文字基线排版。十套战斗 FNT 与 Unity 同名源文件 SHA-256 一致，问题不是图片或位置文件没复制，而是 Unity 把逐字图片链错误交给了 TMP 排版。
- **最终代码路线**：`DamageFontRenderer` 的池对象改用 `LegacyBitmapTextGraphic`。现有 `TMP_FontAsset` 只作为 FNT 生成后的只读 `GlyphRect/GlyphMetrics/atlas` 数据容器；运行时逐字生成原图四边形，矩形直接取全部 glyph 的真实边界，短串复用成长串时重新生成，不再经过 TMP 的 pointSize、lineHeight、baseline、preferred bounds 或文字容器裁切。未同步、未重建任何字体/图集，也未改 Addressables。
- **生命周期根因**：旧 `CanShowCombatFloat` 遍历 `UILayer.Window` 一级子节点，只要某个模块根 `activeInHierarchy` 就拒绝所有新飘字。角色模块关闭只隐藏内部 View，缓存根仍 active，因此首次开窗后门槛永久为 false。现删除窗口根扫描，只保留 `Time.timeScale` 暂停门槛；战斗字位于 `sortingOrder=-30` 的场景子 Canvas，打开窗口时由窗口层自然遮挡，关闭后无需恢复令牌或重建根。
- **定向门禁**：`BitmapFontCase` 的 r3 专项逐字比较十套 FNT 与生成资产的 rect/metrics，检查直接网格字符数、原 atlas、短串→长串扩展和网格边界，并构造 active 的角色模块缓存根验证开窗前后门槛不被污染。旧的 `textBounds` 断言已删除。
- **当前验证状态**：`Shenxiao.Module.Core.csproj` 与 `Shenxiao.Editor.csproj` 离线编译 0 error；未占用用户 Unity，r3 图形专项与同一场战斗的“有飘字→开角色→关闭→仍有飘字”仍待用户实际复验，路线保持 `pending`。

## 7. 2026-08-06 第三次用户复查：自定义 Graphic 零像素

用户第三次在真实战斗中确认修改后完全没有飘字。当前 `Editor.log` 同一战斗持续收到 `20001`，并存在 `damage=64/66/...` 的主角攻击伤害，因此协议、伤害解析和 `FightController` 消费入口正常；日志也没有字体预热不完整或运行异常。故障限定在上一版 `LegacyBitmapTextGraphic` 的实际场景出帧链。

- **验收漏洞**：r3 设计只检查自定义 `CanvasRenderer` 的 Mesh、顶点数、UV 数据和局部边界。Mesh 存在只能证明 `OnPopulateMesh` 生成了几何，不能证明该自定义 `MaskableGraphic` 在真实 `UILayer.Scene` 的嵌套 Canvas、材质和裁剪链中产生像素。该门禁与先前“Renderer/RT 存在不等于模型出帧”属于同类假绿。
- **修复方式**：保留 FNT `GlyphRect/GlyphMetrics` 的逐字边界计算，但移除自定义 Mesh 提交；每个字符改用 Unity 内置 `RawImage`，直接把同一 atlas 赋给 `texture`，把 GlyphRect 换算为 `uvRect`，并按 `xoffset/yoffset/xadvance` 设置子 Rect。短串复用成长串时复用或扩展 RawImage 子项，多余子项隐藏；父级 `CanvasGroup` 统一处理动画淡出。
- **范围**：只修改 `DamageFontRenderer` 和定向 `BitmapFontCase`，没有修改、同步或重建任何 FNT/PNG/TMP 字体资产，没有改 Addressables，也没有扫描全库资源。新证据目录固定为 `evidence-r4/`，避免覆盖已作废的 r2/r3 结论。
- **当前验证状态**：Core/Editor 串行离线编译均为 0 warning / 0 error；未操作用户 Unity。玩家真实战斗像素和“出现→开角色→关闭→再次出现”仍待复验，路线继续 `needs-runtime-verify`。

## 8. 2026-08-06 第四次用户复查：替换图跨 FNT 槽串字

用户第四次真实战斗确认 `RawImage` 路线已经恢复出帧，且日志中的四个真实伤害值依次为 `63/64/64/66`；但截图中的每个两位数都带有额外竖笔或相邻数字残片。第 6 节“问题不是图片”的判断只适用于当时检查的文件同源性，现已被更细的图像槽位检查推翻。

- **根因**：十套战斗 `.fnt` 从未改变，Unity 字体资产的 `GlyphRect/GlyphMetrics` 也逐项正确；但 2026-08-04 的全局“字更换”只替换了 `yu_client/cdn/resource/font/fight_font_*.png`，没有同步调整对应 FNT。替换图把新数字画得比旧槽更宽，实心笔画直接跨过左右边界并与相邻数字重叠。Unity 同步的是 CDN 新图，而 `h5/laya/assets/resource/font` 仍保留替换前、与 FNT 配套的原图，所以此前只比较“Unity 与当前 CDN 哈希相等”反而把错误资源当成了绿灯。
- **证据**：替换前 CDN 的十张战斗 PNG Git blob 与 H5 镜像十张 PNG 完全一致；当前 CDN/Unity 十张则全部不同。对每个数字槽左右边界扫描 Alpha，错误替换图在阈值 `224/255` 下有 77 个数字槽出现实心像素触边，替换前配套图为 0 个。`fight_font_attack` 的错误图在旧 FNT 的 `0/2/3/4/5/6` 连续槽中已经能直接看到相邻字串入。
- **修复**：只把十张 `fight_font_attack/beattack/baoji/huixin/zhuoyue/shenwu/gedang/fantan/liuxue/huifu.png` 恢复为替换前、与现有 FNT 配套的版本，并同时修正老端 CDN 事实源与 Unity 固定副本。未改任何 `.fnt`、`.asset`、Prefab 或 Addressables，也未同步、导入或重建其他 56 套字体资源。
- **新门禁**：`BitmapFontCase` 除了核对 FNT 坐标、RawImage UV 与真实像素，还会读取这十张 PNG，逐个检查 `0-9` 的左右槽边界；Alpha 大于等于 224 的实心笔画触边即失败。以后若要重做战斗字体，必须逐字在固定 FNT 槽内生成并先通过该门禁，不能把整张通用换字结果直接覆盖专用战斗 atlas。
- **当前验证状态**：静态资源哈希与 Alpha 槽位检查已通过；`Shenxiao.Module.Core.csproj` 离线编译为 97 个既有 warning / 0 error，`Shenxiao.Editor.csproj` 为 2 个既有 warning / 0 error。未占用用户 Unity，因此 r5 真实图形专项与玩家同场最终复验仍待执行。玩家复验应覆盖“正常两位/多位伤害可读、无串字/截断，以及开关角色面板后继续出现”。
