using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.GuildActivity;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class GuildGuardEnterCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        private sealed class ModelSnapshot
        {
            private readonly Dictionary<PropertyInfo, object> _properties = new Dictionary<PropertyInfo, object>();
            private readonly List<GuildActivityModel.ObjectReward> _fireReward;
            private readonly List<GuildActivityModel.FoodEntry> _foodList;
            private readonly List<GuildActivityModel.ObjectReward> _rankReward;

            public ModelSnapshot(GuildActivityModel model)
            {
                foreach (PropertyInfo property in typeof(GuildActivityModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanRead && property.GetIndexParameters().Length == 0)
                    {
                        _properties[property] = property.GetValue(model);
                    }
                }

                _fireReward = new List<GuildActivityModel.ObjectReward>(model.LastFireReward);
                _foodList = new List<GuildActivityModel.FoodEntry>(model.FoodList);
                _rankReward = new List<GuildActivityModel.ObjectReward>(model.LastRankReward);
            }

            public void Restore(GuildActivityModel model)
            {
                foreach (KeyValuePair<PropertyInfo, object> property in _properties)
                {
                    property.Key.SetValue(model, property.Value);
                }

                model.LastFireReward.Clear();
                model.LastFireReward.AddRange(_fireReward);
                model.FoodList.Clear();
                model.FoodList.AddRange(_foodList);
                model.LastRankReward.Clear();
                model.LastRankReward.AddRange(_rankReward);
            }

            public bool Matches(GuildActivityModel model)
            {
                foreach (KeyValuePair<PropertyInfo, object> property in _properties)
                {
                    object actual = property.Key.GetValue(model);
                    Type type = property.Key.PropertyType;
                    if (type.IsValueType || type == typeof(string))
                    {
                        if (!Equals(actual, property.Value)) return false;
                    }
                    else if (!ReferenceEquals(actual, property.Value))
                    {
                        return false;
                    }
                }

                return SameReferences(model.LastFireReward, _fireReward)
                    && SameReferences(model.FoodList, _foodList)
                    && SameReferences(model.LastRankReward, _rankReward);
            }

            private static bool SameReferences<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) where T : class
            {
                if (actual.Count != expected.Count) return false;
                for (int i = 0; i < actual.Count; i++)
                {
                    if (!ReferenceEquals(actual[i], expected[i])) return false;
                }
                return true;
            }
        }

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY guildguardenter EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            GuildActivityController controller = GuildActivityController.Instance;
            GuildActivityModel model = GuildActivityModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var modelSnapshot = new ModelSnapshot(model);
            FieldInfo interceptor = typeof(GuildActivityController).GetField("s_guardEnterOutboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 40230; id <= 40232; id++)
            {
                SaveHandler(handlers, savedHandlers, id);
            }

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                if (handlers != null)
                {
                    for (int id = 40230; id <= 40232; id++)
                    {
                        handlers.Remove(id);
                    }
                }

                controller.Init();
                model.Clear();
                MethodInfo on40230 = typeof(GuildActivityController).GetMethod("On40230", F);
                pass = interceptor != null && handlers != null && on40230 != null
                    && handlers.Contains(40230) && !handlers.Contains(40231) && !handlers.Contains(40232);

                object firstHandler = handlers != null && handlers.Contains(40230) ? handlers[40230] : null;
                controller.Init();
                pass &= controller.IsInitialized && firstHandler != null
                    && ReferenceEquals(handlers[40230], firstHandler);

                var frames = new List<byte[]>();
                if (pass)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame =>
                    {
                        frames.Add(frame);
                        return true;
                    }));
                    controller.RequestGuardEnter();
                    pass &= OneEmptyFrame(frames, Proto.GUILD_GUARD_ENTER);
                    frames.Clear();

                    var boss = new GuildActivityModel.BossInfo
                    {
                        Etime = 11,
                        AutoDrumupTime = 12,
                        DunId = 13,
                        GbossMat = 14,
                        RemainTimes = 15,
                        IsAuto = 16,
                        IsDrumToday = 17,
                        MonState = 18,
                    };
                    var act = new GuildActivityModel.ActInfo
                    {
                        Status = 21,
                        ActEndTime = 22,
                        Etime = 23,
                        Stage = 24,
                    };
                    model.SetBoss(boss);
                    model.SetAct(act);
                    model.SetLastError(4020000);

                    pass &= Feed(on40230, controller, 0)
                        && model.HasGuardEnterResult && model.GuardEnterResultCode == 0
                        && RepresentativeSlicesAre(model, boss, act, 4020000) && frames.Count == 0;
                    pass &= Feed(on40230, controller, 1)
                        && model.HasGuardEnterResult && model.GuardEnterResultCode == 1
                        && RepresentativeSlicesAre(model, boss, act, 4020000) && frames.Count == 0;
                    pass &= Feed(on40230, controller, uint.MaxValue)
                        && model.HasGuardEnterResult && model.GuardEnterResultCode == uint.MaxValue
                        && RepresentativeSlicesAre(model, boss, act, 4020000) && frames.Count == 0;
                    pass &= Feed(on40230, controller, 7)
                        && model.HasGuardEnterResult && model.GuardEnterResultCode == 7
                        && RepresentativeSlicesAre(model, boss, act, 4020000) && frames.Count == 0;

                    model.ApplyGbossMatAdd(5, 99);
                    model.SetAct(new GuildActivityModel.ActInfo { Status = 31, ActEndTime = 32, Etime = 33, Stage = 34 });
                    model.SetLastError(4020001);
                    pass &= model.HasGuardEnterResult && model.GuardEnterResultCode == 7 && frames.Count == 0;

                    controller.Dispose();
                    pass &= !controller.IsInitialized && !model.HasGuardEnterResult && model.GuardEnterResultCode == 0
                        && !handlers.Contains(40230) && !handlers.Contains(40231) && !handlers.Contains(40232)
                        && frames.Count == 0;
                }

                Debug.Log("CLIVERIFY guildguardenter VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                modelSnapshot.Restore(model);
                if (wasInitialized)
                {
                    controller.Init();
                }

                for (int id = 40230; id <= 40232; id++)
                {
                    RestoreHandler(handlers, savedHandlers[id], id);
                }

                if (interceptor != null)
                {
                    interceptor.SetValue(null, oldInterceptor);
                }

                restored = ReferenceEquals(GuildActivityController.Instance, controller)
                    && ReferenceEquals(GuildActivityModel.Instance, model)
                    && controller.IsInitialized == wasInitialized
                    && modelSnapshot.Matches(model)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                for (int id = 40230; id <= 40232; id++)
                {
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                }

                Debug.Log("CLIVERIFY guildguardenter restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, GuildActivityController controller, uint code)
        {
            byte[] bytes = new CliVerify.Pkt().I(code).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool RepresentativeSlicesAre(
            GuildActivityModel model,
            GuildActivityModel.BossInfo boss,
            GuildActivityModel.ActInfo act,
            int errorCode)
        {
            return ReferenceEquals(model.Boss, boss) && model.HasBoss
                && boss.Etime == 11 && boss.AutoDrumupTime == 12 && boss.DunId == 13 && boss.GbossMat == 14
                && boss.RemainTimes == 15 && boss.IsAuto == 16 && boss.IsDrumToday == 17 && boss.MonState == 18
                && ReferenceEquals(model.Act, act) && model.HasAct
                && act.Status == 21 && act.ActEndTime == 22 && act.Etime == 23 && act.Stage == 24
                && model.LastErrorCode == errorCode;
        }

        private static bool OneEmptyFrame(IReadOnlyList<byte[]> frames, int protocolId)
        {
            if (frames.Count != 1) return false;
            byte[] frame = frames[0];
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(protocolId >> 8) && frame[5] == (byte)(protocolId & 0xFF);
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> savedHandlers, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            savedHandlers[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState savedHandler, int id)
        {
            if (handlers == null) return;
            if (savedHandler.Exists) handlers[id] = savedHandler.Value;
            else handlers.Remove(id);
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState savedHandler, int id)
        {
            return handlers != null && handlers.Contains(id) == savedHandler.Exists
                && (!savedHandler.Exists || ReferenceEquals(handlers[id], savedHandler.Value));
        }
    }
}
