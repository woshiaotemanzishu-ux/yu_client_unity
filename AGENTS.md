# AGENTS.md

- Git 收口规则：隔离 worktree / `codex/*` 分支只用于开发与验收；每轮已确认成果必须在停止前合并到本地 `main`，确认提交已包含后再删除临时分支和失效 worktree。若用户主工作树存在未提交改动，不得为切换 `main` 而覆盖、暂存或代提交这些现场；应在独立干净 worktree 更新 `main`，待用户改动完成并提交后再把主工作树切回 `main`。

- 文档与经验沉淀规则：开始任务先从 [Docs/README.md](Docs/README.md) 找对应权威文档；凡新增或调整架构、公共组件、工具/资源流水线、协议/登录/进游戏主链、构建发布方式，或解决具有复用价值的疑难问题，必须在同一轮、同一提交中新增或更新技术文档/经验文档，并把新文档加入索引。已验证进度同步更新 `Docs/Shenxiao实施进度.md`；形成 AI 硬约束的决策同步更新 `AGENTS.md`，编码约定同步更新 `Docs/Shenxiao编码规范.md`。纯错字、无行为变化的机械修改可不写，但最终报告必须说明“不触发文档更新”的理由。禁止只把结论留在聊天、外部记忆或临时输出目录中。

- 协议覆盖 baseline 收口规则：`Schemas/ProtocolCoverage/baseline.json` 的历史 `unityRegistered/liveGap` 数值用于防倒退，禁止为追当前报告逐轮改写；但当最新运行时扫描的某家族全部活缺口已由注册或带 evidence 的 killlist 完整治理时，必须在同轮把人工 `status` 转为 `done` 并写 `statusNote`，再以 `ProtocolCoverageCase` 的C段验收。禁止长期保留 `pending/legacy_unverified` 逃避C段，也禁止把真实玩家可达写事务塞进killlist来伪收口。

- 协议负约束优先规则：`AGENTS.md` 对某协议明确写了“不接/No/Do not attach”时，后续轮次不得用“补覆盖率”推翻；若审计发现晚于该规则的常量、注册、sender、模型或用例，必须撤销违规增量并让家族保持 `pending`，不得靠违规注册把 raw 缺口清零后标 `done`。R517 已按此撤销20600/20602与28500。

- Unity Pipeline 许可证恢复规则：仅出现 entitlement 404、命令超时但同 PID/Pipeline 仍为 `ready` 时继续等待重试，禁止重启；只有 Editor 日志明确写出 `Application will terminate with return code 198`、Pipeline 已注销，且该精确 canonical batchmode PID 卡在退出态持续占用 `Temp/UnityLockfile` 时，才可先确认 CLI license 已恢复 active，再只终止这个已退出项目 PID、删除已核实的残留锁并按原参数重启。禁止扩大到健康 Editor、Hub 或全局 Unity 进程。

- R517 Skill 21003/21004/21005：三号在当前服务器 `pp_skill` 的唯一请求入口均整段注释；`lib_skill` 中残留的查询、扣物、DB和属性实现及 `pt_210` writer 没有可达调用方。老端21003启动请求也已注释，21004/05只剩通用sender分支和孤立结果回调。三号保持KILL：禁止新增常量、注册、请求、raw模型、技能强化UI/配置/背包/属性/事件或孤立ACK；只有服务端恢复handler且产品明确恢复完整技能强化链时才成族重审。

- R518 OutWard/Baby/GuildGod 收口规则：16013技能战力查询、16021法阵广播、18202他人宝宝信息虽有服务端实现或回包定义，但老端全仓只有接收注册、没有玩家可达C2S入口；16021还被服务端 `APPERENCE=[1,2,3,4,5,12]` 排除法阵 `TypeId=8`。40505/40507的穿戴/卸下事务真实存在，但服务端成功只推40502、失败只写40500，全仓从不写两份专属结果。五号保持KILL，禁止为清覆盖率新增接收器、请求、raw结果、场景法阵、宝宝他人资料或专属ACK等待；只有老端/产品恢复真实入口或服务端恢复专属writer后才逐号重审。

- Unity 序列化脚本规则：会挂到 Scene/Prefab 的 `MonoBehaviour` / `ScriptableObject` 必须独占同名 `.cs` 文件，禁止把它放在“首个类型为静态类、抽象类或其他不同名类型”的文件里；Editor 生成器 `AddComponent<T>()` 后必须核对产物 `m_Script` 为非零 GUID 且 GUID 指向 `T` 的同名脚本。否则即使 C# 编译通过，Unity 仍可能把组件保存成 Missing Script。

- 主界面技能规则：技能项只允许 `con` 作为唯一 Raycast/Button 点击面，`bg/icon/lock/CD/文字` 等装饰 Graphic 必须关闭 `raycastTarget`；点击验收必须走真实 Prefab 的 `GraphicRaycaster→PointerClick`，直接调用 `OnClickSkill` 不算点击链通过。普通 `AutoFight` 不得拦截玩家手点技能，只有服务端 `13017` 的 `RoleModel.DepositState` 托管态才拦截。手动无锁定目标时按当前朝向原地释放并只在技能 `area` 内局部预选，不得跨全场抢最近怪；自动战斗才允许全场寻敌并接近。接敌范围严格对标老端：`range==1` 使用 `max(100,(distance+area)*0.8)`，其他模式使用 `max(100,distance*0.8)`；命中几何由 `range/distance/area/num` 决定，禁止从 `desc` 文案猜圆形、直线或扇形。

- NPC 对话与选中规则：`DialogueView/_img_bg/_box_model` 必须随全屏父级伸展，`_box_bottom` 只锚真实屏幕底边，禁止按设备高度在运行时代码硬调坐标。对话背景、底栏、继续、领取和跳过必须汇总到 Module 根唯一点击面并走同一语义动作；根点击面须显式挂透明 `Image` 后调用 `UIUtil.AddClick(Graphic, ...)`，禁止把布局首帧可能为零尺寸的根交给容器重载而退用局部子 Graphic。任务完成弹层的任意点击（含关闭图标）必须领取/提交，禁止纯 `Close()` 导致重复弹出，点击验收走真实 `GraphicRaycaster→PointerClick`。NPC/怪物选中只复用 `other_effect/function_selection`：缩放 0.7，挂目标 `Tilt` 后保持 `localRotation=identity`，使最终世界 X 倾角继续为 -38°；禁止再用 +38° 抵消 Tilt，否则平视相机只看见资源地面网格的薄边。目标切换、移除/死亡、清目标、断线和切场景必须清旧实例，禁止按模型复制或手画替代圈。

- 主角随身特效空间规则：按老模型世界单位制作、需要跟随主角 yaw/2.5D 倾斜的循环整体特效（当前任务跑动 `char_acceleratebuff01`）必须挂 `SceneCharacterStage.MainRoleAttachedEffectHost`。禁止挂 `ReplaceableRoleModel.ActiveModel` 或新动作 prefab 内部 `root`：`ArtModelStager` 的 `landingOffset/landingScale` 会把该 root 留在人物后方并缩小特效。骨骼动作/技能特效仍挂 `ActiveModel`，`attach_type=15` 一次性特效仍挂 `MainRoleDetachedEffectHost`，禁止通过放大公共 prefab 针对单个模型补偿。

- 模型无光照规则：选角、游戏内场景、UI 模型台和资产预览统一不创建模型灯、不改写 `RenderSettings.ambientLight`。美术贴图自带最终颜色；新模型常规 Standard/URP Lit 表面只在运行实例上转为 URP Unlit，并关闭投射/接收阴影。禁止恢复 `ArtAmbient`、平行光、PreviewRenderUtility 灯或按页面补光；Panda/粒子特效材质及 Depth/Opaque/StageComposite 渲染口径不受此规则影响。

- 主界面聊天规则：HUD 必须消费 `ChatModel` 并监听 `EVT_CHAT_MESSAGES_UPDATED`，禁止用硬编码欢迎条代替协议消息。`GAME_START` 的 11010 缓存请求、11050/11064/11023 顺序对标老端；11010 wire 为新→旧，展示前须逆序，私聊须保留发收双方与 `is_read`，频道 20 的 11001/11010 均映射到频道 17。频道徽标只占正文首行，模板基础高度 29，多行按 TMP preferred height 扩高。上下双栏、合并或 Tab 属设计决策，未明确前不得在功能修复中顺带改造。

- 主角场景自身特效规则：运行时特效实例统一走 `EffectBinder`，禁止业务层手工 `LoadAsync + Instantiate`。老端 `attach_type=15` 必须挂 `MainRoleDetachedEffects`（与主角同落点、单位旋转、不得继承 `MainRoleTilt`/模型 yaw）；骨骼动作/技能特效仍挂 `ReplaceableRoleModel.ActiveModel`，任务跑动 `char_acceleratebuff01` 必须挂稳定单位空间 `MainRoleAttachedEffectHost`，不得随动作实例切换重挂。任务加速拖尾仅由任务导航触发，严格要求场景 `type∈{0,1,4}`、各轴按 `LogicRatioX/Y` 换算后的距离 `>7` 格并延时 150ms；手点 NPC、战斗接敌、摇杆和普通自动移动不得触发。任务跳跃 `show_effect=false`：中间段 `char_jumpfx_01`、最终段 `effect_jump_qitiaoyan`，禁止误播 `char_jumpfx_02`。场景主角关闭 `Body.always`，UI 模型继续加载，武器/翅膀/背饰常驻特效不受影响。`UIModelStage` 与 `SceneCharacterStage` 的透明模型 RT 必须使用带 Alpha 的 `ARGBHalf` 并开启相机 HDR，再以 `Shenxiao/UI/StageComposite` 预乘合成；禁止退回 `ARGB32` 截断 Panda HDR/半透明中间色，也禁止从 RGB 亮度伪造 Alpha 覆盖。Panda `One/One` 加法粒子保持 `_ScrA=Zero/_DstA=One`，但美术明确设置半透明 `_MainColor.a<1` 的加法 `SkinnedMeshRenderer` 结构层必须写 `_ScrA=One/_DstA=OneMinusSrcAlpha`；禁止再次一刀切清空结构层 Alpha，否则 1005 `wing-2` 会像漏挂一样消失。30004 `code=1` 必须在 `UILayer.Top` 播 `ui_renwuwancheng` 1.5 秒；它不是 `TaskFinishView` 或角色自身粒子。

- 大妖入场横幅规则：`effect_ui_dayaolaixi` 的文字/主体由 Legacy Animation `UI_2103` 驱动，真实结束时间为 1.083 秒；`liutizuo/liutiyou` 是循环底纹，必须在主体片段结束时随整个 `UIEffectStage.Handle` 一起释放。配置的 1.5 秒只能作为上限，3 秒只能作为资源加载/回调失败兜底，禁止让循环底纹单独残留到任一固定超时。

- 旧 Laya 粒子兼容规则：项目保持 Linear，禁止为修复特效发淡而全局退回 Gamma；`LayaParticleUnlit` 必须恢复旧材质 `tint=0.5 × shader 2` 的中性数值。Laya `GradientDataNumber` 按相邻关键点线性插值，转换器禁止使用 Unity 自动平滑切线，否则尺寸/速度曲线会过冲甚至被钳为 0。材质 UV 动画必须绑定 shader 实际读取的属性；当前 `LayaParticleUnlit` 消费 `_MainTex_ST`，`char_acceleratebuff01` 必须保留该属性 `z:0→3/1s` 的循环曲线以形成流光隐现，禁止用固定位置偏移掩盖 UV 定格。任务完成仍走 UI 特效链，老端 `(0,+4)` 在 Unity 仅于该业务边界映射为 `(0,-4)`，不得改挂角色。
- 旧 Laya 特效网格顶点色规则：排查流光硬边、矩形截断或“像放反”时，必须同时核对源 `.lm` 的 `COLOR` 通道与 Unity `Mesh.colors`，禁止只看贴图、UV 或挂点。`char_acceleratebuff01/eff_cys_sz01` 必须保留 32 个顶点色，Alpha 覆盖 `0..1` 且含中间渐变值；丢失顶点色会让 `_MainTex_ST` 扫光在网格边缘形成硬截断，不得用旋转、平移或按模型特判掩盖。

