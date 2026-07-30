# Shenxiao 登录链路 — yu_client → yu_gm → yu_server(2026-06-11 从源码核对)

> L.5 端到端试点的实现依据。每个环节都标了源码出处,实现时以源码为准,本文是导航。

## 链路全貌

```
① 平台配置 cfg ─────── cdn/platform/config_jzy_sh921_test_P0010642.cfg(测试环境)
     url_account_path = http://223.109.142.26:88/
     login_php        = api/            → API 基址 = http://223.109.142.26:88/api/
     ResUrl(CDN)、url_cdn_path 等也在此文件
② HTTP 账号接口 ────── yu_gm app/api/controller/Index.php(ThinkPHP,GET + ?method= 分发)
③ 服务器列表/连接信息 ── 同上(数据源 = yu_gm 的 admin_server 表)
④ WebSocket 游戏服 ──── yu_client UserMsgAdapter(BIG_ENDIAN 二进制)
⑤ 游戏服协议 10000/10003/10004 → 角色列表 → 创角/进游戏 → GAME_START
```

## ② HTTP 签名与接口

- **签名**:`sign = md5(login_key + time + method).toLowerCase()`
  - 客户端:`PlatformManager.ts:810`,`login_key` 常量在 `LoginModel.ts:23`(`LoginModel.LoginKey`)
  - 服务端:`yu_gm app/api/controller/Index.php:20`(`LOGIN_KEY`,与客户端同值,已互验)
- **接口一览**(`Index.php index()` 按 `?method=` 分发,全部 GET,`wallet_buy` 为 POST):

| method | 处理函数 | 用途 / 关键返回 |
|---|---|---|
| `player_login` | `playerLogin()` | 入参 `accname`;**账号不存在自动注册**;返回 `id`(player_id)、`token`、`last`(上次服)、`server/areas/recommend`(服务器列表)、`player_server`(玩家登录过的服) |
| `player_register` | `playerRegister()` | 显式注册 |
| `player_check_login` | `playerCheckLogin()` | 凭据校验 |
| `get_server_list` | `getServerList()` | 服务器列表(数据源 `admin_server` 表) |
| `get_server_info` | `getServerInfo()` | 入参 `player_id, sid`;**返回 `host`/`port`/`sslport`** → 客户端写进 `AppConst.SocketAddress/Port`(`LoginManager.ts:150-152`) |
| `player_server` | `playerServer()` | 进服后回写角色摘要(REPLACE INTO player_server) |
| `last_login_server_update` | `lastLoginServerUpdate()` | 记录上次登录服 |

## ④⑤ WebSocket 与游戏服协议

- 连接:`UserMsgAdapter.InitSocket()`(`UserMsgAdapter.ts:165`),`ws://{host}:{port}`,
  字节序 BIG_ENDIAN;`onSocketOpen` → 事件 `GAME_CONNECT`。
- 协议发送集中在 `LoginController.ts`(`SendFmtToGame(协议号, 格式串, ...)`):

| 协议 | 格式 | 时机 / 内容 |
|---|---|---|
| `10000` | `iiss` | GAME_CONNECT 后立刻发:`pid, time_stamp, account_id, plat_name`。**踩坑(2026-06-12)**:account_id/time_stamp/pid 必须用 `get_server_info` 下发的 `accname`/`time`/`pid`(LoginManager PLAYER_SERVER_INFO),游戏服按 accname 认账号——发 GM 的 player_id 会被当成另一个空账号,满角色的号也会落进创角页 |
| (回包) | — | 角色列表 → 进 LoginSelectRoleView / 无角色进 LoginCreateRoleView |
| `10003` | `cccsslsscscc` | 创角:career, sex, role_name, plat_name, inviter_id, ...；失败结果必须以玩家可见 Toast 展示，不能只写 Console |
| `10004` | `lsisisscscsh` | 选角进游戏:role_id, ... → 成功后事件 `GAME_START` |
| `10006` | —— | 心跳/确认类(LoginController.ts:363) |
| `10007` | `s` | 随机角色名展示前的服务器权威校验；回包 `c`：1成功、3重名、4非法字符、5长度、6已有角色、7敏感词。名字类失败静默换名，其他失败提示玩家 |

- `10006` 心跳节奏必须按老客户端 `LoginController.On10006` 复刻: `10000` 回包后先发一次,
  收到 `10006` 回包后取消旧延迟并重新排一个 5 秒后发送的任务。不要在 `NetManager.Pump()`
  里从连接时刻开始固定周期盲发,否则会和 `10000` 后的首次心跳叠加,导致过早/重复心跳。

- 协议格式串解析与收发:Unity 侧已有 `ErlangParser / UserMsgAdapter / NetManager / Proto` 骨架,
  格式串语义照抄 yu_client(编码规范 3.3:协议照抄,不改服务端)。

## yu_gm ↔ yu_server 的关系

