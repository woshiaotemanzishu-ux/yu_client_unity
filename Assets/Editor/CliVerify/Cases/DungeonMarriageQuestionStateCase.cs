using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Dungeon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>61089 伴侣副本答题状态专项：纯 S2C、11B 原始整包替换与 ambient 精确恢复。</summary>
    public static class DungeonMarriageQuestionStateCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags ALL = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-marriage-question-state EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            DungeonController controller = DungeonController.Instance;
            DungeonModel model = DungeonModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            bool oldHasState = model.HasMarriageQuestionState;
            DungeonModel.MarriageQuestionSnapshot oldState = model.LastMarriageQuestionState;
            DungeonModel.AdvancedExpJumpInfoSnapshot oldSentinel = model.LastAdvancedExpJumpInfo;
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            bool oldHandlerExists = handlers != null
                && handlers.Contains(Proto.DUNGEON_MARRIAGE_QUESTION_STATE);
            object oldHandler = oldHandlerExists
                ? handlers[Proto.DUNGEON_MARRIAGE_QUESTION_STATE]
                : null;
            bool pass = false;
            bool restored = false;

            try
            {
                RestoreModelProperty(model, "HasMarriageQuestionState", false);
                RestoreModelProperty(model, "LastMarriageQuestionState", null);

                MethodInfo on61089 = typeof(DungeonController).GetMethod("On61089", IF);
                MethodInfo request = typeof(DungeonController).GetMethod(
                    "RequestMarriageQuestionState", ALL);
                FieldInfo intercept = typeof(DungeonController).GetField(
                    "s_marriageQuestionStateOutboundIntercept", ALL);
                pass = Proto.DUNGEON_MARRIAGE_QUESTION_STATE == 61089
                    && on61089 != null && request == null && intercept == null
                    && (!oldInitialized || oldHandlerExists);
                Check(ref pass, "s2c-only seams/registration/no-sender", pass);
                if (on61089 == null) throw new MissingMethodException("On61089 seam missing");

                Check(ref pass, "no response keeps clear",
                    !model.HasMarriageQuestionState && model.LastMarriageQuestionState == null);

                Check(ref pass, "type1 max 11B/read-to-end",
                    Feed(on61089, controller, uint.MaxValue, ushort.MaxValue, 1, uint.MaxValue)
                    && State(model, uint.MaxValue, ushort.MaxValue, 1, uint.MaxValue));
                DungeonModel.MarriageQuestionSnapshot type1 = model.LastMarriageQuestionState;

                Check(ref pass, "type2 close whole replace/read-to-end",
                    Feed(on61089, controller, 7, 8, 2, 0)
                    && State(model, 7, 8, 2, 0)
                    && !ReferenceEquals(model.LastMarriageQuestionState, type1));
                DungeonModel.MarriageQuestionSnapshot type2 = model.LastMarriageQuestionState;

                Check(ref pass, "zero fields/unknown type whole replace/read-to-end",
                    Feed(on61089, controller, 0, 0, byte.MaxValue, 0)
                    && State(model, 0, 0, byte.MaxValue, 0)
                    && !ReferenceEquals(model.LastMarriageQuestionState, type2));

                Check(ref pass, "handler/init ambient untouched during run",
                    controller.IsInitialized == oldInitialized
                    && ReferenceEquals(model.LastAdvancedExpJumpInfo, oldSentinel)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler));
                Debug.Log("CLIVERIFY dungeon-marriage-question-state VERDICT pass=" + pass);
            }
            finally
            {
                RestoreModelProperty(model, "HasMarriageQuestionState", oldHasState);
                RestoreModelProperty(model, "LastMarriageQuestionState", oldState);

                restored = controller.IsInitialized == oldInitialized
                    && model.HasMarriageQuestionState == oldHasState
                    && ReferenceEquals(model.LastMarriageQuestionState, oldState)
                    && ReferenceEquals(model.LastAdvancedExpJumpInfo, oldSentinel)
                    && HandlerUnchanged(handlers, oldHandlerExists, oldHandler);
                Debug.Log("CLIVERIFY dungeon-marriage-question-state restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool Feed(MethodInfo handler, DungeonController controller,
            uint dunId, ushort questionId, byte type, uint endTime)
        {
            byte[] bytes = new CliVerify.Pkt().I(dunId).H(questionId).C(type).I(endTime).Bytes();
            if (bytes.Length != 11) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool State(DungeonModel model, uint dunId, ushort questionId,
            byte type, uint endTime)
        {
            DungeonModel.MarriageQuestionSnapshot actual = model.LastMarriageQuestionState;
            return model.HasMarriageQuestionState && actual != null
                && actual.DunId == dunId && actual.QuestionId == questionId
                && actual.Type == type && actual.EndTime == endTime;
        }

        private static bool HandlerUnchanged(IDictionary handlers, bool existed, object value)
        {
            return handlers != null
                && handlers.Contains(Proto.DUNGEON_MARRIAGE_QUESTION_STATE) == existed
                && (!existed || ReferenceEquals(
                    handlers[Proto.DUNGEON_MARRIAGE_QUESTION_STATE], value));
        }

        private static void RestoreModelProperty(DungeonModel model, string propertyName, object value)
        {
            typeof(DungeonModel).GetProperty(
                propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(model, value);
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-marriage-question-state " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