- R507 Designation 41104/41105/41107/41108：41104 是 S2C-only 激活通知 `code:u32,id:u32,end_time:u32`，不得复刻旧端首次激活后自动发送 41102；41105 是 S2C-only 场景称号通知 `player_id:u64,id:u32`，当前只保留原始最后通知，不改场景角色；41107 是显式 `id:u32` 纯战力查询及独立 `errcode:u32,power:u32` 最后快照，不加入 GAME_START，无回复保留旧值；41108 是 S2C-only 移除通知 `id:u32`，不得补丁式改写 41101 权威列表。四份切片逐包完整替换并与 41101 双向隔离，零值/最大值有效。41102/41103 佩戴卸下、41106 升阶、41109 道具激活、41110 过期取消均为真实写操作，继续 DEFER；不得接 UI、场景表现、配置、背包、属性、事件、红点、Toast、自动重拉或乐观状态。
- R508 MiniGame 39901/39902/39904/39922：39901 仅接 S2C 开始通知 `code:u32,game_type:u8,module_id:u16,sub_id:u8,start_time:u32,end_time:u32,info_list:u16×u32`，不得公开同号手动开局 sender；GAME_START 只空发 39902，39902 回包保存同形状去掉 code 的最后原始当前态，39922 消消乐重连包保存完整棋盘/效果/积分棋嵌套快照。39904 仅允许显式 `game_type:u8,module_id:u16,sub_id:u8` 排行查询，按三字段复合键原序保存完整排行，重复角色/空表均有效。请求无回复不得清旧；当前服务器节奏玩法的 39901/39902 调用实参与 `pt_399` writer 分别少 `end_time`，客户端不得补包、猜字段或清旧。39903 是老端只解空包且服务器不下发的空反馈，39905 当前服务器无 writer，39931 是既有全注释 handler，三号均维持 KILL；39921 上传棋盘、39923 主动结算及 39901/39903 的 C2S 开始/反馈均为真实玩法写操作，继续 DEFER。不得接小游戏 UI、配置、倒计时、场景、事件、排行排序或自动开局。
- R509 Chat 11003/11004/11005：三号保持不注册、不发送。11003 的服务器上传、存储与广播链以及 11004 的按需下载回包虽仍有效，但老端唯一录音面板入口、手势绑定、SDK 录音/回调、实际 Fire、点击补拉和自动播放调用均已注释，当前产品不可达；11005 又因服务器调用传裸整数、writer 只匹配列表而落 `cmd=0` 空包。禁止因 raw liveGap 恢复语音常量、sender/handler/model、麦克风/SDK、音频播放器、二进制缓存、语音 Figure 大包或 UI；只有产品明确恢复完整聊天语音功能时才成族重审。既有文字聊天、11010 缓存顺序、私聊和频道映射规则保持不变。
- R511 HolySeal 65401/65405/65408/65409：GAME_START 必须先清 HolySeal 全状态，再严格空发 `65401→65405`；65401 装备表、65405 魂珠表和显式空查询 65409 当前套装表均为原序全量快照，重复项保留、空表 loaded 清旧。65408 仅允许显式 `goods_type_id:u32` 查询；回包不回显请求键，只保存最后一份原始套装预览与 `code:u32`。所有请求无回复保持旧切片，四份快照与 65400 错误、65407 评分互相隔离。65402 强化、65403 穿戴、65406 使用魂珠继续 DEFER，65404 维持 `old_client_never_sends` KILL；不得接 UI、配置、背包、属性、事件、红点、Toast、自动重拉或乐观资产状态。
- R512 AutoBrush 13309/13323/13324：GAME_START 必须先 Reset，再严格空发 `13300→13301→13309→13323→13324`。13309 为 `code:u32,next_stage_reward_gate:u64` 原始绝对快照，禁止把 gate=0 在模型层改成旧 UI 的 99999；13323 为 `node:u8`；13324 为 `daily_ask_time:u16,next_ask_time:u32`，并接受公会协助链同号主动推送。三份 loaded 切片逐包完整替换、零/最大值有效，与既有刷怪/排行/战斗状态互相隔离，Reset/Dispose 全清。13310 领奖与 13322 教程节点持久化继续 DEFER；不得接奖励、教程 UI、配置、红点、公会协助操作、孤立 ACK 或乐观状态。
- R513 WeekDungeon 50805：仅接S2C周本专属结算，wire固定为 `result_type:u8,dun_id:u32,go_time:u32,dun_rewards:u16×{type:u8,times:u16,ObjectList},role_boss_list:u16×{boss_id:u32,reward_st:u8,ObjectList}`。每包完整替换独立loaded快照，保序保重、空表清旧、零/最大值有效；不得暴露请求、复用61003、改50801/50802、接结算UI/场景退出、展示或发放奖励、补查、配置、事件或背包状态。14402打造与13213挂机赎回仍是真实写事务，继续DEFER。
- R506 JJC 28000/28010/28013/28014：28000 是 S2C-only 原始错误 `errcode:u32`；28010 是显式严格空查询及独立 `errcode:u32,honour:u32` 快照，不得回写 28001 的 Honour；28013 是显式严格空查询及 `self_robot_id:u64,self_role_id:u64,rival_robot_id:u64,rival_role_id:u64` 完整快照；28014 是 S2C-only `stage:u8,time:u32`，time 保留服务端 Unix 绝对截止时刻。四号均逐包完整替换自己的最后原始切片，零值/最大值有效，无回复保留旧值；GAME_START 仍严格为 `28004→28001`。28005 购买次数、28012 退出战斗、28015 跳过战斗、28017 突破领奖继续 DEFER；28008 的服务端请求入口已注释，维持 KILL。不得接 UI、场景、货币、奖励、Toast、事件、红点、自动发送或其余 280xx 操作。
- R505 Rune 16704/16705/16706/16709：GAME_START 保持精确 `16700→16704`；16704 是严格空请求及 `rune_dungeon_level:u16` 绝对快照。16705 合成预览请求为 `rule_id:u64,goods_ids:u16×u64`，回包为 `code:u32,lv:u32`；16706 分解预览请求为 `goods_ids:u16×u64`，回包为 `code:u32,exp:u64,result:ObjectList`；16709 觉醒符文拆解预览请求同为 `goods_ids:u16×u64`，回包为 `code:u32,result:ObjectList`。后三号不回显请求键，只保存彼此隔离的最后一份原始预览；失败码、零值、重复 ObjectList 和空表都完整替换，无回复保留旧切片。不得接 16703 兑换、16707 觉醒、16708 拆解、16710 技能升级、16711 卸下，亦不得附带 UI、配置、事件、红点、本地资产变更或乐观成功。
- R484 Game 10205 is the S2C-only global error exit `error_code:u32,args:string` used by every `lib_game:send_error*` overload. Register it unconditionally with the game-start/server-time controller, consume both fields, and mirror the old client's unconditional error display; until the error-code table/template formatter is migrated, show the numeric fallback and retain raw args only in diagnostics. Never expose a request, treat it as a success receipt, write model state, emit business events, retry, or attach it to GAME_START. Keep 10204 absent as `old_client_unreachable`: its `client_ver:u64` setter is live server-side but the old client has no sender. Keep 10207 deferred with the CDN login-notice data/red-dot flow, and 10211 deferred with `data_popup`, login/timer/Temple conditions and real popup consumers; do not register either as a no-op.
- R483 Market 15104/15105 remain excluded by old-client behavior, not because their server query handlers are dead. 15104 sends a string goods name and returns an ordered goods list; 15105 sends `goods_id:u64,type_id:u32` and normally returns `goods_id:u64,type:u8,recommend_price:u32`. Both server query chains are live, but the old client has no sender and each registered handler only decodes then discards the packet. The 15105 non-sellable branch also passes one error value to a three-field writer and cannot produce a valid same-number error packet. Keep both absent—no constants, registration, requests, snapshots, search/recommend-price UI revival, fabricated defaults or client workaround for the malformed server branch. Preserve the existing 17 registered Market protocols.
- R482 RedPacket 33903/33905 remain excluded by old-client reachability, not because their server handlers are dead. 33903 sends `type:u8,extra:u64` and returns `times_limit:u8,remain_times:u8,total_times:u8,split_num:u16`; the server query is live, but the old client has no sender and its registered handler only decodes then discards the packet. 33905 sends `goods_id:u64,gtype_id:u32,split_num:u16` and returns `errcode:u32`; the server validates guild/config/limits, deletes a real item, creates a red packet and increments the daily counter, but the old client has no sender. Keep both absent—no constants, registration, requests, snapshots, result handlers, UI revival, local item deduction, optimistic packet creation or coverage-driven resurrection. Preserve the existing 33900/01/02/04/06/07/08 implementation.
- R481 SevenDay 17501/17503 are real reward-claim transactions, not query snapshots. Both send `day_id:u8` and return `errcode:u32,day_id:u8,reward_list:u16×{good_type:u8,good_id:u32,good_num:u32,good_auto_id:u64}`; success grants through bag or mail and persists that day's status as claimed. Keep both absent until the exact per-day status/config, all three SevenDay views, claim gating/single-flight, error handling, reward presentation, icon/red-dot refresh and bag/mail semantics are migrated together. Do not add constants, registration, senders, raw receipts, optimistic status changes, local inventory grants, blind retry, GAME_START/day-change claims, or reinterpret the returned display list as a second grant. Preserve the existing 17500/17502-only startup/query chain.
- R480 Partner 14200 is S2C-only scene-figure information: `type_id:u8,role_id:u64,figure_id:u32`. Every packet fully replaces the independent immutable last-notice slice and emits only `EVT_PARTNER_SCENE_FIGURE_CHANGE`; it must not mutate 14201/14202 companion data or emit the panel-update event. Do not expose a request, add GAME_START/scene polling, or invent a scene-role cache/render implementation. Keep 14203 follow, 14206 nucleus training and 14207 biography unlock absent until their scene/skill, item-cost, reward/config/UI chains are migrated together; no constants, registration, senders, result toasts, optimistic state, 21002 double-registration, rewards, or red dots.
- R479 Team 24011/24042 remain dead client flows. 24011's server-side leader transaction is live, but the old client's only avatar-menu path ends in an empty `ShowPlayerMenu`, `PlayerMenuView` is absent, and there is no sender; Unity already receives authoritative leader changes through 24015. 24042 is a server read query returning `type:u8,left_time:u8`, but the old client never sends it and its handler only decodes then discards the packet. Keep both absent—no constants, senders, handlers, snapshots, toasts, local leader changes, activity-count derivation, or UI revival.
- R478 CommonRank 22102/22103/22104 are dead: `pp_common_rank.erl` explicitly says guild ranking and worship were split out, then comments all three handlers. The remaining 22102 guild-rank writer and 22104 praise DB/write implementation have no reachable caller; the only 22103 writer is itself commented. Keep all three absent from Unity—no constants, registration, requests, snapshots, praise UI/rewards, toasts, local decrements, or attempts to revive the old flow. Re-audit only if the server restores explicit handlers and the replacement product entry/UI.
- R476 BossRotary 51001/51002/51003/51004 are dead while `mod_server` routes the entire `"510"` family to literal `skip`. 51001's only writer sits behind that disabled route; 51002's boss-kill trigger is a hard `ok` stub and its writer is commented; 51003 abandon and 51004 paid draw/reward are real DB/reward operations internally but likewise unreachable before `pp_boss_rotary`. Keep the whole 51001-51005 family absent from Unity—no constants, handlers, senders, models, UI, local rewards, or attempts to revive the server feature. Re-audit the complete family only if the server route and product flow are explicitly restored.
- R475 TreasureMap 20300 is an S2C-only unified raw error exit: `code:u32`. Every packet fully replaces only this loaded error slice, including zero and `uint.MaxValue`; keep it mutually isolated from the 20303 draw-log snapshot. Do not expose a 20300 request or reproduce the old client's toast/reset side effects. Keep 20301 map-use/navigation and 20302 draw/reward transactions unregistered until their scene, pathfinding, collection, bag and reward-display chains are migrated together; do not attach GAME_START, UI/config, items, events, red dots, local error interpretation, or optimistic state changes.
- R474 HotPoint: GAME_START sends the exact empty 33300 once; its ordered activity list fully replaces only that loaded slice. 33302 and 33303 are explicit `base_type:u16,sub_type:u16` keyed full snapshots; same-key packets replace, different keys coexist, and empty lists remain loaded. Server-side first-load initialization during a 33303 query only creates canonical unclaimed statuses and is not a reward transaction. 33305 is S2C-only keyed progress: preserve its raw ordered delta, overwrite `sum_points`, and merge into an already-loaded 33302 detail only by `mod_id/sub_id/condition_type` plus `33302.dec == 33305.name`; duplicate deltas apply in wire order, last match wins. Never fabricate a missing detail. Every 33305 then sends exactly one same-key 33303 refresh, matching the old client. Keep 33300/02/03/05/06 slices isolated. Do not expose/register 33304 reward claim or attach UI/config, icons, events, red dots, reward display, optimistic claims, or other automatic queries.
- R473 CycleRank 22705 is S2C-only raw rank-change information: `rank_type:u16,rank_subtype:u16,type:u32,rank:u32,value:u32`. Every push fully replaces only this loaded slice, including all-zero and maximum values. Do not expose the server's nominal empty 22705 request: that request clears the player's server-side notification cooldown, and the old client never sends it. Keep 22700/01/02/03/06 state isolated, and do not attach the 22704 reward claim, tips UI, level/config gates, events, red dots, local threshold interpretation, or any automatic request.
- R472 Pray 41501 is an explicit-only strict-empty query and ordered full snapshot: `list:u16×{type:u8,remain_times:u8,free_times:u8,endtime:u32}`. Every reply atomically replaces only this loaded slice, preserving wire order, duplicate types and all raw zero/maximum values; an empty list clears old entries while remaining loaded, and a request without a reply preserves the prior snapshot. Keep the 41500 raw error slice isolated. Do not reproduce the old client's GAME_START/hour-4/view-open/level/VIP auto-requests, and do not attach 41502 pray/reward transaction, UI/config, red dots, events, local countdowns, or optimistic state changes.
- R471 WelfareCard 15901 is an explicit-only strict-empty query and ordered full snapshot: `product_list:u16×{product_type:u32,product_subtype:u32,product_id:u32,state:u8,left_count:u16}`. Every reply atomically replaces only this loaded slice, preserving wire order, duplicate product IDs and all raw zero/maximum values; an empty list clears old cards while remaining loaded, and a request without a reply preserves the prior snapshot. Keep 15800/15801 recharge products and 15905-15908 first-recharge state isolated. Do not add 15901 to GAME_START or day-change, and do not attach the 15902 claim transaction, UI/config, red dots, events, local countdowns, or optimistic state changes.

- R188 HolyBattle 21810 is S2C-only result information: `res:u8,groups:u16×{group_id:u8,tower_num:u8,point:u32},my_group_id:u8,my_rank:u8`. Each packet fully replaces only this slice, preserving group wire order and duplicate IDs; an empty group list clears old groups but remains loaded. Do not expose a request or attach result UI, reward config/mail, buff/skill cleanup, automatic leave, 21803, scene hooks, or operations.
- R187 HolyBattle 21809 is S2C-only death information: `role_name:string,role_id:u64,lv:u16,power:u64,picture_ver:u32,picture:string,anger:u32,server_id:u32,career:u8,turn:u8`. Every packet, including all-zero and empty-string fields, fully replaces only this loaded raw slice. Do not expose a request or attach GameStart, scene hooks, revive UI, anger events, configuration, red dots, or operations.
- R186 HolyBattle 21813 is an explicit empty request and also accepts server pushes. S2C is `mon_list:u16×{mon_auto:u32,mon_cfg_id:u32,hp:u32,hp_all:u32,group_id:u8}`. Apply packets incrementally by `mon_cfg_id`; a matching `hp=0` deletes that entry, an unknown `hp=0` changes nothing, and an empty list is loaded but preserves the existing dictionary. A request without a reply also preserves it. Do not add it to GameStart or scene hooks, and do not attach UI, auto-fight, config, red dots, rewards, or other HolyBattle operations.
- R184 NineSky 13503 is an explicit-only battle-panel snapshot and also accepts server pushes. C2S is an exact empty frame; S2C is `cur_floor:u8,max_floor:u8,left_time:u32,kill_num:u16,score:u32,first_server_num:u16,first_player:string`. Non-NineSky scenes or a missing NineRank produce no reply, so requests without a reply preserve the prior snapshot. R185 NineSky 13504 is S2C-only: `index:u8,server_num:u16,role_id:u64,role_name:string,left_time:u32`; do not expose a Request13504 or add GameStart/scene hooks. A zero-role/no-holder packet (`server_num=0,role_id=0,role_name=""`) is a valid full overwrite while preserving its `index` and `left_time`. Keep 13500 GAME_START unchanged and the 13500/13503/13504 slices mutually isolated; do not add scene detection, UI/config, red dots, rewards, role/scene flags, or 13502/05-10 operations.
- R183 Demon 18302 is an explicit-only single-demon power query. C2S is `demons_id:u32` (10-byte client frame); S2C is `demons_id:u32,power:u32`. Cache by demon id, including a real zero value, and use `TryGetDemonPower` to distinguish zero from not loaded. Requests without a reply preserve the prior entry. Keep GAME_START exactly `18301→18303→18307→50901`; do not wire the old UI, events/red dots, or the 18304/05/10 operation-success requery chain. A Unity domain reload may transiently make `recompile_status` or the first eval return Pipeline `401 Unauthorized`; if the same PID is `ready`, retry the command instead of restarting the Editor.
- R182 Eternity 27906 is an explicit-only read-only relive/death-fatigue snapshot. C2S is an exact empty 6-byte frame; S2C is `die_times:u16,time:u32,die_time:u32,safe_time:u32`. Every reply, including all-zero values, fully replaces only this slice; a request without a reply preserves the old snapshot. Do not derive config timers or "fix" the server's historical `length([DieList])` behavior, and do not add 27906 to GAME_START/Lv480 catch-up. Keep 27900 time, 27901 join and 27906 relive slices mutually isolated; exclude timer/UI/scene/events/red dots and 27902-05/27907-09.
- R181 Demon 18314 is an explicit-only read-only talent-power query. C2S is exactly `demons_id:u32,sign:u8,id:u32,skill_lv:u16` (`"icih"`, 17-byte client frame); S2C is `power:u32,demons_id:u32,sign:u8,skill_id:u32,skill_lv:u16,code:u32`. Match the old client: cache only `code==1`; failures never clear or overwrite a previous success. For `sign!=0`, key by demon/skill/sign/level; for `sign==0`, key by returned skill/level. Keep GAME_START unchanged and exclude 18309/10/12/13/16, UI, events and red dots. After a forced Unity domain reload, Pipeline calls may time out while the asset refresh and Unity AI Account check are still occupying the main thread; verify Editor.log/process progress and wait for the same PID to return `ready` before treating it as a hard hang.
- R515 Demon 18315 and 18317 are receive-only raw slices. 18315 is `open_state:u8`; its C2S empty request writes the daily counter, so never expose a sender or add it to GAME_START, while server reset pushes remain accepted. 18317 is S2C-only `demons_id:u32,skill_id:u32,skill_lv:u16,process:u32,is_active:u8`; upsert the whole raw record by demon/skill key, including all-zero values, without patching 18301. Keep both slices isolated from 18301/03/07/11/14/50901 and exclude Demon UI/config/events/red dots, daily-counter writes, skill activation, attributes, and 18304-06/08-10/12/13/16 operations.
- R516 Equip 15217/15219/15220/15223/15262：装备家族 GAME_START 内部顺序必须保持 `15214→15217→15220→15210(pos=1..10)→15261`；15217神装和15220共鸣位置表均为原序全量快照，保留重复项，空表loaded清旧。15219回包不回显pos，只保存最后一份`power:u32`原始试算；15223按回包`(equip_type,make_type)`、15262按回包`(pos,type,lv)`键控完整替换，奖励/战力表保序保重、空表有效，15262的`combat`保留u64全位。请求无回复不得清旧，各切片互不交叉覆盖。15202维持`old_client_unreachable` KILL；15218升阶、15221打造、15222还原均为真实资产写事务，继续DEFER；不得附带装备UI、配置、背包/货币、属性、事件、红点、Toast、乐观扣物或孤立结果handler。

