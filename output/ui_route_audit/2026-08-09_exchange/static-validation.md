# ExchangeGift 静态验证结果

## 结果

- `dotnet build ExchangeStaticAudit.csproj -c Release -m:1`：通过，0 warning / 0 error（output-only，目标 `net10.0`）。
- `dotnet run --project ExchangeStaticAudit.csproj -c Release --no-build`：`EXCHANGE_STATIC_AUDIT PASS checks=28`。
- `route_ledger.py init`：schema 6，16 节点。
- `route_ledger.py apply`：成功，正式账由通用原子入口更新。
- `route_ledger.py validate`：通过，状态汇总 `blocked=11 / needs-runtime-verify=5`。
- `git diff --check`：通过（Exchange C# 与 Prefab 定向检查）。

## 静态指纹

- Git HEAD：`92b3f5578a90befb85a6255157f08e482214aa1a`
- `ExchangeGiftView.cs`：`cff3a1144ea681288a016089c1b19b61f2cec9e1dad362f16ec5e0039b5c2a79`
- `ExchangeModule.prefab`：`2afa55e54001d3f60c5675fef197d3ab4ad3903ffeb09c492162ec3aabd46767`
- `route-ledger.json`：`ccf79f6c00e49970872185267956fcda377ea3fa816567ad17e9237a289679f4`
- 老端 `ExchangeGiftView.ts`：`5434e2adee943fed2f241ef666af073b1fc73391c76bbb32557a9025bdc369e7`
- 老端 `ExchangeGiftView.scene`：`b2cd70efb92cabe4d29d189964e97c476247dbd1f297168fa17466c6c0676ac2`

## 验证边界

这次验证只证明 C# 在独立桩环境可编译、Exchange View 明确不接入 15087、Prefab 文本与 Bind 关键项存在、schema 6 结构和状态合法。它不证明 Unity 真编译、Prefab 真正出帧、WebGL 输入、15087 请求/回包、奖励弹窗、错误码、关闭重开、视觉 diff 或性能通过。
