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
    /// <summary>61045 冷却查询专项：精确请求帧、按 id 缓存、u32 边界、覆盖/隔离与 ambient 恢复。</summary>
    public static class DungeonCooldownCase
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
                Debug.LogError("CLIVERIFY dungeon-cooldown EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            var oldCooldowns = new Dictionary<uint, uint>(model.CooldownEndTimes);
            FieldInfo interceptField = typeof(DungeonController).GetField("s_cooldownOutboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_COOLDOWN);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_COOLDOWN] : null;
            bool pass = false;
            bool restored = false;

            try
            {
                MethodInfo on61045 = typeof(DungeonController).GetMethod("On61045", IF);
                pass = on61045 != null && interceptField != null && Proto.DUNGEON_COOLDOWN == 61045;
                Check(ref pass, "seams", pass);

                model.CooldownEndTimes.Clear();
                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                controller.RequestCooldown(0);
                controller.RequestCooldown(uint.MaxValue);
                Check(ref pass, "exact requests", frames.Count == 2
                    && Frame(frames[0], 0) && Frame(frames[1], uint.MaxValue));

                Check(ref pass, "no response keeps empty", model.CooldownEndTimes.Count == 0);
                Check(ref pass, "zero", Feed(on61045, controller, 0, 0)
                    && model.TryGetCooldown(0, out uint zero) && zero == 0);
                Check(ref pass, "u32 max", Feed(on61045, controller, uint.MaxValue, uint.MaxValue)
                    && model.TryGetCooldown(uint.MaxValue, out uint max) && max == uint.MaxValue);
                Check(ref pass, "different id coexist", model.CooldownEndTimes.Count == 2
                    && model.TryGetCooldown(0, out zero) && zero == 0
                    && model.TryGetCooldown(uint.MaxValue, out max) && max == uint.MaxValue);
                Check(ref pass, "same id overwrite", Feed(on61045, controller, uint.MaxValue, 7)
                    && model.CooldownEndTimes.Count == 2
                    && model.TryGetCooldown(uint.MaxValue, out max) && max == 7);

                Check(ref pass, "ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && (handlers != null && handlers.Contains(Proto.DUNGEON_COOLDOWN)) == oldHandlerExists
                    && (!oldHandlerExists || ReferenceEquals(handlers[Proto.DUNGEON_COOLDOWN], oldHandler)));
                Debug.Log("CLIVERIFY dungeon-cooldown VERDICT pass=" + pass);
            }
            finally
            {
                model.CooldownEndTimes.Clear();
                foreach (KeyValuePair<uint, uint> pair in oldCooldowns)
                    model.CooldownEndTimes[pair.Key] = pair.Value;
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                restored = controller.IsInitialized == oldInitialized
                    && DictionaryEquals(model.CooldownEndTimes, oldCooldowns)
                    && (handlers != null && handlers.Contains(Proto.DUNGEON_COOLDOWN)) == oldHandlerExists
                    && (!oldHandlerExists || ReferenceEquals(handlers[Proto.DUNGEON_COOLDOWN], oldHandler))
                    && (interceptField == null || ReferenceEquals(interceptField.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-cooldown restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller, uint dunId, uint nextTime)
        {
            byte[] bytes = new CliVerify.Pkt().I(dunId).I(nextTime).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Frame(byte[] frame, uint dunId)
        {
            return frame != null && frame.Length == 10
                && frame[0] == 0 && frame[1] == 10 && frame[2] == 3 && frame[3] == 232
                && frame[4] == 238 && frame[5] == 117
                && frame[6] == (byte)(dunId >> 24) && frame[7] == (byte)(dunId >> 16)
                && frame[8] == (byte)(dunId >> 8) && frame[9] == (byte)dunId;
        }

        private static bool DictionaryEquals(Dictionary<uint, uint> actual, Dictionary<uint, uint> expected)
        {
            if (actual.Count != expected.Count) return false;
            foreach (KeyValuePair<uint, uint> pair in expected)
                if (!actual.TryGetValue(pair.Key, out uint value) || value != pair.Value) return false;
            return true;
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-cooldown " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
