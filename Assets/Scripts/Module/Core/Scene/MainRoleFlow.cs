using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// Creates the local main role after map data is ready.
    /// </summary>
    public static class MainRoleFlow
    {
        private const float MODEL_ROOT_SCALE = 1.1f;
        private const float SCENE_ROTATE_X = 38f;
        private const float SCENE_ROTATE_Y = 180f;
        private const string ACTION_RUN = "run";
        private static readonly string[] STAND_ACTIONS = { "idle" };

        private static GameObject _sceneRoot;
        private static GameObject _mainRoleRoot;
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

            // 跑动用 run 动作(idle 已由装配器自动播放;这里再补 run 的 clip,不自动播)。
            await RoleModelAssembler.PrepareRoleActions(model, role.Career, spec.ClotheRes, new[] { ACTION_RUN });
            if (version != _buildVersion)
            {
                UnityEngine.Object.Destroy(model);
                return;
            }

            EnsureSceneRoot();
            ClearMainRole(false);

            _mainRoleRoot = new GameObject("MainRole_" + role.RoleId);
            _mainRoleRoot.transform.SetParent(_sceneRoot.transform, false);
            ApplyOldClientSceneTransform(_mainRoleRoot.transform, role);

            model.transform.SetParent(_mainRoleRoot.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            // 摇杆移动驱动:主角恒居屏幕中心,相机跟随滚动地图(SceneMapView.SetFocus)。
            MainRoleAgent agent = _mainRoleRoot.AddComponent<MainRoleAgent>();
            agent.Init(model, role.X, role.Y);
            SceneInputDriver.EnsureInstalled();

            GameLog.Info("Scene", "main role ready: roleId={0} pos=({1},{2}) clothe={3}",
                role.RoleId, role.X, role.Y, spec.ClotheRes);
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
        }

        private static void ApplyOldClientSceneTransform(Transform root, RoleModel role)
        {
            // 跟随相机模型:主角恒居屏幕中心,移动靠相机滚动地图(SceneMapView.SetFocus)体现,
            // 故主角节点不按 X/Y 平移,只保留老客户端的场景倾斜与缩放;深度按出生 Y 取一次。
            root.localPosition = new Vector3(0f, 0f, SceneObjDepth(role.Y));
            root.localRotation = Quaternion.Euler(SCENE_ROTATE_X, SCENE_ROTATE_Y, 0f);
            root.localScale = Vector3.one * MODEL_ROOT_SCALE;
        }

        private static float SceneObjDepth(int realY)
        {
            return 900f - realY * 0.02f;
        }
    }
}
