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
- **战斗飘字变小根因**：十套 `fight_font_*` 的 FNT `info size=36`，但 glyph 槽高为 100。老端 `FightFont` 把 Label 与 `BitmapFont.fontSize` 都设为 36，`autoScaleSize=true` 时等价于按图集 1:1 绘制；Unity 字体构建器按 glyph 槽高生成 `faceInfo.pointSize=100`，运行时却仍写 `fontSize=36`，于是又缩成 `36/100`。现由战斗消费者把老端 36 基准换算到字体原生 `pointSize`，并把动态文本容器从 `320×60` 扩到 `480×120`、启用 Overflow 防裁切。
- **禁止全局改指标**：66 套 FNT 的 `glyphHeight/infoSize` 比例差异很大，且存在历史异常 info 值；不能因为战斗字体是 `100/36` 就把 `BitmapFontAssetBuilder` 的 face 指标统一改成 info size。主界面战力 `num_new` 的老端 `autoScaleSize=false`，按原生 22px；觉醒进度则按老端 Label 15 / BitmapFont 14 的比例换算为 `22×15/14≈23.57px`。尺寸必须逐消费链还原。
- **定向复验入口**：本次禁止再调用会同步/重建全部字体的 `RunBatch`。编辑器内使用菜单 `神霄/验证/位图字体复查专项（定向）`；关闭编辑器后可用 `-executeMethod Shenxiao.EditorTools.BitmapFontCase.RunRemediationBatch`。该入口只读两个 HUD Prefab、`FightingUpView` 与十套战斗字体，只向新证据目录写 PNG/JSON，不同步源文件、不重建 66 套字体、不改 Addressables、不扫描全部 Prefab。
- **验证状态**：`Shenxiao.Module.Core.csproj` 与 `Shenxiao.Editor.csproj` 离线编译均为 0 error；真实图形设备专项仍需在不打断用户前台的窗口执行，未执行前不得把本次视觉回卷写成最终 `done`。
