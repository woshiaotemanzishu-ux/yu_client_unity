using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.HolySeal;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class HolySealCase
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
                Debug.LogError("CLIVERIFY holyseal EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            HolySealController controller = HolySealController.Instance;
            HolySealModel model = HolySealModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            string oldErrorArgs = model.LastErrorArgs;
            bool oldHasRating = model.HasRating;
            uint oldTotalRating = model.TotalRating;
            FieldInfo intercept = typeof(HolySealController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 65400; id <= 65409; id++)
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
                    for (int id = 65400; id <= 65409; id++)
                    {
                        handlers.Remove(id);
                    }
                }

                controller.Init();
                model.Reset();
                MethodInfo on65400 = typeof(HolySealController).GetMethod("On65400", F);
                MethodInfo on65407 = typeof(HolySealController).GetMethod("On65407", F);
                pass = handlers != null && on65400 != null
                    && on65407 != null && handlers.Contains(65400) && handlers.Contains(65407)
                    && OnlyRequestRating() && intercept != null;
                for (int id = 65401; id <= 65409; id++)
                {
                    pass &= id == 65407 || !handlers.Contains(id);
                }

                object firstHandler = handlers != null && handlers.Contains(65400) ? handlers[65400] : null;
                object firstRatingHandler = handlers != null && handlers.Contains(65407) ? handlers[65407] : null;
                controller.Init();
                pass &= controller.IsInitialized && firstHandler != null
                    && firstRatingHandler != null && ReferenceEquals(handlers[65400], firstHandler)
                    && ReferenceEquals(handlers[65407], firstRatingHandler);
                object ratingHandler = handlers != null && handlers.Contains(65407) ? handlers[65407] : null;
                pass &= ratingHandler != null;

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestRating();
                pass &= StrictEmptyFrame(frames, Proto.HOLY_SEAL_RATING);

                if (pass)
                {
                    pass &= Feed(on65400, controller, 0, string.Empty)
                        && model.HasError && model.LastErrorCode == 0 && model.LastErrorArgs == string.Empty;
                    pass &= Feed(on65400, controller, 1012, "中文")
                        && model.HasError && model.LastErrorCode == 1012 && model.LastErrorArgs == "中文";
                    pass &= Feed(on65400, controller, uint.MaxValue, "最大值")
                        && model.HasError && model.LastErrorCode == uint.MaxValue && model.LastErrorArgs == "最大值";
                    pass &= Feed(on65400, controller, 7, "后包覆盖")
                        && model.HasError && model.LastErrorCode == 7 && model.LastErrorArgs == "后包覆盖" && !model.HasRating;
                    pass &= FeedRating(on65407, controller, 0) && model.HasRating && model.TotalRating == 0;
                    pass &= FeedRating(on65407, controller, uint.MaxValue) && model.HasRating && model.TotalRating == uint.MaxValue;
                    pass &= FeedRating(on65407, controller, 7) && model.HasRating && model.TotalRating == 7;
                    pass &= model.HasError && model.LastErrorCode == 7 && model.LastErrorArgs == "后包覆盖";
                    pass &= Feed(on65400, controller, 8, "评分后错误")
                        && model.HasError && model.LastErrorCode == 8 && model.LastErrorArgs == "评分后错误"
                        && model.HasRating && model.TotalRating == 7;

                    controller.Dispose();
                    pass &= !controller.IsInitialized && !model.HasError && model.LastErrorCode == 0
                        && model.LastErrorArgs == null && !model.HasRating && model.TotalRating == 0
                        && !handlers.Contains(65400) && !handlers.Contains(65407);
                }

                Debug.Log("CLIVERIFY holyseal VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                RestoreModelProperty(model, "HasError", oldHasError);
                RestoreModelProperty(model, "LastErrorCode", oldErrorCode);
                RestoreModelProperty(model, "LastErrorArgs", oldErrorArgs);
                RestoreModelProperty(model, "HasRating", oldHasRating);
                RestoreModelProperty(model, "TotalRating", oldTotalRating);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (wasInitialized)
                {
                    controller.Init();
                }

                for (int id = 65400; id <= 65409; id++)
                {
                    RestoreHandler(handlers, savedHandlers[id], id);
                }

                restored = ReferenceEquals(HolySealController.Instance, controller)
                    && ReferenceEquals(HolySealModel.Instance, model)
                    && controller.IsInitialized == wasInitialized
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode
                    && model.LastErrorArgs == oldErrorArgs && model.HasRating == oldHasRating
                    && model.TotalRating == oldTotalRating;
                for (int id = 65400; id <= 65409; id++)
                {
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                }

                Debug.Log("CLIVERIFY holyseal restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, HolySealController controller, uint errorCode, string errorArgs)
        {
            byte[] bytes = new CliVerify.Pkt().I(errorCode).S(errorArgs).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool FeedRating(MethodInfo handler, HolySealController controller, uint rating)
        {
            byte[] bytes = new CliVerify.Pkt().I(rating).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool OnlyRequestRating()
        {
            foreach (MethodInfo method in typeof(HolySealController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if ((method.Name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) >= 0
                    || method.Name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) >= 0)
                    && method.Name != nameof(HolySealController.RequestRating))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool StrictEmptyFrame(IReadOnlyList<byte[]> frames, int id)
        {
            if (frames.Count != 1 || frames[0] == null || frames[0].Length != 6) return false;
            byte[] frame = frames[0];
            return frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(id >> 8) && frame[5] == (byte)id;
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

        private static void RestoreModelProperty(HolySealModel model, string propertyName, object value)
        {
            typeof(HolySealModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }
    }
}
