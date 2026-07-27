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
    ///   6 GetActionLength("run") > 0(预热后 Timeline 时长可读,技能节拍依赖);
    ///   7 TryPlayAction("attack")(清单未配)切到预建老拼装模型(激活子树=_oldModel,带 attack clip)。
    /// 日志前缀 "CLIVERIFY mixdriver"。
    /// </summary>
    public static class SceneMixDriverCase
    {
        private const int ClotheRes = 1111; // 职业1(剑士,sex=1)创角默认衣服,本轮新导入并登记清单
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
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
            MethodInfo mLength = typeof(MainRoleAgent).GetMethod("GetActionLength", F);
            FieldInfo fActive = typeof(ReplaceableRoleModel).GetField("_active", F);
            FieldInfo fOldModel = typeof(ReplaceableRoleModel).GetField("_oldModel", F);
            FieldInfo fNewInstances = typeof(ReplaceableRoleModel).GetField("_newInstances", F);
            if (fDriver == null || mTryPlay == null || mLength == null || fActive == null || fOldModel == null
                || fNewInstances == null)
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
            if (warmedRun == null || warmedRun.activeSelf
                || warmedOld == null || warmedOld.activeSelf
                || warmedOld.GetComponent<Animation>()?.GetClip("attack") == null
                || activeAfterWarmup != activeBeforeWarmup || activeAfterWarmup == null || !activeAfterWarmup.activeSelf)
            {
                Debug.LogError("CLIVERIFY mixdriver PrepareActionsAsync 未静默预建 run/attack 或改变了当前 idle");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 4 首战动作已静默预建,当前 idle 未改变");

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

            float runLength = (float)mLength.Invoke(agent, new object[] { "run" });
            if (runLength <= 0f)
            {
                Debug.LogError("CLIVERIFY mixdriver GetActionLength(run)=" + runLength + "(新实例已加载,应>0)");
                return 3;
            }
            Debug.Log("CLIVERIFY mixdriver 6 GetActionLength(run)=" + runLength.ToString("F2") + "s");

            // 7:attack 清单未配 → 切到预建老拼装模型,不再在首次攻击帧临时构建
            bool attackAccepted = (bool)mTryPlay.Invoke(agent, new object[] { "attack", 0.1f, true, 1f });
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
            Debug.Log("CLIVERIFY mixdriver 7 attack → 预建老拼装模型已激活");

            Debug.Log("CLIVERIFY mixdriver ALL PASS");
            return 0;
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
