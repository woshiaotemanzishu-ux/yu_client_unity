# GodBeast 静态验证记录

验证日期：2026-08-09。范围仅为 GodBeast 文件岛和本目录的 output-only 工程；未启动 Unity、浏览器、WebGL 构建或账号写事务。

## schema 6

```text
generate_route.py: generated nodes=168 leaves=143 runtime=7 blocked=136
route_ledger.py init: schema=6 nodes=168 status={'not-run': 168}
route_ledger.py apply: success
route_ledger.py validate: nodes=168 status={'blocked': 161, 'needs-runtime-verify': 7}
```

正式账中没有 `done`；161 个父/叶节点为 `blocked`，7 个静态修复叶为 `needs-runtime-verify`。事务叶、缺主页面叶和真实 Web/Unity 叶没有被静态结果冒充完成。

## output-only 编译与审计

命令：

```powershell
dotnet build output/ui_route_audit/2026-08-09_god_beast/GodBeastStaticAudit.csproj -c Release -m:1
dotnet run --project output/ui_route_audit/2026-08-09_god_beast/GodBeastStaticAudit.csproj -c Release --no-build
```

结果：0 警告、0 错误；全部审计断言通过，`summary failures=0`。审计覆盖 Prefab YAML 头、七个现有子窗、主页面缺失边界、三个外层/三个内层 ScrollRect、转换占位、预览标题、全部 `m_Children` 引用、17300/01/02/08/09 读侧、17303/04/05/06/07/10/11/12 写侧缺口、七类配置缺口，以及 manifest/ledger 的 schema、拓扑、状态与 SHA 绑定。

## 差异门禁

```powershell
git diff --check -- Assets/Scripts/Module/Core/GodBeast Assets/Prefabs/UI/GodBeast output/ui_route_audit/2026-08-09_god_beast
```

结果：通过，无 whitespace error。没有执行全 Core、Unity 或 WebGL build。

## SHA-256

```text
319D86684FA858B65A9F881AB2C048ED2F6EF97D7F67F1B1103067B80873FEF0  Assets/Prefabs/UI/GodBeast/GodBeastModule.prefab
BB106CA8EAF0F794A76FE81CB1F9C90B8DFBCEAD4904E502E08DFD0FA8373453  Assets/Scripts/Module/Core/GodBeast/GodBeastController.cs
2F49689B433F405EC6D40508F23174471352ECE9CB442AAE51AB5EF4CD82FBE6  Assets/Scripts/Module/Core/GodBeast/GodBeastModel.cs
AA29A9C6619ED87827C69EFF25B61474604B65957D1A21A12172DFC9E122CD2A  output/ui_route_audit/2026-08-09_god_beast/route-manifest.json
1B0D293DFA3A5C7ED0D4F31D65E541FF64568C8521D5ED4F4DBF966CCE126E6C  output/ui_route_audit/2026-08-09_god_beast/route-ledger.json
C8C0954D8DF2026232BFA7CE226747F1C8363B54CE62B3A2EB3380BE26320ACC  output/ui_route_audit/2026-08-09_god_beast/results-static-boundary.json
61EE2414F7020D26B8BBD13CD2EF6939BF1A2E7F7688A95C746CF707154E3730  output/ui_route_audit/2026-08-09_god_beast/generate_route.py
DCC7F41A4F7F47D9C1C705ABA55E4B216A04B0326187CD85B9572EC343680594  output/ui_route_audit/2026-08-09_god_beast/Program.cs
```

注：`static-validation.md` 在记录这些哈希之后新增，因此不纳入上述自引用哈希表。
