# Composite 静态证据源

## 老端权威源

- 主路由与页签：`E:/GitProject/yu_client/h5/src/composite/CompositeView.ts`。
- 通用控制器与协议：老端 Composite controller/model 调用链；15020 为合成请求，15028 为规则合成，15019 为拆解，符文分支可能先走 16711。
- 页面目录：`E:/GitProject/yu_client/h5/src/composite/`。
- 御府材料选择：`E:/GitProject/yu_client/h5/src/godCourt/GodCourtComView.ts`、`GCComSelectView.ts`。
- red 条件路线：`E:/GitProject/yu_client/h5/src/redEquip/RedEnterView.ts`、`CompositeEquipView.ts`、`CompositeRankView.ts`、`CompositeEquipResolveView.ts`。
- 菜单配置：`E:/GitProject/yu_client/cdn/resource/config/server/config_compose_menu.json`。

## Unity 当前源

- 页面 Flow/Bootstrap/业务 View：`Assets/Scripts/Module/Core/Composite/`。
- 可编辑 Prefab：`Assets/Prefabs/UI/Composite/`。
- Generated Bind：`Assets/Scripts/Generated/UI/Composite/`，全程只读。
- `CompositeModule.prefab` 内静态扫描到 963 个 `m_Name` 节点；具体页面存在性由独立验证器复核。

## 证据绑定

`static-verification.json` 保存本批关键源 SHA-256：

- `CompositeFlow.cs`
- `CompositeModule.prefab`
- 老端 `CompositeView.ts`
- `config_compose_menu.json`
- `route-manifest.json`
- `static-results.json`

本批未产生 Player/catalog/真实 Web run 或截图，因此台账没有任何 `done` 或运行态闸证据。
