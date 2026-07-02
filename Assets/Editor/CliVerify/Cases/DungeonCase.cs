using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 通用副本进出结算壳(御魂本)实证:config_dungeon 同步 + 61001(进入成功/失败)/61020(状态)/
    /// 61013(结算包,读序烟测)合成包驱动 DungeonModel/Controller,断言字段套值 + 失败/错误码包不抛异常;
    /// 再拉起 DungeonRuneShellView 渲染断言含「御魂」文本。独立文件复用 CliVerify.Stage/Pkt/FindDeep
    /// (已 public),不改 CliVerify.cs 本体(主控统一接 RenderAll)。日志前缀统一 "CLIVERIFY dungeon"。
    ///
    /// ⚠诚实标注:真实进副本需活服+场景切换(61001 成功后服务端才真正把角色切场景,61003/61020 也要活服
    /// 实际推送)。本用例只用手工组的合成包驱动读序与状态机断言,不代表已验证过真实服务端交互。
    /// </summary>
    public static class DungeonCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Dungeon.DungeonConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Dungeon.DungeonConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY dungeon FAIL config_dungeon not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.Dungeon.DungeonController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo m61001 = ctrl.GetType().GetMethod("On61001", F);
                System.Reflection.MethodInfo m61003 = ctrl.GetType().GetMethod("On61003", F);
                System.Reflection.MethodInfo m61020 = ctrl.GetType().GetMethod("On61020", F);
                if (m61001 == null || m61003 == null || m61020 == null)
                {
                    Debug.LogError("CLIVERIFY dungeon handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.Dungeon.DungeonModel model = Shenxiao.Module.Core.Dungeon.DungeonModel.Instance;
                model.Clear();

                // 61001 进入成功:dun_id=12001, scene_id=2005, error_code=1, error_code_args=""。
                byte[] p61001Ok = ConcatBytes(new CliVerify.Pkt()
                    .I(12001)      // dun_id
                    .I(2005)       // scene_id
                    .I(1)          // error_code(1=成功)
                    .Bytes(), EmptyString());   // error_code_args(u16 len=0,对标 NetReader.ReadString)
                Feed(m61001, p61001Ok);
                bool enterOk = model.InDungeonId == 12001;
                Debug.Log("CLIVERIFY dungeon 61001 ok inDungeon=" + model.InDungeonId + " name=" + Shenxiao.Module.Core.Dungeon.DungeonConfigs.GetName(12001) + " ok=" + enterOk);

                // 61001 进入失败:error_code=1200001(场景不可进入),只要不抛异常(走 toast log 分支)即过。
                byte[] p61001Fail = ConcatBytes(new CliVerify.Pkt().I(12002).I(0).I(1200001).Bytes(), EmptyString());
                bool enterFailNoThrow = true;
                try { Feed(m61001, p61001Fail); }
                catch (System.Exception e) { enterFailNoThrow = false; Debug.LogError("CLIVERIFY dungeon 61001 fail threw: " + e); }
                Debug.Log("CLIVERIFY dungeon 61001 fail noThrow=" + enterFailNoThrow);

                // 61020 副本状态:dun_type=12(御魂本), dun_list=1 项(dun_id=12001,各计数字段互不相同便于断言,rec_data 空)。
                byte[] p61020 = new CliVerify.Pkt()
                    .C(12)         // dun_type
                    .H(1)          // dun_list 计数
                        .I(12001)      // dun_id
                        .H(1)          // daily_count
                        .H(2)          // weekly_count
                        .H(3)          // permanent_count
                        .H(4)          // reset_count
                        .H(5)          // vip_count
                        .H(6)          // add_count
                        .C(1)          // is_sweep
                        .H(0)          // rec_data 计数
                    .Bytes();
                Feed(m61020, p61020);
                Shenxiao.Module.Core.Dungeon.DungeonModel.DunState state = model.GetState(12, 12001);
                bool stateOk = state != null && state.DailyCount == 1 && state.WeeklyCount == 2
                    && state.PermanentCount == 3 && state.ResetCount == 4 && state.VipCount == 5
                    && state.AddCount == 6 && state.IsSweep;
                Debug.Log("CLIVERIFY dungeon 61020 state=" + (state != null) + " daily=" + state?.DailyCount
                    + " sweep=" + state?.IsSweep + " ok=" + stateOk);

                // 61003 结算成功(通用结算界面,御魂本实证走 61003,非 61013——见 DungeonController 头注):
                // result=1, result_subtype=0, dun_id=12001, grade=1, scene_id=2005,
                // reward_list=2 项(货币 style=3 金币 typeId=0 count=1000;物品 style=0 typeId=520100 count=1),
                // other_reward=1 组(reward_type=1,内 1 项),ex_data=0,count=1。
                byte[] p61003Ok = new CliVerify.Pkt()
                    .C(1)          // result
                    .C(0)          // result_subtype
                    .I(12001)      // dun_id
                    .C(1)          // grade
                    .I(2005)       // scene_id
                    .H(2)          // reward_list 计数
                        .C(3).I(0).L(1000).L(0)          // 金币 style=3,typeId=0,count=1000,goods_id=0
                        .C(0).I(520100).L(1).L(9001)      // 物品 style=0,typeId=520100,count=1,goods_id=9001
                    .H(1)          // other_reward 计数
                        .C(1)          // reward_type
                        .H(1)          // other_reward_list 计数
                            .C(0).I(520100).L(1).L(9002)
                    .H(0)          // ex_data 计数
                    .C(1)          // count
                    .Bytes();
                Feed(m61003, p61003Ok);
                bool settleOk = model.LastSettleResult == 1 && model.LastSettleRewards.Count == 3;   // 2 reward_list + 1 other_reward
                Debug.Log("CLIVERIFY dungeon 61003 settle result=" + model.LastSettleResult
                    + " rewards=" + model.LastSettleRewards.Count + " ok=" + settleOk);

                // 61003 结算失败:result=0(非1),只要不抛异常即过,且 InDungeonId 被清。
                byte[] p61003Fail = new CliVerify.Pkt()
                    .C(0).C(0).I(12001).C(1).I(2005)
                    .H(0)          // reward_list
                    .H(0)          // other_reward
                    .H(0)          // ex_data
                    .C(0)          // count
                    .Bytes();
                bool settleFailNoThrow = true;
                try { Feed(m61003, p61003Fail); }
                catch (System.Exception e) { settleFailNoThrow = false; Debug.LogError("CLIVERIFY dungeon 61003 fail threw: " + e); }
                bool settleFailClearedDun = model.InDungeonId == 0;
                Debug.Log("CLIVERIFY dungeon 61003 fail noThrow=" + settleFailNoThrow + " inDungeonCleared=" + settleFailClearedDun);

                Shenxiao.Module.Core.Dungeon.DungeonRuneShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round18_dungeon_shell.png");

                bool textOk = false;
                foreach (TMP_Text t in stage.CanvasRoot.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.text != null && t.text.Contains("御魂")) { textOk = true; break; }
                }
                Debug.Log("CLIVERIFY dungeon shell textOk=" + textOk + " shot=" + png);

                bool pass = enterOk && enterFailNoThrow && stateOk && settleOk && settleFailNoThrow && settleFailClearedDun && textOk;
                Debug.Log("CLIVERIFY dungeon VERDICT enterOk=" + enterOk + " enterFailNoThrow=" + enterFailNoThrow
                    + " stateOk=" + stateOk + " settleOk=" + settleOk + " settleFailNoThrow=" + settleFailNoThrow
                    + " settleFailClearedDun=" + settleFailClearedDun + " textOk=" + textOk + " pass=" + pass);

                Shenxiao.Module.Core.Dungeon.DungeonRuneShellView.Close();
                Shenxiao.Module.Core.Dungeon.DungeonModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        /// <summary>空字符串的线格式(u16 len=0,无内容;对标 NetReader.ReadString)。CliVerify.Pkt 没有 S() 写串
        /// 方法(不改 CliVerify.cs 本体),error_code_args 这类尾随字符串手工拼字节。</summary>
        private static byte[] EmptyString() => new byte[] { 0, 0 };

        private static byte[] ConcatBytes(byte[] a, byte[] b)
        {
            var r = new byte[a.Length + b.Length];
            System.Array.Copy(a, 0, r, 0, a.Length);
            System.Array.Copy(b, 0, r, a.Length, b.Length);
            return r;
        }
    }
}
