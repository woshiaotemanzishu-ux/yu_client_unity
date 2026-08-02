using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Generated.UI.Dsgt;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Designation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>R523：称号 41101 权威列表、41109 道具激活事务及真实 Prefab 点击链。</summary>
    public static class DesignationCase
    {
        private const BindingFlags IF = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private const string PrefabPath = "Assets/Prefabs/UI/Dsgt/DsgtModule.prefab";
        private static readonly int[] AllCommands =
            { 41100, 41101, 41102, 41103, 41104, 41105, 41106, 41107, 41108, 41109, 41110 };

        public static async Task<int> Run()
        {
            try
            {
                await DesignationConfigs.EnsureLoaded();
                return RunSync();
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY designation EXCEPTION " + e);
                return 3;
            }
        }

        private static int RunSync()
        {
            DesignationController controller = DesignationController.Instance;
            DesignationModel model = DesignationModel.Instance;
            BagModel bag = BagModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            Dictionary<FieldInfo, object> oldModelFields = CaptureAutoFields(model);
            var oldEntries = new List<DesignationModel.Entry>(model.Entries);
            var oldBag = new List<BagGoods>(bag.BagGoodsList);
            int oldCell = bag.CellNum;
            int oldMax = bag.MaxCell;
            bool oldBagHasData = bag.HasData;
            FieldInfo intercept = typeof(DesignationController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, object>();
            if (handlers != null)
                foreach (int command in AllCommands)
                    if (handlers.Contains(command)) oldHandlers[command] = handlers[command];

            GameObject canvasGo = null;
            GameObject cameraGo = null;
            GameObject eventSystemGo = null;
            GameObject buttonGo = null;
            RenderTexture warmup = null;
            bool pass = true;
            bool restored = false;
            try
            {
                DesignationConfigs.Row row = FindActivatableRow(out DesignationConfigs.Cost cost);
                Check(ref pass, "config single real-bag cost", row != null && cost != null);
                if (!pass) return 3;

                controller.Init();
                model.Reset();
                bag.SetBagFull(1, 50, new List<BagGoods>
                {
                    new BagGoods { GoodsId = 90001, TypeId = cost.TypeId, GoodsNum = cost.Num, Cell = 1 },
                });

                MethodInfo on01 = typeof(DesignationController).GetMethod("On41101", IF);
                MethodInfo on09 = typeof(DesignationController).GetMethod("On41109", IF);
                Check(ref pass, "constants/handlers/registration boundary",
                    Proto.DESIGNATION_LIST == 41101 && Proto.DESIGNATION_ACTIVATE_BY_GOODS == 41109
                    && intercept != null && on01 != null && on09 != null && handlers != null
                    && handlers.Contains(41101) && handlers.Contains(41104) && handlers.Contains(41105)
                    && handlers.Contains(41107) && handlers.Contains(41108) && handlers.Contains(41109)
                    && !handlers.Contains(41100) && !handlers.Contains(41102) && !handlers.Contains(41103)
                    && !handlers.Contains(41106) && !handlers.Contains(41110));

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestStartup();
                Check(ref pass, "startup exact empty frame", EmptyFrame(frames, 41101));
                frames.Clear();

                Check(ref pass, "41101 fields/order/read-to-end",
                    Feed(on01, controller, new CliVerify.Pkt().I(100).H(2)
                        .I(100).C(1).I(4000000000L).I(101).C(2).I(3))
                    && model.HasData && model.CurrentUsedId == 100 && model.Entries.Count == 2
                    && model.Entries[0].Id == 100 && model.Entries[0].Order == 1
                    && model.Entries[0].EndTime == 4000000000U
                    && model.Entries[1].Id == 101 && model.Entries[1].Order == 2
                    && model.Entries[1].EndTime == 3 && frames.Count == 0);
                Check(ref pass, "41101 full replace empty",
                    Feed(on01, controller, new CliVerify.Pkt().I(0).H(0))
                    && model.HasData && model.CurrentUsedId == 0 && model.Entries.Count == 0);

                long beforeGoods = bag.GetTypeGoodsNum(cost.TypeId);
                bool firstSend = controller.TryActivateByGoods(row.Id);
                bool duplicateBlocked = !controller.TryActivateByGoods(row.Id);
                Check(ref pass, "41109 exact frame/single-flight/no-local-deduction",
                    firstSend && duplicateBlocked && U32Frame(frames, 41109, row.Id)
                    && controller.HasPendingActivation && bag.GetTypeGoodsNum(cost.TypeId) == beforeGoods
                    && model.Entries.Count == 0);
                frames.Clear();

                Check(ref pass, "41109 failure raw overwrite/read-end/preserves-authority",
                    Feed(on09, controller, new CliVerify.Pkt().I(uint.MaxValue).I(uint.MaxValue)
                        .I(uint.MaxValue).I(uint.MaxValue))
                    && !controller.HasPendingActivation && model.GoodsActivationResult != null
                    && model.GoodsActivationResult.Code == uint.MaxValue
                    && model.GoodsActivationResult.Power == uint.MaxValue
                    && model.GoodsActivationResult.CurrentUsedId == uint.MaxValue
                    && model.GoodsActivationResult.Id == uint.MaxValue
                    && model.HasData && model.Entries.Count == 0 && frames.Count == 0
                    && bag.GetTypeGoodsNum(cost.TypeId) == beforeGoods);

                Check(ref pass, "41109 retry after failure", controller.TryActivateByGoods(row.Id)
                    && U32Frame(frames, 41109, row.Id));
                frames.Clear();
                Check(ref pass, "41109 success raw then exactly one authoritative refresh",
                    Feed(on09, controller, new CliVerify.Pkt().I(1).I(7654321).I(123).I(row.Id))
                    && !controller.HasPendingActivation && model.GoodsActivationResult.Code == 1
                    && model.GoodsActivationResult.Power == 7654321
                    && model.GoodsActivationResult.CurrentUsedId == 123
                    && model.GoodsActivationResult.Id == row.Id
                    && model.HasData && model.CurrentUsedId == 0 && model.Entries.Count == 0
                    && controller.IsAwaitingActivationRefresh(row.Id)
                    && !controller.TryActivateByGoods(row.Id)
                    && EmptyFrame(frames, 41101) && bag.GetTypeGoodsNum(cost.TypeId) == beforeGoods);
                frames.Clear();

                Check(ref pass, "41101 authority clears refresh guard",
                    Feed(on01, controller, new CliVerify.Pkt().I(0).H(0))
                    && !controller.IsAwaitingActivationRefresh(row.Id));

                bool realClick = VerifyRealPrefabClick(row.Id, frames, controller, on09,
                    ref canvasGo, ref cameraGo, ref eventSystemGo, ref buttonGo, ref warmup);
                Check(ref pass, "real prefab GraphicRaycaster pointer/single-flight", realClick);

                controller.Dispose();
                Check(ref pass, "dispose reset", !controller.IsInitialized && !model.HasData
                    && model.CurrentUsedId == 0 && model.Entries.Count == 0
                    && model.GoodsActivationResult == null && !controller.HasPendingActivation
                    && !controller.IsAwaitingActivationRefresh(row.Id));
            }
            finally
            {
                if (warmup != null) { warmup.Release(); UnityEngine.Object.DestroyImmediate(warmup); }
                if (buttonGo != null) UnityEngine.Object.DestroyImmediate(buttonGo);
                if (eventSystemGo != null) UnityEngine.Object.DestroyImmediate(eventSystemGo);
                if (cameraGo != null) UnityEngine.Object.DestroyImmediate(cameraGo);
                if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);

                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                RestoreEntries(model, oldEntries);
                RestoreAutoFields(model, oldModelFields);
                bag.SetBagFull(oldCell, oldMax, oldBag);
                SetAuto(bag, "HasData", oldBagHasData);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (oldInitialized) controller.Init();
                RestoreHandlers(handlers, oldHandlers);
                restored = controller.IsInitialized == oldInitialized
                    && SameAutoFields(model, oldModelFields) && SameRefs(model.Entries, oldEntries)
                    && bag.HasData == oldBagHasData && SameBag(bag.BagGoodsList, oldBag)
                    && SameHandlers(handlers, oldHandlers)
                    && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldIntercept));
            }

            bool finalPass = pass && restored;
            Debug.Log("CLIVERIFY designation restored=" + restored);
            Debug.Log("CLIVERIFY designation VERDICT pass=" + finalPass);
            return finalPass ? 0 : 3;
        }

        private static DesignationConfigs.Row FindActivatableRow(out DesignationConfigs.Cost cost)
        {
            for (int i = 0; i < DesignationConfigs.All.Count; i++)
            {
                DesignationConfigs.Row row = DesignationConfigs.All[i];
                if (DesignationConfigs.TryGetActivationCost(row.Id, out cost)) return row;
            }
            cost = null;
            return null;
        }

        private static bool VerifyRealPrefabClick(uint designationId, List<byte[]> frames,
            DesignationController controller, MethodInfo on09, ref GameObject canvasGo,
            ref GameObject cameraGo, ref GameObject eventSystemGo, ref GameObject buttonGo,
            ref RenderTexture warmup)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            DsgtDetailsItemBind details = prefab != null
                ? prefab.GetComponentInChildren<DsgtDetailsItemBind>(true)
                : null;
            if (details == null || details.dsgt_Activate_button == null) return false;
            bool materialRender = VerifyRealDetailsMaterial(details, designationId);

            canvasGo = new GameObject("DesignationCase_Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(GraphicRaycaster));
            ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(960f, 640f);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            cameraGo = new GameObject("DesignationCase_Camera", typeof(Camera));
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
            eventSystemGo = new GameObject("DesignationCase_EventSystem", typeof(EventSystem));
            EventSystem eventSystem = eventSystemGo.GetComponent<EventSystem>();

            buttonGo = UnityEngine.Object.Instantiate(
                details.dsgt_Activate_button.gameObject, canvasGo.transform, false);
            buttonGo.name = "dsgt_Activate_button_RealPrefabCase";
            buttonGo.SetActive(true);
            RectTransform button = (RectTransform)buttonGo.transform;
            button.anchorMin = button.anchorMax = button.pivot = new Vector2(0.5f, 0.5f);
            button.anchoredPosition = Vector2.zero;
            MethodInfo bind = typeof(DesignationFlow).GetMethod("BindActivationClick", SF);
            if (bind == null) return false;
            bind.Invoke(null, new object[] { button, designationId });

            Image surface = button.GetComponent<Image>();
            bool uniqueSurface = surface != null && surface.raycastTarget;
            foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true))
                if (graphic != surface && graphic.raycastTarget) uniqueSurface = false;

            canvas.enabled = false;
            canvas.enabled = true;
            Canvas.ForceUpdateCanvases();
            var descriptor = new RenderTextureDescriptor(Math.Max(1, Screen.width),
                Math.Max(1, Screen.height), RenderTextureFormat.ARGB32, 24) { msaaSamples = 1 };
            warmup = new RenderTexture(descriptor);
            if (!warmup.Create()) return false;
            camera.targetTexture = warmup;
            camera.Render();
            camera.targetTexture = null;

            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, button.TransformPoint(button.rect.center)),
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            bool pointerHit = false;
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].gameObject != buttonGo) continue;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                    hits[i].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                pointerHit = true;
                break;
            }
            bool firstSend = U32Frame(frames, 41109, designationId) && controller.HasPendingActivation;
            ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                buttonGo, pointer, ExecuteEvents.pointerClickHandler);
            bool singleFlight = frames.Count == 1;
            bool response = Feed(on09, controller, new CliVerify.Pkt().I(2).I(0).I(0).I(designationId))
                && !controller.HasPendingActivation && frames.Count == 1;
            return materialRender && uniqueSurface && pointerHit && firstSend && singleFlight && response;
        }

        private static bool VerifyRealDetailsMaterial(DsgtDetailsItemBind template, uint designationId)
        {
            GameObject root = null;
            FieldInfo detailsField = typeof(DesignationFlow).GetField("_details", SF);
            FieldInfo costField = typeof(DesignationFlow).GetField("_costItem", SF);
            MethodInfo render = typeof(DesignationFlow).GetMethod("RenderActivation", SF);
            object oldDetails = detailsField?.GetValue(null);
            object oldCost = costField?.GetValue(null);
            try
            {
                if (detailsField == null || costField == null || render == null) return false;
                root = UnityEngine.Object.Instantiate(template.gameObject);
                root.SetActive(true);
                DsgtDetailsItemBind details = root.GetComponent<DsgtDetailsItemBind>();
                if (details == null) return false;
                details.Show();
                detailsField.SetValue(null, details);
                costField.SetValue(null, null);
                DesignationConfigs.Row row = DesignationConfigs.Get(designationId);
                bool hasCost = DesignationConfigs.TryGetActivationCost(
                    designationId, out DesignationConfigs.Cost cost);
                render.Invoke(null, new object[] { row, null });
                var costItem = costField.GetValue(null) as Component;
                Image surface = details.dsgt_Activate_button != null
                    ? details.dsgt_Activate_button.GetComponent<Image>()
                    : null;
                bool unique = surface != null && surface.raycastTarget;
                if (details.dsgt_Activate_button != null)
                    foreach (Graphic graphic in details.dsgt_Activate_button.GetComponentsInChildren<Graphic>(true))
                        if (graphic != surface && graphic.raycastTarget) unique = false;
                string expectedCount = hasCost
                    ? BagModel.Instance.GetTypeGoodsNum(cost.TypeId) + "/" + cost.Num
                    : string.Empty;
                return row != null && hasCost && details.dsgt_Activate_button.gameObject.activeSelf
                    && details.dsgt_expend_label.gameObject.activeSelf
                    && details.dsgt_awarditem_group.gameObject.activeSelf
                    && details.dsgt_number_label.gameObject.activeSelf
                    && details.dsgt_number_label.text == expectedCount
                    && details.dsgt_red_image.gameObject.activeSelf
                    && costItem != null && costItem.gameObject.activeSelf && unique;
            }
            finally
            {
                if (detailsField != null) detailsField.SetValue(null, oldDetails);
                if (costField != null) costField.SetValue(null, oldCost);
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static bool Feed(MethodInfo method, DesignationController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool EmptyFrame(IReadOnlyList<byte[]> frames, int command)
        {
            if (frames.Count != 1 || frames[0] == null) return false;
            byte[] frame = frames[0];
            return frame.Length == 6 && frame[0] == 0 && frame[1] == 6
                && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(command >> 8) && frame[5] == (byte)command;
        }

        private static bool U32Frame(IReadOnlyList<byte[]> frames, int command, uint value)
        {
            if (frames.Count != 1 || frames[0] == null) return false;
            byte[] frame = frames[0];
            return frame.Length == 10 && frame[0] == 0 && frame[1] == 10
                && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(command >> 8) && frame[5] == (byte)command
                && frame[6] == (byte)(value >> 24) && frame[7] == (byte)(value >> 16)
                && frame[8] == (byte)(value >> 8) && frame[9] == (byte)value;
        }

        private static Dictionary<FieldInfo, object> CaptureAutoFields(DesignationModel model)
        {
            var values = new Dictionary<FieldInfo, object>();
            foreach (FieldInfo field in typeof(DesignationModel).GetFields(IF))
                if (field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal))
                    values[field] = field.GetValue(model);
            return values;
        }

        private static void RestoreAutoFields(DesignationModel model, Dictionary<FieldInfo, object> values)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in values) pair.Key.SetValue(model, pair.Value);
        }

        private static bool SameAutoFields(DesignationModel model, Dictionary<FieldInfo, object> values)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in values)
                if (!Equals(pair.Key.GetValue(model), pair.Value)) return false;
            return true;
        }

        private static void RestoreEntries(DesignationModel model, List<DesignationModel.Entry> entries)
        {
            FieldInfo field = typeof(DesignationModel).GetField("_entries", IF);
            var list = field?.GetValue(model) as List<DesignationModel.Entry>;
            if (list == null) return;
            list.Clear();
            list.AddRange(entries);
        }

        private static void SetAuto(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField("<" + name + ">k__BackingField", IF);
            field?.SetValue(target, value);
        }

        private static void RestoreHandlers(IDictionary handlers, Dictionary<int, object> oldHandlers)
        {
            if (handlers == null) return;
            foreach (int command in AllCommands)
            {
                if (handlers.Contains(command)) handlers.Remove(command);
                if (oldHandlers.TryGetValue(command, out object handler)) handlers[command] = handler;
            }
        }

        private static bool SameHandlers(IDictionary handlers, Dictionary<int, object> oldHandlers)
        {
            if (handlers == null) return oldHandlers.Count == 0;
            foreach (int command in AllCommands)
            {
                bool existed = oldHandlers.TryGetValue(command, out object oldHandler);
                if (handlers.Contains(command) != existed
                    || (existed && !ReferenceEquals(handlers[command], oldHandler))) return false;
            }
            return true;
        }

        private static bool SameRefs<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) where T : class
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static bool SameBag(IReadOnlyList<BagGoods> actual, IReadOnlyList<BagGoods> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY designation " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
