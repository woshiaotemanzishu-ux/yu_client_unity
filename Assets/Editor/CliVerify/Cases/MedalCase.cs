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
            FieldInfo intercept = typeof(MedalController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on13401 = typeof(MedalController).GetMethod("On13401", F);
                MethodInfo on13405 = typeof(MedalController).GetMethod("On13405", F);
                var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                bool pass = intercept != null && on13401 != null && on13405 != null && handlers != null && handlers.Contains(Proto.MEDAL_INFO) && handlers.Contains(Proto.MEDAL_TITLE_SNAPSHOT) && !handlers.Contains(13400);
                void Check(string tag, bool ok) { Debug.Log("CLIVERIFY medal " + tag + " ok=" + ok); if (!ok) pass = false; }
                Check("seams/register-13401-13405", pass);
                if (!pass) return 3;
                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; }));
                controller.RequestStartup();
                Check("startup exact empty frames", Frames(frames, Proto.MEDAL_INFO, Proto.MEDAL_TITLE_SNAPSHOT));
                frames.Clear();
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
                    && model.TitleEntries[1].Id == 101 && model.TitleEntries[1].Level == 3 && model.TitleEntries[1].Power == 201 && model.TitleEntries[1].IsEquip == 1 && frames.Count == 0);
                byte[] emptyTitles = new CliVerify.Pkt().H(0).Bytes();
                var emptyTitlesReader = new NetReader(emptyTitles, 0, emptyTitles.Length);
                on13405.Invoke(controller, new object[] { emptyTitlesReader });
                Check("titles full replace empty", emptyTitlesReader.Remaining == 0 && model.HasTitleData && model.TitleEntries.Count == 0 && frames.Count == 0);
                controller.Dispose();
                Check("dispose reset", !controller.IsInitialized && !model.HasData && !model.HasTitleData && model.Id == 0 && model.Honour == 0 && model.Power == 0 && model.TitleEntries.Count == 0);
                Debug.Log("CLIVERIFY medal VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.ReplaceData(oldId, oldLevel, oldExp, oldHonour, oldPower, oldPass);
                if (oldHasTitleData) model.ReplaceTitles(oldTitles);
                if (wasInitialized) controller.Init();
                if (intercept != null) intercept.SetValue(null, oldIntercept);
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
