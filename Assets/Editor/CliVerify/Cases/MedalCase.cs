using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Medal;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>13401 勋章基础快照协议回归。</summary>
    public static class MedalCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY medal EXCEPTION " + e); return Task.FromResult(3); }
        }
        private static int RunSync()
        {
            MedalController controller = MedalController.Instance;
            MedalModel model = MedalModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            uint oldId = model.Id, oldLevel = model.StrengthenLevel, oldExp = model.StrengthenExp, oldPower = model.Power, oldPass = model.PassLayers;
            ulong oldHonour = model.Honour;
            bool oldHasData = model.HasData;
            var oldTitles = new List<MedalModel.TitleEntry>(model.TitleEntries);
            bool oldHasTitleData = model.HasTitleData;
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            FieldInfo intercept = typeof(MedalController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, object>();
            var oldHandlerExists = new Dictionary<int, bool>();
            for (int id = 13400; id <= 13407; id++)
            {
                oldHandlerExists[id] = handlers != null && handlers.Contains(id);
                if (oldHandlerExists[id]) oldHandlers[id] = handlers[id];
            }
            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on13400 = typeof(MedalController).GetMethod("On13400", F);
                MethodInfo on13401 = typeof(MedalController).GetMethod("On13401", F);
                MethodInfo on13405 = typeof(MedalController).GetMethod("On13405", F);
                bool boundary = handlers != null;
                for (int id = 13400; id <= 13407; id++)
                    boundary &= handlers.Contains(id) == (id == 13400 || id == 13401 || id == 13405);
                bool pass = intercept != null && on13400 != null && on13401 != null && on13405 != null && boundary;
                void Check(string tag, bool ok) { Debug.Log("CLIVERIFY medal " + tag + " ok=" + ok); if (!ok) pass = false; }
                Check("seams/register-boundary-13400-13407", pass);
                if (!pass) return 3;
                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                controller.RequestStartup();
                Check("startup exact empty frames", Frames(frames, Proto.MEDAL_INFO, Proto.MEDAL_TITLE_SNAPSHOT));
                frames.Clear();
                var zeroReader = new NetReader(new CliVerify.Pkt().I(0).Bytes(), 0, 4);
                on13400.Invoke(controller, new object[] { zeroReader });
                Check("error raw-zero/uninitialized/no-outbound", zeroReader.Remaining == 0 && model.HasError && model.LastErrorCode == 0
                    && !model.HasData && !model.HasTitleData && model.TitleEntries.Count == 0 && frames.Count == 0);
                uint[] errorCodes = { 1, uint.MaxValue, 7 };
                foreach (uint code in errorCodes)
                {
                    byte[] error = new CliVerify.Pkt().I((long)code).Bytes();
                    var errorReader = new NetReader(error, 0, error.Length);
                    on13400.Invoke(controller, new object[] { errorReader });
                    Check("error raw-overwrite-" + code, errorReader.Remaining == 0 && model.HasError && model.LastErrorCode == code
                        && !model.HasData && !model.HasTitleData && model.TitleEntries.Count == 0 && frames.Count == 0);
                }
                byte[] first = new CliVerify.Pkt().I(1).I(2).I(3).L(5000000000L).I(4).I(5).Bytes();
                var firstReader = new NetReader(first, 0, first.Length);
                on13401.Invoke(controller, new object[] { firstReader });
                Check("fields/read-to-end/no-outbound", firstReader.Remaining == 0 && model.HasData && model.Id == 1 && model.StrengthenLevel == 2
                    && model.StrengthenExp == 3 && model.Honour == 5000000000UL && model.Power == 4 && model.PassLayers == 5 && frames.Count == 0);
                byte[] replacement = new CliVerify.Pkt().I(11).I(12).I(13).L(14).I(15).I(16).Bytes();
                var replacementReader = new NetReader(replacement, 0, replacement.Length);
                on13401.Invoke(controller, new object[] { replacementReader });
                Check("full replace", replacementReader.Remaining == 0 && model.Id == 11 && model.StrengthenLevel == 12 && model.StrengthenExp == 13
                    && model.Honour == 14 && model.Power == 15 && model.PassLayers == 16 && frames.Count == 0);
                byte[] titles = new CliVerify.Pkt().H(2).I(100).H(0).I(200).C(0).I(101).H(3).I(201).C(1).Bytes();
                var titlesReader = new NetReader(titles, 0, titles.Length);
                on13405.Invoke(controller, new object[] { titlesReader });
                Check("titles fields/order/read-to-end", titlesReader.Remaining == 0 && model.HasTitleData && model.TitleEntries.Count == 2
                    && model.TitleEntries[0].Id == 100 && model.TitleEntries[0].Level == 0 && model.TitleEntries[0].Power == 200 && model.TitleEntries[0].IsEquip == 0
                    && model.TitleEntries[1].Id == 101 && model.TitleEntries[1].Level == 3 && model.TitleEntries[1].Power == 201 && model.TitleEntries[1].IsEquip == 1
                    && model.HasError && model.LastErrorCode == 7 && frames.Count == 0);
                var isolatedErrorReader = new NetReader(new CliVerify.Pkt().I(9).Bytes(), 0, 4);
                on13400.Invoke(controller, new object[] { isolatedErrorReader });
                Check("error isolated-from-13401-13405", isolatedErrorReader.Remaining == 0 && model.LastErrorCode == 9
                    && model.Id == 11 && model.StrengthenLevel == 12 && model.StrengthenExp == 13 && model.Honour == 14 && model.Power == 15 && model.PassLayers == 16
                    && model.HasTitleData && model.TitleEntries.Count == 2 && frames.Count == 0);
                byte[] emptyTitles = new CliVerify.Pkt().H(0).Bytes();
                var emptyTitlesReader = new NetReader(emptyTitles, 0, emptyTitles.Length);
                on13405.Invoke(controller, new object[] { emptyTitlesReader });
                Check("titles full replace empty", emptyTitlesReader.Remaining == 0 && model.HasTitleData && model.TitleEntries.Count == 0
                    && model.HasError && model.LastErrorCode == 9 && frames.Count == 0);
                controller.Dispose();
                Check("dispose reset", !controller.IsInitialized && !model.HasData && !model.HasTitleData && !model.HasError && model.LastErrorCode == 0
                    && model.Id == 0 && model.Honour == 0 && model.Power == 0 && model.TitleEntries.Count == 0);
                Debug.Log("CLIVERIFY medal VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.ReplaceData(oldId, oldLevel, oldExp, oldHonour, oldPower, oldPass);
                if (oldHasTitleData) model.ReplaceTitles(oldTitles);
                if (oldHasError) model.SetError(oldErrorCode);
                if (wasInitialized) controller.Init();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (handlers != null)
                {
                    for (int id = 13400; id <= 13407; id++)
                    {
                        if (handlers.Contains(id)) handlers.Remove(id);
                        if (oldHandlerExists[id]) handlers[id] = oldHandlers[id];
                    }
                }
            }
        }
        private static bool Frame(IReadOnlyList<byte[]> frames, int id)
        {
            if (frames.Count != 1 || frames[0] == null) return false;
            byte[] f = frames[0];
            return f.Length == 6 && f[0] == 0 && f[1] == 6 && f[2] == 3 && f[3] == 232 && f[4] == (byte)(id >> 8) && f[5] == (byte)id;
        }
        private static bool Frames(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++) if (!Frame(new[] { frames[i] }, ids[i])) return false;
            return true;
        }
    }
}
