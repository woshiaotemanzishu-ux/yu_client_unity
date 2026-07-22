using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Eternity;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class EternityCase
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
                Debug.LogError("CLIVERIFY eternity EXCEPTION " + exception);
                return Task.FromResult(3);
            }
        }

        private static int RunCore()
        {
            EternityController controller = EternityController.Instance;
            EternityModel model = EternityModel.Instance;
            RoleModel role = RoleModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasData = model.HasData;
            uint oldOpenTime = model.OpenTime;
            uint oldEnterTime = model.EnterTime;
            uint oldEndTime = model.EndTime;
            int oldLevel = role.Level;
            FieldInfo hasBaseInfoField = typeof(RoleModel).GetField("<HasBaseInfo>k__BackingField", InstanceNonPublic);
            bool oldHasBaseInfo = hasBaseInfoField != null && (bool)hasBaseInfoField.GetValue(role);
            FieldInfo interceptField = typeof(EternityController).GetField("s_outboundIntercept", StaticNonPublic);
            FieldInfo lastLevelField = typeof(EternityController).GetField("_lastLevel", InstanceNonPublic);
            object oldIntercept = interceptField == null ? null : interceptField.GetValue(null);
            int oldLastLevel = lastLevelField == null ? -1 : (int)lastLevelField.GetValue(controller);

            try
            {
                controller.Init();
                model.Reset();

                MethodInfo on27900 = typeof(EternityController).GetMethod("On27900", InstanceNonPublic);
                MethodInfo onRoleInfoUpdate = typeof(EternityController).GetMethod("OnRoleInfoUpdate", InstanceNonPublic);
                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticNonPublic)?.GetValue(null) as IDictionary;
                bool pass = hasBaseInfoField != null && interceptField != null && lastLevelField != null
                    && on27900 != null && onRoleInfoUpdate != null && handlers != null && handlers.Contains(27900);
                for (int proto = 27901; proto <= 27909; proto++)
                {
                    pass &= !handlers.Contains(proto);
                }

                if (!pass)
                {
                    Debug.LogError("CLIVERIFY eternity VERDICT pass=false (reflection/protocol registration missing)");
                    return 3;
                }

                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));
                hasBaseInfoField.SetValue(role, true);

                model.Replace(1, 2, 3);
                role.Level = 479;
                controller.RequestStartup();
                pass &= frames.Count == 0 && !model.HasData && model.OpenTime == 0 && model.EnterTime == 0 && model.EndTime == 0;

                model.Replace(4, 5, 6);
                role.Level = 480;
                controller.RequestStartup();
                pass &= frames.Count == 1 && !model.HasData;
                pass &= IsExactRequest(frames[0]);
                frames.Clear();

                role.Level = 479;
                controller.RequestStartup();
                role.Level = 480;
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 1 && IsExactRequest(frames[0]);
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 1;
                role.Level = 481;
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 1;

                frames.Clear();
                role.Level = 479;
                controller.RequestStartup();
                role.Level = 481;
                onRoleInfoUpdate.Invoke(controller, null);
                pass &= frames.Count == 0;

                byte[] firstBytes = new CliVerify.Pkt().I(0).I(4000000000L).I(4294967295L).Bytes();
                var firstReader = new NetReader(firstBytes, 0, firstBytes.Length);
                on27900.Invoke(controller, new object[] { firstReader });
                pass &= firstReader.Remaining == 0 && model.HasData
                    && model.OpenTime == 0 && model.EnterTime == 4000000000U && model.EndTime == uint.MaxValue
                    && frames.Count == 0;

                byte[] secondBytes = new CliVerify.Pkt().I(7).I(8).I(9).Bytes();
                var secondReader = new NetReader(secondBytes, 0, secondBytes.Length);
                on27900.Invoke(controller, new object[] { secondReader });
                pass &= secondReader.Remaining == 0 && model.HasData
                    && model.OpenTime == 7 && model.EnterTime == 8 && model.EndTime == 9;

                controller.Dispose();
                pass &= !model.HasData && model.OpenTime == 0 && model.EnterTime == 0 && model.EndTime == 0;

                Debug.Log("CLIVERIFY eternity VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (controller.IsInitialized)
                {
                    controller.Dispose();
                }

                model.Reset();
                if (oldHasData)
                {
                    model.Replace(oldOpenTime, oldEnterTime, oldEndTime);
                }

                role.Level = oldLevel;
                if (hasBaseInfoField != null)
                {
                    hasBaseInfoField.SetValue(role, oldHasBaseInfo);
                }

                if (wasInitialized)
                {
                    controller.Init();
                }

                if (lastLevelField != null)
                {
                    lastLevelField.SetValue(controller, oldLastLevel);
                }

                if (interceptField != null)
                {
                    interceptField.SetValue(null, oldIntercept);
                }
            }
        }

        private static bool IsExactRequest(byte[] frame)
        {
            return frame != null
                && frame.Length == 6
                && frame[0] == 0 && frame[1] == 6
                && frame[2] == 0x03 && frame[3] == 0xE8
                && frame[4] == (byte)(Proto.ETERNITY_TIME_INFO >> 8)
                && frame[5] == (byte)(Proto.ETERNITY_TIME_INFO & 0xFF);
        }
    }
}
