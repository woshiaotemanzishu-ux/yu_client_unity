using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Kf1vn;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>62100 原始快照、62101 阶段变化查询和 ambient 恢复。</summary>
    public static class Kf1vnActivityInfoCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private sealed class EntryState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY kf1vn-activity EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            Kf1vnController controller = Kf1vnController.Instance;
            Kf1vnModel model = Kf1vnModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasStageInfo = model.HasStageInfo;
            int oldStage = model.Stage;
            int oldTurn = model.Turn;
            long oldEdtime = model.Edtime;
            int oldSubStage = model.SubStage;
            long oldSubEdtime = model.SubEdtime;
            bool oldHasActivityInfo = model.HasActivityInfo;
            byte oldIsSign = model.IsSign;
            uint oldSignNum = model.SignNum;
            ushort oldDefNum = model.DefNum;
            byte oldZone = model.Zone;

            FieldInfo activityIntercept = typeof(Kf1vnController).GetField("s_activityInfoOutboundIntercept", SF);
            FieldInfo exitIntercept = typeof(Kf1vnController).GetField("s_exitOutboundIntercept", SF);
            FieldInfo lastLevelField = typeof(Kf1vnController).GetField("_lastLevel", IF);
            object oldActivityIntercept = activityIntercept?.GetValue(null);
            object oldExitIntercept = exitIntercept?.GetValue(null);
            object oldLastLevel = lastLevelField?.GetValue(controller);

            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            int[] handlerIds = { 62100, 62101, 62102, 62103, 62104, 62105, 62107, 62132 };
            var savedHandlers = new Dictionary<int, EntryState>();
            foreach (int id in handlerIds) savedHandlers[id] = SaveEntry(handlers, id);

            ActivityIconManager iconManager = ActivityIconManager.Instance;
            var mainIcons = typeof(ActivityIconManager).GetField("_iconInfoByType", IF)?.GetValue(iconManager) as IDictionary;
            var boxIcons = typeof(ActivityIconManager).GetField("_iconBoxInfoByType", IF)?.GetValue(iconManager) as IDictionary;
            EntryState oldMainIcon = SaveEntry(mainIcons, Kf1vnController.ICON_TYPE);
            EntryState oldBoxIcon = SaveEntry(boxIcons, Kf1vnController.ICON_TYPE);

            bool pass = false;
            bool restored = false;
            try
            {
                MethodInfo on62100 = typeof(Kf1vnController).GetMethod("On62100", IF);
                MethodInfo on62101 = typeof(Kf1vnController).GetMethod("On62101", IF);
                MethodInfo requestActivityInfo = typeof(Kf1vnController).GetMethod("RequestActivityInfo", IF);
                controller.Init();

                pass = activityIntercept != null && exitIntercept != null && lastLevelField != null
                    && handlers != null && mainIcons != null && boxIcons != null
                    && on62100 != null && on62101 != null && requestActivityInfo != null
                    && Proto.KF1VN_ACTIVITY_INFO == 62100
                    && handlers.Contains(62100) && handlers.Contains(62101)
                    && handlers.Contains(62103) && handlers.Contains(62132)
                    && !handlers.Contains(62102) && !handlers.Contains(62104)
                    && !handlers.Contains(62105) && !handlers.Contains(62107)
                    && typeof(Kf1vnController).GetMethod("RequestSign", BindingFlags.Instance | BindingFlags.Public) == null
                    && typeof(Kf1vnController).GetMethod("On62102", IF) == null;
                Check(ref pass, "registration/boundary/seams", pass);

                model.Reset();
                Check(ref pass, "no-stage-no-icon", !model.GetEntranceOpenState());
                bool tail0 = Feed62100(on62100, controller, 0, 0, 0, 0);
                Check(ref pass, "zero/read-tail", tail0 && model.HasActivityInfo
                    && model.IsSign == 0 && model.SignNum == 0 && model.DefNum == 0 && model.Zone == 0
                    && !model.GetEntranceOpenState());

                bool tailMax = Feed62100(on62100, controller, 255, uint.MaxValue, ushort.MaxValue, 255);
                Check(ref pass, "max/overwrite/read-tail", tailMax && model.HasActivityInfo
                    && model.IsSign == 255 && model.SignNum == uint.MaxValue
                    && model.DefNum == ushort.MaxValue && model.Zone == 255);

                model.SetStageInfo(1, 0, 0, 0, 0);
                model.SetActivityInfo(0, 0, 0, 0);
                Check(ref pass, "stage1-unsigned-open", model.GetEntranceOpenState() && model.GetIconText() == "报名中");
                model.SetActivityInfo(1, 0, 0, 0);
                Check(ref pass, "stage1-signed-closed", !model.GetEntranceOpenState());
                model.SetActivityInfo(255, 0, 0, 0);
                Check(ref pass, "stage1-unknown-open", model.GetEntranceOpenState());
                model.SetStageInfo(2, 0, 0, 0, 0);
                Check(ref pass, "stage2-open", model.GetEntranceOpenState() && model.GetIconText() == "进行中");
                model.SetStageInfo(6, 0, 0, 0, 0);
                Check(ref pass, "stage6-closed", !model.GetEntranceOpenState());

                var frames = new List<byte[]>();
                activityIntercept.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                model.Reset();
                bool firstTail = Feed62101(on62101, controller, 0);
                bool sameTail = Feed62101(on62101, controller, 0);
                bool changedTail = Feed62101(on62101, controller, 6);
                bool repeatedTail = Feed62101(on62101, controller, 6);
                Check(ref pass, "stage-first-and-change-only-query", firstTail && sameTail && changedTail && repeatedTail
                    && frames.Count == 2 && IsEmptyFrame(frames[0], 62100) && IsEmptyFrame(frames[1], 62100));

                model.SetActivityInfo(1, 2, 3, 4);
                model.Reset();
                Check(ref pass, "reset-clears-both-slices", !model.HasStageInfo && model.Stage == 0 && model.Turn == 0
                    && model.Edtime == 0 && model.SubStage == 0 && model.SubEdtime == 0
                    && !model.HasActivityInfo && model.IsSign == 0 && model.SignNum == 0
                    && model.DefNum == 0 && model.Zone == 0);
                Check(ref pass, "unrelated-ambient-untouched",
                    Equals(lastLevelField.GetValue(controller), oldLastLevel)
                    && ReferenceEquals(exitIntercept.GetValue(null), oldExitIntercept));

                Debug.Log("CLIVERIFY kf1vn-activity VERDICT pass=" + pass);
            }
            finally
            {
                if (!oldInitialized && controller.IsInitialized) controller.Dispose();
                if (oldInitialized && !controller.IsInitialized) controller.Init();

                model.Reset();
                model.Stage = oldStage;
                model.Turn = oldTurn;
                model.Edtime = oldEdtime;
                model.SubStage = oldSubStage;
                model.SubEdtime = oldSubEdtime;
                model.HasStageInfo = oldHasStageInfo;
                if (oldHasActivityInfo)
                    model.SetActivityInfo(oldIsSign, oldSignNum, oldDefNum, oldZone);

                if (lastLevelField != null) lastLevelField.SetValue(controller, oldLastLevel);
                if (activityIntercept != null) activityIntercept.SetValue(null, oldActivityIntercept);
                if (exitIntercept != null) exitIntercept.SetValue(null, oldExitIntercept);
                foreach (int id in handlerIds) RestoreEntry(handlers, id, savedHandlers[id]);
                RestoreEntry(mainIcons, Kf1vnController.ICON_TYPE, oldMainIcon);
                RestoreEntry(boxIcons, Kf1vnController.ICON_TYPE, oldBoxIcon);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasStageInfo == oldHasStageInfo && model.Stage == oldStage
                    && model.Turn == oldTurn && model.Edtime == oldEdtime
                    && model.SubStage == oldSubStage && model.SubEdtime == oldSubEdtime
                    && model.HasActivityInfo == oldHasActivityInfo
                    && model.IsSign == oldIsSign && model.SignNum == oldSignNum
                    && model.DefNum == oldDefNum && model.Zone == oldZone
                    && (lastLevelField == null || Equals(lastLevelField.GetValue(controller), oldLastLevel))
                    && (activityIntercept == null || ReferenceEquals(activityIntercept.GetValue(null), oldActivityIntercept))
                    && (exitIntercept == null || ReferenceEquals(exitIntercept.GetValue(null), oldExitIntercept))
                    && EntriesMatch(handlers, handlerIds, savedHandlers)
                    && EntryMatches(mainIcons, Kf1vnController.ICON_TYPE, oldMainIcon)
                    && EntryMatches(boxIcons, Kf1vnController.ICON_TYPE, oldBoxIcon);
                Debug.Log("CLIVERIFY kf1vn-activity restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed62100(
            MethodInfo method,
            Kf1vnController controller,
            byte isSign,
            uint signNum,
            ushort defNum,
            byte zone)
        {
            byte[] body = new CliVerify.Pkt().C(isSign).I(signNum).H(defNum).C(zone).Bytes();
            var reader = new NetReader(body, 0, body.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Feed62101(MethodInfo method, Kf1vnController controller, byte stage)
        {
            byte[] body = new CliVerify.Pkt().C(stage).H(0).I(0).C(0).I(0).Bytes();
            var reader = new NetReader(body, 0, body.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool IsEmptyFrame(byte[] frame, int protocolId)
        {
            return frame != null && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(protocolId >> 8)
                && frame[5] == (byte)protocolId;
        }

        private static EntryState SaveEntry(IDictionary map, object key)
        {
            bool exists = map != null && map.Contains(key);
            return new EntryState { Exists = exists, Value = exists ? map[key] : null };
        }

        private static void RestoreEntry(IDictionary map, object key, EntryState state)
        {
            if (map == null || state == null) return;
            if (state.Exists) map[key] = state.Value;
            else map.Remove(key);
        }

        private static bool EntryMatches(IDictionary map, object key, EntryState state)
        {
            return map != null && state != null && map.Contains(key) == state.Exists
                && (!state.Exists || ReferenceEquals(map[key], state.Value));
        }

        private static bool EntriesMatch(
            IDictionary map,
            IEnumerable<int> ids,
            IReadOnlyDictionary<int, EntryState> states)
        {
            foreach (int id in ids)
            {
                if (!EntryMatches(map, id, states[id])) return false;
            }
            return true;
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY kf1vn-activity " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
