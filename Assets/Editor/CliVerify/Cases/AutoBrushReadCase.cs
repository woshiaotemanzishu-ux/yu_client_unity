using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Generated.UI.AutoBrush;
using Shenxiao.Module.Core.AutoBrush;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    public static class AutoBrushReadCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY autobrushread EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            AutoBrushController controller = AutoBrushController.Instance;
            AutoBrushModel model = AutoBrushModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var old = CaptureModel(model);
            FieldInfo retryField = typeof(AutoBrushController).GetField("_exitRetryCount", F);
            int oldRetry = retryField == null ? 0 : (int)retryField.GetValue(controller);
            FieldInfo pendingField = typeof(AutoBrushController).GetField("_stageRewardPendingUntil", F);
            FieldInfo refreshField = typeof(AutoBrushController).GetField("_stageRewardRefreshPending", F);
            FieldInfo gateField = typeof(AutoBrushController).GetField("_stageRewardRequestGate", F);
            double oldPending = pendingField == null ? 0d : (double)pendingField.GetValue(controller);
            bool oldRefresh = refreshField != null && (bool)refreshField.GetValue(controller);
            ulong oldGate = gateField == null ? 0UL : (ulong)gateField.GetValue(controller);
            FieldInfo intercept = typeof(AutoBrushController).GetField("s_startupOutboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 13300; id <= 13324; id++) SaveHandler(handlers, savedHandlers, id);
            SaveHandler(handlers, savedHandlers, 61002);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                if (handlers != null)
                {
                    for (int id = 13300; id <= 13324; id++) handlers.Remove(id);
                    handlers.Remove(61002);
                }

                controller.Init();
                MethodInfo on09 = Handler("On13309");
                MethodInfo on10 = Handler("On13310");
                MethodInfo on23 = Handler("On13323");
                MethodInfo on24 = Handler("On13324");
                MethodInfo onStart = Handler("OnGameStart");
                int[] expected = { 13300, 13301, 13305, 13306, 13307, 13309, 13310, 13323, 13324 };
                pass = handlers != null && intercept != null && retryField != null
                    && pendingField != null && refreshField != null && gateField != null
                    && on09 != null && on10 != null && on23 != null && on24 != null && onStart != null
                    && ExactRegistrations(handlers, expected) && handlers.Contains(61002);

                var firstHandlers = new Dictionary<int, object>();
                foreach (int id in expected) firstHandlers[id] = handlers[id];
                firstHandlers[61002] = handlers[61002];
                controller.Init();
                foreach (KeyValuePair<int, object> pair in firstHandlers)
                    pass &= ReferenceEquals(handlers[pair.Key], pair.Value);

                SeedModel(model);
                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                onStart.Invoke(controller, null);
                pass &= FramesEqual(frames, 13300, 13301, 13309, 13323, 13324) && IsReset(model);

                var brush = new AutoBrushModel.BrushStrangeInfo
                {
                    Code = 7, CurrentTimes = 8, NeedTimes = 9, AssistId = 10, AssisterId = 11,
                };
                model.SetBrushStrangeInfo(brush);
                model.SetRankInfo(1, 2, 3, "rank", 4);

                pass &= Feed(on09, controller, new CliVerify.Pkt().I(uint.MaxValue).L(-1))
                    && model.HasNextStageReward && model.NextStageRewardCode == uint.MaxValue
                    && model.NextStageRewardGate == ulong.MaxValue
                    && ReferenceEquals(model.BrushInfo, brush) && model.TopRankName == "rank";
                pass &= Feed(on23, controller, new CliVerify.Pkt().C(byte.MaxValue))
                    && model.HasTutorialNode && model.TutorialNode == byte.MaxValue
                    && model.NextStageRewardGate == ulong.MaxValue;
                pass &= Feed(on24, controller, new CliVerify.Pkt().H(ushort.MaxValue).I(uint.MaxValue))
                    && model.HasAssistInfo && model.AssistDailyCount == ushort.MaxValue
                    && model.AssistNextTime == uint.MaxValue && model.TutorialNode == byte.MaxValue;

                pass &= Feed(on09, controller, new CliVerify.Pkt().I(0).L(0))
                    && model.HasNextStageReward && model.NextStageRewardCode == 0 && model.NextStageRewardGate == 0
                    && model.HasTutorialNode && model.HasAssistInfo;
                pass &= Feed(on23, controller, new CliVerify.Pkt().C(0))
                    && model.HasTutorialNode && model.TutorialNode == 0 && model.HasAssistInfo;
                pass &= Feed(on24, controller, new CliVerify.Pkt().H(0).I(0))
                    && model.HasAssistInfo && model.AssistDailyCount == 0 && model.AssistNextTime == 0
                    && ReferenceEquals(model.BrushInfo, brush) && model.Level == 3;

                frames.Clear();
                model.SetRankInfo(0, 0, 10);
                model.ReplaceNextStageReward(0, 10);
                pass &= !controller.RequestStageReward() && frames.Count == 0;
                model.ReplaceNextStageReward(1, 0);
                pass &= !controller.RequestStageReward() && frames.Count == 0;
                model.ReplaceNextStageReward(1, ulong.MaxValue);
                pass &= !controller.RequestStageReward() && frames.Count == 0;
                model.ReplaceNextStageReward(1, 11);
                pass &= !controller.RequestStageReward() && frames.Count == 0;

                model.ReplaceStageRewardResult(77, new List<AutoBrushModel.StageRewardEntry>
                {
                    new AutoBrushModel.StageRewardEntry(7, 8, 9),
                });
                AutoBrushModel.StageRewardResult oldResult = model.LastStageRewardResult;
                model.ReplaceNextStageReward(1, 10);
                pass &= controller.RequestStageReward()
                    && frames.Count == 1 && IsU64Frame(frames[0], 13310, 10)
                    && ReferenceEquals(model.LastStageRewardResult, oldResult)
                    && controller.IsStageRewardPending && !controller.IsStageRewardRefreshPending;
                pass &= !controller.RequestStageReward() && frames.Count == 1;

                pass &= Feed(on10, controller, new CliVerify.Pkt().I(uint.MaxValue).H(2)
                        .C(byte.MaxValue).I(uint.MaxValue).I(0)
                        .C(byte.MaxValue).I(uint.MaxValue).I(uint.MaxValue))
                    && model.LastStageRewardResult.Code == uint.MaxValue
                    && model.LastStageRewardResult.Rewards.Count == 2
                    && model.LastStageRewardResult.Rewards[0].Style == byte.MaxValue
                    && model.LastStageRewardResult.Rewards[0].TypeId == uint.MaxValue
                    && model.LastStageRewardResult.Rewards[0].Count == 0
                    && model.LastStageRewardResult.Rewards[1].TypeId == uint.MaxValue
                    && model.LastStageRewardResult.Rewards[1].Count == uint.MaxValue
                    && frames.Count == 1
                    && !controller.IsStageRewardPending && !controller.IsStageRewardRefreshPending;

                frames.Clear();
                pass &= controller.RequestStageReward()
                    && frames.Count == 1 && IsU64Frame(frames[0], 13310, 10);
                frames.Clear();
                pass &= Feed(on10, controller, new CliVerify.Pkt().I(1).H(3)
                        .C(0).I(42).I(0)
                        .C(0).I(42).I(uint.MaxValue)
                        .C(0).I(42).I(uint.MaxValue))
                    && model.LastStageRewardResult.Code == 1
                    && model.LastStageRewardResult.Rewards.Count == 3
                    && model.LastStageRewardResult.Rewards[0].Count == 0
                    && model.LastStageRewardResult.Rewards[1].TypeId == 42
                    && model.LastStageRewardResult.Rewards[2].TypeId == 42
                    && model.NextStageRewardGate == 10
                    && frames.Count == 1 && FramesEqual(frames, 13309)
                    && !controller.IsStageRewardPending && controller.IsStageRewardRefreshPending
                    && !controller.RequestStageReward() && frames.Count == 1;

                pass &= Feed(on09, controller, new CliVerify.Pkt().I(1).L(11))
                    && model.NextStageRewardGate == 11
                    && !controller.IsStageRewardPending && !controller.IsStageRewardRefreshPending;
                model.SetLevel(11);
                frames.Clear();
                bool prefabClick = VerifyPrefabClick(controller, model, frames);
                Debug.Log("CLIVERIFY autobrushread prefabClick=" + prefabClick + " passBefore=" + pass);
                pass &= prefabClick;

                controller.Dispose();
                pass &= !controller.IsInitialized && IsReset(model)
                    && !controller.IsStageRewardPending && !controller.IsStageRewardRefreshPending;
                for (int id = 13300; id <= 13324; id++) pass &= !handlers.Contains(id);
                pass &= !handlers.Contains(61002);
                Debug.Log("CLIVERIFY autobrushread VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModel(model, old);
                if (retryField != null) retryField.SetValue(controller, oldRetry);
                if (pendingField != null) pendingField.SetValue(controller, oldPending);
                if (refreshField != null) refreshField.SetValue(controller, oldRefresh);
                if (gateField != null) gateField.SetValue(controller, oldGate);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (wasInitialized) controller.Init();
                for (int id = 13300; id <= 13324; id++) RestoreHandler(handlers, savedHandlers[id], id);
                RestoreHandler(handlers, savedHandlers[61002], 61002);

                restored = ReferenceEquals(AutoBrushController.Instance, controller)
                    && ReferenceEquals(AutoBrushModel.Instance, model)
                    && controller.IsInitialized == wasInitialized
                    && (retryField == null || (int)retryField.GetValue(controller) == oldRetry)
                    && (pendingField == null || (double)pendingField.GetValue(controller) == oldPending)
                    && (refreshField == null || (bool)refreshField.GetValue(controller) == oldRefresh)
                    && (gateField == null || (ulong)gateField.GetValue(controller) == oldGate)
                    && ModelMatches(model, old)
                    && HandlerMatches(handlers, savedHandlers[61002], 61002);
                for (int id = 13300; id <= 13324; id++)
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY autobrushread restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static Dictionary<string, object> CaptureModel(AutoBrushModel model) => new Dictionary<string, object>
        {
            ["BrushInfo"] = model.BrushInfo,
            ["AutoBrushState"] = model.AutoBrushState,
            ["Level"] = model.Level,
            ["RoleRank"] = model.RoleRank,
            ["RankType"] = model.RankType,
            ["TopRankName"] = model.TopRankName,
            ["TopRankLevel"] = model.TopRankLevel,
            ["MaxLevel"] = model.MaxLevel,
            ["FailureState"] = model.FailureState,
            ["LastFailureLevel"] = model.LastFailureLevel,
            ["HasNextStageReward"] = model.HasNextStageReward,
            ["NextStageRewardCode"] = model.NextStageRewardCode,
            ["NextStageRewardGate"] = model.NextStageRewardGate,
            ["LastStageRewardResult"] = model.LastStageRewardResult,
            ["HasTutorialNode"] = model.HasTutorialNode,
            ["TutorialNode"] = model.TutorialNode,
            ["HasAssistInfo"] = model.HasAssistInfo,
            ["AssistDailyCount"] = model.AssistDailyCount,
            ["AssistNextTime"] = model.AssistNextTime,
        };

        private static void SeedModel(AutoBrushModel model)
        {
            model.SetBrushStrangeInfo(new AutoBrushModel.BrushStrangeInfo
                { Code = 1, CurrentTimes = 2, NeedTimes = 3, AssistId = 4, AssisterId = 5 });
            model.SetAutoBrushStrangeState(true);
            model.SetRankInfo(6, 7, 8, "seed", 9);
            model.SetMaxLevel(10);
            model.SetFailureState(true, 11);
            model.ReplaceNextStageReward(12, 13);
            model.ReplaceStageRewardResult(17, new List<AutoBrushModel.StageRewardEntry>
            {
                new AutoBrushModel.StageRewardEntry(18, 19, 20),
            });
            model.ReplaceTutorialNode(14);
            model.ReplaceAssistInfo(15, 16);
        }

        private static bool IsReset(AutoBrushModel model) => model.BrushInfo == null
            && !model.AutoBrushState && model.Level == 0 && model.RoleRank == 0 && model.RankType == 0
            && model.TopRankName == "" && model.TopRankLevel == 0 && model.MaxLevel == 0
            && !model.FailureState && model.LastFailureLevel == 0
            && !model.HasNextStageReward && model.NextStageRewardCode == 0 && model.NextStageRewardGate == 0
            && model.LastStageRewardResult == null
            && !model.HasTutorialNode && model.TutorialNode == 0
            && !model.HasAssistInfo && model.AssistDailyCount == 0 && model.AssistNextTime == 0;

        private static MethodInfo Handler(string name) => typeof(AutoBrushController).GetMethod(name, F);

        private static bool Feed(MethodInfo handler, AutoBrushController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool ExactRegistrations(IDictionary handlers, IReadOnlyList<int> expected)
        {
            var set = new HashSet<int>(expected);
            for (int id = 13300; id <= 13324; id++)
                if (handlers.Contains(id) != set.Contains(id)) return false;
            return true;
        }

        private static bool FramesEqual(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] frame = frames[i];
                if (frame == null || frame.Length != 6 || frame[0] != 0 || frame[1] != 6
                    || frame[2] != 3 || frame[3] != 232 || frame[4] != (byte)(ids[i] >> 8)
                    || frame[5] != (byte)ids[i]) return false;
            }
            return true;
        }

        private static bool IsU64Frame(byte[] frame, int id, ulong value)
        {
            if (frame == null || frame.Length != 14 || frame[0] != 0 || frame[1] != 14
                || frame[2] != 3 || frame[3] != 232 || frame[4] != (byte)(id >> 8)
                || frame[5] != (byte)id) return false;
            for (int i = 0; i < 8; i++)
            {
                int shift = (7 - i) * 8;
                if (frame[6 + i] != (byte)(value >> shift)) return false;
            }
            return true;
        }

        private static bool VerifyPrefabClick(AutoBrushController controller, AutoBrushModel model,
            List<byte[]> frames)
        {
            GameObject canvasGo = null;
            GameObject cameraGo = null;
            GameObject eventGo = null;
            GameObject instance = null;
            RenderTexture target = null;
            AutoBrushMainView view = null;
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/AutoBrush/AutoBrushModule.prefab");
                if (prefab == null)
                {
                    Debug.LogError("CLIVERIFY autobrushread prefab missing");
                    return false;
                }

                canvasGo = new GameObject("AutoBrushReadCaseCanvas", typeof(RectTransform),
                    typeof(Canvas), typeof(GraphicRaycaster));
                ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(960f, 640f);
                Canvas canvas = canvasGo.GetComponent<Canvas>();
                cameraGo = new GameObject("AutoBrushReadCaseCamera", typeof(Camera));
                Camera camera = cameraGo.GetComponent<Camera>();
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.orthographic = true;
                camera.orthographicSize = 360f;
                camera.pixelRect = new Rect(0f, 0f, Math.Max(1, Screen.width), Math.Max(1, Screen.height));
                camera.aspect = 1.5f;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>();
                raycaster.ignoreReversedGraphics = false;

                EventSystem eventSystem = EventSystem.current;
                if (eventSystem == null)
                {
                    eventGo = new GameObject("AutoBrushReadCaseEventSystem", typeof(EventSystem));
                    eventSystem = eventGo.GetComponent<EventSystem>();
                }

                instance = UnityEngine.Object.Instantiate(prefab, canvasGo.transform, false);
                instance.SetActive(true);
                AutoBrushMainViewBind bind = instance.GetComponentInChildren<AutoBrushMainViewBind>(true);
                if (bind == null || bind._box_click == null)
                {
                    Debug.LogError("CLIVERIFY autobrushread bind/box_click missing bind=" + (bind != null));
                    return false;
                }

                model.SetLevel(11);
                model.ReplaceNextStageReward(1, 11);
                view = new AutoBrushMainView(bind);
                view.Show();
                canvas.enabled = false;
                canvas.enabled = true;
                Canvas.ForceUpdateCanvases();
                target = new RenderTexture(new RenderTextureDescriptor(Math.Max(1, Screen.width),
                    Math.Max(1, Screen.height), RenderTextureFormat.ARGB32, 24) { msaaSamples = 1 });
                if (!target.Create()) return false;
                camera.targetTexture = target;
                camera.Render();
                camera.targetTexture = null;

                Image hitImage = bind._box_click.GetComponent<Image>();
                Button hitButton = bind._box_click.GetComponent<Button>();
                bool rootHit = hitImage != null && hitButton != null && hitImage.enabled
                    && hitImage.raycastTarget && Mathf.Approximately(hitImage.color.a, 0f);

                Vector3 worldCenter = bind._box_click.TransformPoint(bind._box_click.rect.center);
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldCenter);
                var pointer = new PointerEventData(eventSystem) { position = screenPoint };
                var hits = new List<RaycastResult>();
                raycaster.Raycast(pointer, hits);
                GameObject hit = null;
                for (int i = 0; i < hits.Count; i++)
                {
                    Transform t = hits[i].gameObject.transform;
                    if (t == bind._box_click || t.IsChildOf(bind._box_click))
                    {
                        hit = hits[i].gameObject;
                        break;
                    }
                }
                Debug.Log("CLIVERIFY autobrushread raycast rootHit=" + rootHit
                    + " rect=" + bind._box_click.rect + " point=" + screenPoint
                    + " hits=" + hits.Count + " boxHit=" + (hit != null)
                    + " active=" + bind._box_click.gameObject.activeInHierarchy
                    + " imageActive=" + hitImage.isActiveAndEnabled
                    + " depth=" + hitImage.depth + " cull=" + hitImage.canvasRenderer.cull
                    + " canvas=" + (hitImage.canvas != null ? hitImage.canvas.name : "null"));
                if (!rootHit || hit == null) return false;

                frames.Clear();
                ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
                bool first = frames.Count == 1 && IsU64Frame(frames[0], 13310, 11)
                    && controller.IsStageRewardPending;
                ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
                return first && frames.Count == 1;
            }
            finally
            {
                view?.Hide();
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);
                if (cameraGo != null) UnityEngine.Object.DestroyImmediate(cameraGo);
                if (eventGo != null) UnityEngine.Object.DestroyImmediate(eventGo);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }

        private static void RestoreModel(AutoBrushModel model, IDictionary<string, object> old)
        {
            foreach (KeyValuePair<string, object> pair in old)
                typeof(AutoBrushModel).GetProperty(pair.Key)?.SetValue(model, pair.Value);
        }

        private static bool ModelMatches(AutoBrushModel model, IDictionary<string, object> old)
        {
            foreach (KeyValuePair<string, object> pair in old)
                if (!Equals(typeof(AutoBrushModel).GetProperty(pair.Key)?.GetValue(model), pair.Value)) return false;
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

        private static bool HandlerMatches(IDictionary handlers, HandlerState saved, int id) =>
            handlers != null && handlers.Contains(id) == saved.Exists
            && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));
    }
}
