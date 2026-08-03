# Art 模板验收与挂点排查经验（2026-08-03）

## 目标与边界

Art 项目 `E:\Project\ArtsProject` 是美术直接复制使用的模板工程。美术必须在 Art 项目内完成模型结构、骨架挂点、
握柄中心和轴向；程序侧只在主工程「神霄/资产管理」中设置一次 Art 项目根目录，然后为当前模型选择具体目录并点击
「选择目录并导入替换」。不以游戏内逐件偏移作为新资源交付流程。

## 三类模板的真值来源

- Role：`role_mount_profile.json` 是该骨架 `rhand` 的唯一真值；`role_assembly_profile.json` 是该角色
  跨动作体量和标准附件空间倍率的唯一真值。`head_mount` 由实际头骨绑定姿态逆矩阵生成，`root` 作为特效挂点。
- Head：`head_attach` 是对接点，`head_content` 承载 FBX/骨架/动画。新资源从单位模板开始。
- Weapon：`weapon_attach` 是握柄中心和轴向，`weapon_content` 保持单位变换。存在 `Bone_wq_r` 时二者必须一致。

粒度规则：每套角色骨架校准一次 `rhand`，每把武器定义一次 `weapon_attach`，每套头饰定义一次 `head_attach`；
不是每个“角色 × 动作 × 部件”组合分别调一组数值。

## 本次 1213 / 1200 得到的经验

1. `rhand` 跟随动画稳定不等于位置正确。1213 原先误把 `Bip001 R Hand` 腕骨原点当手心；数值误差始终接近 0，
   但护手仍偏离手掌。最终按右手及手指蒙皮权重网格测得 `(-0.14,0.03,0.02)`。
2. locator 位置对齐不等于方向正确。1200 的 `Bone_wq_r` 带 `Z=89.71°` 握持轴，旧程序只平移定位器、忽略旋转，
   导致剑尖朝上。现在运行时同时解算 locator 的位置和旋转。
3. prefab 根位移可能只是 Art 单独预览摆位。1200 的 `(0.9067,-1.10961,-0.98606)` 烘焙后会把剑推到肩部，
   因此丢弃；不能看到非零根变换就认定它是单位或挂点补偿。
4. 缩放问题要分层判断：同一个 Role 内，展示缩放会同时作用于身体和子挂件，不应给单件 Head/Weapon 反向补；
   但同一标准附件跨不同 Role 时，如果两套角色 prefab 的 `landingScale` 不同，装配后的公共父缩放也不同，必须由
   Role 的 `attachmentSpaceScale` 统一换算，不能全局缩放共享附件。部件自身包络错误仍回到 `head_content` 或 FBX。
5. 1201/1213 实锤：1201 `idle landingScale=1.3540467`、1213 为 `0.364421`。1213 头饰直接挂 1201 会被
   额外放大 `3.7156×`；1201 角色级倍率应为其倒数 `0.26913473`。1201 身体保持原体型，只缩放标准附件空间。
6. death、jump、run 等姿势包围盒高度不是角色身高。已标准化角色必须以 `idle` 固定所有动作的 `landingScale`，
   各动作只保留各自落点；否则死亡卧倒会把同一身体错误放大数倍。

## 固定排查顺序

1. 确认游戏实际加载的是新 prefab，而不是旧资源回退。
2. 检查 Role socket：唯一性、父骨、局部位置/轴向、所有动作一致性。
3. 检查附件 locator：握柄/头部中心、轴向、scale1；若有作者骨则做矩阵一致性比较。
4. 对跨 Role 异常，比较目标 Role 与标准 Role 的 `idle landingScale`，检查 `role_assembly_profile.json`，再比较
   `landingScale × attachmentSpaceScale` 是否回到同一标准附件世界尺度。
5. 检查同一 Role 所有动作是否共享 canonical `landingScale`；若只在 death/jump/run 跳变，优先判定姿势采样污染。
6. 使用正式运行时对齐器测 locator→socket 的位置和旋转误差。
7. 最后做正/背/左/右四方向和动作多时刻视觉验收。数值检查只能证明规则被执行，不能替代刀刃方向、穿掌、露头皮判断。

## 自动检查与导入闸门

- Art 项目运行「交付/检查全部模板与基准」，Role/Head/Weapon 必须全部 0 失败。
- Art 项目「交付/预览/打开角色部件装配预览」按主工程同一套 locator 公式装配，并可生成正背左右四方向截图；
  新资源在 Art 侧完成视觉验收后再交付，不能把主工程当第一个预览器。
- 主工程 `ImportPart` 导入后再次检查最终 prefab 的节点数量、父级、单位变换、绑定姿态补偿，以及
  `weapon_attach ↔ Bone_wq_r` 一致性。
- 第二道检查失败时，资源保留用于定位问题，但资产管理不会自动把它写入 `model_replacement.json`。
- 正式验证用例必须使用 `head_attach/head_mount` 与 `weapon_attach/rhand`，禁止再用“附件根清零”冒烟。
