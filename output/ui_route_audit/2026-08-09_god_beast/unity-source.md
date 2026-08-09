# GodBeast Unity 静态事实源与增量修复

## 现有实现

- 可编辑 Prefab：`Assets/Prefabs/UI/GodBeast/GodBeastModule.prefab`。
- 模块代码只有 `GodBeastController.cs` 与 `GodBeastModel.cs`；Controller 当前显式为读侧，仅注册/处理 17300、17301、17302、17308、17309。
- Prefab 保存 `GodBeastBagView`、`GodBeastComView`、`GodBeastSelectView`、`GodBeastSkillView`、`GodBeastStrView`、`GodBeastStrenView`、`GodBeastTipsView` 七个子窗。
- Prefab 不含主 `GodBeastView`，也不含主页面所需 `GodBeastItem/GodBeastEquipItem/GodBeastAttrItem/GodBeastSkillItem` 模板；模块内没有业务 View/Flow，也没有加载该模块的当前 C# 消费者。
- Unity 配置闭包缺 `config_eudemons_item/equip_pos/equip_attr/strength/compose/cfg` 与 `ConfigGodBeast`；仅发现不等价的 `config_eudemons_boss_cfg.json`。

## fix-view 增量修复

- 禁用三个只用于裁切外壳、却重复消费滚轮/拖拽的外层 `ScrollRect`：
  - `GodBeastStrView/scroller`，component fileID `161477009195818581`；内层 `_list` 保持启用。
  - `GodBeastComView/_Scroller1`，component fileID `4885670100450534145`；内层 `bagGp` 保持启用。
  - `GodBeastSelectView/_Scroller1`，component fileID `1024554220334860621`；内层 `_group_item` 保持启用。
- 清除转换占位文案：`tipsLb=aaa`、`curAttr/nextAttr=aa`、`lv_txt=htmlText`。
- 将铸造目标预览标题从“遗骸装预览”修正为老端当前运行时覆写文本“遗骸预览”。

这些项仍是静态确定的 Prefab 修正；触摸拖拽、遮罩裁切、短长文案排版、两档 viewport 和真实像素均保留 `needs-runtime-verify`。

## 阻断项

- 主页面 Prefab/Bind/业务链/列表模板整块缺失。因为已存在 GodBeast 子窗 Prefab，本轮按 `fix-view` 只增量修复；没有跨岛调用转换器或虚构主页面。
- 17303/04/05/06/07/10/11/12 的 UI 写侧控制器、Model 派生状态和即时刷新链缺失。
- 共享 `BeastToolTips`、`PetSkillView`、Halo、Composite、Boss 跳转、BaseWindow 和入口不在本岛，均未修改。
- 未启动 Unity、浏览器或账号事务，故没有 Player/catalog、真实 Web、cold/warm、状态矩阵、弹窗身份、列表末项可达、成功回包/即时刷新/重开一致性证据。
