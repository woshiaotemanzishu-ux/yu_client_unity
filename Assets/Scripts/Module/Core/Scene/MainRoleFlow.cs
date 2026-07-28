using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Skill;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// Creates the local main role after map data is ready.
    /// </summary>
    public static class MainRoleFlow
    {
        private const string ACTION_RUN = "run";
        // 新美术模板保持统一的 0/0/1 资源规范；正式地图与旧场景角色的体量差在场景侧统一校准。
        // 0.85 表示相对模板原始体量缩小 15%，只作用于主角，不影响选角/创角/资产预览及 NPC。
        private const float NEW_ART_MAIN_ROLE_SCENE_SCALE = 0.85f;
        private static readonly string[] STAND_ACTIONS = { "idle" };

        private static GameObject _sceneRoot;
        private static GameObject _mainRoleRoot;
        private static GameObject _mainRoleModel;
        private static RoleModelSpec _lastSpec;
        private static int _buildVersion;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On(GlobalEvent.EVT_SCENE_MAP_READY, OnSceneMapReady);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnNetDisconnected);
        }

        private static async void OnSceneMapReady()
        {
            await RebuildAsync();
        }

        private static void OnNetDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                GameLog.Info("Scene", "keep main role during in-game reconnect");
                return;
            }

            ClearMainRole(true);
        }

        private static async Task RebuildAsync()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Figure == null)
            {
                GameLog.Warn("Scene", "skip main role: role figure is not ready");
                return;
            }

            if (SceneMapLoader.Current == null)
            {
                GameLog.Warn("Scene", "skip main role: scene map is not ready");
                return;
            }

            int version = ++_buildVersion;
            RoleModelSpec spec = await BuildSpecAsync(role);
            if (version != _buildVersion) return;

            if (spec.ClotheRes <= 0)
            {
                GameLog.Warn("Scene", "skip main role: missing clothe res roleId={0} career={1}", role.RoleId, role.Career);
                return;
            }

            SkillVisualWarmupPlan combatPlan =
                await SkillMovieConfigs.BuildCareerWarmupPlanAsync(role.Career);
            if (version != _buildVersion) return;

            // 同形象且模型仍活着(同图副本进出/跨图传送/重连同帧重进):不整只销毁重建——
            // 老端这里角色原地不动,重建等于白重载 衣/头/武器/翅膀+动作 一整套。Init 本身是复位函数,
            // 复位坐标/动作/移动状态即可。形象真变了(换装)照走完整重建。
            if (_mainRoleRoot != null && _mainRoleModel != null && SameFigure(spec, _lastSpec))
            {
                MainRoleAgent existing = _mainRoleRoot.GetComponent<MainRoleAgent>();
                if (existing != null)
                {
                    await PrepareFirstCombatActionsAsync(
                        _mainRoleModel, role.Career, spec.ClotheRes, combatPlan);
                    if (version != _buildVersion) return;
                    existing.Init(_mainRoleModel, role.X, role.Y, role.Career, role.Figure?.sex ?? 0, spec.ClotheRes);
                    GameLog.Info("Scene", "main role reused (same figure): pos=({0},{1})", role.X, role.Y);
                    return;
                }
            }

            GameObject model = await RoleModelAssembler.BuildAsync(spec);
            if (version != _buildVersion)
            {
                if (model != null) UnityEngine.Object.Destroy(model);
                return;
            }

            if (model == null)
            {
                GameLog.Warn("Scene", "main role model load failed roleId={0} clothe={1}", role.RoleId, spec.ClotheRes);
                return;
            }

            // idle 已由装配器自动播放；正式交给战斗状态机前，把跑动、普攻和当前职业技能动作
            // 一次性准备好。混合模型还会在这里预建未替换动作所需的老模型兼容分支。
            await PrepareFirstCombatActionsAsync(model, role.Career, spec.ClotheRes, combatPlan);
            if (version != _buildVersion)
            {
                UnityEngine.Object.Destroy(model);
                return;
            }

            EnsureSceneRoot();
            ClearMainRole(false);

            // 视觉:模型进 3D 合成台(专用相机 → RT → Scene 层 RawImage),压在地图之上、HUD 之下。
            // 不能直接摆世界里——根 Canvas 是 ScreenSpaceOverlay,不透明地图会盖住任何世界 3D 物体。
            bool isNewArtModel = model.GetComponentInChildren<ArtModelRenderProfile>(true) != null;
            float sceneScale = isNewArtModel ? NEW_ART_MAIN_ROLE_SCENE_SCALE : 1f;
            SceneCharacterStage.SetMainRole(model, sceneScale);

            // 逻辑:MainRoleAgent 挂在轻量逻辑节点上,跨层驱动合成台里的模型(转向/动作/相机跟随/上报)。
            _mainRoleRoot = new GameObject("MainRole_" + role.RoleId);
            _mainRoleRoot.transform.SetParent(_sceneRoot.transform, false);

            // 摇杆移动驱动:主角恒居屏幕中心,相机跟随滚动地图(SceneMapView.SetFocus)。
            MainRoleAgent agent = _mainRoleRoot.AddComponent<MainRoleAgent>();
            agent.Init(model, role.X, role.Y, role.Career, role.Figure?.sex ?? 0, spec.ClotheRes);
            SceneInputDriver.EnsureInstalled();

            _mainRoleModel = model;
            _lastSpec = spec;
            GameLog.Info("Scene", "main role ready: roleId={0} pos=({1},{2}) clothe={3} sceneScale={4:0.###}",
                role.RoleId, role.X, role.Y, spec.ClotheRes, sceneScale);
        }

        private static bool SameFigure(RoleModelSpec a, RoleModelSpec b)
        {
            return a != null && b != null
                && a.Career == b.Career
                && a.ClotheRes == b.ClotheRes
                && a.WeaponRes == b.WeaponRes
                && a.HeadRes == b.HeadRes
                && a.WingId == b.WingId
                && a.BackOrnamentId == b.BackOrnamentId;
        }

        private static async Task PrepareFirstCombatActionsAsync(GameObject model, int career,
            int clotheRes, SkillVisualWarmupPlan combatPlan)
        {
            if (model == null) return;
            var actions = new List<string> { ACTION_RUN };
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ACTION_RUN };
            string[] configured = combatPlan?.Actions ?? Array.Empty<string>();
            for (int i = 0; i < configured.Length; i++)
            {
                string action = configured[i];
                if (!string.IsNullOrEmpty(action) && seen.Add(action)) actions.Add(action);
            }

            ReplaceableRoleModel driver = model.GetComponent<ReplaceableRoleModel>();
            if (driver != null)
            {
                await driver.PrepareActionsAsync(actions);
                return;
            }

            await RoleModelAssembler.PrepareRoleActions(model, career, clotheRes, actions.ToArray());
        }

        private static async Task<RoleModelSpec> BuildSpecAsync(RoleModel role)
        {
            FigureProto figure = role.Figure;
            int career = figure.career;
            int sex = figure.sex;
            int clotheRes = figure.ClotheModelId;
            int weaponRes = figure.WeaponModelId;
            int headRes = figure.HeadModelId;

            if (clotheRes <= 0 || weaponRes <= 0 || headRes <= 0)
            {
                await LoginConfigs.EnsureLoaded();
                LoginConfigs.CareerRes defaults = LoginConfigs.GetCreateRes(career, sex);
                if (defaults != null)
                {
                    if (clotheRes <= 0) clotheRes = defaults.RoleRes;
                    if (weaponRes <= 0) weaponRes = defaults.WeaponRes;
                    if (headRes <= 0) headRes = defaults.HeadRes;
                }
            }

            return new RoleModelSpec
            {
                Career = career,
                ClotheRes = clotheRes,
                WeaponRes = weaponRes,
                HeadRes = headRes,
                WingId = figure.WingId,
                BackOrnamentId = figure.BackOrnamentId,
                Actions = STAND_ACTIONS,
                // 老端场景角色刻意关闭 Body.always；武器/翅膀/背饰自身的常驻特效仍照常装配。
                IncludeBodyAlwaysEffects = false,
            };
        }

        private static void EnsureSceneRoot()
        {
            if (_sceneRoot != null) return;
            _sceneRoot = GameObject.Find("__SceneRoot");
            if (_sceneRoot == null)
            {
                _sceneRoot = new GameObject("__SceneRoot");
            }
        }

        private static void ClearMainRole(bool bumpVersion)
        {
            if (bumpVersion)
            {
                ++_buildVersion;
                // 彻底清主角(非重建)时一并撤掉场景输入,避免无主角时仍在采集移动。
                SceneInputDriver.Remove();
            }
            if (_mainRoleRoot != null)
            {
                UnityEngine.Object.Destroy(_mainRoleRoot);
                _mainRoleRoot = null;
            }
            _mainRoleModel = null;
            _lastSpec = null;
            // 模型挂在合成台上,逻辑节点销毁时一并清掉合成台里的主角与画面。
            SceneCharacterStage.Clear();
        }
    }
}
