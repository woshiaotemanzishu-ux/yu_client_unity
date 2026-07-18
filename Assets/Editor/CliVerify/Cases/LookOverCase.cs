using System;
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
                bool renderOk = await RunRenderAsync(stage);

                bool pass = selfGuardOk && renderOk;
                Debug.Log("CLIVERIFY lookover VERDICT selfGuard=" + selfGuardOk + " render=" + renderOk + " pass=" + pass);
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

        private static async Task<bool> RunRenderAsync(CliVerify.Stage stage)
        {
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
                return false;
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
                return false;
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

            string png = stage.Capture("Temp/round21_lookover_card.png");
            Debug.Log("CLIVERIFY lookover render renderedOk=" + renderedOk + " rowCount=" + rowCount + " rowsOk=" + rowsOk
                + " name=" + view.lblName?.text + " server=" + view.lblServer?.text + " roleId=" + view.lblRoleId?.text
                + " combat=" + view.lblCombat?.text + " achv=" + view.lblAchv?.text + " shot=" + png);

            Shenxiao.Module.Core.LookOver.LookOverFlow.Close();
            Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = savedSelfId;
            Shenxiao.Module.Core.Friend.FriendModel.Instance.Reset();

            return loadingOk && renderedOk && rowsOk;
        }

        /// <summary>按 FigureProto.SCHEMA 字段序逐项写一个全零/空的最小 Figure 块(与 FriendMailCase/ChatCase
        /// 的 AppendMinimalFigure 逐字节相同,独立文件各自持有一份,避免跨用例文件耦合;name 可变,其余全零)。</summary>
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
