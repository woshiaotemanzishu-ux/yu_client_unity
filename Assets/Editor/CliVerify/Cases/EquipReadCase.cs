using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Equip;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>R516：15217/15219/15220/15223/15262只读切片、精确wire、隔离与环境恢复。</summary>
    public static class EquipReadCase
    {
        private const BindingFlags I = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags S = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly int[] Protocols = { 15217, 15219, 15220, 15223, 15262 };

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunCore()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY equip-read EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunCore()
        {
            EquipReadController controller = EquipReadController.Instance;
            EquipReadModel model = EquipReadModel.Instance;
            FieldInfo intercept = typeof(EquipReadController).GetField("s_outboundIntercept", S);
            var ambient = new Ambient(controller, model, intercept);
            bool pass = false, restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                IDictionary handlers = GetHandlers();
                foreach (int id in Protocols) handlers?.Remove(id);
                var frames = new List<byte[]>();
                intercept?.SetValue(null, new Func<byte[], bool>(frame => { frames.Add((byte[])frame.Clone()); return true; }));
                controller.Init();

                MethodInfo on17 = typeof(EquipReadController).GetMethod("On15217", I);
                MethodInfo on19 = typeof(EquipReadController).GetMethod("On15219", I);
                MethodInfo on20 = typeof(EquipReadController).GetMethod("On15220", I);
                MethodInfo on23 = typeof(EquipReadController).GetMethod("On15223", I);
                MethodInfo on62 = typeof(EquipReadController).GetMethod("On15262", I);
                pass = handlers != null && intercept != null && on17 != null && on19 != null && on20 != null && on23 != null && on62 != null;
                foreach (int id in Protocols) pass &= handlers != null && handlers.Contains(id);
                pass &= handlers != null && !handlers.Contains(15202) && !handlers.Contains(15218) && !handlers.Contains(15221) && !handlers.Contains(15222);

                // 启动只清本模型并严格空发15217→15220；其它152启动号由既有控制器持有。
                model.ReplaceGodInfo(1, new List<EquipReadModel.GodEntry> { new EquipReadModel.GodEntry(1, 1) });
                model.ReplaceGodPowerPreview(2);
                model.ReplaceSuitInfo(new List<EquipReadModel.SuitEntry> { new EquipReadModel.SuitEntry(1, 1, 1) });
                model.ReplaceReturnPreview(new EquipReadModel.SuitReturnPreview(1, 1, new List<EquipReadModel.RewardEntry>()));
                model.ReplaceSuitPower(new EquipReadModel.SuitPowerSnapshot(1, 1, 1, new List<EquipReadModel.SuitPowerEntry>()));
                controller.RequestStartup();
                pass &= EmptyFrames(frames, 15217, 15220) && EmptyModel(model);

                frames.Clear();
                controller.RequestGodInfo();
                controller.RequestGodPowerPreview(byte.MaxValue);
                controller.RequestSuitInfo();
                controller.RequestSuitReturnPreview(254, 253);
                controller.RequestSuitPower(252, 251, ushort.MaxValue);
                pass &= frames.Count == 5 && EmptyFrame(frames[0], 15217)
                    && U8Frame(frames[1], 15219, byte.MaxValue) && EmptyFrame(frames[2], 15220)
                    && TwoU8Frame(frames[3], 15223, 254, 253)
                    && SuitPowerFrame(frames[4], 252, 251, ushort.MaxValue);

                // 15217全量：保序保重、u32/u16全位，后包空表完整替换且旧快照不回写。
                pass &= Feed(on17, controller, new CliVerify.Pkt().I(uint.MaxValue).H(2)
                    .C(7).H(ushort.MaxValue).C(7).H(0));
                IReadOnlyList<EquipReadModel.GodEntry> oldGod = model.GodEntries;
                pass &= model.HasGodInfo && model.GodTotalPower == uint.MaxValue && oldGod.Count == 2
                    && oldGod[0].Pos == 7 && oldGod[0].Level == ushort.MaxValue && oldGod[1].Pos == 7 && oldGod[1].Level == 0;
                pass &= Feed(on17, controller, new CliVerify.Pkt().I(0).H(0))
                    && model.HasGodInfo && model.GodTotalPower == 0 && model.GodEntries.Count == 0 && oldGod.Count == 2;

                // 15219不回显pos，只保存最后原始试算；真实0覆盖最大值。
                pass &= Feed(on19, controller, new CliVerify.Pkt().I(uint.MaxValue))
                    && model.HasGodPowerPreview && model.GodPowerPreview == uint.MaxValue;
                pass &= Feed(on19, controller, new CliVerify.Pkt().I(0)) && model.GodPowerPreview == 0;

                // 15220全量，重复位置和空表有效；不会清神装切片。
                pass &= Feed(on20, controller, new CliVerify.Pkt().H(2)
                    .C(9).C(byte.MaxValue).H(ushort.MaxValue).C(9).C(0).H(0));
                IReadOnlyList<EquipReadModel.SuitEntry> oldSuit = model.SuitEntries;
                pass &= model.HasSuitInfo && oldSuit.Count == 2 && oldSuit[0].EquipType == 9
                    && oldSuit[0].Type == byte.MaxValue && oldSuit[0].Level == ushort.MaxValue
                    && oldSuit[1].EquipType == 9 && oldSuit[1].Type == 0 && oldSuit[1].Level == 0;

                // 15223按回包(equip_type,make_type)键控；奖励保序保重和原始attr_list字符串。
                pass &= Feed(on23, controller, ReturnPacket(3, 4, true));
                pass &= model.TryGetReturnPreview(3, 4, out EquipReadModel.SuitReturnPreview preview)
                    && preview.Rewards.Count == 2 && preview.Rewards[0].Type == byte.MaxValue
                    && preview.Rewards[0].Id == uint.MaxValue && preview.Rewards[0].Num == ushort.MaxValue
                    && preview.Rewards[0].AttrList == "[{中文}]" && preview.Rewards[1].Id == uint.MaxValue
                    && preview.Rewards[1].AttrList == string.Empty;
                EquipReadModel.SuitReturnPreview oldPreview = preview;
                pass &= Feed(on23, controller, ReturnPacket(3, 5, false)) && model.ReturnPreviewCount == 2
                    && model.TryGetReturnPreview(3, 5, out EquipReadModel.SuitReturnPreview otherPreview)
                    && otherPreview.Rewards.Count == 0;
                controller.RequestSuitReturnPreview(4, 3);
                pass &= ReferenceEquals(oldPreview, GetReturn(model, 3, 4));
                pass &= Feed(on23, controller, ReturnPacket(3, 4, false))
                    && model.ReturnPreviewCount == 2 && GetReturn(model, 3, 4).Rewards.Count == 0
                    && oldPreview.Rewards.Count == 2;

                // 15262按(pos,type,lv)键控；combat保留u64全位，重复num与空表均有效。
                pass &= Feed(on62, controller, PowerPacket(6, 7, ushort.MaxValue, true));
                pass &= model.TryGetSuitPower(6, 7, ushort.MaxValue, out EquipReadModel.SuitPowerSnapshot power)
                    && power.Entries.Count == 2 && power.Entries[0].Num == byte.MaxValue
                    && power.Entries[0].Combat == ulong.MaxValue && power.Entries[1].Num == byte.MaxValue
                    && power.Entries[1].Combat == 0;
                EquipReadModel.SuitPowerSnapshot oldPower = power;
                pass &= Feed(on62, controller, PowerPacket(6, 8, 0, false)) && model.SuitPowerCount == 2
                    && GetPower(model, 6, 8, 0).Entries.Count == 0;
                controller.RequestSuitPower(6, 7, ushort.MaxValue);
                pass &= ReferenceEquals(oldPower, GetPower(model, 6, 7, ushort.MaxValue));
                pass &= Feed(on62, controller, PowerPacket(6, 7, ushort.MaxValue, false))
                    && model.SuitPowerCount == 2 && GetPower(model, 6, 7, ushort.MaxValue).Entries.Count == 0
                    && oldPower.Entries.Count == 2;

                // 单切片更新不交叉清理。
                pass &= Feed(on20, controller, new CliVerify.Pkt().H(0))
                    && model.HasGodInfo && model.HasGodPowerPreview && model.HasSuitInfo && model.SuitEntries.Count == 0
                    && model.ReturnPreviewCount == 2 && model.SuitPowerCount == 2 && oldSuit.Count == 2;

                controller.Dispose();
                pass &= !controller.IsInitialized && EmptyModel(model);
                foreach (int id in Protocols) pass &= handlers != null && !handlers.Contains(id);
                Debug.Log("CLIVERIFY equip-read pass=" + pass);
            }
            finally
            {
                restored = ambient.Restore(controller, model, intercept);
                Debug.Log("CLIVERIFY equip-read restored=" + restored + " pass=" + pass);
            }
            return pass && restored ? 0 : 3;
        }

        private static CliVerify.Pkt ReturnPacket(byte equipType, byte makeType, bool rows)
        {
            var p = new CliVerify.Pkt().C(equipType).C(makeType).H(rows ? 2 : 0);
            if (!rows) return p;
            return p.C(byte.MaxValue).I(uint.MaxValue).H(ushort.MaxValue).S("[{中文}]")
                .C(0).I(uint.MaxValue).H(0).S(string.Empty);
        }

        private static CliVerify.Pkt PowerPacket(byte pos, byte type, ushort level, bool rows)
        {
            var p = new CliVerify.Pkt().C(pos).C(type).H(level).H(rows ? 2 : 0);
            if (!rows) return p;
            return p.C(byte.MaxValue).L(-1).C(byte.MaxValue).L(0);
        }

        private static bool Feed(MethodInfo method, EquipReadController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static EquipReadModel.SuitReturnPreview GetReturn(EquipReadModel m, byte equip, byte make)
        { m.TryGetReturnPreview(equip, make, out EquipReadModel.SuitReturnPreview v); return v; }
        private static EquipReadModel.SuitPowerSnapshot GetPower(EquipReadModel m, byte pos, byte type, ushort level)
        { m.TryGetSuitPower(pos, type, level, out EquipReadModel.SuitPowerSnapshot v); return v; }

        private static bool EmptyModel(EquipReadModel m) => !m.HasGodInfo && m.GodEntries.Count == 0
            && !m.HasGodPowerPreview && !m.HasSuitInfo && m.SuitEntries.Count == 0
            && m.ReturnPreviewCount == 0 && m.SuitPowerCount == 0;

        private static bool EmptyFrames(List<byte[]> frames, params int[] ids)
        { if (frames.Count != ids.Length) return false; for (int i = 0; i < ids.Length; i++) if (!EmptyFrame(frames[i], ids[i])) return false; return true; }
        private static bool EmptyFrame(byte[] f, int id) => f != null && f.Length == 6 && Header(f, id, 6);
        private static bool U8Frame(byte[] f, int id, byte value) => f != null && f.Length == 7 && Header(f, id, 7) && f[6] == value;
        private static bool TwoU8Frame(byte[] f, int id, byte a, byte b) => f != null && f.Length == 8 && Header(f, id, 8) && f[6] == a && f[7] == b;
        private static bool SuitPowerFrame(byte[] f, byte pos, byte type, ushort level) => f != null && f.Length == 10
            && Header(f, 15262, 10) && f[6] == pos && f[7] == type && f[8] == (byte)(level >> 8) && f[9] == (byte)level;
        private static bool Header(byte[] f, int id, int length) => f[0] == 0 && f[1] == length && f[2] == 3 && f[3] == 232
            && f[4] == (byte)(id >> 8) && f[5] == (byte)id;

        private static IDictionary GetHandlers() => typeof(NetManager).GetField("_handlers", S)?.GetValue(null) as IDictionary;

        private sealed class Ambient
        {
            private readonly bool _initialized, _hasGod, _hasPreview, _hasSuit, _hadGameStart;
            private readonly uint _godPower, _previewPower;
            private readonly List<EquipReadModel.GodEntry> _gods;
            private readonly List<EquipReadModel.SuitEntry> _suits;
            private readonly Dictionary<ushort, EquipReadModel.SuitReturnPreview> _returns;
            private readonly Dictionary<uint, EquipReadModel.SuitPowerSnapshot> _powers;
            private readonly object _intercept;
            private readonly Dictionary<int, object> _handlers = new Dictionary<int, object>();
            private readonly List<Delegate> _gameStartHandlers;

            public Ambient(EquipReadController c, EquipReadModel m, FieldInfo intercept)
            {
                _initialized = c.IsInitialized; _hasGod = m.HasGodInfo; _godPower = m.GodTotalPower;
                _gods = new List<EquipReadModel.GodEntry>(m.GodEntries); _hasPreview = m.HasGodPowerPreview;
                _previewPower = m.GodPowerPreview; _hasSuit = m.HasSuitInfo;
                _suits = new List<EquipReadModel.SuitEntry>(m.SuitEntries);
                _returns = Copy<ushort, EquipReadModel.SuitReturnPreview>(m, "_returnPreviews");
                _powers = Copy<uint, EquipReadModel.SuitPowerSnapshot>(m, "_suitPowers");
                _intercept = intercept?.GetValue(null);
                IDictionary net = GetHandlers(); if (net != null) foreach (int id in Protocols) if (net.Contains(id)) _handlers[id] = net[id];
                IDictionary events = typeof(EventDispatcher).GetField("_handlers", S)?.GetValue(null) as IDictionary;
                _hadGameStart = events != null && events.Contains(GlobalEvent.EVT_GAME_START);
                if (_hadGameStart) _gameStartHandlers = new List<Delegate>((IList<Delegate>)events[GlobalEvent.EVT_GAME_START]);
            }

            public bool Restore(EquipReadController c, EquipReadModel m, FieldInfo intercept)
            {
                try
                {
                    if (c.IsInitialized) c.Dispose();
                    m.Reset();
                    if (_hasGod) m.ReplaceGodInfo(_godPower, _gods);
                    if (_hasPreview) m.ReplaceGodPowerPreview(_previewPower);
                    if (_hasSuit) m.ReplaceSuitInfo(_suits);
                    foreach (var x in _returns) m.ReplaceReturnPreview(x.Value);
                    foreach (var x in _powers) m.ReplaceSuitPower(x.Value);
                    if (_initialized) c.Init();
                    IDictionary net = GetHandlers(); if (net == null) return false;
                    foreach (int id in Protocols) if (_handlers.TryGetValue(id, out object h)) net[id] = h; else net.Remove(id);
                    IDictionary events = typeof(EventDispatcher).GetField("_handlers", S)?.GetValue(null) as IDictionary;
                    if (events == null) return false;
                    if (_hadGameStart) events[GlobalEvent.EVT_GAME_START] = new List<Delegate>(_gameStartHandlers); else events.Remove(GlobalEvent.EVT_GAME_START);
                    intercept?.SetValue(null, _intercept);
                    return c.IsInitialized == _initialized && m.HasGodInfo == _hasGod && m.GodTotalPower == _godPower
                        && RefList(m.GodEntries, _gods) && m.HasGodPowerPreview == _hasPreview && m.GodPowerPreview == _previewPower
                        && m.HasSuitInfo == _hasSuit && RefList(m.SuitEntries, _suits)
                        && MapMatches(Copy<ushort, EquipReadModel.SuitReturnPreview>(m, "_returnPreviews"), _returns)
                        && MapMatches(Copy<uint, EquipReadModel.SuitPowerSnapshot>(m, "_suitPowers"), _powers)
                        && HandlersMatch(net) && EventsMatch(events) && ReferenceEquals(intercept?.GetValue(null), _intercept);
                }
                catch (Exception e) { Debug.LogError("CLIVERIFY equip-read restore EXCEPTION " + e); return false; }
            }

            private bool HandlersMatch(IDictionary net)
            { foreach (int id in Protocols) { bool had = _handlers.TryGetValue(id, out object h); if (net.Contains(id) != had || (had && !ReferenceEquals(net[id], h))) return false; } return true; }
            private bool EventsMatch(IDictionary events)
            {
                if (events.Contains(GlobalEvent.EVT_GAME_START) != _hadGameStart) return false;
                if (!_hadGameStart) return true;
                IList<Delegate> actual = events[GlobalEvent.EVT_GAME_START] as IList<Delegate>;
                if (actual == null || actual.Count != _gameStartHandlers.Count) return false;
                for (int i = 0; i < actual.Count; i++) if (!ReferenceEquals(actual[i], _gameStartHandlers[i])) return false;
                return true;
            }
        }

        private static Dictionary<TKey, TValue> Copy<TKey, TValue>(EquipReadModel m, string field) =>
            new Dictionary<TKey, TValue>((Dictionary<TKey, TValue>)typeof(EquipReadModel).GetField(field, I).GetValue(m));
        private static bool RefList<T>(IReadOnlyList<T> actual, List<T> expected) where T : class
        { if (actual.Count != expected.Count) return false; for (int i = 0; i < actual.Count; i++) if (!ReferenceEquals(actual[i], expected[i])) return false; return true; }
        private static bool MapMatches<TKey, TValue>(Dictionary<TKey, TValue> actual, Dictionary<TKey, TValue> expected) where TValue : class
        { if (actual.Count != expected.Count) return false; foreach (var x in expected) if (!actual.TryGetValue(x.Key, out TValue v) || !ReferenceEquals(v, x.Value)) return false; return true; }
    }
}
