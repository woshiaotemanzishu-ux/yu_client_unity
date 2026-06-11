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
| `10000` | `iiss` | GAME_CONNECT 后立刻发:`pid, time_stamp, account_id, plat_name`(账号登录游戏服) |
| (回包) | — | 角色列表 → 进 LoginSelectRoleView / 无角色进 LoginCreateRoleView |
| `10003` | `cccsslsscscc` | 创角:career, sex, role_name, plat_name, inviter_id, ... |
| `10004` | `lsisisscscsh` | 选角进游戏:role_id, ... → 成功后事件 `GAME_START` |
| `10006` | —— | 心跳/确认类(LoginController.ts:363) |

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

- **2D 背景**:登录全程只有一张 `scene/dragonBones/denglu/denglu_bg.jpg`(龙图,
  LoginBgView 三个分支同源;denglu_bg1.jpg 无代码引用)。Unity 已对齐。
- **创角/选角的樱花树/石台/角色** = **3D 展示链**:
  `ResManager.SetRoleModel(this, _gp_model_con, show_model_data)`,
  UI_MODEL_TYPE.ROLE + clothe/weapon/wing res(即 model_clothe_*.lh 那套)+ 环境场景。
  归 .lh/.lm/.lani 3D 转换线,2D 流水线不伪造。
- **创角职业头像**:ConfigLogin.CreateRole.UI(select_icon/unselect_icon,
  选中底图 ui_Login_02/未选 ui_Login_03),**左侧竖排** item.SetPosition(0, career*133)。
- 加载页背景按 ConfigLoadingBgTime 条件表选 load_bg{id}(等级/开服天数/星期等),
  Unity 当前取第一张为编辑器默认,接配表线后按表选。
