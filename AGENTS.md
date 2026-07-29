# AGENTS.md

- Git 收口规则：隔离 worktree / `codex/*` 分支只用于开发与验收；每轮已确认成果必须在停止前合并到本地 `main`，确认提交已包含后再删除临时分支和失效 worktree。若用户主工作树存在未提交改动，不得为切换 `main` 而覆盖、暂存或代提交这些现场；应在独立干净 worktree 更新 `main`，待用户改动完成并提交后再把主工作树切回 `main`。

- 文档与经验沉淀规则：开始任务先从 [Docs/README.md](Docs/README.md) 找对应权威文档；凡新增或调整架构、公共组件、工具/资源流水线、协议/登录/进游戏主链、构建发布方式，或解决具有复用价值的疑难问题，必须在同一轮、同一提交中新增或更新技术文档/经验文档，并把新文档加入索引。已验证进度同步更新 `Docs/Shenxiao实施进度.md`；形成 AI 硬约束的决策同步更新 `AGENTS.md`，编码约定同步更新 `Docs/Shenxiao编码规范.md`。纯错字、无行为变化的机械修改可不写，但最终报告必须说明“不触发文档更新”的理由。禁止只把结论留在聊天、外部记忆或临时输出目录中。

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

- R188 HolyBattle 21810 is S2C-only result information: `res:u8,groups:u16×{group_id:u8,tower_num:u8,point:u32},my_group_id:u8,my_rank:u8`. Each packet fully replaces only this slice, preserving group wire order and duplicate IDs; an empty group list clears old groups but remains loaded. Do not expose a request or attach result UI, reward config/mail, buff/skill cleanup, automatic leave, 21803, scene hooks, or operations.
- R187 HolyBattle 21809 is S2C-only death information: `role_name:string,role_id:u64,lv:u16,power:u64,picture_ver:u32,picture:string,anger:u32,server_id:u32,career:u8,turn:u8`. Every packet, including all-zero and empty-string fields, fully replaces only this loaded raw slice. Do not expose a request or attach GameStart, scene hooks, revive UI, anger events, configuration, red dots, or operations.
- R186 HolyBattle 21813 is an explicit empty request and also accepts server pushes. S2C is `mon_list:u16×{mon_auto:u32,mon_cfg_id:u32,hp:u32,hp_all:u32,group_id:u8}`. Apply packets incrementally by `mon_cfg_id`; a matching `hp=0` deletes that entry, an unknown `hp=0` changes nothing, and an empty list is loaded but preserves the existing dictionary. A request without a reply also preserves it. Do not add it to GameStart or scene hooks, and do not attach UI, auto-fight, config, red dots, rewards, or other HolyBattle operations.
- R184 NineSky 13503 is an explicit-only battle-panel snapshot and also accepts server pushes. C2S is an exact empty frame; S2C is `cur_floor:u8,max_floor:u8,left_time:u32,kill_num:u16,score:u32,first_server_num:u16,first_player:string`. Non-NineSky scenes or a missing NineRank produce no reply, so requests without a reply preserve the prior snapshot. R185 NineSky 13504 is S2C-only: `index:u8,server_num:u16,role_id:u64,role_name:string,left_time:u32`; do not expose a Request13504 or add GameStart/scene hooks. A zero-role/no-holder packet (`server_num=0,role_id=0,role_name=""`) is a valid full overwrite while preserving its `index` and `left_time`. Keep 13500 GAME_START unchanged and the 13500/13503/13504 slices mutually isolated; do not add scene detection, UI/config, red dots, rewards, role/scene flags, or 13502/05-10 operations.
- R183 Demon 18302 is an explicit-only single-demon power query. C2S is `demons_id:u32` (10-byte client frame); S2C is `demons_id:u32,power:u32`. Cache by demon id, including a real zero value, and use `TryGetDemonPower` to distinguish zero from not loaded. Requests without a reply preserve the prior entry. Keep GAME_START exactly `18301→18303→18307→50901`; do not wire the old UI, events/red dots, or the 18304/05/10 operation-success requery chain. A Unity domain reload may transiently make `recompile_status` or the first eval return Pipeline `401 Unauthorized`; if the same PID is `ready`, retry the command instead of restarting the Editor.
- R182 Eternity 27906 is an explicit-only read-only relive/death-fatigue snapshot. C2S is an exact empty 6-byte frame; S2C is `die_times:u16,time:u32,die_time:u32,safe_time:u32`. Every reply, including all-zero values, fully replaces only this slice; a request without a reply preserves the old snapshot. Do not derive config timers or "fix" the server's historical `length([DieList])` behavior, and do not add 27906 to GAME_START/Lv480 catch-up. Keep 27900 time, 27901 join and 27906 relive slices mutually isolated; exclude timer/UI/scene/events/red dots and 27902-05/27907-09.
- R181 Demon 18314 is an explicit-only read-only talent-power query. C2S is exactly `demons_id:u32,sign:u8,id:u32,skill_lv:u16` (`"icih"`, 17-byte client frame); S2C is `power:u32,demons_id:u32,sign:u8,skill_id:u32,skill_lv:u16,code:u32`. Match the old client: cache only `code==1`; failures never clear or overwrite a previous success. For `sign!=0`, key by demon/skill/sign/level; for `sign==0`, key by returned skill/level. Keep GAME_START unchanged and exclude 18309/10/12/13/15/16/17, UI, events and red dots. After a forced Unity domain reload, Pipeline calls may time out while the asset refresh and Unity AI Account check are still occupying the main thread; verify Editor.log/process progress and wait for the same PID to return `ready` before treating it as a hard hang.

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