- R180 PushGift 19102 is an explicit keyed detail query: C2S `gift_id:u16,sub_id:u16`; S2C `gift_id:u16,sub_id:u16,gift_name:string,end_time:u32,conditions:string,reward_list:u16×{grade_id:u16,grade_name:string,buy_cnt:u8,buy_time:u32,rewards_conditions:string,rewards:string}`. Replace only the matching composite key, retain wire order/duplicate grades, and treat an empty reward list as a loaded detail. Missing/expired gifts silently do not reply, so requests never clear cached detail. Keep GAME_START exactly `19104 -> 19101`; do not attach 19103 purchase, UI, events, red dots or popups.

- R166 JJC 28009 is an explicit empty-query full snapshot, not the 28016 live push: `errcode` is a u32 wire bit-pattern stored through unchecked int cast; retain all 14 record fields, duplicate ids and wire order; only UI may sort by time. Empty and err=-1 replies still replace/load the record slice.

- R178 BrightSea 18904 is explicit-only `auto_id:u64` and replaces an independent detail snapshot (robber fields plus reward/rob-reward ObjectLists); exclude old UI, notifications, events, red dot and reward chains.

- R179 BrightSea 18917 is an explicit-only strict-empty request with an independent full ship-status snapshot `auto_id:u64,status:u8,reward_times:u8,total_reward_times:u8`; every reply, including all-zero, replaces all four raw values. Activity-closed no-response preserves the old slice. Keep GAME_START as all-state clear followed by only 18900; do not attach the old red-dot/UI/event behavior or 18918-18920.

- R167-R179 BrightSea: GAME_START clears all BrightSea state then sends only empty 18900. 18900 main snapshot is `pic:string,pic_ver:u32,reward_times:u8,total_reward_times:u8,rob_times:u8,total_rob_times:u8,auto_id:u64,status:u8,send_list:u16×{auto_id:u64,shipping_id:u8,ser_id:u32,ser_num:u32,guild_id:u64,guild_name:string,role_id:u64,role_name:string,role_lv:u16,power:u64,sex:u8,career:u16,turn:u8,pic:string,pic_ver:u32,end_time:u32,rob_times:u8}`. Explicit-only 18901 is an independent full cruise-log snapshot `log_list:u16×{auto_id:u64,type:u8,rober_serid:u32,rober_sernum:u32,rober_gid:u64,rober_gname:string,rober_id:u64,rober_name:string,rober_power:u64,shipping_id:u8,reward/back_list/recv_list:ObjectList,time:u32}`, where ObjectList is `u16×{type:u8,type_id:u32,num:u32}`. Explicit-only 18902 is a separate ship-page state snapshot `shipping_id:u8,luckey_value:u16,reward_times:u8,total_reward_times:u8,up_times:u8,total_up_times:u8`; every packet replaces all six values, including zero. Explicit-only 18915 is a separate full server-info snapshot `treasure_mod:u8,wlv:u16,enemy:u16×{ser_id:u32,ser_num:u16,ser_name:string,world_lv:u16},un_satisfy_mod:u8,un_satisfy_wlv:u16,min_wlv:u16,un_satisfy:u16×same`; all lists retain wire order/duplicates and empty replies clear while loaded. Explicit-only 18916 is the read-only server daily-count/constant snapshot `daily_num:u16,max_bgold_num:u16`, replacing both values including zero. Explicit-only 18917 is a separate full ship-status snapshot `auto_id:u64,status:u8,reward_times:u8,total_reward_times:u8`; all-zero replies remain loaded. Ordinary 18900/18901/18902/18904/18915/18916/18917 packets never cross-clear. Do not attach 18903/18905-18914 operations or deltas, 18918-18920, shipping/reward/rob/scene/UI/config/red-dot chains, GuildHelp entry, old UI/event/derived-bool behavior, or old `startRound` derivation.

- R165 JJC 28004: wire is a standalone full snapshot `errcode:i32(32-bit wire; ReadU32 then unchecked cast int),left_num:u16,num_refresh:u32,can_buy_num:u16`; retain the absolute refresh timestamp as `uint`, and never overwrite 28001's `Num/NumRefresh`. GAME_START clears every JJC slice then sends exact empty `28004 -> 28001`; 28003 result sends exact `28004 -> 28002`.
- R173-R176 FriendInvite: 34005 help snapshot is `count:u16,reward_list:u16×{reward_id:u8,status:u8},pos_list:u16×{invitee_id:u64,pos:u8,name:string,lv:u16,career:u8,status:u8}` and 34006 is independent level-invite snapshot with the same pos item. 34012 welfare status is query-only at fixed type 3; 34008 boost snapshot is `lv:u16,total_count:u16,ObjectList(u16×{type:u8,type_id:u32,num:u32})` at fixed lv60. Preserve wire order/duplicates; empty is loaded and clears only its own slice. GAME_START exact order is `34001 -> 34012(3) -> 34005 -> 34006 -> 34008(60)`. A true level change only re-runs that sequence when sharing is open and the ordinary 340 icon is absent; a box icon does not count, and same-level/share-closed/ordinary-icon-present events send nothing. Do not attach 34002-04/07/09-11, 11301-02, UI, red dots, claims or configuration.

- R170 SnatchTreasure 65201: explicit-only strict-empty entry snapshot, never GAME_START. Wire is `belong_list:u16*{dunid:u32,score:u16,guild_id:u64,guild_name:string},territory_score:u16,have_territory:u8`; tail scalars are outside the list. Replace atomically, retain duplicate DunId/GuildId and wire order, and empty list remains loaded. 65208 stays exclusively in ActivityForeshow; do not attach 65200/65202-08, scene, battle, rewards, UI, config, sorting, or red dots.

- R171 Setting 11307: GAME_START clears only the wx-subscription slice then sends the strict empty request. Reply is raw `res:u8`; preserve raw and derive enabled only as `res==1`, replacing every packet. Do not attach 11305/11306/11308, SDK, UI, events, red dots or config.

## KfSingleRank 50701/50702/50703 (R142/R144/R145)

- GAME_START clears main, 50703 area-top, and 50702 area-tower state, then sends parameterless 50701 once; server owns the rank-dungeon open gate. Normal 50701/50702/50703 packets replace only their own snapshot and must not clear the other caches. 50702 and 50703 are explicit `area_id:u8` requests with independent ordered per-area full snapshots; preserve duplicates and empty snapshots. Do not hardcode a 460 level catch-up or bind role updates; exclude 50704/05, UI, scene, sorting, config, red dots, and auto-fight.

## MonBook 44201/44205/44207 (R138/R139/R143)

- 44201 is on-demand `type:u16` and caches per-Type full groups/pictures/combat, preserving wire order and duplicates; Type0/combat0 are valid. 44205 is an on-demand parameterless full snapshot: `pic_list:u16×pic_id:u32`, with no tail. 44207 is an on-demand preview cache. Do not bind GAME_START, config-type traversal, bag-finish, 44202-04/06, config, decomposition, red dots, or UI.

## Deposit 19201/19206/19208 (R133/R140/R141)

- GAME_START sends only parameterless 19201. Its complete snapshot is `day_coin:u32,onhook_coin:u32,activities:u16×{module_id:u16,sub_module:u16,select_time:u32,behaviours:u16×{behaviour_id:u16,select_time:u32,times:u16}}`; both list levels have no tail fields. Preserve wire order and duplicates, atomically replace on every reply, and clear on an empty list. Fields stay `DayCoin` then `OnhookCoin`; do not copy the old setter's reversed arguments. 19206 is an on-demand full record list `u16×{u16,u16,u32,u32,u16,u32}` that independently replaces Records. S2C-only 19208 is a two-u32 absolute coin snapshot: it sets HasCoins but must not mark Activities loaded (HasData). Do not attach 19202-05/07, operations, exchange, UI, config, or red dots.

## NoonParty 28503/28504/28505/28506 (R132/R134/R135/R136)

- 28503 is an on-demand, parameterless cumulative-experience scalar snapshot: `exp:u32`. Both server paths send the absolute cumulative total, so every packet replaces `TotalExp`; never add deltas. 28504 is separately on-demand and returns `low_box:u32,high_box:u32`; response/push packets replace both absolute counts together, so no local +1 and a later `(0,0)` clears old counts. 28505 is separately on-demand and carries `time:u32`, a Unix absolute Boss/treasure-monster reborn deadline; every response/push replaces `RebornDeadline`, never adds a duration. 28506 is separately on-demand and returns the activity's Unix absolute end deadline; each response replaces `EndDeadline`. Do not attach GAME_START, level/scene gates, entry/exit, 28500-02, HUD, auto-fight, UI, config, inventory, or other NoonParty behavior.

## MondaysAward 17904/17905/17907/17908 (R131/R148/R149/R153)

- 17904 is the only parameterless GAME_START task-state snapshot: `task_state:u16*{task_id:u16,state:u8}`. 17905 is an independent on-demand cross-server record snapshot: `count:u16*{server_id:u32,server_num:u16,role_id:u64,role_name:string,type:u8,pool_id:u16,utime:u32,picture:string,picture_ver:u32,career:u16,turn:u16}`. 17908 is an independent on-demand current-pool snapshot: `pool_count:u16*{id:u16,rid_count:u16,rid:u16*rid_count}`. 17907 is an independent on-demand draw-window snapshot `code:u8,draw_times:u16`; code is raw (only 1 is open) and DrawTimes is an absolute cumulative value. Each replaces only its own state, preserving duplicate RoleIds/Rids; every empty list clears old entries but remains loaded. Do not reproduce the old client’s first-17904 automatic 17907 request or attach 17900-03/06, draw/claim/personal-record operations, config, red dots, or UI.

## Kaifu 42001 (R150)

- 42001 is an explicit `type:u8` investment-state request/reply snapshot: `type:u8,cur_lv:u16,buy_time:u32,get_time:u32,login_days:u16,rewards:u16*{id:u8,got_lv:u16}`. Cache only the received Type; each full packet preserves reward wire order and duplicate IDs, and an empty reward list clears that Type while remaining loaded. Do not attach GAME_START, day/level/UI triggers, 42002 purchase, 42003 claim/Type2 refetch, investment UI, config, or red dots.

## Adventure 42701 (R155)

- 42701 is an explicit parameterless board-state snapshot: `circle:u16,location:u16,left_times:u16,throw_times:u16,free_reset_times:u16,free_throw_times:u16`; every packet is the full current absolute state. `left_times` is the server ADVEN_RESET_NUM value, not a client-side derivation. Do not attach GAME_START, day/level hooks, 42700 follow-ups, 42702-06 operations, UI, config, or red dots.
- 42704 is an explicit parameterless shop snapshot: `times:u32,refresh_cost:ObjectList,goods:u16×{id:u16,type:u8,reward:ObjectList,show_price:u32,price:u32,over:u8,state:u8}`. Every packet replaces the complete state; retain duplicate objects and IDs and ObjectList wire order, while exposing goods in the old-client-compatible reversed wire order. Do not attach GAME_START, day hooks, 42700/01 follow-ups, operations, UI, config, or red dots.

## HolyBattle 21801/21804/21805/21807/21808/21809/21810/21811/21813 (R130/R146/R147/R151/R152/R154/R186/R187/R188)

- GAME_START requests 21801 then 21805; 21804, 21808, 21811, and 21813 remain on-demand only. 21807 is S2C-only personal fight state `point:u16,single_rank:u16,group_rank:u8,anger:u8,anger_end:u32,buffs:u16*{attr_id:u16,value:u32}`: no client request, it replaces only its own scalars/buff list and never updates 21805 Point. 21801 is the parameterless world snapshot: `mod:u8,status:u8,end_time:u32,servers:u16*{server_id:u32,server_num:u32,server_name:string(u16 UTF8),level:u32}`. 21804 is the independent waiting-scene absolute cumulative experience snapshot `all_exp:u64`, never locally added. 21805 is the independent full score snapshot `point:u32,rewards:u16*{stage:u16,status:u8}`; requested replies and GM repairs replace Point and the ordered reward table atomically, and an empty table clears old rewards. 21808 is the independent full record snapshot `groups:u16*{group_id:u8,tower_num:u8,point:u32,rank:u8,roles:u16*{role_id:u64,rank:u8,server_id:u32,server_num:u32,name:string,point:u32,kill:u16,assists:u16}}`; group wire order and duplicate IDs are preserved, while each role list is point-descending with stable equal-point wire order. Empty groups clear old stats. 21809 is S2C-only death info `role_name:string,role_id:u64,lv:u16,power:u64,picture_ver:u32,picture:string,anger:u32,server_id:u32,career:u8,turn:u8`: every packet fully replaces its independent raw snapshot, and all-zero/empty fields are valid loaded state. 21810 is S2C-only result info `res:u8,groups:u16*{group_id:u8,tower_num:u8,point:u32},my_group_id:u8,my_rank:u8`: every packet fully replaces its own result snapshot, preserving group wire order and duplicate group IDs; an empty list clears old groups while remaining loaded. 21811 is the independent absolute phase snapshot `status:u8,end_time:u32`; status remains raw 0/1/2 and EndTime is a Unix deadline, not a duration. 21813 is the independent raw monster-dictionary delta `count:u16*{mon_auto:u32,mon_cfg_id:u32,hp:u32,hp_all:u32,group_id:u8}`: upsert by cfg id, remove only known cfg ids on `hp=0`, and an empty list remains loaded without clearing old entries. Normal packets do not clear one another. Exclude 21802/03/06/12, all 21809 request/revive UI/anger event/scene/config/UI/red-dot linkage, all 21810 requests/result UI/reward config or mail/buff-clear skills/automatic leave/21803/scene linkage, 21813 scene/UI/auto-fight linkage, anger skills/red dots/config, and all other operations.

