using System;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Module.Core.Scene;
using UnityEngine;
using UnityEngine.Playables;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 场景主角混合驱动接线实证(新模型逐动作替换):model_replacement 命中的衣服(role/1111)经
    /// RoleModelAssembler.BuildAsync 拿到混合容器后,MainRoleAgent 的动作出口必须委托
    /// ReplaceableRoleModel(接线前直驱容器根 Animation → _anim=null → 动作切换全静默)。断言:
    ///   1 清单命中 role/1111(配置前提);
    ///   2 BuildAsync 返回容器挂 ReplaceableRoleModel;
    ///   3 MainRoleAgent.Init 后私有 _driver 非空(接线存在);
    ///   4 PrepareActionsAsync 预建 run 新实例和 attack 老分支，但保持 idle 仍在台上;
    ///   5 TryPlayAction("run")(清单已配)返回 true,立即切换到预建新模型实例;
    ///   6 新模型表面统一 Unlit，UI/场景台均不改写全局环境光;
    ///   7 GetActionLength("run") > 0(预热后 Timeline 时长可读,技能节拍依赖);
    ///   8 新模型翅膀的 yincang 节点在 idle 显示、run 隐藏;
    ///   9 TryPlayAction("attack")(清单未配)切到预建老拼装模型(激活子树=_oldModel,带 attack clip)。
    /// 日志前缀 "CLIVERIFY mixdriver"。
    /// </summary>
    public static class SceneMixDriverCase
    {
        private const int ClotheRes = 1111; // 职业1(剑士,sex=1)创角默认衣服,本轮新导入并登记清单
        private const int WingId = 1005;
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            if (!VerifyStagesDoNotChangeAmbient()) return 3;
            await ModelReplacement.EnsureLoaded();
            if (!ModelReplacement.HasEntry("role", ClotheRes))
            {
                Debug.LogError("CLIVERIFY mixdriver 清单未命中 role/" + ClotheRes + "(model_replacement.json 前提不成立)");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 1 清单命中 role/" + ClotheRes);

            GameObject model = await RoleModelAssembler.BuildAsync(new RoleModelSpec
            {
                Career = 1,
                ClotheRes = ClotheRes,
                WingId = WingId,
                Actions = new[] { "idle" },
            });
            if (model == null)
            {
                Debug.LogError("CLIVERIFY mixdriver BuildAsync 返回 null");
                return 3;
            }
            var driver = model.GetComponent<ReplaceableRoleModel>();
            if (driver == null)
            {
                Debug.LogError("CLIVERIFY mixdriver 容器未挂 ReplaceableRoleModel");
                UnityEngine.Object.DestroyImmediate(model);
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 2 混合容器就绪 " + model.name);

            var host = new GameObject("mixdriver_host");
            int code;
            try
            {
                model.transform.SetParent(host.transform, false);
                var agent = host.AddComponent<MainRoleAgent>();
                agent.Init(model, 100, 100, 1, 1, ClotheRes);
                code = await Assert(agent, driver, model);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
            return code;
        }

        private static async Task<int> Assert(MainRoleAgent agent, ReplaceableRoleModel driver, GameObject model)
        {
            FieldInfo fDriver = typeof(MainRoleAgent).GetField("_driver", F);
            MethodInfo mTryPlay = typeof(MainRoleAgent).GetMethod("TryPlayAction", F);
            MethodInfo mTryPlayAsync = typeof(MainRoleAgent).GetMethod("TryPlayActionAsync", F);
            MethodInfo mEffectHost = typeof(MainRoleAgent).GetMethod("GetActiveActionEffectHost", F);
            MethodInfo mLength = typeof(MainRoleAgent).GetMethod("GetActionLength", F);
            FieldInfo fActive = typeof(ReplaceableRoleModel).GetField("_active", F);
            FieldInfo fOldModel = typeof(ReplaceableRoleModel).GetField("_oldModel", F);
            FieldInfo fNewInstances = typeof(ReplaceableRoleModel).GetField("_newInstances", F);
            if (fDriver == null || mTryPlay == null || mTryPlayAsync == null || mEffectHost == null
                || mLength == null || fActive == null || fOldModel == null || fNewInstances == null)
            {
                Debug.LogError("CLIVERIFY mixdriver 反射目标缺失(字段/方法被改名?)");
                return 3;
            }

            if (fDriver.GetValue(agent) == null)
            {
                Debug.LogError("CLIVERIFY mixdriver Init 后 _driver 为空(接线缺失)");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 3 MainRoleAgent._driver 已接线");

            // 4:模拟 MainRoleFlow 的首战预热。预建不能抢走当前 idle 画面。
            var activeBeforeWarmup = fActive.GetValue(driver) as GameObject;
            await driver.PrepareActionsAsync(new[] { "run", "attack" });
            var newInstances = (System.Collections.IDictionary)fNewInstances.GetValue(driver);
            var warmedRun = newInstances["run"] as GameObject;
            var warmedOld = fOldModel.GetValue(driver) as GameObject;
            var activeAfterWarmup = fActive.GetValue(driver) as GameObject;
            Transform idleWingMarker = activeBeforeWarmup != null
                ? RoleModelAssembler.FindBone(activeBeforeWarmup.transform, "yincang") : null;
            Transform runWingMarker = warmedRun != null
                ? RoleModelAssembler.FindBone(warmedRun.transform, "yincang") : null;
            if (warmedRun == null || warmedRun.activeSelf
                || warmedOld == null || warmedOld.activeSelf
                || warmedOld.GetComponent<Animation>()?.GetClip("attack") == null
                || activeAfterWarmup != activeBeforeWarmup || activeAfterWarmup == null || !activeAfterWarmup.activeSelf)
            {
                Debug.LogError("CLIVERIFY mixdriver PrepareActionsAsync 未静默预建 run/attack 或改变了当前 idle");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 4 首战动作已静默预建,当前 idle 未改变");
            if (idleWingMarker == null || !idleWingMarker.gameObject.activeSelf
                || runWingMarker == null || runWingMarker.gameObject.activeSelf)
            {
                Debug.LogError("CLIVERIFY mixdriver 1005 翅膀 yincang 未按 idle显示/run隐藏");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 4b 1005 翅膀 yincang idle显示/run隐藏");

            // 5/6:run 已配新模型 → 出口返回 true,激活子树切到带 Timeline 的预建实例,时长可读
            bool runAccepted = (bool)mTryPlay.Invoke(agent, new object[] { "run", 0.1f, true, 1f });
            if (!runAccepted)
            {
                Debug.LogError("CLIVERIFY mixdriver TryPlayAction(run) 返回 false(应走新模型)");
                return 3;
            }
            // BuildAsync 初始已播 idle(同为带 Timeline 的新实例),必须认准 run 自己的实例加载完并上台,
            // 否则条件被 idle 实例秒满足 → 紧跟的时长断言撞上 run 未加载的竞态
            bool runActive = await WaitUntil(() =>
            {
                if (!newInstances.Contains("run")) return false;
                var inst = newInstances["run"] as GameObject;
                var active = fActive.GetValue(driver) as GameObject;
                return inst != null && active == inst && inst.activeSelf
                    && inst.GetComponentInChildren<PlayableDirector>(true) != null;
            });
            if (!runActive)
            {
                Debug.LogError("CLIVERIFY mixdriver run 未切到新模型实例(激活子树无 PlayableDirector)");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 5 run → 预建新模型实例已激活");

            GameObject activeRun = fActive.GetValue(driver) as GameObject;
            if (activeRun == null || HasLitSurface(activeRun) || HasEnabledLight(activeRun))
            {
                Debug.LogError("CLIVERIFY mixdriver 新模型仍含 Lit 表面或 Light 组件");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 6 新模型表面已 Unlit,无 Light 组件");

            float runLength = (float)mLength.Invoke(agent, new object[] { "run" });
            if (runLength <= 0f)
            {
                Debug.LogError("CLIVERIFY mixdriver GetActionLength(run)=" + runLength + "(新实例已加载,应>0)");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 7 GetActionLength(run)=" + runLength.ToString("F2") + "s");

            // 9:attack 清单未配 → 切到预建老拼装模型,不再在首次攻击帧临时构建
            var attackTask = (Task<bool>)mTryPlayAsync.Invoke(
                agent, new object[] { "attack", 0.1f, true, 1f });
            bool attackAccepted = await attackTask;
            if (!attackAccepted)
            {
                Debug.LogError("CLIVERIFY mixdriver TryPlayAction(attack) 返回 false(应回落老模型)");
                return 3;
            }
            bool oldActive = await WaitUntil(() =>
            {
                var old = fOldModel.GetValue(driver) as GameObject;
                var active = fActive.GetValue(driver) as GameObject;
                return old != null && active == old && old.activeSelf && old.GetComponent<Animation>() != null;
            });
            if (!oldActive)
            {
                Debug.LogError("CLIVERIFY mixdriver attack 未回落老拼装模型(_oldModel 未建或未激活)");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 8 attack → 预建老拼装模型已激活");

            var effectHost = mEffectHost.Invoke(agent, null) as GameObject;
            if (effectHost == null || effectHost != driver.ActiveModel || effectHost != warmedOld)
            {
                Debug.LogError("CLIVERIFY mixdriver attack 特效宿主未指向当前激活的老模型");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 9 attack 特效宿主已锁定当前激活模型");

            Debug.Log("CLIVERIFY mixdriver ALL PASS");
            return 0;
        }

        private static bool VerifyStagesDoNotChangeAmbient()
        {
            UnityEngine.Rendering.AmbientMode savedMode = RenderSettings.ambientMode;
            Color savedColor = RenderSettings.ambientLight;
            Color probeColor = new Color(0.13f, 0.27f, 0.41f, 1f);
            UIModelStage uiStage = null;
            GameObject container = null;
            try
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = probeColor;

                container = new GameObject("mixdriver_ui_stage", typeof(RectTransform));
                RectTransform rect = container.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(128f, 128f);
                GameObject uiModel = new GameObject("mixdriver_ui_art");
                uiModel.AddComponent<ArtModelRenderProfile>();
                uiStage = new UIModelStage();
                uiStage.PlaceInstance(rect, uiModel);
                bool uiUntouched = RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat &&
                                   RenderSettings.ambientLight == probeColor;
                DisposeUiStageImmediate(uiStage);
                uiStage = null;

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = probeColor;
                GameObject sceneModel = new GameObject("mixdriver_scene_art");
                sceneModel.AddComponent<ArtModelRenderProfile>();
                SceneCharacterStage.SetMainRole(sceneModel);
                bool sceneUntouched = RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat &&
                                      RenderSettings.ambientLight == probeColor;
                ResetSceneStageImmediate();

                if (!uiUntouched || !sceneUntouched)
                {
                    Debug.LogError("CLIVERIFY mixdriver UI/场景模型台仍在改写 RenderSettings 环境光");
                    return false;
                }
                Debug.Log("CLIVERIFY mixdriver 0 UI/场景模型台均不改写环境光");
                return true;
            }
            finally
            {
                if (uiStage != null) DisposeUiStageImmediate(uiStage);
                ResetSceneStageImmediate();
                if (container != null) UnityEngine.Object.DestroyImmediate(container);
                RenderSettings.ambientMode = savedMode;
                RenderSettings.ambientLight = savedColor;
            }
        }

        private static void DisposeUiStageImmediate(UIModelStage stage)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = typeof(UIModelStage);
            var image = type.GetField("_img", flags)?.GetValue(stage) as Component;
            var root = type.GetField("_root", flags)?.GetValue(stage) as GameObject;
            var target = type.GetField("_rt", flags)?.GetValue(stage) as RenderTexture;
            if (image != null) UnityEngine.Object.DestroyImmediate(image.gameObject);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void ResetSceneStageImmediate()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            Type type = typeof(SceneCharacterStage);
            var image = type.GetField("_img", flags)?.GetValue(null) as Component;
            var root = type.GetField("_root", flags)?.GetValue(null) as GameObject;
            var target = type.GetField("_rt", flags)?.GetValue(null) as RenderTexture;
            if (image != null) UnityEngine.Object.DestroyImmediate(image.gameObject);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }

            foreach (string fieldName in new[]
                     {
                         "_root", "_cam", "_rt", "_img", "_charsRoot", "_mainRole", "_mainRoleTilt",
                         "_mainRoleAttachedEffects", "_mainRoleDetachedEffects", "_mainRoleDriver"
                     })
                type.GetField(fieldName, flags)?.SetValue(null, null);
        }

        private static bool HasLitSurface(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    string shader = material != null && material.shader != null ? material.shader.name : string.Empty;
                    if (shader == "Standard" || shader == "Standard (Specular setup)" ||
                        shader == "Universal Render Pipeline/Lit" ||
                        shader == "Universal Render Pipeline/Simple Lit" ||
                        shader == "Universal Render Pipeline/Complex Lit" ||
                        shader == "Universal Render Pipeline/Baked Lit")
                        return true;
                }
            }
            return false;
        }

        private static bool HasEnabledLight(GameObject root)
        {
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light != null && light.enabled) return true;
            }
            return false;
        }

        private static async Task<bool> WaitUntil(Func<bool> cond, int maxTicks = 3000)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                if (cond()) return true;
                await Task.Yield();
            }
            return cond();
        }
    }
}
