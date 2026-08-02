using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Revelation;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class RevelationCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY revelation EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            RevelationController controller = RevelationController.Instance;
            RevelationModel model = RevelationModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            ushort oldMax = model.MaxFigureId;
            ushort oldCurrent = model.CurrentFigureId;
            ulong oldPower = model.Power;
            var oldGatherings = new List<RevelationModel.Gathering>(model.Gatherings);
            var oldSuits = new List<RevelationModel.Suit>(model.Suits);
            var oldSkills = new List<RevelationModel.Skill>(model.Skills);
            FieldInfo interceptor = typeof(RevelationController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 28600; id <= 28609; id++)
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
                    for (int id = 28600; id <= 28609; id++)
                    {
                        handlers.Remove(id);
                    }
                }

                controller.Init();
                model.Reset();
                MethodInfo on28606 = typeof(RevelationController).GetMethod("On28606", F);
                MethodInfo on28609 = typeof(RevelationController).GetMethod("On28609", F);
                pass = interceptor != null && on28606 != null && on28609 != null && handlers != null
                    && handlers.Contains(28606) && handlers.Contains(28609);
                for (int id = 28600; id <= 28609; id++)
                {
                    if (id != 28606 && id != 28609)
                    {
                        pass &= !handlers.Contains(id);
                    }
                }

                bool registrationPass = pass;
                Check("seams/register", registrationPass, ref pass);
                var frames = new List<byte[]>();
                if (pass)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame =>
                    {
                        frames.Add(frame);
                        return true;
                    }));

                    controller.RequestStartup();
                    Check("startup exact empty frame", Frame(frames, Proto.REVELATION_INFO), ref pass);
                    frames.Clear();
                    controller.RequestPower();
                    Check("power exact empty frame", Frame(frames, Proto.REVELATION_POWER), ref pass);
                    frames.Clear();

                    byte[] pre = new CliVerify.Pkt().L(5000000001L).Bytes();
                    var preReader = new NetReader(pre, 0, pre.Length);
                    on28609.Invoke(controller, new object[] { preReader });
                    Check("preload power ignored", preReader.Remaining == 0 && !model.HasData && model.Power == 0 && frames.Count == 0, ref pass);

                    byte[] first = new CliVerify.Pkt()
                        .H(65535).H(65534).L(5000000000L).H(2)
                        .C(0).H(65535).I(4000000000L).C(255)
                        .C(255).H(1).I(2).C(0)
                        .H(2).I(4000000000L).I(3).I(4).I(5)
                        .H(2).I(6).H(65535).I(7).H(1)
                        .Bytes();
                    var firstReader = new NetReader(first, 0, first.Length);
                    on28606.Invoke(controller, new object[] { firstReader });
                    Check("fields/order/read-to-end/no-outbound", firstReader.Remaining == 0
                        && model.HasData && model.MaxFigureId == 65535 && model.CurrentFigureId == 65534 && model.Power == 5000000000UL
                        && model.Gatherings.Count == 2
                        && model.Gatherings[0].Pos == 0 && model.Gatherings[0].Level == 65535
                        && model.Gatherings[0].Experience == 4000000000U && model.Gatherings[0].Flag == 255
                        && model.Gatherings[1].Pos == 255 && model.Gatherings[1].Level == 1
                        && model.Gatherings[1].Experience == 2 && model.Gatherings[1].Flag == 0
                        && model.Suits.Count == 2
                        && model.Suits[0].Star == 4000000000U && model.Suits[0].Number == 3
                        && model.Suits[1].Star == 4 && model.Suits[1].Number == 5
                        && model.Skills.Count == 2
                        && model.Skills[0].SkillId == 6 && model.Skills[0].Level == 65535
                        && model.Skills[1].SkillId == 7 && model.Skills[1].Level == 1
                        && frames.Count == 0, ref pass);

                    byte[] loadedPower = new CliVerify.Pkt().L(5000000001L).Bytes();
                    var loadedPowerReader = new NetReader(loadedPower, 0, loadedPower.Length);
                    on28609.Invoke(controller, new object[] { loadedPowerReader });
                    Check("power loaded replace", loadedPowerReader.Remaining == 0 && model.Power == 5000000001UL
                        && model.MaxFigureId == 65535 && model.CurrentFigureId == 65534
                        && model.Gatherings.Count == 2 && model.Suits.Count == 2 && model.Skills.Count == 2
                        && frames.Count == 0, ref pass);

                    byte[] loadedMax = new CliVerify.Pkt().L(unchecked((long)ulong.MaxValue)).Bytes();
                    var loadedMaxReader = new NetReader(loadedMax, 0, loadedMax.Length);
                    on28609.Invoke(controller, new object[] { loadedMaxReader });
                    Check("power u64 max", loadedMaxReader.Remaining == 0 && model.Power == ulong.MaxValue
                        && model.MaxFigureId == 65535 && model.CurrentFigureId == 65534
                        && model.Gatherings.Count == 2 && model.Suits.Count == 2 && model.Skills.Count == 2
                        && frames.Count == 0, ref pass);

                    byte[] empty = new CliVerify.Pkt().H(2).H(1).L(7).H(0).H(0).H(0).Bytes();
                    var emptyReader = new NetReader(empty, 0, empty.Length);
                    on28606.Invoke(controller, new object[] { emptyReader });
                    Check("whole replace empty lists", emptyReader.Remaining == 0
                        && model.MaxFigureId == 2 && model.CurrentFigureId == 1 && model.Power == 7
                        && model.Gatherings.Count == 0 && model.Suits.Count == 0 && model.Skills.Count == 0
                        && frames.Count == 0, ref pass);

                    var finalReader = new NetReader(first, 0, first.Length);
                    on28606.Invoke(controller, new object[] { finalReader });
                    Check("nonempty snapshot before dispose", finalReader.Remaining == 0 && model.HasData
                        && model.MaxFigureId == 65535 && model.CurrentFigureId == 65534 && model.Power == 5000000000UL
                        && model.Gatherings.Count == 2 && model.Suits.Count == 2 && model.Skills.Count == 2
                        && frames.Count == 0, ref pass);

                    controller.Dispose();
                    Check("dispose reset/unregister", !controller.IsInitialized
                        && !model.HasData && model.MaxFigureId == 0 && model.CurrentFigureId == 0 && model.Power == 0
                        && model.Gatherings.Count == 0 && model.Suits.Count == 0 && model.Skills.Count == 0
                        && !handlers.Contains(28600) && !handlers.Contains(28606) && !handlers.Contains(28609), ref pass);
                }

                Debug.Log("CLIVERIFY revelation VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                model.Reset();
                model.Replace(oldMax, oldCurrent, oldPower, oldGatherings, oldSuits, oldSkills);
                RestoreModelProperty(model, "HasData", oldHasData);
                if (wasInitialized)
                {
                    controller.Init();
                }

                for (int id = 28600; id <= 28609; id++)
                {
                    RestoreHandler(handlers, savedHandlers[id], id);
                }

                if (interceptor != null)
                {
                    interceptor.SetValue(null, oldInterceptor);
                }

                restored = controller.IsInitialized == wasInitialized
                    && model.HasData == oldHasData && model.MaxFigureId == oldMax
                    && model.CurrentFigureId == oldCurrent && model.Power == oldPower
                    && SameReferences(model.Gatherings, oldGatherings)
                    && SameReferences(model.Suits, oldSuits)
                    && SameReferences(model.Skills, oldSkills)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                for (int id = 28600; id <= 28609; id++)
                {
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                }

                Debug.Log("CLIVERIFY revelation restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static void Check(string title, bool value, ref bool pass)
        {
            Debug.Log("CLIVERIFY revelation " + title + " ok=" + value);
            if (!value)
            {
                pass = false;
            }
        }

        private static bool Frame(IReadOnlyList<byte[]> frames, int proto)
        {
            return frames.Count == 1 && frames[0] != null && frames[0].Length == 6
                && frames[0][0] == 0 && frames[0][1] == 6 && frames[0][2] == 3 && frames[0][3] == 232
                && frames[0][4] == (byte)(proto >> 8) && frames[0][5] == (byte)(proto & 0xFF);
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> savedHandlers, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            savedHandlers[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState savedHandler, int id)
        {
            if (handlers == null)
            {
                return;
            }

            if (savedHandler.Exists)
            {
                handlers[id] = savedHandler.Value;
            }
            else
            {
                handlers.Remove(id);
            }
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState savedHandler, int id)
        {
            return handlers != null && handlers.Contains(id) == savedHandler.Exists
                && (!savedHandler.Exists || ReferenceEquals(handlers[id], savedHandler.Value));
        }

        private static void RestoreModelProperty(RevelationModel model, string propertyName, object value)
        {
            typeof(RevelationModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static bool SameReferences<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) where T : class
        {
            if (actual.Count != expected.Count)
            {
                return false;
            }

            for (int i = 0; i < actual.Count; i++)
            {
                if (!ReferenceEquals(actual[i], expected[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
