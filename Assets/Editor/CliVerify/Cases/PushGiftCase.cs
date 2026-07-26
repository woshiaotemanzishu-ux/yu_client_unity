using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.PushGift;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>19102 单礼包详情快照：独立运行，不挂入总路由。</summary>
    public static class PushGiftCase
    {
        private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags NonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY pushgift EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunCore()
        {
            PushGiftController controller = PushGiftController.Instance;
            PushGiftModel model = PushGiftModel.Instance;
            FieldInfo intercept = typeof(PushGiftController).GetField("s_outboundIntercept", NonPublicStatic);
            var ambient = new AmbientState(controller, model, intercept);
            bool pass = false, restored = false, entered = false;
            try
            {
                // 先从注册表静默摘出宿主原有191图标；测试中的 DeleteIcon 因而不会发删除事件污染已打开的 MainUI。
                if (!ambient.DetachIconState())
                {
                    Debug.LogError("CLIVERIFY pushgift cannot isolate ambient icon/event state");
                    return 3;
                }
                entered = true;
                pass = true;
                controller.Init();
                model.Reset();
                MethodInfo on19101 = typeof(PushGiftController).GetMethod("On19101", NonPublicInstance);
                MethodInfo on19102 = typeof(PushGiftController).GetMethod("On19102", NonPublicInstance);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", NonPublicStatic)?.GetValue(null) as IDictionary;
                pass &= intercept != null && on19101 != null && on19102 != null && handlers != null
                    && ambient.CanVerify
                    && handlers.Contains(19101) && handlers.Contains(19102) && !handlers.Contains(19103) && !handlers.Contains(19104);

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestGiftDetail(ushort.MaxValue, 0);
                pass &= DetailFrame(frames, ushort.MaxValue, 0);

                frames.Clear();
                controller.RequestStartup();
                pass &= frames.Count == 2 && EmptyFrame(frames[0], 19104) && EmptyFrame(frames[1], 19101);

                model.SetGiftList(new List<PushGiftModel.GiftEntry>
                {
                    new PushGiftModel.GiftEntry(21, 22, uint.MaxValue)
                });
                pass &= model.GetEntranceOpenState();

                Invoke(on19102, controller, MultiPacket(), out int remaining);
                PushGiftModel.GiftDetail first = model.GetGiftDetail(ushort.MaxValue, 1);
                pass &= remaining == 0 && first != null && model.HasGiftDetail(ushort.MaxValue, 1) && model.GiftDetailCount == 1
                    && first.GiftName == "礼包中文" && first.EndTime == uint.MaxValue && first.Conditions == ""
                    && first.RewardList.Count == 2 && RewardIs(first.RewardList[0], ushort.MaxValue, "档位甲", byte.MaxValue, uint.MaxValue, "条件", "奖励")
                    && RewardIs(first.RewardList[1], ushort.MaxValue, "", 0, 0, "", "") && model.GetEntranceOpenState();

                Invoke(on19102, controller, SinglePacket(ushort.MaxValue, 1), out remaining);
                first = model.GetGiftDetail(ushort.MaxValue, 1);
                pass &= remaining == 0 && first != null && first.GiftName == "single" && first.EndTime == 7 && first.Conditions == "c"
                    && first.RewardList.Count == 1 && RewardIs(first.RewardList[0], 2, "one", 3, 4, "rc", "r")
                    && model.GetEntranceOpenState();

                Invoke(on19102, controller, SinglePacket(9, 10), out remaining);
                PushGiftModel.GiftDetail other = model.GetGiftDetail(9, 10);
                pass &= remaining == 0 && other != null && other.RewardList.Count == 1 && model.GiftDetailCount == 2
                    && first == model.GetGiftDetail(ushort.MaxValue, 1) && first.GiftName == "single" && model.GetEntranceOpenState();

                Invoke(on19102, controller, EmptyPacket(ushort.MaxValue, 1), out remaining);
                first = model.GetGiftDetail(ushort.MaxValue, 1);
                pass &= remaining == 0 && first != null && model.HasGiftDetail(ushort.MaxValue, 1) && first.GiftName == "" && first.EndTime == 0
                    && first.Conditions == "" && first.RewardList.Count == 0 && other == model.GetGiftDetail(9, 10)
                    && model.GetEntranceOpenState();

                frames.Clear();
                controller.RequestGiftDetail(9, 10); // 服务端无响应时本地绝不清理。
                pass &= DetailFrame(frames, 9, 10) && other == model.GetGiftDetail(9, 10)
                    && first == model.GetGiftDetail(ushort.MaxValue, 1) && model.GetEntranceOpenState();

                // 既有 19101 type4 仍完整读包、删除激活礼包，但绝不污染两个 19102 详情 key。
                Invoke(on19101, controller, ListPacket(4, 21, 22), out int listRemaining);
                pass &= listRemaining == 0 && model.IsGiftListEmpty() && model.GiftDetailCount == 2
                    && first == model.GetGiftDetail(ushort.MaxValue, 1) && other == model.GetGiftDetail(9, 10);

                // Dispose 必须从 19101 与 19102 都非空的状态出发，避免 Reset 假绿。
                model.SetGiftList(new List<PushGiftModel.GiftEntry>
                {
                    new PushGiftModel.GiftEntry(31, 32, uint.MaxValue)
                });
                pass &= model.GetEntranceOpenState() && model.GiftDetailCount == 2;

                controller.Dispose();
                pass &= !controller.IsInitialized && !handlers.Contains(19101) && !handlers.Contains(19102)
                    && model.GiftDetailCount == 0 && model.GetGiftDetail(9, 10) == null && model.IsGiftListEmpty()
                    && !HasRoleInfoSubscriber(controller);
            }
            finally
            {
                restored = !entered || ambient.Restore(controller, model, intercept);
                Debug.Log("CLIVERIFY pushgift restored=" + restored + " VERDICT pass=" + pass);
            }
            return pass && restored ? 0 : 3;
        }

        private static void Invoke(MethodInfo method, PushGiftController controller, byte[] bytes, out int remaining)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            remaining = reader.Remaining;
        }

        private static bool EmptyFrame(byte[] frame, int proto) => frame != null && frame.Length == 6
            && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
            && frame[4] == (byte)(proto >> 8) && frame[5] == (byte)proto;
        private static bool DetailFrame(List<byte[]> frames, int giftId, int subId) => frames.Count == 1 && frames[0].Length == 10
            && frames[0][0] == 0 && frames[0][1] == 10 && frames[0][2] == 3 && frames[0][3] == 232 && frames[0][4] == 74 && frames[0][5] == 158
            && frames[0][6] == (byte)(giftId >> 8) && frames[0][7] == (byte)giftId && frames[0][8] == (byte)(subId >> 8) && frames[0][9] == (byte)subId;
        private static bool RewardIs(PushGiftModel.RewardEntry x, int id, string name, byte count, uint time, string conditions, string rewards)
            => x != null && x.GradeId == id && x.GradeName == name && x.BuyCount == count && x.BuyTime == time && x.RewardsConditions == conditions && x.Rewards == rewards;

        private static byte[] MultiPacket() => new CliVerify.Pkt().H(65535).H(1).S("礼包中文").I(4294967295L).S("").H(2)
            .H(65535).S("档位甲").C(255).I(4294967295L).S("条件").S("奖励")
            .H(65535).S("").C(0).I(0).S("").S("").Bytes();
        private static byte[] SinglePacket(int giftId, int subId) => new CliVerify.Pkt().H(giftId).H(subId).S("single").I(7).S("c").H(1)
            .H(2).S("one").C(3).I(4).S("rc").S("r").Bytes();
        private static byte[] EmptyPacket(int giftId, int subId) => new CliVerify.Pkt().H(giftId).H(subId).S("").I(0).S("").H(0).Bytes();
        private static byte[] ListPacket(byte type, int giftId, int subId) => new CliVerify.Pkt().C(type).H(1)
            .H(giftId).H(subId).S("title").S("gift").I(uint.MaxValue).S("info").Bytes();

        private static bool HasRoleInfoSubscriber(PushGiftController controller)
        {
            IDictionary events = typeof(EventDispatcher).GetField("_handlers", NonPublicStatic)?.GetValue(null) as IDictionary;
            if (events == null || !events.Contains(GlobalEvent.EVT_ROLE_INFO_UPDATE)) return false;
            foreach (Delegate subscriber in (List<Delegate>)events[GlobalEvent.EVT_ROLE_INFO_UPDATE])
                if (ReferenceEquals(subscriber.Target, controller) && subscriber.Method.Name == "OnRoleInfoUpdate") return true;
            return false;
        }

        private sealed class AmbientState
        {
            private static readonly int[] Protocols = { 19101, 19102, 19103, 19104 };
            private readonly bool _initialized;
            private readonly object _intercept;
            private readonly Dictionary<int, object> _handlers = new Dictionary<int, object>();
            private readonly Dictionary<string, long> _ends;
            private readonly Dictionary<string, PushGiftModel.GiftDetail> _details;
            private readonly int _lastLevel;
            private readonly IDictionary _eventHandlers;
            private readonly bool _hadRoleEvent;
            private readonly List<Delegate> _roleSubscribers;
            private readonly IDictionary _icons;
            private readonly IDictionary _boxIcons;
            private readonly bool _hadIcon;
            private readonly bool _hadBoxIcon;
            private readonly object _icon;
            private readonly object _boxIcon;

            public bool CanVerify => _eventHandlers != null && _icons != null && _boxIcons != null;

            public bool DetachIconState()
            {
                if (!CanVerify) return false;
                _icons.Remove(PushGiftModel.ICON_TYPE);
                _boxIcons.Remove(PushGiftModel.ICON_TYPE);
                return true;
            }

            public AmbientState(PushGiftController controller, PushGiftModel model, FieldInfo intercept)
            {
                _initialized = controller.IsInitialized;
                _intercept = intercept == null ? null : intercept.GetValue(null);
                _ends = CloneDictionary<long>(model, "_giftEndTime");
                _details = CloneDictionary<PushGiftModel.GiftDetail>(model, "_giftDetails");
                _lastLevel = (int)typeof(PushGiftController).GetField("_lastLevel", NonPublicInstance).GetValue(controller);
                IDictionary net = typeof(NetManager).GetField("_handlers", NonPublicStatic)?.GetValue(null) as IDictionary;
                if (net != null) foreach (int proto in Protocols) if (net.Contains(proto)) _handlers[proto] = net[proto];

                _eventHandlers = typeof(EventDispatcher).GetField("_handlers", NonPublicStatic)?.GetValue(null) as IDictionary;
                _hadRoleEvent = _eventHandlers != null && _eventHandlers.Contains(GlobalEvent.EVT_ROLE_INFO_UPDATE);
                _roleSubscribers = _hadRoleEvent
                    ? new List<Delegate>((List<Delegate>)_eventHandlers[GlobalEvent.EVT_ROLE_INFO_UPDATE])
                    : new List<Delegate>();

                ActivityIconManager iconManager = ActivityIconManager.Instance;
                _icons = typeof(ActivityIconManager).GetField("_iconInfoByType", NonPublicInstance)?.GetValue(iconManager) as IDictionary;
                _boxIcons = typeof(ActivityIconManager).GetField("_iconBoxInfoByType", NonPublicInstance)?.GetValue(iconManager) as IDictionary;
                _hadIcon = _icons != null && _icons.Contains(PushGiftModel.ICON_TYPE);
                _hadBoxIcon = _boxIcons != null && _boxIcons.Contains(PushGiftModel.ICON_TYPE);
                _icon = _hadIcon ? _icons[PushGiftModel.ICON_TYPE] : null;
                _boxIcon = _hadBoxIcon ? _boxIcons[PushGiftModel.ICON_TYPE] : null;
            }

            public bool Restore(PushGiftController controller, PushGiftModel model, FieldInfo intercept)
            {
                try
                {
                    if (controller.IsInitialized) controller.Dispose();
                    model.Reset();
                    RestoreDictionary(model, "_giftEndTime", _ends);
                    RestoreDictionary(model, "_giftDetails", _details);
                    typeof(PushGiftController).GetField("_lastLevel", NonPublicInstance).SetValue(controller, _lastLevel);
                    if (_initialized) controller.Init();
                    IDictionary net = typeof(NetManager).GetField("_handlers", NonPublicStatic)?.GetValue(null) as IDictionary;
                    if (net == null) return false;
                    foreach (int proto in Protocols) { if (_handlers.TryGetValue(proto, out object handler)) net[proto] = handler; else net.Remove(proto); }
                    if (intercept != null) intercept.SetValue(null, _intercept);

                    _eventHandlers.Remove(GlobalEvent.EVT_ROLE_INFO_UPDATE);
                    if (_hadRoleEvent) _eventHandlers[GlobalEvent.EVT_ROLE_INFO_UPDATE] = new List<Delegate>(_roleSubscribers);
                    _icons.Remove(PushGiftModel.ICON_TYPE);
                    if (_hadIcon) _icons[PushGiftModel.ICON_TYPE] = _icon;
                    _boxIcons.Remove(PushGiftModel.ICON_TYPE);
                    if (_hadBoxIcon) _boxIcons[PushGiftModel.ICON_TYPE] = _boxIcon;

                    return controller.IsInitialized == _initialized && ModelMatches(model) && HandlersMatch(net)
                        && RoleEventMatches() && IconMatches(_icons, _hadIcon, _icon) && IconMatches(_boxIcons, _hadBoxIcon, _boxIcon)
                        && (intercept == null || ReferenceEquals(intercept.GetValue(null), _intercept));
                }
                catch (Exception e) { Debug.LogError("CLIVERIFY pushgift restore EXCEPTION " + e); return false; }
            }

            private bool HandlersMatch(IDictionary net)
            {
                foreach (int proto in Protocols)
                {
                    bool had = _handlers.TryGetValue(proto, out object expected);
                    if (net.Contains(proto) != had || (had && !ReferenceEquals(net[proto], expected))) return false;
                }
                return true;
            }

            private bool RoleEventMatches()
            {
                if (_eventHandlers.Contains(GlobalEvent.EVT_ROLE_INFO_UPDATE) != _hadRoleEvent) return false;
                if (!_hadRoleEvent) return true;
                var current = (List<Delegate>)_eventHandlers[GlobalEvent.EVT_ROLE_INFO_UPDATE];
                if (current.Count != _roleSubscribers.Count) return false;
                for (int i = 0; i < current.Count; i++)
                    if (!ReferenceEquals(current[i], _roleSubscribers[i])) return false;
                return true;
            }

            private static bool IconMatches(IDictionary icons, bool had, object expected)
                => icons.Contains(PushGiftModel.ICON_TYPE) == had
                    && (!had || ReferenceEquals(icons[PushGiftModel.ICON_TYPE], expected));

            private bool ModelMatches(PushGiftModel model)
            {
                Dictionary<string, long> ends = CloneDictionary<long>(model, "_giftEndTime");
                Dictionary<string, PushGiftModel.GiftDetail> details = CloneDictionary<PushGiftModel.GiftDetail>(model, "_giftDetails");
                if (ends.Count != _ends.Count || details.Count != _details.Count) return false;
                foreach (var pair in _ends) if (!ends.TryGetValue(pair.Key, out long value) || value != pair.Value) return false;
                foreach (var pair in _details)
                    if (!details.TryGetValue(pair.Key, out PushGiftModel.GiftDetail value) || !DetailSame(pair.Value, value)) return false;
                return true;
            }

            private static bool DetailSame(PushGiftModel.GiftDetail a, PushGiftModel.GiftDetail b)
            {
                if (a == null || b == null || a.GiftId != b.GiftId || a.SubId != b.SubId || a.GiftName != b.GiftName
                    || a.EndTime != b.EndTime || a.Conditions != b.Conditions || a.RewardList.Count != b.RewardList.Count) return false;
                for (int i = 0; i < a.RewardList.Count; i++)
                    if (!RewardIs(b.RewardList[i], a.RewardList[i].GradeId, a.RewardList[i].GradeName, a.RewardList[i].BuyCount,
                        a.RewardList[i].BuyTime, a.RewardList[i].RewardsConditions, a.RewardList[i].Rewards)) return false;
                return true;
            }

            private static Dictionary<string, T> CloneDictionary<T>(PushGiftModel model, string name)
            {
                var source = (Dictionary<string, T>)typeof(PushGiftModel).GetField(name, NonPublicInstance).GetValue(model);
                return new Dictionary<string, T>(source);
            }
            private static void RestoreDictionary<T>(PushGiftModel model, string name, Dictionary<string, T> values)
            {
                var target = (Dictionary<string, T>)typeof(PushGiftModel).GetField(name, NonPublicInstance).GetValue(model);
                target.Clear(); foreach (var pair in values) target[pair.Key] = pair.Value;
            }
        }
    }
}
