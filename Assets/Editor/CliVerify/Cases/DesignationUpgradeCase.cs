using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Generated.UI.Dsgt;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Designation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>R526：41106 称号升阶的配置、资产门禁、权威刷新与真实 Prefab 点击链。</summary>
    public static class DesignationUpgradeCase
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
                await GoodsModel.EnsureLoaded();
                return RunSync();
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY designation-upgrade EXCEPTION " + e);
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
            GameObject detailsGo = null;
            RenderTexture warmup = null;
            bool pass = true;
            bool restored = false;
            try
            {
                DesignationConfigs.Row row = FindUpgradeRow(out byte currentOrder,
                    out DesignationConfigs.Cost cost);
                Check(ref pass, "order config/current-next/single-real-bag-cost",
                    row != null && currentOrder > 0 && cost != null
                    && row.MainType == 3 && row.OrderLimit > currentOrder
                    && DesignationConfigs.GetDisplayAttrs(row.Id, currentOrder)?.Count > 0
                    && !DesignationConfigs.TryGetUpgradeCost(row.Id, row.OrderLimit, out _));
                if (!pass) return 3;

                controller.Init();
                model.Reset();
                model.ReplaceData(777, new List<DesignationModel.Entry>
                    { new DesignationModel.Entry(row.Id, currentOrder, 4000000000U) });
                model.ReplaceGoodsActivationResult(8, 9, 10, 11);
                DesignationModel.GoodsActivationResultSnapshot activationSentinel = model.GoodsActivationResult;
                bag.SetBagFull(1, 50, new List<BagGoods>
                {
                    new BagGoods { GoodsId = 91001, TypeId = cost.TypeId, GoodsNum = cost.Num, Cell = 1 },
                });

                MethodInfo on01 = Handler("On41101");
                MethodInfo on06 = Handler("On41106");
                Check(ref pass, "constant/handler/registration boundary",
                    Proto.DESIGNATION_UPGRADE == 41106 && intercept != null
                    && on01 != null && on06 != null && handlers != null && handlers.Contains(41106)
                    && !handlers.Contains(41102) && !handlers.Contains(41103) && !handlers.Contains(41110));

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                long beforeGoods = bag.GetTypeGoodsNum(cost.TypeId);
                bool firstSend = controller.TryUpgrade(row.Id);
                bool duplicateBlocked = !controller.TryUpgrade(row.Id);
                bool crossWriteBlocked = !controller.TryActivateByGoods(row.Id);
                Check(ref pass, "exact frame/family single-flight/no optimistic asset or list patch",
                    firstSend && duplicateBlocked && crossWriteBlocked
                    && U32Frame(frames, 41106, row.Id) && controller.HasPendingUpgrade
                    && !controller.HasPendingActivation && bag.GetTypeGoodsNum(cost.TypeId) == beforeGoods
                    && model.CurrentUsedId == 777 && model.Entries.Count == 1
                    && model.Entries[0].Order == currentOrder);
                frames.Clear();

                Check(ref pass, "failure raw overwrite/read-end/preserves authority/no refresh",
                    Feed(on06, controller, new CliVerify.Pkt().I(uint.MaxValue).C(byte.MaxValue)
                        .I(uint.MaxValue).I(uint.MaxValue).I(uint.MaxValue))
                    && !controller.HasPendingUpgrade && model.UpgradeResult != null
                    && model.UpgradeResult.Code == uint.MaxValue
                    && model.UpgradeResult.Order == byte.MaxValue
                    && model.UpgradeResult.Power == uint.MaxValue
                    && model.UpgradeResult.CurrentUsedId == uint.MaxValue
                    && model.UpgradeResult.Id == uint.MaxValue
                    && ReferenceEquals(model.GoodsActivationResult, activationSentinel)
                    && model.CurrentUsedId == 777 && model.Entries[0].Order == currentOrder
                    && frames.Count == 0 && bag.GetTypeGoodsNum(cost.TypeId) == beforeGoods);

                Check(ref pass, "retry after failure", controller.TryUpgrade(row.Id)
                    && U32Frame(frames, 41106, row.Id));
                frames.Clear();
                byte returnedOrder = checked((byte)(currentOrder + 1));
                Check(ref pass, "success raw then exactly one authoritative 41101 refresh",
                    Feed(on06, controller, new CliVerify.Pkt().I(1).C(returnedOrder)
                        .I(7654321).I(123).I(row.Id))
                    && !controller.HasPendingUpgrade && model.UpgradeResult.Code == 1
                    && model.UpgradeResult.Order == returnedOrder
                    && model.UpgradeResult.Power == 7654321
                    && model.UpgradeResult.CurrentUsedId == 123 && model.UpgradeResult.Id == row.Id
                    && model.CurrentUsedId == 777 && model.Entries[0].Order == currentOrder
                    && controller.IsAwaitingUpgradeRefresh(row.Id) && !controller.TryUpgrade(row.Id)
                    && EmptyFrame(frames, 41101) && bag.GetTypeGoodsNum(cost.TypeId) == beforeGoods);
                frames.Clear();

                Check(ref pass, "41101 full authority replaces order and clears refresh guard",
                    Feed(on01, controller, new CliVerify.Pkt().I(123).H(1)
                        .I(row.Id).C(returnedOrder).I(4000000001L))
                    && model.CurrentUsedId == 123 && model.Entries.Count == 1
                    && model.Entries[0].Id == row.Id && model.Entries[0].Order == returnedOrder
                    && model.Entries[0].EndTime == 4000000001U
                    && !controller.IsAwaitingUpgradeRefresh(row.Id) && frames.Count == 0);

                model.ReplaceData(777, new List<DesignationModel.Entry>
                    { new DesignationModel.Entry(row.Id, currentOrder, 4000000000U) });
                bool realClick = VerifyRealPrefab(row, currentOrder, cost, frames, controller, on06,
                    ref canvasGo, ref cameraGo, ref eventSystemGo, ref buttonGo, ref detailsGo, ref warmup);
                Check(ref pass, "real prefab material and GraphicRaycaster pointer chain", realClick);

                model.ReplaceData(0, new List<DesignationModel.Entry>
                    { new DesignationModel.Entry(row.Id, checked((byte)row.OrderLimit), 0) });
                frames.Clear();
                Check(ref pass, "max order rejects without packet", !controller.TryUpgrade(row.Id)
                    && frames.Count == 0 && model.Entries[0].Order == row.OrderLimit);

                controller.Dispose();
                Check(ref pass, "dispose reset", !controller.IsInitialized && !model.HasData
                    && model.Entries.Count == 0 && model.UpgradeResult == null
                    && !controller.HasPendingUpgrade && !controller.IsAwaitingUpgradeRefresh(row.Id));
            }
            finally
            {
                if (warmup != null) { warmup.Release(); UnityEngine.Object.DestroyImmediate(warmup); }
                if (detailsGo != null) UnityEngine.Object.DestroyImmediate(detailsGo);
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
                    && bag.HasData == oldBagHasData && SameRefs(bag.BagGoodsList, oldBag)
                    && SameHandlers(handlers, oldHandlers)
                    && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldIntercept));
            }

            bool finalPass = pass && restored;
            Debug.Log("CLIVERIFY designation-upgrade restored=" + restored);
            Debug.Log("CLIVERIFY designation-upgrade VERDICT pass=" + finalPass);
            return finalPass ? 0 : 3;
        }

        private static DesignationConfigs.Row FindUpgradeRow(out byte order, out DesignationConfigs.Cost cost)
        {
            foreach (DesignationConfigs.Row row in DesignationConfigs.All)
            {
                for (int value = 1; value < row.OrderLimit && value < byte.MaxValue; value++)
                {
                    if (!DesignationConfigs.TryGetUpgradeCost(row.Id, value, out cost)) continue;
                    order = (byte)value;
                    return row;
                }
            }
            order = 0;
            cost = null;
            return null;
        }

        private static bool VerifyRealPrefab(DesignationConfigs.Row row, byte order,
            DesignationConfigs.Cost cost, List<byte[]> frames, DesignationController controller,
            MethodInfo on06, ref GameObject canvasGo, ref GameObject cameraGo,
            ref GameObject eventSystemGo, ref GameObject buttonGo, ref GameObject detailsGo,
            ref RenderTexture warmup)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            DsgtDetailsItemBind template = prefab != null
                ? prefab.GetComponentInChildren<DsgtDetailsItemBind>(true) : null;
            if (template == null || template.dsgt_Activate_button == null) return false;

            FieldInfo detailsField = typeof(DesignationFlow).GetField("_details", SF);
            FieldInfo costField = typeof(DesignationFlow).GetField("_costItem", SF);
            MethodInfo render = typeof(DesignationFlow).GetMethod("RenderActivation", SF);
            MethodInfo bind = typeof(DesignationFlow).GetMethod("BindUpgradeClick", SF);
            object oldDetails = detailsField?.GetValue(null);
            object oldCost = costField?.GetValue(null);
            if (detailsField == null || costField == null || render == null || bind == null) return false;

            detailsGo = UnityEngine.Object.Instantiate(template.gameObject);
            detailsGo.SetActive(true);
            DsgtDetailsItemBind details = detailsGo.GetComponent<DsgtDetailsItemBind>();
            if (details == null) return false;
            details.Show();
            bool material;
            try
            {
                detailsField.SetValue(null, details);
                costField.SetValue(null, null);
                var entry = new DesignationModel.Entry(row.Id, order, 4000000000U);
                render.Invoke(null, new object[] { row, entry });
                var costItem = costField.GetValue(null) as Component;
                Image detailsSurface = details.dsgt_Activate_button.GetComponent<Image>();
                material = details.dsgt_Activate_button.gameObject.activeSelf
                    && details.labelDisplay1.text == "升阶"
                    && details.dsgt_expend_label.text == "升阶消耗："
                    && details.dsgt_number_label.text
                        == BagModel.Instance.GetTypeGoodsNum(cost.TypeId) + "/" + cost.Num
                    && details.dsgt_red_image.gameObject.activeSelf
                    && costItem != null && costItem.gameObject.activeSelf
                    && UniqueSurface(details.dsgt_Activate_button, detailsSurface);
            }
            finally
            {
                detailsField.SetValue(null, oldDetails);
                costField.SetValue(null, oldCost);
            }

            canvasGo = new GameObject("DesignationUpgradeCase_Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(GraphicRaycaster));
            ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(960f, 640f);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            cameraGo = new GameObject("DesignationUpgradeCase_Camera", typeof(Camera));
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
            eventSystemGo = new GameObject("DesignationUpgradeCase_EventSystem", typeof(EventSystem));
            EventSystem eventSystem = eventSystemGo.GetComponent<EventSystem>();

            buttonGo = UnityEngine.Object.Instantiate(
                template.dsgt_Activate_button.gameObject, canvasGo.transform, false);
            buttonGo.SetActive(true);
            RectTransform button = (RectTransform)buttonGo.transform;
            button.anchorMin = button.anchorMax = button.pivot = new Vector2(0.5f, 0.5f);
            button.anchoredPosition = Vector2.zero;
            bind.Invoke(null, new object[] { button, row.Id });
            Image surface = button.GetComponent<Image>();
            bool unique = UniqueSurface(button, surface);

            canvas.enabled = false;
            canvas.enabled = true;
            Canvas.ForceUpdateCanvases();
            warmup = new RenderTexture(new RenderTextureDescriptor(Math.Max(1, Screen.width),
                Math.Max(1, Screen.height), RenderTextureFormat.ARGB32, 24) { msaaSamples = 1 });
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
            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject != buttonGo) continue;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                    hit.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                pointerHit = true;
                break;
            }
            bool sent = U32Frame(frames, 41106, row.Id) && controller.HasPendingUpgrade;
            ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                buttonGo, pointer, ExecuteEvents.pointerClickHandler);
            bool singleFlight = frames.Count == 1;
            bool response = Feed(on06, controller, new CliVerify.Pkt().I(2).C(0).I(0).I(0).I(row.Id))
                && !controller.HasPendingUpgrade && frames.Count == 1;

            return material && unique && pointerHit && sent && singleFlight && response;
        }

        private static bool UniqueSurface(RectTransform root, Image surface)
        {
            if (root == null || surface == null || !surface.raycastTarget) return false;
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                if (graphic != surface && graphic.raycastTarget) return false;
            return true;
        }

        private static MethodInfo Handler(string name)
            => typeof(DesignationController).GetMethod(name, IF);

        private static bool Feed(MethodInfo method, DesignationController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool EmptyFrame(IReadOnlyList<byte[]> frames, int command)
            => frames.Count == 1 && frames[0] != null && frames[0].Length == 6
                && frames[0][0] == 0 && frames[0][1] == 6
                && frames[0][4] == (byte)(command >> 8) && frames[0][5] == (byte)command;

        private static bool U32Frame(IReadOnlyList<byte[]> frames, int command, uint value)
            => frames.Count == 1 && frames[0] != null && frames[0].Length == 10
                && frames[0][0] == 0 && frames[0][1] == 10
                && frames[0][4] == (byte)(command >> 8) && frames[0][5] == (byte)command
                && frames[0][6] == (byte)(value >> 24) && frames[0][7] == (byte)(value >> 16)
                && frames[0][8] == (byte)(value >> 8) && frames[0][9] == (byte)value;

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
            var list = typeof(DesignationModel).GetField("_entries", IF)?.GetValue(model)
                as List<DesignationModel.Entry>;
            list?.Clear();
            list?.AddRange(entries);
        }

        private static void SetAuto(object target, string name, object value)
            => target.GetType().GetField("<" + name + ">k__BackingField", IF)?.SetValue(target, value);

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

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY designation-upgrade " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