- `admin_server` 表 = 服务器注册表(sid/host/port/区服状态),登录链路只读它。
- `app/api/controller/ServerApi.php` 是运维面(start/stop/restart/hotUpdate Erlang 节点),
  与登录链路无关,不用动。
- 游戏服对 10000 的账号校验:account 库为 yu_gm 与 yu_server 共享数据源
  (验证细节在 yu_server,试点联调时再核对)。

## Unity 实现状态(2026-06-11 已落码)

- **线协议(逐字节对标 Laya)**:发 `[i16 总长][i16 1000][i16 cmd][字段]`,
  收 `[u32 总长][u16 cmd][u8 压缩标记][载荷]`;格式字符 c/C/h/H/i/I/l/L/s。
  实现:`Framework/Net/UserMsgAdapter.cs`(编码)、`NetReader.cs`(解码)、
  `NetManager.cs`(连接/拆帧/主线程泵/心跳)。处理器签名 `Handler(NetReader)`,
  用法与 Laya 一致:`reader.ReadFmt("clihi")`。
- **登录链**:`LoginController.DevLoginAsync`(player_login 自动注册)→
  `SelectServerAsync` → `ResolveSelectedServerEndpointAsync`(get_server_info)→
  `ConnectGameAsync`(ws 连接 + 心跳 + 发 10000"iiss")→ `OnAccountLogin`
  解析角色列表头 → `EVT_GAME_ROLE_LIST`。
- **冒烟开关**:AppConfig.asset 勾 `autoLoginSmokeTest`,Play 后自动跑全链,
  Console 看 ①②③④ 步日志,终点是"✅ 登录链全通"。
- **配置驱动**:环境地址不进代码——菜单 `神霄/配置/从 yu_client 平台cfg 导入登录环境`
  把 `cdn/platform/*.cfg` 的 url_account_path+login_php 写进 AppConfig;
  心跳间隔、devAccount 同在 AppConfig。
- 待办:10000 回包的 FigureProtoVo 外观块解析(选角 UI 阶段)、断线重连策略、wss。

## 对 Unity 实现的直接结论(M1-M4 映射)

1. **M2 登录**:`HttpUtil.GetAsync` + 上表接口即可,`GmApi.cs` 已是现成的 HTTP 调用范例;
   先实现 `player_login`(自动注册,测试最顺)→ 不需要单独问账号,**随便起个 accname 就能跑**。
2. **M3 选服**:`player_login` 返回里已带完整服务器列表,首版可不单独调 `get_server_list`。
3. **M4 连接**:`get_server_info` 拿 host/port → WebSocket → 发 `10000` → 等角色列表回包。
4. 测试环境入口写死取 `config_jzy_sh921_test_P0010642.cfg` 的值,做成 AppConfig 字段,
   不在代码里拼地址(编码规范:Addressable key/路径不硬编码同理)。

## 创角/选角阶段的加载链(2026-06-11 查证)

- **2D 背景分阶段**(LoginBgView.ts):登录/踏入仙界 = `denglu_bg.jpg`(龙图);
  进创角/选角(OPEN_VIEW 触发 `ChangeRoleStatus`)= 换 `ui_Login_bg2.jpg`(樱花底)
  并**隐藏版权块**;回登录态 `UpdateView` 恢复。Unity 已按此实现。
- **创角/选角的樱花树/石台/角色** = **3D 展示链**:
  `ResManager.SetRoleModel(this, _gp_model_con, show_model_data)`,
  UI_MODEL_TYPE.ROLE + clothe/weapon/wing res(即 model_clothe_*.lh 那套)+ 环境场景。
  归 .lh/.lm/.lani 3D 转换线,2D 流水线不伪造。
- **创角职业头像**:ConfigLogin.CreateRole.UI(select_icon/unselect_icon,
  选中底图 ui_Login_02/未选 ui_Login_03),**左侧竖排** item.SetPosition(0, career*133)。
- 加载页背景按 ConfigLoadingBgTime 条件表选 load_bg{id}(等级/开服天数/星期等),
  Unity 当前取第一张为编辑器默认,接配表线后按表选。

## 创角/选角页表现对齐(2026-06-11 二次查证,老客户端为准)

- **分流**(On10000):role_num==0 → 创角页;>0 → 选角页。两页可互达:
  创角页返回 → 有角色回**选角页**、无角色断线回踏入仙界页;选角页空槽点击 → 创角页。
- **选角页**(LoginSelectRoleView/Item.ts):固定 `SelectRole.TotalCount`(4)个槽位,
  角色按 role_id 升序;**空槽=创建角色入口**(只显 `ui_Login_04` 底图,bg2/名字/等级全藏);
  角色槽选中态 `ui_Login_02`+`ui_Login_05`、未选 `ui_Login_03`+`ui_Login_06`;
  等级>370 显「升仙」角标 `_img_sc` 且等级显示 level-370;行位 (50, index*136);
  默认选中**上次登录角色**(cookie LAST_LOGIN_ROLE_ID ≈ Prefs login.lastRoleId);
  模型=选中角色形象 + idle(Unity 暂为职业默认装,形象线 TODO)。
