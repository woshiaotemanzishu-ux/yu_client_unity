using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Rune;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// R505 / 16704、16705、16706、16709：启动顺序、三种变长 u64 请求、原始预览快照、
    /// ObjectList 顺序/重复/空覆盖、切片隔离、无回包保留及 ambient 深恢复。
    /// </summary>
    public static class RuneReadContinuationCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly int[] AllCommands =
            { 16700, 16701, 16702, 16703, 16704, 16705, 16706, 16707, 16708, 16709, 16710, 16711 };
        private static readonly int[] RegisteredCommands =
            { 16700, 16701, 16702, 16704, 16705, 16706, 16709 };
        private static readonly int[] DeferredCommands = { 16703, 16707, 16708, 16710, 16711 };

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY rune-read-continuation EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            RuneController controller = RuneController.Instance;
            RuneModel model = RuneModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            int oldRunePoint = model.RunePoint;
            int oldRuneChip = model.RuneChip;
            long oldSumPower = model.SumPower;
            bool oldHasData = model.HasData;
            bool oldHasRuneBag = model.HasRuneBag;
            var oldSlots = new List<RuneModel.SlotVo>(model.Slots);
            var oldBag = new List<RuneModel.BagGoodsVo>(model.RuneBagGoods);
            RuneModel.DungeonLevelSnapshot oldLevel = model.DungeonLevel;
            RuneModel.ComposePreviewSnapshot oldCompose = model.ComposePreview;
            RuneModel.DecomposePreviewSnapshot oldDecompose = model.DecomposePreview;
            RuneModel.DismantlePreviewSnapshot oldDismantle = model.DismantlePreview;

            FieldInfo intercept = typeof(RuneController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, object>();
            if (handlers != null)
                foreach (int cmd in AllCommands)
                    if (handlers.Contains(cmd)) oldHandlers[cmd] = handlers[cmd];

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                model.ClearReadSnapshots();
                MethodInfo h04 = Handler("On16704");
                MethodInfo h05 = Handler("On16705");
                MethodInfo h06 = Handler("On16706");
                MethodInfo h09 = Handler("On16709");
                pass = Proto.RUNE_DUNGEON_LEVEL == 16704
                    && Proto.RUNE_COMPOSE_PREVIEW == 16705
                    && Proto.RUNE_DECOMPOSE_PREVIEW == 16706
                    && Proto.RUNE_DISMANTLE_PREVIEW == 16709
                    && h04 != null && h05 != null && h06 != null && h09 != null && intercept != null
                    && HasHandlers(handlers, RegisteredCommands, true)
                    && HasHandlers(handlers, DeferredCommands, false);
                Check(ref pass, "constants/registration/deferred-excluded", pass);

                model.ReplaceDungeonLevel(7);
                model.ReplaceComposePreview(8, 9);
                model.ReplaceDecomposePreview(10, 11, new List<RuneModel.ObjectEntry>());
                model.ReplaceDismantlePreview(12, new List<RuneModel.ObjectEntry>());
                RuneModel.DungeonLevelSnapshot sentinelLevel = model.DungeonLevel;
                RuneModel.ComposePreviewSnapshot sentinelCompose = model.ComposePreview;
                RuneModel.DecomposePreviewSnapshot sentinelDecompose = model.DecomposePreview;
                RuneModel.DismantlePreviewSnapshot sentinelDismantle = model.DismantlePreview;

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestStartup();
                controller.RequestComposePreview(0x0102030405060708UL, new ulong[] { 0, ulong.MaxValue });
                controller.RequestDecomposePreview(new ulong[] { 1, ulong.MaxValue });
                controller.RequestDismantlePreview(Array.Empty<ulong>());
                Check(ref pass, "exact startup/preview frames/no-response-preserves", frames.Count == 5
                    && Frame(frames[0], 16700, Array.Empty<byte>())
                    && Frame(frames[1], 16704, Array.Empty<byte>())
                    && Frame(frames[2], 16705, new CliVerify.Pkt().L(0x0102030405060708L).H(2).L(0).L(-1).Bytes())
                    && Frame(frames[3], 16706, new CliVerify.Pkt().H(2).L(1).L(-1).Bytes())
                    && Frame(frames[4], 16709, new CliVerify.Pkt().H(0).Bytes())
                    && ReferenceEquals(model.DungeonLevel, sentinelLevel)
                    && ReferenceEquals(model.ComposePreview, sentinelCompose)
                    && ReferenceEquals(model.DecomposePreview, sentinelDecompose)
                    && ReferenceEquals(model.DismantlePreview, sentinelDismantle));

                Check(ref pass, "16704 u16 absolute replace/read-end",
                    Feed(h04, controller, new CliVerify.Pkt().H(ushort.MaxValue))
                    && model.DungeonLevel.Level == ushort.MaxValue
                    && Feed(h04, controller, new CliVerify.Pkt().H(0))
                    && model.DungeonLevel.Level == 0);

                Check(ref pass, "16705 raw zero/max overwrite/read-end",
                    Feed(h05, controller, new CliVerify.Pkt().I(uint.MaxValue).I(uint.MaxValue))
                    && model.ComposePreview.Code == uint.MaxValue && model.ComposePreview.Level == uint.MaxValue
                    && Feed(h05, controller, new CliVerify.Pkt().I(0).I(0))
                    && model.ComposePreview.Code == 0 && model.ComposePreview.Level == 0);

                var decomposePacket = new CliVerify.Pkt().I(uint.MaxValue).L(-1).H(3)
                    .C(0).I(uint.MaxValue).I(0)
                    .C(0).I(uint.MaxValue).I(uint.MaxValue)
                    .C(byte.MaxValue).I(0).I(7);
                Check(ref pass, "16706 u64/object-list ordered-duplicate/frozen",
                    Feed(h06, controller, decomposePacket)
                    && model.DecomposePreview.Code == uint.MaxValue
                    && model.DecomposePreview.Experience == ulong.MaxValue
                    && Entries(model.DecomposePreview.Result, 3)
                    && model.DecomposePreview.Result[0].Style == 0
                    && model.DecomposePreview.Result[0].TypeId == uint.MaxValue
                    && model.DecomposePreview.Result[0].Count == 0
                    && model.DecomposePreview.Result[1].TypeId == uint.MaxValue
                    && model.DecomposePreview.Result[1].Count == uint.MaxValue
                    && model.DecomposePreview.Result[2].Style == byte.MaxValue
                    && model.DecomposePreview.Result[2].TypeId == 0
                    && model.DecomposePreview.Result[2].Count == 7
                    && IsFrozen(model.DecomposePreview.Result));
                RuneModel.DecomposePreviewSnapshot decomposeBefore09 = model.DecomposePreview;

                Check(ref pass, "16709 object-list ordered-duplicate/isolation",
                    Feed(h09, controller, new CliVerify.Pkt().I(1).H(2)
                        .C(1).I(2).I(3).C(1).I(2).I(4))
                    && model.DismantlePreview.Code == 1
                    && Entries(model.DismantlePreview.Result, 2)
                    && model.DismantlePreview.Result[0].TypeId == 2
                    && model.DismantlePreview.Result[0].Count == 3
                    && model.DismantlePreview.Result[1].TypeId == 2
                    && model.DismantlePreview.Result[1].Count == 4
                    && IsFrozen(model.DismantlePreview.Result)
                    && ReferenceEquals(model.DecomposePreview, decomposeBefore09));
                RuneModel.DismantlePreviewSnapshot dismantleBefore06 = model.DismantlePreview;

                Check(ref pass, "16706/16709 empty full replace remains loaded",
                    Feed(h06, controller, new CliVerify.Pkt().I(0).L(0).H(0))
                    && model.DecomposePreview != decomposeBefore09
                    && model.DecomposePreview.Code == 0 && model.DecomposePreview.Experience == 0
                    && model.DecomposePreview.Result.Count == 0
                    && ReferenceEquals(model.DismantlePreview, dismantleBefore06)
                    && Feed(h09, controller, new CliVerify.Pkt().I(0).H(0))
                    && model.DismantlePreview != dismantleBefore06
                    && model.DismantlePreview.Code == 0 && model.DismantlePreview.Result.Count == 0);

                model.ClearReadSnapshots();
                Check(ref pass, "clear owns four slices/base-state-isolated",
                    model.DungeonLevel == null && model.ComposePreview == null
                    && model.DecomposePreview == null && model.DismantlePreview == null
                    && model.RunePoint == oldRunePoint && model.RuneChip == oldRuneChip
                    && model.SumPower == oldSumPower && model.HasData == oldHasData
                    && model.HasRuneBag == oldHasRuneBag && SameRefs(model.Slots, oldSlots)
                    && SameBag(model.RuneBagGoods, oldBag));
                Debug.Log("CLIVERIFY rune-read-continuation VERDICT pass=" + pass);
            }
            finally
            {
                if (!oldInitialized && controller.IsInitialized) controller.Dispose();
                if (!oldInitialized) RestoreHandlers(handlers, AllCommands, oldHandlers);
                model.Clear();
                model.RunePoint = oldRunePoint;
                model.RuneChip = oldRuneChip;
                model.SumPower = oldSumPower;
                model.Slots.AddRange(oldSlots);
                model.RuneBagGoods.AddRange(oldBag);
                SetAuto(model, "HasData", oldHasData);
                SetAuto(model, "HasRuneBag", oldHasRuneBag);
                SetAuto(model, "DungeonLevel", oldLevel);
                SetAuto(model, "ComposePreview", oldCompose);
                SetAuto(model, "DecomposePreview", oldDecompose);
                SetAuto(model, "DismantlePreview", oldDismantle);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                restored = controller.IsInitialized == oldInitialized
                    && model.RunePoint == oldRunePoint && model.RuneChip == oldRuneChip
                    && model.SumPower == oldSumPower && model.HasData == oldHasData
                    && model.HasRuneBag == oldHasRuneBag && SameRefs(model.Slots, oldSlots)
                    && SameBag(model.RuneBagGoods, oldBag)
                    && ReferenceEquals(model.DungeonLevel, oldLevel)
                    && ReferenceEquals(model.ComposePreview, oldCompose)
                    && ReferenceEquals(model.DecomposePreview, oldDecompose)
                    && ReferenceEquals(model.DismantlePreview, oldDismantle)
                    && SameHandlers(handlers, AllCommands, oldHandlers)
                    && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldIntercept));
                Debug.Log("CLIVERIFY rune-read-continuation restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static MethodInfo Handler(string name) => typeof(RuneController).GetMethod(name, IF);

        private static bool Feed(MethodInfo method, RuneController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Frame(byte[] frame, int command, byte[] payload)
        {
            if (frame == null || payload == null || frame.Length != 6 + payload.Length) return false;
            int length = (frame[0] << 8) | frame[1];
            int actualCommand = (frame[4] << 8) | frame[5];
            if (length != frame.Length || frame[2] != 3 || frame[3] != 232 || actualCommand != command)
                return false;
            for (int i = 0; i < payload.Length; i++) if (frame[6 + i] != payload[i]) return false;
            return true;
        }

        private static bool Entries(IReadOnlyList<RuneModel.ObjectEntry> entries, int count) =>
            entries != null && entries.Count == count;

        private static bool IsFrozen(IReadOnlyList<RuneModel.ObjectEntry> entries)
        {
            if (!(entries is IList<RuneModel.ObjectEntry> mutable)) return true;
            try { mutable.Add(new RuneModel.ObjectEntry(0, 0, 0)); return false; }
            catch (NotSupportedException) { return true; }
        }

        private static bool HasHandlers(IDictionary handlers, IReadOnlyList<int> commands, bool expected)
        {
            if (handlers == null) return false;
            for (int i = 0; i < commands.Count; i++) if (handlers.Contains(commands[i]) != expected) return false;
            return true;
        }

        private static bool SameHandlers(IDictionary handlers, IReadOnlyList<int> commands,
            Dictionary<int, object> oldHandlers)
        {
            if (handlers == null) return oldHandlers.Count == 0;
            for (int i = 0; i < commands.Count; i++)
            {
                int command = commands[i];
                bool existed = oldHandlers.TryGetValue(command, out object oldHandler);
                if (handlers.Contains(command) != existed
                    || (existed && !ReferenceEquals(handlers[command], oldHandler))) return false;
            }
            return true;
        }

        private static void RestoreHandlers(IDictionary handlers, IReadOnlyList<int> commands,
            Dictionary<int, object> oldHandlers)
        {
            if (handlers == null) return;
            for (int i = 0; i < commands.Count; i++)
            {
                int command = commands[i];
                if (handlers.Contains(command)) handlers.Remove(command);
                if (oldHandlers.TryGetValue(command, out object oldHandler)) handlers[command] = oldHandler;
            }
        }

        private static bool SameRefs<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) where T : class
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static bool SameBag(IReadOnlyList<RuneModel.BagGoodsVo> actual,
            IReadOnlyList<RuneModel.BagGoodsVo> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (actual[i].GoodsId != expected[i].GoodsId || actual[i].TypeId != expected[i].TypeId
                    || actual[i].Num != expected[i].Num) return false;
            return true;
        }

        private static void SetAuto<T>(RuneModel model, string property, T value)
        {
            FieldInfo field = typeof(RuneModel).GetField("<" + property + ">k__BackingField", IF);
            if (field == null) throw new MissingFieldException(typeof(RuneModel).FullName, property);
            field.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY rune-read-continuation " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
