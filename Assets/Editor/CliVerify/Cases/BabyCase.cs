using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Baby;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// Baby(pt_182) 数据态验证：运行时注册、GameStart 单次首拉、18201 激活后的严格查询顺序，
    /// 15 个入站协议的字段位宽、双 u64、嵌套数组、失败不污染、尾哨兵，以及第二包 7 个 C2S 的真实编码。
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
            FieldInfo[] stateFields = typeof(BabyModel).GetFields(F);
            object[] savedState = new object[stateFields.Length];
            for (int i = 0; i < stateFields.Length; i++) savedState[i] = stateFields[i].GetValue(model);
            try
            {
                model.Reset();
                return RunIsolated(ctrl, model);
            }
            finally
            {
                model.Reset();
                for (int i = 0; i < stateFields.Length; i++) stateFields[i].SetValue(model, savedState[i]);
            }
        }

        private static int RunIsolated(BabyController ctrl, BabyModel model)
        {
            MethodInfo M(string name) => ctrl.GetType().GetMethod(name, F);
            MethodInfo m18200 = M("On18200");
            MethodInfo m18201 = M("On18201");
            MethodInfo m18203 = M("On18203");
            MethodInfo m18204 = M("On18204");
            MethodInfo m18205 = M("On18205");
            MethodInfo m18206 = M("On18206");
            MethodInfo m18207 = M("On18207");
            MethodInfo m18208 = M("On18208");
            MethodInfo m18209 = M("On18209");
            MethodInfo m18210 = M("On18210");
            MethodInfo m18211 = M("On18211");
            MethodInfo m18213 = M("On18213");
            MethodInfo m18214 = M("On18214");
            MethodInfo m18215 = M("On18215");
            MethodInfo m18217 = M("On18217");
            MethodInfo m18221 = M("On18221");
            MethodInfo m18222 = M("On18222");
            MethodInfo m18223 = M("On18223");
            MethodInfo m18224 = M("On18224");
            MethodInfo mGameStart = M("OnGameStart");

            bool allPass = m18200 != null && m18201 != null && m18203 != null && m18204 != null
                && m18205 != null && m18206 != null && m18207 != null && m18208 != null && m18209 != null
                && m18210 != null && m18211 != null && m18213 != null && m18214 != null && m18215 != null
                && m18217 != null && m18221 != null && m18222 != null && m18223 != null && m18224 != null && mGameStart != null;
            void Check(string tag, bool ok)
            {
                Debug.Log("CLIVERIFY baby " + tag + " ok=" + ok);
                if (!ok) allPass = false;
            }
            Check("handlers", allPass);
            if (!allPass) return 3;

            FieldInfo handlersField = typeof(NetManager).GetField("_handlers", SF);
            var handlers = handlersField?.GetValue(null) as IDictionary;
            var registeredRoutes = new Dictionary<int, MethodInfo>
            {
                [Proto.BABY_ERROR] = m18200,
                [Proto.BABY_BASIC_INFO] = m18201,
                [Proto.BABY_RAISE_INFO] = m18203,
                [Proto.BABY_STAGE_INFO] = m18204,
                [Proto.BABY_EQUIP_INFO] = m18205,
                [Proto.BABY_FIGURE_INFO] = m18206,
                [Proto.BABY_FAMILY_INFO] = m18207,
                [Proto.BABY_LIKE_RANK] = m18208,
                [Proto.BABY_LIKE_RECORDS] = m18209,
                [Proto.BABY_ACTIVATE] = m18210,
                [Proto.BABY_STAGE_UP] = m18211,
                [Proto.BABY_FIGURE_STAR_UP] = m18213,
                [Proto.BABY_FIGURE_WEAR] = m18214,
                [Proto.BABY_RENAME] = m18215,
                [Proto.BABY_PRAISE] = m18217,
                [Proto.BABY_TASK_UPDATE] = m18221,
                [Proto.BABY_TASK_REWARD] = m18222,
                [Proto.BABY_FIGURE_POWER] = m18223,
                [Proto.BABY_PRAISE_PUSH] = m18224
            };
            int actualRouteCount = 0;
            if (handlers != null)
            {
                foreach (DictionaryEntry entry in handlers)
                    if (entry.Value is Delegate value && ReferenceEquals(value.Target, ctrl)) actualRouteCount++;
            }
            bool registrationOk = handlers != null && actualRouteCount == registeredRoutes.Count;
            foreach (KeyValuePair<int, MethodInfo> route in registeredRoutes)
            {
                if (!registrationOk || !handlers.Contains(route.Key)
                    || !(handlers[route.Key] is Delegate handler)
                    || !ReferenceEquals(handler.Target, ctrl)
                    || handler.Method != route.Value)
                {
                    registrationOk = false;
                    break;
                }
            }
            Check("runtime registration count=19", registrationOk);

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
            bool RequestTraceEquals(IReadOnlyList<byte[]> actual, params int[] expected)
            {
                if (actual.Count != expected.Length) return false;
                for (int i = 0; i < expected.Length; i++)
                    if (!FrameEquals(actual[i], expected[i], Array.Empty<byte>())) return false;
                return true;
            }
            bool TraceEquals(IReadOnlyList<int> actual, params int[] expected)
            {
                if (actual.Count != expected.Length) return false;
                for (int i = 0; i < expected.Length; i++)
                    if (actual[i] != expected[i]) return false;
                return true;
            }

            FieldInfo interceptField = ctrl.GetType().GetField("s_outboundIntercept", SF);
            FieldInfo startupField = ctrl.GetType().GetField("_startupRequested", F);
            if (interceptField == null || startupField == null)
            {
                Debug.LogError("CLIVERIFY baby request probe missing");
                return 3;
            }

            object oldIntercept = interceptField.GetValue(null);
            bool oldStartup = (bool)startupField.GetValue(ctrl);
            long oldRoleId = RoleModel.Instance.RoleId;
            var requestTrace = new List<byte[]>();
            var updateTrace = new List<int>();
            void OnBabyUpdate(int protoId) => updateTrace.Add(protoId);
            EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
            try
            {
                Func<byte[], bool> intercept = frame =>
                {
                    requestTrace.Add(frame);
                    return true;
                };
                interceptField.SetValue(null, intercept);

                startupField.SetValue(ctrl, false);
                mGameStart.Invoke(ctrl, null);
                mGameStart.Invoke(ctrl, null);
                Check("GameStart only 18201 once", RequestTraceEquals(requestTrace, Proto.BABY_BASIC_INFO));

                requestTrace.Clear();
                ctrl.RequestActivate();
                bool c2s18210 = requestTrace.Count == 1
                    && FrameEquals(requestTrace[0], Proto.BABY_ACTIVATE, Array.Empty<byte>());
                requestTrace.Clear();
                ctrl.RequestStageUp();
                bool c2s18211 = requestTrace.Count == 1
                    && FrameEquals(requestTrace[0], Proto.BABY_STAGE_UP, Array.Empty<byte>());
                requestTrace.Clear();
                ctrl.RequestFigureStarUp(0x01020304);
                bool c2s18213 = requestTrace.Count == 1
                    && FrameEquals(requestTrace[0], Proto.BABY_FIGURE_STAR_UP,
                        new CliVerify.Pkt().I(0x01020304).Bytes());
                requestTrace.Clear();
                ctrl.RequestSetFigure(2, 0x11121314);
                bool c2s18214 = requestTrace.Count == 1
                    && FrameEquals(requestTrace[0], Proto.BABY_FIGURE_WEAR,
                        new CliVerify.Pkt().C(2).I(0x11121314).Bytes());
                requestTrace.Clear();
                ctrl.RequestRename("baby-name");
                bool c2s18215 = requestTrace.Count == 1
                    && FrameEquals(requestTrace[0], Proto.BABY_RENAME,
                        new CliVerify.Pkt().S("baby-name").Bytes());
                requestTrace.Clear();
                ctrl.RequestTaskReward(0x1234);
                bool c2s18222 = requestTrace.Count == 1
                    && FrameEquals(requestTrace[0], Proto.BABY_TASK_REWARD,
                        new CliVerify.Pkt().H(0x1234).Bytes());
                requestTrace.Clear();
                ctrl.RequestFigurePower(0x21222324);
                bool c2s18223 = requestTrace.Count == 1
                    && FrameEquals(requestTrace[0], Proto.BABY_FIGURE_POWER,
                        new CliVerify.Pkt().I(0x21222324).Bytes());
                requestTrace.Clear();
                ctrl.RequestLikeRank();
                bool c2s18208 = RequestTraceEquals(requestTrace, Proto.BABY_LIKE_RANK);
                requestTrace.Clear();
                ctrl.RequestLikeRecords();
                bool c2s18209 = RequestTraceEquals(requestTrace, Proto.BABY_LIKE_RECORDS);
                requestTrace.Clear();
                ctrl.RequestPraise(0x0102030405060708L, 2);
                bool c2s18217 = requestTrace.Count == 1 && FrameEquals(requestTrace[0], Proto.BABY_PRAISE,
                    new CliVerify.Pkt().L(0x0102030405060708L).C(2).Bytes());
                requestTrace.Clear();
                ctrl.RequestSetFigure(0, 1);
                ctrl.RequestSetFigure(3, 1);
                ctrl.RequestTaskReward(0);
                ctrl.RequestTaskReward(ushort.MaxValue + 1);
                ctrl.RequestFigureStarUp(0);
                ctrl.RequestFigurePower(0);
                ctrl.RequestPraise(0, 2);
                ctrl.RequestPraise(1, 0);
                ctrl.RequestPraise(1, 3);
                bool c2sGuards = requestTrace.Count == 0;
                Check("second packet C2S exact wire/guards", c2s18210 && c2s18211 && c2s18213
                    && c2s18214 && c2s18215 && c2s18222 && c2s18223 && c2s18208 && c2s18209 && c2s18217 && c2sGuards);

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
                Check("18201 active query order", RequestTraceEquals(requestTrace,
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

                requestTrace.Clear();
                NetReader seedBasic = Feed(m18201, new CliVerify.Pkt()
                    .I(1700000303).I(8100).S("origin").C(0).I(Tail).Bytes());
                Check("second packet basic seed/tail", TailOk(seedBasic) && model.Basic != null
                    && model.Basic.BabyId == 8100 && model.Basic.BabyName == "origin" && !model.Basic.IsChangeName);

                requestTrace.Clear();
                updateTrace.Clear();
                NetReader r18210Fail = Feed(m18210, new CliVerify.Pkt().I(5).I(Tail).Bytes());
                Check("18210 fail no pollution/tail", TailOk(r18210Fail)
                    && model.LastActivateResult != null && model.LastActivateResult.Code == 5
                    && model.Basic.BabyId == 8100 && requestTrace.Count == 0
                    && TraceEquals(updateTrace, Proto.BABY_ACTIVATE));

                requestTrace.Clear();
                updateTrace.Clear();
                NetReader r18210Ok = Feed(m18210, new CliVerify.Pkt().I(1).I(Tail).Bytes());
                Check("18210 success only refetch 18201", TailOk(r18210Ok)
                    && model.LastActivateResult != null && model.LastActivateResult.Succeeded
                    && RequestTraceEquals(requestTrace, Proto.BABY_BASIC_INFO)
                    && TraceEquals(updateTrace, Proto.BABY_ACTIVATE));

                BabyStageInfo stageBeforeFail = model.Stage;
                updateTrace.Clear();
                NetReader r18211Fail = Feed(m18211, new CliVerify.Pkt()
                    .I(5).H(99).C(88).I(7777).I(6666).I(Tail).Bytes());
                Check("18211 fail no pollution/tail", TailOk(r18211Fail)
                    && model.LastStageUpResult != null && model.LastStageUpResult.Code == 5
                    && ReferenceEquals(model.Stage, stageBeforeFail) && model.Stage.Stage == 12
                    && TraceEquals(updateTrace, Proto.BABY_STAGE_UP));

                updateTrace.Clear();
                NetReader r18211Ok = Feed(m18211, new CliVerify.Pkt()
                    .I(1).H(13).C(8).I(556).I(889).I(Tail).Bytes());
                Check("18211 success replaces stage/tail", TailOk(r18211Ok)
                    && model.LastStageUpResult != null && model.LastStageUpResult.Succeeded
                    && model.Stage != null && model.Stage.Stage == 13 && model.Stage.StageLevel == 8
                    && model.Stage.StageExp == 556 && model.Stage.Power == 889
                    && TraceEquals(updateTrace, Proto.BABY_STAGE_UP));

                NetReader emptyFigures = Feed(m18206, new CliVerify.Pkt().H(0).I(Tail).Bytes());
                Check("18213 empty figure seed", TailOk(emptyFigures)
                    && model.Figures != null && model.Figures.ActiveList.Count == 0);

                const long failPower = 0x0101010102020202L;
                const long failNextPower = 0x0303030304040404L;
                requestTrace.Clear();
                updateTrace.Clear();
                NetReader r18213Fail = Feed(m18213, new CliVerify.Pkt()
                    .I(5).I(9001).H(9).L(failPower).L(failNextPower).I(Tail).Bytes());
                Check("18213 fail no pollution/double long/tail", TailOk(r18213Fail)
                    && model.LastFigureStarResult != null && model.LastFigureStarResult.Code == 5
                    && model.LastFigureStarResult.Power == failPower
                    && model.LastFigureStarResult.NextPower == failNextPower
                    && model.Figures.ActiveList.Count == 0 && requestTrace.Count == 0
                    && TraceEquals(updateTrace, Proto.BABY_FIGURE_STAR_UP));

                long starPower = unchecked((long)0xF112131415161718UL);
                const long starNextPower = 0x2122232425262728L;
                requestTrace.Clear();
                updateTrace.Clear();
                NetReader r18213First = Feed(m18213, new CliVerify.Pkt()
                    .I(1).I(9001).H(1).L(starPower).L(starNextPower).I(Tail).Bytes());
                BabyFigureEntry figure = model.FindFigure(9001);
                bool firstFigureRequests = requestTrace.Count == 2
                    && FrameEquals(requestTrace[0], Proto.BABY_FIGURE_INFO, Array.Empty<byte>())
                    && FrameEquals(requestTrace[1], Proto.BABY_FIGURE_WEAR,
                        new CliVerify.Pkt().C(1).I(9001).Bytes());
                Check("18213 first activation/refetch/wear/tail", TailOk(r18213First)
                    && figure != null && figure.BabyStar == 1 && figure.IsActivated
                    && figure.Power == starPower && figure.NextPower == starNextPower
                    && model.LastFigureStarResult != null && model.LastFigureStarResult.NextPower == starNextPower
                    && firstFigureRequests && TraceEquals(updateTrace, Proto.BABY_FIGURE_STAR_UP));

                NetReader figureRefetch = Feed(m18206, new CliVerify.Pkt()
                    .H(1).I(9001).H(1).I(Tail).Bytes());
                figure = model.FindFigure(9001);
                Check("18206 preserves 18213 powers", TailOk(figureRefetch) && figure != null
                    && figure.Power == starPower && figure.NextPower == starNextPower);

                const long starPower2 = 0x3132333435363738L;
                const long starNextPower2 = 0x4142434445464748L;
                requestTrace.Clear();
                NetReader r18213Existing = Feed(m18213, new CliVerify.Pkt()
                    .I(1).I(9001).H(2).L(starPower2).L(starNextPower2).I(Tail).Bytes());
                figure = model.FindFigure(9001);
                Check("18213 existing merge no refetch/tail", TailOk(r18213Existing)
                    && requestTrace.Count == 0 && figure != null && figure.BabyStar == 2
                    && figure.Power == starPower2 && figure.NextPower == starNextPower2);

                updateTrace.Clear();
                NetReader r18214Fail = Feed(m18214, new CliVerify.Pkt()
                    .I(5).C(2).I(9001).I(Tail).Bytes());
                Check("18214 fail no pollution/tail", TailOk(r18214Fail)
                    && model.LastFigureWearResult != null && model.LastFigureWearResult.Code == 5
                    && model.Basic.BabyId == 8100 && TraceEquals(updateTrace, Proto.BABY_FIGURE_WEAR));

                NetReader r18214Off = Feed(m18214, new CliVerify.Pkt()
                    .I(1).C(2).I(9001).I(Tail).Bytes());
                Check("18214 type2 clears current/tail", TailOk(r18214Off) && model.Basic.BabyId == 0);
                NetReader r18214On = Feed(m18214, new CliVerify.Pkt()
                    .I(1).C(1).I(9001).I(Tail).Bytes());
                Check("18214 type1 sets current/tail", TailOk(r18214On) && model.Basic.BabyId == 9001);

                updateTrace.Clear();
                NetReader r18215Fail = Feed(m18215, new CliVerify.Pkt()
                    .I(5).S("bad-name").I(Tail).Bytes());
                Check("18215 fail no pollution/tail", TailOk(r18215Fail)
                    && model.LastRenameResult != null && model.LastRenameResult.Code == 5
                    && model.Basic.BabyName == "origin" && !model.Basic.IsChangeName
                    && TraceEquals(updateTrace, Proto.BABY_RENAME));
                NetReader r18215Ok = Feed(m18215, new CliVerify.Pkt()
                    .I(1).S("renamed").I(Tail).Bytes());
                Check("18215 success rename/tail", TailOk(r18215Ok)
                    && model.Basic.BabyName == "renamed" && model.Basic.IsChangeName);

                requestTrace.Clear();
                updateTrace.Clear();
                NetReader r18222Fail = Feed(m18222, new CliVerify.Pkt()
                    .I(5).H(51).H(99).C(1).I(Tail).Bytes());
                Check("18222 fail still refetch/no pollution/tail", TailOk(r18222Fail)
                    && model.LastTaskRewardResult != null && model.LastTaskRewardResult.Code == 5
                    && ReferenceEquals(existingTask, model.Raise.TaskList[0])
                    && existingTask.FinishNum == 3 && existingTask.FinishState == 0
                    && RequestTraceEquals(requestTrace, Proto.BABY_RAISE_INFO)
                    && TraceEquals(updateTrace, Proto.BABY_TASK_REWARD));

                requestTrace.Clear();
                updateTrace.Clear();
                NetReader r18222Ok = Feed(m18222, new CliVerify.Pkt()
                    .I(1).H(51).H(3).C(1).I(Tail).Bytes());
                Check("18222 success refetch/tail", TailOk(r18222Ok)
                    && model.LastTaskRewardResult != null && model.LastTaskRewardResult.Succeeded
                    && RequestTraceEquals(requestTrace, Proto.BABY_RAISE_INFO)
                    && TraceEquals(updateTrace, Proto.BABY_TASK_REWARD));

                const long rawPower = 0x5152535455565758L;
                long rawNextPower = unchecked((long)0xF162636465666768UL);
                requestTrace.Clear();
                updateTrace.Clear();
                NetReader r18223 = Feed(m18223, new CliVerify.Pkt()
                    .I(9001).H(7).L(rawPower).L(rawNextPower).I(Tail).Bytes());
                figure = model.FindFigure(9001);
                Check("18223 existing raw star/u64/activation/tail", TailOk(r18223)
                    && model.LastFigurePowerResult != null && model.LastFigurePowerResult.BabyStar == 7
                    && model.LastFigurePowerResult.IsActivated
                    && model.LastFigurePowerResult.Power == rawPower
                    && model.LastFigurePowerResult.NextPower == rawNextPower
                    && figure != null && figure.BabyStar == 2 && figure.IsActivated
                    && figure.Power == rawPower && figure.NextPower == rawNextPower
                    && requestTrace.Count == 0 && TraceEquals(updateTrace, Proto.BABY_FIGURE_POWER));

                NetReader clearFigures = Feed(m18206, new CliVerify.Pkt().H(0).I(Tail).Bytes());
                const long previewPower = 0x7172737475767778L;
                const long previewNextPower = 0x0102030405060708L;
                updateTrace.Clear();
                NetReader r18223Preview = Feed(m18223, new CliVerify.Pkt()
                    .I(9900).H(1).L(previewPower).L(previewNextPower).I(Tail).Bytes());
                Check("18223 preview does not activate/create", TailOk(clearFigures) && TailOk(r18223Preview)
                    && model.LastFigurePowerResult != null && model.LastFigurePowerResult.BabyId == 9900
                    && model.LastFigurePowerResult.BabyStar == 1 && !model.LastFigurePowerResult.IsActivated
                    && model.LastFigurePowerResult.Power == previewPower
                    && model.LastFigurePowerResult.NextPower == previewNextPower
                    && model.FindFigure(9900) == null && model.Figures.ActiveList.Count == 0
                    && TraceEquals(updateTrace, Proto.BABY_FIGURE_POWER));

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
                    && RequestTraceEquals(requestTrace, Proto.BABY_RAISE_INFO)
                    && updateTrace.Count == 0
                    && existingTask.FinishNum == 8 && existingTask.FinishState == 0);

                updateTrace.Clear();
                NetReader r18208 = Feed(m18208, new CliVerify.Pkt().L(99).H(2)
                    .L(101).S("rank-a").I(700).I(12)
                    .L(202).S("rank-b").I(800).I(13).I(Tail).Bytes());
                Check("18208 u64/string/u32 list/tail", TailOk(r18208) && model.PraiseRank != null
                    && model.PraiseRank.RoleId == 99 && model.PraiseRank.Entries.Count == 2
                    && model.PraiseRank.Entries[1].Name == "rank-b" && model.PraiseRank.Entries[1].PraiseNum == 13
                    && TraceEquals(updateTrace, Proto.BABY_LIKE_RANK));

                updateTrace.Clear();
                NetReader r18209Pending = Feed(m18209, new CliVerify.Pkt().H(2)
                    .L(301).S("fan-a").C(0).L(302).S("fan-b").C(1).I(Tail).Bytes());
                Check("18209 pending red/tail", TailOk(r18209Pending) && model.PraiseRecords != null
                    && model.PraiseRecords.Entries.Count == 2 && model.BabyLikeRed
                    && TraceEquals(updateTrace, Proto.BABY_LIKE_RECORDS));
                updateTrace.Clear();
                NetReader r18209AllBack = Feed(m18209, new CliVerify.Pkt().H(1)
                    .L(301).S("fan-a").C(1).I(Tail).Bytes());
                Check("18209 all back clears red/tail", TailOk(r18209AllBack) && !model.BabyLikeRed
                    && TraceEquals(updateTrace, Proto.BABY_LIKE_RECORDS));

                RoleModel.Instance.RoleId = 777;
                updateTrace.Clear();
                NetReader r18224Self = Feed(m18224, new CliVerify.Pkt().L(777).I(Tail).Bytes());
                Check("18224 self filtered/tail", TailOk(r18224Self) && !model.BabyLikeRed && updateTrace.Count == 0);
                updateTrace.Clear();
                NetReader r18224Other = Feed(m18224, new CliVerify.Pkt().L(778).I(Tail).Bytes());
                Check("18224 other red/tail", TailOk(r18224Other) && model.LastPraisePush != null
                    && model.LastPraisePush.PraiserId == 778 && model.BabyLikeRed
                    && TraceEquals(updateTrace, Proto.BABY_PRAISE_PUSH));

                requestTrace.Clear();
                updateTrace.Clear();
                NetReader r18217Ok = Feed(m18217, new CliVerify.Pkt().I(1).L(101).C(2).H(1)
                    .C(3).I(68010001).I(2).I(Tail).Bytes());
                Check("18217 success reward/refetch/tail", TailOk(r18217Ok) && model.LastPraiseAction != null
                    && model.LastPraiseAction.Succeeded && model.LastPraiseAction.Rewards.Count == 1
                    && model.LastPraiseAction.Rewards[0].TypeId == 68010001
                    && RequestTraceEquals(requestTrace, Proto.BABY_LIKE_RECORDS)
                    && TraceEquals(updateTrace, Proto.BABY_PRAISE));
                requestTrace.Clear();
                NetReader r18217Fail = Feed(m18217, new CliVerify.Pkt().I(5).L(101).C(2).H(0).I(Tail).Bytes());
                NetReader r18217Opr1 = Feed(m18217, new CliVerify.Pkt().I(1).L(101).C(1).H(0).I(Tail).Bytes());
                Check("18217 failed or opr1 no refetch/tail", TailOk(r18217Fail) && TailOk(r18217Opr1)
                    && requestTrace.Count == 0 && model.LastPraiseAction != null && model.LastPraiseAction.Opr == 1);
            }
            finally
            {
                EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
                interceptField.SetValue(null, oldIntercept);
                startupField.SetValue(ctrl, oldStartup);
                RoleModel.Instance.RoleId = oldRoleId;
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
