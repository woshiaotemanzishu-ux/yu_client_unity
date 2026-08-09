# VIP 主页面 revision-v4 canonical 静态审计

v4 保留 v3 的全部 1347 节点、父子、类型、风险、控件清单与 1013 个叶状态，只修正配置证据权威链。规范化 route 前缀后拓扑差异为 0。

## 老 H5 实际配置消费链

1. `GameResPath.GetClientConfigPath/GetServerConfigPath` 生成 `resource/config/client|server/<name>.json`。
2. `ResManager.LoadConfigZip` 加载 `resource/config.zip`；`Config` 建立 PRELOAD 配置对象。
3. `ResManager.LoadGameConfig` 先查 `Config.PRELOAD_CLIENT_CONFIG/PRELOAD_SERVER_CONFIG`；存在时直接消费内存对象，不读取同名散文件；不存在时才读取 `cdn/resource/config` 散文件。
4. `GameScriptModule.ts`、`ResVersionManager/ResManager` 与 `laya2cdn.bat` 共同确定发布到 `cdn/resource` 的加载/打包链；`cdn/assets/resource/config` 是原始副本，不是本轮老 H5 的最终消费路径。

加载链证据：`h5/src/util/GameResPath.ts`、`h5/src/GameScriptModule.ts`、`h5/src/common/ResManager.ts`、`h5/src/common/Config.ts`、`h5/laya2cdn.bat`。

## canonical 配置证据

- `cdn/resource/config.zip`: `031984617dbcf27128265961b76014b7a4e68c7d047de46e6448ae4fcf2b3ac9`
- 内嵌 `config.json`: `aeea254274a99f9b092328d476bcae72b78fdbfe119700144e66ab4d0acca43a`
- PRELOAD `config_recharge_product`：95 项，canonical SHA `1dbb6c0ca8db235a741f858182d8a97728d1fcd9d05f93f015de23af754ff264`；type1 候选仅 product 2..8，type2 为 0。
- PRELOAD `config_recharge_return`：16 项，canonical SHA `c6a13d4f7902edc8660f91e1dbcaa127c1e63a503c66ed980add5f89b11328ea`。
- PRELOAD `ClientRechargeShow`：11 项，canonical SHA `e5087fd768de584f34ed2ee192d114e301819000eccb5ce918f3f53a0f12cdbb`。
- 非 PRELOAD `ClientVipPrivilege`：`c5cef232b28faf29b536a2ef18cc103ed19c04c9490d061cace8c3a2943c0dc1`。
- 非 PRELOAD `config_vip_card`：`0f17f7a6da10828bedbceac7336c93c39fb96579db374155cff9c8682058f73b`。
- 非 PRELOAD `config_vip_config`：`dfdb45285cfe664c05badcb0c250a5861c2d581be5f8939057596401066fbda6`。
- 非 PRELOAD `ClientVipWelfare`：`4f5ccb17d2e3877a2271624ca2836a17bf5b51d85c049abacd6082ef4c8d9182`，与 Unity `clientvipwelfare.json` 相同。

## 路线状态

- 7 个 type1 充值候选仅是配置候选；是否显示仍与 15800 有序快照取交集。15901 的 type2 商品保持动态模板，不伪造数量和 product_id。
- 所有充值、购买、领取、领奖、VIP 显示写入和平台支付叶均 `blocked`，未点击、未发送。
- 其余叶均 `needs-runtime-verify`；本轮没有启动 Unity/浏览器，静态编译、Prefab YAML 和台账校验不能代替同账号真实 H5/Unity Web。
