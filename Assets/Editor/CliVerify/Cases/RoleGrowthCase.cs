using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Event;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 角色成长补全 + 改名 + 转职(自动循环 轮5)实证:手工按服务端权威字节序拼合成包,反射喂
    /// RoleController/TransferJobController 私有 On 方法,断言 RoleModel/SkillManager 状态 + GlobalEvent
    /// 触发;尾段渲染 TransferJobCardView(TransferJobCreator 生成→实例化→喂 config→断言卡片数)+ 截图。
    /// 日志前缀统一 "CLIVERIFY rolegrowth"。独立文件复用 CliVerify.Stage/Pkt,不改 CliVerify.cs 本体。
    /// </summary>
    public static class RoleGrowthCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            object roleCtrl = Shenxiao.Module.Core.Role.RoleController.Instance;
            object transferCtrl = Shenxiao.Module.Core.TransferJob.TransferJobController.Instance;

            MethodInfo GetM(object target, string name)
            {
                MethodInfo m = target.GetType().GetMethod(name, F);
                if (m == null) Debug.LogError("CLIVERIFY rolegrowth handler missing(reflection): " + name);
                return m;
            }

            MethodInfo m13011 = GetM(roleCtrl, "On13011");
            MethodInfo m13017 = GetM(roleCtrl, "On13017");
            MethodInfo m13020 = GetM(roleCtrl, "On13020");
            MethodInfo m13036 = GetM(roleCtrl, "On13036");
            MethodInfo m13046 = GetM(roleCtrl, "On13046");
            MethodInfo m13080 = GetM(roleCtrl, "On13080");
            MethodInfo m13081 = GetM(roleCtrl, "On13081");
            MethodInfo m13083 = GetM(roleCtrl, "On13083");
            MethodInfo m42601 = GetM(roleCtrl, "On42601");
            MethodInfo m42602 = GetM(roleCtrl, "On42602");
            MethodInfo m42604 = GetM(roleCtrl, "On42604");
            MethodInfo m13045 = GetM(transferCtrl, "On13045");
            if (m13011 == null || m13017 == null || m13020 == null || m13036 == null || m13046 == null
                || m13080 == null || m13081 == null || m13083 == null || m42601 == null || m42602 == null
                || m42604 == null || m13045 == null)
            {
                return 3;
            }

            void Feed(MethodInfo m, object target, byte[] pkt) =>
                m.Invoke(target, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

            var model = Shenxiao.Module.Core.Role.RoleModel.Instance;
            model.Reset();

            bool worldLvOk = Test13011(m13011, roleCtrl, Feed, model);
            bool depositOk = Test13017(m13017, roleCtrl, Feed, model);
            bool passiveOk = Test13020(m13020, roleCtrl, Feed);
            bool expFloatOk = Test13036(m13036, roleCtrl, Feed);
            bool cooldownOk = Test13046(m13046, roleCtrl, Feed, model);
            bool headChainOk = Test13080_81_83(m13080, m13081, m13083, roleCtrl, Feed, model);
            bool renameOk = Test42601(m42601, roleCtrl, Feed);
            bool transferOk = Test13045(m13045, transferCtrl, Feed, model);

            int render = await RenderTransferJobCardAsync();

            bool pass = worldLvOk && depositOk && passiveOk && expFloatOk && cooldownOk && headChainOk
                && renameOk && transferOk && render == 0;
            Debug.Log("CLIVERIFY rolegrowth VERDICT worldLv=" + worldLvOk + " deposit=" + depositOk
                + " passive=" + passiveOk + " expFloat=" + expFloatOk + " cooldown=" + cooldownOk
                + " headChain=" + headChainOk + " rename=" + renameOk + " transfer=" + transferOk
                + " render=" + render + " pass=" + pass);

            model.Reset();
            return pass ? 0 : 3;
        }

        // ---- 13011:"Hh" worldLvExp(16位有符号,可负)+ worldLv(16位无符号) ----
        private static bool Test13011(MethodInfo m, object ctrl, Action<MethodInfo, object, byte[]> feed,
            Shenxiao.Module.Core.Role.RoleModel model)
        {
            byte[] pkt = new CliVerify.Pkt().H(-5).H(200).Bytes(); // H=-5(有符号,Pkt.H 按低16位截断即两补码)/h=200(无符号)
            feed(m, ctrl, pkt);
            bool ok = model.WorldLvExp == -5 && model.WorldLv == 200;
            Debug.Log("CLIVERIFY rolegrowth 13011 worldLvExp=" + model.WorldLvExp + " worldLv=" + model.WorldLv + " ok=" + ok);
            return ok;
        }

        // ---- 13017:"c"=1 → DepositState=true ----
        private static bool Test13017(MethodInfo m, object ctrl, Action<MethodInfo, object, byte[]> feed,
            Shenxiao.Module.Core.Role.RoleModel model)
        {
            feed(m, ctrl, new CliVerify.Pkt().C(1).Bytes());
            bool ok = model.DepositState;
            Debug.Log("CLIVERIFY rolegrowth 13017 depositState=" + model.DepositState + " ok=" + ok);
            return ok;
        }

        // ---- 13020:反射注入合成 config_skill(type==2 被动才并入,level 恒 1;非被动/未知 id 不入) ----
        private static bool Test13020(MethodInfo m, object ctrl, Action<MethodInfo, object, byte[]> feed)
        {
            FieldInfo skillField = typeof(Shenxiao.Module.Core.Skill.SkillConfigs).GetField("_skill",
                BindingFlags.NonPublic | BindingFlags.Static);
            object saved = skillField?.GetValue(null);
            try
            {
                var synthetic = new JObject
                {
                    ["90001001"] = new JObject { ["type"] = 2 }, // 被动
                    ["90001002"] = new JObject { ["type"] = 1 }, // 主动(不应并入)
                };
                skillField.SetValue(null, synthetic);

                Shenxiao.Module.Core.Skill.SkillManager.Instance.Clear();
                byte[] pkt = new CliVerify.Pkt().H(2).I(90001001).I(90001002).Bytes();
                feed(m, ctrl, pkt);

                bool passiveIn = Shenxiao.Module.Core.Skill.SkillManager.Instance.GetSkill(90001001) != null
                    && Shenxiao.Module.Core.Skill.SkillManager.Instance.GetSkill(90001001).Level == 1;
                bool activeOut = Shenxiao.Module.Core.Skill.SkillManager.Instance.GetSkill(90001002) == null;
                bool ok = passiveIn && activeOut;
                Debug.Log("CLIVERIFY rolegrowth 13020 passiveIn=" + passiveIn + " activeOut=" + activeOut + " ok=" + ok);
                return ok;
            }
            finally
            {
                skillField.SetValue(null, saved);
                Shenxiao.Module.Core.Skill.SkillManager.Instance.Clear();
            }
        }

        // ---- 13036:三种 expType 分支(0 纯飘字/8 百分比飘字/2 toast)—— log 断言 ----
        private static bool Test13036(MethodInfo m, object ctrl, Action<MethodInfo, object, byte[]> feed)
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            bool noThrow = true;
            try
            {
                feed(m, ctrl, new CliVerify.Pkt().C(0).L(100).H(0).Bytes());     // expType0 → "经验 +100"
                feed(m, ctrl, new CliVerify.Pkt().C(8).L(50).H(150).Bytes());    // expType8 percent150(-100=50%) → "经验 +50 (+50%)"
                feed(m, ctrl, new CliVerify.Pkt().C(2).L(77).H(0).Bytes());      // expType2 → toast「获得经验 x77」
            }
            catch (Exception e) { noThrow = false; Debug.LogError("CLIVERIFY rolegrowth 13036 threw: " + e); }
            finally { Application.logMessageReceived -= cb; }

            bool plainOk = logs.Exists(l => l.Contains("经验 +100"));
            bool pctOk = logs.Exists(l => l.Contains("经验 +50 (+50%)"));
            bool toastOk = logs.Exists(l => l.Contains("获得经验 x77"));
            bool ok = noThrow && plainOk && pctOk && toastOk;
            Debug.Log("CLIVERIFY rolegrowth 13036 plainOk=" + plainOk + " pctOk=" + pctOk + " toastOk=" + toastOk
                + " noThrow=" + noThrow + " ok=" + ok);
            return ok;
        }

        // ---- 13046:"i" 绝对时间戳存储(勿当剩余秒,直接存原值) ----
        private static bool Test13046(MethodInfo m, object ctrl, Action<MethodInfo, object, byte[]> feed,
            Shenxiao.Module.Core.Role.RoleModel model)
        {
            const long ts = 1780000000L;
            feed(m, ctrl, new CliVerify.Pkt().I(ts).Bytes());
            bool ok = model.ChangeCareerTime == ts;
            Debug.Log("CLIVERIFY rolegrowth 13046 changeCareerTime=" + model.ChangeCareerTime + " ok=" + ok);
            return ok;
        }

        // ---- 13080(全量列表)/13081(推送激活,服务端字节序 Res:32,Id:64)/13083(设置头像,i,i,s)链 ----
        private static bool Test13080_81_83(MethodInfo m80, MethodInfo m81, MethodInfo m83, object ctrl,
            Action<MethodInfo, object, byte[]> feed, Shenxiao.Module.Core.Role.RoleModel model)
        {
            model.Figure = new Shenxiao.Common.Proto.FigureProto();

            feed(m80, ctrl, new CliVerify.Pkt().H(2).I(10).I(20).Bytes());
            bool listOk = model.HeadIdList.Contains(10) && model.HeadIdList.Contains(20)
                && model.IsHeadActivated(1) && model.IsHeadActivated(3) && !model.IsHeadActivated(99);

            bool activateFired = false;
            Action onListUpdate = () => activateFired = true;
            EventDispatcher.On(GlobalEvent.EVT_ROLE_HEAD_LIST_UPDATE, onListUpdate);
            feed(m81, ctrl, new CliVerify.Pkt().I(1).L(30).Bytes()); // Res:32=1, Id:64=30
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_HEAD_LIST_UPDATE, onListUpdate);
            bool activateOk = activateFired && model.HeadIdList.Contains(30);

            bool setFired = false;
            Action onSetSuccess = () => setFired = true;
            EventDispatcher.On(GlobalEvent.EVT_ROLE_HEAD_SET_SUCCESS, onSetSuccess);
            feed(m83, ctrl, new CliVerify.Pkt().I(1).I(5).S("30").Bytes()); // code=1, ver=5, idStr="30"
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_HEAD_SET_SUCCESS, onSetSuccess);
            bool setOk = setFired && model.Figure.Raw.TryGetValue("picture", out object pic) && (string)pic == "30";

            bool ok = listOk && activateOk && setOk;
            Debug.Log("CLIVERIFY rolegrowth headChain listOk=" + listOk + " activateOk=" + activateOk
                + " setOk=" + setOk + " ok=" + ok);
            return ok;
        }

        // ---- 改名:42601 成功(toast+事件)/ 失败码(服务端权威数值码 1009→"该名字已被使用") ----
        private static bool Test42601(MethodInfo m, object ctrl, Action<MethodInfo, object, byte[]> feed)
        {
            bool fired = false;
            Action onSuccess = () => fired = true;
            EventDispatcher.On(GlobalEvent.EVT_ROLE_RENAME_SUCCESS, onSuccess);
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                feed(m, ctrl, new CliVerify.Pkt().I(1).S("新名字").Bytes());
            }
            finally { Application.logMessageReceived -= cb; }
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_RENAME_SUCCESS, onSuccess);
            bool successOk = fired && logs.Exists(l => l.Contains("改名成功"));

            logs.Clear();
            Application.logMessageReceived += cb;
            bool noThrow = true;
            try { feed(m, ctrl, new CliVerify.Pkt().I(1009).S("新名字").Bytes()); } // name_exist(服务端权威数值码)
            catch (Exception e) { noThrow = false; Debug.LogError("CLIVERIFY rolegrowth 42601 fail threw: " + e); }
            finally { Application.logMessageReceived -= cb; }
            bool failOk = noThrow && logs.Exists(l => l.Contains("该名字已被使用"));

            bool ok = successOk && failOk;
            Debug.Log("CLIVERIFY rolegrowth 42601 successOk=" + successOk + " failOk=" + failOk + " ok=" + ok);
            return ok;
        }

        // ---- 13045:成功(Figure.career/sex 更新 + EVT_CAREER_CHANGED + 级联重拉不炸)/ 失败码不炸 ----
        private static bool Test13045(MethodInfo m, object ctrl, Action<MethodInfo, object, byte[]> feed,
            Shenxiao.Module.Core.Role.RoleModel model)
        {
            model.Figure = new Shenxiao.Common.Proto.FigureProto();

            (int career, int sex, bool fired) changed = (-1, -1, false);
            Action<int, int> onChanged = (c, s) => changed = (c, s, true);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_CAREER_CHANGED, onChanged);
            bool noThrowOk = true;
            try { feed(m, ctrl, new CliVerify.Pkt().I(1).S("").C(2).C(2).Bytes()); }
            catch (Exception e) { noThrowOk = false; Debug.LogError("CLIVERIFY rolegrowth 13045 success threw: " + e); }
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_CAREER_CHANGED, onChanged);
            bool successOk = noThrowOk && changed.fired && changed.career == 2 && changed.sex == 2
                && model.Figure.career == 2 && model.Figure.sex == 2;

            changed = (-1, -1, false);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_CAREER_CHANGED, onChanged);
            bool noThrowFail = true;
            try { feed(m, ctrl, new CliVerify.Pkt().I(2100010).S("").C(0).C(0).Bytes()); }
            catch (Exception e) { noThrowFail = false; Debug.LogError("CLIVERIFY rolegrowth 13045 fail threw: " + e); }
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_CAREER_CHANGED, onChanged);
            bool failOk = noThrowFail && !changed.fired;

            bool ok = successOk && failOk;
            Debug.Log("CLIVERIFY rolegrowth 13045 successOk=" + successOk + " failOk=" + failOk + " ok=" + ok);
            return ok;
        }

        // ---- 渲染段:TransferJobCreator 生成 → 实例化 → 喂 config_career/ClientTransfer → 断言卡片数 → 截图 ----
        private static async Task<int> RenderTransferJobCardAsync()
        {
            Shenxiao.Editor.UiCreator.TransferJob.TransferJobCreator.Generate();

            CliVerify.Stage stage = CliVerify.Stage.Create(); // 设 ResManager.EditorPreferFallback=true(AssetDatabase 兜底命中)
            var model = Shenxiao.Module.Core.Role.RoleModel.Instance;
            Shenxiao.Common.Proto.FigureProto savedFigure = model.Figure;
            try
            {
                await Shenxiao.Module.Core.TransferJob.TransferJobModel.EnsureLoaded();
                if (!Shenxiao.Module.Core.TransferJob.TransferJobModel.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY rolegrowth render FAIL config_career/ClientTransfer not loaded" +
                        "(檢查 Assets/GameRes/resource/config/{server/config_career.json, client/clienttransfer.json})");
                    return 3;
                }

                model.Figure = new Shenxiao.Common.Proto.FigureProto { career = 1 }; // 剑士,期望除自身外 3 张目标卡

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/TransferJob/TransferJobCardView.prefab");
                if (prefab == null)
                {
                    Debug.LogError("CLIVERIFY rolegrowth render FAIL TransferJobCardView.prefab missing after Generate()");
                    return 3;
                }
                GameObject go = UnityEngine.Object.Instantiate(prefab, stage.CanvasRoot);
                var view = go.GetComponent<Shenxiao.Module.Core.TransferJob.TransferJobCardView>();
                if (view == null)
                {
                    Debug.LogError("CLIVERIFY rolegrowth render FAIL prefab root missing TransferJobCardView component");
                    UnityEngine.Object.DestroyImmediate(go);
                    return 3;
                }
                view.gameObject.SetActive(true);
                view.Show();
                await Task.Delay(300);
                stage.ForceCjkFont();

                var items = go.GetComponentsInChildren<Shenxiao.Module.Core.TransferJob.TransferJobCardItem>(false);
                bool cardCountOk = items.Length == 3; // config_career 4 项,除自身职业(career=1)外 3 项

                string png = stage.Capture("Temp/round5_transferjob_card.png");
                Debug.Log("CLIVERIFY rolegrowth render cardCount=" + items.Length + "/3 cardCountOk=" + cardCountOk + " shot=" + png);

                UnityEngine.Object.DestroyImmediate(go);
                return cardCountOk ? 0 : 3;
            }
            finally
            {
                model.Figure = savedFigure;
                stage.Dispose();
            }
        }
    }
}
