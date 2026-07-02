# 工单:OutWard 幻化外观最小闭环(坐骑阶星 + 坐骑/同修等级线)

日期:2026-07-02(第16轮侦察代理产出,主控审定)
目标:一套 OutWard 实现同时解开主线卡点 **#34(100330 坐骑1阶2星,ctype23)**、**#57(100521 剑魄同修升2级,ctype90 id=2)**、
以及后续 **100901(坐骑升2级,ctype90 id=1)**。

## 核心认知(侦察结论)

- 老端没有独立 MountController:坐骑/同修/翼影等统一走 **OutWard 框架**(OutWardController.ts,协议段 pt_160),
  按 `type_id` 参数化(1=坐骑 2=剑魄同修 3/4/5=翼影/圣器/神兵)。
- **两套并存的养成线**(同一对象身上):
  - 系统A(ctype23):阶/星/祝福 —— 16002 信息 + 16023 一键升星(坐骑/同修专线)
  - 系统B(ctype90):等级/经验/技能 —— 16028 面板 + 16029 升级(服务端模块标 @deprecated 但主线任务在用,必须实现)
- 注意:已有的 PartnerController(pt_142,14202/14205)是同修的另一套「companion 阶星」系统(主线 100190),与本工单 OutWard 并存,勿混。

## 任务条件(server 权威)

- 100330:ctype=23, id=1(阶), need_num=2(星) → 坐骑(MOUNT_ID=1)`Stage>1 或 (Stage==1 且 Star>=2)` 完成
- 100521:ctype=90, id=2(type_id=同修), need_num=2 → 同修系统B等级 ≥2 完成
- 100901:ctype=90, id=1(坐骑), need_num=2

## 协议(Proto.cs 常量已就位:OUTWARD_INFO=16002/OUTWARD_STAR_UP=16023/OUTWARD_LV_PANEL=16028/OUTWARD_LV_UP=16029)

字节序大端;c=u8, h=u16, i=u32, l=u64(NetReader:ReadU8/ReadU16/ReadU32/ReadU64)。

- **16002 请求** `"c"`(type_id);**回包**:`type_id:c, stage:c, star:h, blessing:i, figure_stage:c, combat:i, etime:l, auto_buy:c, attr_list[u16×{attr_id:c, attr_val:i}], skill_list[u16×{skill_id:i}]`
- **16023 请求** `"ccc"`(type_id, auto_buy=0, gold_type=0);**回包**:`errcode:i, type_id:c, stage:c, star:h, blessing:i, blessing_plus:i, etime:l, auto_buy:c, ratio_list[u16×{rate:c, rate_num:h}]`
  —— errcode==1 成功套 stage/star/blessing;失败显码 toast(错误码表未移植)。老端成功后另拉一次 16002 刷属性(照做)。
- **16028 请求** `"c"`(type_id);**回包**:`type_id:c, level:h, cur_exp:i, combat:i, attr_list[u16×{attr_id:c,attr_val:i}], skill_list[u16×{skill_id:i, skill_level:c}]`
- **16029 请求** `"c"`(type_id);**回包**:`errcode:i, type_id:c, level:h, cur_exp:i, add_exp:i, combat:i, skill_list[u16×{skill_id:i,skill_level:c}], ratio_list[u16×{rate:c,rate_num:h}]`
  —— errcode==1 套 level/cur_exp;升级成功老端会连点(一键连续升级),TEMP 壳单次即可。

老端锚点:OutWardController.ts:265-275(On16023)、:302-315(On16029:成功后 `REQUEST_PROTO 16002` 联动刷新)、
:436-443(升星发包:坐骑/同修走 16023 "ccc")、:475-478(升级发包 16029 "c")。

## 进游戏时机

EVT_GAME_START → 对 type_id 1 和 2 各发一次 16002 与 16028(共 4 包,对标老端登录拉取)。

## 配表(json 从 d:\git_res\yu_client\cdn\resource\config\server\ 复制到 Assets\GameRes\resource\config\server\;SYNC_LIST 已由主控加好)

- config_mount_constant.json / config_mount_stage.json / config_mount_level.json / config_mount_goods.json / config_mount_prop.json(具名键)
- config_mount_star.json(⚠数字键,列序:0=type_id,1=stage,2=star,3=max_blessing,4=attr,5=combat,6=clear_status;主键 "type@stage@star")
- 本轮最小闭环只需读:config_mount_star 的 max_blessing(显示"祝福 X/Y")与 config_mount_stage 的 max_star(可选);其余表复制落位即可不必解析。

## Unity 实现范围(新文件全在 Assets/Scripts/Module/Core/OutWard/)

1. `OutWardModel.cs`:OutWardVo{TypeId, Stage, Star, Blessing, Combat, Level, CurExp, LvCombat}(阶星与等级两组字段);
   Dictionary<int,OutWardVo>;Apply16002/Apply16023/Apply16028/Apply16029;Clear。
   可选 OutWardConfigs:读 config_mount_star 的 max_blessing(数字键按上面列序)。
2. `OutWardController.cs`:BaseController 单例(照抄 PartnerController 模式);Register 注册 4 协议 + EVT_GAME_START;
   公开 StarUp(typeId)(发 16023 "ccc" typeId,0,0)与 LvUp(typeId)(发 16029 "c");
   handler 严格按上面 schema 逐字段读,读完打日志(含 remaining),errcode!=1 → TipsManager.Toast("提升失败("+code+")")。
3. `OutWardShellView.cs`:TEMP 壳(照抄 PartnerShellView 模式:静态 Show/Close,Window 层,代码建 uGUI,样式从简):
   标题「坐骑/同修培养」;两行(坐骑/同修),每行:名字(坐骑/剑魄同修 硬字面即可——这是系统名非配置数据)+
   「N阶M星 祝福X」+「等级L(经验E)」+ 按钮[升星](StarUp)与[升级](LvUp);无数据行显示「等待 16002/16028(需活服)」。
4. CLI 用例:新文件 `Assets/Editor/CliVerify/Cases/OutWardCase.cs`(namespace Shenxiao.EditorTools;
   `public static class OutWardCase { public static async Task<int> Run() {...} }`):
   复用 CliVerify.Stage/CliVerify.Pkt/CliVerify.FindDeep(已 public);
   合成包:16002(type1 stage1 star1 blessing5 …attr/skill 计数0)→断言 Model;16023 成功包(star2)→断言升星;
   16029 成功包(type2 level2)→断言等级;16023 失败包 errcode!=1 →不抛异常;
   Stage.Create + Show 壳 →截图 Temp/round16_outward_shell.png →断言「1阶2星」文本与 Btn升星 存在。
   ⚠不要改 CliVerify.cs 本体(主控统一接 RenderAll)。

## 接线(主控做,代理勿碰)

ControllerHub 注册、TaskModel.DoTask ctype23/90 分支与 UNPORTED_TIP_SYSTEM 移除、CliVerify.RenderAll 挂用例。

## 交付要求

- 只新增:Assets/Scripts/Module/Core/OutWard/ 三文件 + Assets/Editor/CliVerify/Cases/OutWardCase.cs + 配表 json 复制。
- 新 .cs 需手动补进对应 csproj(Shenxiao.Module.Core.csproj / Shenxiao.Editor.csproj,照 TaskModel.cs 的 Compile Include 格式插入)。
- `dotnet build yu_client_unity.slnx -v:minimal` 0 错;不跑 Unity。
- 红线:不造假数据(合成包=测试夹具);错误显码降级;字段序逐字段照本工单。
