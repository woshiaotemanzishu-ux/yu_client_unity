using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.MonBook;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class MonBookCase
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception exception) { Debug.LogError("CLIVERIFY monbook EXCEPTION " + exception); return Task.FromResult(3); }
        }
        private static int RunCore()
        {
            MonBookController controller = MonBookController.Instance;
            MonBookModel model = MonBookModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            var oldPics = new List<uint>(model.ActivatedPics);
            var oldPreviewPowers = new Dictionary<uint, ulong>(model.PreviewPowers);
            FieldInfo interceptField = typeof(MonBookController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);
            try
            {
                controller.Init(); model.Reset();
                MethodInfo handler = typeof(MonBookController).GetMethod("On44205", InstanceNonPublic); MethodInfo preview = typeof(MonBookController).GetMethod("On44207", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && handler != null && preview != null && handlers != null && handlers.Contains(44205) && handlers.Contains(44207);
                for (int id = 44201; id <= 44207; id++) if (id != 44205 && id != 44207) pass &= !handlers.Contains(id);
                if (!pass) return 3;
                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestActivatedPics();
                pass &= Frame(frames.Count == 1 ? frames[0] : null); frames.Clear();
                controller.RequestPreviewPower(4000000000U); pass &= PreviewFrame(frames.Count == 1 ? frames[0] : null, 4000000000U); frames.Clear();
                pass &= Feed(preview, controller, new CliVerify.Pkt().I(7).L(0).Bytes()) && model.TryGetPreviewPower(7, out ulong zeroPower) && zeroPower == 0 && frames.Count == 0;
                pass &= Feed(preview, controller, new CliVerify.Pkt().I(7).L(5000000001L).Bytes()) && model.TryGetPreviewPower(7, out ulong replacedPower) && replacedPower == 5000000001UL && frames.Count == 0;
                pass &= Feed(preview, controller, new CliVerify.Pkt().I(7).L(unchecked((long)ulong.MaxValue)).Bytes()) && model.TryGetPreviewPower(7, out ulong maxPower) && maxPower == ulong.MaxValue && frames.Count == 0;
                pass &= Feed(preview, controller, new CliVerify.Pkt().I(uint.MaxValue).L(9).Bytes()) && model.TryGetPreviewPower(uint.MaxValue, out ulong otherPower) && otherPower == 9 && model.PreviewPowers.Count == 2 && frames.Count == 0;
                pass &= Feed(handler, controller, new CliVerify.Pkt().H(4).I(0).I(70000).I(70000).I(uint.MaxValue).Bytes())
                    && model.HasData && model.ActivatedPics.Count == 4 && model.ActivatedPics[0] == 0 && model.ActivatedPics[1] == 70000
                    && model.ActivatedPics[2] == 70000 && model.ActivatedPics[3] == uint.MaxValue && frames.Count == 0;
                pass &= Feed(handler, controller, new CliVerify.Pkt().H(1).I(7).Bytes())
                    && model.ActivatedPics.Count == 1 && model.ActivatedPics[0] == 7 && frames.Count == 0;
                pass &= Feed(handler, controller, new CliVerify.Pkt().H(0).Bytes())
                    && model.HasData && model.ActivatedPics.Count == 0 && frames.Count == 0;
                controller.Dispose(); pass &= !model.HasData && model.ActivatedPics.Count == 0 && model.PreviewPowers.Count == 0;
                Debug.Log("CLIVERIFY monbook VERDICT pass=" + pass); return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset(); if (oldHasData) model.Replace(oldPics); foreach (KeyValuePair<uint, ulong> entry in oldPreviewPowers) model.ReplacePreviewPower(entry.Key, entry.Value);
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
            }
        }
        private static bool Feed(MethodInfo handler, MonBookController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length); handler.Invoke(controller, new object[] { reader }); return reader.Remaining == 0;
        }
        private static bool Frame(byte[] frame)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(Proto.MON_BOOK_ACTIVATED_PICS >> 8)
                && frame[5] == (byte)(Proto.MON_BOOK_ACTIVATED_PICS & 0xFF);
        }
        private static bool PreviewFrame(byte[] frame, uint picId)
        {
            return frame != null && frame.Length == 10 && frame[0] == 0 && frame[1] == 10 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(Proto.MON_BOOK_PREVIEW_POWER >> 8)
                && frame[5] == (byte)(Proto.MON_BOOK_PREVIEW_POWER & 0xFF)
                && frame[6] == (byte)(picId >> 24) && frame[7] == (byte)(picId >> 16) && frame[8] == (byte)(picId >> 8) && frame[9] == (byte)picId;
        }
    }
}
