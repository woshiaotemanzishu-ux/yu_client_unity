using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.ActivityForeshow;
using Shenxiao.Module.Core.SnatchTreasure;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>65201 only: keep 65208's ActivityForeshow ownership ambient and untouched.</summary>
    public static class SnatchTreasureCase
    {
        private const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY snatchtreasure EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunCore()
        {
            SnatchTreasureController ctrl = SnatchTreasureController.Instance;
            SnatchTreasureModel model = SnatchTreasureModel.Instance;
            ActivityForeshowController foreshow = ActivityForeshowController.Instance;
            bool wasCtrl = ctrl.IsInitialized, wasForeshow = foreshow.IsInitialized;
            SavedState saved = new SavedState(model);
            FieldInfo intercept = typeof(SnatchTreasureController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = intercept?.GetValue(null);
            IDictionary handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 65200; id <= 65208; id++) SaveHandler(handlers, savedHandlers, id);
            bool pass = false;
            bool restored = false;
            try
            {
                if (ctrl.IsInitialized) ctrl.Dispose();
                if (handlers != null)
                {
                    for (int id = 65200; id <= 65208; id++) handlers.Remove(id);
                    if (savedHandlers[65208].Exists) handlers[65208] = savedHandlers[65208].Value;
                }
                model.Clear();
                ctrl.Init();
                bool had65208 = savedHandlers[65208].Exists;
                object old65208 = savedHandlers[65208].Value;
                MethodInfo on = typeof(SnatchTreasureController).GetMethod("On65201", F);
                MethodInfo onExit = typeof(SnatchTreasureController).GetMethod("On65206", F);
                pass = handlers != null && intercept != null && on != null && onExit != null && handlers.Contains(65201) && handlers.Contains(65206)
                    && handlers.Contains(65208) == had65208 && (!had65208 || ReferenceEquals(old65208, handlers[65208]));
                pass &= foreshow.IsInitialized == wasForeshow;
                for (int id = 65200; id <= 65208; id++)
                    pass &= handlers.Contains(id) == (id == 65201 || id == 65206 || (id == 65208 && had65208));
                void Check(string tag, bool value) { Debug.Log("CLIVERIFY snatchtreasure " + tag + " ok=" + value); if (!value) pass = false; }

                Check("registration 65201-65206 preserves ambient 65208", pass);
                if (pass)
                {
                    object handler65201 = handlers[65201];
                    object handler65206 = handlers[65206];
                    ctrl.Init();
                    Check("init idempotent", ctrl.IsInitialized && ReferenceEquals(handler65201, handlers[65201]) && ReferenceEquals(handler65206, handlers[65206]));

                    var frames = new List<byte[]>();
                    intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                    ctrl.RequestEntryInfo();
                    Check("explicit 6B empty request", Frames(frames, 65201));
                    frames.Clear(); ctrl.RequestExit();
                    Check("explicit 65206 6B empty request", Frames(frames, 65206));
                    frames.Clear(); EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                    Check("no GAME_START request", frames.Count == 0);

                    Feed(onExit, ctrl, new CliVerify.Pkt().I(0).Bytes(), out NetReader exitZero);
                    Check("65206 zero/read-end/no-outbound", exitZero.Remaining == 0 && model.HasExitResult && model.ExitResultCode == 0 && !model.HasEntryInfo && frames.Count == 0);
                    Feed(onExit, ctrl, new CliVerify.Pkt().I(1).Bytes(), out NetReader exitOne);
                    Check("65206 one/read-end", exitOne.Remaining == 0 && model.HasExitResult && model.ExitResultCode == 1 && frames.Count == 0);
                    Feed(onExit, ctrl, new CliVerify.Pkt().I(uint.MaxValue).Bytes(), out NetReader exitMax);
                    Check("65206 uint-max/read-end", exitMax.Remaining == 0 && model.HasExitResult && model.ExitResultCode == uint.MaxValue && frames.Count == 0);
                    Feed(onExit, ctrl, new CliVerify.Pkt().I(7).Bytes(), out NetReader exitOverwrite);
                    Check("65206 later-packet-overwrites", exitOverwrite.Remaining == 0 && model.HasExitResult && model.ExitResultCode == 7 && frames.Count == 0);

                    Feed(on, ctrl, new CliVerify.Pkt().H(2)
                        .I(uint.MaxValue).H(ushort.MaxValue).L(-1).S("中文")
                        .I(uint.MaxValue).H(0).L(-1).S("")
                        .H(ushort.MaxValue).C(byte.MaxValue).Bytes(), out NetReader multi);
                    Check("wire/order/duplicates/tail/read-end", multi.Remaining == 0 && model.HasEntryInfo && model.BelongList.Count == 2
                        && model.BelongList[0].DunId == uint.MaxValue && model.BelongList[0].Score == ushort.MaxValue
                        && model.BelongList[0].GuildId == ulong.MaxValue && model.BelongList[0].GuildName == "中文"
                        && model.BelongList[1].DunId == uint.MaxValue && model.BelongList[1].Score == 0
                        && model.BelongList[1].GuildId == ulong.MaxValue && model.BelongList[1].GuildName == ""
                        && model.TerritoryScore == ushort.MaxValue && model.HaveTerritory == byte.MaxValue);
                    Feed(on, ctrl, new CliVerify.Pkt().H(1).I(7).H(8).L(9).S("single").H(10).C(11).Bytes(), out NetReader single);
                    Check("whole replacement", single.Remaining == 0 && model.BelongList.Count == 1 && model.BelongList[0].DunId == 7
                        && model.TerritoryScore == 10 && model.HaveTerritory == 11);
                    Check("65201 isolated from 65206", model.HasExitResult && model.ExitResultCode == 7);

                    List<SnatchTreasureModel.BelongEntry> entryBeforeExit = model.BelongList;
                    Feed(onExit, ctrl, new CliVerify.Pkt().I(9).Bytes(), out NetReader exitIsolated);
                    Check("65206 isolated from 65201", exitIsolated.Remaining == 0 && model.ExitResultCode == 9
                        && ReferenceEquals(model.BelongList, entryBeforeExit) && model.BelongList.Count == 1
                        && model.TerritoryScore == 10 && model.HaveTerritory == 11 && frames.Count == 0);
                    int before = model.BelongList.Count; Check("no response retains state", before == 1 && model.TerritoryScore == 10);
                    Feed(on, ctrl, new CliVerify.Pkt().H(0).H(0).C(0).Bytes(), out NetReader empty);
                    Check("empty replacement", empty.Remaining == 0 && model.HasEntryInfo && model.BelongList.Count == 0 && model.TerritoryScore == 0 && model.HaveTerritory == 0);
                    Check("65206 survives 65201 replacement", model.HasExitResult && model.ExitResultCode == 9 && frames.Count == 0);

                    ctrl.Dispose();
                    Check("dispose preserves ambient 65208", !ctrl.IsInitialized && !handlers.Contains(65201)
                        && handlers.Contains(65208) == had65208 && (!had65208 || ReferenceEquals(old65208, handlers[65208]))
                        && foreshow.IsInitialized == wasForeshow && !model.HasEntryInfo && model.BelongList.Count == 0);
                    Check("dispose clears 65206 result", !handlers.Contains(65206) && !model.HasExitResult && model.ExitResultCode == 0);
                }
                Debug.Log("CLIVERIFY snatchtreasure VERDICT pass=" + pass);
            }
            finally
            {
                if (ctrl.IsInitialized) ctrl.Dispose();
                model.Clear(); saved.Restore(model);
                if (wasCtrl) ctrl.Init();
                if (intercept != null) intercept.SetValue(null, oldInterceptor);
                for (int id = 65200; id <= 65208; id++) RestoreHandler(handlers, savedHandlers[id], id);

                restored = ReferenceEquals(SnatchTreasureController.Instance, ctrl)
                    && ReferenceEquals(SnatchTreasureModel.Instance, model)
                    && ctrl.IsInitialized == wasCtrl && foreshow.IsInitialized == wasForeshow
                    && saved.Matches(model)
                    && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldInterceptor));
                for (int id = 65200; id <= 65208; id++) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY snatchtreasure restored=" + restored);
                // Never initialise, dispose, or otherwise mutate ActivityForeshow in this isolated case.
            }

            return pass && restored ? 0 : 3;
        }

        private static void Feed(MethodInfo method, SnatchTreasureController ctrl, byte[] bytes, out NetReader r)
        { r = new NetReader(bytes, 0, bytes.Length); method.Invoke(ctrl, new object[] { r }); }

        private static bool Frames(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] f = frames[i];
                if (f == null || f.Length != 6 || f[0] != 0 || f[1] != 6 || f[2] != 3 || f[3] != 232 || f[4] != (byte)(ids[i] >> 8) || f[5] != (byte)ids[i]) return false;
            }
            return true;
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

        private static void RestoreModelProperty(SnatchTreasureModel model, string propertyName, object value)
        {
            typeof(SnatchTreasureModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private sealed class SavedState
        {
            private readonly bool _loaded;
            private readonly ushort _score;
            private readonly byte _have;
            private readonly List<SnatchTreasureModel.BelongEntry> _list;
            private readonly bool _hasExit;
            private readonly uint _exitCode;

            public SavedState(SnatchTreasureModel m)
            {
                _loaded = m.HasEntryInfo;
                _score = m.TerritoryScore;
                _have = m.HaveTerritory;
                _list = m.BelongList;
                _hasExit = m.HasExitResult;
                _exitCode = m.ExitResultCode;
            }

            public void Restore(SnatchTreasureModel m)
            {
                m.ReplaceEntryInfo(_list, _score, _have);
                RestoreModelProperty(m, "HasEntryInfo", _loaded);
                m.ReplaceExitResult(_exitCode);
                RestoreModelProperty(m, "HasExitResult", _hasExit);
            }

            public bool Matches(SnatchTreasureModel m)
            {
                return m.HasEntryInfo == _loaded && m.TerritoryScore == _score && m.HaveTerritory == _have
                    && ReferenceEquals(m.BelongList, _list)
                    && m.HasExitResult == _hasExit && m.ExitResultCode == _exitCode;
            }
        }
    }
}