## Eternity 27900 (R129)

- 27900 is the parameterless time snapshot `open_time:u32,enter_time:u32,end_time:u32`; same-number replies/pushes replace all three fields. GAME_START resets the model and sends only at level >=480. On role updates, only an exact changed level `==480` sends a catch-up request; a jump past 480 deliberately does not send, matching the old client. 27901 is independently requested only by the main view/manual refresh: `can_enter_scene:u8,join_list:u16×{scene:u32,self_server_num:u16,scene_num:u16}`. It replaces its raw ordered list (including duplicate scenes; server wire is already folded reverse), and empty remains a loaded snapshot. Do not attach 27902-27909, config, polling, UI, red dots, or operations.

## TSCrack 20401/20402/20404/20405/20407/20409/20410/20411 (R128/R485)

- GAME_START still sends only the empty 20411 world query. Never restore the old `status==1` fan-out to 20401/20405/20407/20409/20410 or the level-change refresh. 20401/20404/20405/20407/20409/20410 are explicit-only empty queries; 20402 is explicit `castle_id:u16` and also accepts server pushes. Requested replies and pushes replace only their own raw slice, never request another protocol, and an absent reply preserves prior state.
- 20401 is `my_value:u32,my_server_value:u32,castles:u16×Castle`; 20402 is one `Castle`; `Castle={castle_id:u16,base_server_num:u32,need_value:u32,server_num:u32,server_name:string,servers:u16×{server_num:u32,server_name:string,value:u32},roles:u16×{server_num:u32,role_name:string,value:u32,is_occupy:u8},role_num:u16,provide_num:u16}`. 20401 atomically replaces its ordered main list; 20402 upserts only its independent detail bucket by castle id.
- 20404=`activities:u16×{module_id:u16,sub_module_id:u16,value:u32}`; 20405=`value:u32,total_value:u32,rewards:u16×{stage:u8,status:u8}`; 20407=`goals:u16×{goal_id:u16,value:u32,status:u8}`; 20409=`ranks:u16×{server_num:u32,role_id:u64,role_name:string,value:u32}`; 20410=`castle_id:u32`; 20411=`status:u8,servers:u16×{server_num:u32,server_name:string,level:u16}`. Preserve wire order and duplicates; empty lists are loaded replacements, zero/max scalars are real values, and all eight slices are mutually isolated. Exclude 20400 errors tied to deferred operations, 20403 station/teleport, 20406/20408 reward claims, UI, config, sorting, red dots, rewards, scenes, and other operations.

## GhostWalk 20601 (R127)

- 20601 is the empty GAME_START request and full snapshot `state:u8,etime:u32,ser_mod:u8,group_id:u32,servers:u16*{ser_id:u16,ser_num:u16,name:string,open_day:u16,world_lv:u16},avg_wlv:u16`. `avg_wlv` is the global tail outside the list; same-number replies/pushes replace the entire snapshot and an empty list clears old servers. Do not auto-request 20602 after 20601. No 20600/20602-20605, UI, config, red dots, or operations.

## NineSky 13500 / 13503 / 13504 (R126 / R184 / R185)

- 13500 is the parameterless GAME_START snapshot. Wire is `state:u8,left_time:u32,mod:u32,group_id:u32,servers:u16*{server_id:u64,server_num:u64,name:string,world_lv:u64},avg_lv:u64`; `avg_lv` is the global tail after the list, not an entry field. 13503 is the explicit/on-push battle panel described above. 13504 is an S2C-only raw holder snapshot and must not expose a request method. Same-number replies/pushes replace only their own complete slice; empty server lists, empty first-player strings, and zero-role/empty holder names clear the corresponding old value without clearing the slice's `Has*` flag. Keep all three slices bidirectionally isolated. Do not attach the remaining 13501-02/05-10, UI, configuration, red dots, scenes, rewards or operations.

## Guard 21601 (R125)

- 21601 is an on-demand, parameterless authoritative circle snapshot; it is deliberately not a GAME_START request. Same-number responses/pushes atomically replace the ordered list, including empty-list clear. Do not attach 21600, 21602-21606, maintenance, operations, dialogs, scene appearance, config, red dots, UI, or resources.

## Achievement 40901/03/06/08 (R124)

- GAME_START sends four empty frames in order 40901->40903->40906->40908. Each reply owns an independent full snapshot (stage rewards, achievement entries, scalar star, type stars); all lists replace atomically and empty lists clear old values. Do not attach 40900/02/04/05/07/09, configuration derivation, red dots, UI, or reward operations.

## Revelation 28606/28609 (R123/R137)

- 28606 is the parameterless full main-state snapshot: raw figure IDs/power plus ordered gathering, suit, and skill lists. Every packet atomically replaces all fields and empty lists clear prior state. 28609 is an on-demand `power:u64` refresh: ignore it before a 28606 snapshot; otherwise replace only Power, preserving every other field/list. Do not attach 28600-05/07/08, configuration, red dots, bag, appearance resources, 3D, or UI.

## Demon 18301 / 18303 / 18307 / 18311 / 18315 / 18317 / 50901 (R119-R122/R158/R515)

- 18301 is only the raw Demon entity snapshot: `open_state:u8,demons:u16*{id:u32,level:u16,exp:u32,star:u8,slot_num:u8,skills:u16*{id:u32,lv:u16,process:u32,is_active:u8},slot_skills:u16*{id:u32,lv:u16,slot:u8,quality:u8,sort:u16}}`. Every packet replaces the complete list; an empty list clears it.
- 18311 is an explicit parameterless talent-shop snapshot: `refresh_time:u32,refresh_num:u16,cost:ObjectList,shop:u16×{id:u32,goods_id:u32,price:u32,num:u16,cost_num:u16,discount:u8,can_buy_num:u16,buy_num:u16}`. Every packet replaces all fields and lists, preserves the server wire order and duplicate IDs (the server already emits reversed Goods order), and allows the valid unopened `[0,0,[],[]]` snapshot. Do not attach GAME_START, 18312/18313 actions, the 18315 daily-counter request, UI, currency, config, or red dots.
- Current Unity has no DemonMainView open gate. Controlled simplification: send only the four existing base snapshots 18301/18303/18307/50901 on GAME_START; 18311 remains explicit on-demand, while 18315/18317 are receive-only. Do not add 18304-06, 18308-10/18312-13/18316, 50902, configuration, derived red dots, events, UI, resources, or 3D.
- 18303 is the independent full fetter snapshot `fetters:u16*fetter_id:u32`; GAME_START sends 18301 then 18303 as empty frames. Replace each packet atomically, dedupe repeated IDs while preserving first-seen order, and allow an empty packet to clear the list. The prior scope exclusion is narrowed only for 18303; still do not attach 18304-06, 18308-10/18312-13/18316, config, red dots, UI, or 3D.
- 18307 is the independent full painting ID snapshot `paintings:u16*painting_id:u8`; GAME_START now sends 18301->18303->18307. Deduplicate repeated IDs in first-seen order and clear on an empty packet. Do not attach 18308 claim or any other operation, config, red dots, UI, resources, or 3D.
- 50901 is an independent scalar blessing snapshot `bless_value:u32`; GAME_START now sends 18301->18303->18307->50901. Server pushes after rotary actions only replace this value and must not trigger a request. Do not attach 50902 or rotary operations, config, red dots, UI, resources, or 3D.

## Dress 11200 (R118)

- GAME_START sends four `11200 + dress_type:u8` requests in the fixed order `1(Bubble) -> 2(Photo) -> 3(Foot) -> 5(Head)`; do not attach 11201-11205.
- The reply is a type-local full snapshot: `type:u8,used_dress_id:u32,enable_list:u16×{dress_id:u32,dress_lv:u16,cur_power:u64,next_power:u64}`. The U16 is the list count, not a second business field. Same type replaces (an empty list clears only that type); different types coexist. Keep it query-only: no config, wear, activation, upgrade, preview, UI, resources, or scene sync.

## TempleAwaken 42901/42902/42903/42904/42905/42909（R117/R510）
- GAME_START 顺序仍严格空发 42901→42909；42901 为章节/子章/阶段全量树，process 是 u64，空列表清旧。42900 成功后仍仅重拉 42901。
- 42902 是显式 `chapter:u16` 纯查询，回包为 `chapter:u16,status:u8,subs:u16×{sub_chapter:u16,sub_status:u8,stages:u16×{stage:u16,stage_status:u8,process:u64}}`；按 chapter 替换首个同键章节并保留 42901 已有 `is_wear`，重复子章/阶段和 wire 原序不去重，空子章表有效。章节锁定或不存在时服务器不回复，请求不得清旧。
- 42903/42904/42905 都是 S2C-only 权威增量，分别为章节状态、子章状态和阶段进度。每包先记录完整 raw 最后增量，再替换树中首个同键项，未知键按老端语义追加占位，后续 42901 全量可覆盖；42905 process 必须保留 u64。42903 后无条件补查同 chapter 的 42902，42904/05 不自动发包。普通增量不得把全量 `HasInfo` 伪造为已加载。
- 42906 章节领奖、42907 阶段领奖、42908 穿戴/脱下外形均为真实资产/外形写操作，42910 是场景进入/退出且老端控制器未注册，继续排除；不得接领奖 UI、奖励/背包、本地发奖、外形/场景、红点或乐观状态。10211 仍属于通用 `data_popup` 链，不并入本族。

## DragonBall 14311（轮101）

- `config_start_nuclear` 是龙珠礼包图标 143 的权威门槛；仅消费 id/open_lv/open_day/times_limit。当前 8 行，最低开启等级 150；1..7 限购1，8限购3。
- 14311 为 `id:u32,buy_times:u16`。显隐还必须经过 `CheckFuncOpenState("DragonBallView")` 且非 alpha；等级事件只在精确命中配置 open_lv 时重拉。
- Unity CLI `eval` 在主线程执行：专项若会 `await ResManager.LoadAsync`，必须像批处理用例一样暂置并恢复 `ResManager.EditorPreferFallback=true`，确保 AssetDatabase 兜底同步命中；且 `GetAwaiter().GetResult()` 路径中的整个 Task 必须同步完成，不能含 `Task.Yield`/Delay/等待下一帧，否则会锁死编辑器主线程，只能重启隔离 Unity。
- 轮105补14310、轮106补14303：GAME_START 严格空发14310→14303→14311；14310回包 `status:u8,power:u64` 是全量雕像总览，status=1时服务端刻意下发power=0，必须覆盖旧预期战力。14303回包为 `wear_type:u8,items:u16×{type:u8,lv:u8,power:u64,next_power:u64}`，严格按老端以type upsert，包中缺席type不清除。等级命中open_lv与跨天仍只重拉14311，首充仍零出站；不得模仿完整老面板追发14300/14306、计算红点或播放特效。
- 轮107补14300后修订边界：14310 从非1变为1时只补发一次空14300，重复active不发，仍不追14306/14311；14300回包 `items:u16×{dragon_id:u32,dragon_lv:u16,power:u64,next_power:u64}` 按老端以dragon_id upsert，服务端也会在激活/套装变化后主动推送。不得清除包中缺席项，也不据此实现升级红点或面板。

## 属性药剂 pt_217（轮102）

- 21701 的 `lv:u8` 是药剂档位（当前配置1..4），**不是角色等级**；老端只在界面档位缓存缺失时请求，没有角色升级订阅。21703 是启动/跨天的全档位请求。
- 21701/21703 单项均为 `goods_id:u32,lv:u8,current_day_count:u32,current_count:u64`。21701 替换整个档位桶；21703 按 `(lv,goods_id)` 幂等合并。跨天先清客户端缓存再空发21703，不得把历史总次数本地归零。
- 21702 成功没有本号回包，服务端随后推21701；失败才走21700。使用入口必须从 `config_attr_medicament` 派生档位，并按真实背包数、`config_attr_medicament_use_count` 日/总余量共同裁剪，不能让调用方任意传档位，也不能乐观扣包或改计数。

## OnHook 13218（轮104）

- 13218 是物品自动熔炼成功后与15024并列下发的服务端主动推送，不是挂机请求。格式为 `exp_list:u16 count × {add_exp:u16,ratio:u8}`；老端只把全部 `add_exp` 相加后覆盖 `auto_smelt_exp`，空列表覆盖为0。`ratio` 当前不入模但必须读到尾；不得因此主动请求或抢占15024，也不得污染13212快照、13215经验效率或奖励列表。
- 13217 is an explicit parameterless exp-addition snapshot: `count:u16×{type:u32,ratio:u64,end_time:u32}`. Every packet replaces the complete list, retaining wire order and duplicate types; an empty list clears old items while remaining loaded. Do not attach GAME_START, 13218, UI, red dots, config, or operations.

## Armor 14401（轮109）

- GAME_START 必须发送14401的 `stage:u8=0,type:u8=0` 基础请求。回包为 `stage_list:u16×{stage:u8,type_list:u16×{type:u8,status:u8,pos_list:u16×{gtype_id:u32,pos:u8,status:u8}}}` 全量树；每包替换并允许空包清旧，保存前按stage/type/pos升序，`gtype_id` 必须保留u32。当前只建数据地基，不按角色等级/config过滤，不启用红点/列表/14402打造。

## Medal 13400/13401/13405（R110/R114/R226/R490）

- GAME_START固定按13401→13405连续发送两个严格空包。13401回 `id:u32,stren_lv:u32,stren_exp:u32,honour:u64,power:u32,pass_layers:u32` 并逐包完整覆盖；不得把power擅自写入RoleModel。13405回 `titles:u16×{id:u32,level:u16,power:u32,is_equip:u8}`，同时包含未激活level=0项，保留wire顺序并以空表loaded清旧。13400是S2C-only raw `code:u32` 错误切片；三者互相隔离，不接配置、红点或UI。
- 13402勋章晋升会扣配置物品、写DB、改Figure/Scene/属性并触发任务、成就、礼包、神殿、活动和至尊VIP链；13403称号激活/升级会扣物、写DB、改属性，首次激活还会自动佩戴并广播场景；13404佩戴和13406卸下都会持久化称号状态并同步Role/Figure/Scene；13407按 `cost_list:u16×{goods_auto_id:u64,num:u32}` 删除背包实例、写强化等级经验、重算属性并同步场景战斗属性。五号必须随配置、背包货币、角色/场景、提示、UI和结果重拉闭环整体迁移，禁止只注册结果、裸sender、本地乐观修改或自动操作。

## Arcana 21101/21102/21103/21104（R491）

