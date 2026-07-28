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
    /// <summary>61066 众生之门怪物进度专项：纯 S2C、原始整体覆盖、既有切片隔离与 ambient 恢复。</summary>
    public static class SentientActMonsterProgressCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags ALL = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;

        private sealed class ExistingAmbient
        {
            private readonly bool _hasInfo;
            private readonly byte _state;
            private readonly uint _endTime;
            private readonly uint _mod;
            private readonly uint _groupId;
            private readonly uint _nextStartTime;
            private readonly long _avgLevel;
            private readonly IReadOnlyList<SentientActModel.ServerEntry> _servers;
            private readonly bool _hasPortals;
            private readonly IReadOnlyList<SentientActModel.PortalEntry> _portals;
            private readonly bool _hasCounts;
            private readonly uint _assistNum;
            private readonly uint _enterNum;

            public ExistingAmbient(SentientActModel model)
            {
                _hasInfo = model.HasInfo;
                _state = model.State;
                _endTime = model.EndTime;
                _mod = model.Mod;
                _groupId = model.GroupId;
                _nextStartTime = model.NextStartTime;
                _avgLevel = model.AvgLevel;
                _servers = model.Servers;
                _hasPortals = model.HasPortals;
                _portals = model.Portals;
                _hasCounts = model.HasCounts;
                _assistNum = model.AssistNum;
                _enterNum = model.EnterNum;
            }

            public bool Matches(SentientActModel model)
            {
                return model.HasInfo == _hasInfo && model.State == _state
                    && model.EndTime == _endTime && model.Mod == _mod
                    && model.GroupId == _groupId && model.NextStartTime == _nextStartTime
                    && model.AvgLevel == _avgLevel && ReferenceEquals(model.Servers, _servers)
                    && model.HasPortals == _hasPortals && ReferenceEquals(model.Portals, _portals)
                    && model.HasCounts == _hasCounts && model.AssistNum == _assistNum
                    && model.EnterNum == _enterNum;
            }
        }

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY sentient-act-monster-progress EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            SentientActController controller = SentientActController.Instance;
            SentientActModel model = SentientActModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasProgress = model.HasMonsterProgress;
            SentientActModel.MonsterProgressSnapshot oldProgress = model.LastMonsterProgress;
            var existing = new ExistingAmbient(model);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null
                && handlers.Contains(Proto.DUNGEON_SENTIENT_MONSTER_PROGRESS);
            object oldHandler = oldHandlerExists
                ? handlers[Proto.DUNGEON_SENTIENT_MONSTER_PROGRESS]
                : null;
            bool pass = false;
            bool restored = false;

            try
            {
                RestoreProperty(model, "HasMonsterProgress", false);
                RestoreProperty(model, "LastMonsterProgress", null);

                MethodInfo on61066 = typeof(SentientActController).GetMethod("On61066", IF);
                MethodInfo request = typeof(SentientActController).GetMethod(
                    "RequestMonsterProgress", ALL);
                FieldInfo intercept = typeof(SentientActController).GetField(
                    "s_monsterProgressOutboundIntercept", ALL);
                pass = Proto.DUNGEON_SENTIENT_MONSTER_PROGRESS == 61066 && on61066 != null
                    && request == null && intercept == null && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "s2c-only seams/registration/no-sender", pass);
                if (on61066 == null) throw new MissingMethodException("On61066 seam missing");

                Check(ref pass, "no response keeps clear/existing slices isolated",
                    !model.HasMonsterProgress && model.LastMonsterProgress == null
                    && existing.Matches(model));

                Check(ref pass, "12B max whole replace/read-to-end",
                    Feed(on61066, controller, uint.MaxValue, uint.MaxValue, uint.MaxValue)
                    && Progress(model, uint.MaxValue, uint.MaxValue, uint.MaxValue)
                    && existing.Matches(model));
                SentientActModel.MonsterProgressSnapshot max = model.LastMonsterProgress;

                Check(ref pass, "12B dead-over-mon preserved/whole replace/read-to-end",
                    Feed(on61066, controller, 7, 9, 3)
                    && Progress(model, 7, 9, 3)
                    && !ReferenceEquals(model.LastMonsterProgress, max)
                    && existing.Matches(model));
                SentientActModel.MonsterProgressSnapshot deadOverMon = model.LastMonsterProgress;

                Check(ref pass, "12B zero preserved/whole replace/read-to-end",
                    Feed(on61066, controller, 0, 0, 0)
                    && Progress(model, 0, 0, 0)
                    && !ReferenceEquals(model.LastMonsterProgress, deadOverMon)
                    && existing.Matches(model));

                Check(ref pass, "handler/init ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY sentient-act-monster-progress VERDICT pass=" + pass);
            }
            finally
            {
                RestoreProperty(model, "HasMonsterProgress", oldHasProgress);
                RestoreProperty(model, "LastMonsterProgress", oldProgress);
                restored = controller.IsInitialized == oldInitialized
                    && model.HasMonsterProgress == oldHasProgress
                    && ReferenceEquals(model.LastMonsterProgress, oldProgress)
                    && existing.Matches(model)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler);
                Debug.Log("CLIVERIFY sentient-act-monster-progress restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, SentientActController controller,
            uint waveNum, uint deadMonNum, uint monNum)
        {
            byte[] bytes = new CliVerify.Pkt().I(waveNum).I(deadMonNum).I(monNum).Bytes();
            if (bytes.Length != 12) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Progress(SentientActModel model,
            uint waveNum, uint deadMonNum, uint monNum)
        {
            SentientActModel.MonsterProgressSnapshot snapshot = model.LastMonsterProgress;
            return model.HasMonsterProgress && snapshot != null
                && snapshot.WaveNum == waveNum && snapshot.DeadMonNum == deadMonNum
                && snapshot.MonNum == monNum;
        }

        private static void RestoreProperty(SentientActModel model, string propertyName, object value)
        {
            typeof(SentientActModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                ?.SetValue(model, value);
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null
                && handlers.Contains(Proto.DUNGEON_SENTIENT_MONSTER_PROGRESS) == existed
                && (!existed
                    || ReferenceEquals(handlers[Proto.DUNGEON_SENTIENT_MONSTER_PROGRESS], value));
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY sentient-act-monster-progress " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
