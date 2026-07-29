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


## 7. 进游戏框架(Phase A 落地,2026-06-15)

进游戏后的协议接入骨架就位,"进游戏看到主角数据"打通:

- **ControllerHub**(Module/Core/Game):游戏内控制器注册中心。进游戏 `InitAll()`、断线/登出 `DisposeAll()`;新增模块在 `ALL[]` 加一行。当前含 RoleController、GmCheatController。
- **GameEntryFlow**(Module/Core/Game):进游戏编排。`EVT_GAME_ENTERED`(10004 成功)→ 注册控制器 → 等 13001 全量 → 发 `EVT_ROLE_READY`(交主城/场景);`EVT_NET_DISCONNECTED` → 注销 + 重置主角。`[RuntimeInitializeOnLoadMethod]` 自装,无需手挂。
- **RoleController + RoleModel**(Module/Core/Role):首个游戏内模块。接 13001(全量)/13002(经验)/13003(升级 hll)/13006(货币 liii),写 RoleModel,发 `EVT_ROLE_INFO_UPDATE`。格式严格对标 pt_130。
- **BaseController 升级**:记录已注册协议号,`Dispose()` 统一注销(可重入)。
- **NetReader.ReadArray<T>**:u16 计数数组通用读取(对标 pt:write_array);**BattleAttrProto**(Common/Proto):Hp/HpLim/Speed + 属性表,对标 pt:write_battle_attr。

**模块接入模板(后续每个 130xx+ 模块照此)**:
1. `Proto` 加该段协议号常量(注释带格式串 + pt 出处)。
2. 复合 VO 放 `Common/Proto`(跨模块)或模块内;数组用 `NetReader.ReadArray`。
3. `{X}Controller : BaseController`,Register 注册回调,回包写 `{X}Model`,发 `EVT_*`。
4. 在 `ControllerHub.ALL` 注册一行。

**下一步**:① 真机验证 13001 链路(进游戏 Console 看到主角名/等级/货币 + 不刷未注册错误);② 场景段 120xx 最小接入(出生场景);③ Phase B 协议代码生成器(schema→常量/VO/读取/桩)。

### 7.1 全局错误 10205（2026-07-30 核对）

- `10205` 无 C2S，是 `lib_game:send_error/2..4 -> make_error_bin_data -> pt_102:write(10205, ...)` 的全局错误出口；wire 固定为 `error_code:u32,args:string`。
- `GameStartController` 随控制器初始化常驻注册，但绝不主动发送、不加入 GAME_START。每包完整消费两个字段并无条件显示错误，对标老端 `ServerTimeController.On10205 -> Util.ErrorCodeShow`。
- Unity 尚未迁移 `data_error_code` 与 args 模板替换器，因此用户提示降级为 `操作失败(code)`，原始 args 只写诊断日志；不得把 args 原样冒充最终本地化文案。
- 相邻号不能顺手并入：10204 的版本 setter 在老端无 sender；10207 依赖 CDN 登录公告版本/正文/红点；10211 依赖服务端 `data_popup`、登录/定时/神殿条件和真实弹窗消费。三者分别按 KILL/DEFER 治理，不注册空 handler。

### 7.2 时空圣痕 204xx 只读切片（2026-07-30 核对）

- GAME_START 仍只空发 `20411`；`20401/20404/20405/20407/20409/20410` 仅允许业务显式空查，`20402` 显式发送 `castle_id:u16` 且兼容同号服务端推送。收到 `20411 status==1` 或等级变化都不得自动扇出其他 204xx。
- `20401` 保存个人/本服争夺值和完整有序据点表；`20402` 按回包 `castle_id` 只替换独立明细桶。两者即使字段形状相同也不共写，避免明细晚包污染主快照。
- `20404` 每日来源表、`20405` 每日阶段奖励状态、`20407` 赛季目标、`20409` 个人排行、`20410` 当前据点和 `20411` 世界服表均为独立原始切片。列表保持 wire 顺序与重复项，空表是 loaded 清旧；零值、u32/u64 最大位型不作“未加载”推断。
- 读侧不排序、不加载配置、不发事件或红点，也不展示或发放奖励。`20403` 驻扎/传送、`20406/20408` 领奖及其 `20400` 错误链仍须随对应 UI/场景/奖励闭环迁移，当前禁止公开 sender 或孤立结果处理。

### 7.3 聊天 110xx 的 GAME_START 与缓存语义（2026-07-28 核对）

- `ChatController` 在 `GAME_START` 依次请求 `11010(仙宗) -> 11010(私聊) -> 11010(世界)`；非开服第 1 天
  再请求 `11010(小跨服17) -> 11010(百煞冲霄20)`，随后空发 `11050 -> 11064 -> 11023`，最后插入本地欢迎语。
- `11010` 不携带独立频道尾字段，空缓存只能得到 `count=0`，因此不能从空回包反推出请求频道或借此清理其他频道。
- 服务端缓存按“新 → 旧”下发；客户端写入展示模型前必须逆序为“旧 → 新”。私聊项必须保留
  `player_list` 中发、收双方，并依据 `is_read` 恢复未读状态。
- 老端把频道 20 的 `11001` 实时消息和 `11010` 缓存统一映射到小跨服频道 17；Unity 必须保持一致。
- 主界面消费链和显示规则见 [主界面聊天消息链路-经验与排障](主界面聊天消息链路-经验与排障.md)。