- 四号均正式KILL。21101虽被老端GAME_START空发且服务端能返回完整奥术表，但老端handler只解包即丢弃；`SkillSubView` 的“远古奥术”页签/视图和 `SkillUIModel` 的整段消费均已注释。Unity禁止恢复21101启动请求、常量、handler、模型桶、事件或UI。
- 21102升级、21103突破和21104选核心的服务端链仍会真实扣物、写DB、改技能/快捷栏/属性或场景状态，但老端全仓只有通用序列化与结果回调，没有任何可达发送点。不可因服务端写事务存活而反造客户端入口；禁止裸sender、孤立成功回执、本地扣物或21002/21101跟随重拉。

## Unreal 14900/14904/14906/14907/14908（R492）

- GAME_START先清全部幻饰原始切片，再严格依次发送14904的 `cell:u8=1..6` 六帧，最后空发14908。14904按回包cell键控完整替换 `res:u32,cell:u8,level:u16,point:u32`；14908是已解锁槽位有序全量，保留wire顺序/重复/任意raw u8，空表loaded清旧，并接受装备或进阶后的服务端同号推送。
- 14906进阶预览和14907分解预览仅允许业务显式按 `goods_id:u64` 查询；回包按goods id在各自独立字典覆盖，完整保留评分及 `color:u8,type_id:u8,attr_id:u16,attr_val:u32,plus_interval:u8,plus_unit:u32` 有序属性表。14900只保存独立raw错误码/字符串；五切片互不交叉清理，请求无回复保留旧值。
- 14901穿戴、14902卸下、14903进阶和14905强化都会修改真实装备/背包/DB/属性并触发多系统，必须随幻饰UI、配置、背包、角色属性、错误/成功提示和权威刷新整体迁移；当前禁止常量、sender、孤立结果handler、本地扣物/换装或红点推导。

## Auction 15401/15402/15407/15408/15409/15410/15411（R493）

- GAME_START先清全部拍卖原始切片，再严格发送15401参数 `(auction_type=2,type=0,module_id=0)`。15401回包仅携auction_type与商品有序全量，每个auction_type独立替换，保留重复goods_id和wire顺序，空表loaded清该类型；显式15401请求无回复时保留旧值。
- 15402是S2C-only商品更新，按 `(auction_type,goods_id)` 独立键控完整覆盖；它不得补丁15401商品表，也不得自动追发15407/15409。15407是显式 `(auction_type,module_id)` 预计分红查询并按同复合键覆盖；15408是S2C-only生命周期raw广播并按同复合键覆盖。三类字典及15401彼此隔离。
- 15409个人竞拍记录和15410分红记录均为显式严格空请求，每包分别全量替换其有序列表，保留重复项，空表仍为loaded。15411是S2C-only `all_close:u8` raw快照，不因值为1而清任何其他拍卖切片。15400目录与15404退款通知因旧端解包即丢弃正式KILL；15405/15406因服务端入口注释且旧端丢弃正式KILL；15403竞价是真实扣费事务，继续DEFER。禁止接拍卖UI、配置、红点、Toast、钱包/邮件修改、自动跟随查询或本地状态机。

## Longlang 62200/62201/62207/62208/62209（R494）

- GAME_START先清全部龙语读侧切片，再只发送62201严格空包。62201是 `equip_list:u16×{pos:u8,goods_id:u64,stren:u16}` 有序全量；每包完整替换，保留wire顺序和重复部位，空表仍loaded清旧。兼容老端按部位读取时，重复pos以wire最后一项生效；不得把缺失部位伪造成协议项。
- 62207总评分与62209当前套装表只允许页面显式空包查询；62208只允许按 `goods_type_id:u32` 显式预览。62208/09套装项真实wire均为 `suit_id:u32,num:u16`，62208尾部 `code` 真实为u32且仅 `code==1` 表示有效预览；回包不回显goods_type_id，因此只保存最后raw预览，不得虚构键控字典。所有列表保留wire顺序/重复项，空表loaded；请求无回复保留旧值。
- 62200是S2C-only `error_code:u32,error_code_args:string` 原始错误切片，只保存最后值，不弹Toast、不改其他切片。62202强化、62203穿戴、62204脱下均为真实资产/装备/DB/属性事务，继续DEFER且禁止裸sender或孤立成功回执；62205/06当前无可达wire，不得按历史注释复活。五个读侧切片互相隔离，不接配置、背包、角色属性、事件、红点、UI或本地评分/套装推导。

## Dungeon 61031/61032/61033/61034/61035/61041/61042（R504）

- 61031击杀数、61032伤害榜、61034怪物血量全表、61035波次、61041累计经验、61042额外奖励状态都只允许显式查询，不得挂GAME_START、场景、公会入口或其他生命周期。61031/32/34/35的C2S是严格空包，61041为`dun_id:u32`，61042为`dun_type:u8`；请求无回复保留旧切片。
- 61032列表保留wire顺序/重复角色，61034全表保留wire顺序/重复auto_id且空表loaded清旧，61035按返回dun_id键控，61041按返回dun_id保存真实零值，61042按返回dun_type键控并保留重复dun_id。61033是S2C-only单怪血量增量，必须镜像服务端`lists:keystore(AutoId,1,...)`：只替换首个同auto_id项，未知键追加，禁止公开请求。
- 61028批量扫荡、61043额外奖励领取、61052快速出怪、61054阶段奖励领取、61056释放临时技能、61090提交伴侣答案均为真实次数/资产/战斗/交互写操作，继续DEFER且禁止裸sender或乐观状态。61057/61060/61091/61093维持killlist裁决。不得接结社守卫/副本额外奖励UI、配置、事件、红点、场景、自动战斗、本地发奖或基于文案的派生逻辑。

## HolyTerritory 28300/28301/28302/28306/28307/28308/28309/28310/28311/28312/28313/28314/28316/28317/28318/28319（R503）

- GAME_START不得清旧态，严格按`28301(1)→28301(2)→28301(3)→28314→28316→28318`发送。28301按回包sanctuary_id键控完整覆盖，28302/28310/28314分别完整覆盖公会排行、公会成员排行和上次结算；28311按回包`(sanctuary_id,boss_id)`复合键覆盖，28312按回包sanctuary_id键控覆盖。所有列表保留wire顺序和重复项，空表loaded清本键；任何显式请求无回复都保留旧切片，尤其不得绕过服务端28302空排行错参和28308无死亡记录误投pt_460的历史行为。
- 28306/07/08/09/13/17/19是独立推送或兼容推送原始切片。28309保存最后击杀raw，并且只在同sanctuary的28301已加载时更新其首个同boss_id条目的绝对reborn_time，未知层/Boss不创建。28317积分获得不得修改28301绝对point；28319疲劳获得不得修改28318绝对fatigue。28300错误、28316首开code及全部切片互相隔离。
- 28303进入、28304退出、28305关注/取消关注均为真实场景或DB写操作，继续DEFER且不开放裸sender；28315旧端唯一发送已注释，服务端只增加每日打开计数且从不写同号回包，维持KILL/不注册。禁止接圣域UI、配置、红点、场景/复活/自动战斗、toast、奖励、资产、事件或基于增量的本地乐观推导。

## KfHolyArea 28400/28401/28403/28405/28407/28410/28411/28412/28413/28414/28415/28416/28417/28421/28422/28423（R502）

- 当前284前缀只以`mod_server`实际路由的`sanctuary_cluster2/pp_sanctuary_cluster`为事实源；同名`sanctuary_cluster/pp_c_sanctuary`是未路由旧家族，禁止用其中的28418/28419/28420实现反推当前客户端。GAME_START仍只空发28410且不清旧态；每个28410回包保存绝对活动起止时间后严格补查`28400→28405`。28411保存占领raw后只重查28400，28423保存`scene_id:u16` raw后以`u32`场景参数只重查28401。
- 28400、28401、28405分别是总览、按回包scene键控的建筑全量和个人积分/怒气/奖励状态全量；28401必须读取服务端现行尾部排行表。28403只保存最后Boss伤害raw，因为回包不回显请求scene。28412按`(scene_id,monster_id)`复合键全量覆盖，条目真实`server_num`为u32且不存在旧声明中的`c_server_msg`。28415按包完整覆盖死亡疲劳绝对时间，28422按回包`scene_id:u16`键控角色排名。所有列表保留wire顺序和重复项，空表loaded清本键；请求无回复保留旧切片。
- 28413、28416、28417分别只保存Boss刷新、Boss生死/绝对复活时间、绝对踢出截止时间raw。28421保存场景排行raw，并且只在同scene的28401已加载时替换其归属阵营和排行；早包/未知scene不创建建筑。保留既有28407兼容错误出口和28414统一错误出口。28404进入、28406付费解锁、28408归属领奖、28409积分领奖均为真实场景/资产事务，继续DEFER；active成功链从不写28406同号ACK而只重推28405，失败走28414，禁止注册伪28406回执。28418属未路由旧家族，28419当前无handler/writer，28420当前显式skip，全部KILL。禁止接玩法UI、配置派生、红点、场景/自动战斗、钱包/背包、奖励或其他写操作。

## Kf1vn 62100/62101/62103/62104/62105/62108/62109/62110/62112/62113/62116/62117/62119/62120/62123/62132/62133/62135（R501）

- GAME_START先清全部621读侧状态与入口图标，再严格空发`62101→62133`；兑换商店继续归Goods模块。62101首包只补查62100；后续仅当主`stage`变化时严格补查`62100→62104`，turn/sub_stage变化不得放大重查。62100/62101继续独立驱动既有入口图标；请求无回复保留当前切片。
- 62104等待战绩、62105资格赛对阵、62108资格赛结果、62109资格赛结算、62112擂主战对阵、62113擂主战结果、62119等待排行、62120擂主战结算、62133竞猜历史均按各自完整raw快照替换。62110/62116分别按回包`area:u8`键控资格榜/擂主榜，不同赛区共存；62117是含嵌套挑战者的竞猜有序全量。所有列表保留wire顺序/重复项和u64字段，空表loaded清本切片；62109/62113/62116/62120内的ObjectList只是展示原始值，不改背包或本地发奖。
- 62123保存个人竞猜结果raw；仅当62117已加载且`bet_result!=0`时补丁首个同`battle_id`项为`status=2,battle_result,is_bet=1,bet_result`。62135保存公共单场结果raw，并只补丁已加载62117首个同`battle_id`项；`battle_result!=0`时同时置`status=2`，不得改个人下注结果。62103/62132保留既有错误提示边界。62102报名、62118竞猜扣费、62121观战切场景、62134领奖继续DEFER；62107仅C2S退出且服务端不写同号回执，62111旧端空消费，均不得注册S2C handler。禁止赛事UI、场景/自动战斗、配置红点、乐观扣费、资产/奖励入包或其他621写操作。

## TerritoryWar / GuildFight 50600/01/03/04/06/07/11/12/17/19/20/21/22/24/25/26/27（R500）

- GAME_START不清旧态，严格空发`50600→50601→50622→50624`。50600每包完整覆盖`war_state:u8,ready_time:u32,start_time:u32,end_time:u32`，随后`war_state==1`补查50624，否则补查50621，最后总查50620。50611结算后严格查`50620→50600`；50620保存轮次后总查50621；50625保存raw资格更新、只在已有50624时覆盖qualification并保留isChoose，然后严格查`50620→50600`。请求无回复不得清旧态。
- 50604是显式空包战场完整快照：战区/结束时间/个人分后接公会、阶段、据点三张有序表，保留wire顺序/重复项，空表loaded清旧；另按完整包建立当前公会/据点字典，重复ID后项生效。50606/50607只更新字典中已知ID，未知忽略、重复delta后项生效，并保留最后raw增量；不得改写50604原始线序表。50612是个人分绝对值推送。
- 50601总览、50621对阵、50622服务器分组均为各自全量；50617召集、50619连杀、50626对阵刷新提示、50627战区提醒只保存独立raw，不接召集移动/自动战斗、击杀UI、页面重拉、Alert或自动进入。保留既有50603固定type=1进入边界，不新增离场。50602领奖、50618分配奖励、50623选战区继续DEFER；50605是服务端完成实际发奖后的提示，旧端解包即丢，正式KILL；50613虽为服务端据点归属广播，但旧端全仓从未注册且同链50607已有权威状态，也正式KILL。禁止玩法UI、配置红点、场景、奖励/邮件、乐观状态或其他写操作。

## SeaCraftDaily 18701/18703/18704/18710/18711/18712/18714/18715（R499）

- GAME_START沿旧端完整子序列且不清186/187旧态：严格发送`18600 -> 18607 -> 18615 -> 18617 -> 18624 -> 18712 -> 18654(1,1)`。18712为任务有序全量，也接受服务端进度推送；每包完整替换，保留重复task_id，空表仍loaded。普通请求无回复不得清任何旧切片。
- 18701概览、18703当前场景、18711全海域榜、18715统治公会均为显式空请求及各自有序全量；18704请求`sea_id:u8`，按回包`sea_id:u32`键控完整榜单，不同海域共存，空榜清本键。18711必须按`pt_187.erl`真实条目`sea_id:u8,pos:u8,server_num:u32,role_name:string,power:u64,num:u32`解析，旧`proto187.d.ts`多出的`c_server_msg:string`不存在，禁止读取。所有列表保留wire顺序/重复项，不复制旧UI的本地rank或排序。
- 18710是S2C-only搬砖完成次数与ObjectList，18714是S2C-only踢出raw code；只保存独立最后快照，不改背包、任务、概览、场景或UI。18702进场、18705开始搬运、18706卸砖、18707升级、18708退场、18709完成搬运、18713领奖均为真实场景/资产/任务事务，继续DEFER；尤其18705/06/07/13不得因旧端有同号handler而注册孤立ACK。禁止玩法UI、配置红点、场景跳转、资产发奖、Toast、乐观状态或自动操作。

## SeaHegemony 18600/18601/18604/18607/18608/18609/18611/18612/18614/18615/18616/18617/18618/18622/18623/18624/18625/18626/18651/18653/18654/18655/18656/18700（R498）

- GAME_START沿旧端顺序且不清旧态，严格空发`18600 -> 18607 -> 18615 -> 18617 -> 18624`后发送`18654(1,1)`；18712属于187日常族，不得借本轮加入。角色等级真变化只补查18600，不得放大成六包启动序列。18600是十字段完整快照，回包后严格补查18625，`self_level==1`时再补查18604，最后补查18656；18625继续独立驱动既有活动图标，所有请求无回复均保留旧切片。
- 18601禁卫、18604申请、18607活动、18611统计、18615霸主、18617攻守、18618阵营、18622申请限制、18624后续时间、18651特权、18653功勋、18655分布均按各自完整raw快照替换；所有数组保留wire顺序/重复项，空表loaded清本切片，禁止复制旧UI排序。18608按回包camp键控全量；18654按回包`(page_size,page_num)`复合键控全页，不同页尺寸共存。18609保留最后raw增量包并按`mon_id`增量覆盖字典，`hp=0`是有效条目而非删除，空包loaded但不清字典。
- 18612只保存结算raw与两个ObjectList；18623每包覆盖raw code，只有`code!=0`才严格补查`18607 -> 18624 -> 18625`；18626每包覆盖raw code，只有`code!=0`才补查18600。既有18614/18616/18700错误出口继续保存最后raw并维持旧提示边界，18617不接场景/门/自动战斗，18651不接配置/禁言/UI，所有切片互不交叉清理。
- 18602审批、18603申请、18605/18606任命、18610切舰、18613退场、18619进场、18620加入、18621退出、18652特权操作均会真实写DB、角色、公会、场景、资产或战斗状态，继续DEFER；18650旧端handler为空消费且没有老端sender，正式KILL。禁止裸sender、孤立ACK、乐观扣费/发奖/职位修改、自动进退场、玩法UI、配置红点、场景技能或奖励链。

