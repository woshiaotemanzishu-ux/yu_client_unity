using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoBrush;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene.Vo;
using Shenxiao.Module.Core.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 场景怪物/采集物渲染层(对标老客户端 Scene.CreateMonster → Monster.InitMonster,见
    /// yu_client/h5/src/scene/sceneobj/Monster.ts:140-175 + SceneObj.CreateClotheSprite)。
    ///
    /// 职责:订阅数据层 <see cref="SceneManager"/> 的 MonsterAdded/MonsterRemoved/MonsterMoved/MonsterHpChanged
    /// (12002/12007/12012 落表后触发),把真实 <see cref="MonsterVo"/> 加载成 3D 模型摆进
    /// <see cref="SceneCharacterStage"/> 合成台(与主角/NPC 共用相机/RT),位置 = (怪物世界像素 - 主角世界像素)。
    /// 本类不碰协议/字节,只消费 vo;与 <see cref="NpcRenderer"/> 同构(static flow + 一个 MonoBehaviour 驱动)。
    ///
    /// 资源来源(真实资源,绝不画假怪 / 不用 cube 占位):
    ///   模型   = object/monster/model_clothe_{monster_res}/model_clothe_{monster_res}
    ///   待机   = object/monster/action/{monster_res}/idle
    ///   其中 monster_res = 协议下发的 <see cref="MonsterVo.MonsterRes"/>(服务端 Body,非 type_id)。
    ///   对标老端 GameResPath:resource/object/monster/objs/model_clothe_${monster_res}.lh,
    ///   Unity 转换产物布局同 NPC(object/{module}/{name}/{name}),经 <see cref="ResManager"/> 加载。
    ///   例:首条打怪 type_id=10001001(转运傀儡)→ monster_res=10020103 → 既有模型/动作资源,无需造假。
    ///
    /// 名牌 + 血条(数据来自协议 vo + <see cref="MonsterConfigs"/>):
    ///   名字 = 协议 vo.Name 优先(对标老端 Monster.ts:326 name_board.SetName(this.vo.name),如实采 "转运达摩"),
    ///         空则回退 config_mon["1"] 模板名(如 "转运傀儡");两者皆空则不挂名(降级,不写假名)。
    ///   血条 = vo.Hp/vo.HpLim(12007 下发 + 12009 MonsterHpChanged 实时刷新);采集物(<see cref="MonsterVo.IsCollect"/>)
    ///         按老端 Monster.SetHpState 不显血条。名牌走最小原生 TMP/Image 屏幕跟随件(同 NpcRenderer 约定,
    ///         老端 NameBoard.lh 的 Unity 转换产物名字节点缺失,合成台 RT 也无法直接叠 2D 名牌)。
    ///   缩放 = config_mon["11"] icon_scale(对标 Monster.ts SetScale(vo.icon_scale)),缺表用合成台默认。
    ///
    /// 已知 blocker / 后续(精确写明,不臆造):
    ///   - 怪物朝向(config_mon brith_rot / 移动方向)本轮不接,用合成台默认朝向。
    ///   - 移动插值:本轮按 vo 当前坐标每帧贴位(无插值),九宫格 12008/12012 改坐标即跟随。
    ///   - 受击/死亡/攻击动作、任务怪头顶标:本轮只待机 idle,记录为后续。
    /// </summary>
    public static class MonsterRenderer
    {
        private const string ACTION_IDLE = "idle";

        private sealed class MonView
        {
            public int InstanceId;
            public MonsterVo Vo;
            public int MonsterRes;   // 当前模型资源(用于判定重复推送时是否需要重建)
            public Transform Tilt;    // SceneCharacterStage 里的配角 tilt(挂模型)
            public GameObject Model;
            public RectTransform NameplateRt; // 屏幕跟随名牌根(名字 + 血条);无可显内容为 null
            public Image HpFill;      // 血条填充(采集物为 null)
            public int Epoch;         // 清场代次:切场景/断线后过期的在途加载结果丢弃
            public bool Loaded;
            public int ActionVersion;
        }

        private static readonly Dictionary<int, MonView> _views = new Dictionary<int, MonView>();
        private static GameObject _driverGo;
        private static int _epoch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager mgr = SceneManager.Instance;
            mgr.MonsterAdded += OnMonsterAdded;
            mgr.MonsterRemoved += OnMonsterRemoved;
            mgr.MonsterMoved += OnMonsterMoved;
            mgr.MonsterHpChanged += OnMonsterHpChanged;
            EventDispatcher.On(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED, ClearAll);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnNetDisconnected);
        }

        private static void OnNetDisconnected()
        {
            // 游戏内自动重连:保留怪,等重连后 12002/12012 覆盖(避免重连闪一下);真正断开才清场。
            if (LoginController.Instance.CanAutoReconnectInGame) return;
            ClearAll();
        }

        private static async void OnMonsterAdded(MonsterVo vo)
        {
            if (vo == null) return;

            // 主线副本大妖:服务端只下发占位(type=7001 "主线副本"),真实 模型/名字/缩放 由客户端按挂机层级覆盖
            // (对标老端 Monster.InitAutoBrushMoster)。须在去重/建视图/加载模型前改 vo,后续才读到正确 res/name/scale。
            float autoBrushScale = await ApplyAutoBrushBossOverrideAsync(vo);

            // 大妖(副本 BOSS)入场「大妖来袭」表现(对标老端 ShowBossBornEffect;内部按 IsBoss + 在副本内 + 去重判)。
            BossBornEffectFlow.NotifyMonsterAdded(vo);

            // 重复推送(同实例 id 再来,如九宫格 12012 刷新):资源未变就地更新 vo + 血条,避免模型重载闪烁;
            // 资源变了(极少)才撤旧重建。
            if (_views.TryGetValue(vo.InstanceId, out MonView exist))
            {
                if (exist.MonsterRes == vo.MonsterRes && exist.Loaded)
                {
                    exist.Vo = vo;
                    RefreshHp(exist);
                    return;
                }
                RemoveView(exist);
            }

            var view = new MonView { InstanceId = vo.InstanceId, Vo = vo, MonsterRes = vo.MonsterRes, Epoch = _epoch };
            _views[vo.InstanceId] = view;
            EnsureDriver();

            // config_mon(名字/缩放)按需加载(EnsureLoaded 幂等);缺表优雅降级(见 MonsterConfigs)。
            await MonsterConfigs.EnsureLoaded();
            if (IsStale(view)) return;
            MonsterConfigs.MonCfg cfg = MonsterConfigs.Get(vo.TypeId);

            GameLog.Info("Scene",
                "monster render: ins={0} type={1} monster_res={2} cfgBody={3} pos=({4},{5}) hp={6}/{7} canAtk={8} collect={9} name=\"{10}\" iconScale={11}",
                vo.InstanceId, vo.TypeId, vo.MonsterRes, cfg?.Body ?? -1, vo.X, vo.Y, vo.Hp, vo.HpLim,
                vo.CanAttack, vo.IsCollect, DisplayName(vo, cfg), cfg?.IconScale ?? -1f);

            if (vo.MonsterRes <= 0)
            {
                GameLog.Error("Scene",
                    "monster 渲染缺 monster_res(blocker):ins={0} type={1} — 协议未下发模型资源,无法定位真实模型,不画假怪。",
                    vo.InstanceId, vo.TypeId);
                return;
            }

            string modelKey = ModelKey(vo.MonsterRes);
            GameObject prefab = await ResManager.LoadAsync<GameObject>(modelKey);
            if (IsStale(view)) return;

            if (prefab == null)
            {
                // 数据到了但模型资源缺 → 精确 blocker,不画假怪冒充。
                GameLog.Error("Scene",
                    "monster model 缺失(blocker):ins={0} type={1} key={2} — 怪数据已到但模型未转换/未入库,形象不可见。" +
                    "处置:用 神霄/资源 转该怪模型,或确认 Assets/GameRes/object/monster/model_clothe_{3} 存在。",
                    vo.InstanceId, vo.TypeId, modelKey, vo.MonsterRes);
                return;
            }

            // 帧预算:prefab 命中缓存时几十只怪的 await 同帧返回,Instantiate+名牌+Animation 会挤在一帧 → 排队限流。
            await SceneSpawnBudget.WaitTurnAsync();
            if (IsStale(view)) return;

            GameObject model = Object.Instantiate(prefab);
            // 大妖优先用 ConfigAutoBrush 的 scene_scale(对标老端 vo.icon_scale=cfg.scene_scale);否则 config_mon icon_scale。
            float iconScale = autoBrushScale > 0f ? autoBrushScale
                : (cfg != null && cfg.IconScale > 0f ? cfg.IconScale : -1f);
            Transform tilt = iconScale > 0f
                ? SceneCharacterStage.AddSceneCharacter(model, iconScale)
                : SceneCharacterStage.AddSceneCharacter(model);
            view.Model = model;
            view.Tilt = tilt;
            view.Loaded = true;
            CreateNameplate(view, cfg); // 名字(config/vo)+ 血条(非采集);无可显内容则跳过
            UpdateViewPosition(view);    // 立即摆一次位(含名牌),driver 之后每帧维持
            // BOSS(大妖)一进场就面向玩家(横幅停顿期间也正对玩家,对标老端 birth 朝向 + 战斗 SetDirection)。
            if (vo.IsBoss) FaceMonster(vo.InstanceId, RoleModel.Instance.X, RoleModel.Instance.Y);

            await PlayIdle(model, vo.MonsterRes);
            if (IsStale(view)) return; // PlayIdle 期间被清场:模型已随 tilt 销毁,放弃后续

            GameLog.Info("Scene", "monster visible: ins={0} type={1} model={2} pos=({3},{4})",
                vo.InstanceId, vo.TypeId, modelKey, vo.X, vo.Y);
        }

        /// <summary>
        /// 主线副本大妖模型覆盖(对标老端 Monster.InitAutoBrushMoster,Monster.ts:685-709):服务端只下发占位
        /// 怪 type=<see cref="AutoBrushModel.AutoBrushMonsterId"/>=7001("主线副本"),真实 模型/名字/缩放 由客户端
        /// 按当前挂机层级从 ConfigAutoBrush 决定。原地改 vo(MonsterRes/Name)并返回 scene_scale(>0 时覆盖 icon_scale);
        /// 非大妖或无命中配置则返回 0(走原 config_mon icon_scale)。
        /// </summary>
        private static async Task<float> ApplyAutoBrushBossOverrideAsync(MonsterVo vo)
        {
            if (vo.TypeId != AutoBrushModel.AutoBrushMonsterId) return 0f;

            await AutoBrushConfigs.EnsureLoaded();
            int nextLevel = AutoBrushModel.Instance.Level + 1;
            AutoBrushConfigs.BrushBossModel boss = AutoBrushConfigs.GetBrushBossModel(nextLevel);
            if (boss == null)
            {
                GameLog.Warn("Scene", "大妖模型覆盖缺配置:nextLevel={0} ins={1}(ConfigAutoBrush level/turn_boss_cfg 未命中,沿用占位)",
                    nextLevel, vo.InstanceId);
                return 0f;
            }

            if (boss.Res > 0) vo.MonsterRes = boss.Res;
            if (!string.IsNullOrEmpty(boss.Name)) vo.Name = boss.Name;
            GameLog.Info("Scene", "大妖模型覆盖:nextLevel={0} res→{1} name→\"{2}\" sceneScale={3}",
                nextLevel, vo.MonsterRes, vo.Name, boss.SceneScale);
            return boss.SceneScale;
        }

        private static void OnMonsterRemoved(int instanceId)
        {
            if (_views.TryGetValue(instanceId, out MonView view)) RemoveView(view);
        }

        private static void OnMonsterMoved(MonsterVo vo)
        {
            if (vo == null || !_views.TryGetValue(vo.InstanceId, out MonView view)) return;
            view.Vo = vo;              // SceneManager 原地改的是同一 vo,这里保持引用一致
            UpdateViewPosition(view);  // 立即跟随(driver 每帧也会维持)
        }

        private static void OnMonsterHpChanged(MonsterVo vo)
        {
            if (vo == null || !_views.TryGetValue(vo.InstanceId, out MonView view)) return;
            view.Vo = vo;
            RefreshHp(view);
        }

        /// <summary>切场景/真正断线:清掉所有怪视图(SceneManager.Clear 不逐个发 MonsterRemoved,故整清)。
        /// 当帧只做隐藏(便宜),真正的 Destroy 分帧摊开——每只怪连模型带名牌是两次 Destroy,
        /// 旧场景几十只挤在切图那一帧就是主线程尖刺。</summary>
        private static void ClearAll()
        {
            _epoch++; // 让在途加载全部过期
            if (_views.Count == 0) return;
            var doomed = new List<MonView>(_views.Values);
            _views.Clear();
            _ = DestroyViewsSlicedAsync(doomed);
        }

        private static async Task DestroyViewsSlicedAsync(List<MonView> doomed)
        {
            const int DestroysPerFrame = 8;
            for (int i = 0; i < doomed.Count; i++)
            {
                MonView v = doomed[i];
                if (v == null) continue;
                if (v.Tilt != null) v.Tilt.gameObject.SetActive(false);
                if (v.NameplateRt != null) v.NameplateRt.gameObject.SetActive(false);
            }
            int done = 0;
            for (int i = 0; i < doomed.Count; i++)
            {
                MonView v = doomed[i];
                if (v == null) continue;
                SceneCharacterStage.RemoveSceneCharacter(v.Tilt);
                DestroyNameplate(v);
                if (++done % DestroysPerFrame == 0) await Task.Yield();
            }
        }

        private static void RemoveView(MonView view)
        {
            if (view == null) return;
            SceneCharacterStage.RemoveSceneCharacter(view.Tilt);
            DestroyNameplate(view);
            view.Loaded = false;
            if (_views.TryGetValue(view.InstanceId, out MonView cur) && cur == view)
            {
                _views.Remove(view.InstanceId);
            }
        }

        /// <summary>视图是否已失效(被清场/移除/替换);在途 await 之后用它丢弃过期结果。</summary>
        private static bool IsStale(MonView view)
        {
            return view == null || view.Epoch != _epoch
                || !_views.TryGetValue(view.InstanceId, out MonView cur) || cur != view;
        }

        /// <summary>每帧由 driver 调:怪随主角移动重摆相对位(锚在地图上);怪自身移动经 MonsterMoved 改 vo 坐标。</summary>
        internal static void TickPositions()
        {
            if (_views.Count == 0) return;
            foreach (MonView v in _views.Values)
            {
                UpdateViewPosition(v);
            }
        }

        private static void UpdateViewPosition(MonView view)
        {
            if (view == null || !view.Loaded || view.Tilt == null) return;
            RoleModel role = RoleModel.Instance;
            Vector2 off = new Vector2(view.Vo.X - role.X, view.Vo.Y - role.Y);
            SceneCharacterStage.SetSceneCharacterPixelOffset(view.Tilt, off);
            UpdateNameplatePosition(view);
        }

        private static async Task PlayIdle(GameObject model, int monsterRes)
        {
            if (model == null) return;
            string actionKey = $"object/monster/action/{monsterRes}/{ACTION_IDLE}";
            AnimationClip clip = await ResManager.LoadAsync<AnimationClip>(actionKey);
            if (model == null) return; // 加载期间被销毁(Unity 重载 == null)
            if (clip == null)
            {
                GameLog.Warn("Scene", "monster idle 动作未转换,静态展示:res={0} key={1}", monsterRes, actionKey);
                return;
            }
            if (!clip.legacy)
            {
                // 非 legacy 片段无法挂到 Animation 组件播放(避免抛错);静态展示并记录(同 NPC 走 legacy 约定)。
                GameLog.Warn("Scene", "monster idle 非 legacy 片段,静态展示:res={0} key={1}", monsterRes, actionKey);
                return;
            }
            Animation anim = model.GetComponent<Animation>();
            if (anim == null) anim = model.AddComponent<Animation>();
            if (anim.GetClip(ACTION_IDLE) == null) anim.AddClip(clip, ACTION_IDLE);
            anim.Play(ACTION_IDLE);
        }

        public static void PlaySkill(int instanceId, int skillId)
        {
            if (!_views.TryGetValue(instanceId, out MonView view) || !view.Loaded || view.Model == null) return;
            _ = PlaySkillAsync(view, skillId);
        }

        public static void PlayBeHit(int instanceId)
        {
            if (!_views.TryGetValue(instanceId, out MonView view) || !view.Loaded || view.Model == null) return;
            _ = PlaySimpleActionAsync(view, "behit", true);
        }

        /// <summary>
        /// 让怪面向某像素点(战斗时面向玩家,对标老端 FightController 攻击帧 attacker.SetDirection(target-attacker))。
        /// yaw 口径与 <see cref="MainRoleAgent.Face"/> 一致(舞台坐标 x右/y下,Atan2(dx,-dy)):怪与主角共用同一
        /// 2.5D 后倾父容器(tilt),故只改模型自身 yaw、不动 tilt。由 <see cref="SceneCombat"/> 在发起攻击时调用
        /// (玩家移动后下次攻击会重新对正,无逐帧开销)。
        /// </summary>
        public static void FaceMonster(int instanceId, int targetX, int targetY)
        {
            if (!_views.TryGetValue(instanceId, out MonView view) || !view.Loaded || view.Model == null) return;
            float dx = targetX - view.Vo.X;   // 怪 → 目标(玩家)
            float dy = targetY - view.Vo.Y;
            if (dx * dx + dy * dy < 0.0001f) return; // 与目标同格:保持当前朝向
            float yaw = Mathf.Atan2(dx, -dy) * Mathf.Rad2Deg; // 与 MainRoleAgent.Face 同式(若实跑朝向反,整体 +180 或翻符号)
            Transform mt = view.Model.transform;
            Vector3 e = mt.localEulerAngles;
            mt.localEulerAngles = new Vector3(e.x, yaw, e.z); // 只改 yaw,保留美术 x/z
        }

        private static async Task PlaySkillAsync(MonView view, int skillId)
        {
            try
            {
                if (IsStale(view) || view.Model == null) return;
                await SkillMovieConfigs.EnsureLoaded();

                string actionName = SkillMovieConfigs.GetActionName(skillId);
                IReadOnlyList<SkillMovieParticle> particles = SkillMovieConfigs.GetParticles(skillId);
                if (string.IsNullOrEmpty(actionName) && (particles == null || particles.Count == 0))
                {
                    GameLog.Warn("Scene", "monster skill movie missing skill={0} ins={1}", skillId, view.InstanceId);
                    return;
                }

                int version = ++view.ActionVersion;
                if (!string.IsNullOrEmpty(actionName))
                {
                    await PrepareMonsterAction(view, actionName);
                    if (IsStale(view) || view.Model == null || version != view.ActionVersion) return;

                    await EffectBinder.AttachAction(view.Model, "monster", view.MonsterRes.ToString(), actionName);
                    if (!TryPlayMonsterAction(view, actionName, 0.08f, true))
                    {
                        GameLog.Warn("Scene", "monster skill action missing skill={0} action={1} monster_res={2}",
                            skillId, actionName, view.MonsterRes);
                    }
                }
                else
                {
                    EffectBinder.ClearTag(view.Model, "action");
                }

                PlayMonsterParticles(view, skillId, particles, version);

                float wait = Mathf.Max(GetMonsterActionLength(view, actionName),
                    SkillMovieConfigs.GetConfiguredDurationSeconds(skillId));
                if (wait > 0f)
                {
                    await Task.Delay(Mathf.RoundToInt(wait * 1000f));
                    if (!IsStale(view) && version == view.ActionVersion && view.Model != null)
                    {
                        TryPlayMonsterAction(view, ACTION_IDLE, 0.12f, false);
                    }
                }
            }
            catch (System.Exception ex)
            {
                GameLog.Warn("Scene", "play monster skill failed ins={0} skill={1}: {2}",
                    view?.InstanceId ?? 0, skillId, ex.Message);
            }
        }

        private static async Task PlaySimpleActionAsync(MonView view, string actionName, bool backToIdle)
        {
            try
            {
                if (IsStale(view) || view.Model == null) return;
                int version = ++view.ActionVersion;
                await PrepareMonsterAction(view, actionName);
                if (IsStale(view) || view.Model == null || version != view.ActionVersion) return;
                if (!TryPlayMonsterAction(view, actionName, 0.06f, true)) return;

                float wait = GetMonsterActionLength(view, actionName);
                if (backToIdle && wait > 0f)
                {
                    await Task.Delay(Mathf.RoundToInt(wait * 1000f));
                    if (!IsStale(view) && version == view.ActionVersion && view.Model != null)
                    {
                        TryPlayMonsterAction(view, ACTION_IDLE, 0.12f, false);
                    }
                }
            }
            catch (System.Exception ex)
            {
                GameLog.Warn("Scene", "play monster action failed ins={0} action={1}: {2}",
                    view?.InstanceId ?? 0, actionName, ex.Message);
            }
        }

        private static async Task PrepareMonsterAction(MonView view, string actionName)
        {
            if (view == null || view.Model == null || string.IsNullOrEmpty(actionName)) return;
            Animation anim = view.Model.GetComponent<Animation>();
            if (anim == null) anim = view.Model.AddComponent<Animation>();
            if (anim.GetClip(actionName) != null) return;

            string actionKey = $"object/monster/action/{view.MonsterRes}/{actionName}";
            AnimationClip clip = await ResManager.LoadAsync<AnimationClip>(actionKey);
            if (view.Model == null) return;
            if (clip == null)
            {
                GameLog.Warn("Scene", "monster action missing monster_res={0} action={1} key={2}",
                    view.MonsterRes, actionName, actionKey);
                return;
            }
            if (!clip.legacy)
            {
                GameLog.Warn("Scene", "monster action non-legacy monster_res={0} action={1} key={2}",
                    view.MonsterRes, actionName, actionKey);
                return;
            }
            anim.AddClip(clip, actionName);
        }

        private static bool TryPlayMonsterAction(MonView view, string actionName, float fade, bool restart)
        {
            if (view == null || view.Model == null || string.IsNullOrEmpty(actionName)) return false;
            Animation anim = view.Model.GetComponent<Animation>();
            if (anim == null || anim.GetClip(actionName) == null) return false;
            if (!restart && anim.IsPlaying(actionName)) return true;
            if (restart) anim.Stop(actionName);
            anim.CrossFade(actionName, fade);
            return true;
        }

        private static float GetMonsterActionLength(MonView view, string actionName)
        {
            if (view == null || view.Model == null || string.IsNullOrEmpty(actionName)) return 0f;
            Animation anim = view.Model.GetComponent<Animation>();
            AnimationClip clip = anim != null ? anim.GetClip(actionName) : null;
            return clip != null ? clip.length : 0f;
        }

        private static void PlayMonsterParticles(MonView view, int skillId,
            IReadOnlyList<SkillMovieParticle> particles, int version)
        {
            if (particles == null || particles.Count == 0) return;
            for (int i = 0; i < particles.Count; i++)
            {
                SkillMovieParticle particle = particles[i];
                if (particle == null || string.IsNullOrEmpty(particle.Res)) continue;
                _ = PlayMonsterParticleAsync(view, skillId, particle, version);
            }
        }

        private static async Task PlayMonsterParticleAsync(MonView view, int skillId,
            SkillMovieParticle particle, int version)
        {
            try
            {
                if (particle.StartTime > 0f)
                    await Task.Delay(Mathf.RoundToInt(particle.StartTime * 1000f));
                if (IsStale(view) || view.Model == null || version != view.ActionVersion) return;

                GameObject effect = await EffectBinder.AttachOne(view.Model, "root", "skills_effect",
                    particle.Res, "action", false);
                if (effect == null) return;
                if (particle.Scale > 0f && Mathf.Abs(particle.Scale - 1f) > 0.001f)
                    effect.transform.localScale = Vector3.one * particle.Scale;

                if (particle.PlayTimeLen > 0f)
                {
                    EffectBinder.PlayEffect(effect);
                    Object.Destroy(effect, particle.PlayTimeLen);
                }
                else
                {
                    EffectBinder.PlayOneShot(effect);
                }
            }
            catch (System.Exception ex)
            {
                GameLog.Warn("Scene", "play monster particle failed ins={0} skill={1} res={2}: {3}",
                    view?.InstanceId ?? 0, skillId, particle.Res, ex.Message);
            }
        }

        // 模型 key:object/monster/model_clothe_{monster_res}/model_clothe_{monster_res}
        // (转换后模型布局,同 NPC model_clothe_ 约定;monster_res = 协议 vo.MonsterRes,服务端 Body)。
        private static string ModelKey(int monsterRes)
            => $"object/monster/model_clothe_{monsterRes}/model_clothe_{monsterRes}";

        private static string DisplayName(MonsterVo vo, MonsterConfigs.MonCfg cfg)
        {
            // 对标老端 Monster.UpdateName:name_board.SetName(this.vo.name)(Monster.ts:326)——服务器实例名优先
            // (如 type 10001001 实例实采 vo.name="转运达摩";config_mon["1"]="转运傀儡" 仅模板,boss 升级路径才覆盖 vo.name)。
            // vo.name 空 → 回退 config_mon 名(模板);皆空 → 不挂名(降级,不写假名)。
            if (vo != null && !string.IsNullOrEmpty(vo.Name)) return vo.Name;
            return cfg != null && !string.IsNullOrEmpty(cfg.Name) ? cfg.Name : "";
        }

        // ===================== 名牌(名字 + 血条,屏幕跟随)=====================
        // 怪经合成台 RT 合成(3D 体在隔离区),2D 名牌无法直接叠进 RT,故名牌做成 Scene 层屏幕跟随件:
        // 怪屏幕位 = (怪像素 - 相机像素)(与合成台/地图同一锚定口径,同 NpcRenderer)。精确竖直贴合属"待真机微调"。
        private const float NAMEPLATE_HEAD_OFFSET = 140f; // 名牌相对脚底锚点上移(参考像素;头顶高度,实跑可调)
        private const float HP_BAR_WIDTH = 90f;
        private const float HP_BAR_HEIGHT = 8f;

        private const float BODY_HIT_HALF_WIDTH = 110f;
        private const float BODY_HIT_BOTTOM = -25f;
        private const float BODY_HIT_TOP = 210f;

        private static RectTransform _nameplateRoot;
        private static TMP_FontAsset _font;
        private static Material _fontMat;
        private static Sprite _whiteSprite;

        private static void CreateNameplate(MonView view, MonsterConfigs.MonCfg cfg)
        {
            string name = DisplayName(view.Vo, cfg);
            bool showHp = !view.Vo.IsCollect && view.Vo.HpLim > 0; // 采集物不显血条(对标 Monster.SetHpState)
            if (string.IsNullOrEmpty(name) && !showHp) return;      // 无名且无血条 → 不挂

            EnsureNameplateRoot();
            if (_nameplateRoot == null) return;

            var go = new GameObject($"MonNameplate_{view.InstanceId}", typeof(RectTransform));
            go.transform.SetParent(_nameplateRoot, false);
            var root = (RectTransform)go.transform;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f); // 锚屏幕中心:anchoredPosition 用(怪 - 相机)像素
            root.pivot = new Vector2(0.5f, 0f);                        // 底边贴在头顶偏移处
            root.sizeDelta = new Vector2(220f, 56f);

            if (!string.IsNullOrEmpty(name))
            {
                var txtGo = new GameObject("Name", typeof(RectTransform));
                txtGo.transform.SetParent(root, false);
                var trt = (RectTransform)txtGo.transform;
                trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f);
                trt.pivot = new Vector2(0.5f, 0f);
                trt.anchoredPosition = new Vector2(0f, HP_BAR_HEIGHT + 6f); // 名字在血条之上
                trt.sizeDelta = new Vector2(220f, 28f);

                var t = txtGo.AddComponent<TextMeshProUGUI>();
                t.fontSize = 20;
                t.alignment = TextAlignmentOptions.Bottom;
                t.raycastTarget = false;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                ApplyFont(t);
                // 怪名暖色(红橙),与 NPC 名牌(青)区分,对标老端敌对怪名牌配色。
                t.text = $"<color=#ff7a6a>{name}</color>";
            }

            if (showHp)
            {
                EnsureWhiteSprite();
                var bgGo = new GameObject("HpBarBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bgGo.transform.SetParent(root, false);
                var bgRt = (RectTransform)bgGo.transform;
                bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0f);
                bgRt.pivot = new Vector2(0.5f, 0f);
                bgRt.anchoredPosition = new Vector2(0f, 2f);
                bgRt.sizeDelta = new Vector2(HP_BAR_WIDTH, HP_BAR_HEIGHT);
                var bg = bgGo.GetComponent<Image>();
                bg.sprite = _whiteSprite;
                bg.color = new Color(0f, 0f, 0f, 0.6f);
                bg.raycastTarget = false;

                var fillGo = new GameObject("HpBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillGo.transform.SetParent(bgRt, false);
                var fillRt = (RectTransform)fillGo.transform;
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = new Vector2(1f, 1f);
                fillRt.offsetMax = new Vector2(-1f, -1f);
                var fill = fillGo.GetComponent<Image>();
                fill.sprite = _whiteSprite;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.color = new Color(0.86f, 0.22f, 0.20f, 1f); // 怪血条红
                fill.raycastTarget = false;
                view.HpFill = fill;
            }

            view.NameplateRt = root;
            RefreshHp(view);
        }

        private static void RefreshHp(MonView view)
        {
            if (view?.HpFill == null) return;
            long lim = view.Vo.HpLim;
            float frac = lim > 0 ? Mathf.Clamp01((float)view.Vo.Hp / lim) : 0f;
            view.HpFill.fillAmount = frac;
        }

        private static void UpdateNameplatePosition(MonView view)
        {
            if (view?.NameplateRt == null) return;
            Vector2 cam = SceneMapView.CameraPos;
            float sx = view.Vo.X - cam.x;
            float sy = -(view.Vo.Y - cam.y);
            view.NameplateRt.anchoredPosition = new Vector2(sx, sy + NAMEPLATE_HEAD_OFFSET);
        }

        internal static bool TryHitMonster(Vector2 screenPosition, out MonsterVo monster)
        {
            monster = null;
            if (_views.Count == 0) return false;
            if (!ScreenToSceneLocal(screenPosition, out Vector2 sceneLocal)) return false;

            MonView best = null;
            float bestScore = float.MaxValue;
            Camera eventCamera = GetEventCamera();
            foreach (MonView view in _views.Values)
            {
                if (view == null || !view.Loaded || view.Vo == null) continue;

                Vector2 foot = SceneLocalPosition(view.Vo.X, view.Vo.Y);
                bool bodyHit = IsBodyHit(sceneLocal, foot);
                bool nameHit = view.NameplateRt != null
                    && RectTransformUtility.RectangleContainsScreenPoint(view.NameplateRt, screenPosition, eventCamera);
                if (!bodyHit && !nameHit) continue;

                Vector2 center = new Vector2(foot.x, foot.y + NAMEPLATE_HEAD_OFFSET * 0.5f);
                float score = (sceneLocal - center).sqrMagnitude;
                if (nameHit) score -= 100000f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = view;
                }
            }

            monster = best?.Vo;
            return monster != null;
        }

        private static bool IsBodyHit(Vector2 sceneLocal, Vector2 foot)
        {
            float dx = Mathf.Abs(sceneLocal.x - foot.x);
            float dy = sceneLocal.y - foot.y;
            return dx <= BODY_HIT_HALF_WIDTH && dy >= BODY_HIT_BOTTOM && dy <= BODY_HIT_TOP;
        }

        private static Vector2 SceneLocalPosition(int x, int y)
        {
            Vector2 cam = SceneMapView.CameraPos;
            return new Vector2(x - cam.x, -(y - cam.y));
        }

        private static bool ScreenToSceneLocal(Vector2 screenPosition, out Vector2 local)
        {
            RectTransform sceneLayer = ViewManager.GetLayer(UILayer.Scene) as RectTransform;
            if (sceneLayer == null)
            {
                local = screenPosition - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                return true;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sceneLayer,
                screenPosition,
                GetEventCamera(),
                out local);
        }

        private static Camera GetEventCamera()
        {
            Transform sceneLayer = ViewManager.GetLayer(UILayer.Scene);
            Canvas canvas = sceneLayer != null ? sceneLayer.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera;
        }

        private static void DestroyNameplate(MonView view)
        {
            if (view?.NameplateRt == null) return;
            Object.Destroy(view.NameplateRt.gameObject);
            view.NameplateRt = null;
            view.HpFill = null;
        }

        private static void EnsureNameplateRoot()
        {
            if (_nameplateRoot != null) return;
            Transform sceneLayer = ViewManager.GetLayer(UILayer.Scene);
            if (sceneLayer == null) return;

            var go = new GameObject("__MonsterNameplates", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(sceneLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = -40; // 合成台 RT(-50)之上、HUD(默认 0)之下,同 NPC 名牌
            _nameplateRoot = rt;
        }

        private static void EnsureWhiteSprite()
        {
            if (_whiteSprite != null) return;
            Texture2D tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // 复用场景里已打开文本的 TMP 字体(含中文字形),避免名牌豆腐块(同 NpcRenderer/DialogueView 约定)。
        private static void ApplyFont(TextMeshProUGUI t)
        {
            if (_font == null)
            {
                TextMeshProUGUI src = Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (src != null) { _font = src.font; _fontMat = src.fontSharedMaterial; }
            }
            if (_font != null) t.font = _font;
            if (_fontMat != null) t.fontSharedMaterial = _fontMat;
        }

        private static void EnsureDriver()
        {
            if (_driverGo != null) return;
            _driverGo = new GameObject("__MonsterRendererDriver");
            Object.DontDestroyOnLoad(_driverGo);
            _driverGo.AddComponent<MonsterRendererDriver>();
        }
    }

    /// <summary>怪物渲染层的每帧驱动(对标 NpcRendererDriver):把怪随主角移动重摆相对位。</summary>
    public sealed class MonsterRendererDriver : MonoBehaviour
    {
        private void Update() => MonsterRenderer.TickPositions();
    }
}
