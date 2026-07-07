# 技能特效路由 + 技能CD遮罩 + 掉线/卡顿 · 第 22 轮

范围:用户三连反馈 ① 四职业技能特效乱;② 自动放技能没有 CD 倒计时/遮罩动画;③ Unity 端很卡、动不动掉线(老端同机同服无此问题)。

---

## 0. 诊断结论(全部有实录/源码证据)

### ① 技能特效乱 — pos_type 被整体忽略
- 老端 ConfigCareerSkillMovies:职业烤在 8 位技能 id 第 3 位(591x/592x/593x/594x),特效同放 `skills_effect/`,res 名前缀区分(effect_male_/effect_female_/1030000_/1040000_)。**表现落点由 particles[].pos_type 决定**(FightMovieInfo.ts:627-719):0/2=攻击者、1/3/12=每个受击者、4=受击者中心、6=攻击点。
- Unity `MainRoleAgent.PlaySkillParticleAsync` 此前**所有 particle 一律挂主角模型 root**:职业3/4 的技能是"施法(_cast,pos2)+命中(pos4/6)"双段——命中特效全糊在自己脚下、与施法特效叠加,怪物身上永远没有命中反馈 → "四个角色的特效是乱的"。
- 次因:combo 连段(59x00011…)只补发 20001 不补播表现,连段动作/特效整段缺失。

### ② 技能按钮无 CD 表现 — 整条链未实现
- 老端:释放即 `ResetSkill` → `START_SKILL_CD` → `CirCleCdView` 运行时 drawPie 黑色 0.8 透明扇形(12点顺时针 clock-wipe)+ 白字倒计时(>1s 取整/≤1s 一位小数),帧驱动;数据源 lv_data.cd(毫秒,实测技能1=3000/普攻=0);自动战斗与手动同一路径;僵直不显遮罩、CD 结束无闪光。
- Unity:MainUISkillItem 类头自述"CirCleCdView 无真实 CD 数据 → 不显示";SkillManager 只有僵直、无 CD 状态;自动战斗选技也不看 CD。

### ③ 卡 + 掉线 — 三个独立问题叠加
- **心跳僵尸循环(掉线不自愈)**:LoginController 心跳超时只会无限 "resend once"。实录 Editor-prev.log serial 7-35 **连续 29 个心跳全部 12s 超时、零回包**,连接半死(服务端进程没了/TCP 未断)客户端永远僵着——玩家感知"掉线了没反应"。老端是 has_recevie_10006 检测 → 断线重连。
- **编辑器兼容层全工程扫描(顿挫)**:ResManager 编辑器兜底 `AssetDatabase.FindAssets(文件名)` **miss 不缓存**——同一缺失 key(未转换的 idle/behit 动作、未转换特效)每次加载都按文件名("idle" 命中数百资产)全扫一遍;实录采集怪"助眠草"每次刷新都撞 `object/monster/action/15010031/idle`。战斗/采集期间几秒一卡。
- **日志栈采集(顿挫)**:GameLog.Info 走 Debug.Log,编辑器每条抓完整托管栈写 Editor.log+刷 Console,实录单场会话 12 万行。
- 另实录到心跳 rtt 8-12s 的 S2C 突发延迟窗口(与怪物模型首载/对话打开重合),上面两项修完后若仍复现再专项抓(本轮不定性)。

---

## 1. 改动

### A. 技能特效(MainRoleAgent/MonsterRenderer/SceneCombat)
- `MonsterRenderer.PlayHitParticle(ins, particle)`:受击者/落点特效挂目标怪 root(不参与 ActionVersion 门控,怪不在场静默跳过)。
- `MainRoleAgent.PlaySkill(skillId, hitMonsterIds)`:pos_type 路由——0/2→主角;1/3/12→逐个目标怪;4/6→主目标(中心/攻击点=主目标坐标,与 20001 发包同源);13→有目标同 3、无目标回落主角;5→不播(老端 default)。**已知近似**:pos2 老端定格世界坐标、本端挂模型短暂跟随(特效寿命≤3s);dir_type 未接。
- `SceneCombat`:`BuildAttackTargets` 前置(发包与表现同一份目标);combo 补发时若有表现配置同步 `PlaySkill(comboSkillId, alive)`(连段动作/特效补齐,无配置静默)。

### B. 技能 CD(SkillConfigs/SkillManager/SceneCombat/MainUISkillItem)
- `SkillConfigs.GetCdMsForLevel`:lv_data[level-1].cd(毫秒)。
- `SkillManager`:`ResetSkill/GetCdLeftMs/GetCdTotalMs`(TickCount 差值防回绕,到点自清);`GetNextCombatSkill/GetNextAutoFightSkill` 跳过 CD 中技能(普攻 cd=0 恒可用,不空转);Clear() 清 CD/僵直。
- `SceneCombat.ReleaseMainSkill` → `ResetSkill`(对标老端 FightMovieInfo 预播即进 CD;自动/手动同路)。
- `MainUISkillItem`:运行时建 Radial360/Top/顺时针黑色 0.8 遮罩 + TMP 倒计时(>1s 取整/≤1s 一位小数),Update 轮询(对标 CirCleCdView 帧驱动);CD 中点击不发。老端圆 pie→本端方形图标 radial,视觉等价;无闪光/僵直不显遮罩=老端一致。

### C. 网络/卡顿(LoginController/ResManager/GameLog)
- 心跳:连续 2 次超时(≈24s 零回包)判死链路 → `NetManager.DisconnectAsync()` → 既有 EVT_NET_DISCONNECTED → 游戏内自动重连;任一回包清零计数。
- ResManager 编辑器兜底:资产/预件双缓存加**负缓存**(miss 记空串,不再重扫);TryImport* 补图成功后 `InvalidateEditorPathCache(key)` 精确失效,不挡"稍后补图"。
- GameLog:`Application.SetStackTraceLogType(LogType.Log, None)`(SubsystemRegistration 时机),Info/Debug 不再抓栈;Warning/Error 栈保留。

## 2. 验证
- `dotnet build`(排除并行会话未完成的 PetFlow.cs 后):**本轮 10 个改动文件 0 错误**。工作树存在他人在途改动 PetFlow.cs 调用未实现的 `OutWardBaseView.SetType` → 全量构建 1 个错误,**与本轮无关**,等该会话补全即消。
- 待真机回归:① 职业3/4 打怪命中特效落在怪身上、_cast 留在自己身上;② 技能1 释放后按钮 3s 时钟遮罩+倒计时(自动战斗同样显示);③ 拔网线/杀服务端进程 ≈24s 内自动断开重连;④ 编辑器战斗/采集不再几秒一卡;Editor.log 体积明显下降。

## 3. 遗留
- pos2 世界坐标定格、dir_type 特效朝向、直线/扇形 AOE 几何(2/3)仍未接。
- 心跳 rtt 8-12s 的 S2C 突发延迟窗口未定性(修完 C 后观察)。
- PetFlow.cs(并行工作)编译错误待其作者补 `OutWardBaseView.SetType`。