## DiamondFight 13700/13701/13703/13704/13705/13708/13710/13711/13714/13716/13718/13719/13721/13722/13724（R497）

- GAME_START先清全部137读侧状态，再严格空发`13700 -> 13703 -> 13716 -> 13721`。本仓缺`config_drumwar_value`，不得硬编码开启等级；沿用既有受控简化，真等级变化时重发这四个只读查询且不清旧态。13700完整覆盖`war_state:u8,end_time:u32`：状态0/5同时把报名raw置0；状态1补查13701，其他非0/5状态补查13701和13703。13701的`is_sign:u8`只会在报名阶段1且值为1时隐藏图标，状态2..4仍显示活动进行中。
- 13703小阶段、13705等待面板、13708单场结果、13710双方命数、13714假人信息、13716赛区和13718竞猜更新通知均保存完整最后raw值。13711只允许按`war_no:u8`显式查询，各期战报独立完整替换，条目严格为`zone:u8,rank:u8,role_id:u64,server_id:u32,platform:string,platform_id:u32,role_name:string,guild_name:string,vip:u8,power:u64,career:u8`；保留wire顺序和重复项，不复制旧端UI层rank排序，空表loaded清本期。
- 13719只允许显式空包查询，保存截止时间及`action -> match`两层有序全量，保留重复action/对阵和全部u64；空表loaded。13721是GAME_START/显式空包的本人竞猜记录有序全量；13722是S2C-only单条增量，追加到已加载13721，早包也建立仅含该条的loaded表。13724保存最后胜者raw事件，并只在已加载13719中更新首个同action且包含该胜者的对阵；没有匹配项时不改13719。13718不因UI不存在而自动重查13719。
- 13704只保留既有进入结果接收/失败提示，不暴露进入请求。13702报名、13706买命、13707退出、13709激活怪物、13715用技能、13720下注和13723领奖均会真实改资产、场景、战斗或活动状态，继续DEFER；13712旧端空消费维持KILL。禁止裸sender、孤立ACK、乐观扣费/发奖、自动进入/退出/激活/战斗、配置红点、场景/UI或奖励链。

## TopPk 28100/28101/28105/28107/28111/28112/28113/28115/28117（R496）

- GAME_START严格空发`28101 -> 28105 -> 28107`；旧端`ResetData`为空，因此启动请求前不得清除任何281切片，无回复保留旧值。28101为基本信息完整快照，包含`daily_reward_counts:u16×{count:u8,state:u8}`；28105为段位奖励有序全量；28107为活动状态完整三标量，也接受服务端主动推送。空列表loaded清本切片，列表保留wire顺序和重复项。
- 28115只允许排行榜页面显式空包查询，回包为`u16×{role_id:u64,role_name:string,career:u8,power:u64,guild_name:string,platform:string,server:u16,rank_lv:u8,point:u32}`，不得采用服务端旧注释里已经失效的type/grade/star形状。28111匹配对手、28112阶段、28113结算和28117段位提升均为S2C-only最后原始事件，彼此及与查询快照隔离；28113的point是由flag解释的变化量，禁止依配置本地推段位、改写28101或发奖。
- 28102/03/06会真实领奖，28104会扣绑元购买次数，28110/14会修改匹配房间与玩家动作锁，28116会退出真实战场且没有同号回包；全部继续DEFER。禁止裸sender、孤立ACK、乐观扣费/发奖、自动匹配/取消/退出、场景/UI/config/red-dot/Toast或本地段位派生。

## RuneTreasure 41600/41601/41603/41608/41610/41612/41613/41615/41620/41621（R495）

- GAME_START先清全部416读侧状态，再严格依次请求41601(type4)、41608的(1,1)(2,1)(3,1)(1,2)(2,2)(3,2)、41610的1/2/3；开服天>=8时再请求41612的1/2/3；随后请求41608(5,1)、41613的1/2/3、41620(type5)。41601只在type4回复，两个时间字段均为u64；41608按(htype,rtype)完整替换；41610/12/13/20按htype替换。空表均是loaded清本键，请求无回复保留旧键。
- 41612条目真实wire为`server_id:u32,server_num:u32,role_id:u64,name:string,type:u8,gtyp_id:u32,goods_num:u16,time:u32,is_rare:u8`，禁止把goods_num照通用记录误读为u32。41603只保存最后原始推送，并仅在rtype=1且列表非空时用首项htype重查41608；41608仅在该htype首次出现或draw_weapon变化时重查41613；41615保存最后htype并重查41613。上述重查不得直接补丁其他切片。
- 41621是41620任务表的增量：仅修改已加载列表中的同task_id，保留原顺序和重复项；delta重复id以最后一项生效，未知id忽略，空delta不清全量。41611虽然服务端真实发送，但旧端handler为空消费，正式KILL；41602旧端无sender/handler，不复活。41604/05/06/07/09/14/22是真实扣费、物品搬移、领奖、兑换或状态写事务，继续DEFER，禁止裸sender、孤立ACK、乐观扣费/发奖及UI/配置/红点联动。

## KfStage 10200（轮111）

- GAME_START 发送10200严格空包。服务端回 `open_day:u32,server_info:u16×{server_id:u16,server_num:u16,server_name:string,world_lv:u16},modules:u16×{module_id:u16,mod:u8,avg_lv:u16,server_ids:u16×u16,next_server_ids:u16×u16}`，并在跨服分组或服务器名变化时主动重推同号。当前按完整快照替换并允许空列表清旧，只建查询数据底座；不迁老端 Cookie、ViewOrder/KfStart UI，也不接10204/10205/10208/10209。

## Reincarnation 16400（轮112）

- GAME_START 发送16400严格空包，回包为 `active_ids:u16×u32`。当前仅按包全量替换并保留服务端顺序/重复项，空列表清旧；不接16401激活、13040/13041角色转生阶段，不做配置派生 last/next、等级重拉、事件、红点、UI或角色属性修改。

## GodBeast 17300/17301/17302/17308/17309（R486）

- GAME_START 只发送17301严格空包。17301回包为 `fight_count:u8,eudemons:u16×{id:u32,state:u8,score:u32,equips:u16×{pos:u8,goods_id:u64,stren:u16,exp:u32},attrs:u16×{attr_type:u16,attr_value:u32}}` 权威全量快照，空列表清旧，保留wire顺序、重复ID和u64位型。17302是S2C-only同结构单兽更新：仅当17301已加载且存在同ID时替换首个匹配项，早包和未知ID均忽略，不创建新项、不改FightCount。
- 17308是显式强化预览，C2S为 `goods_id:u64,is_double:u8,goods_list:u16×u64`，S2C为 `goods_id:u64,stren:u16,exp:u32`，每包完整替换独立预览切片。17309是显式部分属性战力试算，C2S为 `module_id:u16,sub_module_id:u8,attrs:u16×{attr_id:u16,attr_value:u32}`，S2C为 `module_id:u16,sub_module_id:u8,combat_power:u32`；按module/sub复合键缓存，同键替换、异键共存，真实0值有效。无回复不得清旧。
- 17300错误、17301总览、17308预览和17309键控战力互不交叉清理。当前不接17303-17307、17310-17312装备/出战/扩位/强化/合成操作，不做配置排序、装备字典/GoodsModel映射、背包消耗、派生战斗数、事件、红点、UI或3D资源。

## GodCourt 23300/23301/23306/23310（R488）
- GAME_START 必须先清空神庭全部原始切片，再严格空发 `23301 -> 23306`；`EVT_ROLE_INFO_UPDATE` 只在角色等级真实变化且新等级精确等于490时补发同一序列，等级跳过490、同级事件或基础角色包未到均不得发送。
- 23301是神庭总览全量：`courts:u16×{court_id:u32,court_lv:u16,power:u64,attrs:u16×{attr_id:u16,value:u32},is_active:u8,equips:u16×{pos:u8,equip_id:u64,stage:u8},suits:u16×{stage:u8,num:u16}}`。保留所有层级的wire顺序、重复ID和u64位型；空表loaded清旧。23306是独立水晶屋全量：`reward_lv:u16,sum_num:u32,crystal_color:u8,daily_num:u32,house_lv:u16,house_exp:u16,grand_status:u16×{times:u16,status:u8}`，raw状态和重复项必须保留，空表仅清本切片。
- 23310是S2C-only单神庭完整更新，结构与23301中的Court相同；必须按 `court_id` 保存独立keyed覆盖切片，同ID替换、异ID共存、早包有效，禁止patch或重排23301的原始有序总览。23300是S2C-only `error_code:u32,error_code_args:string` 原始覆盖；四类切片互不交叉清理，不接UI、提示、配置、事件或红点。

## VIP 45000/45004/45005/45006（R489）
- GAME_START必须先清空VIP全部切片，再严格空发 `45000 -> 45004 -> 15800`；跨天只重拉同一只读子序列但不得清旧值。15901继续显式按需，15803未迁移，禁止借启动或跨天恢复旧端的15901/15803自动请求。
- 45000是完整基础快照：`vip_lv:u16,vip_exp:u32,need_exp:u32,vip_hide:u8,got_rewards:u16×u16,can_rewards:u16×u16,use_cards:u16×{card_type:u8,time:u32}`；45004是独立完整特权卡表 `cards:u16×{card_type:u8,is_temp_card:u8,is_active:u8,is_forever:u8,time:u32}`。两者保留wire顺序、重复ID和raw零/最大值，空表loaded清旧，请求无回包保留旧值。
- 45005是S2C-only激活通知，45006是S2C-only超时通知，二者均只覆盖各自最后一份 `card_type:u8,is_temp_card:u8` 原始切片，不得直接patch 45004；45006每包随后严格空查一次45004。四个450切片及15800/15801/15901互相隔离，不改Role/Figure，不接45001/02奖励、45003购买、45007领免费卡、45008隐藏VIP，也不接UI、配置、红点、提示、计时器、背包货币或本地奖励。
- 23302-23305与23307-23309会真实解锁、穿戴、升阶、强化、开水晶或领奖，必须等待背包/装备、配置、货币、奖励和UI结果闭环整体迁移；当前禁止公开sender、孤立成功handler、本地扣物或发奖。

## TopVip 45101/45102/45104/45109/45110/45111/45112（R487）

- GAME_START 固定按 `45101 -> 45102 -> 45104` 连续发送三个严格空包；角色等级或 `vip_flag` 变化只复判图标，不得重拉协议。45101是完整基础信息快照：`supvip_type:u8,supvip_time:u32,right_list:u16×{right_type:u8,data_str:string,utime:u32},charge_day:u8,today_gold:u32,is_free_protect:u8`；权益保留wire顺序/重复项，空表loaded清旧。
- 45102是技能任务全量 `stage:u8,sub_stage:u8,task_list:u16×{task_id:u16,is_finish:u8,is_commit:u8,content:string}`；45104是同Task结构的至尊币任务全量。45110/45111为各自Task子集的S2C-only变化通知：保存独立最后通知后，分别精确空查45102/45104，不直接改全量；空通知同样loaded且仍重查。45109是S2C-only空升级通知，只空查45101。
- 45112虽有服务端空查询分支，但老端没有sender；Unity只注册接收 `is_free:u8`，保存独立raw覆盖切片，不公开请求，也不与45101尾部 `is_free_protect` 合并。45101/02/04全量、45110/11最后通知和45112状态互不交叉清理；45120继续由SvipController独立持有，禁止双注册。
- 45103/45105任务领奖、45106/45107购买和45108权益领奖均会真实写状态、扣货币或发奖，必须等UI/配置/背包与结果闭环一起迁移；当前不公开sender、不接孤立成功回执，也不新增红点、弹窗、商店转发或本地奖励。

## Designation 41101/41104/41105/41107/41108（轮115/R507）

- 老端在背包初始化完成后空发41101；本端沿用 Fashion 同类迁移经验，简化为 GAME_START 空发一次（请求自身无背包参数）。回包为 `current_used:u32,items:u16×{id:u32,order:u8,end_time:u32}` 完整快照，服务端已负责清过期与特殊称号过滤，本端只全量替换、空列表清旧。R507 增加 41104/41105/41107/41108 独立原始读侧，边界以前述 R507 硬约束为准。不得与 Medal 13405 普通标题表混为一体；仍不接写操作、配置、背包数量、红点、事件、UI或场景表现。

## Mask 51101（轮116）

- GAME_START 发送51101严格空包，服务端也会在使用/取消蒙面后主动重推同号；回包仅 `mask_id:u8,end_time:u32`，每包全量覆盖。当前只是数据地基，不代表蒙面表现完成：不接51102取消操作，不写 Role/Figure，不做 Scene 广播消费、变身资源、特效、提示或UI。

本仓库的 AI 编码约束统一维护在:

- [.github/copilot-instructions.md](.github/copilot-instructions.md) — 精简红线(GitHub Copilot 自动加载)
- [Docs/Shenxiao编码规范.md](Docs/Shenxiao编码规范.md) — 完整编码规范
- [Docs/Shenxiao重构实施方案.md](Docs/Shenxiao重构实施方案.md) — 整体方案与架构
- [Docs/LayaUI转换流水线.md](Docs/LayaUI转换流水线.md) — UI 主路线:粒度/烘焙/Bind/验收规矩
- [Docs/Shenxiao登录链路.md](Docs/Shenxiao登录链路.md) — yu_client→yu_gm→yu_server 链路与协议出处
- [Docs/Shenxiao进游戏链路.md](Docs/Shenxiao进游戏链路.md) — 选角/创角后 MainUI、地图、主角、NPC/怪物、弹层的阶段接管规矩

## 本机项目全局记忆

- `D:\git_res\yu_client` 是老客户端；这台电脑的主要工作是把这个老客户端重构到新客户端。老客户端用于查协议、资源、旧端行为和对照，不要默认把旧端技术债务搬到新客户端。
- `D:\git_res\yu_client_unity` 是新 Unity 客户端，也是当前准备重构和持续接管的客户端。重构时按全新客户端思路做，只保留必须兼容的资源、协议和运行时行为。
- `D:\git_res\yu_client\tools\yu-resource-tool` 是老客户端里的 Electron 资源管理项目，大部分资源管理、导出、检查、修复工作优先在这里找入口或补工具链。
- `D:\git_res\yu_server` 是服务端，主要是 Erlang 代码；服务端改动通常需要上传到服务器后编译并重启。部署前先检查 `%USERPROFILE%\.ssh\config` 的服务器 Host 信息，并检查是否有 SFTP 配置；当前已知 SSH Host 有 `aliyun`、`jzy`、`sg`，当前已知 SFTP 配置在 `D:\git_res\yu_gm\.vscode\sftp.json`。
- 读取配置表的功能不得在业务代码里补硬编码兜底；宁可让配置缺失导致功能残缺并暴露缺表，也不要把任务、引导、活动、奖励、入口、资源名等写死在代码里。需要补表现时先补真实配置/同步工具/读取器。

