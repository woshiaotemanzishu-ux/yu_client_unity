using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>侍魂装备 16014-16017 的配置、运行时注册、逐字节 wire、状态门控与失败不变性实证。</summary>
    public static class PetEquipCase
    {
        private const BindingFlags INSTANCE_PRIVATE = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags STATIC_PRIVATE = BindingFlags.NonPublic | BindingFlags.Static;

        public static async Task<int> Run()
        {
            bool editorPreferFallbackBefore = Shenxiao.Framework.Res.ResManager.EditorPreferFallback;
            Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
            try
            {
                return await RunCore();
            }
            finally
            {
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = editorPreferFallbackBefore;
            }
        }

        private static async Task<int> RunCore()
        {
            Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
            await Shenxiao.Module.Core.PetEquip.PetEquipConfigs.EnsureLoaded();
            await Shenxiao.Module.Core.Common.FuncOpenConfig.EnsureLoaded();

            bool configCountsOk = Shenxiao.Module.Core.PetEquip.PetEquipConfigs.PositionCount == 8
                && Shenxiao.Module.Core.PetEquip.PetEquipConfigs.PositionLevelCount == 2408
                && Shenxiao.Module.Core.PetEquip.PetEquipConfigs.StageCount == 1600
                && Shenxiao.Module.Core.PetEquip.PetEquipConfigs.StarCount == 1600
                && Shenxiao.Module.Core.PetEquip.PetEquipConfigs.GoodsCount == 120;
            bool configSchemaOk = Shenxiao.Module.Core.PetEquip.PetEquipConfigs.GetPosition(1, 1)?["type_id"]?.Value<int>() == 1
                && Shenxiao.Module.Core.PetEquip.PetEquipConfigs.GetPositionLevel(1, 1, 0)?["2"]?.Value<int>() == 0
                && Shenxiao.Module.Core.PetEquip.PetEquipConfigs.GetStage(1, 1, 1)?["2"]?.Value<int>() == 1
                && Shenxiao.Module.Core.PetEquip.PetEquipConfigs.GetStar(1, 1, 1)?["2"]?.Value<int>() == 1
                && Shenxiao.Module.Core.PetEquip.PetEquipConfigs.GetGoods(460110101)?["pos"]?.Value<int>() == 1;
            bool configMaxOk = Shenxiao.Module.Core.PetEquip.PetEquipConfigs.HasNextPositionLevel(1, 1, 299)
                && !Shenxiao.Module.Core.PetEquip.PetEquipConfigs.HasNextPositionLevel(1, 1, 300)
                && Shenxiao.Module.Core.PetEquip.PetEquipConfigs.HasNextStage(1, 1, 199)
                && !Shenxiao.Module.Core.PetEquip.PetEquipConfigs.HasNextStage(1, 1, 200)
                && Shenxiao.Module.Core.PetEquip.PetEquipConfigs.HasNextStar(1, 1, 199)
                && !Shenxiao.Module.Core.PetEquip.PetEquipConfigs.HasNextStar(1, 1, 200);
            Debug.Log("CLIVERIFY petEquip configs counts=" + configCountsOk + " schema=" + configSchemaOk
                + " nextRowMax=" + configMaxOk);

            Shenxiao.Module.Core.PetEquip.PetEquipController ctrl = Shenxiao.Module.Core.PetEquip.PetEquipController.Instance;
            bool wasInitialized = ctrl.IsInitialized;
            ctrl.Init();

            FieldInfo outboundField = ctrl.GetType().GetField("s_outboundIntercept", STATIC_PRIVATE);
            FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", STATIC_PRIVATE);
            FieldInfo registeredField = typeof(Shenxiao.Framework.Net.BaseController).GetField("_registered", INSTANCE_PRIVATE);
            MethodInfo m16014 = ctrl.GetType().GetMethod("On16014", INSTANCE_PRIVATE);
            MethodInfo m16015 = ctrl.GetType().GetMethod("On16015", INSTANCE_PRIVATE);
            MethodInfo m16016 = ctrl.GetType().GetMethod("On16016", INSTANCE_PRIVATE);
            MethodInfo m16017 = ctrl.GetType().GetMethod("On16017", INSTANCE_PRIVATE);
            MethodInfo mGameStart = ctrl.GetType().GetMethod("OnGameStart", INSTANCE_PRIVATE);
            MethodInfo mRoleUpdate = ctrl.GetType().GetMethod("OnRoleInfoUpdate", INSTANCE_PRIVATE);
            FieldInfo sessionStartedField = ctrl.GetType().GetField("_sessionStarted", INSTANCE_PRIVATE);
            FieldInfo featureWasOpenField = ctrl.GetType().GetField("_featureWasOpen", INSTANCE_PRIVATE);
            FieldInfo sessionVersionField = ctrl.GetType().GetField("_sessionVersion", INSTANCE_PRIVATE);

            bool reflectionOk = outboundField != null && handlersField != null && registeredField != null
                && m16014 != null && m16015 != null && m16016 != null && m16017 != null
                && mGameStart != null && mRoleUpdate != null && sessionStartedField != null
                && featureWasOpenField != null && sessionVersionField != null;

            IDictionary runtimeHandlers = handlersField?.GetValue(null) as IDictionary;
            IList registered = registeredField?.GetValue(ctrl) as IList;
            int[] protos =
            {
                Shenxiao.Framework.Net.Proto.PET_EQUIP_INFO,
                Shenxiao.Framework.Net.Proto.PET_EQUIP_WEAR,
                Shenxiao.Framework.Net.Proto.PET_EQUIP_STRENGTHEN,
                Shenxiao.Framework.Net.Proto.PET_EQUIP_POLISH
            };
            bool registrationOk = runtimeHandlers != null && registered != null;
            foreach (int proto in protos)
            {
                int ownCount = 0;
                if (registered != null)
                {
                    foreach (object value in registered) if ((int)value == proto) ownCount++;
                }
                if (ownCount != 1 || !runtimeHandlers.Contains(proto)
                    || !(runtimeHandlers[proto] is Delegate handler) || !ReferenceEquals(handler.Target, ctrl))
                {
                    registrationOk = false;
                }
            }
            Debug.Log("CLIVERIFY petEquip registration unique=" + registrationOk + " reflection=" + reflectionOk);

            if (!reflectionOk)
            {
                if (!wasInitialized) ctrl.Dispose();
                return 3;
            }

            bool savedSessionStarted = (bool)sessionStartedField.GetValue(ctrl);
            bool savedFeatureWasOpen = (bool)featureWasOpenField.GetValue(ctrl);
            int savedSessionVersion = (int)sessionVersionField.GetValue(ctrl);
            int savedRoleLevel = Shenxiao.Module.Core.Role.RoleModel.Instance.Level;

            var outbound = new List<byte[]>();
            Func<byte[], bool> intercept = frame =>
            {
                outbound.Add(frame);
                return true;
            };
            bool requestWireOk = false;
            bool responseOk = false;
            bool lifecycleOk = false;

            try
            {
                outboundField.SetValue(null, intercept);

                const long WEAR_GOODS = 0x0102030405060708L;
                const long STRENGTH_GOODS = 0x1112131415161718L;
                const long COST_A = 0x2122232425262728L;
                const long COST_B = 0x3132333435363738L;
                const long POLISH_GOODS = 0x4142434445464748L;
                const long POLISH_COST = 0x5152535455565758L;

                ctrl.RequestInfo(2);
                bool infoWire = outbound.Count == 1 && FrameEquals(outbound[0], Shenxiao.Framework.Net.Proto.PET_EQUIP_INFO,
                    new CliVerify.Pkt().C(2).Bytes());
                outbound.Clear();

                ctrl.RequestWear(1, 3, WEAR_GOODS);
                bool wearWire = outbound.Count == 1 && FrameEquals(outbound[0], Shenxiao.Framework.Net.Proto.PET_EQUIP_WEAR,
                    new CliVerify.Pkt().C(1).C(3).L(WEAR_GOODS).Bytes());
                outbound.Clear();

                ctrl.RequestStrengthen(2, STRENGTH_GOODS, new[] { COST_A, COST_B });
                bool strengthenWire = outbound.Count == 1 && FrameEquals(outbound[0], Shenxiao.Framework.Net.Proto.PET_EQUIP_STRENGTHEN,
                    new CliVerify.Pkt().C(2).L(STRENGTH_GOODS).H(2).L(COST_A).L(COST_B).Bytes());
                outbound.Clear();

                ctrl.RequestPolish(1, POLISH_GOODS, POLISH_COST);
                bool polishWire = outbound.Count == 1 && FrameEquals(outbound[0], Shenxiao.Framework.Net.Proto.PET_EQUIP_POLISH,
                    new CliVerify.Pkt().C(1).L(POLISH_GOODS).L(POLISH_COST).Bytes());
                outbound.Clear();
                requestWireOk = infoWire && wearWire && strengthenWire && polishWire;
                Debug.Log("CLIVERIFY petEquip request wire info=" + infoWire + " wear=" + wearWire
                    + " strengthen=" + strengthenWire + " polish=" + polishWire);

                Shenxiao.Module.Core.PetEquip.PetEquipModel model = Shenxiao.Module.Core.PetEquip.PetEquipModel.Instance;
                model.Clear();
                int updateEvents = 0;
                int strengthenEvents = 0;
                int starEvents = 0;
                Action<int> onUpdate = typeId => updateEvents++;
                Action onStrengthen = () => strengthenEvents++;
                Action onStar = () => starEvents++;
                Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_PET_EQUIP_UPDATE, onUpdate);
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_PET_EQUIP_STRENGTH_SUCCESS, onStrengthen);
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_PET_EQUIP_STAR_SUCCESS, onStar);
                try
                {
                    NetTail result16014 = Feed(m16014, ctrl, new CliVerify.Pkt()
                        .C(1).I(1).I(1000).H(2)
                        .C(1).I(5).I(2).H(3).I(40).L(1001).I(460110101)
                        .C(2).I(6).I(4).H(5).I(50).L(1002).I(460120101)
                        .I(0x13572468).Bytes(), 0x13572468);
                    Shenxiao.Module.Core.PetEquip.PetEquipModel.PetEquipInfo info = model.Get(1);
                    bool b16014Ok = result16014.Ok && info != null && info.CombatPower == 1000 && info.Items.Count == 2
                        && info.Items[0].GoodsId == 1001 && info.Items[1].PosId == 2 && updateEvents == 1;

                    NetTail result16014Fail = Feed(m16014, ctrl, new CliVerify.Pkt()
                        .C(1).I(5).I(9999).H(1)
                        .C(1).I(99).I(99).H(99).I(99).L(9999).I(9999)
                        .I(0x24681357).Bytes(), 0x24681357);
                    info = model.Get(1);
                    bool b16014Fail = result16014Fail.Ok && info != null && info.CombatPower == 1000
                        && info.Items.Count == 2 && info.Items[0].GoodsId == 1001 && updateEvents == 1;

                    outbound.Clear();
                    NetTail result16015 = Feed(m16015, ctrl, new CliVerify.Pkt()
                        .C(1).I(1).C(1).L(1001).L(0).I(460110101).I(1100)
                        .I(0x35792468).Bytes(), 0x35792468);
                    bool b16015Ok = result16015.Ok && outbound.Count == 1
                        && FrameEquals(outbound[0], Shenxiao.Framework.Net.Proto.PET_EQUIP_INFO,
                            new CliVerify.Pkt().C(1).Bytes());
                    outbound.Clear();
                    NetTail result16015Fail = Feed(m16015, ctrl, new CliVerify.Pkt()
                        .C(1).I(5).C(1).L(1001).L(0).I(460110101).I(0)
                        .I(0x46813579).Bytes(), 0x46813579);
                    bool b16015Fail = result16015Fail.Ok && outbound.Count == 0 && model.Get(1).CombatPower == 1000;

                    NetTail result16016 = Feed(m16016, ctrl, new CliVerify.Pkt()
                        .C(1).I(1).I(77).H(6).L(1001).I(1200)
                        .I(0x57924681).Bytes(), 0x57924681);
                    info = model.Get(1);
                    bool b16016Ok = result16016.Ok && info.CombatPower == 1200 && info.Items[0].PosPoint == 77
                        && info.Items[0].PosLevel == 6 && strengthenEvents == 1 && updateEvents == 2;

                    NetTail result16016Same = Feed(m16016, ctrl, new CliVerify.Pkt()
                        .C(1).I(1).I(88).H(6).L(1001).I(1300)
                        .I(0x68135792).Bytes(), 0x68135792);
                    bool b16016Same = result16016Same.Ok && info.CombatPower == 1300 && info.Items[0].PosPoint == 88
                        && info.Items[0].PosLevel == 6 && strengthenEvents == 1 && updateEvents == 3;

                    NetTail result16016Fail = Feed(m16016, ctrl, new CliVerify.Pkt()
                        .C(1).I(5).I(999).H(20).L(1001).I(9999)
                        .I(0x71356824).Bytes(), 0x71356824);
                    bool b16016Fail = result16016Fail.Ok && info.CombatPower == 1300 && info.Items[0].PosPoint == 88
                        && info.Items[0].PosLevel == 6 && strengthenEvents == 1 && updateEvents == 3;

                    NetTail result16017 = Feed(m16017, ctrl, new CliVerify.Pkt()
                        .C(1).I(1).H(4).H(7).L(1001).L(2002).I(1500).I(99).H(8)
                        .I(0x79246813).Bytes(), 0x79246813);
                    bool b16017Ok = result16017.Ok && info.CombatPower == 1500 && info.Items[0].Stage == 4
                        && info.Items[0].Star == 7 && info.Items[0].PosPoint == 99 && info.Items[0].PosLevel == 8
                        && starEvents == 1 && updateEvents == 4;

                    NetTail result16017Fail = Feed(m16017, ctrl, new CliVerify.Pkt()
                        .C(1).I(5).H(9).H(9).L(1001).L(2002).I(9000).I(900).H(20)
                        .I(0x81357924).Bytes(), 0x81357924);
                    bool b16017Fail = result16017Fail.Ok && info.CombatPower == 1500 && info.Items[0].Stage == 4
                        && info.Items[0].Star == 7 && info.Items[0].PosPoint == 99 && info.Items[0].PosLevel == 8
                        && starEvents == 1 && updateEvents == 4;

                    responseOk = b16014Ok && b16014Fail && b16015Ok && b16015Fail
                        && b16016Ok && b16016Same && b16016Fail && b16017Ok && b16017Fail;
                    Debug.Log("CLIVERIFY petEquip responses 16014=" + (b16014Ok && b16014Fail)
                        + " 16015=" + (b16015Ok && b16015Fail) + " 16016=" + (b16016Ok && b16016Same && b16016Fail)
                        + " 16017=" + (b16017Ok && b16017Fail));
                }
                finally
                {
                    Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_PET_EQUIP_UPDATE, onUpdate);
                    Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_PET_EQUIP_STRENGTH_SUCCESS, onStrengthen);
                    Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_PET_EQUIP_STAR_SUCCESS, onStar);
                }

                Shenxiao.Module.Core.Role.RoleModel.Instance.Level = 219;
                bool closedAt219 = !Shenxiao.Module.Core.Common.FuncOpenConfig.CheckFuncOpenState("PetEquipBaseView");
                outbound.Clear();
                mGameStart.Invoke(ctrl, null);
                await Task.Delay(20);
                bool closedStartSuppressed = outbound.Count == 0;

                Shenxiao.Module.Core.Role.RoleModel.Instance.Level = 220;
                bool openAt220 = Shenxiao.Module.Core.Common.FuncOpenConfig.CheckFuncOpenState("PetEquipBaseView");
                mRoleUpdate.Invoke(ctrl, null);
                bool transitionPair = outbound.Count == 2
                    && FrameEquals(outbound[0], Shenxiao.Framework.Net.Proto.PET_EQUIP_INFO, new CliVerify.Pkt().C(1).Bytes())
                    && FrameEquals(outbound[1], Shenxiao.Framework.Net.Proto.PET_EQUIP_INFO, new CliVerify.Pkt().C(2).Bytes());
                mRoleUpdate.Invoke(ctrl, null);
                bool repeatSuppressed = outbound.Count == 2;
                lifecycleOk = closedAt219 && openAt220 && closedStartSuppressed && transitionPair && repeatSuppressed;
                Debug.Log("CLIVERIFY petEquip lifecycle closedStartSuppressed=" + closedStartSuppressed + " transition=" + transitionPair
                    + " repeatSuppressed=" + repeatSuppressed);
            }
            finally
            {
                outboundField.SetValue(null, null);
                Shenxiao.Module.Core.Role.RoleModel.Instance.Level = savedRoleLevel;
                Shenxiao.Module.Core.PetEquip.PetEquipModel.Instance.Clear();
                if (wasInitialized)
                {
                    sessionStartedField.SetValue(ctrl, savedSessionStarted);
                    featureWasOpenField.SetValue(ctrl, savedFeatureWasOpen);
                    sessionVersionField.SetValue(ctrl, savedSessionVersion);
                }
                else
                {
                    ctrl.Dispose();
                }
            }

            bool pass = configCountsOk && configSchemaOk && configMaxOk && registrationOk && requestWireOk
                && responseOk && lifecycleOk;
            Debug.Log("CLIVERIFY petEquip VERDICT configs=" + (configCountsOk && configSchemaOk && configMaxOk)
                + " registration=" + registrationOk + " requestWire=" + requestWireOk
                + " responses=" + responseOk + " lifecycle=" + lifecycleOk + " pass=" + pass);
            return pass ? 0 : 3;
        }

        private readonly struct NetTail
        {
            public readonly bool Ok;
            public NetTail(bool ok) { Ok = ok; }
        }

        private static NetTail Feed(MethodInfo method, object target, byte[] packet, uint sentinel)
        {
            var reader = new Shenxiao.Framework.Net.NetReader(packet, 0, packet.Length);
            method.Invoke(target, new object[] { reader });
            bool ok = reader.Remaining == 4 && reader.ReadU32() == sentinel && reader.Remaining == 0;
            return new NetTail(ok);
        }

        private static bool FrameEquals(byte[] actual, int protoId, byte[] payload)
        {
            int total = 6 + payload.Length;
            if (actual == null || actual.Length != total) return false;
            byte[] expected = new byte[total];
            expected[0] = (byte)(total >> 8);
            expected[1] = (byte)total;
            expected[2] = 0x03;
            expected[3] = 0xE8;
            expected[4] = (byte)(protoId >> 8);
            expected[5] = (byte)protoId;
            Buffer.BlockCopy(payload, 0, expected, 6, payload.Length);
            for (int i = 0; i < total; i++)
            {
                if (actual[i] != expected[i]) return false;
            }
            return true;
        }
    }
}
