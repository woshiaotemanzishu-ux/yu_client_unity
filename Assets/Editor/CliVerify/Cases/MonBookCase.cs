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
            try
            {
                return Task.FromResult(RunCore());
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY monbook EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            MonBookController controller = MonBookController.Instance;
            MonBookModel model = MonBookModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            var oldPics = new List<uint>(model.ActivatedPics);
            var oldPreviewPowers = new Dictionary<uint, ulong>(model.PreviewPowers);
            var oldBooks = new Dictionary<ushort, MonBookModel.BookSnapshot>(model.Books);
            FieldInfo interceptField = typeof(MonBookController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo typeInfo = typeof(MonBookController).GetMethod("On44201", InstanceNonPublic);
                MethodInfo activated = typeof(MonBookController).GetMethod("On44205", InstanceNonPublic);
                MethodInfo preview = typeof(MonBookController).GetMethod("On44207", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = interceptField != null && typeInfo != null && activated != null && preview != null
                    && handlers != null && handlers.Contains(44201) && handlers.Contains(44205) && handlers.Contains(44207);
                for (int id = 44201; id <= 44207; id++)
                {
                    if (id != 44201 && id != 44205 && id != 44207) pass &= !handlers.Contains(id);
                }
                if (!pass) return 3;

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                controller.RequestType(ushort.MaxValue);
                pass &= TypeFrame(frames.Count == 1 ? frames[0] : null, ushort.MaxValue);
                frames.Clear();
                controller.RequestActivatedPics();
                pass &= ActivatedFrame(frames.Count == 1 ? frames[0] : null);
                frames.Clear();
                controller.RequestPreviewPower(4000000000U);
                pass &= PreviewFrame(frames.Count == 1 ? frames[0] : null, 4000000000U);
                frames.Clear();

                pass &= Feed(preview, controller, new CliVerify.Pkt().I(7).L(0).Bytes())
                    && model.TryGetPreviewPower(7, out ulong zeroPower) && zeroPower == 0 && frames.Count == 0;
                pass &= Feed(preview, controller, new CliVerify.Pkt().I(7).L(5000000001L).Bytes())
                    && model.TryGetPreviewPower(7, out ulong replacedPower) && replacedPower == 5000000001UL && frames.Count == 0;
                pass &= Feed(preview, controller, new CliVerify.Pkt().I(7).L(unchecked((long)ulong.MaxValue)).Bytes())
                    && model.TryGetPreviewPower(7, out ulong maxPower) && maxPower == ulong.MaxValue && frames.Count == 0;
                pass &= Feed(preview, controller, new CliVerify.Pkt().I(uint.MaxValue).L(9).Bytes())
                    && model.TryGetPreviewPower(uint.MaxValue, out ulong otherPower) && otherPower == 9
                    && model.PreviewPowers.Count == 2 && frames.Count == 0;

                pass &= Feed(activated, controller, new CliVerify.Pkt().H(4).I(0).I(70000).I(70000).I(uint.MaxValue).Bytes())
                    && model.HasData && model.ActivatedPics.Count == 4
                    && model.ActivatedPics[0] == 0 && model.ActivatedPics[1] == 70000
                    && model.ActivatedPics[2] == 70000 && model.ActivatedPics[3] == uint.MaxValue
                    && frames.Count == 0;

                byte[] firstType = new CliVerify.Pkt().H(0).H(2)
                    .I(uint.MaxValue).H(ushort.MaxValue)
                    .I(uint.MaxValue).H(0)
                    .H(3)
                    .I(7).H(0).L(0).L(unchecked((long)ulong.MaxValue))
                    .I(7).H(ushort.MaxValue).L(5000000001L).L(2)
                    .I(uint.MaxValue).H(1).L(unchecked((long)ulong.MaxValue)).L(0)
                    .L(unchecked((long)ulong.MaxValue))
                    .Bytes();
                pass &= Feed(typeInfo, controller, firstType)
                    && model.TryGetBook(0, out MonBookModel.BookSnapshot typeZero)
                    && typeZero.Type == 0 && typeZero.Groups.Count == 2
                    && typeZero.Groups[0].GroupId == uint.MaxValue && typeZero.Groups[0].Level == ushort.MaxValue
                    && typeZero.Groups[1].GroupId == uint.MaxValue && typeZero.Groups[1].Level == 0
                    && typeZero.Pictures.Count == 3
                    && typeZero.Pictures[0].PicId == 7 && typeZero.Pictures[0].Level == 0
                    && typeZero.Pictures[0].CurPower == 0 && typeZero.Pictures[0].NextPower == ulong.MaxValue
                    && typeZero.Pictures[1].PicId == 7 && typeZero.Pictures[1].Level == ushort.MaxValue
                    && typeZero.Pictures[1].CurPower == 5000000001UL && typeZero.Pictures[1].NextPower == 2
                    && typeZero.Pictures[2].PicId == uint.MaxValue && typeZero.Pictures[2].Level == 1
                    && typeZero.Pictures[2].CurPower == ulong.MaxValue && typeZero.Pictures[2].NextPower == 0
                    && typeZero.PicCombat == ulong.MaxValue
                    && model.ActivatedPics.Count == 4 && model.PreviewPowers.Count == 2 && frames.Count == 0;

                pass &= Feed(typeInfo, controller, new CliVerify.Pkt().H(ushort.MaxValue).H(0).H(0).L(0).Bytes())
                    && model.TryGetBook(ushort.MaxValue, out MonBookModel.BookSnapshot maxType)
                    && maxType.Type == ushort.MaxValue && maxType.Groups.Count == 0 && maxType.Pictures.Count == 0
                    && maxType.PicCombat == 0 && model.TryGetBook(0, out _) && model.Books.Count == 2
                    && model.ActivatedPics.Count == 4 && model.PreviewPowers.Count == 2 && frames.Count == 0;

                byte[] replacement = new CliVerify.Pkt().H(0).H(1).I(1).H(2).H(1)
                    .I(3).H(4).L(5).L(6).L(7).Bytes();
                pass &= Feed(typeInfo, controller, replacement)
                    && model.TryGetBook(0, out MonBookModel.BookSnapshot replaced)
                    && replaced.Groups.Count == 1 && replaced.Groups[0].GroupId == 1 && replaced.Groups[0].Level == 2
                    && replaced.Pictures.Count == 1 && replaced.Pictures[0].PicId == 3 && replaced.Pictures[0].Level == 4
                    && replaced.Pictures[0].CurPower == 5 && replaced.Pictures[0].NextPower == 6 && replaced.PicCombat == 7
                    && model.TryGetBook(ushort.MaxValue, out maxType) && maxType.PicCombat == 0
                    && model.ActivatedPics.Count == 4 && model.PreviewPowers.Count == 2 && frames.Count == 0;

                pass &= Feed(typeInfo, controller, new CliVerify.Pkt().H(0).H(0).H(0).L(0).Bytes())
                    && model.TryGetBook(0, out MonBookModel.BookSnapshot cleared)
                    && cleared.Groups.Count == 0 && cleared.Pictures.Count == 0 && cleared.PicCombat == 0
                    && model.TryGetBook(ushort.MaxValue, out _) && model.Books.Count == 2
                    && model.ActivatedPics.Count == 4 && model.PreviewPowers.Count == 2 && frames.Count == 0;

                pass &= Feed(activated, controller, new CliVerify.Pkt().H(1).I(7).Bytes())
                    && model.ActivatedPics.Count == 1 && model.ActivatedPics[0] == 7
                    && model.Books.Count == 2 && frames.Count == 0;
                pass &= Feed(activated, controller, new CliVerify.Pkt().H(0).Bytes())
                    && model.HasData && model.ActivatedPics.Count == 0
                    && model.Books.Count == 2 && model.PreviewPowers.Count == 2 && frames.Count == 0;

                controller.Dispose();
                pass &= !model.HasData && model.ActivatedPics.Count == 0
                    && model.PreviewPowers.Count == 0 && model.Books.Count == 0;
                Debug.Log("CLIVERIFY monbook VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasData) model.Replace(oldPics);
                foreach (KeyValuePair<uint, ulong> entry in oldPreviewPowers)
                {
                    model.ReplacePreviewPower(entry.Key, entry.Value);
                }
                foreach (KeyValuePair<ushort, MonBookModel.BookSnapshot> entry in oldBooks)
                {
                    MonBookModel.BookSnapshot snapshot = entry.Value;
                    model.ReplaceBook(entry.Key, new List<MonBookModel.GroupEntry>(snapshot.Groups),
                        new List<MonBookModel.PictureEntry>(snapshot.Pictures), snapshot.PicCombat);
                }
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
            }
        }

        private static bool Feed(MethodInfo handler, MonBookController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool ActivatedFrame(byte[] frame)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.MON_BOOK_ACTIVATED_PICS >> 8)
                && frame[5] == (byte)(Proto.MON_BOOK_ACTIVATED_PICS & 0xFF);
        }

        private static bool PreviewFrame(byte[] frame, uint picId)
        {
            return frame != null && frame.Length == 10
                && frame[0] == 0 && frame[1] == 10 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.MON_BOOK_PREVIEW_POWER >> 8)
                && frame[5] == (byte)(Proto.MON_BOOK_PREVIEW_POWER & 0xFF)
                && frame[6] == (byte)(picId >> 24) && frame[7] == (byte)(picId >> 16)
                && frame[8] == (byte)(picId >> 8) && frame[9] == (byte)picId;
        }

        private static bool TypeFrame(byte[] frame, ushort type)
        {
            return frame != null && frame.Length == 8
                && frame[0] == 0 && frame[1] == 8 && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.MON_BOOK_TYPE_INFO >> 8)
                && frame[5] == (byte)(Proto.MON_BOOK_TYPE_INFO & 0xFF)
                && frame[6] == (byte)(type >> 8) && frame[7] == (byte)(type & 0xFF);
        }
    }
}
