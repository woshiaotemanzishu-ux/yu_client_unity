using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.TopVip;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class TopVipCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly int[] RegisteredIds = { 45101, 45102, 45104, 45109, 45110, 45111, 45112 };

        private sealed class HandlerState { public bool Exists; public object Value; }

        private sealed class IconState
        {
            public string IconType;
            public long Time;
            public string IconTxt;
            public string IconImg;
            public bool RedDot;
            public int BadgeCount;
            public ActivityIconManager.IconPresentation Presentation;
            public MainUIConfigs.FunctionIconCfg Data;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY topvip EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            TopVipController controller = TopVipController.Instance;
            TopVipModel model = TopVipModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            TopVipModel.InfoSnapshot oldInfo = model.Info;
            TopVipModel.SkillTaskSnapshot oldSkill = model.SkillTasks;
            TopVipModel.TaskListSnapshot oldCurrency = model.CurrencyTasks;
            TopVipModel.TaskListSnapshot oldSkillUpdate = model.LastSkillTaskUpdate;
            TopVipModel.TaskListSnapshot oldCurrencyUpdate = model.LastCurrencyTaskUpdate;
            bool oldHasFree = model.HasFreeProtectUpdate;
            byte oldFree = model.FreeProtectUpdate;
            FieldInfo lastLevelField = typeof(TopVipController).GetField("_lastLevel", F);
            FieldInfo lastVipField = typeof(TopVipController).GetField("_lastVipFlag", F);
            object oldLastLevel = lastLevelField?.GetValue(controller);
            object oldLastVip = lastVipField?.GetValue(controller);
            FieldInfo interceptor = typeof(TopVipController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            foreach (int id in RegisteredIds) SaveHandler(handlers, savedHandlers, id);

            ActivityIconManager iconManager = ActivityIconManager.Instance;
            IDictionary iconMap = GetDictionary(iconManager, "_iconInfoByType");
            IDictionary boxMap = GetDictionary(iconManager, "_iconBoxInfoByType");
            IDictionary redMap = GetDictionary(iconManager, "_redDotByType");
            IDictionary badgeMap = GetDictionary(iconManager, "_badgeByType");
            Dictionary<object, object> oldIconMap = CopyDictionary(iconMap);
            Dictionary<object, object> oldBoxMap = CopyDictionary(boxMap);
            Dictionary<object, object> oldRedMap = CopyDictionary(redMap);
            Dictionary<object, object> oldBadgeMap = CopyDictionary(badgeMap);
            Dictionary<ActivityIconManager.IconInfo, IconState> oldIconStates = CaptureIconStates(oldIconMap, oldBoxMap);

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on45101 = typeof(TopVipController).GetMethod("On45101", F);
                MethodInfo on45102 = typeof(TopVipController).GetMethod("On45102", F);
                MethodInfo on45104 = typeof(TopVipController).GetMethod("On45104", F);
                MethodInfo on45109 = typeof(TopVipController).GetMethod("On45109", F);
                MethodInfo on45110 = typeof(TopVipController).GetMethod("On45110", F);
                MethodInfo on45111 = typeof(TopVipController).GetMethod("On45111", F);
                MethodInfo on45112 = typeof(TopVipController).GetMethod("On45112", F);

                bool a = interceptor != null && handlers != null && on45101 != null && on45102 != null
                    && on45104 != null && on45109 != null && on45110 != null && on45111 != null && on45112 != null;
                foreach (int id in RegisteredIds) a &= handlers != null && handlers.Contains(id);
                int[] deferred = { 45103, 45105, 45106, 45107, 45108 };
                foreach (int id in deferred) a &= handlers != null && !handlers.Contains(id);

                bool b = false;
                bool c = false;
                bool d = false;
                bool e = false;
                bool f = false;
                bool g = false;
                bool h = false;
                bool i = false;
                var frames = new List<byte[]>();
                if (a)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                    controller.RequestStartup();
                    b = FramesAre(frames, 45101, 45102, 45104);
                    frames.Clear();
                    controller.RequestInfo();
                    controller.RequestSkillTasks();
                    controller.RequestCurrencyTasks();
                    b &= FramesAre(frames, 45101, 45102, 45104);
                    frames.Clear();

                    byte[] infoPacket = new CliVerify.Pkt().C(255).I(uint.MaxValue).H(3)
                        .C(7).S("").I(0)
                        .C(7).S("权益甲").I(uint.MaxValue)
                        .C(8).S("{}单独").I(9)
                        .C(255).I(uint.MaxValue).C(255).Bytes();
                    c = Invoke(on45101, controller, infoPacket)
                        && model.HasInfo && model.Info.SupvipType == byte.MaxValue
                        && model.Info.SupvipTime == uint.MaxValue && model.Info.Rights.Count == 3
                        && model.Info.Rights[0].RightType == 7 && model.Info.Rights[0].Data == "" && model.Info.Rights[0].UpdateTime == 0
                        && model.Info.Rights[1].RightType == 7 && model.Info.Rights[1].Data == "权益甲"
                        && model.Info.Rights[1].UpdateTime == uint.MaxValue
                        && model.Info.Rights[2].RightType == 8 && model.Info.Rights[2].Data == "{}单独"
                        && model.Info.ChargeDay == byte.MaxValue && model.Info.TodayGold == uint.MaxValue
                        && model.Info.IsFreeProtect == byte.MaxValue && frames.Count == 0;
                    TopVipModel.InfoSnapshot fullInfo = model.Info;

                    byte[] skillPacket = new CliVerify.Pkt().C(255).C(254).H(3)
                        .H(9).C(0).C(255).S("")
                        .H(9).C(2).C(3).S("技能任务")
                        .H(65535).C(255).C(0).S("末项").Bytes();
                    d = Invoke(on45102, controller, skillPacket)
                        && model.HasSkillTasks && model.SkillTasks.Stage == 255 && model.SkillTasks.SubStage == 254
                        && TasksAre(model.SkillTasks.Tasks, 9, 9, 65535)
                        && model.SkillTasks.Tasks[0].Content == "" && model.SkillTasks.Tasks[0].IsCommit == 255
                        && model.SkillTasks.Tasks[1].Content == "技能任务" && model.SkillTasks.Tasks[1].IsFinish == 2
                        && model.SkillTasks.Tasks[2].Content == "末项";
                    TopVipModel.SkillTaskSnapshot fullSkill = model.SkillTasks;

                    byte[] currencyPacket = new CliVerify.Pkt().H(2)
                        .H(4).C(1).C(0).S("至尊币")
                        .H(4).C(255).C(255).S("").Bytes();
                    e = Invoke(on45104, controller, currencyPacket)
                        && model.HasCurrencyTasks && TasksAre(model.CurrencyTasks.Tasks, 4, 4)
                        && model.CurrencyTasks.Tasks[0].Content == "至尊币"
                        && model.CurrencyTasks.Tasks[1].Content == "";
                    TopVipModel.TaskListSnapshot fullCurrency = model.CurrencyTasks;

                    byte[] skillDelta = new CliVerify.Pkt().H(2)
                        .H(9).C(1).C(1).S("变化")
                        .H(9).C(0).C(0).S("").Bytes();
                    f = Invoke(on45110, controller, skillDelta)
                        && model.HasSkillTaskUpdate && TasksAre(model.LastSkillTaskUpdate.Tasks, 9, 9)
                        && model.LastSkillTaskUpdate.Tasks[0].Content == "变化"
                        && ReferenceEquals(model.SkillTasks, fullSkill) && ReferenceEquals(model.Info, fullInfo)
                        && ReferenceEquals(model.CurrencyTasks, fullCurrency)
                        && FramesAre(frames, 45102);
                    frames.Clear();
                    f &= Invoke(on45110, controller, new CliVerify.Pkt().H(0).Bytes())
                        && model.HasSkillTaskUpdate && model.LastSkillTaskUpdate.Tasks.Count == 0
                        && ReferenceEquals(model.SkillTasks, fullSkill) && FramesAre(frames, 45102);
                    frames.Clear();

                    byte[] currencyDelta = new CliVerify.Pkt().H(1).H(4).C(3).C(2).S("币变化").Bytes();
                    g = Invoke(on45111, controller, currencyDelta)
                        && model.HasCurrencyTaskUpdate && TasksAre(model.LastCurrencyTaskUpdate.Tasks, 4)
                        && model.LastCurrencyTaskUpdate.Tasks[0].Content == "币变化"
                        && ReferenceEquals(model.SkillTasks, fullSkill) && ReferenceEquals(model.CurrencyTasks, fullCurrency)
                        && FramesAre(frames, 45104);
                    frames.Clear();

                    h = Invoke(on45109, controller, Array.Empty<byte>())
                        && ReferenceEquals(model.Info, fullInfo) && ReferenceEquals(model.SkillTasks, fullSkill)
                        && ReferenceEquals(model.CurrencyTasks, fullCurrency) && FramesAre(frames, 45101);
                    frames.Clear();

                    i = Invoke(on45112, controller, new CliVerify.Pkt().C(255).Bytes())
                        && model.HasFreeProtectUpdate && model.FreeProtectUpdate == byte.MaxValue
                        && model.Info.IsFreeProtect == byte.MaxValue && frames.Count == 0;
                    i &= Invoke(on45112, controller, new CliVerify.Pkt().C(0).Bytes())
                        && model.HasFreeProtectUpdate && model.FreeProtectUpdate == 0
                        && model.Info.IsFreeProtect == byte.MaxValue && frames.Count == 0;
                    i &= Invoke(on45101, controller, new CliVerify.Pkt().C(0).I(0).H(0).C(0).I(0).C(1).Bytes())
                        && model.HasInfo && model.Info.Rights.Count == 0 && model.Info.IsFreeProtect == 1
                        && model.HasFreeProtectUpdate && model.FreeProtectUpdate == 0
                        && model.HasSkillTaskUpdate && model.LastSkillTaskUpdate.Tasks.Count == 0
                        && model.HasCurrencyTaskUpdate && model.LastCurrencyTaskUpdate.Tasks.Count == 1;
                    i &= Invoke(on45102, controller, new CliVerify.Pkt().C(0).C(0).H(0).Bytes())
                        && model.HasSkillTasks && model.SkillTasks.Tasks.Count == 0
                        && model.HasCurrencyTasks && model.CurrencyTasks.Tasks.Count == 2
                        && Invoke(on45104, controller, new CliVerify.Pkt().H(0).Bytes())
                        && model.HasCurrencyTasks && model.CurrencyTasks.Tasks.Count == 0
                        && model.HasSkillTasks && model.SkillTasks.Tasks.Count == 0;

                    controller.Dispose();
                    i &= !controller.IsInitialized && !model.HasInfo && !model.HasSkillTasks && !model.HasCurrencyTasks
                        && !model.HasSkillTaskUpdate && !model.HasCurrencyTaskUpdate
                        && !model.HasFreeProtectUpdate && model.FreeProtectUpdate == 0
                        && Equals(lastLevelField?.GetValue(controller), -1)
                        && Equals(lastVipField?.GetValue(controller), -1);
                    foreach (int id in RegisteredIds) i &= !handlers.Contains(id);
                }

                pass = a && b && c && d && e && f && g && h && i;
                Debug.Log($"CLIVERIFY topvip VERDICT A={a} B={b} C={c} D={d} E={e} F={f} G={g} H={h} I={i} pass={pass}");
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModelProperty(model, "Info", oldInfo);
                RestoreModelProperty(model, "SkillTasks", oldSkill);
                RestoreModelProperty(model, "CurrencyTasks", oldCurrency);
                RestoreModelProperty(model, "LastSkillTaskUpdate", oldSkillUpdate);
                RestoreModelProperty(model, "LastCurrencyTaskUpdate", oldCurrencyUpdate);
                RestoreModelProperty(model, "HasFreeProtectUpdate", oldHasFree);
                RestoreModelProperty(model, "FreeProtectUpdate", oldFree);
                RestoreIconStates(oldIconStates);
                RestoreDictionary(iconMap, oldIconMap);
                RestoreDictionary(boxMap, oldBoxMap);
                RestoreDictionary(redMap, oldRedMap);
                RestoreDictionary(badgeMap, oldBadgeMap);
                if (wasInitialized) controller.Init();
                if (lastLevelField != null) lastLevelField.SetValue(controller, oldLastLevel);
                if (lastVipField != null) lastVipField.SetValue(controller, oldLastVip);
                foreach (int id in RegisteredIds) RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = controller.IsInitialized == wasInitialized
                    && ReferenceEquals(model.Info, oldInfo) && ReferenceEquals(model.SkillTasks, oldSkill)
                    && ReferenceEquals(model.CurrencyTasks, oldCurrency)
                    && ReferenceEquals(model.LastSkillTaskUpdate, oldSkillUpdate)
                    && ReferenceEquals(model.LastCurrencyTaskUpdate, oldCurrencyUpdate)
                    && model.HasFreeProtectUpdate == oldHasFree && model.FreeProtectUpdate == oldFree
                    && DictionaryMatches(iconMap, oldIconMap) && DictionaryMatches(boxMap, oldBoxMap)
                    && DictionaryMatches(redMap, oldRedMap) && DictionaryMatches(badgeMap, oldBadgeMap)
                    && IconStatesMatch(oldIconStates)
                    && (lastLevelField == null || Equals(lastLevelField.GetValue(controller), oldLastLevel))
                    && (lastVipField == null || Equals(lastVipField.GetValue(controller), oldLastVip))
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                foreach (int id in RegisteredIds) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY topvip restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool Invoke(MethodInfo handler, TopVipController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool TasksAre(IReadOnlyList<TopVipModel.TaskEntry> tasks, params int[] ids)
        {
            if (tasks == null || tasks.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
                if (tasks[i].TaskId != ids[i]) return false;
            return true;
        }

        private static bool FramesAre(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames == null || frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
                if (!BytesEqual(frames[i], new CliVerify.Pkt().H(6).H(1000).H(ids[i]).Bytes())) return false;
            return true;
        }

        private static bool BytesEqual(IReadOnlyList<byte> actual, IReadOnlyList<byte> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> saved, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            saved[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState saved, int id)
        {
            if (handlers == null) return;
            if (saved.Exists) handlers[id] = saved.Value;
            else handlers.Remove(id);
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState saved, int id)
        {
            return handlers != null && handlers.Contains(id) == saved.Exists
                && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));
        }

        private static void RestoreModelProperty(TopVipModel model, string name, object value)
        {
            typeof(TopVipModel).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static IDictionary GetDictionary(ActivityIconManager manager, string name)
        {
            return typeof(ActivityIconManager).GetField(name, F)?.GetValue(manager) as IDictionary;
        }

        private static Dictionary<object, object> CopyDictionary(IDictionary source)
        {
            var copy = new Dictionary<object, object>();
            if (source == null) return copy;
            foreach (DictionaryEntry pair in source) copy[pair.Key] = pair.Value;
            return copy;
        }

        private static void RestoreDictionary(IDictionary target, IReadOnlyDictionary<object, object> source)
        {
            if (target == null) return;
            target.Clear();
            foreach (KeyValuePair<object, object> pair in source) target[pair.Key] = pair.Value;
        }

        private static bool DictionaryMatches(IDictionary actual, IReadOnlyDictionary<object, object> expected)
        {
            if (actual == null || actual.Count != expected.Count) return false;
            foreach (KeyValuePair<object, object> pair in expected)
                if (!actual.Contains(pair.Key) || !Equals(actual[pair.Key], pair.Value)) return false;
            return true;
        }

        private static Dictionary<ActivityIconManager.IconInfo, IconState> CaptureIconStates(
            IReadOnlyDictionary<object, object> first,
            IReadOnlyDictionary<object, object> second)
        {
            var result = new Dictionary<ActivityIconManager.IconInfo, IconState>();
            Capture(first, result);
            Capture(second, result);
            return result;
        }

        private static void Capture(
            IReadOnlyDictionary<object, object> source,
            IDictionary<ActivityIconManager.IconInfo, IconState> result)
        {
            foreach (object value in source.Values)
            {
                var info = value as ActivityIconManager.IconInfo;
                if (info == null || result.ContainsKey(info)) continue;
                result[info] = new IconState
                {
                    IconType = info.IconType,
                    Time = info.Time,
                    IconTxt = info.IconTxt,
                    IconImg = info.IconImg,
                    RedDot = info.RedDot,
                    BadgeCount = info.BadgeCount,
                    Presentation = info.Presentation,
                    Data = info.Data,
                };
            }
        }

        private static void RestoreIconStates(IReadOnlyDictionary<ActivityIconManager.IconInfo, IconState> states)
        {
            foreach (KeyValuePair<ActivityIconManager.IconInfo, IconState> pair in states)
            {
                ActivityIconManager.IconInfo info = pair.Key;
                IconState state = pair.Value;
                info.IconType = state.IconType;
                info.Time = state.Time;
                info.IconTxt = state.IconTxt;
                info.IconImg = state.IconImg;
                info.RedDot = state.RedDot;
                info.BadgeCount = state.BadgeCount;
                info.Presentation = state.Presentation;
                info.Data = state.Data;
            }
        }

        private static bool IconStatesMatch(IReadOnlyDictionary<ActivityIconManager.IconInfo, IconState> states)
        {
            foreach (KeyValuePair<ActivityIconManager.IconInfo, IconState> pair in states)
            {
                ActivityIconManager.IconInfo info = pair.Key;
                IconState state = pair.Value;
                if (info.IconType != state.IconType || info.Time != state.Time || info.IconTxt != state.IconTxt
                    || info.IconImg != state.IconImg || info.RedDot != state.RedDot || info.BadgeCount != state.BadgeCount
                    || info.Presentation != state.Presentation || !ReferenceEquals(info.Data, state.Data)) return false;
            }
            return true;
        }
    }
}