## TSCrack 20411 (R128)

- 20411 is the only parameterless GAME_START world snapshot: `status:u8,servers:u16*{server_num:u32,server_name:string(u16 UTF8),level:u16}`. Every reply replaces the entire ordered list and an empty list clears it. Never auto-request 20401/20405/20407/20409/20410 (or any other 204xx) after the reply or on level change; do not add UI, config, red dots, or operations.

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

## Demon 18301 / 18303 / 18307 / 18311 / 50901 (R119-R122/R158)

- 18301 is only the raw Demon entity snapshot: `open_state:u8,demons:u16*{id:u32,level:u16,exp:u32,star:u8,slot_num:u8,skills:u16*{id:u32,lv:u16,process:u32,is_active:u8},slot_skills:u16*{id:u32,lv:u16,slot:u8,quality:u8,sort:u16}}`. Every packet replaces the complete list; an empty list clears it.
- 18311 is an explicit parameterless talent-shop snapshot: `refresh_time:u32,refresh_num:u16,cost:ObjectList,shop:u16×{id:u32,goods_id:u32,price:u32,num:u16,cost_num:u16,discount:u8,can_buy_num:u16,buy_num:u16}`. Every packet replaces all fields and lists, preserves the server wire order and duplicate IDs (the server already emits reversed Goods order), and allows the valid unopened `[0,0,[],[]]` snapshot. Do not attach GAME_START, 18312/18313 actions, 18315, UI, currency, config, or red dots.
- Current Unity has no DemonMainView open gate. Controlled simplification: send only the four existing base snapshots 18301/18303/18307/50901 on GAME_START; 18311 remains explicit on-demand. Do not add 18302, 18304-06, 18308-10/18312-18317, 50902, configuration, derived red dots, events, UI, resources, or 3D.
- 18303 is the independent full fetter snapshot `fetters:u16*fetter_id:u32`; GAME_START sends 18301 then 18303 as empty frames. Replace each packet atomically, dedupe repeated IDs while preserving first-seen order, and allow an empty packet to clear the list. The prior scope exclusion is narrowed only for 18303; still do not attach 18302, 18304-06, 18308-18317, 50901, config, red dots, UI, or 3D.
- 18307 is the independent full painting ID snapshot `paintings:u16*painting_id:u8`; GAME_START now sends 18301->18303->18307. Deduplicate repeated IDs in first-seen order and clear on an empty packet. Do not attach 18308 claim or any other operation, config, red dots, UI, resources, or 3D.
- 50901 is an independent scalar blessing snapshot `bless_value:u32`; GAME_START now sends 18301->18303->18307->50901. Server pushes after rotary actions only replace this value and must not trigger a request. Do not attach 50902 or rotary operations, config, red dots, UI, resources, or 3D.

