using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// LookOver(他人资料卡 module1,轮21 §2 PL)实证:反射喂 FriendController.On19502 合成包驱动
    /// <see cref="Shenxiao.Module.Core.LookOver.Views.LookOverCardView"/> 渲染,断言:
    /// ① 自查拦截(陷阱③,lib_player_look_over.erl:89/:59 自己查自己零回包)——
    ///    Show(自己) 不应实例化任何面板(反射查 LookOverFlow._view 仍为 null);
    /// ② Show(他人) → 加载中态(lblLoading 显/infoGroup 隐)→ 喂匹配 role_id 的 19502 →
    ///    姓名/服务器/角色ID/战力/成就阶文本 + 装备/法阵/仙灵行数落地 + 截图。
    ///
    /// 独立用例文件,复用 CliVerify.Pkt/Stage,**不改 CliVerify.cs 本体**(该文件的 RenderAll/Run(...) 接线
    /// 是公共资源,不属本包所有权 Assets/Editor/CliVerify/Cases/ 之外的部分;留给下次touch CliVerify.cs 的改动
    /// 顺手补两行:`public static void LookOver() => Run(LookOverCase.Run, 200.0);` + RenderAll 调用点)。
    ///
    /// 独立跑法(不依赖 CliVerify.cs):
    /// Unity.exe -batchmode -projectPath . -executeMethod Shenxiao.EditorTools.LookOverCase.RunBatch
    ///   -logFile Temp/cliverify_lookover.log
    /// 日志前缀 "CLIVERIFY lookover"。
    /// </summary>
    public static class LookOverCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class CheckResult
        {
            public bool Modules;
            public bool Wire;
            public bool Filter;
            public bool Cache;
            public bool Clear;
        }

        /// <summary>自带批处理泵循环的独立入口(不依赖 CliVerify.cs 的 Run helper,后者是 private)。</summary>
        public static void RunBatch()
        {
            Task<int> task = null;
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 200.0;
            UnityEditor.EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                try
                {
                    if (task == null) task = Run();
                    if (task.IsCompleted)
                    {
                        UnityEditor.EditorApplication.update -= tick;
                        int code = task.IsFaulted ? 1 : task.Result;
                        if (task.IsFaulted) Debug.LogError("CLIVERIFY lookover EXCEPTION " + task.Exception);
                        Debug.Log("CLIVERIFY lookover EXIT " + code);
                        UnityEditor.EditorApplication.Exit(code);
                    }
                    else if (UnityEditor.EditorApplication.timeSinceStartup > deadline)
                    {
                        UnityEditor.EditorApplication.update -= tick;
                        Debug.LogError("CLIVERIFY lookover TIMEOUT");
                        UnityEditor.EditorApplication.Exit(2);
                    }
                }
                catch (Exception e)
                {
                    UnityEditor.EditorApplication.update -= tick;
                    Debug.LogError("CLIVERIFY lookover EXCEPTION " + e);
                    UnityEditor.EditorApplication.Exit(1);
                }
            };
            UnityEditor.EditorApplication.update += tick;
        }

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                bool selfGuardOk = RunSelfGuard();
                CheckResult checks = await RunRenderAsync(stage);
                bool modulesOk = selfGuardOk && checks.Modules;

                bool pass = modulesOk && checks.Wire && checks.Filter && checks.Cache && checks.Clear;
                Debug.Log("CLIVERIFY lookover VERDICT modules=" + modulesOk + " wire=" + checks.Wire
                    + " filter=" + checks.Filter + " cache=" + checks.Cache + " clear=" + checks.Clear + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        // =====================================================================================
        // ① 自查拦截(陷阱③)
        // =====================================================================================

        private static bool RunSelfGuard()
        {
            FieldInfo viewField = typeof(Shenxiao.Module.Core.LookOver.LookOverFlow).GetField("_view", SF);
            if (viewField == null)
            {
                Debug.LogError("CLIVERIFY lookover selfGuard: LookOverFlow._view 反射失败(字段改名?)");
                return false;
            }

            long savedSelfId = Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId;
            Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = 42;
            try
            {
                Shenxiao.Module.Core.LookOver.LookOverFlow.Show(42); // 点自己头像
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY lookover selfGuard threw: " + e);
                return false;
            }
            finally
            {
                Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = savedSelfId;
            }

            object view = viewField.GetValue(null);
            bool noInstance = view == null;
            Debug.Log("CLIVERIFY lookover selfGuard Show(自己) 后 _view==null: " + noInstance);
            return noInstance;
        }

        // =====================================================================================
        // ② Show(他人) → 加载中态 → 19502 落地渲染
        // =====================================================================================

        private static async Task<CheckResult> RunRenderAsync(CliVerify.Stage stage)
        {
            var checks = new CheckResult();
            const long roleId = 55001;
            long savedSelfId = Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId;
            Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = 1; // 与 roleId 不同,不触发自查

            Shenxiao.Module.Core.LookOver.LookOverFlow.Show(roleId);

            Shenxiao.Module.Core.LookOver.Views.LookOverCardView view = null;
            for (int i = 0; i < 40 && view == null; i++)
            {
                await Task.Delay(50);
                var arr = UnityEngine.Object.FindObjectsByType<Shenxiao.Module.Core.LookOver.Views.LookOverCardView>(
                    FindObjectsSortMode.None);
                if (arr.Length > 0) view = arr[0];
            }
            if (view == null)
            {
                Debug.LogError("CLIVERIFY lookover render: LookOverCardView 未能实例化(prefab/Addressable 缺失,先跑 LookOverCardCreator.GenerateBatch)");
                Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = savedSelfId;
                return checks;
            }

            bool loadingOk = view.lblLoading != null && view.lblLoading.gameObject.activeSelf
                && view.infoGroup != null && !view.infoGroup.activeSelf;
            Debug.Log("CLIVERIFY lookover render 加载中态 loadingOk=" + loadingOk);

            object ctrl = Shenxiao.Module.Core.Friend.FriendController.Instance;
            MethodInfo m19502 = ctrl.GetType().GetMethod("On19502", F);
            if (m19502 == null)
            {
                Debug.LogError("CLIVERIFY lookover render: FriendController.On19502 反射失败");
                Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = savedSelfId;
                return checks;
            }

            byte[] p19502 = new CliVerify.Pkt().H(7).L(roleId).L(66666).H(9)
                .AppendMinimalFigure("卡片验证")
                .H(1).L(8001).I(520200).H(3).C(2).H(15).C(4).H(25).H(35).H(2) // equip item
                .H(1).C(5).C(1).C(0).I(1900000099) // magic circle item
                .H(1).H(3).C(1) // fairy item
                .Bytes();
            m19502.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(p19502, 0, p19502.Length) });

            await Task.Delay(150);
            stage.ForceCjkFont();

            bool renderedOk = view.lblLoading != null && !view.lblLoading.gameObject.activeSelf
                && view.infoGroup != null && view.infoGroup.activeSelf
                && view.lblName != null && view.lblName.text == "卡片验证"
                && view.lblServer != null && view.lblServer.text == "服务器 7"
                && view.lblRoleId != null && view.lblRoleId.text == "ID " + roleId
                && view.lblCombat != null && view.lblCombat.text == "战力 66666"
                && view.lblAchv != null && view.lblAchv.text == "成就阶 9";

            int rowCount = view.listDetail != null && view.listDetail.content != null ? view.listDetail.content.childCount : -1;
            bool rowsOk = rowCount == 4; // "装备1件" 标题行 + 1装备 + 1法阵 + 1仙灵

            CheckResult extended = await RunExtendedAsync(view, roleId, 7);
            checks.Modules = loadingOk && renderedOk && rowsOk && extended.Modules;
            checks.Wire = extended.Wire;
            checks.Filter = extended.Filter;
            checks.Cache = extended.Cache;
            checks.Clear = extended.Clear;

            string png = stage.Capture("Temp/round21_lookover_card.png");
            Debug.Log("CLIVERIFY lookover render renderedOk=" + renderedOk + " rowCount=" + rowCount + " rowsOk=" + rowsOk
                + " name=" + view.lblName?.text + " server=" + view.lblServer?.text + " roleId=" + view.lblRoleId?.text
                + " combat=" + view.lblCombat?.text + " achv=" + view.lblAchv?.text + " shot=" + png);

            Shenxiao.Module.Core.LookOver.LookOverFlow.Close();
            Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = savedSelfId;
            return checks;
        }

        /// <summary>按 FigureProto.SCHEMA 字段序逐项写一个全零/空的最小 Figure 块(与 FriendMailCase/ChatCase
        /// 的 AppendMinimalFigure 逐字节相同,独立文件各自持有一份,避免跨用例文件耦合;name 可变,其余全零)。</summary>
        private static async Task<CheckResult> RunExtendedAsync(
            Shenxiao.Module.Core.LookOver.Views.LookOverCardView view, long roleId, int serverId)
        {
            var result = new CheckResult();
            Shenxiao.Module.Core.Friend.FriendController ctrl = Shenxiao.Module.Core.Friend.FriendController.Instance;
            Shenxiao.Module.Core.Friend.FriendModel model = Shenxiao.Module.Core.Friend.FriendModel.Instance;
            Type controllerType = ctrl.GetType();
            FieldInfo interceptField = controllerType.GetField("s_lookOverOutboundIntercept", SF);
            var handlers = new Dictionary<int, MethodInfo>();
            bool reflectionOk = interceptField != null;
            for (int cmd = 19503; cmd <= 19512; cmd++)
            {
                MethodInfo handler = controllerType.GetMethod("On" + cmd, F);
                handlers[cmd] = handler;
                reflectionOk &= handler != null;
            }
            if (!reflectionOk)
            {
                Debug.LogError("CLIVERIFY lookover PK-C reflection contract missing");
                return result;
            }

            object savedIntercept = interceptField.GetValue(null);
            var outbound = new List<byte[]>();
            Func<byte[], bool> intercept = frame => { outbound.Add(frame); return true; };
            try
            {
                interceptField.SetValue(null, intercept);

                ctrl.RequestPlayerCard(roleId, 1, 7);
                ctrl.RequestPlayerCard(roleId, 12, 9);
                int validCount = outbound.Count;
                result.Wire = validCount == 2
                    && FrameEquals(outbound[0], 19501, new CliVerify.Pkt().H(7).L(roleId).H(1).Bytes())
                    && FrameEquals(outbound[1], 19501, new CliVerify.Pkt().H(9).L(roleId).H(12).Bytes());

                ctrl.RequestPlayerCard(0, 1, 7);
                ctrl.RequestPlayerCard(roleId, 0, 7);
                ctrl.RequestPlayerCard(roleId, 13, 7);
                result.Filter = outbound.Count == validCount;

                var cases = new (int Cmd, int Variant, int ModuleId)[]
                {
                    (19503, 0, 2), (19504, 3, 3), (19504, 4, 4), (19505, 0, 6),
                    (19506, 0, 5), (19507, 0, 7), (19508, 0, 8), (19509, 0, 9),
                    (19510, 0, 10), (19511, 0, 11), (19512, 0, 12),
                };
                bool modulesOk = true;
                foreach ((int cmd, int variant, int moduleId) in cases)
                {
                    ctrl.RequestPlayerCard(roleId, moduleId, serverId);
                    byte[] payload = BuildModulePayload(cmd, variant);
                    var reader = new Shenxiao.Framework.Net.NetReader(payload, 0, payload.Length);
                    handlers[cmd].Invoke(ctrl, new object[] { reader });
                    Shenxiao.Module.Core.Friend.LookOverModuleSnapshot snapshot =
                        model.GetLookOverModule(roleId, moduleId);
                    modulesOk &= reader.Remaining == 0 && snapshot != null
                        && snapshot.RoleId == roleId && snapshot.ServerId == serverId
                        && snapshot.ModuleId == moduleId && snapshot.PrimaryPower == ExpectedPrimary(cmd, variant);
                    if (cmd == 19503) modulesOk &= RowsContain(snapshot, "33001234", "3210");
                    if (cmd == 19506) modulesOk &= RowsContain(snapshot, "66001122", "6123", "66002233");
                    if (cmd == 19511) modulesOk &= RowsContain(snapshot, "77110011", "70001", "77112233");
                }
                result.Modules = modulesOk;

                model.ClearLookOverModules();
                view.SelectModule(2);
                ctrl.RequestPlayerCard(roleId, 3, serverId);
                Feed(handlers[19504], ctrl, BuildModulePayload(19504, 3), out bool sys3Consumed);
                await Task.Delay(50);
                bool outOfOrderIgnored = sys3Consumed && view.lblLoading != null && view.lblLoading.gameObject.activeSelf;

                ctrl.RequestPlayerCard(roleId, 2, serverId);
                Feed(handlers[19503], ctrl, BuildModulePayload(19503, 0), out bool ballConsumed);
                await Task.Delay(50);
                bool module2Rendered = ballConsumed && LabelEndsWith(view.lblCombat, "2030003")
                    && LabelEndsWith(view.lblAchv, "2");

                view.SelectModule(3);
                await Task.Delay(50);
                bool module3Rendered = LabelEndsWith(view.lblCombat, "2041003") && LabelEndsWith(view.lblAchv, "3");
                Shenxiao.Module.Core.Friend.LookOverModuleSnapshot cached2 = model.GetLookOverModule(roleId, 2);
                Shenxiao.Module.Core.Friend.LookOverModuleSnapshot cached3 = model.GetLookOverModule(roleId, 3);
                result.Cache = outOfOrderIgnored && module2Rendered && module3Rendered
                    && cached2 != null && cached3 != null && !ReferenceEquals(cached2, cached3)
                    && cached2.ModuleId == 2 && cached3.ModuleId == 3;

                view.SelectModule(1);
                await Task.Delay(50);
                model.Reset();
                bool clearOk = model.LastLookOverModule == null;
                for (int moduleId = 2; moduleId <= 12; moduleId++)
                    clearOk &= model.GetLookOverModule(roleId, moduleId) == null;
                result.Clear = clearOk;
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY lookover PK-C exception " + e);
            }
            finally
            {
                interceptField.SetValue(null, savedIntercept);
            }
            return result;
        }

        private static void Feed(MethodInfo handler, object ctrl, byte[] payload, out bool consumed)
        {
            var reader = new Shenxiao.Framework.Net.NetReader(payload, 0, payload.Length);
            handler.Invoke(ctrl, new object[] { reader });
            consumed = reader.Remaining == 0;
        }

        private static long ExpectedPrimary(int cmd, int variant)
        {
            if (cmd == 19504) return 2041000 + variant;
            switch (cmd)
            {
                case 19503: return 2030003;
                case 19505: return 2050005;
                case 19506: return 2060006;
                case 19507: return 2070007;
                case 19508: return 2080008;
                case 19509: return 2090009;
                case 19510: return 2100010;
                case 19511: return 4221022; // CompanionPower + DemonsPower
                case 19512: return 2120012;
                default: return -1;
            }
        }

        private static byte[] BuildModulePayload(int cmd, int variant)
        {
            switch (cmd)
            {
                case 19503:
                    return new CliVerify.Pkt().L(2030003).C(1)
                        .H(1).I(33001234).H(3210).H(1).C(17).C(29).Bytes();
                case 19504:
                    return new CliVerify.Pkt().C(variant).L(2040000 + variant).L(2041000 + variant)
                        .H(0).H(0).H(0).H(0).H(0).H(0).H(0).Bytes();
                case 19505:
                    return new CliVerify.Pkt().H(650).H(651).L(2050005).L(2051005)
                        .H(0).H(0).H(0).Bytes();
                case 19506:
                    return new CliVerify.Pkt().L(2060006).H(1)
                        .H(1).C(5).C(1).L(66001122)
                            .H(1).C(2).H(6123).H(23).H(34).L(66002233).I(1900000000)
                                .H(0).H(0).H(0)
                        .H(0).H(0).H(0).Bytes();
                case 19507:
                    return new CliVerify.Pkt().L(2070007).H(0).Bytes();
                case 19508:
                    return new CliVerify.Pkt().L(2080008).H(0).Bytes();
                case 19509:
                    return new CliVerify.Pkt().L(2090009).H(209).H(0).Bytes();
                case 19510:
                    return new CliVerify.Pkt().L(2100010).C(5).C(2).H(0).Bytes();
                case 19511:
                    return new CliVerify.Pkt().L(2110011)
                        .H(1).C(7).H(21).H(8).C(1).H(99).L(77110011).H(0)
                        .L(2111011).I(70001)
                        .H(1).I(70001).H(35).C(9).C(4).L(77112233).H(0).H(0).Bytes();
                case 19512:
                    return new CliVerify.Pkt().L(2120012).C(12).H(0).Bytes();
                default:
                    throw new ArgumentOutOfRangeException(nameof(cmd));
            }
        }

        private static bool RowsContain(
            Shenxiao.Module.Core.Friend.LookOverModuleSnapshot snapshot, params string[] tokens)
        {
            IReadOnlyList<string> rows = snapshot.BuildRows();
            string joined = rows == null ? "" : string.Join("|", rows);
            foreach (string token in tokens) if (!joined.Contains(token)) return false;
            return true;
        }

        private static bool LabelEndsWith(TMPro.TextMeshProUGUI label, string suffix) =>
            label != null && label.text != null && label.text.EndsWith(suffix, StringComparison.Ordinal);

        private static bool FrameEquals(byte[] frame, int protoId, byte[] payload)
        {
            if (frame == null || payload == null || frame.Length != payload.Length + 6) return false;
            if (((frame[0] << 8) | frame[1]) != frame.Length
                || ((frame[2] << 8) | frame[3]) != 1000
                || ((frame[4] << 8) | frame[5]) != protoId) return false;
            for (int i = 0; i < payload.Length; i++) if (frame[i + 6] != payload[i]) return false;
            return true;
        }

        private static CliVerify.Pkt AppendMinimalFigure(this CliVerify.Pkt p, string name)
        {
            return p
                .S(name)  // name
                .C(0)     // sex
                .C(0)     // realm
                .C(0)     // career
                .H(0)     // level
                .C(0)     // GM
                .C(0)     // vip_flag
                .C(0)     // is_hide_vip
                .C(0)     // touxian
                .H(0)     // level_model_list count
                .H(0)     // fashion_model_list count
                .S("")    // picture
                .I(0)     // prcture_ver
                .L(0)     // guild_id
                .S("")    // guild_name
                .C(0)     // position
                .S("")    // position_name
                .I(0)     // dsgt_id
                .I(0)     // liveness_id
                .C(0)     // turn
                .C(0)     // turn_stage
                .C(0)     // grade_id
                .C(0)     // is_marriage
                .L(0)     // marriage_id
                .S("")    // marriage_name
                .I(0)     // escort_state
                .I(0)     // block_id
                .I(0)     // house_id
                .H(0)     // house_lv
                .H(0)     // figure_list count
                .H(0)     // figure_ride_list count
                .H(0)     // achv_lv
                .H(0)     // medal_id
                .I(0)     // fazhen_id
                .H(0)     // dress_list count
                .I(0)     // god_id
                .I(0)     // revelation_suit
                .I(0)     // demon_id
                .C(0)     // supreme_vip
                .I(0)     // title_id
                .C(0)     // mask_id
                .C(0)     // seaCamp
                .C(0)     // brick_id
                .C(0)     // dummy_type
                .C(0)     // suit_fashion_id
                .C(0);    // collect_state
        }
    }
}
