# BaseWindowSkin 共享修复静态结果

## 已确认的老端语义

- `BaseView1.ts` 在移动端为 `BaseWindowComponent` 创建 `full_screen_bg1.jpg`，高度跟随舞台、宽度保持 16:9。
- `PopManager.popViewOpen` 打开新大窗时先关闭当前大窗；`popViewClocse` 只清空当前引用，不恢复历史窗口。

## 本轮修改

- `BaseWindowSkin.prefab` 根节点拉伸到父级，首子节点 `AdaptiveBackground` 直接持有同源背景，使用 `AspectRatioFitter.EnvelopeParent`；背景拦截空白区点击。
- `_root_gp` 保持 870×1280，业务内容和现有 `_img_bg` 坐标未改。
- `BaseWindowManager` 在共享 `BaseWindowSkinView` 生命周期内维护唯一当前窗口；新窗隐藏旧窗，当前窗关闭或销毁时清空引用。
- 静态消费者盘点命中 14 个 Flow：Baby、Bag、Composite、Daily、Designation、Equip、Fashion、GodBefall、Guild、Pet、PetEquip、Resonance、Role、Shop。

## 已执行门禁

- `dotnet build Shenxiao.Module.Core.csproj --no-restore`：0 error，99 个既有 warning。
- `dotnet build Shenxiao.Editor.csproj --no-restore`：0 error，2 个既有 warning。
- `git diff --check`：通过。
- `PrefabBackgroundOwnershipCase` 已加入 Prefab 结构和双窗替换断言，但为保护用户当前 Unity 前台，本轮未启动 CLI Unity 执行该用例。

## 待真实运行

- 720×1280 与宽屏下背景覆盖、裁切和业务主体不拉伸。
- 背包→角色→关闭：只显示角色，关闭一次回主场景。
- 角色→背包→关闭：只显示背包，关闭一次回主场景。
- 两条路径的 cold/warm 重开、主 UI 恢复和玩家可见像素。

当前专项状态：`needs-runtime-verify`。
