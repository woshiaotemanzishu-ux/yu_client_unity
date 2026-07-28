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
    /// <summary>61113 灵魄奖励状态：分桶整表快照、严格 c 请求、读尾与无全局 Dispose 的环境恢复。</summary>
    public static class DungeonRuneRewardInfoCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
        private sealed class Wire { public uint Id; public byte Type; public byte Status; }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY dungeon-rune-reward EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            var oldBuckets = new Dictionary<byte, DungeonModel.RuneRewardSnapshot>(model.DungeonRuneRewardInfoByType);
            FieldInfo intercept = typeof(DungeonController).GetField("s_runeRewardInfoOutboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null && handlers.Contains(Proto.DUNGEON_RUNE_REWARD_INFO);
            object oldHandler = oldHandlerExists ? handlers[Proto.DUNGEON_RUNE_REWARD_INFO] : null;
            bool pass = false, restored = false;
            try
            {
                model.ClearDungeonRuneRewardInfo();
                MethodInfo handler = typeof(DungeonController).GetMethod("On61113", IF);
                MethodInfo request = typeof(DungeonController).GetMethod("RequestDungeonRuneRewardInfo");
                pass = Proto.DUNGEON_RUNE_REWARD_INFO == 61113 && handler != null && request != null
                    && intercept != null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "registration/seams/no-auto-send", pass);

                var seed = new List<DungeonModel.RuneRewardEntry> { Entry(7, 8, 9) };
                model.ApplyDungeonRuneRewardInfo(1, seed);
                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                controller.RequestDungeonRuneRewardInfo(255);
                bool seedCopied = model.TryGetDungeonRuneRewardInfo(1, out var seeded)
                    && !ReferenceEquals(seeded.Entries, seed) && seeded.Entries.Count == 1;
                seed.Clear();
                Check(ref pass, "exact 7B c request/no response preserves immutable copy", frames.Count == 1
                    && Frame(frames[0], 255) && seedCopied && seeded.Entries.Count == 1
                    && seeded.Entries[0].DunId == 7 && seeded.Entries[0].RewardType == 8
                    && seeded.Entries[0].RewardStatus == 9);

                var multi = new[] { new Wire { Id = uint.MaxValue, Type = 0, Status = 255 }, new Wire { Id = 0, Type = 255, Status = 128 }, new Wire { Id = uint.MaxValue, Type = 0, Status = 7 } };
                Check(ref pass, "u32/u8 bounds duplicate original order unknown status/read-to-end", Feed(handler, controller, 0, multi) && Snapshot(model, 0, multi));
                DungeonModel.RuneRewardSnapshot before = model.DungeonRuneRewardInfoByType[0];
                var single = new[] { new Wire { Id = 3, Type = 4, Status = 5 } };
                Check(ref pass, "same type multi-to-single whole replace", Feed(handler, controller, 0, single)
                    && Snapshot(model, 0, single) && !ReferenceEquals(model.DungeonRuneRewardInfoByType[0], before));
                Check(ref pass, "other type isolated and active push semantics", Feed(handler, controller, 255, new[] { new Wire { Id = 0, Type = 0, Status = 0 } })
                    && Snapshot(model, 0, single) && Snapshot(model, 255, new[] { new Wire { Id = 0, Type = 0, Status = 0 } }));
                Check(ref pass, "same type single-to-empty loaded/read-to-end", Feed(handler, controller, 0, Array.Empty<Wire>())
                    && Snapshot(model, 0, Array.Empty<Wire>()));
                model.ClearDungeonRuneRewardInfo();
                Check(ref pass, "clear/dispose-owned slice only", model.DungeonRuneRewardInfoByType.Count == 0
                    && typeof(DungeonController).GetMethod("Dispose") != null);
                Check(ref pass, "ambient untouched", controller.IsInitialized == oldInitialized && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-rune-reward VERDICT pass=" + pass);
            }
            finally
            {
                model.ClearDungeonRuneRewardInfo();
                foreach (var pair in oldBuckets) model.DungeonRuneRewardInfoByType.Add(pair.Key, pair.Value);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                restored = controller.IsInitialized == oldInitialized && SameBuckets(model.DungeonRuneRewardInfoByType, oldBuckets)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler)
                    && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY dungeon-rune-reward restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static DungeonModel.RuneRewardEntry Entry(uint id, byte type, byte status) =>
            new DungeonModel.RuneRewardEntry(id, type, status);
        private static bool Feed(MethodInfo method, DungeonController controller, byte dunType, params Wire[] rows)
        {
            var p = new CliVerify.Pkt().C(dunType).H(rows.Length);
            foreach (Wire row in rows) p.I(row.Id).C(row.Type).C(row.Status);
            byte[] bytes = p.Bytes(); var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader }); return reader.Remaining == 0;
        }
        private static bool Frame(byte[] f, byte type) => f != null && f.Length == 7 && f[0] == 0 && f[1] == 7 && f[2] == 3 && f[3] == 232 && f[4] == 238 && f[5] == 185 && f[6] == type;
        private static bool Snapshot(DungeonModel m, byte type, IReadOnlyList<Wire> expected)
        {
            if (!m.TryGetDungeonRuneRewardInfo(type, out var got) || !got.Loaded || got.Entries.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++) if (got.Entries[i].DunId != expected[i].Id || got.Entries[i].RewardType != expected[i].Type || got.Entries[i].RewardStatus != expected[i].Status) return false;
            return true;
        }
        private static bool SameBuckets(Dictionary<byte, DungeonModel.RuneRewardSnapshot> actual, Dictionary<byte, DungeonModel.RuneRewardSnapshot> old) { if (actual.Count != old.Count) return false; foreach (var p in old) if (!actual.TryGetValue(p.Key, out var v) || !ReferenceEquals(v, p.Value)) return false; return true; }
        private static bool HandlerUnchanged(IDictionary h, bool exists, object value) => h != null && h.Contains(Proto.DUNGEON_RUNE_REWARD_INFO) == exists && (!exists || ReferenceEquals(h[Proto.DUNGEON_RUNE_REWARD_INFO], value));
        private static void Check(ref bool pass, string name, bool ok) { Debug.Log("CLIVERIFY dungeon-rune-reward " + name + " ok=" + ok); pass &= ok; }
    }
}
