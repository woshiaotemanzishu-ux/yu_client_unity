# 战斗表现层补齐 · 第 21 轮

范围:用户反馈"战斗很奇怪:没飘字、没技能感、怪物无受击/死亡表现、血条变化不明显、升级/任务完成弹出怪异"。对照老端(yu_client)`scene/fight` 三件套(FightDamageManager/FightFontManager/FightFont)、`Monster.DoBeAttacked/DoDead`、`TaskFinishView`、`DungeonVictoryView` 逐项排查修复。

方法:① Editor.log 实录取证(真实战斗会话 20001 S2C 全量在案);② 老端源码逐行对标;③ 修复后 `dotnet build Shenxiao.Module.Core.csproj` 0 错误。

---

## 0. 诊断结论(全部有日志/源码实证)

| 用户感知 | 根因 | 判定 |
|---|---|---|
| 没有伤害飘字 | 20001 defender.damage/damage_flag 解析后**只记日志不消费**(FightController 类头自述"本期只记录不消费");全工程无 DamageText 类 | 缺失,本轮补 |
| 怪物没受击动画 | behit 链其实已有(behit.anim 已转换、PlayBeHit 已接),但 ① engage 帧(damage=0)也空播受击(老端门槛 damage>0);② **死亡瞬间消失**(hp==0 → DeleteSceneObj 直接 Destroy,无 death 动作/尸体停留)——四只怪同帧凭空蒸发,观感=“没表现” | 门槛错 + 死亡缺失,本轮修 |
| 血条变化不明显 | 怪头顶小血条链路正常(20001→ApplyHp→fillAmount);“不明显”主因是无飘字辅助 + 伤害本来就一刀近半(engage 帧 damage=0 血条不动属服务端语义)。**主角自己被打 HUD 血条不动**(RoleHpChanged 无人订阅、主角不在 _roles,FightController 明写"只记录") | 主角血条缺失,本轮补 |
| “没技能” | 主角技能动作+粒子链已实现(SkillMovieConfigs+EffectBinder);怪物普攻 59000001 无 movie 配置不播动作——**老端行为一致**(ConfigMonsterSkillMovies 同样无此 id,GetFightSkillMovie 返回 {} → rigidity=0 → 不播),真正攻击表现在 59000002(有配置) | 非 bug,不动 |
| 任务完成弹出怪异 | 实锤死循环:任务 100060 "DoTask 完成→打开 TaskFinishView" 每 10s 重开数十分钟、无一次 30004。三个叠加缺陷:① `_loadTask` 缓存失效引用(模块被外因销毁后 Unity fake-null,SetActive 抛 MissingReference);② `_ = OpenAsync()` fire-and-forget **异常静默吞没**(无任何报错);③ 自动任务 tick 重开会 `StartTime()` **重置 10s 倒计时**,自动提交永远到不了点(新会话 100150 重开两次实录) | 三处都修 |
| 战斗胜利怪异 | 61003 结算只 Toast 一行"副本通关";老端是 DungeonVictoryView/DungeonFailureView 结算弹层。DungeonCommonModule.prefab 已转换、Bind 已生成,只是**没人接线** | 本轮接线 |
| 升级怪异 | 13003 → PlayLevelUpEffect(effect_xemlvup 真特效)+ EVT_ROLE_INFO_UPDATE 刷等级文本,与老端一致;老端另有 `SoundManager.PlaySoundEffect("upgrade")` —— 本端**整个音效系统未移植** | 特效已对标;音效属全局缺口,记 blocker |

---

## 1. 本轮改动

### 1.1 新增 `Scene/DamageFontRenderer.cs` — 伤害飘字
- 数据源:20001 defender.damage/damage_flag(不本地算伤)。
- 对标 FightDamageManager:显示门槛=主角攻击或主角被击;damage==0 仅闪避有字，免疫与 flag=7 None 静默;flag 全表 0-10。
- 三套动画对标 FightFontAniType:普通(弹出→停→上飘淡出 0.7s)、暴击系(0.5→2.0 backOut 回弹 0.9s)、主角被击(红字自己头顶 0.95s);横向随机散布 ±75(end_pos_offset)。
- 位置口径与 MonsterRenderer 名牌一致(UILayer.Scene,anchored=世界像素-相机像素,每帧重算不漂移);对象池,上限 40 条复用最老。
- **2026-08-05 已解除呈现降级**：十套 `fight_font_*` 已转为静态 TMP Bitmap 字体；flag 直接选择老端字体与 `a/b/c` 图形字，不再用普通 TMP 字体、中文前缀或顶点色模拟。20001/20028 的技能名字图也已接回，详见 [全局迁移记录](BitmapFont-全局迁移-20260805.md)。

