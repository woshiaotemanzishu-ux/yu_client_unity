using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Module.Core.AttributePotion;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>pt_217 配置、wire、模型合并、使用裁剪、日切和生命周期专项。</summary>
    public static class AttributePotionCase
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

        public static async Task<int> Run()
        {
            bool oldFallback = ResManager.EditorPreferFallback;
            ResManager.EditorPreferFallback = true;
            try
            {
                await AttributePotionConfigs.EnsureLoaded();
                return RunSync();
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY attributepotion EXCEPTION " + e);
                return 3;
            }
            finally
            {
                ResManager.EditorPreferFallback = oldFallback;
            }
        }

        private static int RunSync()
        {
            AttributePotionController controller = AttributePotionController.Instance;
            AttributePotionModel model = AttributePotionModel.Instance;
            RoleModel role = RoleModel.Instance;
            BagModel bag = BagModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            List<AttributePotionModel.Count> savedCounts = SnapshotCounts(model);
            var savedBag = new List<BagGoods>(bag.BagGoodsList);
            int oldRoleLevel = role.Level;
            bool oldHasBaseInfo = role.HasBaseInfo;

            FieldInfo hasBaseInfo = typeof(RoleModel).GetField("<HasBaseInfo>k__BackingField", PrivateInstance);
            FieldInfo intercept = controller.GetType().GetField("s_outboundIntercept", PrivateStatic);
            FieldInfo guideIntercept = typeof(RoleController).GetField(
                "s_potionGuideOutboundIntercept", PrivateStatic);
            MethodInfo on21700 = controller.GetType().GetMethod("On21700", PrivateStatic);
            MethodInfo on21701 = controller.GetType().GetMethod("On21701", PrivateInstance);
            MethodInfo on21703 = controller.GetType().GetMethod("On21703", PrivateInstance);
            object oldIntercept = intercept?.GetValue(null);
            object oldGuideIntercept = guideIntercept?.GetValue(null);
            var frames = new List<byte[]>();
            bool pass = true;
            void Check(string name, bool ok)
            {
                Debug.Log("CLIVERIFY attributepotion " + name + " ok=" + ok);
                if (!ok) pass = false;
            }

            try
            {
                controller.Init();
                model.Clear();
                bag.BagGoodsList.Clear();

                bool configOk = AttributePotionConfigs.PotionCount == 16
                    && AttributePotionConfigs.LimitCount == 224
                    && AttributePotionConfigs.TryGetPotion(56010001, out AttributePotionConfigs.Potion firstPotion)
                    && firstPotion.Level == 1
                    && firstPotion.Attrs.Count == 1
                    && firstPotion.Attrs[0].Id == 1 && firstPotion.Attrs[0].Value == 10
                    && AttributePotionConfigs.TryGetPotion(56040004, out AttributePotionConfigs.Potion lastPotion)
                    && lastPotion.Level == 4
                    && lastPotion.Attrs.Count == 2
                    && lastPotion.Attrs[0].Id == 3 && lastPotion.Attrs[0].Value == 150
                    && lastPotion.Attrs[1].Id == 4 && lastPotion.Attrs[1].Value == 150
                    && AttributePotionConfigs.GetPotions(1).Count == 4
                    && AttributePotionConfigs.GetPotions(2).Count == 4
                    && AttributePotionConfigs.GetPotions(3).Count == 4
                    && AttributePotionConfigs.GetPotions(4).Count == 4
                    && AttributePotionConfigs.HasPotionLevel(1)
                    && AttributePotionConfigs.HasPotionLevel(4)
                    && !AttributePotionConfigs.HasPotionLevel(5)
                    && AttributePotionConfigs.TryGetLimit(56010001, 100, out AttributePotionConfigs.Limit firstLimit)
                    && firstLimit.DayTimes == 100 && firstLimit.AllTimes == 100
                    && AttributePotionConfigs.TryGetLimit(56040004, 800, out AttributePotionConfigs.Limit lastLimit)
                    && lastLimit.DayTimes == 100 && lastLimit.AllTimes == 700
                    && AttributePotionConfigs.Guide != null
                    && AttributePotionConfigs.Guide.Direction == 6
                    && Math.Abs(AttributePotionConfigs.Guide.EffectScaleX - 0.65f) < 0.001f
                    && Math.Abs(AttributePotionConfigs.Guide.EffectScaleY - 0.8f) < 0.001f
                    && AttributePotionConfigs.Guide.Text.Contains("药水");
                Check("config 16/224/attrs/guide/key rows", configOk);

                FieldInfo handlersField = typeof(NetManager).GetField("_handlers", PrivateStatic);
                var handlers = handlersField?.GetValue(null) as IDictionary;
                bool registrationOk = handlers != null
                    && handlers.Contains(Proto.ATTRIBUTE_POTION_ERROR)
                    && handlers.Contains(Proto.ATTRIBUTE_POTION_LEVEL_COUNT)
                    && handlers.Contains(Proto.ATTRIBUTE_POTION_ALL_COUNT)
                    && on21700 != null && on21701 != null && on21703 != null && intercept != null;
                Check("registration/hooks", registrationOk);

                if (intercept != null)
                {
                    intercept.SetValue(null, new Func<byte[], bool>(frame =>
                    {
                        frames.Add(frame);
                        return true;
                    }));
                }
                if (guideIntercept != null)
                {
                    guideIntercept.SetValue(null, new Func<byte[], bool>(frame =>
                    {
                        frames.Add(frame);
                        return true;
                    }));
                }

                controller.RequestStartup();
                Check("startup empty 21703", OneFrame(frames, Proto.ATTRIBUTE_POTION_ALL_COUNT, Array.Empty<byte>()));
                frames.Clear();
                controller.RequestLevel(1);
                Check("21701 potion tier u8", OneFrame(frames, Proto.ATTRIBUTE_POTION_LEVEL_COUNT, new byte[] { 1 }));
                frames.Clear();
                controller.RequestLevel(0);
                controller.RequestLevel(5);
                Check("invalid potion tier no outbound", frames.Count == 0);

                byte[] allPacket = new CliVerify.Pkt().H(3)
                    .I(56010001).C(1).I(7).L(5000000000L)
                    .I(56010002).C(1).I(8).L(9)
                    .I(56020001).C(2).I(10).L(11)
                    .Bytes();
                var allReader = new NetReader(allPacket, 0, allPacket.Length);
                on21703?.Invoke(controller, new object[] { allReader });
                on21703?.Invoke(controller, new object[] { new NetReader(allPacket, 0, allPacket.Length) });
                bool mergeOk = allReader.Remaining == 0 && model.LevelCount == 2
                    && model.TryGet(1, 56010001, out AttributePotionModel.Count count)
                    && count.CurrentDayCount == 7 && count.CurrentCount == 5000000000UL
                    && model.TryGet(1, 56010002, out _)
                    && model.TryGet(2, 56020001, out _);
                Check("21703 merge/u64/idempotent/read-end", mergeOk);

                byte[] levelPacket = new CliVerify.Pkt().H(2)
                    .I(56010001).C(1).I(10).L(11)
                    .I(56010003).C(1).I(12).L(13)
                    .Bytes();
                var levelReader = new NetReader(levelPacket, 0, levelPacket.Length);
                on21701?.Invoke(controller, new object[] { levelReader });
                bool replaceOk = levelReader.Remaining == 0
                    && model.TryGet(1, 56010001, out count) && count.CurrentCount == 11
                    && model.TryGet(1, 56010003, out _)
                    && !model.TryGet(1, 56010002, out _)
                    && model.TryGet(2, 56020001, out _);
                Check("21701 replace whole tier/read-end", replaceOk);

                role.Level = 100;
                hasBaseInfo?.SetValue(role, true);

                bag.BagGoodsList.Clear();
                var allPotionCounts = new List<AttributePotionModel.Count>(16);
                int bagInstance = 91000000;
                for (byte tier = 1; tier <= 4; tier++)
                {
                    IReadOnlyList<AttributePotionConfigs.Potion> rows = AttributePotionConfigs.GetPotions(tier);
                    for (int i = 0; i < rows.Count; i++)
                    {
                        bag.BagGoodsList.Add(new BagGoods
                        {
                            GoodsId = bagInstance++,
                            TypeId = rows[i].GoodsId,
                            GoodsNum = 2,
                        });
                        allPotionCounts.Add(new AttributePotionModel.Count
                        {
                            GoodsId = rows[i].GoodsId,
                            Level = tier,
                            CurrentDayCount = 0,
                            CurrentCount = 0,
                        });
                    }
                }
                model.Clear();
                model.MergeAll(allPotionCounts);
                bool everyUseControl = true;
                int verifiedUseControls = 0;
                for (byte tier = 1; tier <= 4; tier++)
                {
                    IReadOnlyList<AttributePotionConfigs.Potion> rows = AttributePotionConfigs.GetPotions(tier);
                    for (int i = 0; i < rows.Count; i++)
                    {
                        frames.Clear();
                        int goodsId = rows[i].GoodsId;
                        bool sent = controller.TryRequestUse(goodsId);
                        byte[] expected = new CliVerify.Pkt().I(goodsId).I(2).C(tier).Bytes();
                        everyUseControl &= sent
                            && OneFrame(frames, Proto.ATTRIBUTE_POTION_USE, expected)
                            && model.TryGet(tier, goodsId, out AttributePotionModel.Count untouched)
                            && untouched.CurrentDayCount == 0 && untouched.CurrentCount == 0;
                        verifiedUseControls++;
                    }
                }
                Check("21702 all 4x4 controls/exact frame/no optimistic update",
                    everyUseControl && verifiedUseControls == 16);

                frames.Clear();
                RoleController.Instance.CompletePotionFirstUseGuide();
                Check("13089 first-use guide exact hhh",
                    OneFrame(frames, Proto.ROLE_LIFELONG_INCREMENT,
                        new CliVerify.Pkt().H(300).H(1).H(1).Bytes()));

                bag.BagGoodsList.Clear();
                bag.BagGoodsList.Add(new BagGoods { GoodsId = 90000001, TypeId = 56010001, GoodsNum = 50 });
                model.ReplaceLevel(1, new List<AttributePotionModel.Count>
                {
                    new AttributePotionModel.Count
                    {
                        GoodsId = 56010001,
                        Level = 1,
                        CurrentDayCount = 99,
                        CurrentCount = 98,
                    },
                });
                frames.Clear();
                bool useSent = controller.TryRequestUse(56010001, 99);
                byte[] usePayload = new CliVerify.Pkt().I(56010001).I(1).C(1).Bytes();
                bool useOk = useSent && OneFrame(frames, Proto.ATTRIBUTE_POTION_USE, usePayload)
                    && model.TryGet(1, 56010001, out count)
                    && count.CurrentDayCount == 99 && count.CurrentCount == 98;
                Check("21702 derive tier/cap/no optimistic update", useOk);

                frames.Clear();
                model.Clear();
                bool missingSnapshot = !controller.TryRequestUse(56010001, 1) && frames.Count == 0;
                model.ReplaceLevel(1, new List<AttributePotionModel.Count>
                {
                    new AttributePotionModel.Count { GoodsId = 56010001, Level = 1, CurrentDayCount = 100, CurrentCount = 98 },
                });
                bool dayExhausted = !controller.TryRequestUse(56010001, 1) && frames.Count == 0;
                model.ReplaceLevel(1, new List<AttributePotionModel.Count>
                {
                    new AttributePotionModel.Count { GoodsId = 56010001, Level = 1, CurrentDayCount = 1, CurrentCount = 100 },
                });
                bool allExhausted = !controller.TryRequestUse(56010001, 1) && frames.Count == 0;
                bag.BagGoodsList.Clear();
                model.ReplaceLevel(1, new List<AttributePotionModel.Count>
                {
                    new AttributePotionModel.Count { GoodsId = 56010001, Level = 1, CurrentDayCount = 1, CurrentCount = 1 },
                });
                bool bagEmpty = !controller.TryRequestUse(56010001, 1) && frames.Count == 0;
                Check("TryUse guards zero outbound", missingSnapshot && dayExhausted && allExhausted && bagEmpty);

                model.ReplaceLevel(1, new List<AttributePotionModel.Count>
                {
                    new AttributePotionModel.Count { GoodsId = 56010001, Level = 1, CurrentDayCount = 2, CurrentCount = 3 },
                });
                var errorReader = new NetReader(new CliVerify.Pkt().I(2170001).Bytes(), 0, 4);
                on21700?.Invoke(null, new object[] { errorReader });
                bool errorOk = errorReader.Remaining == 0
                    && model.TryGet(1, 56010001, out count)
                    && count.CurrentDayCount == 2 && count.CurrentCount == 3;
                Check("21700 read-end/no mutation", errorOk);

                frames.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_SERVER_DAY_CHANGE);
                bool dayChangeOk = model.LevelCount == 0
                    && OneFrame(frames, Proto.ATTRIBUTE_POTION_ALL_COUNT, Array.Empty<byte>());
                Check("day clear plus 21703", dayChangeOk);

                controller.Dispose();
                EventDispatcher.Emit(GlobalEvent.EVT_SERVER_DAY_CHANGE);
                Check("dispose unsubscribe/reset", !controller.IsInitialized && frames.Count == 1 && model.LevelCount == 0);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (guideIntercept != null) guideIntercept.SetValue(null, oldGuideIntercept);
                model.Clear();
                model.MergeAll(savedCounts);
                bag.BagGoodsList.Clear();
                bag.BagGoodsList.AddRange(savedBag);
                role.Level = oldRoleLevel;
                hasBaseInfo?.SetValue(role, oldHasBaseInfo);
                if (wasInitialized) controller.Init();
            }

            Debug.Log("CLIVERIFY attributepotion VERDICT pass=" + pass);
            return pass ? 0 : 3;
        }

        private static List<AttributePotionModel.Count> SnapshotCounts(AttributePotionModel model)
        {
            var result = new List<AttributePotionModel.Count>();
            FieldInfo field = typeof(AttributePotionModel).GetField("_byLevel", PrivateInstance);
            var levels = field?.GetValue(model) as Dictionary<int, Dictionary<int, AttributePotionModel.Count>>;
            if (levels == null) return result;
            foreach (Dictionary<int, AttributePotionModel.Count> rows in levels.Values)
            {
                foreach (AttributePotionModel.Count row in rows.Values)
                {
                    result.Add(new AttributePotionModel.Count
                    {
                        GoodsId = row.GoodsId,
                        Level = row.Level,
                        CurrentDayCount = row.CurrentDayCount,
                        CurrentCount = row.CurrentCount,
                    });
                }
            }
            return result;
        }

        private static bool OneFrame(IReadOnlyList<byte[]> frames, int protocolId, byte[] payload)
        {
            if (frames.Count != 1) return false;
            byte[] frame = frames[0];
            int length = 6 + payload.Length;
            if (frame == null || frame.Length != length
                || frame[0] != (byte)(length >> 8) || frame[1] != (byte)(length & 0xFF)
                || frame[2] != 3 || frame[3] != 232
                || frame[4] != (byte)(protocolId >> 8) || frame[5] != (byte)(protocolId & 0xFF))
            {
                return false;
            }
            for (int i = 0; i < payload.Length; i++) if (frame[i + 6] != payload[i]) return false;
            return true;
        }
    }
}
