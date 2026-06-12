# 协议架构(yu_server ⇄ 客户端,2026-06-12 查证)

进入游戏前的准备文档:帧格式、协议分段、路由、进游戏推送链、心跳/重连约束,
以及 Unity 侧的承接设计。所有事实均出自 yu_server / yu_client 源码(标注文件位置)。

## 1. 帧格式(已在 Unity 实现并跑通)

- 收发均大端序(BIG_ENDIAN)。
- **服务端 → 客户端**:`[u32 总长][u16 cmd][u8 压缩位][payload]`,总长 = payload + 7;
  压缩位恒 0(pt.erl:360 压缩逻辑被注释)。
- **客户端 → 服务端**:`[u32 总长][u16 cmd][u8 压缩位=0][payload]`(UserMsgAdapter.ts:491)。
- 基础类型:c=u8 / C=i8 / h=u16 / H=i16 / i=u32 / I=i32 / l=u64 / L=i64 /
  s=u16 长度+UTF8;数组=u16 计数+元素;浮点=整数×1000(pt:read_float,3 位精度);
  语音=u32 长度+bytes。figure 复合块见 `FigureProto.cs`(已对齐 pt:write_figure)。
- Unity 对应物:`NetManager`(分帧/收发/心跳)、`NetReader`(顺序读+ReadFmt)、
  `UserMsgAdapter.cs`(帧编码)、`BaseController`(注册回调)。

## 2. 协议号分段(yu_server src/pt/pt_*.erl)

路由:`mod_server.erl:635 routing/3` 取协议号前三位(百位)分发到 pp_* 模块。
**协议号体系不可改**,Unity 必须原样使用。

| 段 | pt 文件 | 模块 | 段 | pt 文件 | 模块 |
|---|---|---|---|---|---|
| 100xx | pt_100 | 注册登录(已接) | 134xx | pt_134 | 勋章 |
| 102xx | pt_102 | 游戏控制 | 135xx | pt_135 | 九魂圣殿 |
| 110xx | pt_110 | 聊天 | 137xx | pt_137 | 钻石大战 |
| 111xx | pt_111 | GM 秘籍(已接) | 138xx | pt_138 | 模块预告 |
| 112xx | pt_112 | 装扮 | 139xx | pt_139 | 好友 |
| 113xx | pt_113 | 微信 | 140xx | pt_140 | 战斗相关 |
| 120xx | pt_120 | 场景信息 | 141xx | pt_141 | 关系 |
| 121xx | pt_121 | NPC | 142xx | pt_142 | 伙伴 |
| 130xx | pt_130 | 玩家信息 | 150xx | pt_150 | 物品 |
| 131xx | pt_131 | (未明) | 151xx | pt_151 | 交易市场 |
| 132xx | pt_132 | 离线挂机 | 152xx | pt_152 | 装备 |
| 133xx | pt_133 | 结界守护 | 153xx | pt_153 | 商城 |
| | | | 200xx | pt_200 | 战斗信息 |
| | | | 300xx | pt_300 | 任务 |

(40000+ 公会、16000+ 技能等更多段在 src/pt 全列,接对应模块时再查。)

## 3. 登录段(100xx)已接协议与勘误

| cmd | 方向/格式 | 说明 |
|---|---|---|
| 10000 | 发 "iiss" / 回 "clihi"+角色 | 账号登录。**accname/time/pid 用 get_server_info 下发值** |
| 10003 | 发 "cccsslsscscc" / 回 "cl" | 创角 |
| 10004 | 发 "lsisisscscsh" / 回 "c" | 选角进游戏(成功=1 → GAME_START) |
| 10006 | 空 | 心跳。**服务端有频率限制(pp_login.erl:219,登录后计数防刷)** |
| 10007 | 发 "s" / 回 "c" | **角色名验证**(勘误:曾误标为踢线通知,已改 Proto.NAME_VERIFY) |

## 4. 进入游戏后服务端推什么(mod_login.erl:690+)

10004 回 1 之后,服务端起玩家进程并触发 `?EVENT_LOGIN_CAST`,各 lib_* 模块**主动推送**初始化数据,主要有:

- 130xx 玩家:13001 属性 / 13002 经验 / 13003 等级 / 13006 金币 / 13011 世界等级 /
  13017 托管 / 13080+ 头像……
- 150xx 物品(lib_goods:login)、16000+ 技能、40000+ 公会;120xx 场景与出生点。
- 老客户端 GAME_START 后也会主动**请求**一批(任务 30005、龙珠 143xx 等,各 Controller 自发)。

**Unity 承接设计(已定,按需逐模块实现):**

1. 一个业务模块 = 一个 `BaseController` 子类(单例 + `Register()` 注册本段协议),对标老客户端
   commonController/*.ts 一比一搬;`GmCheatController` 是首个游戏内模块样板。
2. 未注册协议:NetManager 收到没有 handler 的 cmd 时只记 Debug 级日志不报错——进游戏初期
   服务端会推几十条我们尚未实现的协议,这是预期内噪音,按模块推进逐个消化。
3. 进游戏后第一个落地目标:RoleController(13001/13002/13003/13006)+ 场景段(120xx 出生场景),
   即"进入游戏能看到主角"。
4. 复杂回包优先在 pt_*.erl 里查 write 格式(服务端是格式真相源),old client ReadFmt 做对照。

## 5. 心跳 / 断线 / 顶号(约束清单)

- 心跳 10006:我们按 AppConfig.heartbeatIntervalSec 定时发(NetManager.ConfigureHeartbeat),
  服务端登录后有防刷计数(pp_login.erl:219-230),间隔别低于秒级,当前配置安全。
- 老客户端重连状态机(UserMsgAdapter.ts:183-290):4 类 reconnect_type、最多 4 次提示、
  顶号(other_place_login)禁止自动重连。Unity 暂未实现重连(断线即回登录),
  做主城线时按此状态机补——记账。
- 踢线/顶号的真实协议号待查(不是 10007),接聊天/登录段细化时确认。

## 6. GM 秘籍(111xx,已接)

- 11100 请求清单(空)→ 回包 u16 分类数 × { s 分类名, u16 命令数 × { s 命令, s 中文名,
  u16×s 参数描述, u16×s 默认值 } }(pt_111.erl)。服务端 pp_gm.erl 约 350+ 条命令,
  **清单由服务端下发,客户端零硬编码**。
- 11101 执行:发 "s",格式 `命令_参数_参数`(下划线分隔,pp_gm.erl:737 string:tokens)。
- 鉴权:`config:get_gm_password()` 为空 → 全放行(开发模式);非空 → 先发
  `setgmpassword_密码`(进程内记住)。无等级/账号位判断。
- Unity 工具:`神霄/GM 秘籍`(Play 模式)——拉取清单 → 分类/搜索/参数默认值/一键发送,
  顶部直发框可敲任意命令。运行时 `GmCheatController` 可被其他调试入口复用。
- 老客户端对照:按 Z 呼出 CheatInputView(KeyInput.ts:65,需 TestYouWant=1 或本地模式)。
