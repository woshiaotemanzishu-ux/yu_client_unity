using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Tasks;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>30004 主线完成号的单调推进与事件语义专项。</summary>
    public static class TaskNewestFinishCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private const string ConfigPath = "Assets/GameRes/resource/config/server/config_task.json";

        public static Task<int> Run() => Task.FromResult(RunCore());
        public static void RunBatch() => _ = RunBatchAsync();

        private static async Task RunBatchAsync()
        {
            int code = 3;
            try
            {
                code = await Run();
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY task-newest-finish EXCEPTION " + exception);
                code = 3;
            }
            finally
            {
                Debug.Log("CLIVERIFY task-newest-finish EXIT " + code);
                EditorApplication.Exit(code);
            }
        }

        private static int RunCore()
        {
            TaskController controller = TaskController.Instance;
            TaskModel model = TaskModel.Instance;
            FieldInfo taskField = typeof(TaskConfigs).GetField("_task", StaticPrivate);
            FieldInfo cacheField = typeof(TaskConfigs).GetField("_cache", StaticPrivate);
            FieldInfo eventHandlersField = typeof(EventDispatcher).GetField("_handlers", StaticPrivate);
            FieldInfo netHandlersField = typeof(NetManager).GetField("_handlers", StaticPrivate);
            FieldInfo effectField = typeof(TaskController).GetField("_taskSuccessEffect", InstancePrivate);
            FieldInfo effectEpochField = typeof(TaskController).GetField("_taskSuccessEffectEpoch", InstancePrivate);
            FieldInfo pendingAutoField = typeof(TaskController).GetField("_taskFinishPendingAuto", InstancePrivate);
            FieldInfo oneAutoEpochField = typeof(TaskController).GetField("_taskOneAutoEpoch", InstancePrivate);
            FieldInfo loginKickoffField = typeof(TaskController).GetField("_loginKickoffDone", InstancePrivate);
            FieldInfo startupTaskListField = typeof(TaskController).GetField("_startupTaskListRequested", InstancePrivate);
            MethodInfo on30004 = typeof(TaskController).GetMethod("On30004", InstancePrivate);
            MethodInfo clearEffect = typeof(TaskController).GetMethod("ClearTaskSuccessEffect", InstancePrivate);
            bool seams = taskField != null && cacheField != null && eventHandlersField != null && netHandlersField != null
                && effectField != null && effectEpochField != null && pendingAutoField != null && oneAutoEpochField != null
                && loginKickoffField != null && startupTaskListField != null
                && on30004 != null && clearEffect != null;
            if (!seams)
            {
                Debug.LogError("CLIVERIFY task-newest-finish reflection seam missing");
                return 3;
            }

            IDictionary cache = cacheField.GetValue(null) as IDictionary;
            IDictionary eventHandlers = eventHandlersField.GetValue(null) as IDictionary;
            IDictionary netHandlers = netHandlersField.GetValue(null) as IDictionary;
            JObject oldTask = taskField.GetValue(null) as JObject;
            var oldCache = Copy(cache);
            var oldEvents = Copy(eventHandlers);
            int oldNewest = model.NewestFinishTaskId;
            object oldEffect = effectField.GetValue(controller);
            int oldEffectEpoch = (int)effectEpochField.GetValue(controller);
            bool oldPendingAuto = (bool)pendingAutoField.GetValue(controller);
            int oldOneAutoEpoch = (int)oneAutoEpochField.GetValue(controller);
            bool oldLoginKickoff = (bool)loginKickoffField.GetValue(controller);
            bool oldStartupTaskList = (bool)startupTaskListField.GetValue(controller);
            bool isolated = !controller.IsInitialized && oldTask == null && cache != null && cache.Count == 0
                && eventHandlers != null && eventHandlers.Count == 0
                && netHandlers != null && !netHandlers.Contains(Proto.CC_TASK_FINISH)
                && ViewManager.GetLayer(UILayer.Top) == null
                && IsDefaultModel(model)
                && oldEffect == null && oldEffectEpoch == 0 && !oldPendingAuto && oldOneAutoEpoch == 0
                && !oldLoginKickoff && !oldStartupTaskList;
            bool restored = false;
            bool pass = false;
            int result = 3;
            Action updated = null;

            try
            {
                if (!isolated)
                {
                    Debug.LogWarning("CLIVERIFY task-newest-finish SKIP=2: ambient controller/config/event/model/effect/UI runtime");
                    result = 2;
                }
                else
                {
                    string fullConfigPath = Path.GetFullPath(ConfigPath);
                    if (!File.Exists(fullConfigPath))
                    {
                        Debug.LogError("CLIVERIFY task-newest-finish config missing: " + fullConfigPath);
                    }
                    else
                    {
                        taskField.SetValue(null, JObject.Parse(File.ReadAllText(fullConfigPath)));
                        cache.Clear();
                        bool configTypes = TaskConfigs.Get(100010)?.Type == TaskModel.MAIN_LINE
                            && TaskConfigs.Get(100020)?.Type == TaskModel.MAIN_LINE
                            && TaskConfigs.Get(2000000)?.Type == TaskModel.AWAKE_LINE;
                        if (!configTypes)
                        {
                            Debug.LogError("CLIVERIFY task-newest-finish config task type evidence mismatch");
                        }
                        else
                        {
                            int updates = 0;
                            updated = () => updates++;
                            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, updated);

                            Feed(on30004, controller, 100010, 0);
                            bool failed = model.NewestFinishTaskId == 0 && updates == 0;

                            Feed(on30004, controller, 100010, 1);
                            bool firstAdvance = model.NewestFinishTaskId == 100010 && updates == 1;

                            Feed(on30004, controller, 100020, 1);
                            bool secondAdvance = model.NewestFinishTaskId == 100020 && updates == 2;

                            Feed(on30004, controller, 100020, 1);
                            bool duplicate = model.NewestFinishTaskId == 100020 && updates == 2;

                            Feed(on30004, controller, 100010, 1);
                            bool smaller = model.NewestFinishTaskId == 100020 && updates == 2;

                            Feed(on30004, controller, 2000000, 1);
                            bool nonMain = model.NewestFinishTaskId == 100020 && updates == 2;

                            model.SetNewestFinishTaskId(7);
                            bool exactSet = model.NewestFinishTaskId == 7;
                            pass = failed && firstAdvance && secondAdvance && duplicate && smaller && nonMain && exactSet;
                            result = pass ? 0 : 3;
                            Debug.Log("CLIVERIFY task-newest-finish VERDICT config=" + configTypes
                                + " failed=" + failed + " first=" + firstAdvance + " second=" + secondAdvance
                                + " duplicate=" + duplicate + " smaller=" + smaller + " nonMain=" + nonMain
                                + " exactSet=" + exactSet + " pass=" + pass);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY task-newest-finish exception: " + exception);
                result = 3;
            }
            finally
            {
                if (updated != null) EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, updated);
                try { clearEffect.Invoke(controller, null); }
                catch (Exception exception) { Debug.LogError("CLIVERIFY task-newest-finish cleanup effect exception: " + exception); }
                effectField.SetValue(controller, oldEffect);
                effectEpochField.SetValue(controller, oldEffectEpoch);
                pendingAutoField.SetValue(controller, oldPendingAuto);
                oneAutoEpochField.SetValue(controller, oldOneAutoEpoch);
                loginKickoffField.SetValue(controller, oldLoginKickoff);
                startupTaskListField.SetValue(controller, oldStartupTaskList);
                model.SetNewestFinishTaskId(oldNewest);
                taskField.SetValue(null, oldTask);
                Restore(cache, oldCache);
                Restore(eventHandlers, oldEvents);
                restored = model.NewestFinishTaskId == oldNewest && ReferenceEquals(taskField.GetValue(null), oldTask)
                    && Same(cache, oldCache) && Same(eventHandlers, oldEvents)
                    && ReferenceEquals(effectField.GetValue(controller), oldEffect)
                    && (int)effectEpochField.GetValue(controller) == oldEffectEpoch
                    && (bool)pendingAutoField.GetValue(controller) == oldPendingAuto
                    && (int)oneAutoEpochField.GetValue(controller) == oldOneAutoEpoch
                    && (bool)loginKickoffField.GetValue(controller) == oldLoginKickoff
                    && (bool)startupTaskListField.GetValue(controller) == oldStartupTaskList;
                if (!restored) result = 3;
                Debug.Log("CLIVERIFY task-newest-finish result=" + result + " pass=" + pass + " restored=" + restored);
            }
            return result;
        }

        private static void Feed(MethodInfo method, TaskController controller, int taskId, int code)
        {
            // 30004 = task_id:u32 + code:u32 + reward:ObjectList；此处 reward 的 u16 count 精确为 0。
            byte[] payload = new CliVerify.Pkt().I(taskId).I(code).H(0).Bytes();
            method.Invoke(controller, new object[] { new NetReader(payload, 0, payload.Length) });
        }

        private static Dictionary<object, object> Copy(IDictionary dictionary)
        {
            var copy = new Dictionary<object, object>();
            if (dictionary == null) return copy;
            foreach (DictionaryEntry entry in dictionary) copy[entry.Key] = entry.Value;
            return copy;
        }

        private static void Restore(IDictionary dictionary, Dictionary<object, object> snapshot)
        {
            if (dictionary == null) return;
            dictionary.Clear();
            foreach (KeyValuePair<object, object> entry in snapshot) dictionary[entry.Key] = entry.Value;
        }

        private static bool Same(IDictionary dictionary, Dictionary<object, object> snapshot)
        {
            if (dictionary == null || dictionary.Count != snapshot.Count) return false;
            foreach (KeyValuePair<object, object> entry in snapshot)
                if (!dictionary.Contains(entry.Key) || !ReferenceEquals(dictionary[entry.Key], entry.Value)) return false;
            return true;
        }

        private static bool IsDefaultModel(TaskModel model)
        {
            if (model.NewestFinishTaskId != 0 || model.NowSelectTaskId != 0 || model.MainLineTaskVo != null
                || !model.AutoTaskEnabled || model.AllTaskList.Count != 0) return false;
            string[] fields =
            {
                "_hasReceiveTaskList", "_canTaskList", "_allTaskList", "_finishView",
                "_pendingAutoFightTaskId", "_pendingAutoFightMonsterTypeId", "_pendingCollectTaskId",
                "_pendingCollectMonsterTypeId", "_collectRetryToken", "_pendingCrossSceneTask",
                "_lastCrossSceneRequestAt",
            };
            foreach (string name in fields)
            {
                FieldInfo field = typeof(TaskModel).GetField(name, InstancePrivate);
                if (field == null) return false;
                object value = field.GetValue(model);
                if (value is IDictionary dictionary)
                {
                    if (dictionary.Count != 0) return false;
                }
                else if (value is int number)
                {
                    if (number != 0) return false;
                }
                else if (value is float decimalNumber)
                {
                    if (decimalNumber != 0f) return false;
                }
                else if (value != null) return false;
            }
            return true;
        }
    }
}
