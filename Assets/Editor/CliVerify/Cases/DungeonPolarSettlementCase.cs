using System;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>50805 周本专属 S2C 结算快照：完整读序、原序重复项、空表 loaded 与环境恢复。</summary>
    public static class DungeonPolarSettlementCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            PolarModel model = PolarModel.Instance;
            bool oldHas = model.HasSettlement;
            PolarModel.SettlementSnapshot oldSnapshot = model.Settlement;
            bool pass = false;
            bool restored = false;
            try
            {
                MethodInfo handler = typeof(DungeonController).GetMethod("On50805", InstancePrivate);
                pass = handler != null && Proto.POLAR_SETTLEMENT == 50805;

                var full = new CliVerify.Pkt()
                    .C(2).I(uint.MaxValue).I(123456)
                    .H(2)
                        .C(1).H(3).H(2).C(4).I(4001).I(5).C(4).I(4001).I(6)
                        .C(2).H(0).H(0)
                    .H(2)
                        .I(5001).C(1).H(1).C(5).I(50001).I(uint.MaxValue)
                        .I(5001).C(0).H(0)
                    .Bytes();
                pass &= Feed(handler, full);
                PolarModel.SettlementSnapshot snapshot = model.Settlement;
                pass &= model.HasSettlement && snapshot != null
                    && snapshot.ResultType == 2 && snapshot.DunId == uint.MaxValue && snapshot.GoTime == 123456
                    && snapshot.DungeonRewards.Count == 2 && snapshot.DungeonRewards[0].Type == 1
                    && snapshot.DungeonRewards[0].Times == 3 && snapshot.DungeonRewards[0].Rewards.Count == 2
                    && snapshot.DungeonRewards[0].Rewards[0].TypeId == 4001
                    && snapshot.DungeonRewards[0].Rewards[1].Num == 6
                    && snapshot.DungeonRewards[1].Type == 2 && snapshot.DungeonRewards[1].Rewards.Count == 0
                    && snapshot.RoleBosses.Count == 2 && snapshot.RoleBosses[0].BossId == 5001
                    && snapshot.RoleBosses[0].RewardState == 1
                    && snapshot.RoleBosses[0].Rewards[0].Num == uint.MaxValue
                    && snapshot.RoleBosses[1].BossId == 5001 && snapshot.RoleBosses[1].Rewards.Count == 0;

                pass &= Feed(handler, new CliVerify.Pkt().C(0).I(0).I(0).H(0).H(0).Bytes());
                snapshot = model.Settlement;
                pass &= model.HasSettlement && snapshot != null && snapshot.ResultType == 0
                    && snapshot.DunId == 0 && snapshot.GoTime == 0
                    && snapshot.DungeonRewards.Count == 0 && snapshot.RoleBosses.Count == 0;
                Debug.Log("CLIVERIFY dungeonpolarsettlement VERDICT pass=" + pass);
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeonpolarsettlement EXCEPTION " + e);
                pass = false;
            }
            finally
            {
                typeof(PolarModel).GetProperty(nameof(PolarModel.HasSettlement))?.SetValue(model, oldHas);
                typeof(PolarModel).GetProperty(nameof(PolarModel.Settlement))?.SetValue(model, oldSnapshot);
                restored = model.HasSettlement == oldHas && ReferenceEquals(model.Settlement, oldSnapshot);
                Debug.Log("CLIVERIFY dungeonpolarsettlement restored=" + restored);
            }

            return Task.FromResult(pass && restored ? 0 : 3);
        }

        private static bool Feed(MethodInfo handler, byte[] bytes)
        {
            if (handler == null) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(DungeonController.Instance, new object[] { reader });
            return reader.Remaining == 0;
        }
    }
}
