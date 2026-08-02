using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.SentientAct;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>24106 门户销毁推送：只存原始通知并精确重查 24102，不补丁式改写权威门户列表。</summary>
    public static class SentientActPortalRemovedCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY sentient-act-portal-removed EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            SentientActController controller = SentientActController.Instance;
            SentientActModel model = SentientActModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var ambient = new Ambient(model);
            FieldInfo interceptField = typeof(SentientActController).GetField("s_outboundIntercept", SF);
            object oldIntercept = interceptField?.GetValue(null);
            bool pass = false;
            int result = 3;

            try
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                controller.Init();

                MethodInfo on24102 = typeof(SentientActController).GetMethod("On24102", IF);
                MethodInfo on24106 = typeof(SentientActController).GetMethod("On24106", IF);
                MethodInfo forbiddenRequest = typeof(SentientActController).GetMethod(
                    "RequestPortalRemoved", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                pass = Proto.SENTIENT_ACT_PORTAL_REMOVED == 24106
                    && on24102 != null && on24106 != null && forbiddenRequest == null
                    && interceptField != null && handlers != null && handlers.Contains(24106);
                Check(ref pass, "s2c-only seam/register/no same-number sender", pass);
                if (!pass) throw new InvalidOperationException("SentientAct 24106 verification seams are incomplete.");

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                NetReader early = Feed(on24106, controller, long.MinValue);
                SentientActModel.PortalRemovedSnapshot earlyRaw = model.LastPortalRemoved;
                Check(ref pass, "early min raw-only and one authoritative refresh", early.Remaining == 0
                    && model.HasPortalRemoved && earlyRaw != null
                    && earlyRaw.PortalId == long.MinValue
                    && !model.HasPortals && model.Portals.Count == 0
                    && Frames(frames, 24102));
                frames.Clear();

                model.ReplaceInfo(9, 10, 11, 12, 13,
                    new List<SentientActModel.ServerEntry>
                    {
                        new SentientActModel.ServerEntry(-1, 2, "keep", 3)
                    }, 14);
                model.ReplacePortals(new List<SentientActModel.PortalEntry>
                {
                    new SentientActModel.PortalEntry(7, 8, 9),
                    new SentientActModel.PortalEntry(7, 10, 11),
                    new SentientActModel.PortalEntry(-1, uint.MaxValue, 0)
                });
                model.ReplaceCounts(15, 16);
                IReadOnlyList<SentientActModel.PortalEntry> authoritative = model.Portals;

                NetReader duplicate = Feed(on24106, controller, 7);
                SentientActModel.PortalRemovedSnapshot duplicateRaw = model.LastPortalRemoved;
                Check(ref pass, "loaded duplicate id keeps authority and refreshes once", duplicate.Remaining == 0
                    && duplicateRaw != null && duplicateRaw.PortalId == 7
                    && !ReferenceEquals(duplicateRaw, earlyRaw) && earlyRaw.PortalId == long.MinValue
                    && ReferenceEquals(model.Portals, authoritative) && model.Portals.Count == 3
                    && model.Portals[0].PortalId == 7 && model.Portals[1].PortalId == 7
                    && model.State == 9 && model.Servers.Count == 1
                    && model.AssistNum == 15 && model.EnterNum == 16
                    && Frames(frames, 24102));
                frames.Clear();

                NetReader max = Feed(on24106, controller, -1);
                SentientActModel.PortalRemovedSnapshot maxRaw = model.LastPortalRemoved;
                Check(ref pass, "u64 max replaces raw but not full snapshot", max.Remaining == 0
                    && maxRaw != null && maxRaw.PortalId == -1
                    && !ReferenceEquals(maxRaw, duplicateRaw) && duplicateRaw.PortalId == 7
                    && ReferenceEquals(model.Portals, authoritative)
                    && model.Portals[2].PortalId == -1
                    && Frames(frames, 24102));
                frames.Clear();

                byte[] replacement = new CliVerify.Pkt().H(1).L(99).I(100).I(101).Bytes();
                var fullReader = new NetReader(replacement, 0, replacement.Length);
                on24102.Invoke(controller, new object[] { fullReader });
                Check(ref pass, "later 24102 alone replaces authority and preserves raw", fullReader.Remaining == 0
                    && model.HasPortals && model.Portals.Count == 1
                    && model.Portals[0].PortalId == 99 && model.Portals[0].X == 100
                    && model.Portals[0].Y == 101
                    && model.HasPortalRemoved && ReferenceEquals(model.LastPortalRemoved, maxRaw)
                    && frames.Count == 0);

                controller.Dispose();
                Check(ref pass, "dispose clears raw/full and unregisters", !controller.IsInitialized
                    && !model.HasPortalRemoved && model.LastPortalRemoved == null
                    && !model.HasPortals && model.Portals.Count == 0
                    && !handlers.Contains(24106) && frames.Count == 0);

                Debug.Log("CLIVERIFY sentient-act-portal-removed VERDICT pass=" + pass);
                result = pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                ambient.Restore(model);
                if (wasInitialized) controller.Init();
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);

                var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
                bool restored = controller.IsInitialized == wasInitialized
                    && handlers != null && handlers.Contains(24106) == wasInitialized
                    && ambient.Matches(model)
                    && ReferenceEquals(interceptField?.GetValue(null), oldIntercept);
                Debug.Log("CLIVERIFY sentient-act-portal-removed restored=" + restored);
                if (!restored) result = 3;
            }

            return result;
        }

        private static NetReader Feed(MethodInfo handler, SentientActController controller, long portalId)
        {
            byte[] bytes = new CliVerify.Pkt().L(portalId).Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader;
        }

        private static void Check(ref bool pass, string tag, bool ok)
        {
            Debug.Log("CLIVERIFY sentient-act-portal-removed " + tag + " ok=" + ok);
            pass &= ok;
        }

        private static bool Frames(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] frame = frames[i];
                if (frame == null || frame.Length != 6
                    || frame[0] != 0 || frame[1] != 6
                    || frame[2] != 3 || frame[3] != 232
                    || frame[4] != (byte)(ids[i] >> 8)
                    || frame[5] != (byte)ids[i]) return false;
            }
            return true;
        }

        private sealed class Ambient
        {
            private readonly bool _hasInfo;
            private readonly byte _state;
            private readonly uint _end;
            private readonly uint _mod;
            private readonly uint _group;
            private readonly uint _next;
            private readonly long _avg;
            private readonly List<SentientActModel.ServerEntry> _servers;
            private readonly bool _hasPortals;
            private readonly List<SentientActModel.PortalEntry> _portals;
            private readonly bool _hasRemoved;
            private readonly long _removedId;
            private readonly bool _hasCounts;
            private readonly uint _assist;
            private readonly uint _enter;
            private readonly bool _hasMonsterProgress;
            private readonly uint _wave;
            private readonly uint _dead;
            private readonly uint _mon;

            public Ambient(SentientActModel model)
            {
                _hasInfo = model.HasInfo;
                _state = model.State;
                _end = model.EndTime;
                _mod = model.Mod;
                _group = model.GroupId;
                _next = model.NextStartTime;
                _avg = model.AvgLevel;
                _servers = new List<SentientActModel.ServerEntry>(model.Servers);
                _hasPortals = model.HasPortals;
                _portals = new List<SentientActModel.PortalEntry>(model.Portals);
                _hasRemoved = model.HasPortalRemoved;
                _removedId = model.LastPortalRemoved?.PortalId ?? 0;
                _hasCounts = model.HasCounts;
                _assist = model.AssistNum;
                _enter = model.EnterNum;
                _hasMonsterProgress = model.HasMonsterProgress;
                _wave = model.LastMonsterProgress?.WaveNum ?? 0;
                _dead = model.LastMonsterProgress?.DeadMonNum ?? 0;
                _mon = model.LastMonsterProgress?.MonNum ?? 0;
            }

            public void Restore(SentientActModel model)
            {
                model.Reset();
                if (_hasInfo) model.ReplaceInfo(_state, _end, _mod, _group, _next, _servers, _avg);
                if (_hasPortals) model.ReplacePortals(_portals);
                if (_hasRemoved) model.ReplacePortalRemoved(_removedId);
                if (_hasCounts) model.ReplaceCounts(_assist, _enter);
                if (_hasMonsterProgress) model.ReplaceMonsterProgress(_wave, _dead, _mon);
            }

            public bool Matches(SentientActModel model)
            {
                return model.HasInfo == _hasInfo && model.State == _state
                    && model.EndTime == _end && model.Mod == _mod
                    && model.GroupId == _group && model.NextStartTime == _next
                    && model.AvgLevel == _avg && SameServers(model.Servers, _servers)
                    && model.HasPortals == _hasPortals && SamePortals(model.Portals, _portals)
                    && model.HasPortalRemoved == _hasRemoved
                    && (model.LastPortalRemoved?.PortalId ?? 0) == _removedId
                    && model.HasCounts == _hasCounts && model.AssistNum == _assist
                    && model.EnterNum == _enter
                    && model.HasMonsterProgress == _hasMonsterProgress
                    && (model.LastMonsterProgress?.WaveNum ?? 0) == _wave
                    && (model.LastMonsterProgress?.DeadMonNum ?? 0) == _dead
                    && (model.LastMonsterProgress?.MonNum ?? 0) == _mon;
            }

            private static bool SameServers(IReadOnlyList<SentientActModel.ServerEntry> left,
                IReadOnlyList<SentientActModel.ServerEntry> right)
            {
                if (left.Count != right.Count) return false;
                for (int i = 0; i < left.Count; i++)
                {
                    if (left[i].ServerId != right[i].ServerId
                        || left[i].ServerNum != right[i].ServerNum
                        || left[i].Name != right[i].Name
                        || left[i].WorldLevel != right[i].WorldLevel) return false;
                }
                return true;
            }

            private static bool SamePortals(IReadOnlyList<SentientActModel.PortalEntry> left,
                IReadOnlyList<SentientActModel.PortalEntry> right)
            {
                if (left.Count != right.Count) return false;
                for (int i = 0; i < left.Count; i++)
                {
                    if (left[i].PortalId != right[i].PortalId
                        || left[i].X != right[i].X || left[i].Y != right[i].Y) return false;
                }
                return true;
            }
        }
    }
}
