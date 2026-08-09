using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Unreal;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>幻饰14900/04/06/07/08二进制边界、启动帧、切片隔离、不可变、配置闭包与生命周期专项。</summary>
    public static class UnrealCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly int[] RegisteredIds = { 14900, 14904, 14906, 14907, 14908 };

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY unreal EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            UnrealController controller = UnrealController.Instance;
            UnrealModel model = UnrealModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            UnrealModel.ErrorSnapshot oldError = model.LastError;
            UnrealModel.UnlockSnapshot oldUnlock = model.UnlockedCells;
            Dictionary<byte, UnrealModel.StrengthSnapshot> oldStrength = Copy(model.StrengthByCell);
            Dictionary<ulong, UnrealModel.PreviewSnapshot> oldStage = Copy(model.StagePreviews);
            Dictionary<ulong, UnrealModel.PreviewSnapshot> oldDecompose = Copy(model.DecomposePreviews);
            FieldInfo interceptor = typeof(UnrealController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            foreach (int id in RegisteredIds) SaveHandler(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                model.Reset();
                MethodInfo on14900 = typeof(UnrealController).GetMethod("On14900", F);
                MethodInfo on14904 = typeof(UnrealController).GetMethod("On14904", F);
                MethodInfo on14906 = typeof(UnrealController).GetMethod("On14906", F);
                MethodInfo on14907 = typeof(UnrealController).GetMethod("On14907", F);
                MethodInfo on14908 = typeof(UnrealController).GetMethod("On14908", F);

                bool a = handlers != null && interceptor != null && on14900 != null && on14904 != null
                    && on14906 != null && on14907 != null && on14908 != null;
                foreach (int id in RegisteredIds) a &= handlers != null && handlers.Contains(id);
                foreach (int id in new[] { 14901, 14902, 14903, 14905 })
                    a &= handlers != null && !handlers.Contains(id);

                bool b = false;
                bool c = false;
                bool d = false;
                bool e = false;
                bool f = false;
                bool g = false;
                bool h = false;
                var frames = new List<byte[]>();
                if (a)
                {
                    interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                    model.ReplaceError(1, "seed");
                    model.ReplaceStrength(1, 1, 1, 1);
                    model.ReplaceStagePreview(1, 1, Array.Empty<UnrealModel.PreviewAttr>());
                    model.ReplaceDecomposePreview(1, 1, Array.Empty<UnrealModel.PreviewAttr>());
                    model.ReplaceUnlockedCells(new byte[] { 1 });
                    controller.RequestStartup();
                    b = StartupFramesAre(frames) && !model.HasError && !model.HasUnlockedCells
                        && model.StrengthByCell.Count == 0 && model.StagePreviews.Count == 0
                        && model.DecomposePreviews.Count == 0;
                    frames.Clear();

                    UnrealModel.StrengthSnapshot zero = null;
                    c = Invoke(on14904, controller,
                            new CliVerify.Pkt().I(uint.MaxValue).C(0).H(ushort.MaxValue).I(4000000000L).Bytes())
                        && model.TryGetStrength(0, out zero)
                        && zero.Result == uint.MaxValue && zero.Level == ushort.MaxValue && zero.Point == 4000000000U;
                    UnrealModel.StrengthSnapshot zeroRef = zero;
                    c &= Invoke(on14904, controller, new CliVerify.Pkt().I(1).C(6).H(2).I(3).Bytes())
                        && model.StrengthByCell.Count == 2 && model.TryGetStrength(6, out UnrealModel.StrengthSnapshot six)
                        && six.Level == 2 && ReferenceEquals(model.StrengthByCell[0], zeroRef);
                    UnrealModel.StrengthSnapshot replaced = null;
                    c &= Invoke(on14904, controller, new CliVerify.Pkt().I(0).C(0).H(0).I(0).Bytes())
                        && model.StrengthByCell.Count == 2 && model.TryGetStrength(0, out replaced)
                        && !ReferenceEquals(replaced, zeroRef) && replaced.Result == 0 && replaced.Level == 0
                        && zeroRef.Result == uint.MaxValue && zeroRef.Point == 4000000000U && frames.Count == 0;
                    UnrealModel.StrengthSnapshot strengthRef = replaced;

                    d = Invoke(on14908, controller, new CliVerify.Pkt().H(4).C(6).C(1).C(6).C(255).Bytes())
                        && model.HasUnlockedCells && model.UnlockedCells.Cells.Count == 4
                        && model.UnlockedCells.Cells[0] == 6 && model.UnlockedCells.Cells[1] == 1
                        && model.UnlockedCells.Cells[2] == 6 && model.UnlockedCells.Cells[3] == byte.MaxValue
                        && ReferenceEquals(model.StrengthByCell[0], strengthRef);
                    UnrealModel.UnlockSnapshot unlockRef = model.UnlockedCells;

                    byte[] stagePacket = new CliVerify.Pkt().L(-1).I(uint.MaxValue).H(3)
                        .C(0).C(0).H(0).I(0).C(0).I(0)
                        .C(255).C(255).H(65535).I(uint.MaxValue).C(255).I(4000000000L)
                        .C(255).C(255).H(65535).I(7).C(255).I(uint.MaxValue).Bytes();
                    UnrealModel.PreviewSnapshot stage = null;
                    e = Invoke(on14906, controller, stagePacket)
                        && model.TryGetStagePreview(ulong.MaxValue, out stage)
                        && stage.GoodsId == ulong.MaxValue && stage.OverallRating == uint.MaxValue
                        && stage.Attrs.Count == 3 && stage.Attrs[0].Color == 0 && stage.Attrs[0].AttrId == 0
                        && stage.Attrs[1].Color == byte.MaxValue && stage.Attrs[1].TypeId == byte.MaxValue
                        && stage.Attrs[1].AttrId == ushort.MaxValue && stage.Attrs[1].AttrValue == uint.MaxValue
                        && stage.Attrs[1].PlusInterval == byte.MaxValue && stage.Attrs[1].PlusUnit == 4000000000U
                        && stage.Attrs[2].AttrId == ushort.MaxValue;
                    UnrealModel.PreviewSnapshot stageRef = stage;
                    e &= Invoke(on14906, controller, new CliVerify.Pkt().L(5).I(0).H(0).Bytes())
                        && model.StagePreviews.Count == 2 && model.TryGetStagePreview(5, out UnrealModel.PreviewSnapshot stageFive)
                        && stageFive.Attrs.Count == 0 && ReferenceEquals(model.StagePreviews[ulong.MaxValue], stageRef)
                        && stageRef.Attrs.Count == 3 && ReferenceEquals(model.UnlockedCells, unlockRef);

                    byte[] decomposePacket = new CliVerify.Pkt().L(5).I(123).H(2)
                        .C(2).C(1).H(9).I(10).C(11).I(12)
                        .C(2).C(1).H(9).I(13).C(11).I(14).Bytes();
                    UnrealModel.PreviewSnapshot decompose = null;
                    f = Invoke(on14907, controller, decomposePacket)
                        && model.TryGetDecomposePreview(5, out decompose)
                        && decompose.OverallRating == 123 && decompose.Attrs.Count == 2
                        && decompose.Attrs[0].AttrId == 9 && decompose.Attrs[1].AttrId == 9
                        && decompose.Attrs[1].AttrValue == 13 && ReferenceEquals(model.StagePreviews[ulong.MaxValue], stageRef)
                        && ReferenceEquals(model.UnlockedCells, unlockRef) && ReferenceEquals(model.StrengthByCell[0], strengthRef);
                    UnrealModel.PreviewSnapshot decomposeRef = decompose;

                    g = Invoke(on14900, controller, new CliVerify.Pkt().I(0).S("").Bytes())
                        && model.HasError && model.LastError.Code == 0 && model.LastError.Message == "";
                    g &= Invoke(on14900, controller, new CliVerify.Pkt().I(uint.MaxValue).S("幻饰参数原样").Bytes())
                        && model.LastError.Code == uint.MaxValue && model.LastError.Message == "幻饰参数原样"
                        && ReferenceEquals(model.StagePreviews[ulong.MaxValue], stageRef)
                        && ReferenceEquals(model.DecomposePreviews[5], decomposeRef)
                        && ReferenceEquals(model.UnlockedCells, unlockRef) && ReferenceEquals(model.StrengthByCell[0], strengthRef);
                    UnrealModel.ErrorSnapshot errorRef = model.LastError;

                    h = Invoke(on14908, controller, new CliVerify.Pkt().H(0).Bytes())
                        && model.HasUnlockedCells && model.UnlockedCells.Cells.Count == 0
                        && ReferenceEquals(model.LastError, errorRef) && ReferenceEquals(model.StagePreviews[ulong.MaxValue], stageRef)
                        && ReferenceEquals(model.DecomposePreviews[5], decomposeRef) && ReferenceEquals(model.StrengthByCell[0], strengthRef);
                    UnrealModel.UnlockSnapshot emptyUnlock = model.UnlockedCells;
                    controller.RequestStrength(255);
                    controller.RequestStagePreview(ulong.MaxValue);
                    controller.RequestDecomposePreview(0);
                    controller.RequestUnlockedCells();
                    h &= ExplicitFramesAre(frames) && ReferenceEquals(model.UnlockedCells, emptyUnlock)
                        && ReferenceEquals(model.LastError, errorRef) && ReferenceEquals(model.StagePreviews[ulong.MaxValue], stageRef)
                        && ReferenceEquals(model.DecomposePreviews[5], decomposeRef) && ReferenceEquals(model.StrengthByCell[0], strengthRef);

                    controller.Dispose();
                    h &= !controller.IsInitialized && !model.HasError && !model.HasUnlockedCells
                        && model.StrengthByCell.Count == 0 && model.StagePreviews.Count == 0
                        && model.DecomposePreviews.Count == 0;
                    foreach (int id in RegisteredIds) h &= !handlers.Contains(id);
                }

                bool i = ValidateConfigs();
                pass = a && b && c && d && e && f && g && h && i;
                Debug.Log($"CLIVERIFY unreal VERDICT A={a} B={b} C={c} D={d} E={e} F={f} G={g} H={h} I={i} pass={pass}");
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                RestoreProperty(model, "LastError", oldError);
                RestoreProperty(model, "UnlockedCells", oldUnlock);
                RestoreDictionary(model, "_strengthByCell", oldStrength);
                RestoreDictionary(model, "_stagePreviews", oldStage);
                RestoreDictionary(model, "_decomposePreviews", oldDecompose);
                if (wasInitialized) controller.Init();
                foreach (int id in RegisteredIds) RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = controller.IsInitialized == wasInitialized
                    && ReferenceEquals(model.LastError, oldError) && ReferenceEquals(model.UnlockedCells, oldUnlock)
                    && DictionaryMatches(model.StrengthByCell, oldStrength)
                    && DictionaryMatches(model.StagePreviews, oldStage)
                    && DictionaryMatches(model.DecomposePreviews, oldDecompose)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                foreach (int id in RegisteredIds) restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY unreal restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool Invoke(MethodInfo handler, UnrealController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool ValidateConfigs()
        {
            var json = new Dictionary<string, string>
            {
                ["config_decoration_kv"] = "{\"1\":{\"key\":1,\"value\":\"100\",\"desc\":\"容量\"}}",
                ["config_decoration_attr"] = "{\"55014101\":{\"goods_id\":55014101,\"stage\":1,\"star\":1,\"base_rating\":9,\"recommend_attr\":\"[]\",\"color_attr\":\"[]\"}}",
                ["config_decoration_level_max"] = "{\"1@1@4\":{\"pos\":1,\"stage\":1,\"color\":4,\"limit_level\":10}}",
                ["config_decoration_level"] = "{\"1@0\":{\"0\":1,\"1\":0,\"2\":\"[{\\\"0\\\":0,\\\"1\\\":38040018,\\\"2\\\":10}]\",\"3\":\"[]\"}}",
                ["config_decoration_stage"] = "{\"55014101\":{\"goods_id\":55014101,\"cost\":\"[{\\\"0\\\":0,\\\"1\\\":38040019,\\\"2\\\":1}]\",\"new_goods_id\":55014102}}",
                ["config_decoration_stage_max"] = "{\"360\":{\"player_level\":360,\"limit_stage\":3},\"480\":{\"player_level\":480,\"limit_stage\":6}}",
                ["config_dec_unlock_cell"] = "{\"1\":{\"equip_cell\":1,\"unlock_stage\":0},\"2\":{\"equip_cell\":2,\"unlock_stage\":0},\"3\":{\"equip_cell\":3,\"unlock_stage\":1},\"4\":{\"equip_cell\":4,\"unlock_stage\":1},\"5\":{\"equip_cell\":5,\"unlock_stage\":2},\"6\":{\"equip_cell\":6,\"unlock_stage\":2}}",
                ["config_goods_decompose"] = "{\"55014101\":{\"0\":55014101,\"1\":233,\"2\":0,\"3\":\"[]\",\"4\":1,\"5\":\"[{\\\"0\\\":255,\\\"1\\\":36255098,\\\"2\\\":5}]\"}}",
                ["config_soul_attr_num"] = "{\"1@1\":{\"sub_type\":1,\"lv\":1,\"attr_num_list\":\"[]\",\"lv_up_num\":0}}",
                ["goodssubtype"] = "{\"55@1\":{\"type\":55,\"subtype\":1,\"subtype_name\":\"冥面\"},\"55@2\":{\"type\":55,\"subtype\":2,\"subtype_name\":\"灵玉\"},\"55@3\":{\"type\":55,\"subtype\":3,\"subtype_name\":\"腰环\"},\"55@4\":{\"type\":55,\"subtype\":4,\"subtype_name\":\"耳坠\"},\"55@5\":{\"type\":55,\"subtype\":5,\"subtype_name\":\"符令\"},\"55@6\":{\"type\":55,\"subtype\":6,\"subtype_name\":\"道印\"}}",
            };
            UnrealConfigs.Snapshot cfg = UnrealConfigs.ParseForValidation(json);
            return cfg.IsValid && cfg.BagCapacity == 100 && cfg.AttrCount == 1 && cfg.LevelCount == 1
                && cfg.StageCount == 1 && cfg.PositionCount == 6 && cfg.SoulAttrCount == 1
                && cfg.GetPositionName(1) == "冥面" && cfg.GetPositionName(6) == "道印"
                && cfg.GetUnlockStage(3) == 1 && cfg.GetUnlockStage(7) == -1
                && cfg.GetLevelLimit(1, 1, 4) == 10 && cfg.GetMaxStage(359) == 0
                && cfg.GetMaxStage(360) == 3 && cfg.GetMaxStage(999) == 6
                && cfg.TryGetLevel(1, 0, out UnrealConfigs.LevelRow level) && level.Costs.Count == 1
                && level.Costs[0].TypeId == 38040018 && level.Costs[0].Count == 10
                && cfg.TryGetStage(55014101, out UnrealConfigs.StageRow stage)
                && stage.NewGoodsId == 55014102 && stage.Costs.Count == 1
                && cfg.GetDecomposeRewards(55014101).Count == 1
                && cfg.GetDecomposeRewards(55014101)[0].Type == 255
                && cfg.GetDecomposeRewards(55014101)[0].TypeId == 36255098
                && cfg.GetSoulAttrRow(1, 1) != null && cfg.GetSoulAttrRow(1, 2) == null;
        }

        private static bool StartupFramesAre(IReadOnlyList<byte[]> frames)
        {
            if (frames == null || frames.Count != 7) return false;
            for (int i = 0; i < 6; i++)
            {
                byte[] expected = new CliVerify.Pkt().H(7).H(1000).H(14904).C(i + 1).Bytes();
                if (!BytesEqual(frames[i], expected)) return false;
            }
            return BytesEqual(frames[6], new CliVerify.Pkt().H(6).H(1000).H(14908).Bytes());
        }

        private static bool ExplicitFramesAre(IReadOnlyList<byte[]> frames)
        {
            return frames != null && frames.Count == 4
                && BytesEqual(frames[0], new CliVerify.Pkt().H(7).H(1000).H(14904).C(255).Bytes())
                && BytesEqual(frames[1], new CliVerify.Pkt().H(14).H(1000).H(14906).L(-1).Bytes())
                && BytesEqual(frames[2], new CliVerify.Pkt().H(14).H(1000).H(14907).L(0).Bytes())
                && BytesEqual(frames[3], new CliVerify.Pkt().H(6).H(1000).H(14908).Bytes());
        }

        private static bool BytesEqual(IReadOnlyList<byte> actual, IReadOnlyList<byte> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static Dictionary<TKey, TValue> Copy<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> source)
        {
            var copy = new Dictionary<TKey, TValue>();
            foreach (KeyValuePair<TKey, TValue> pair in source) copy[pair.Key] = pair.Value;
            return copy;
        }

        private static void RestoreProperty(object target, string name, object value)
        {
            target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(target, value);
        }

        private static void RestoreDictionary<TKey, TValue>(object target, string name, IDictionary<TKey, TValue> values)
        {
            var dictionary = target.GetType().GetField(name, F)?.GetValue(target) as IDictionary<TKey, TValue>;
            if (dictionary == null) return;
            dictionary.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in values) dictionary[pair.Key] = pair.Value;
        }

        private static bool DictionaryMatches<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> actual,
            IReadOnlyDictionary<TKey, TValue> expected)
        {
            if (actual.Count != expected.Count) return false;
            foreach (KeyValuePair<TKey, TValue> pair in expected)
                if (!actual.TryGetValue(pair.Key, out TValue value) || !ReferenceEquals(value, pair.Value)) return false;
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
    }
}