## Unity MCP 连接记忆

- 连接 Unity MCP 服务前，先检查是否存在残留的 Unity MCP bridge/relay 进程，重点看 `relay_win.exe`；残留桥接会占满槽位导致新连接失败。确认是僵尸桥后，直接结束该残留进程，再重新连接 Unity MCP。

## Codex 独立工作树与 Unity 性能约束（2026-07-21）

- 当前“定时迁移”是持续任务：除非用户明确喊停，或工单中的可迁移内容已全部完成，否则每完成并提交一包就直接进入下一包，不要停下来征询“是否继续”。技术问题由 Codex 自行定位、实现和验证；只有产品取舍、权限、不可逆操作或多个正确方向需要用户拍板时才提问。
- 用户日常打开和精修的 Unity 项目是 `E:\GitProject\yu_client_unity`，收口后必须常驻本地 `main`；Codex 自动迁移固定使用 linked worktree `E:\GitProject\yu_client_unity_codex`。隔离目录由提交 `40e68b1dcb836ff59d2e8dc00d5392ad622aaadd` 建立，不是普通文件夹副本；空闲或只做 Unity 验证时应 detached 在最新 `main`，避免为常驻工作树制造无意义的长期分支。
- 每轮开工、锁定 Unity 自动刷新和下发实现任务前，都必须再次核对两个 worktree 的 HEAD、分支与 `git status`。隔离目录开始写代码时才新建一次性 `codex/*` 分支；若只是验证则保持 detached，不得在 detached HEAD 上落未提交成果。外部窗口导致 HEAD 漂移时，先中止实现代理，确认工作树干净后恢复到本轮预期提交/分支，再重新下发。常驻 Editor 遇到整树切换会触发域重载，重载期间 CLI 短暂 `unreachable`/`401` 时先用 `unity status` 等待恢复，不要重复启动 Editor。
- 两个 worktree 不能同时检出 `main`。Codex 临时分支的成果必须先提交、编译验收，再合并到 `main` 并立即删除临时分支；随后让原目录重新检出 `main`，隔离目录 detached 到同一提交。用户若临时创建功能分支，也按相同规则收口，不得把一个旧功能分支继续当长期总开发分支。不要假设另一目录里的未提交修改会自动同步；同改 scene、prefab、`.meta` 或大资源前要先协调，避免二进制/序列化冲突。
- `Library/`、`Temp/`、`obj/` 等 Unity 缓存不在工作树间共享。Codex 工作树初建时没有 `Library/`；第一次打开必然进行完整导入，属于高负载操作，只能安排在用户明确空闲的时间窗口，不能为了普通代码检查擅自启动。
- 所有 Codex 任务全局最多只能有一个任务调用 Unity。用户的原目录正在运行 Unity 时，Codex 默认只做代码迁移、协议/旧端对照和静态检查，不再启动第二套 Unity；Unity 验证集中到阶段收尾，不要每个小任务启动一次。
- 确需自动运行 Unity 时，先确认其他 Codex 任务没有 Unity 进程，使用低优先级和较小的 job/background worker 数，验证完成立即退出。不得让两个 Codex 任务并行启动 Editor、AssetImportWorker 或 ShaderCompiler。
- 2026-07-21 用户已明确授权：定时迁移允许在原项目 Unity 运行时启动第二个 `yu_client_unity_codex` Unity 做编译验证，不必逐次询问。实现包至少经过隔离工作树的 Unity `-batchmode -nographics -quit` 全项目脚本编译，才能标记“编译通过”；Roslyn/`dotnet` 仅作前置快检。第二个 Unity 仍必须单实例、整个进程树设为 `BelowNormal`，不得同时跑实现子代理的重负载任务。
- 2026-07-21 晚间实证：用户同时开着 `yu_client_unity` 与 `ArtsProject` 两个交互 Editor 时，Codex 再用“全核心 + BelowNormal”启动第三个 Editor，仍可能因并发脚本编译/ILPP 把机器拖到卡死重启；`BelowNormal` 不是资源上限。此机后续批处理统一使用 `Idle`、仅绑定 16～19 四个 E 核（affinity `0xF0000`）、`-job-worker-count 2`，且代码全部定稿后再集中启动，禁止边编译边改脚本。启动前必须按命令行里的 `-projectPath` 区分主 Editor 与 AssetImportWorker，不得误杀用户两个项目的子进程。
- `CliVerify` 的入口约定与纯编译/生成器不同：必须保留图形设备，且由用例自己的 `EditorApplication.Exit` 收尾，因此运行 `Shenxiao.EditorTools.CliVerify.*` 时**不要加** `-nographics` 或 `-quit`；否则可能只完成导入就以 0 退出，实际一行 `CLIVERIFY` 都没执行。验收必须在日志里同时看到具体 `VERDICT pass=True` 与 `CLIVERIFY EXIT 0`，不能只看进程返回 0。
- Unity 启动时会清理项目自身的 `Temp/`，所以两份 TMP 字体的运行前备份**不能**放在 `Temp/CodexVerify`；应放 `%LOCALAPPDATA%\Temp` 等项目外临时目录，进程退出后恢复并用 `git hash-object` 对照运行前/HEAD。`ClientConfigSync.SyncIfStale(true)` 还可能只因行尾把 `Assets/GameRes/resource/config/client/configfunctionicon.json` 标脏；若运行前该文件干净且 `git diff` 无业务内容，按运行前版本精确恢复，不要混入提交。
- 隔离工作树自己的 `Library/` 在首次全量导入后保留且不提交，后续只做增量编译；不得与原项目复制、共享或软链接。首次 batchmode 退出会由 TMP `InitializeFontAssetResourceChangeCallBacks` 清空动态字体缓存，已观察到 `Assets/_App/Fonts/DFPYuanW7 SDF.asset` 与 `FZYHJW SDF.asset` 被改写；每次 Unity 验证后必须核对 `git status`，只还原本次进程产生的这类明确副作用，不得把字体清空结果提交。
- 2026-07-21 的只读诊断显示：单个用户 Editor 会派生 2 个 AssetImportWorker 和 3 个 ShaderCompiler，Unity 进程合计约 7.9 GB、333 个线程；全系统约 357 个进程、8241 个线程，出现过 120～180 的处理器队列和约 8.8 万次/秒上下文切换。机器是 i7-12700KF、48 GB 内存、NVMe，检查时内存、页面文件和磁盘均未耗尽；卡顿主因应先排查 Unity 并发导入/编译、Defender 扫描和后台 Chromium/WebView 进程，而不是直接归因于硬件性能不足。
- 本机网络默认路由和 DNS 经过 `TAG Wintun`、`mihomo-tag`/`tagtunnel`。物理 Realtek 网卡到路由器检查时零丢包且没有断线记录；重负载时若仅本机“断网”，优先同时记录 TUN 进程响应、网关连通和公网连通，判断代理进程是否被调度饿死。不要未经用户同意修改 Defender 排除项、网卡节能或代理优先级。
- 本仓库约有 13.5 万个受控文件并使用 Git LFS。首次 `git worktree add` 可能超过命令包装器的超时，但底层 `git`/`git-lfs` 仍会继续检出；遇到超时先检查相关进程、文件数和目录体积是否持续增长，正常增长就等待完成，不要立即重建、删除目录或重复 checkout。最终必须用 `git status`、HEAD、分支、受控文件数和 `Assets/Packages/ProjectSettings` 完整性验收。
- 这台电脑的原仓库 `E:\GitProject\yu_client_unity` 保存共享的本地 LFS 对象库；Git 历史中的 LFS pointer 是正常存储格式，但工作目录里的资源必须是展开后的真实二进制文件。2026-07-21 已验证原工作树和 Codex 工作树各有 56,036 个 LFS 路径，全部为 materialized、指针内容匹配数为 0，共享对象库约 48,463 个去重对象/5.03 GB，`git lfs fsck --objects --pointers HEAD` 通过。新建 worktree 时出现 `git-lfs` 进程只是从共享本地对象库展开内容，不代表重新初始化或从远端拿到占位引用。以后怀疑资源为指针时先看 `git lfs ls-files -l` 的状态标记并执行 `git lfs fsck`，不要直接重拉或覆盖资源。
- 诊断还发现近 18 天存在 7 次无蓝屏代码的意外关机记录，以及三条不同型号的 16 GB 内存混插和 2023 年 BIOS。若意外关机不是用户在卡死后手动重启造成，需要单独排查内存、BIOS、电源和硬件稳定性；当前 WHEA 只有信息型厂商 CPER 记录，不能据此断言某个硬件已经损坏。
- 定时迁移的代理分工固定为“主代理总控、低成本子代理执行”：确定、重复、机械性的侦察和实现优先交给低推理强度代理；主代理只做范围裁定、协议/架构决策、diff 审核、Unity 验收和提交。子任务必须限定目录、产物和字数，禁止多个代理重复通读全仓库；实现代理不得启动 Unity，所有 Unity 操作只由主代理串行执行。
- 2026-07-21 已实测并采用官方 Unity CLI：本机二进制为 `C:\Users\FXL\AppData\Local\Unity\bin\unity.exe`（`1.0.0-beta.2`），隔离项目使用 `com.unity.pipeline@0.3.1-exp.1`，不得把 Pipeline 试装到用户日常工作的原项目。Codex 应让一个受限的隔离 Editor 常驻并通过 `unity status/list/command` 复用，避免每包重复启动 batchmode；首次安装 Pipeline 仍会触发一次完整脚本编译/ILPP，不属于轻量操作。
- Unity CLI 的内建命令优先于 Roslyn `eval`：实测 `editor_status` 约 0.6 秒，热 `eval` 约 1～2 秒，冷 `eval` 可能约 9 秒。`eval` 代码必须是完整方法体片段（例如 `return UnityEditor.EditorApplication.isCompiling;`），并同时检查外层 `success` 与 `data.result.success`，因为 Roslyn 编译失败时 CLI 进程仍可能返回 0、外层仍为成功。CLI 暴露了约 140 个工具，静态查询、重编译、测试、截图和构建优先使用已有工具，不要重复造 Editor 脚本。
- 常驻 Pipeline Editor 与低成本实现代理并行时，主代理应先通过 `eval` 调用 `AssetDatabase.DisallowAutoRefresh()` 和 `EditorApplication.LockReloadAssemblies()`；实现定稿并完成 diff 审核后，再调用 `UnlockReloadAssemblies()`、`AllowAutoRefresh()` 与一次 `AssetDatabase.Refresh()`，然后等待 `editor_status` 恢复 `ready/compiling=false`。这样可避免代理边写脚本、Unity 边反复编译。Pipeline 的 Roslyn `eval` 不能直接 `await Case.Run()`；用 `_ = Case.Run(); return true;` 启动并检查日志中的 `VERDICT ... pass=True`。若完整用例接近 30 秒导致外层 CLI 超时，但日志已出现 `pass=True`，等 Editor 恢复 `ready` 后，用反射调用该 Case 唯一的 private/static/零参数/bool 验证器，并要求 CLI 内层 `data.result.success=true/result=true`；调用前临时设 `ResManager.EditorPreferFallback=true`，调用后恢复，否则立即断言异步图片时会出现假失败。传统独立 batch 才要求日志中的 `CLIVERIFY EXIT 0`。
- Pipeline `eval` 在 Unity 主线程执行，**禁止**在其中对 `ResManager.LoadAsync` / Addressables 等需要主线程继续泵帧的任务调用 `.GetAwaiter().GetResult()`、`.Result` 或 `.Wait()`，否则会形成主线程互等，外层 CLI 超时也无法中止。正确做法是第一条 `eval` 用 `_ = Xxx.EnsureLoaded(); return true;` 启动异步工作，随后用独立的轻量 `eval` 轮询 `IsLoaded` / 结果字段。若误锁死，只结束隔离项目的 Editor PID，保留 `Library`，再按 `Idle` + `0xF0000` + `-job-worker-count 2` 原参数重启；不得碰用户日常 Unity 进程。

## UI 生成/修复记忆

- UI 静态结构、背景、窗框、皮肤、尺寸、默认图片、模板、Bind 回填、Addressables 分组等生成问题，必须优先修通用 LayaUI 转换链路、默认表或回填工具，然后通过 Unity Editor 菜单重新转换/回填/分组/验收；不要直接手工改 prefab 当作最终方案。
- prefab 变更应来自通用转换器或 Unity Editor 菜单生成结果。只有用户明确要求手调，或确认是一次性验收调整时，才允许手工改 prefab，并且必须记录原因和风险。
- 业务 View/Flow 只负责旧端运行时行为: 真实数据刷新、按钮事件、动态列表/模板实例化、运行时换图、角色模型、显隐状态和协议链路。不要用业务代码硬补本该由转换器生成的静态 UI。
- 独立 item prefab 被模块 prefab 作为嵌套模板引用时，给 item 新增业务子类不能只升级模块根 prefab；必须把独立源 prefab 也交给同一 Editor upgrader 重绑，再验证模块里的嵌套模板已解析到业务组件。ListDuobao 的 `ListGoodsItem.prefab` 就是这一类。
- Laya 的 `Box`（例如 `effectGp`）转换后可能只有 `RectTransform`，不能因节点名或用途就按 `Image` 绑定；纯显隐节点用 `Transform/GameObject.SetActive`，并在交互用例里断言 `activeSelf`。嵌套模板内外存在同名节点时，查找必须限制在直属父级或模板根作用域，避免误绑到子模板。
- 宝宝铭刻四个 orphan scene（BabyImprintView/AddImprintView/ImprintItem/AddImprintItem）必须先显式 `GenerateImprintStatic` 生成 prefab/Bind，等 Unity 编译后只跑 `UpgradeImprintStatic` 回填并嵌模板；增量回归不得重复 ConvertSingle。旧 JSON 误引用 `common/texture/com_rect_btn12.png`，真实同名字节只在 old alert/H5 镜像，已按 SHA256 `83ABC71B...DDEF558` 复制到新 common 路径并按 PNG LFS 管理；静态验收需确认主 prefab 真正引用该 Sprite GUID。
- BabyImprintView/BabyForgeView 都是 628×744 且没有 close 节点，尺寸与行为表明它们是 720×992 BabyEquipView 内的子面板，不是独立模态 Window；四个铭刻 prefab 已绑定无 UIViewAttribute 的业务子类，本地 item callback 必须保持零出站。后续只能在确认装备页内层级、切换与返回关系后嵌入，不能仅因 prefab 独立就注册 ViewManager 地址。
- BabyEquipFuncView 是装备外壳：`viewGp` 同一时刻只能有一个活动的直属子页（Equip/Forge/Imprint）。子页时外壳 `closeBtn` 必须本地返回 Equip；Equip 主页时才关闭整个窗口。BabyEquipView 的 forge/imprint 入口默认不能有 Button；只能由该外壳配置本地回调后动态添加，回调只把当前槽位交给外壳换屏，严禁直接发送强化/铭刻协议。
- 18219 强化只能由 BabyForgeView 子页的 `lvBtn` / `stageBtn` 发起，主页 forge 入口只能换屏。按钮必须按当前 Level/Stage 模式、实时 Preview、库存和有效装备实例决定可用性；确认时冻结槽位、装备 Id、材料指纹与状态版本，回调复核后才发包，pending 期间两按钮都禁用，18219 成功或失败事件都必须清 pending。
- 发现页面背景透明、窗框缺失、按钮皮肤/列表模板/九宫格/图片尺寸不对时，先归因为转换器、资源映射、默认皮肤、Bind 或运行时加载链路，优先找共性修复；避免逐页精修。

