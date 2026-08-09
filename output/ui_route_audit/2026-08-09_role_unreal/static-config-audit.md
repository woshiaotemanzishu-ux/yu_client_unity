# 九霄冥饰配置闭包静态审计（2026-08-09）

- 范围：只读配置同步与类型化访问器；没有打开 Unity、没有操作浏览器、没有发送账号写事务。
- Unity 实现：`Assets/Scripts/Module/Core/Unreal/UnrealConfigs.cs`，SHA-256 `B41D7868BE42D9618007EC0D158E59FDCB9E11283B4AC8FFAD5E03EB635C72DD`。
- 同步入口：`Assets/Editor/ConfigGenerator/ClientConfigSync.cs` 已登记九张 decoration/decompose 表及 `GoodsSubtype`。该文件同时包含并发中的成就同步改动，因此只按新增表项守界，不把整文件哈希当作 Unreal 独占证据。
- 编译：`UnrealConfigs.Isolated.csproj` 把新增文件连同现役 Framework/Core/Unity/Newtonsoft 引用独立编译，0 warning / 0 error；`UnrealCase.Isolated.csproj` 用最小 CliVerify stub 编译新增配置专项，0 warning / 0 error；`dotnet build Shenxiao.Module.Core.csproj --no-restore` 对当前已登记 Core 源也为 0 warning / 0 error。Unity 尚未获准刷新生成 `.csproj`，所以后者不能单独证明新增文件已进入 Unity 工程列表。
- 边界：`14901/14902/14903/14905` 仍维持 `Schemas/ProtocolCoverage/hard_negative_constraints.json` 的事务阻断；Unreal 目录没有新增这些协议的 sender、handler 或 Proto 常量。

## 权威源闭包

| 配置 | 行数 | SHA-256 |
| --- | ---: | --- |
| config_decoration_kv | 1 | `507FEB1DB0A7C0211E979F984A4D7AF9DFE2C967A213EB48654F211AB76A4EBA` |
| config_decoration_attr | 498 | `1BC0124EBE6A72457AEBBBD96271BB066A10B8F541D7F8B406BE68FFED00A60E` |
| config_decoration_level_max | 282 | `7DDB6CC57B083E85638467A01B5F9C7FD2E438E0237928FF4436592BA383A7B5` |
| config_decoration_level | 606 | `8F6852AE5212CEBE1B6AB4178EB731677B3869FC732D5301A8601B390D7F7050` |
| config_decoration_stage | 330 | `EB9B6590430AB2F23AF6212277345CBA62B231B421DC7B15D4F410ED3937508A` |
| config_decoration_stage_max | 2 | `E599E5ECB482058C95FACA5A223D2F4AE280928170FCDF2B16C96936E4B52539` |
| config_dec_unlock_cell | 6 | `2F9C9EE56E8DB43E2AB639AF331AA49CC1313BC7FDDEC6AE7DDC0BBFBE546336` |
| config_goods_decompose | 2449 | `9228A7F2F4EDFAD14DF8B1CA4DE58119F5E120B738F997E2E39573D70E104E06` |
| config_soul_attr_num | 4322 | `79540843717453E15036D78926B1D509E07AC116B0E867C15657FEA1D6F77E11` |
| GoodsSubtype | 434 | `760448A5BAFBB66F46D9A31BCE40BC52A6471044D75636B4E4B934E9FB3B3DEE` |

固定事实：背包容量为 100；解锁表正好六行；`GoodsSubtype` 的 type=55 正好映射 `1:冥面、2:灵玉、3:腰环、4:耳坠、5:符令、6:道印`。访问器覆盖背包容量、六部位名称/解锁、强化等级/上限/材料、进阶/上限/材料、属性原文、分解返还和属性数量表。

## 尚未完成

- 当前资源目录尚未生成这十份同步产物；需要在获准操作 Unity 后执行项目既有的 `ClientConfigSync`，再构建 Addressables 内容。静态登记不等于运行时资源已可加载。
- `Assembly-CSharp-Editor.csproj` 的现存生成清单尚未包含本轮新增的 `MedalConfigs.cs`/`UnrealConfigs.cs`，因此其命令行编译报类型缺失；本轮没有为消掉该环境错误而手改 Unity 生成工程文件。
- 当前老 H5 账号 111111 为 260 级，而入口条件为 360 级；`old_readonly_v4/blocked.json` 已证明人物页 `_Group6` 隐藏。
- Unity 缺 `SecretTreasureMainView` 五页签共享容器和四个可编辑 Unreal Prefab。首次落地必须走 convert-module，不能用独立窗口冒充秘宝页签。
- 所有节点仍保持 `blocked`/`not-run`；本报告不把配置编译通过抬成真实 Web、Unity runtime 或页面 `done`。
