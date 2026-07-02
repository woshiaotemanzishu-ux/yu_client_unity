using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 收尾三件套实证(第20轮工单):灵魄强化 16702 成功/失败包断言;神装合成 15020 成功包断言不抛
    /// + config_goods_compose 加载断言(type=2 规则数&gt;0);排位赛 28001 页面信息包(字段可全 0)断言不抛。
    /// 纯逻辑用例(无壳渲染/截图),复用 CliVerify.Stage/Pkt(均已 public),不改 CliVerify.cs 本体
    /// (主控统一接 RenderAll)。独立文件避免多代理改 CliVerify.cs 冲突。日志前缀统一 "CLIVERIFY finalslice"。
    /// </summary>
    public static class FinalSliceCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                bool runeOk = RunRuneUpgrade();
                bool composeOk = await RunComposeAsync();
                bool jjcOk = RunJjcInfo();

                bool pass = runeOk && composeOk && jjcOk;
                Debug.Log("CLIVERIFY finalslice VERDICT runeOk=" + runeOk + " composeOk=" + composeOk
                    + " jjcOk=" + jjcOk + " pass=" + pass);

                await Task.CompletedTask;
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        /// <summary>灵魄强化 16702:成功包(code=1)套值 RunePoint + 事件不抛;失败包(code!=1,常见 err167 经验不足)
        /// 走 Toast 显码分支不抛异常。反射喂 RuneController 私有 On16702。</summary>
        private static bool RunRuneUpgrade()
        {
            object ctrl = Shenxiao.Module.Core.Rune.RuneController.Instance;
            const System.Reflection.BindingFlags F =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            System.Reflection.MethodInfo m16702 = ctrl.GetType().GetMethod("On16702", F);
            if (m16702 == null)
            {
                Debug.LogError("CLIVERIFY finalslice rune handler missing (reflection)");
                return false;
            }
            void Feed(byte[] pkt) => m16702.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

            Shenxiao.Module.Core.Rune.RuneModel model = Shenxiao.Module.Core.Rune.RuneModel.Instance;
            model.Clear();

            // 16702 强化成功:code=1, rune_point=88, goods_id=1234567。
            byte[] pOk = new CliVerify.Pkt().I(1).I(88).L(1234567).Bytes();
            Feed(pOk);
            bool successOk = model.RunePoint == 88;
            Debug.Log("CLIVERIFY finalslice rune 16702 ok rune_point=" + model.RunePoint + " ok=" + successOk);

            // 16702 强化失败:code=167(经验不足),只要不抛异常(走 Toast 显码分支)即过,数据不应回退。
            byte[] pFail = new CliVerify.Pkt().I(167).I(0).L(1234567).Bytes();
            bool failNoThrow = true;
            try { Feed(pFail); }
            catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY finalslice rune 16702 fail threw: " + e); }
            bool dataUnchanged = model.RunePoint == 88;
            Debug.Log("CLIVERIFY finalslice rune 16702 fail noThrow=" + failNoThrow + " dataUnchanged=" + dataUnchanged);

            model.Clear();
            bool pass = successOk && failNoThrow && dataUnchanged;
            Debug.Log("CLIVERIFY finalslice rune VERDICT pass=" + pass);
            return pass;
        }

        /// <summary>神装合成 15020:config_goods_compose 加载断言(type=2 规则数&gt;0)+ 成功包(code=1)
        /// 套值不抛。反射喂 ComposeController 私有 On15020。</summary>
        private static async Task<bool> RunComposeAsync()
        {
            Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
            await Shenxiao.Module.Core.Compose.ComposeConfigs.EnsureLoaded();
            if (!Shenxiao.Module.Core.Compose.ComposeConfigs.IsLoaded)
            {
                Debug.LogError("CLIVERIFY finalslice compose FAIL config_goods_compose not loaded");
                return false;
            }
            int equipRuleCount = Shenxiao.Module.Core.Compose.ComposeConfigs.CountEquipRules();
            bool configOk = equipRuleCount > 0;
            Debug.Log("CLIVERIFY finalslice compose config type2RuleCount=" + equipRuleCount + " ok=" + configOk);

            object ctrl = Shenxiao.Module.Core.Compose.ComposeController.Instance;
            const System.Reflection.BindingFlags F =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            System.Reflection.MethodInfo m15020 = ctrl.GetType().GetMethod("On15020", F);
            if (m15020 == null)
            {
                Debug.LogError("CLIVERIFY finalslice compose handler missing (reflection)");
                return false;
            }
            void Feed(byte[] pkt) => m15020.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

            Shenxiao.Module.Core.Compose.ComposeModel model = Shenxiao.Module.Core.Compose.ComposeModel.Instance;
            model.Clear();

            // 15020 合成成功包:code=1, compose_type=2, rule_id=110400101, goods_id=987654。
            byte[] pOk = new CliVerify.Pkt().I(1).C(2).I(110400101).L(987654).Bytes();
            bool okNoThrow = true;
            try { Feed(pOk); }
            catch (System.Exception e) { okNoThrow = false; Debug.LogError("CLIVERIFY finalslice compose 15020 ok threw: " + e); }
            bool valueOk = model.LastCode == 1 && model.LastRuleId == 110400101 && model.LastGoodsId == 987654;
            Debug.Log("CLIVERIFY finalslice compose 15020 ok noThrow=" + okNoThrow + " valueOk=" + valueOk);

            // 15020 合成失败包:code!=1,不抛异常即过(走 Toast 显码分支)。
            byte[] pFail = new CliVerify.Pkt().I(1500).C(0).I(110400101).L(0).Bytes();
            bool failNoThrow = true;
            try { Feed(pFail); }
            catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY finalslice compose 15020 fail threw: " + e); }
            Debug.Log("CLIVERIFY finalslice compose 15020 fail noThrow=" + failNoThrow);

            model.Clear();
            bool pass = configOk && okNoThrow && valueOk && failNoThrow;
            Debug.Log("CLIVERIFY finalslice compose VERDICT configOk=" + configOk + " okNoThrow=" + okNoThrow
                + " valueOk=" + valueOk + " failNoThrow=" + failNoThrow + " pass=" + pass);
            return pass;
        }

        /// <summary>排位赛 28001 页面信息:字段可全 0 的合成包,断言不抛异常(工单允许"字段可全0",
        /// 28002/28003 figure 嵌套块未强制要求本用例覆盖)。反射喂 JjcController 私有 On28001。</summary>
        private static bool RunJjcInfo()
        {
            object ctrl = Shenxiao.Module.Core.Jjc.JjcController.Instance;
            const System.Reflection.BindingFlags F =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            System.Reflection.MethodInfo m28001 = ctrl.GetType().GetMethod("On28001", F);
            if (m28001 == null)
            {
                Debug.LogError("CLIVERIFY finalslice jjc handler missing (reflection)");
                return false;
            }
            void Feed(byte[] pkt) => m28001.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

            Shenxiao.Module.Core.Jjc.JjcModel model = Shenxiao.Module.Core.Jjc.JjcModel.Instance;
            model.Clear();

            // 28001 字段全 0:rank/history_rank/reward_rank/combat/hp/num/num_refresh/honour/is_reward/pet_id 全 0,
            // break_id_list 空表(u16 计数=0)。
            byte[] pZero = new CliVerify.Pkt()
                .I(0)      // rank
                .I(0)      // history_rank
                .I(0)      // reward_rank
                .L(0)      // combat
                .I(0)      // hp
                .H(0)      // num
                .I(0)      // num_refresh
                .I(0)      // honour
                .C(0)      // is_reward
                .I(0)      // pet_id
                .H(0)      // break_id_list 计数=0
                .Bytes();
            bool noThrow = true;
            try { Feed(pZero); }
            catch (System.Exception e) { noThrow = false; Debug.LogError("CLIVERIFY finalslice jjc 28001 threw: " + e); }
            bool dataOk = model.HasInfo && model.Rank == 0 && model.Num == 0 && model.BreakIdList.Count == 0;
            Debug.Log("CLIVERIFY finalslice jjc 28001 noThrow=" + noThrow + " dataOk=" + dataOk);

            model.Clear();
            bool pass = noThrow && dataOk;
            Debug.Log("CLIVERIFY finalslice jjc VERDICT pass=" + pass);
            return pass;
        }
    }
}