### 1.2 `MonsterRenderer` — 死亡动作 + 尸体停留
- 新增 `NotifyKilled(ins)`:FightController 在 hp==0、DeleteSceneObj **之前**预告;数据层照常立即移除(寻怪/目标马上看不到),仅视图层走死亡路径。
- 死亡路径:出 _views → 名牌立即销毁 → 播 `death`(按需加载 clip,老端 Character.PlayAction("death"))→ 停留 2.0s(老端 UpdateStateDead fade_time)→ 回收。尸体期间锚定死亡点世界像素(主角走动尸体不跟人)。
- 切场景/断线 ClearAll 立即回收全部尸体;非击杀移除(出九宫格 12006)不受影响仍瞬删。
- 未接(记录):受击击退位移(老端 end_pos 0.1s 插值)、尸体 alpha 淡出(合成台 RT 3D 模型无统一透明通道)。

### 1.3 `FightController` — 表现消费闭环
- 受击门槛对齐老端 executeHitedAnimation:506:`damage>0 || flag==闪避` 才 PlayBeHit(engage 帧不再空播)。
- `ApplyDamageFontsAndMainRoleHp`:逐 defender 飘字(怪取在场 vo 坐标,主角取 RoleModel);主角自身 hp(攻击头 hp / defender hp,服务端新绝对值)→ `RoleModel.BattleAttr.Hp` + `EVT_ROLE_INFO_UPDATE` → MainUITopView 既有血条链(不开新路径)。值没变不发事件。
- hp==0 → `MonsterRenderer.NotifyKilled` + DeleteSceneObj。

### 1.4 `TaskFinishView` — 死循环三修
- `EnsureLoaded`:已完结的 `_loadTask` 若引用已失效(fake-null)/上次失败 → 丢缓存重载(自愈)。
- `OpenAsync`:try/catch + GameLog.Error(fire-and-forget 不再静默吞异常,下次复现能看到真因)。
- `Open`:同任务且弹层可见 → 幂等跳过(不再重置倒计时/重建奖励格;10s 自动提交能真正到点)。

### 1.5 新增 `Dungeon/DungeonResultView.cs` + `DungeonController.On61003` 接线
- 61003 → 胜利开 DungeonVictoryView(评级星 grade→3 星、奖励格 _tpl_CommonRewardItem 内嵌 EquipmentItem 真图标、点击任意处关闭)/失败开 DungeonFailureView(关闭按钮;“战力提升建议”列表未接线,隐藏)。
- 奖励经 GoodsModel.GetMappingTypeId 还原真 goods_id;加载失败自回退 Toast(不落静默)。
- 未接演出(记录):SYTweenLite 铜牌/宝箱抖动、ShowExpAni 经验条增长、_html_left_time 自动退出、result_type 分型布局、再次挑战/退出按钮链。

### 1.6 注释修正
- FightVo damage_flag 全表补至 0-10(原注释只写到 5,实测已出现 7);MonsterRenderer 类头过期的"受击/死亡只待机"描述刷新。

## 2. 验证
- `dotnet build Shenxiao.Module.Core.csproj --no-incremental -p:DefineConstants="UNITY_EDITOR"` → **0 错误**(8 个既有 Generated 警告)。
- 新文件已插 `Shenxiao.Module.Core.csproj`(编辑器占用时离线编译约定;Unity 刷新后自动纳管)。
- 待真机回归:进主线打怪场景确认 ① 飘字(普通淡金/暴击橙金放大/主角被击红字);② 怪死播 death 倒地停 2s;③ 主角被击 HUD 血条实时掉;④ 任务完成弹层 10s 自动提交不再被重置;⑤ 主线副本通关弹结算界面。

## 3. 遗留 blocker(不臆造,后续轮)
- 音效系统(SoundManager)整体未移植:升级 "upgrade"、技能、受击音全缺。
- 位图字体与技能名字图缺口已于 2026-08-05 解除；其余 blocker 不变。
- BOSS 大血条 MainUIHiterBigBloodView 仍是空壳(SHOW_HITER_BIG_BLOOD_VIEW/hiter_vo 链未接)。
- 受击击退位移、尸体透明淡出、结算演出动画(经验条/宝箱/倒计时)。
- TaskModule 被外因销毁的第一现场未抓到(本轮 Error 日志已布防,复现即见栈)。

## 2026-07-27 补充：FightingUpView 战力数字与定位精确复原

### 用户可见问题与根因

- Unity 的当前战力和绿色增量使用普通 TMP 字体叠加渐变/描边，只是早期临时近似；老端实际分别加载 `fight_up.fnt/png` 与 `fight_up2.fnt/png` 彩色 BMFont，所以字形、宽度、颜色和右对齐都不可能一致。
- Creator 把老端 Label 的左上坐标直接当成中心坐标，漏算了文本矩形的半宽/半高；现有 prefab 又仍保留转换期的 `25×29/fontSize=24` 起步值，导致主数值与增量挤在一起。
- 老端根节点使用 `centerX=0 + bottom=400`，不是固定 `centerY`。Unity 旧 prefab 位于父层中心，在不同屏幕纵横比下底距会变化。
- 老端增量执行 `y-30` 是向上移动；Unity 坐标向上为正，旧实现仍做 `anchoredPosition.y-30`，实际方向相反。

