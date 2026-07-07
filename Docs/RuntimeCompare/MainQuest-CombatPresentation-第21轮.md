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
- 对标 FightDamageManager:显示门槛=主角攻击或主角被击;damage==0 仅 闪避/免疫 有字;flag 全表 0-10(实测 flag=7 None 不飘)。
- 三套动画对标 FightFontAniType:普通(弹出→停→上飘淡出 0.7s)、暴击系(0.5→2.0 backOut 回弹 0.9s)、主角被击(红字自己头顶 0.95s);横向随机散布 ±75(end_pos_offset)。
- 位置口径与 MonsterRenderer 名牌一致(UILayer.Scene,anchored=世界像素-相机像素,每帧重算不漂移);对象池,上限 40 条复用最老。
- **呈现降级(记录)**:老端位图字体 `fight_font_*.fnt` 未转换,先用场景 TMP 字体按 flag 配色(淡金/橙金/紫/蓝/红,取自字体 png 实色)近似;字体资产转换后替换 ApplyFont 即可。

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
- 位图字体 fight_font_*.fnt → Unity 字体资产未转换(飘字美术字)。
- BOSS 大血条 MainUIHiterBigBloodView 仍是空壳(SHOW_HITER_BIG_BLOOD_VIEW/hiter_vo 链未接)。
- 受击击退位移、尸体透明淡出、结算演出动画(经验条/宝箱/倒计时)。
- TaskModule 被外因销毁的第一现场未抓到(本轮 Error 日志已布防,复现即见栈)。