## Dress 11200 (R118)

- GAME_START sends four `11200 + dress_type:u8` requests in the fixed order `1(Bubble) -> 2(Photo) -> 3(Foot) -> 5(Head)`; do not attach 11201-11205.
- The reply is a type-local full snapshot: `type:u8,used_dress_id:u32,enable_list:u16×{dress_id:u32,dress_lv:u16,cur_power:u64,next_power:u64}`. The U16 is the list count, not a second business field. Same type replaces (an empty list clears only that type); different types coexist. Keep it query-only: no config, wear, activation, upgrade, preview, UI, resources, or scene sync.

## TempleAwaken 42901（R117）
- GAME_START 顺序空发 42901→42909；42901 为章节/子章/阶段全量树，process 是 u64，空列表清旧。
- 42900 成功后仅重拉 42901；同号推送只替换模型并发更新事件，不接领奖、UI 或配置推导。

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

## Medal 13401（轮110）

- GAME_START 发送13401严格空包。回包依次为 `id:u32,stren_lv:u32,stren_exp:u32,honour:u64,power:u32,pass_layers:u32`，每包完整覆盖；服务端也会主动重推同号，接收时只更新 MedalModel，不得回环请求或把 `power` 擅自写入 RoleModel。当前不接13400错误出口、13402-13404/13406-13407操作协议、配置、红点或UI。
- 轮114补13405后，GAME_START固定按13401→13405连续发两个严格空包。13405回 `titles:u16×{id:u32,level:u16,power:u32,is_equip:u8}`，服务端会把已拥有与未拥有（level=0）称号都放进完整列表；每包全量替换且空列表清旧。仍不接13403/04/06佩戴升级、13407强化、称号配置、红点或UI。

## KfStage 10200（轮111）

- GAME_START 发送10200严格空包。服务端回 `open_day:u32,server_info:u16×{server_id:u16,server_num:u16,server_name:string,world_lv:u16},modules:u16×{module_id:u16,mod:u8,avg_lv:u16,server_ids:u16×u16,next_server_ids:u16×u16}`，并在跨服分组或服务器名变化时主动重推同号。当前按完整快照替换并允许空列表清旧，只建查询数据底座；不迁老端 Cookie、ViewOrder/KfStart UI，也不接10204/10205/10208/10209。

## Reincarnation 16400（轮112）

- GAME_START 发送16400严格空包，回包为 `active_ids:u16×u32`。当前仅按包全量替换并保留服务端顺序/重复项，空列表清旧；不接16401激活、13040/13041角色转生阶段，不做配置派生 last/next、等级重拉、事件、红点、UI或角色属性修改。

## GodBeast 17301（轮113）

- GAME_START 发送17301严格空包。回包为 `fight_count:u8,eudemons:u16×{id:u32,state:u8,score:u32,equips:u16×{pos:u8,goods_id:u64,stren:u16,exp:u32},attrs:u16×{attr_type:u16,attr_value:u32}}` 权威全量快照，空列表清旧，`goods_id` 保留u64。当前不接17300错误出口、17302-17312养成操作，不做配置排序、装备字典/GoodsModel映射、派生战斗数、红点、UI或3D资源。

## Designation 41101（轮115）

- 老端在背包初始化完成后空发41101；本端沿用 Fashion 同类迁移经验，简化为 GAME_START 空发一次（请求自身无背包参数）。回包为 `current_used:u32,items:u16×{id:u32,order:u8,end_time:u32}` 完整快照，服务端已负责清过期与特殊称号过滤，本端只全量替换、空列表清旧。不得与 Medal 13405 普通标题表混为一体；当前不接41102-41110、配置、背包数量、红点、事件、UI或场景广播。

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
