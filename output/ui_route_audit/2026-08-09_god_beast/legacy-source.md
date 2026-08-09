# GodBeast 老端静态事实源

本记录只固定当前老 H5 源码可静态确认的路线语义；未启动浏览器，不能替代同账号、同状态、同 viewport 的真实运行结果。

## 入口与开放条件

- `SecretTreasure` 当前页签顺序为 Rune、MonBook、Lung、GodBeast、Unreal；GodBeast 是第 4 个逻辑页签，当前标题为“荒祖遗骸”。
- 外层 `SecretTreasure` 开放条件为等级 48、任务 100970；`GodBeastView` 为等级 320、开服第 8 天；`GodBeastCompositeView` 为等级 350、开服第 6 天。
- 老端主实现位于 `E:/GitProject/yu_client/h5/src/godBeast/`，协议控制器位于 `E:/GitProject/yu_client/h5/src/commonController/GodBeastController.ts`。

## 当前页面和弹窗

- `GodBeastView.ts`：幻兽横向列表、当前/默认/红点状态、属性区、5 个装备位、技能区、出战/助战计数、扩位、全部卸下、背包、召回、出战、置灰、快速装备、铸造。
- `GodBeastBagView.ts`：背包滚动列表及每格 `BeastToolTips`。
- `GodBeastSelectView.ts`：按部位选择装备、已装备/未装备状态、穿戴/替换/卸下详情动作。
- `GodBeastComView.ts`：4 个同品质同星级材料位、目标预览、背包、品质下拉、单次铸造、Halo 自动铸造、成功奖励弹窗及 `ui_shenyaohecheng` 特效。
- `GodBeastStrView.ts`：已穿遗骸横向列表、材料 39510000、强化一次/十次、当前/下级属性、进度、成功演出。
- `GodBeastTipsView.ts`：扩充助战位所需道具、等级/上限条件、确认/返回。
- `GodBeastSkillItem.ts` 当前打开共享 `PetSkillView`；`GodBeastStrenView.ts` 的实现已全部注释，不属于当前功能线。

## 事务协议边界

- 读侧：17301 总览、17302 更新、17308 强化预览、17309 属性战力；17300 为错误回包。
- 写侧：17303 穿戴/替换、17304 卸下（`pos=0` 为全部）、17305 出战/召回、17306 扩位、17310 铸造、17311 新强化、17312 快速装备；17307 为旧强化语义。
- 本轮禁止账号写事务，因此所有上述写侧叶均为 `blocked`，没有把“能发包”或静态代码存在当作成功证据。

## 共享依赖

`EquipmentItem`、`BaseAwardItem`、`BeastToolTips`、`DownDropBtn`、`Alert`、`CongratulationObtainView`、`PetSkillView`、Halo privilege 6/51402、`Composite/GodBeast`、跨服/幻兽 Boss、`SecretTreasure/BaseWindow`、`GiftPush` 均在本文件岛之外，只登记依赖和运行态门禁，不修改。
