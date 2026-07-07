# 自动任务丝滑度 + 副本进场时序 · 第 23 轮

范围:用户反馈 ① 接任务后自动打怪"愣一下才去",不如老端丝滑(注:任务完成弹层 10s 倒计时是老端原设计,不动);② 进主线副本(大妖)时,应该"怪物和主角都等进场动画结束才开打",实际大妖立刻开打、主角站着挨打。

---

## 0. 诊断(实录+老端源码对照)

### ① 接任务后"愣一下"——三个叠加的起手延迟(老端都没有)
- **只锁怪不走位(主因)**:`TaskModel.ResumeCurrentTaskAutoFight`(30001 进度续跑入口)对击杀/夺物任务只做
  `TrySetNearestMonsterByType`(锁视野内怪),怪不在九宫格视野时只挂 `WaitTaskMonster` 干等,**从不走向任务点**;
  只能等 MainUITaskTeamView 10s 兜底轮询 `FindNextAutoFightTask → DoTask` 才出发 → 接完任务原地发愣数秒。
  老端对照:`TaskModel.ts:2226-2234` FindNextAutoFightTask 对主线任务**立即 DoTask**(DoTask 内含自动寻路)。
- **人为延迟**:TaskController 在 30001 后停 350ms(完成分支)/250ms(进度分支)再续跑;老端只有帮派/日常任务
  setTimeout 700ms,**主线是同步 DoTask**。
- (10s 兜底轮询=老端同款设计,保留不动。)

### ② 副本进场:大妖先动手、主角站桩
- 实录(Editor.log 92595 起):"大妖来袭"横幅冻结期内,BOSS(ins=8772)连续对玩家广播 20001 攻击——但
  **damage=0 / flag=7(无伤害)**,服务端本就不结算演出期伤害;问题纯在表现层:Unity 冻结了玩家的自动战斗
  (CombatFreeze),却**没冻结怪物的攻击动画/位移表现** → 观感=怪在打、我站桩。
- 老端对照:`BaseDungeonController.ts:2184` ShowBossBornEffect → SetAutoFight(false) + 开全屏
  DungeonFightSceneMaskView(3s setTimeout 自关,`DungeonFightSceneMaskView.ts:90-97` 关闭时 STARTAUTOFIGHT +
  SetAutoFight(true));演出视图打开期间 `FightMovieInfo.ts:547` **CURR_OPEN_VIEW 非空直接 return,不播任何战斗
  表现** → 双方看起来都"等演出结束才开打"。

## 1. 改动
- `FightController.ApplyMonsterFightVisuals / ApplyMoveAnimToMainRole`:`AutoFightModel.CombatFreeze` 期间整体
  return(怪攻击动作/受击/位移表现全冻,对标老端 CURR_OPEN_VIEW 门控);**数据层 hp/死亡照常应用**,不吞协议。
  演出期怪的攻击本就 damage=0,纯砍表现零风险。
- `TaskModel.ResumeCurrentTaskAutoFight`:锁怪失败(目标不在视野)时立刻 `DoTask(task)` 走向任务点
  (WaitTaskMonster 照挂,途中怪下发/到点二保险;WaitTaskMonster 幂等防重复挂;跨场景分支自带 3s 冷却防刷)。
- `TaskController`:30001 后续跑延迟 350/250ms → 统一 100ms(保留 epoch 去重窗防同一完成连发多条 30001 抖动;
  对齐老端"进度即续跑"观感)。

## 2. 验证
- 编译:排除并行会话在途的 PetFlow.cs 后 0 错误(PetFlow 调未实现的 OutWardBaseView.SetType,属另一会话工作)。
- 待真机回归:① 接击杀任务后(含 10s 倒计时结束后)角色应立即出发,无站桩;② 进主线副本横幅期间大妖不再挥砍、
  横幅收起后双方同时开打;③ 杀怪链间隙不出现多余站桩。

## 3. 遗留
- 主角移动本身(直线接近+撞墙滑行,无 A*)与老端寻路的路径差异未在本轮范围。
- 横幅期间遮罩贴图加载会顺延演出开始(SetupAsync await),编辑器首载明显;负缓存(第22轮)已缓解,真机资源就位后无感。
