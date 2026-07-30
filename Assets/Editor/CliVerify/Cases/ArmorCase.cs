using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Module.Core.Armor;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Equip;
using Shenxiao.Module.Core.Role;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>14401 全量树、14402 权威打造切片、配置前置条件与真实 GraphicRaycaster 点击专项。</summary>
    public static class ArmorCase
    {
        private const string PrefabPath = "Assets/Prefabs/UI/EquipArmor/EquipArmorModule.prefab";
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        public static async Task<int> Run()
        {
            bool oldFallback = ResManager.EditorPreferFallback;
            try
            {
                ResManager.EditorPreferFallback = true;
                await ArmorConfigs.EnsureLoaded();
                return RunSync();
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY armor EXCEPTION " + e);
                return 3;
            }
            finally
            {
                ResManager.EditorPreferFallback = oldFallback;
            }
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
                Debug.LogError("CLIVERIFY armor batch EXCEPTION " + e);
                code = 1;
            }
            Debug.Log("CLIVERIFY EXIT " + code);
            EditorApplication.Exit(code);
        }

        private static int RunSync()
        {
            ArmorController controller = ArmorController.Instance;
            ArmorModel model = ArmorModel.Instance;
            BagModel bag = BagModel.Instance;
            RoleModel role = RoleModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var savedStages = new List<ArmorModel.StageEntry>(model.Stages);
            bool savedHasData = model.HasData;
            bool savedHasMakeResult = model.HasMakeResult;
            uint savedMakeCode = model.LastMakeCode;
            int savedVersion = model.Version;
            var savedBag = new List<BagGoods>(bag.BagGoodsList);
            int savedBagCell = bag.CellNum;
            int savedBagMax = bag.GetMaxCell(BagModel.POS_BAG);
            bool savedBagHasData = bag.HasData;
            int savedRoleLevel = role.Level;
            bool savedRoleHasData = role.HasBaseInfo;
            FieldInfo intercept = typeof(ArmorController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            FieldInfo handlersField = typeof(NetManager).GetField("_handlers", SF);
            var handlers = handlersField?.GetValue(null) as IDictionary;
            bool had14401 = handlers != null && handlers.Contains(Proto.ARMOR_INFO);
            bool had14402 = handlers != null && handlers.Contains(Proto.ARMOR_MAKE);
            object old14401 = had14401 ? handlers[Proto.ARMOR_INFO] : null;
            object old14402 = had14402 ? handlers[Proto.ARMOR_MAKE] : null;
            Action updatedHandler = null;
            Action<uint> resultHandler = null;
            GameObject canvasGo = null;
            GameObject cameraGo = null;
            GameObject eventSystemGo = null;
            GameObject prefabRoot = null;
            RenderTexture warmup = null;
            EquipArmorView view = null;
            try
            {
                controller.Init();
                model.Reset();
                SetAuto(role, "HasBaseInfo", true);
                role.Level = 470;
                bag.SetBagFull(1, Math.Max(1, savedBagMax), new List<BagGoods>
                {
                    new BagGoods { GoodsId = 90014402, TypeId = 82110101, GoodsNum = 100 },
                });

                MethodInfo on14401 = typeof(ArmorController).GetMethod("On14401", F);
                MethodInfo on14402 = typeof(ArmorController).GetMethod("On14402", F);
                bool pass = intercept != null && on14401 != null && on14402 != null && handlers != null
                    && handlers.Contains(Proto.ARMOR_INFO) && handlers.Contains(Proto.ARMOR_MAKE);
                void Check(string tag, bool ok) { Debug.Log("CLIVERIFY armor " + tag + " ok=" + ok); if (!ok) pass = false; }
                Check("seams/register", pass);
                if (!pass) return 3;

                int updateEvents = 0;
                int resultEvents = 0;
                uint lastEventCode = 0;
                updatedHandler = () => updateEvents++;
                resultHandler = code => { resultEvents++; lastEventCode = code; };
                EventDispatcher.On(GlobalEvent.EVT_ARMOR_UPDATED, updatedHandler);
                EventDispatcher.On<uint>(GlobalEvent.EVT_ARMOR_MAKE_RESULT, resultHandler);

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestStartup();
                Check("startup exact frame", FrameInfo(frames, 0, 0));

                frames.Clear();
                byte[] first = new CliVerify.Pkt().H(2)
                    .C(2).H(1).C(1).C(0).H(1).I(82010002).C(1).C(0)
                    .C(1).H(2)
                        .C(2).C(0).H(1).I(82060001).C(6).C(0)
                        .C(1).C(0).H(1).I(82010001).C(1).C(0)
                    .Bytes();
                var firstReader = new NetReader(first, 0, first.Length);
                on14401.Invoke(controller, new object[] { firstReader });
                ArmorModel.StageEntry stage1 = model.FindStage(1);
                ArmorModel.StageEntry stage2 = model.FindStage(2);
                Check("14401 nested/full/sorted", firstReader.Remaining == 0 && model.HasData && model.Stages.Count == 2
                    && model.Stages[0].Stage == 1 && model.Stages[1].Stage == 2
                    && stage1.Types.Count == 2 && stage1.Types[0].Type == 1 && stage1.Types[1].Type == 2
                    && stage1.Types[1].Positions[0].GTypeId == 82060001U && updateEvents == 1 && frames.Count == 0);

                ArmorConfigs.EquipmentCfg cfg = ArmorConfigs.GetEquipment(2, 1, 1);
                ArmorConfigs.SuitCfg suit = ArmorConfigs.GetSuit(9, 2);
                IReadOnlyList<byte> type1Positions = ArmorConfigs.GetPositions(1);
                Check("config exact imported", ArmorConfigs.IsLoaded && ArmorConfigs.EquipmentCount == 90 && ArmorConfigs.SuitCount == 18
                    && cfg != null && cfg.Id == 82010002 && cfg.PreStage == 1 && cfg.Costs.Count == 2
                    && !cfg.Costs[0].IsArmorState && cfg.Costs[1].IsArmorState && cfg.Costs[1].TypeId == 82010001
                    && suit != null && suit.OpenLevel == 670 && type1Positions.Count == 5
                    && type1Positions[0] == 1 && type1Positions[4] == 5);

                ArmorConfigs.PreviewResult stage1Preview = ArmorConfigs.Preview(1, 1, 1);
                ArmorConfigs.PreviewResult stage2Before = ArmorConfigs.Preview(2, 1, 1);
                Check("precheck before make", stage1Preview.CanMake && stage1Preview.RealCosts.Count == 1
                    && stage1Preview.RealCosts[0].TypeId == 82110101 && stage2Before.Block == ArmorConfigs.MakeBlock.MissingPreviousStage);

                controller.RequestMake(1, 1, 1);
                Check("14402 exact request/no optimistic mutation", FrameMake(frames, 1, 1, 1)
                    && !model.IsMade(1, 1, 1) && !model.HasMakeResult);

                int versionBeforeFailure = model.Version;
                byte[] failure = new CliVerify.Pkt().I(4294967295L).H(0).Bytes();
                var failureReader = new NetReader(failure, 0, failure.Length);
                on14402.Invoke(controller, new object[] { failureReader });
                Check("failure preserves tree", failureReader.Remaining == 0 && model.HasMakeResult
                    && model.LastMakeCode == uint.MaxValue && model.Version == versionBeforeFailure
                    && !model.IsMade(1, 1, 1) && model.FindStage(2) == stage2
                    && resultEvents == 1 && lastEventCode == uint.MaxValue && updateEvents == 1);

                byte[] success = new CliVerify.Pkt().I(1).H(1)
                    .C(1).H(1).C(1).C(1).H(1).I(82010001).C(1).C(1).Bytes();
                var successReader = new NetReader(success, 0, success.Length);
                on14402.Invoke(controller, new object[] { successReader });
                Check("success authoritative slice", successReader.Remaining == 0 && model.LastMakeCode == 1
                    && model.IsMade(1, 1, 1) && model.IsTypeComplete(1, 1)
                    && model.FindType(1, 2).Positions[0].GTypeId == 82060001U
                    && model.FindStage(2) == stage2 && updateEvents == 2 && resultEvents == 2 && lastEventCode == 1);

                ArmorConfigs.PreviewResult stage2After = ArmorConfigs.Preview(2, 1, 1);
                Check("state-material filter", stage2After.CanMake && stage2After.DisplayCosts.Count == 2
                    && stage2After.RealCosts.Count == 1 && stage2After.DisplayCosts[1].IsArmorState
                    && ArmorConfigs.IsArmorStateAvailable(82010001)
                    && !string.IsNullOrEmpty(ArmorConfigs.BuildFingerprint(stage2After)));

                frames.Clear();
                bool uiOk = VerifyRealClick(frames, controller, on14402, ref canvasGo, ref cameraGo,
                    ref eventSystemGo, ref prefabRoot, ref warmup, ref view);
                Check("prefab/raycaster/make single-flight", uiOk);

                byte[] empty = new CliVerify.Pkt().H(0).Bytes();
                var emptyReader = new NetReader(empty, 0, empty.Length);
                on14401.Invoke(controller, new object[] { emptyReader });
                Check("14401 empty full replace", emptyReader.Remaining == 0 && model.HasData && model.Stages.Count == 0
                    && model.HasMakeResult && model.LastMakeCode != 0);

                controller.Dispose();
                Check("dispose reset", !controller.IsInitialized && !model.HasData && !model.HasMakeResult
                    && model.Stages.Count == 0 && model.LastMakeCode == 0);
                Debug.Log("CLIVERIFY armor VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                EventDispatcher.Off(GlobalEvent.EVT_ARMOR_UPDATED, updatedHandler);
                EventDispatcher.Off<uint>(GlobalEvent.EVT_ARMOR_MAKE_RESULT, resultHandler);
                CloseConfirmDialog();
                view?.Hide();
                if (prefabRoot != null) UnityEngine.Object.DestroyImmediate(prefabRoot);
                if (eventSystemGo != null) UnityEngine.Object.DestroyImmediate(eventSystemGo);
                if (cameraGo != null) UnityEngine.Object.DestroyImmediate(cameraGo);
                if (warmup != null)
                {
                    warmup.Release();
                    UnityEngine.Object.DestroyImmediate(warmup);
                }
                if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);

                if (controller.IsInitialized) controller.Dispose();
                if (wasInitialized) controller.Init();
                RestoreHandler(handlers, Proto.ARMOR_INFO, had14401, old14401);
                RestoreHandler(handlers, Proto.ARMOR_MAKE, had14402, old14402);
                if (intercept != null) intercept.SetValue(null, oldIntercept);

                model.Reset();
                if (savedHasData) model.ReplaceData(new List<ArmorModel.StageEntry>(savedStages));
                SetAuto(model, "HasData", savedHasData);
                SetAuto(model, "HasMakeResult", savedHasMakeResult);
                SetAuto(model, "LastMakeCode", savedMakeCode);
                SetAuto(model, "Version", savedVersion);

                bag.SetBagFull(savedBagCell, savedBagMax, savedBag);
                SetAuto(bag, "HasData", savedBagHasData);
                role.Level = savedRoleLevel;
                SetAuto(role, "HasBaseInfo", savedRoleHasData);
            }
        }

        private static bool VerifyRealClick(List<byte[]> frames, ArmorController controller, MethodInfo on14402,
            ref GameObject canvasGo, ref GameObject cameraGo, ref GameObject eventSystemGo, ref GameObject prefabRoot,
            ref RenderTexture warmup, ref EquipArmorView view)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || prefab.GetComponentInChildren<EquipArmorView>(true) == null
                || prefab.GetComponentInChildren<ArmorAttrView>(true) == null) return false;

            canvasGo = new GameObject("ArmorCase_Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(960f, 640f);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            cameraGo = new GameObject("ArmorCase_Camera", typeof(Camera));
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
            eventSystemGo = new GameObject("ArmorCase_EventSystem", typeof(EventSystem));
            EventSystem eventSystem = eventSystemGo.GetComponent<EventSystem>();

            prefabRoot = PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform) as GameObject;
            if (prefabRoot == null) return false;
            view = prefabRoot.GetComponentInChildren<EquipArmorView>(true);
            ArmorAttrView attrView = prefabRoot.GetComponentInChildren<ArmorAttrView>(true);
            if (view == null || attrView == null) return false;
            attrView.gameObject.SetActive(false);
            prefabRoot.SetActive(true);
            view.gameObject.SetActive(true);
            view.Show();
            frames.Clear(); // OnShow 的显式 14401 刷新不计入本次打造断言。

            canvas.enabled = false;
            canvas.enabled = true;
            Canvas.ForceUpdateCanvases();
            var descriptor = new RenderTextureDescriptor(Math.Max(1, Screen.width), Math.Max(1, Screen.height), RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = 1,
            };
            warmup = new RenderTexture(descriptor);
            if (!warmup.Create()) return false;
            camera.targetTexture = warmup;
            camera.Render();
            camera.targetTexture = null;

            RectTransform make = view._btn_make;
            if (make == null || view.SelectedStage != 2 || view.SelectedType != 1 || view.SelectedPosition != 1) return false;
            Image surface = make.GetComponent<Image>();
            bool uniqueSurface = surface != null && surface.raycastTarget;
            foreach (Graphic graphic in make.GetComponentsInChildren<Graphic>(true))
                if (graphic != surface && graphic.raycastTarget) uniqueSurface = false;

            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, make.TransformPoint(make.rect.center)),
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            bool pointerHit = false;
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].gameObject != make.gameObject) continue;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(hits[i].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                pointerHit = true;
                break;
            }
            if (!pointerHit) return false;

            if (frames.Count == 0)
            {
                MethodInfo confirm = typeof(EquipArmorView).GetMethod("ConfirmMake", F);
                confirm?.Invoke(view, null);
            }
            bool firstSend = FrameMake(frames, 2, 1, 1) && view.Pending && !string.IsNullOrEmpty(view.LastConfirmText);
            ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(make.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            bool singleFlight = frames.Count == 1;

            byte[] failure = new CliVerify.Pkt().I(1440001).H(0).Bytes();
            var reader = new NetReader(failure, 0, failure.Length);
            on14402.Invoke(controller, new object[] { reader });
            bool responseClearsPending = reader.Remaining == 0 && !view.Pending && !ArmorModel.Instance.IsMade(2, 1, 1);
            return uniqueSurface && pointerHit && firstSend && singleFlight && responseClearsPending;
        }

        private static bool FrameInfo(IReadOnlyList<byte[]> frames, byte stage, byte type)
        {
            if (frames.Count != 1 || frames[0] == null) return false;
            byte[] frame = frames[0];
            return frame.Length == 8 && frame[0] == 0 && frame[1] == 8 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(Proto.ARMOR_INFO >> 8) && frame[5] == (byte)(Proto.ARMOR_INFO & 0xff)
                && frame[6] == stage && frame[7] == type;
        }

        private static bool FrameMake(IReadOnlyList<byte[]> frames, byte stage, byte type, byte position)
        {
            if (frames.Count != 1 || frames[0] == null) return false;
            byte[] frame = frames[0];
            return frame.Length == 9 && frame[0] == 0 && frame[1] == 9 && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(Proto.ARMOR_MAKE >> 8) && frame[5] == (byte)(Proto.ARMOR_MAKE & 0xff)
                && frame[6] == stage && frame[7] == type && frame[8] == position;
        }

        private static void CloseConfirmDialog()
        {
            MethodInfo close = typeof(ConfirmDialog).GetMethod("Close", SF);
            close?.Invoke(null, new object[] { false });
        }

        private static void RestoreHandler(IDictionary handlers, int id, bool had, object value)
        {
            if (handlers == null) return;
            if (had) handlers[id] = value;
            else if (handlers.Contains(id)) handlers.Remove(id);
        }

        private static void SetAuto(object target, string propertyName, object value)
        {
            target.GetType().GetField("<" + propertyName + ">k__BackingField", F)?.SetValue(target, value);
        }
    }
}
