using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.MiniGame;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>R508：MiniGame 39900/01/02/04/22 原始读侧、启动帧、键控排行与环境恢复。</summary>
    public static class MiniGameCase
    {
        private const BindingFlags IF = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly int[] Registered = { 39900, 39901, 39902, 39904, 39922 };

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY minigame EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            MiniGameController controller = MiniGameController.Instance;
            MiniGameModel model = MiniGameModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            Dictionary<FieldInfo, object> oldAutoFields = CaptureAutoFields(model);
            var oldRanks = new Dictionary<MiniGameModel.RankKey, MiniGameModel.RankSnapshot>(model.Ranks);
            FieldInfo intercept = typeof(MiniGameController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var oldHandlers = CaptureHandlers(handlers);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                RemoveFamilyHandlers(handlers);
                controller.Init();
                model.Reset();

                MethodInfo h00 = Handler("On39900");
                MethodInfo h01 = Handler("On39901");
                MethodInfo h02 = Handler("On39902");
                MethodInfo h04 = Handler("On39904");
                MethodInfo h22 = Handler("On39922");
                pass = Proto.MINI_GAME_ERROR == 39900 && Proto.MINI_GAME_START_NOTICE == 39901
                    && Proto.MINI_GAME_CURRENT == 39902 && Proto.MINI_GAME_RANK == 39904
                    && Proto.MINI_GAME_ELIM_RECONNECT == 39922 && intercept != null
                    && h00 != null && h01 != null && h02 != null && h04 != null && h22 != null
                    && RegistrationsExact(handlers) && RequestSurfaceExact();
                Check(ref pass, "constants/registration/surface", pass);

                model.ReplaceError(7, "sentinel");
                model.ReplaceStartNotice(8, 9, 10, 11, 12, 13, new uint[] { 14 });
                model.ReplaceCurrent(15, 16, 17, 18, 19, new uint[] { 20 });
                model.ReplaceRank(21, 22, 23, new[]
                    { new MiniGameModel.RankEntry(24, 25, 26, 27, "rank", 28) });
                model.ReplaceElimReconnect(29, 30, 31, 32, 33,
                    Array.Empty<MiniGameModel.BoardRow>(), Array.Empty<MiniGameModel.EffectEntry>(),
                    Array.Empty<MiniGameModel.ScoreChessEntry>());
                MiniGameModel.StartNoticeSnapshot sentStart = model.StartNotice;
                MiniGameModel.CurrentSnapshot sentCurrent = model.Current;
                MiniGameModel.ElimReconnectSnapshot sentReconnect = model.ElimReconnect;
                int sentRankCount = model.Ranks.Count;

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestStartup();
                controller.RequestRank(0, 0, 0);
                controller.RequestRank(byte.MaxValue, ushort.MaxValue, byte.MaxValue);
                Check(ref pass, "exact requests/no-response-preserves", frames.Count == 3
                    && EmptyFrame(frames[0], 39902)
                    && RankFrame(frames[1], 0, 0, 0)
                    && RankFrame(frames[2], byte.MaxValue, ushort.MaxValue, byte.MaxValue)
                    && ReferenceEquals(model.StartNotice, sentStart)
                    && ReferenceEquals(model.Current, sentCurrent)
                    && ReferenceEquals(model.ElimReconnect, sentReconnect)
                    && model.Ranks.Count == sentRankCount && model.HasError && model.LastErrorCode == 7);
                frames.Clear();

                Check(ref pass, "39900 full overwrite",
                    Feed(h00, controller, new CliVerify.Pkt().I(uint.MaxValue).S("中文"))
                    && model.HasError && model.LastErrorCode == uint.MaxValue && model.LastErrorMessage == "中文"
                    && Feed(h00, controller, new CliVerify.Pkt().I(0).S(string.Empty))
                    && model.LastErrorCode == 0 && model.LastErrorMessage == string.Empty);

                Check(ref pass, "39901 raw max/order/duplicate/full overwrite",
                    Feed(h01, controller, new CliVerify.Pkt().I(uint.MaxValue).C(255).H(65535).C(255)
                        .I(uint.MaxValue).I(uint.MaxValue).H(3).I(9).I(9).I(0))
                    && model.StartNotice.Code == uint.MaxValue && model.StartNotice.GameType == 255
                    && model.StartNotice.ModuleId == 65535 && model.StartNotice.SubId == 255
                    && model.StartNotice.StartTime == uint.MaxValue && model.StartNotice.EndTime == uint.MaxValue
                    && U32s(model.StartNotice.Info, 9, 9, 0)
                    && Feed(h01, controller, new CliVerify.Pkt().I(0).C(0).H(0).C(0).I(0).I(0).H(0))
                    && model.StartNotice.Code == 0 && model.StartNotice.GameType == 0
                    && model.StartNotice.ModuleId == 0 && model.StartNotice.SubId == 0
                    && model.StartNotice.StartTime == 0 && model.StartNotice.EndTime == 0
                    && model.StartNotice.Info.Count == 0 && frames.Count == 0);
                MiniGameModel.StartNoticeSnapshot startAfter01 = model.StartNotice;

                Check(ref pass, "39902 raw zero-valid/full overwrite/isolation",
                    Feed(h02, controller, new CliVerify.Pkt().C(255).H(65535).C(255)
                        .I(uint.MaxValue).I(uint.MaxValue).H(2).I(5).I(5))
                    && model.Current.GameType == 255 && model.Current.ModuleId == 65535
                    && model.Current.SubId == 255 && model.Current.StartTime == uint.MaxValue
                    && model.Current.EndTime == uint.MaxValue && U32s(model.Current.Info, 5, 5)
                    && Feed(h02, controller, new CliVerify.Pkt().C(0).H(0).C(0).I(0).I(0).H(0))
                    && model.Current.GameType == 0 && model.Current.ModuleId == 0 && model.Current.SubId == 0
                    && model.Current.StartTime == 0 && model.Current.EndTime == 0 && model.Current.Info.Count == 0
                    && ReferenceEquals(model.StartNotice, startAfter01));
                MiniGameModel.CurrentSnapshot currentAfter02 = model.Current;

                var rankA = new MiniGameModel.RankKey(1, 402, 2);
                var rankB = new MiniGameModel.RankKey(255, 65535, 255);
                Check(ref pass, "39904 composite-key/full fields/wire order/duplicates/empty",
                    Feed(h04, controller, new CliVerify.Pkt().C(1).H(402).C(2).H(2)
                        .I(9).I(8).H(7).L(-1).S("乙").I(6)
                        .I(5).I(4).H(3).L(-1).S(string.Empty).I(2))
                    && model.Ranks.TryGetValue(rankA, out MiniGameModel.RankSnapshot a)
                    && a.Entries.Count == 2 && a.Entries[0].ServerId == 9 && a.Entries[0].ServerNumber == 8
                    && a.Entries[0].Rank == 7 && a.Entries[0].RoleId == ulong.MaxValue
                    && a.Entries[0].Name == "乙" && a.Entries[0].Score == 6
                    && a.Entries[1].ServerId == 5 && a.Entries[1].RoleId == ulong.MaxValue
                    && a.Entries[1].Name == string.Empty && a.Entries[1].Score == 2
                    && Feed(h04, controller, new CliVerify.Pkt().C(255).H(65535).C(255).H(0))
                    && model.Ranks.TryGetValue(rankB, out MiniGameModel.RankSnapshot b) && b.Entries.Count == 0
                    && model.Ranks.TryGetValue(rankA, out a) && a.Entries.Count == 2
                    && Feed(h04, controller, new CliVerify.Pkt().C(1).H(402).C(2).H(0))
                    && model.Ranks.TryGetValue(rankA, out a) && a.Entries.Count == 0
                    && model.Ranks.TryGetValue(rankB, out b) && b.Entries.Count == 0
                    && ReferenceEquals(model.Current, currentAfter02));

                Check(ref pass, "39922 nested wire order/duplicates/max/full overwrite/isolation",
                    Feed(h22, controller, new CliVerify.Pkt().H(65535).C(255)
                        .I(uint.MaxValue).I(uint.MaxValue).I(uint.MaxValue)
                        .H(2).C(7).H(3).C(9).C(9).C(0).C(7).H(0)
                        .H(2).C(1).C(2).C(3).C(4).C(1).C(2).C(3).C(4)
                        .H(2).C(5).C(6).C(5).C(6))
                    && model.ElimReconnect.ModuleId == 65535 && model.ElimReconnect.SubId == 255
                    && model.ElimReconnect.StartTime == uint.MaxValue && model.ElimReconnect.EndTime == uint.MaxValue
                    && model.ElimReconnect.Score == uint.MaxValue && model.ElimReconnect.Board.Count == 2
                    && model.ElimReconnect.Board[0].RowId == 7 && Bytes(model.ElimReconnect.Board[0].Notes, 9, 9, 0)
                    && model.ElimReconnect.Board[1].RowId == 7 && model.ElimReconnect.Board[1].Notes.Count == 0
                    && model.ElimReconnect.Effects.Count == 2
                    && model.ElimReconnect.Effects[0].X == 1 && model.ElimReconnect.Effects[0].Y == 2
                    && model.ElimReconnect.Effects[0].EffectType == 3 && model.ElimReconnect.Effects[0].Parameter == 4
                    && model.ElimReconnect.Effects[1].X == 1 && model.ElimReconnect.ScoreChess.Count == 2
                    && model.ElimReconnect.ScoreChess[0].NoteId == 5 && model.ElimReconnect.ScoreChess[0].Rate == 6
                    && model.ElimReconnect.ScoreChess[1].NoteId == 5 && model.ElimReconnect.ScoreChess[1].Rate == 6
                    && Feed(h22, controller, new CliVerify.Pkt().H(0).C(0).I(0).I(0).I(0).H(0).H(0).H(0))
                    && model.ElimReconnect.ModuleId == 0 && model.ElimReconnect.SubId == 0
                    && model.ElimReconnect.StartTime == 0 && model.ElimReconnect.EndTime == 0
                    && model.ElimReconnect.Score == 0 && model.ElimReconnect.Board.Count == 0
                    && model.ElimReconnect.Effects.Count == 0 && model.ElimReconnect.ScoreChess.Count == 0
                    && ReferenceEquals(model.Current, currentAfter02) && model.Ranks.Count == 3);

                controller.RequestStartup();
                Check(ref pass, "request-after-loaded-preserves", frames.Count == 1 && EmptyFrame(frames[0], 39902)
                    && model.Current.GameType == 0 && model.ElimReconnect.ModuleId == 0 && model.Ranks.Count == 3);

                controller.Dispose();
                Check(ref pass, "dispose clears owned state/handlers", !controller.IsInitialized
                    && !model.HasError && model.LastErrorCode == 0 && model.LastErrorMessage == null
                    && model.StartNotice == null && model.Current == null && model.Ranks.Count == 0
                    && model.ElimReconnect == null && NoFamilyHandlers(handlers));
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                RestoreRanks(model, oldRanks);
                RestoreAutoFields(model, oldAutoFields);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (oldInitialized) controller.Init();
                RestoreHandlers(handlers, oldHandlers);
                restored = controller.IsInitialized == oldInitialized
                    && SameAutoFields(model, oldAutoFields) && SameRanks(model.Ranks, oldRanks)
                    && SameHandlers(handlers, oldHandlers)
                    && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldIntercept));
            }

            bool finalPass = pass && restored;
            Debug.Log("CLIVERIFY minigame restored=" + restored);
            Debug.Log("CLIVERIFY minigame VERDICT pass=" + finalPass);
            return finalPass ? 0 : 3;
        }

        private static MethodInfo Handler(string name) => typeof(MiniGameController).GetMethod(name, IF);

        private static bool Feed(MethodInfo method, MiniGameController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool RequestSurfaceExact()
        {
            int requests = 0;
            foreach (MethodInfo method in typeof(MiniGameController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name.StartsWith("Request", StringComparison.Ordinal))
                {
                    requests++;
                    if (method.Name != "RequestStartup" && method.Name != "RequestRank") return false;
                }
                if (method.Name.StartsWith("Send", StringComparison.Ordinal)) return false;
            }
            return requests == 2;
        }

        private static bool EmptyFrame(byte[] frame, int command) => Header(frame, command, 6);
        private static bool RankFrame(byte[] frame, byte gameType, ushort moduleId, byte subId)
            => Header(frame, 39904, 10) && frame[6] == gameType
                && frame[7] == (byte)(moduleId >> 8) && frame[8] == (byte)moduleId && frame[9] == subId;
        private static bool Header(byte[] frame, int command, int length) => frame != null && frame.Length == length
            && frame[0] == 0 && frame[1] == length && frame[2] == 3 && frame[3] == 232
            && frame[4] == (byte)(command >> 8) && frame[5] == (byte)command;

        private static bool IsRegistered(int command)
        {
            for (int i = 0; i < Registered.Length; i++) if (Registered[i] == command) return true;
            return false;
        }

        private static bool RegistrationsExact(IDictionary handlers)
        {
            if (handlers == null) return false;
            for (int command = 39900; command <= 39931; command++)
                if (handlers.Contains(command) != IsRegistered(command)) return false;
            return true;
        }

        private static bool NoFamilyHandlers(IDictionary handlers)
        {
            if (handlers == null) return false;
            for (int command = 39900; command <= 39931; command++)
                if (handlers.Contains(command)) return false;
            return true;
        }

        private static Dictionary<int, object> CaptureHandlers(IDictionary handlers)
        {
            var saved = new Dictionary<int, object>();
            if (handlers != null)
                for (int command = 39900; command <= 39931; command++)
                    if (handlers.Contains(command)) saved[command] = handlers[command];
            return saved;
        }

        private static void RemoveFamilyHandlers(IDictionary handlers)
        {
            if (handlers == null) return;
            for (int command = 39900; command <= 39931; command++) handlers.Remove(command);
        }

        private static void RestoreHandlers(IDictionary handlers, Dictionary<int, object> saved)
        {
            if (handlers == null) return;
            RemoveFamilyHandlers(handlers);
            foreach (KeyValuePair<int, object> pair in saved) handlers[pair.Key] = pair.Value;
        }

        private static bool SameHandlers(IDictionary handlers, Dictionary<int, object> saved)
        {
            if (handlers == null) return saved.Count == 0;
            for (int command = 39900; command <= 39931; command++)
            {
                bool existed = saved.TryGetValue(command, out object oldHandler);
                if (handlers.Contains(command) != existed
                    || (existed && !ReferenceEquals(handlers[command], oldHandler))) return false;
            }
            return true;
        }

        private static Dictionary<FieldInfo, object> CaptureAutoFields(MiniGameModel model)
        {
            var fields = new Dictionary<FieldInfo, object>();
            foreach (FieldInfo field in typeof(MiniGameModel).GetFields(IF))
                if (field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal)) fields[field] = field.GetValue(model);
            return fields;
        }

        private static void RestoreAutoFields(MiniGameModel model, Dictionary<FieldInfo, object> values)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in values) pair.Key.SetValue(model, pair.Value);
        }

        private static bool SameAutoFields(MiniGameModel model, Dictionary<FieldInfo, object> values)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in values)
                if (!Equals(pair.Key.GetValue(model), pair.Value)) return false;
            return true;
        }

        private static void RestoreRanks(MiniGameModel model,
            Dictionary<MiniGameModel.RankKey, MiniGameModel.RankSnapshot> saved)
        {
            FieldInfo field = typeof(MiniGameModel).GetField("_rankByKey", IF);
            var ranks = field?.GetValue(model) as Dictionary<MiniGameModel.RankKey, MiniGameModel.RankSnapshot>;
            if (ranks == null) return;
            ranks.Clear();
            foreach (KeyValuePair<MiniGameModel.RankKey, MiniGameModel.RankSnapshot> pair in saved) ranks[pair.Key] = pair.Value;
        }

        private static bool SameRanks(IReadOnlyDictionary<MiniGameModel.RankKey, MiniGameModel.RankSnapshot> actual,
            Dictionary<MiniGameModel.RankKey, MiniGameModel.RankSnapshot> expected)
        {
            if (actual.Count != expected.Count) return false;
            foreach (KeyValuePair<MiniGameModel.RankKey, MiniGameModel.RankSnapshot> pair in expected)
                if (!actual.TryGetValue(pair.Key, out MiniGameModel.RankSnapshot value)
                    || !ReferenceEquals(value, pair.Value)) return false;
            return true;
        }

        private static bool U32s(IReadOnlyList<uint> actual, params uint[] expected)
        {
            if (actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static bool Bytes(IReadOnlyList<byte> actual, params byte[] expected)
        {
            if (actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY minigame " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