## 协议迁移补充记忆

## DragonWhisper 65101 / 65106（R160/R161）

- 65101 是龙语秘境主面板的显式空包请求和完整 S2C 快照：`left_count:u8, all_count:u8, map_lists:u16×{map_id:u8,role_num:u16,mon_list:u16×{mon_id:u32,reborn_time:u32}}`。每包完整替换，必须保留服务端 map/monster 原始顺序和重复 ID，空列表清旧；不得绑定 GAME_START、等级/开放门、VIP、UI、场景、配置、红点或 65102-65107。
- 65106 是显式空包请求的掉落记录完整快照：`drop_log:u16×{time:u32,server_id:u32,server_num:u32,role_id:u64,name:string,boss_id:u32,goods_id:u32,num:u32,rating:u32,equip_extra_attr:u16×{color:u8,type_id:u8,attr_id:u16,attr_val:u32,plus_interval:u8,plus_unit:u32},is_top:u8}`。保留记录与属性原始顺序/重复项，空表清旧且 loaded；与 65101 双向隔离，仅 Reset/Dispose 同时清空。

## TreasureMap 20303（R162）

- 20303 是藏宝图开奖记录的显式严格空包请求，S2C 为 `log_list:u16×{server_num:u32,role_id:u64,name:string,reward_list:u16×{style:u8,type_id:u32,count:u32}}`。服务端 RecordMap 的 wire 已是最新→最旧，Unity 必须按收到顺序和重复项完整替换，不得再 reverse。旧端空包只显示空提示而未清内部 record_list；Unity 仍必须将空包作为权威空快照清旧并设 `HasDrawLog=true`，以消除残留。510xx 是与 20303 无关的独立 BossRotary 前缀，本轮不得带入。

## DungeonPartner 61105 / 61106（R163）

- 两号均显式发送 `level:u8`；61105 S2C 为 `level:u8,sweep_count:u16,dun_list:u16×{dun_id:u32,score:u8}`，61106 S2C 为 `level:u8,stage_reward:u16×{score:u16,status:u8}`。61105 的 SweepCount 是跨章节全局日计数，每个05包覆盖全局值；两类列表分别按 level 完整替换、保序保重、空表仍 loaded，不同 level 与05/06相互隔离。无效 level 服务端静默无响应，客户端保留旧桶；仅 Reset/Dispose 全清。不接依赖章节配置的 GAME_START/日切遍历，也不接61107-61110、配置/UI/红点/奖励操作。

## SentientAct 24101 / 24102 / 24107（R164）

- GAME_START 无门槛固定空发24101→24107→24102。24101 是活动状态完整快照并兼具服务端主动广播，S2C 为 `state:u8,end_time:u32,mod:u32,group_id:u32,next_start_time:u32,servers:u16×{server_id:u64,server_num:u64,name:string,world_lv:u64},avg_lv:u64`；每个 state 非0包都追发24102，state=0不清旧门户。24102完整替换 `portals:u16×{portal_id:u64,x:u32,y:u32}`；24107按 wire `assist_num:u32,enter_num:u32` 完整覆盖。三slice普通包互不清，仅登录Reset/Dispose全清；24106删除门户delta及24103-05/08/09场景/操作链不在本轮。

- 宝宝装备的通用物品容器必须分开：`pos=36` 是已穿戴装备实例库，供 18205/18218 槽位里的装备实例 id 反查；`pos=37` 是待穿候选背包。二者都接收 15010/15017/15018，但使用独立存储与事件；老端登录批量请求中 36/37 均被注释，Unity 当前只主动请求 37，36 保持被动接收，未经实证不要擅自加入启动请求。182xx 槽位包与 pos36 物品包没有固定先后，UI 必须同时监听两条更新链，实例未到或 id 不匹配时先降级显示槽位包的 `GoodsTypeId`。候选变强红点严格比较 `BagGoods.Rating`（不用 `OverallRating`）：空槽或候选更高即红；红点节点是 `BabyEquipSubItem.redImg`，槽位 `BabyEquipIcon.effectGp` 只表示选中。
- 宝宝装备强化 18219 上行只有 `pos_id:u8`；回包是 `code:i,pos:u8,id:u64,type:i,stage:u16,stage_lv:u16,stage_exp:i,power:i`。服务端会自行挑选强化经验材料或按升阶配置直接扣固定 cost，客户端不发送材料列表；扣包另走 15017/15018。实际消费必须放在 Forge 子面板，并走“实时预览足够 → 列出材料名×数量 → 二次确认 → 回调再次校验同槽位/实例/消费快照 → pending 防连点 → 发 18219”的链路，不能把主页入口直接绑定到 `RequestEquipUpgrade`。
- `imprintBtn` 是铭刻 18220，上行 `pos:u8,count:u16,N×type_id:i32`，回包 `code:i,pos:u8,id:u64,type:i,skill_id:i,power:i`；协议、模型与 `config_baby_equip_engrave` 只读预览已迁，但独立选择/概率/结果 UI 尚未迁，所以按钮必须继续无 Button。每个提交的 type 都按装备颜色对应配置的 `num` 全扣，重复 type 会重复计费，ratio 累加后封顶 10000；服务端先扣料再掷概率，因此 `code=1,skill_id=0` 是“已消费但铭刻失败”，不能当协议错误，也不能回滚本地背包。材料只来自普通背包 `pos=4`，已有 SkillId 的装备禁止再次铭刻。
- 宝宝装备主页的 `forgeBtn`、`imprintBtn` 都是入口图片，禁止在主页直接消费或发送协议；18219 只能由 Forge 子面板的 `lvBtn`/`stageBtn` 执行。Forge 静态迁移固定两阶段：先 `GenerateForgeStatic` 生成 Bind，待 Unity 编译后再运行 `UpgradeForgeStatic`/`VerifyForgeStatic`。
- 18219 的只读消耗预览来自三处：`config_baby_value[8]` 定义材料固定顺序 38040031/32/33 与每件 10/50/100 经验，`config_baby_equip_stren` 用 `pos@stage@nextLv.point_con - 当前stage_exp` 算经验缺口，当前阶段满级时改读下一条 `config_baby_equip_stage.cost`。服务端按该材料顺序从普通背包 `pos=4` 取最少件数；升阶预览即使有一项不足也必须返回完整 cost，不能提前截断。
- 不得从 S2C 命令号反推 C2S。连服夺宝是已核实的非对称链：老端专用 `ListDuobaoView.ts` 发 33191，服务端 `pp_custom_act.erl` 在 `type=116` 时转入 rush treasure，成功后回 33803；Unity 必须保留 33191 请求 + 33803 独立接收。另一个通用 `CompetelistView` 直接发 33803，不代表专用夺宝页也应照抄。
- 活动入口路由必须查新客户端实际 `configcustomactivity/configfunctionicon` 键，不要只照旧端拼接规则。当前连服夺宝实际可见父入口是 `331@110`，子活动数据是 `116@0`；在通用父容器尚未接管前，专用路由只能有条件占用 `331@110`，同时保留精确键 `331@116@0`。
- 时装第二刀已核实的 wire：41305 上行是 `PosId:u8 + Count:u16 + N×{GoodsInstanceId:u64,Num:u16}`，这里必须发背包实例 id，严禁用 `type_id`；当前服务端实际只允许衣服位 `pos=1`。41313 下行是无 Code 的套装全量快照，落地前要清旧表；41314/41315 的 Code 都位于 `SuitId/ActiveNum或Lv` 之后的第三字段，不能套用“Code 总在包头”的惯例。41314 只开放 2 件/4 件两档，4 件成功时套装等级置 1。
- 时装第二刀权威配置是老端 CDN `config_fashion_pos`、`config_fashion_suit`、`config_fashion_suit_star`，分别驱动部位经验/属性、四件条件/激活属性、套装 1～10 阶；不得把条件、阶数或消耗硬编码进 View。`FashionModule.prefab` 的 Level/Suit/材料/页签/条件格都是顶层模板节点，业务 Flow 必须在 reparent 前保存模板引用，BindUpgrader 当前成功判据为 9 个业务组件。
- 41305 服务端会删除请求里给出的全部数量；客户端默认候选必须只凑当前等级的经验缺口，最后一个实例按 `ceil(剩余缺口/单件经验)` 裁量并受真实库存/u16 限制，补足后停止，绝不能默认把整堆材料全发。41315 不能只查 cost：必须逐项核对 `SuitStarRow.Conditions`；Slot 先映射 `SuitRow.Conditions`，时装取指定基础色星级，幻化 subtype 1/2 取 Star、其余取 Stage，条件不足时按钮、红点和发包都要拦截。

- 2026-07-23 宝宝家庭 18207：Controller 反转 `info_list` 后，View 再依“本人且男 / 非本人且女”落左槽；资料显示名称/血型/生日/星座，战力用 `FightingShowSmallItem`。`type=1` 补 ClientBaby.defaultAttr(1..8) 并按 ConfigItemAttr.kind=2 分栏，其他 type 标题“给予TA的加成”；父母/子女模型与伴侣资料不在 18207，不能伪造。
- 2026-07-23 宝宝孕育：消耗只读 `config_baby_value[2]` 首项（当前 `{type=2,type_id=0,num=288}`），`type=2/type_id=0` 经 `GoodsModel.GetMappingTypeId` 映射为绑定灵玉展示物品；`GestateBabyView` 复用内嵌 `_tpl_BaseAwardItem` 显示真实图标和数量。点击须先以 `RoleModel.BGold` 校验；不足按配置货币名提示且零出站，足额同次打开只发一次空包 18210 并关闭。
- 2026-07-23 神纹：老端 GAME_START 严格依次空发 18100、18105、18112；18100 回包是 `attr_list[u16×{attr_id:u8,attr_value:u32}],pos_list[u16×{pos:u8,lv:u16,next_power:u64}],combat_power:u32` 全量快照，空数组也必须清旧，`next_power` 不得截成 u32。每次 REFRESH_SERVER_TIME 均再空发 18112，**不要自行去重双触发**；18112 回包 `crucible_id:u16,start_time:u32` 落快照后无条件追发 18105，等级变化仍只重拉 18105。不得据此造倒计时或 UI。
- 2026-07-23 宝宝晒娃：`pt_182.erl read(18216, _)` 确认 18216 为严格空 C2S 包，`pp_baby.erl` 的展示处理目前注释且无业务回包，故 Controller 只提供 `RequestShowBaby()` 空包入口、不注册 S2C。培养页 `showBtn` 必须启用；旧端 `BabyCultivateView.ts:108-116` 用实例字段 `limitTime` 和服务器秒钟冷却 5 秒：首次发包后提示“世界频道晒娃成功”，冷却点击只提示“等待{剩余秒}秒后才可再次发送”，同一 View 跨 Hide/Show 不清零，Dispose 自然释放。回归以 `TimeUtil.SyncServerTime` 推进，须恢复 TimeUtil 私有时间基线，验证空 payload、冷却单帧、Hide/Show 保留及第 5 秒可再发。

任何 AI 工具(Claude Code / Cursor / Codex / Copilot 等)写代码前必须读前三份;
动 UI/转换器读流水线文档,动登录/网络读登录链路文档,动进游戏/主界面/场景接管读进游戏链路文档。
冲突时以 `Docs/Shenxiao重构实施方案.md` 为权威;实施进度与变更日志见
[Docs/Shenxiao实施进度.md](Docs/Shenxiao实施进度.md)。
- BabyForgeView 当前仅为纯显示骨架：不得添加 Button、发送协议或注册 UIView/路由；按“存在下一强化配置=Level，否则预览为升阶=Stage，否则有效装备=Max”切换，Level/Stage 仅渲染 `Preview.Costs`，`targetGp/effectGp/targetEffectGp` 固定隐藏。其 `lvGp/stageGp/maxStage` 实际挂在 `_Group1` 下，材料 `Content/Content1` 也不是可依赖的直接子节点，业务缓存必须按各自命名子树判断，不能限定 `parent == root` 或使用一层 `Find`。显示回归会从老端自动补入真实 LFS 图 `goodsicon/38040031.png`（SHA256 `ADB96DCF...2B29DB9`）与 `38040034.png`（`A97C5299...7B09D6`）。
- 2026-07-23 宝宝改名：老端入口为 BabyFamilyView 的 reName1/reName2，且仅本人 role_id 记录可见；复用同源 SettingChangeNameView，不手造 prefab。18215 上行只含 string(name)，去空白后按 ASCII=1/其他=2 的宽度校验 4-12，并以 ConfigLanguageMask 本地拦截敏感词；无效值零出站。真实 `ConfigLanguageMask.json` 已从老端迁入（SHA256 `4A14E4F7...CDEDBB4`），再次改名消耗取 `config_baby_value[7]`（当前 200）。静态 prefab 只首次由 `BabyBindUpgrader.GenerateRenameStatic` 从 SettingModule 抽取；由于 `SettingChangeNameViewBind` 有 Setting/Baby 两个业务子类，不能走通用 Fill，必须复制源 Bind 字段后保存，增量回归只运行 `UpgradeRenameStatic`/`VerifyRenameStatic`。
- 2026-07-23 宝宝家庭 child1Gp/child2Gp 的 3D 暂不接：老端 `CreateChildModel` 是 `BabyId -> config_baby_figure.resource_id/scale -> UI_MODEL_TYPE.BABY`，并播放 `show`。Unity 已有已 materialize 的 `object/child/model_child_{1011,1021,1022,1031,1032,1041,1042,1051,1052}` prefab 和对应 Addressables key，也有 `UIModelStage` 的独立实例/Dispose 生命周期；但本机只有 1011/1021/1022 的 `object/child/action/{id}/show.anim`，1031/1032/1041/1042/1051/1052 均缺少 `show`。在全量动作资源及既有动作绑定 helper 被补齐并经 Editor fallback 证实前，不得以静态模型、idle 或猜测 Controller 替代，也不得修改父母/伴侣模型。
- R98/R99 OnHook：13211=`i,i,i`、13212=`c,h,i,ObjectList,i,l,i,i,l,i`、13214=`i,i`、13215=`l/u64 exp_effect`，按 NetReader c/u8,h/u16,i/u32,l/u64 读完。13211/14 是增量，不能清空13212奖励；13215只被动接收且只更新经验效率，绝不新增请求/定时轮询。壳窗口打开只发空13212并显示缓存累计/剩余挂机时间、真实服务端经验效率和奖励项数。13216成功由服务端主动紧跟13212，客户端不得重拉，失败也不得出站。未迁：13213依赖赎回购买/背包/确认链；另有自动弹窗、全局 next_time 定时器、离线卡自动使用。
