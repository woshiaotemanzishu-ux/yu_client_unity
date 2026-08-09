# Armor 静态证据源

## 老端

- `E:/GitProject/yu_client/h5/src/equipArmor/EquipArmorView.ts`
- `E:/GitProject/yu_client/h5/src/equipArmor/ArmorAttrView.ts`
- `E:/GitProject/yu_client/h5/src/equipArmor/ArmorItem.ts`
- `E:/GitProject/yu_client/h5/src/equipArmor/ArmorTabItem.ts`
- `E:/GitProject/yu_client/h5/src/equipArmor/ArmorAttrItem.ts`
- `E:/GitProject/yu_client/h5/src/commonController/ArmorController.ts`
- `E:/GitProject/yu_client/h5/src/commonModel/ArmorModel.ts`
- `E:/GitProject/yu_client/cdn/resource/game/equipArmor/` 的五份布局 JSON/scene、atlas 与页面图片。

## Unity 可写岛

- `Assets/Scripts/Module/Core/Armor/ArmorController.cs`
- `Assets/Scripts/Module/Core/Armor/ArmorModel.cs`
- `Assets/Scripts/Module/Core/Armor/ArmorConfigs.cs`
- `Assets/Prefabs/UI/EquipArmor/EquipArmorModule.prefab`
- `Assets/Prefabs/UI/EquipArmor/ArmorAttrItem.prefab`

上述生产文件本轮均未修改。

## Unity 只读交叉

- `Assets/Scripts/Module/Core/Equip/EquipFlow.cs`
- `Assets/Scripts/Module/Core/Equip/Views/EquipArmorView.cs`
- `Assets/Scripts/Module/Core/Equip/Views/ArmorAttrView.cs`
- `Assets/Scripts/Generated/UI/EquipArmor/*.cs`
- `Assets/Scripts/Module/Core/Common/Views/BaseAwardItem.cs`
- `Assets/Scripts/Framework/UI/UIUtil.cs`
- `Assets/Editor/CliVerify/Cases/ArmorCase.cs`（历史静态/Editor 专项线索，不作为本轮运行证据）

## 配置最小闭包

- `Assets/GameRes/resource/config/server/config_armour_equipment.json`：90 行装备配置。
- `Assets/GameRes/resource/config/server/config_armour_suit.json`：18 行套装配置。
- `Assets/GameRes/resource/config/server/config_armour_kv.json`：2 行类型部位配置。

`static-verification.json` 保存 Armor Controller/Model/Configs、EquipArmorModule Prefab、老端 EquipArmorView、manifest/results 的 SHA-256。本轮没有 Player/catalog、真实 Web run、截图或账号状态证据。