### 最终方案

- 原样纳入老端 `fight_up`/`fight_up2` 的 `.fnt + .png`，由可复用的 `BitmapFontAssetBuilder` 解析 BMFont glyph rect、bearing 与 advance，生成使用 `TextMeshPro/Bitmap Custom Atlas` 的静态 TMP 字体资产；不再用 SDF 字体模拟图片数字。
- `FightingUpView` 根节点改成底部中心锚，固定 `bottom=400`；背景保持老端 `x=0,y=0,398×95`。
- 主数值按老端 `x=119,y=33,fontSize=50`，增量按 `x=231,y=18,fontSize=50` 建树；两个 RectTransform 均使用左上锚，运行时继续按原 BMFont advance 做右边缘对齐。
- 增量动画改成 Unity `Y+30`，语义等价于老端顶部坐标 `Y-30`。
- 以上布局全部写入 `HudOverlayCombatCreator.GenerateFightingUp` 和生成后的 prefab；业务代码只更新文字和播放动画。

### 生成与验收

1. 退出 Play Mode，打开“神霄/重构 UI 生成器”，选择 `MainUI / FightingUpView(战力飘字)`，点击①生成。
2. 重新进入 Play Mode 后点②预览，检查金色当前战力、绿色 `+580` 均为原图字形；绿色增量在主数值上方并与其右边缘对齐。
3. 在实际战力变化中确认提示水平居中、距屏幕底部保持老端 400 设计单位，不再随屏幕高度漂到任务框附近。
4. 离线编译：`Shenxiao.Module.Core.csproj`、`Shenxiao.Editor.csproj` 均 0 error；最终视觉结果以生成后的 Play Mode 预览截图为准。

## 2026-07-27 补充：FightingUpView 偶发不自动关闭

- **复现条件**：战力动画完成并启动 1.8 秒关闭协程后，任务对话调用 `DialogueView.SetMainLayersVisible(false)`，把 `Window` 父层临时设为 inactive；对话结束后恢复该层。
- **根因**：Unity 在父层失活时终止子节点 Coroutine，但 `_autoClose` 仍保留非空句柄。窗口恢复后继续显示，`StartAutoClose` 又被非空守卫拦截，因此永久不关。老端使用浏览器 `setTimeout`，不受显示树和游戏 `timeScale` 影响。
- **修复**：去掉自动关闭 Coroutine，改存 `Time.unscaledTime + waitSeconds` 的绝对截止时间；正常显示时到点关闭，父层被隐藏时继续计时，恢复后的第一帧发现超时立即补关。连续战力增长仍由 `StopAutoClose/StartAutoClose` 重置截止时间，累加动画语义不变。
- **验证**：`dotnet build Shenxiao.Module.Core.csproj --no-restore` 0 error；运行态需覆盖“战力提示出现 → 立刻进入任务对话 → 对话结束”路径，确认提示不会复活并常驻。

## 2026-07-27 补充：混合角色普攻刀光延迟到收招后出现

- **现象勘误**：首次施法卡顿前移后，动作本身已不卡，但普通攻击期间基本看不到刀光，反而在攻击结束、角色停下或面板弹出的瞬间出现一次。这不是粒子速度或资源仍未预热。
- **根因**：`role/1111` 的 `idle/run` 是新动作 Prefab，`attack/skill*` 回退旧拼装模型。技能代码先把粒子挂到混合容器；容器递归查找 `root` 时先命中新 `idle` 子树。攻击期间该子树 inactive，收招切回 idle 后错误粒子才随之显示。
- **老端依据**：`FightMovieInfo.Update` 在 `past_time >= particle.start_time` 时播放粒子；职业普攻配置 `start_time=0,pos_type=2`，即动作起始立即在攻击者坐标播放，不存在收招补播语义。
- **修复**：`ReplaceableRoleModel` 公开只读 `ActiveModel`；技能播放改为等待 `PlayAsync` 完成新旧实例切换，再将动作特效及技能粒子挂到本次激活子模型。延时粒子显式捕获本次动作宿主，避免延时结束时角色已回待机又挂错。`RoleModelAssembler.PrepareRoleActions/PlayActionAsync` 同步补齐混合模型公共路径。
- **验证**：Common/Core/Editor 三工程串行离线编译均 0 error；`SceneMixDriverCase` 新增“attack 特效宿主等于当前激活旧模型”的断言。视觉验收需退出并重新进入 Play Mode，观察普攻刀光在挥刀阶段出现，收招后不再补闪。
