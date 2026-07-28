using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>61058 跳关奖励专项：纯 S2C、有序整表替换、位宽/重复/空表、读尾与 ambient 恢复。</summary>
    public static class DungeonDragonJumpRewardCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags ALL = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;

        private sealed class WireReward
        {
            public byte Type;
            public uint TypeId;
            public uint Num;
        }

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-dragon-jump-reward EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasReward = model.HasDragonJumpReward;
            DungeonModel.DragonJumpRewardSnapshot oldReward = model.LastDragonJumpReward;
            bool oldHasSkill = model.HasDragonSkillInfo;
            List<DungeonModel.DragonSkillInfoEntry> oldSkills = model.DragonSkillInfo;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_DRAGON_JUMP_REWARD);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_DRAGON_JUMP_REWARD] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                RestoreModelProperty(model, "HasDragonJumpReward", false);
                RestoreModelProperty(model, "LastDragonJumpReward", null);

                MethodInfo on61058 = typeof(DungeonController).GetMethod("On61058", IF);
                MethodInfo on61055 = typeof(DungeonController).GetMethod("On61055", IF);
                MethodInfo request = typeof(DungeonController).GetMethod("RequestDragonJumpReward", ALL);
                FieldInfo intercept = typeof(DungeonController).GetField("s_dragonJumpRewardOutboundIntercept", ALL);
                pass = Proto.DUNGEON_DRAGON_JUMP_REWARD == 61058 && on61058 != null && on61055 != null
                    && request == null && intercept == null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "s2c-only seams/registration/no-request", pass);
                if (on61058 == null || on61055 == null)
                    throw new MissingMethodException("On61058/On61055 seam missing");

                Check(ref pass, "no response keeps clear",
                    !model.HasDragonJumpReward && model.LastDragonJumpReward == null);

                var seedList = new List<DungeonModel.DragonJumpRewardEntry>
                {
                    new DungeonModel.DragonJumpRewardEntry { Type = 7, TypeId = 8, Num = 9 },
                };
                model.ApplyDragonJumpReward(10, seedList);
                DungeonModel.DragonJumpRewardSnapshot seed = model.LastDragonJumpReward;
                Check(ref pass, "no response keeps existing snapshot",
                    model.HasDragonJumpReward && ReferenceEquals(model.LastDragonJumpReward, seed)
                    && ReferenceEquals(model.LastDragonJumpReward.RewardList, seedList));

                Check(ref pass, "other packet unrelated/read-to-end",
                    FeedEmptySkillInfo(on61055, controller)
                    && ReferenceEquals(model.LastDragonJumpReward, seed)
                    && ReferenceEquals(model.LastDragonJumpReward.RewardList, seedList));

                var maxDuplicate = new[]
                {
                    new WireReward { Type = byte.MaxValue, TypeId = uint.MaxValue, Num = uint.MaxValue },
                    new WireReward { Type = 0, TypeId = 0, Num = 0 },
                    new WireReward { Type = byte.MaxValue, TypeId = uint.MaxValue, Num = 7 },
                };
                Check(ref pass, "u32/u8 max-zero duplicate original order/read-to-end",
                    Feed(on61058, controller, uint.MaxValue, maxDuplicate)
                    && Snapshot(model, uint.MaxValue, maxDuplicate));
                DungeonModel.DragonJumpRewardSnapshot multi = model.LastDragonJumpReward;

                var single = new[]
                {
                    new WireReward { Type = 3, TypeId = 4, Num = 5 },
                };
                Check(ref pass, "multi-to-single whole replace/read-to-end",
                    Feed(on61058, controller, 6, single) && Snapshot(model, 6, single)
                    && !ReferenceEquals(model.LastDragonJumpReward, multi));
                DungeonModel.DragonJumpRewardSnapshot one = model.LastDragonJumpReward;

                Check(ref pass, "single-to-empty whole replace/read-to-end",
                    Feed(on61058, controller, 0, Array.Empty<WireReward>())
                    && Snapshot(model, 0, Array.Empty<WireReward>())
                    && !ReferenceEquals(model.LastDragonJumpReward, one));

                Check(ref pass, "handler/init ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-dragon-jump-reward VERDICT pass=" + pass);
            }
            finally
            {
                RestoreModelProperty(model, "HasDragonJumpReward", oldHasReward);
                RestoreModelProperty(model, "LastDragonJumpReward", oldReward);
                RestoreModelProperty(model, "HasDragonSkillInfo", oldHasSkill);
                RestoreModelProperty(model, "DragonSkillInfo", oldSkills);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasDragonJumpReward == oldHasReward
                    && ReferenceEquals(model.LastDragonJumpReward, oldReward)
                    && model.HasDragonSkillInfo == oldHasSkill
                    && ReferenceEquals(model.DragonSkillInfo, oldSkills)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler);
                Debug.Log("CLIVERIFY dungeon-dragon-jump-reward restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, uint wave,
            params WireReward[] rewards)
        {
            var p = new CliVerify.Pkt().I(wave).H(rewards.Length);
            foreach (WireReward reward in rewards) p.C(reward.Type).I(reward.TypeId).I(reward.Num);
            byte[] bytes = p.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool FeedEmptySkillInfo(MethodInfo handler, DungeonController controller)
        {
            byte[] bytes = new CliVerify.Pkt().H(0).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Snapshot(DungeonModel model, uint wave, IReadOnlyList<WireReward> expected)
        {
            if (!model.HasDragonJumpReward || model.LastDragonJumpReward == null
                || model.LastDragonJumpReward.Wave != wave || model.LastDragonJumpReward.RewardList == null
                || model.LastDragonJumpReward.RewardList.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++)
            {
                DungeonModel.DragonJumpRewardEntry actual = model.LastDragonJumpReward.RewardList[i];
                WireReward wanted = expected[i];
                if (actual == null || actual.Type != wanted.Type || actual.TypeId != wanted.TypeId
                    || actual.Num != wanted.Num) return false;
            }
            return true;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_DRAGON_JUMP_REWARD) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_DRAGON_JUMP_REWARD], value));
        }

        private static void RestoreModelProperty(DungeonModel model, string propertyName, object value)
        {
            typeof(DungeonModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-dragon-jump-reward " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