- **创角页**(LoginCreateRoleView.ts):进入按 `random_weight` **加权随机预选职业** +
  自动随机名(ConfigRandomName 姓+性别名);右侧三张职业介绍图 img1/2/3
  (login/other/ 下,随职业切换);模型动作=ConfigModelAni 的 create2→create3。
- **随机名不能只在本地拼接后直接创角**：老端每次生成候选名后先发 `10007`，失败自动继续随机，
  最多 10 次。服务端敏感词包含运行时数据库 `config_word`，客户端配置无法可靠预判；Unity 必须保留
  这道服务器校验。手动输入名仍由最终 `10003` 裁决，但所有失败码都必须弹出玩家可见提示。
- 以上配置全部运行时读(LoginConfigs),编辑器菜单「神霄/配表/同步客户端配置(JSON)」
  负责把 JSON 从 yu_client 带进 GameRes。
- 未对齐(记账):入场 Tween(条目飞入/按钮浮入)、角色头像 CustomHeadItem、
  创角音效+骨骼特效、选角页形象线换装、创角视频(LoginCreatRoleVideoView)。

## 连接等待、超时与玩家反馈（2026-07-28）

- 老客户端在 `START_GAME_CONNECT` 时给 `WaitforOpenViewLoading` 注册 loading 源，收到
  `GAME_CONNECT` 后才撤销；等待层延迟 `0.15s` 显形、旋转圈 `2s` 一圈、单源 `15s` 过期。
  Unity 的 `WaitforOpenViewLoading` 保留相同时序，由“重构UI 生成器 → Login”生成独立 Prefab，
  不再依赖 Laya 转换器生成的 `WaitforOpenViewLoadingBind`，也不得重烤现有登录页面。
- Unity 连接等待覆盖三个阶段：`get_server_info` 获取入口、WebSocket 建连并发送 `10000`、等待
  `10000` 角色列表回包。阶段切换使用同一个 loading source 刷新过期时间；角色列表到达、失败、
  超时、主动返回和登录模块退役都必须移除该 source，禁止遗留常驻转圈。
- HTTP 入口解析、WebSocket 建连、`10000` 回包各自以 `15s` 为失败边界；连接任务超时后必须
  主动 `DisconnectAsync` 取消底层 socket，不能只把 UI 撤掉后让旧任务继续占用连接。
- 玩家反馈不能只写 Console 或短 Toast：入口失败、连接失败、等待角色数据超时、等待期间断线，
  均显示“是否重新连接”的确认弹窗；详细异常只写日志，弹窗使用可理解的网络提示。新等待 Prefab
  尚未生成时只降级为文字 Toast，不得阻断登录页其余功能。

## 运营公告 10207 与登录/游戏内消费（2026-07-30）

- `10207` 只有S2C `type:u8`，没有C2S；PHP/GM广播中非0表示重新检查运营公告。Unity常驻注册并完整读尾，但绝不主动发送或加入GAME_START。
- 公告根地址来自平台cfg的 `url_cdn_path`，不是Addressables/资源CDN。先取 `login_notice/jzy/login_notice.json.cfg.v`，只有版本变化或当前内存没有正文时再取 `.json.cfg`；正文解析和结构校验成功后才同时提交版本与快照，网络/JSON失败保留旧数据。`BootConfig`可部署覆盖 `noticeCdnBaseUrl/platName/platBelong`。
- 正文只按 `belong[plat_belong]` 的逗号ID顺序展开，再按公告 `source` 是否为 `plat_name` 前缀及服务器时间窗筛选；内容段严格以 `open_login/open_inside` 分流。平台没有belong项是合法的已加载空快照，不回退到其他平台公告。
- 登录公告在账号就绪后的ServerEnter页异步初查并提供手动“公告”入口。自动弹出规则为：新账号遇 `new_reg=0` 不弹；`show_rule=1` 新版本一次、2每日首次、3每次账号会话、4不弹。窗口复用真实 `GameNoticeModule`，正文按 `\n\n##` 分段显示。
- 游戏内公告红点以正文MD5为键，持久化根按角色ID隔离：规则1/2在新公告或正文变化时置红，3每次主动选角登录重置，4每天首次重置且未读状态会延续；自动重连不重新开始角色会话。标题项根透明Image是唯一点击面，选择后落已读并刷新聚合红点。
- `WelfareView`外壳尚未迁移，417入口目前受控直达已完成的游戏公告子页；417红点为“公告未读或既有福利可领”。这只是外壳边界，后续嵌回福利页时不得改公告模型或协议语义。10211属于独立配置弹窗链，不能并入10207。
