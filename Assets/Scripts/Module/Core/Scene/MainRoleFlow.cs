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
        private static readonly string[] FIRST_SCREEN_ACTIONS = { "idle", ACTION_RUN };

        private static GameObject _sceneRoot;
        private static GameObject _mainRoleRoot;
        private static GameObject _mainRoleModel;
        private static RoleModelSpec _lastSpec;
        private static int _buildVersion;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On(GlobalEvent.EVT_SCENE_MAP_READY, OnSceneMapReady);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_FIGURE_UPDATE, OnRoleFigureUpdate);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnNetDisconnected);
        }

        private static async void OnSceneMapReady()
        {
            await RebuildAsync();
        }

        private static async void OnRoleFigureUpdate()
        {
            if (SceneMapLoader.Current == null || _mainRoleRoot == null) return;
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

            // 同形象且模型仍活着(同图副本进出/跨图传送/重连同帧重进):不整只销毁重建——
            // 老端这里角色原地不动,重建等于白重载 衣/头/武器/翅膀+动作 一整套。Init 本身是复位函数,
            // 复位坐标/动作/移动状态即可。形象真变了(换装)照走完整重建。
            if (_mainRoleRoot != null && _mainRoleModel != null && SameFigure(spec, _lastSpec))
            {
                MainRoleAgent existing = _mainRoleRoot.GetComponent<MainRoleAgent>();
                if (existing != null)
                {
                    existing.Init(_mainRoleModel, role.X, role.Y, role.Career, role.Figure?.sex ?? 0, spec.ClotheRes);
                    _ = PrepareFirstCombatInBackgroundAsync(
                        _mainRoleModel, role.Career, spec.ClotheRes, version);
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

            // 模型和首屏动作已就绪，先交给场景；完整战斗动作在挂到场景后延迟预热。
            EnsureSceneRoot();
            ClearMainRole(false);

            // 视觉:模型进 3D 合成台(专用相机 → RT → Scene 层 RawImage),压在地图之上、HUD 之下。
            // 不能直接摆世界里——根 Canvas 是 ScreenSpaceOverlay,不透明地图会盖住任何世界 3D 物体。
            // 美术模型以 1400 为体量参照统一交付，场景不再对新模型额外缩小。
            SceneCharacterStage.SetMainRole(model);

            // 逻辑:MainRoleAgent 挂在轻量逻辑节点上,跨层驱动合成台里的模型(转向/动作/相机跟随/上报)。
            _mainRoleRoot = new GameObject("MainRole_" + role.RoleId);
            _mainRoleRoot.transform.SetParent(_sceneRoot.transform, false);

            // 摇杆移动驱动:主角恒居屏幕中心,相机跟随滚动地图(SceneMapView.SetFocus)。
            MainRoleAgent agent = _mainRoleRoot.AddComponent<MainRoleAgent>();
            agent.Init(model, role.X, role.Y, role.Career, role.Figure?.sex ?? 0, spec.ClotheRes);
            SceneInputDriver.EnsureInstalled();

            _mainRoleModel = model;
            _lastSpec = spec;
            // 首屏只阻塞 idle/run。技能动作在画面揭开后静默预热，避免 WebGL 在 55% 处
            // 一次性反序列化整套职业技能与特效，造成“网络已结束但页面像卡死”的长停顿。
            _ = PrepareFirstCombatInBackgroundAsync(model, role.Career, spec.ClotheRes, version);
            GameLog.Info("Scene", "main role ready: roleId={0} pos=({1},{2}) clothe={3} sceneScale=1",
                role.RoleId, role.X, role.Y, spec.ClotheRes);
        }

        private static async Task PrepareFirstCombatInBackgroundAsync(GameObject model, int career,
            int clotheRes, int version)
        {
            try
            {
                await Task.Delay(1000);
                if (version != _buildVersion || model == null || model != _mainRoleModel) return;
                SkillVisualWarmupPlan combatPlan =
                    await SkillMovieConfigs.BuildCareerWarmupPlanAsync(career);
                if (version != _buildVersion || model == null || model != _mainRoleModel) return;
                await PrepareFirstCombatActionsAsync(model, career, clotheRes, combatPlan);
                if (version == _buildVersion && model != null && model == _mainRoleModel)
                    GameLog.Info("Scene", "first combat actions warmed in background");
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Scene", "background combat warmup skipped: {0}", e.Message);
            }
        }

        private static bool SameFigure(RoleModelSpec a, RoleModelSpec b)
        {
            return a != null && b != null
                && a.Career == b.Career
                && a.ClotheRes == b.ClotheRes
                && a.ClotheChartletId == b.ClotheChartletId
                && a.WeaponRes == b.WeaponRes
                && a.HeadRes == b.HeadRes
                && a.HeadChartletId == b.HeadChartletId
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
                ClotheChartletId = figure.ClotheChartletId,
                WeaponRes = weaponRes,
                HeadRes = headRes,
                HeadChartletId = figure.HeadChartletId,
                WingId = figure.WingId,
                BackOrnamentId = figure.BackOrnamentId,
                Actions = FIRST_SCREEN_ACTIONS,
                AutoPlayActions = false,
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
