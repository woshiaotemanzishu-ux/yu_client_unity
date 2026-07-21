using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Baby;
using Shenxiao.Module.Core.Game;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// Baby(pt_182) 首个只读态包验证：运行时注册、GameStart 单次首拉、18201 激活后的严格查询顺序，
    /// 以及 18200/01/03/04/05/06/07/21 合成回包的字段位宽、嵌套数组、尾哨兵和模型落地。
    /// 独立运行入口：-executeMethod Shenxiao.EditorTools.BabyCase.RunBatch。
    /// </summary>
    public static class BabyCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private const int Tail = 0x13572468;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY baby EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            BabyController ctrl = BabyController.Instance;
            bool wasInitialized = ctrl.IsInitialized;
            try
            {
                ctrl.Init();
                return RunInitialized(ctrl);
            }
            finally
            {
                if (!wasInitialized && ctrl.IsInitialized) ctrl.Dispose();
            }
        }

        private static int RunInitialized(BabyController ctrl)
        {
            BabyModel model = BabyModel.Instance;
            model.Reset();

            MethodInfo M(string name) => ctrl.GetType().GetMethod(name, F);
            MethodInfo m18200 = M("On18200");
            MethodInfo m18201 = M("On18201");
            MethodInfo m18203 = M("On18203");
            MethodInfo m18204 = M("On18204");
            MethodInfo m18205 = M("On18205");
            MethodInfo m18206 = M("On18206");
            MethodInfo m18207 = M("On18207");
            MethodInfo m18221 = M("On18221");
            MethodInfo mGameStart = M("OnGameStart");

            bool allPass = m18200 != null && m18201 != null && m18203 != null && m18204 != null
                && m18205 != null && m18206 != null && m18207 != null && m18221 != null && mGameStart != null;
            void Check(string tag, bool ok)
            {
                Debug.Log("CLIVERIFY baby " + tag + " ok=" + ok);
                if (!ok) allPass = false;
            }
            Check("handlers", allPass);
            if (!allPass) return 3;

            FieldInfo handlersField = typeof(NetManager).GetField("_handlers", SF);
            var handlers = handlersField?.GetValue(null) as IDictionary;
            int[] registeredIds =
            {
                Proto.BABY_ERROR, Proto.BABY_BASIC_INFO, Proto.BABY_RAISE_INFO, Proto.BABY_STAGE_INFO,
                Proto.BABY_EQUIP_INFO, Proto.BABY_FIGURE_INFO, Proto.BABY_FAMILY_INFO, Proto.BABY_TASK_UPDATE
            };
            bool registrationOk = handlers != null;
            foreach (int protoId in registeredIds)
            {
                if (!registrationOk || !handlers.Contains(protoId)
                    || !(handlers[protoId] is Delegate handler)
                    || !ReferenceEquals(handler.Target, ctrl))
                {
                    registrationOk = false;
                    break;
                }
            }
            Check("runtime registration count=8", registrationOk);

            FieldInfo hubAllField = typeof(ControllerHub).GetField("ALL", SF);
            var hubAll = hubAllField?.GetValue(null) as IEnumerable;
            bool hubContainsBaby = false;
            if (hubAll != null)
            {
                foreach (object controller in hubAll)
                {
                    if (!ReferenceEquals(controller, ctrl)) continue;
                    hubContainsBaby = true;
                    break;
                }
            }
            Check("ControllerHub.ALL contains Baby", hubContainsBaby);

            FieldInfo eventHandlersField = typeof(EventDispatcher).GetField("_handlers", SF);
            var eventHandlers = eventHandlersField?.GetValue(null) as IDictionary;
            var gameStartHandlers = eventHandlers?[GlobalEvent.EVT_GAME_START] as IList;
            bool gameStartSubscribed = false;
            if (gameStartHandlers != null)
            {
                foreach (object value in gameStartHandlers)
                {
                    if (!(value is Delegate handler)
                        || !ReferenceEquals(handler.Target, ctrl)
                        || handler.Method != mGameStart) continue;
                    gameStartSubscribed = true;
                    break;
                }
            }
            Check("EVT_GAME_START subscribed OnGameStart", gameStartSubscribed);

            NetReader Feed(MethodInfo method, byte[] packet)
            {
                var reader = new NetReader(packet, 0, packet.Length);
                method.Invoke(ctrl, new object[] { reader });
                return reader;
            }
            bool TailOk(NetReader reader) => reader.ReadI32() == Tail && reader.Remaining == 0;
            bool TraceEquals(IReadOnlyList<int> actual, params int[] expected)
            {
                if (actual.Count != expected.Length) return false;
                for (int i = 0; i < expected.Length; i++)
                    if (actual[i] != expected[i]) return false;
                return true;
            }

            FieldInfo interceptField = ctrl.GetType().GetField("s_requestIntercept", SF);
            FieldInfo startupField = ctrl.GetType().GetField("_startupRequested", F);
            if (interceptField == null || startupField == null)
            {
                Debug.LogError("CLIVERIFY baby request probe missing");
                return 3;
            }

            object oldIntercept = interceptField.GetValue(null);
            bool oldStartup = (bool)startupField.GetValue(ctrl);
            var requestTrace = new List<int>();
            var updateTrace = new List<int>();
            void OnBabyUpdate(int protoId) => updateTrace.Add(protoId);
            EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
            try
            {
                Func<int, bool> intercept = protoId =>
                {
                    requestTrace.Add(protoId);
                    return true;
                };
                interceptField.SetValue(null, intercept);

                startupField.SetValue(ctrl, false);
                mGameStart.Invoke(ctrl, null);
                mGameStart.Invoke(ctrl, null);
                Check("GameStart only 18201 once", TraceEquals(requestTrace, Proto.BABY_BASIC_INFO));

                requestTrace.Clear();
                NetReader r18200 = Feed(m18200, new CliVerify.Pkt()
                    .H(Proto.BABY_TASK_UPDATE).I(-77).S("args").I(Tail).Bytes());
                Check("18200 wire/tail", TailOk(r18200)
                    && model.LastError != null && model.LastError.Command == Proto.BABY_TASK_UPDATE
                    && model.LastError.ErrorCode == -77 && model.LastError.Args == "args");

                NetReader r18201 = Feed(m18201, new CliVerify.Pkt()
                    .I(1700000000).I(8001).S("baby-a").C(1).I(Tail).Bytes());
                Check("18201 model/tail", TailOk(r18201)
                    && model.Basic != null && model.Basic.ActiveTime == 1700000000 && model.Basic.BabyId == 8001
                    && model.Basic.BabyName == "baby-a" && model.Basic.IsChangeName && model.Basic.IsActive);
                Check("18201 active query order", TraceEquals(requestTrace,
                    Proto.BABY_RAISE_INFO, Proto.BABY_STAGE_INFO, Proto.BABY_EQUIP_INFO,
                    Proto.BABY_FIGURE_INFO, Proto.BABY_FAMILY_INFO));

                requestTrace.Clear();
                NetReader r18201Inactive = Feed(m18201, new CliVerify.Pkt()
                    .I(0).I(0).S(string.Empty).C(0).I(Tail).Bytes());
                Check("18201 inactive no cascade", TailOk(r18201Inactive) && requestTrace.Count == 0
                    && model.Basic != null && !model.Basic.IsActive);

                NetReader r18203 = Feed(m18203, new CliVerify.Pkt()
                    .H(9).I(1234).H(1).H(51).H(3).C(0).I(777).I(Tail).Bytes());
                Check("18203 task/tail", TailOk(r18203) && model.Raise != null
                    && model.Raise.RaiseLevel == 9 && model.Raise.RaiseExp == 1234 && model.Raise.Power == 777
                    && model.Raise.TaskList.Count == 1 && model.Raise.TaskList[0].TaskId == 51
                    && model.Raise.TaskList[0].FinishNum == 3 && model.Raise.TaskList[0].FinishState == 0);
                BabyTaskInfo existingTask = model.Raise.TaskList[0];

                NetReader r18204 = Feed(m18204, new CliVerify.Pkt()
                    .H(12).C(7).I(555).I(888).I(Tail).Bytes());
                Check("18204 wire/tail", TailOk(r18204) && model.Stage != null
                    && model.Stage.Stage == 12 && model.Stage.StageLevel == 7
                    && model.Stage.StageExp == 555 && model.Stage.Power == 888);

                const long equipId = 0x00000012ABCDEF34L;
                NetReader r18205 = Feed(m18205, new CliVerify.Pkt()
                    .H(1).C(4).L(equipId).I(7001).H(33).H(513).I(99).I(42).I(999).I(Tail).Bytes());
                Check("18205 u64/u16/tail", TailOk(r18205) && model.Equip != null
                    && model.Equip.Power == 999 && model.Equip.EquipList.Count == 1
                    && model.Equip.EquipList[0].PositionId == 4 && model.Equip.EquipList[0].Id == equipId
                    && model.Equip.EquipList[0].GoodsTypeId == 7001 && model.Equip.EquipList[0].Stage == 33
                    && model.Equip.EquipList[0].StageLevel == 513 && model.Equip.EquipList[0].StageExp == 99
                    && model.Equip.EquipList[0].SkillId == 42);

                NetReader r18206 = Feed(m18206, new CliVerify.Pkt()
                    .H(2).I(8001).H(2).I(8002).H(5).I(Tail).Bytes());
                Check("18206 array/tail", TailOk(r18206) && model.Figures != null
                    && model.Figures.ActiveList.Count == 2 && model.Figures.ActiveList[0].BabyId == 8001
                    && model.Figures.ActiveList[1].BabyStar == 5);

                NetReader r18207 = Feed(m18207, new CliVerify.Pkt()
                    .H(2)
                    .L(101).I(1700000101).I(8101).S("family-a").H(11).H(21).C(3).I(10001)
                        .H(2)
                            .C(1).H(2).H(101).I(1000).H(102).I(2000)
                            .C(2).H(1).H(201).I(3000)
                    .L(202).I(1700000202).I(8202).S("family-b").H(12).H(22).C(4).I(20002)
                        .H(1).C(3).H(0)
                    .I(Tail).Bytes());
                Check("18207 nested/reverse/tail", TailOk(r18207) && model.Family != null
                    && model.Family.InfoList.Count == 2
                    && model.Family.InfoList[0].RoleId == 202 && model.Family.InfoList[0].BabyName == "family-b"
                    && model.Family.InfoList[1].RoleId == 101 && model.Family.InfoList[1].AttrInfo.Count == 2
                    && model.Family.InfoList[1].AttrInfo[0].AttrList.Count == 2
                    && model.Family.InfoList[1].AttrInfo[0].AttrList[1].AttrId == 102
                    && model.Family.InfoList[1].AttrInfo[1].AttrList[0].Value == 3000);

                updateTrace.Clear();
                NetReader r18221Progress = Feed(m18221, new CliVerify.Pkt()
                    .H(51).H(8).C(0).I(Tail).Bytes());
                Check("18221 update existing in place", TailOk(r18221Progress)
                    && ReferenceEquals(existingTask, model.Raise.TaskList[0])
                    && existingTask.FinishNum == 8 && existingTask.FinishState == 0
                    && TraceEquals(updateTrace, Proto.BABY_TASK_UPDATE));

                requestTrace.Clear();
                updateTrace.Clear();
                NetReader r18221Finish = Feed(m18221, new CliVerify.Pkt()
                    .H(51).H(99).C(1).I(Tail).Bytes());
                Check("18221 finished only refetch 18203", TailOk(r18221Finish)
                    && TraceEquals(requestTrace, Proto.BABY_RAISE_INFO)
                    && updateTrace.Count == 0
                    && existingTask.FinishNum == 8 && existingTask.FinishState == 0);
            }
            finally
            {
                EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
                interceptField.SetValue(null, oldIntercept);
                startupField.SetValue(ctrl, oldStartup);
                model.Reset();
            }

            Debug.Log("CLIVERIFY baby RESULT " + (allPass ? "PASS" : "FAIL"));
            return allPass ? 0 : 3;
        }

        public static void RunBatch()
        {
            _ = RunBatchAsync();
        }

        private static async Task RunBatchAsync()
        {
            int code;
            try
            {
                code = await Run();
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY baby EXCEPTION " + e);
                code = 1;
            }
            UnityEditor.EditorApplication.Exit(code);
        }
    }
}
