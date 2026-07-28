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
    /// <summary>61053 快速出怪信息专项：严格空请求、整体替换、读尾与 ambient 恢复。</summary>
    public static class DungeonDragonQuickInfoCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-dragon-quick-info EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasInfo = model.HasDragonQuickInfo;
            ushort oldQuickCount = model.QuickCount;
            ushort oldTotalQuickCount = model.TotalQuickCount;
            uint oldNextQuickTime = model.NextQuickTime;
            FieldInfo interceptField = typeof(DungeonController).GetField("s_dragonQuickInfoOutboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_DRAGON_QUICK_INFO);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_DRAGON_QUICK_INFO] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                RestoreModelProperty(model, "HasDragonQuickInfo", false);
                RestoreModelProperty(model, "QuickCount", (ushort)0);
                RestoreModelProperty(model, "TotalQuickCount", (ushort)0);
                RestoreModelProperty(model, "NextQuickTime", (uint)0);

                MethodInfo on61053 = typeof(DungeonController).GetMethod("On61053", IF);
                pass = Proto.DUNGEON_DRAGON_QUICK_INFO == 61053 && on61053 != null
                    && interceptField != null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "seams/registration", pass);

                model.ApplyDragonQuickInfo(7, 8, 9);
                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                controller.RequestDragonQuickInfo();
                Check(ref pass, "exact 6B empty request", frames.Count == 1 && EmptyFrame(frames[0]));
                Check(ref pass, "request no response keeps snapshot", Snapshot(model, 7, 8, 9));

                Check(ref pass, "u16/u32 max whole replace/read-to-end",
                    Feed(on61053, controller, ushort.MaxValue, ushort.MaxValue, uint.MaxValue)
                    && Snapshot(model, ushort.MaxValue, ushort.MaxValue, uint.MaxValue));
                Check(ref pass, "max-to-small whole replace/read-to-end",
                    Feed(on61053, controller, 1, 2, 3) && Snapshot(model, 1, 2, 3));
                Check(ref pass, "small-to-zero whole replace/read-to-end",
                    Feed(on61053, controller, 0, 0, 0) && Snapshot(model, 0, 0, 0));

                Check(ref pass, "ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-dragon-quick-info VERDICT pass=" + pass);
            }
            finally
            {
                RestoreModelProperty(model, "HasDragonQuickInfo", oldHasInfo);
                RestoreModelProperty(model, "QuickCount", oldQuickCount);
                RestoreModelProperty(model, "TotalQuickCount", oldTotalQuickCount);
                RestoreModelProperty(model, "NextQuickTime", oldNextQuickTime);
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasDragonQuickInfo == oldHasInfo
                    && model.QuickCount == oldQuickCount
                    && model.TotalQuickCount == oldTotalQuickCount
                    && model.NextQuickTime == oldNextQuickTime
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler)
                    && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-dragon-quick-info restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, ushort quickCount,
            ushort totalQuickCount, uint nextQuickTime)
        {
            byte[] bytes = new CliVerify.Pkt().H(quickCount).H(totalQuickCount).I(nextQuickTime).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool EmptyFrame(byte[] frame)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
                && frame[4] == 238 && frame[5] == 125;
        }

        private static bool Snapshot(DungeonModel model, ushort quickCount, ushort totalQuickCount,
            uint nextQuickTime)
        {
            return model.HasDragonQuickInfo && model.QuickCount == quickCount
                && model.TotalQuickCount == totalQuickCount && model.NextQuickTime == nextQuickTime;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null && handlers.Contains(Proto.DUNGEON_DRAGON_QUICK_INFO) == existed
                && (!existed || ReferenceEquals(handlers[Proto.DUNGEON_DRAGON_QUICK_INFO], value));
        }

        private static void RestoreModelProperty(DungeonModel model, string propertyName, object value)
        {
            typeof(DungeonModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-dragon-quick-info " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
