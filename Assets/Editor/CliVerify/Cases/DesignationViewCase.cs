using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Dsgt;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Designation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 人物→称号真实窗口验收：首屏资源、列表/详情、真实滚动、战力查询、材料大卡与热重开。
    /// 本用例必须使用图形设备运行；-nographics 下的 RenderTexture 灰帧不构成视觉证据。
    /// </summary>
    public static class DesignationViewCase
    {
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string EvidenceRoot =
            "output/ui_route_audit/2026-08-04_role_web_round2/cli_designation_20260805_2520/";

        public static async Task<int> Run()
        {
            bool fallbackBefore = ResManager.EditorPreferFallback;
            DesignationController controller = DesignationController.Instance;
            DesignationModel model = DesignationModel.Instance;
            BagModel bag = BagModel.Instance;
            bool controllerWasInitialized = controller.IsInitialized;
            Dictionary<FieldInfo, object> oldModelFields = CaptureAutoFields(model);
            var oldEntries = new List<DesignationModel.Entry>(model.Entries);
            var oldBag = new List<BagGoods>(bag.BagGoodsList);
            int oldCell = bag.CellNum;
            int oldMax = bag.MaxCell;
            bool oldBagHasData = bag.HasData;
            FieldInfo interceptField = typeof(DesignationController).GetField(
                "s_outboundIntercept", StaticPrivate);
            object oldIntercept = interceptField?.GetValue(null);
            CliVerify.Stage stage = null;
            GameObject eventSystemGo = null;
            bool pass = true;
            bool restored = false;

            try
            {
                ResManager.EditorPreferFallback = true;
                ResetDesignationFlow();
                IllusionTipsFlow.Reset();
                await Task.WhenAll(DesignationConfigs.EnsureLoaded(), GoodsModel.EnsureLoaded());
                if (DesignationConfigs.All.Count < 6)
                    throw new InvalidOperationException("称号配置少于首屏所需条目");

                DesignationConfigs.Row current = DesignationConfigs.Get(801001)
                    ?? DesignationConfigs.All[0];
                DesignationConfigs.Row materialRow = DesignationConfigs.Get(307001)
                    ?? DesignationConfigs.All.FirstOrDefault(row =>
                        row.Id != current.Id && DesignationConfigs.TryGetActivationCost(row.Id, out _));
                if (materialRow == null)
                    throw new InvalidOperationException("没有可验收材料大卡的未激活称号");
                DesignationConfigs.TryGetActivationCost(materialRow.Id, out DesignationConfigs.Cost materialCost);

                if (!controllerWasInitialized) controller.Init();
                if (interceptField == null)
                    throw new MissingFieldException("DesignationController.s_outboundIntercept");
                var frames = new List<byte[]>();
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                model.Reset();
                model.ReplaceData(current.Id, new List<DesignationModel.Entry>
                {
                    new DesignationModel.Entry(current.Id, 1, 0),
                });
                bag.SetBagFull(1, 50, new List<BagGoods>
                {
                    new BagGoods
                    {
                        GoodsId = 919001,
                        TypeId = materialCost.TypeId,
                        GoodsNum = materialCost.Num,
                        Cell = 1,
                    },
                });

                stage = CliVerify.Stage.Create();
                eventSystemGo = new GameObject("DesignationViewCase_EventSystem", typeof(EventSystem));
                EventSystem eventSystem = eventSystemGo.GetComponent<EventSystem>();
                Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
                Camera camera = canvas.worldCamera;
                GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();

                Stopwatch cold = Stopwatch.StartNew();
                DesignationFlow.Open();
                DsgtViewBind view = await WaitActive<DsgtViewBind>(20d);
                BaseWindowSkinView window = await WaitActive<BaseWindowSkinView>(5d);
                bool firstReady = await WaitUntil(() => IsFirstScreenReady(view, window), 20d);
                Canvas.ForceUpdateCanvases();
                TickEffectStage();
                await Task.Delay(100);
                TickEffectStage();
                bool dynamicRendered = CaptureDynamicEffectFrame("ui_305414", "designation_effect_rt.png",
                    out int effectAlpha, out int effectLit);
                firstReady &= dynamicRendered;
                cold.Stop();

                MethodInfo on41107 = typeof(DesignationController).GetMethod("On41107", InstancePrivate);
                Feed(on41107, controller, new CliVerify.Pkt().I(1).I(76869).Bytes());
                await Task.Delay(100);
                stage.ForceCjkFont();
                string mainShot = stage.Capture(EvidenceRoot + "main.png");

                DsgtItemRendererBind[] items = view != null
                    ? view.GetComponentsInChildren<DsgtItemRendererBind>(false)
                        .OrderBy(item => item.transform.GetSiblingIndex()).ToArray()
                    : Array.Empty<DsgtItemRendererBind>();
                DsgtDetailsItemBind details = view?.GetComponentInChildren<DsgtDetailsItemBind>(false);
                FightingShowSmallItem fight = details?.GetComponentInChildren<FightingShowSmallItem>(false);
                ScrollRect scroll = view?.dsgt_scoller;
                RectTransform viewport = scroll?.viewport;
                RectTransform content = scroll?.content;
                bool structure = scroll != null && scroll.vertical && !scroll.horizontal
                    && viewport != null && viewport.GetComponent<RectMask2D>() != null
                    && content != null && scroll.content == content
                    && items.Length == DesignationConfigs.All.Count
                    && content.rect.height >= items.Length * 119f;
                bool initialState = firstReady && details != null
                    && GetSelectedId() == current.Id
                    && details.dsgt_order_label?.text == "1阶"
                    && details.dsgt_adorn_button != null
                    && details.dsgt_adorn_button.gameObject.activeSelf
                    && details.labelDisplay?.text == "卸下"
                    && UniqueSurface(details.dsgt_adorn_button)
                    && fight != null && fight._lb_fighting?.text == "76869"
                    && items[0].dsgt_status_label?.text == "永久有效"
                    && items[0].dsgt_adorning_image != null
                    && items[0].dsgt_adorning_image.gameObject.activeSelf
                    && CountCommand(frames, 41101) == 1 && CountCommand(frames, 41107) == 1;

                MethodInfo on41102 = typeof(DesignationController).GetMethod("On41102", InstancePrivate);
                MethodInfo on41103 = typeof(DesignationController).GetMethod("On41103", InstancePrivate);
                frames.Clear();
                bool unwearClicked = Click(details?.dsgt_adorn_button, camera, raycaster, eventSystem);
                bool unwearSingleFlight = unwearClicked && ExactU32Frame(frames, 41103, current.Id)
                    && controller.HasPendingWear
                    && !controller.TryToggleWear(current.Id)
                    && frames.Count == 1;
                Feed(on41103, controller, new CliVerify.Pkt().I(7).Bytes());
                bool failurePreserved = model.CurrentUsedId == current.Id
                    && model.WearResult != null && model.WearResult.Code == 7
                    && model.WearResult.Id == current.Id && model.WearResult.IsUnwear
                    && CountCommand(frames, 41101) == 0 && !controller.HasPendingWear;

                frames.Clear();
                bool unwearRetry = Click(details?.dsgt_adorn_button, camera, raycaster, eventSystem)
                    && ExactU32Frame(frames, 41103, current.Id);
                Feed(on41103, controller, new CliVerify.Pkt().I(1).Bytes());
                bool unwearSuccess = unwearRetry && model.CurrentUsedId == current.Id
                    && CountCommand(frames, 41103) == 1 && CountCommand(frames, 41101) == 1
                    && controller.IsAwaitingWearRefresh(current.Id);
                Feed(typeof(DesignationController).GetMethod("On41101", InstancePrivate), controller,
                    new CliVerify.Pkt().I(0).H(1).I(current.Id).C(1).I(0).Bytes());
                await Task.Delay(50);
                details = view?.GetComponentInChildren<DsgtDetailsItemBind>(false);
                bool authoritativeUnwear = model.CurrentUsedId == 0
                    && !controller.IsAwaitingWearRefresh(current.Id)
                    && details?.dsgt_adorn_button != null
                    && details.dsgt_adorn_button.gameObject.activeSelf
                    && details.labelDisplay?.text == "佩戴";

                frames.Clear();
                bool wearClicked = Click(details?.dsgt_adorn_button, camera, raycaster, eventSystem)
                    && ExactU32Frame(frames, 41102, current.Id);
                Feed(on41102, controller, new CliVerify.Pkt().I(1).I(current.Id).Bytes());
                bool wearSuccess = wearClicked && model.CurrentUsedId == 0
                    && CountCommand(frames, 41102) == 1 && CountCommand(frames, 41101) == 1
                    && controller.IsAwaitingWearRefresh(current.Id);
                Feed(typeof(DesignationController).GetMethod("On41101", InstancePrivate), controller,
                    new CliVerify.Pkt().I(current.Id).H(1).I(current.Id).C(1).I(0).Bytes());
                await Task.Delay(50);
                details = view?.GetComponentInChildren<DsgtDetailsItemBind>(false);
                bool authoritativeWear = model.CurrentUsedId == current.Id
                    && !controller.IsAwaitingWearRefresh(current.Id)
                    && details?.labelDisplay?.text == "卸下";
                bool wearTransactions = unwearSingleFlight && failurePreserved && unwearSuccess
                    && authoritativeUnwear && wearSuccess && authoritativeWear;

                float beforeY = content?.anchoredPosition.y ?? 0f;
                bool dragged = DragVertical(scroll, camera, raycaster, eventSystem);
                await Task.Delay(100);
                float afterY = content?.anchoredPosition.y ?? 0f;
                bool dragMoved = dragged && afterY > beforeY + 20f;
                if (scroll != null)
                {
                    scroll.verticalNormalizedPosition = 0f;
                    Canvas.ForceUpdateCanvases();
                }
                DsgtItemRendererBind last = items.LastOrDefault();
                bool lastReachable = last != null && viewport != null
                    && OverlapsViewport(last.transform as RectTransform, viewport);
                stage.ForceCjkFont();
                string endShot = stage.Capture(EvidenceRoot + "list_end.png");

                int materialIndex = GetRuntimeIndex(materialRow.Id);
                if (scroll != null && materialIndex >= 0)
                {
                    float max = Mathf.Max(1f, content.rect.height - viewport.rect.height);
                    float rowCenter = materialIndex * 120f + 60f;
                    float targetY = Mathf.Clamp(rowCenter - viewport.rect.height * 0.5f, 0f, max);
                    scroll.StopMovement();
                    content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetY);
                    Canvas.ForceUpdateCanvases();
                }
                DsgtItemRendererBind materialItem = materialIndex >= 0 ? items[materialIndex] : null;
                bool selected = Click(materialItem?.bg_dsgtitem_png, camera, raycaster, eventSystem);
                bool detailsReady = selected && await WaitUntil(() =>
                {
                    DsgtDetailsItemBind d = view?.GetComponentInChildren<DsgtDetailsItemBind>(false);
                    return d != null && GetSelectedId() == materialRow.Id
                        && d.dsgt_awarditem_group != null && d.dsgt_awarditem_group.gameObject.activeSelf;
                }, 5d);
                details = view?.GetComponentInChildren<DsgtDetailsItemBind>(false);
                BaseAwardItem costItem = details?.GetComponentInChildren<BaseAwardItem>(false);
                bool costVisual = detailsReady && costItem != null && costItem.icon != null
                    && await WaitUntil(() => costItem.icon.sprite != null, 8d);
                stage.ForceCjkFont();
                string materialShot = stage.Capture(EvidenceRoot + "material.png");

                bool tipClick = costVisual && Click(costItem.click_group, camera, raycaster, eventSystem);
                bool tipReady = tipClick && await WaitUntil(
                    () => IllusionTipsFlow.ActiveView != null && IllusionTipsFlow.IsVisualReady, 20d);
                ItemTipsModalLayout tipLayout = FindActive<ItemTipsModalLayout>();
                bool tipIdentity = tipReady && tipLayout != null && IllusionTipsFlow.ActiveView != null
                    && IllusionTipsFlow.ActiveView.goods_name?.text?.Length > 0
                    && IllusionTipsFlow.ActiveView._img_bg != null
                    && IllusionTipsFlow.ActiveView._img_bg.sprite != null;
                stage.ForceCjkFont();
                string tipShot = stage.Capture(EvidenceRoot + "material_tip.png");
                bool tipClosed = tipIdentity && Click(tipLayout.dimBlocker, camera, raycaster, eventSystem)
                    && await WaitUntil(() => IllusionTipsFlow.ActiveView == null, 3d);

                DesignationFlow.Close();
                bool closed = await WaitUntil(() => view == null || !view.gameObject.activeInHierarchy, 3d);
                frames.Clear();
                Stopwatch warm = Stopwatch.StartNew();
                DesignationFlow.Open();
                bool reopened = await WaitUntil(() => view != null && view.gameObject.activeInHierarchy, 5d);
                warm.Stop();
                bool warmState = reopened && CountCommand(frames, 41101) == 1
                    && CountCommand(frames, 41107) == 1 && warm.ElapsedMilliseconds < 5000;

                pass = structure && initialState && wearTransactions && dynamicRendered && dragMoved && lastReachable
                    && detailsReady && costVisual && tipIdentity && tipClosed && closed && warmState;
                Debug.Log("CLIVERIFY designation-view coldMs=" + cold.ElapsedMilliseconds
                    + " warmMs=" + warm.ElapsedMilliseconds + " structure=" + structure
                    + " initial=" + initialState + " dynamic=" + dynamicRendered
                    + " wearTransactions=" + wearTransactions
                    + " effectAlpha=" + effectAlpha + " effectLit=" + effectLit
                    + " drag=" + dragMoved + " dragInvoked=" + dragged
                    + " beforeY=" + beforeY + " afterY=" + afterY + " last=" + lastReachable
                    + " details=" + detailsReady + " costVisual=" + costVisual
                    + " tipIdentity=" + tipIdentity + " tipClosed=" + tipClosed
                    + " closed=" + closed + " warm=" + warmState
                    + " shots=" + mainShot + "," + endShot + "," + materialShot + "," + tipShot);
            }
            catch (Exception e)
            {
                pass = false;
                Debug.LogError("CLIVERIFY designation-view EXCEPTION " + e);
            }
            finally
            {
                ResetDesignationFlow();
                IllusionTipsFlow.Reset();
                if (eventSystemGo != null) Object.DestroyImmediate(eventSystemGo);
                stage?.Dispose();
                if (controller.IsInitialized && !controllerWasInitialized) controller.Dispose();
                model.Reset();
                RestoreEntries(model, oldEntries);
                RestoreAutoFields(model, oldModelFields);
                bag.SetBagFull(oldCell, oldMax, oldBag);
                SetAuto(bag, "HasData", oldBagHasData);
                if (interceptField != null) interceptField.SetValue(null, oldIntercept);
                ResManager.EditorPreferFallback = fallbackBefore;
                restored = controller.IsInitialized == controllerWasInitialized
                    && SameAutoFields(model, oldModelFields)
                    && SameEntries(model.Entries, oldEntries)
                    && bag.HasData == oldBagHasData;
            }

            bool finalPass = pass && restored;
            Debug.Log("CLIVERIFY designation-view restored=" + restored);
            Debug.Log("CLIVERIFY designation-view VERDICT pass=" + finalPass);
            return finalPass ? 0 : 3;
        }

        private static bool IsFirstScreenReady(DsgtViewBind view, BaseWindowSkinView window)
        {
            if (view == null || window == null || window._img_bg?.sprite == null
                || window._img_title?.sprite == null) return false;
            DsgtItemRendererBind[] items = view.GetComponentsInChildren<DsgtItemRendererBind>(false)
                .OrderBy(item => item.transform.GetSiblingIndex()).ToArray();
            if (items.Length != DesignationConfigs.All.Count) return false;
            IReadOnlyList<DesignationConfigs.Row> rows = GetRuntimeRows();
            if (rows == null || rows.Count != items.Length) return false;
            int visibleCount = Mathf.Min(5, items.Length);
            for (int i = 0; i < visibleCount; i++)
            {
                bool ready = rows[i].Type == 1
                    ? items[i]._gp_dsgt_effect != null
                        && items[i]._gp_dsgt_effect.gameObject.activeSelf
                    : items[i].resource_image != null && items[i].resource_image.sprite != null;
                if (!ready) return false;
            }
            DsgtDetailsItemBind details = view.GetComponentInChildren<DsgtDetailsItemBind>(false);
            return details?.dsgt_icon_image?.sprite != null;
        }

        private static bool DragVertical(ScrollRect scroll, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (scroll?.viewport == null || scroll.content == null || camera == null
                || raycaster == null || eventSystem == null) return false;
            Canvas.ForceUpdateCanvases();
            RectTransform viewport = scroll.viewport;
            Vector3 worldStart = viewport.TransformPoint(viewport.rect.center);
            Vector2 start = RectTransformUtility.WorldToScreenPoint(camera, worldStart);
            Vector2 end = start + new Vector2(0f, 240f);
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = start,
                pressPosition = start,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            RaycastResult hit = hits.FirstOrDefault(result => result.gameObject != null
                && result.gameObject.transform.IsChildOf(viewport));
            if (hit.gameObject == null) return false;
            pointer.pointerPressRaycast = hit;
            pointer.pointerCurrentRaycast = hit;
            scroll.StopMovement();
            pointer.pointerDrag = scroll.gameObject;
            pointer.dragging = true;
            scroll.OnBeginDrag(pointer);
            pointer.delta = end - start;
            pointer.position = end;
            scroll.OnDrag(pointer);
            scroll.OnEndDrag(pointer);
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private static void TickEffectStage()
        {
            typeof(UIEffectStage).GetMethod("Tick", StaticPrivate)?.Invoke(null, null);
        }

        private static bool CaptureDynamicEffectFrame(string label, string fileName,
            out int alphaPixels, out int litPixels)
        {
            alphaPixels = 0;
            litPixels = 0;
            List<UIEffectStage.EffectDiagnostic> diagnostics = UIEffectStage.CollectDiagnostics();
            for (int i = 0; i < diagnostics.Count; i++)
            {
                UIEffectStage.EffectDiagnostic item = diagnostics[i];
                Debug.Log("CLIVERIFY designation-effect label=" + item.Label + " parent=" + item.ParentName
                    + " channel=" + item.Channel + " alive=" + item.EffectAlive
                    + " active=" + item.EffectActiveInHierarchy + " renderers=" + item.RendererCount);
            }
            UIEffectStage.EffectDiagnostic diagnostic = diagnostics.Find(item =>
                string.Equals(item.Label, label, StringComparison.OrdinalIgnoreCase));
            UIEffectStage.ChannelDiagnostic channel = UIEffectStage.CollectChannelDiagnostics()
                .Find(item => item.Name == diagnostic.Channel);
            if (!diagnostic.EffectAlive || !diagnostic.EffectActiveInHierarchy
                || channel.Camera == null || channel.Texture == null || channel.Image == null)
                return false;

            channel.Image.gameObject.SetActive(true);
            channel.Camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = channel.Texture;
            var copy = new Texture2D(channel.Texture.width, channel.Texture.height,
                TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0f, 0f, channel.Texture.width, channel.Texture.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            Color32[] pixels = copy.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                if (pixel.a > 2) alphaPixels++;
                if (pixel.r > 2 || pixel.g > 2 || pixel.b > 2) litPixels++;
            }

            string path = Path.GetFullPath(EvidenceRoot + fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, copy.EncodeToPNG());
            Object.DestroyImmediate(copy);
            return alphaPixels > 100 && litPixels > 100;
        }

        private static uint GetSelectedId()
        {
            FieldInfo field = typeof(DesignationFlow).GetField("_selectedId", StaticPrivate);
            return field != null && field.GetValue(null) is uint value ? value : 0u;
        }

        private static IReadOnlyList<DesignationConfigs.Row> GetRuntimeRows()
        {
            FieldInfo field = typeof(DesignationFlow).GetField("RuntimeRows", StaticPrivate);
            return field?.GetValue(null) as IReadOnlyList<DesignationConfigs.Row>;
        }

        private static int GetRuntimeIndex(uint id)
        {
            IReadOnlyList<DesignationConfigs.Row> rows = GetRuntimeRows();
            if (rows == null) return -1;
            for (int i = 0; i < rows.Count; i++)
                if (rows[i] != null && rows[i].Id == id) return i;
            return -1;
        }

        private static bool Click(Component target, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (target == null || camera == null || raycaster == null || eventSystem == null
                || !target.gameObject.activeInHierarchy) return false;
            Graphic surface = target as Graphic ?? target.GetComponent<Graphic>()
                ?? target.GetComponentInChildren<Graphic>(true);
            if (surface == null || !surface.enabled || !surface.raycastTarget) return false;
            Canvas.ForceUpdateCanvases();
            RectTransform rect = surface.rectTransform;
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(
                    camera, rect.TransformPoint(rect.rect.center)),
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject != surface.gameObject
                    && !hit.gameObject.transform.IsChildOf(target.transform)) continue;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                    hit.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return true;
            }
            return false;
        }

        private static bool UniqueSurface(RectTransform root)
        {
            if (root == null) return false;
            Image surface = root.GetComponent<Image>();
            if (surface == null || !surface.raycastTarget) return false;
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                if (graphic != surface && graphic.raycastTarget) return false;
            return true;
        }

        private static bool ExactU32Frame(IReadOnlyList<byte[]> frames, int command, uint value)
        {
            if (frames == null || frames.Count != 1 || frames[0] == null) return false;
            byte[] frame = frames[0];
            return frame.Length == 10 && frame[0] == 0 && frame[1] == 10
                && frame[4] == (byte)(command >> 8) && frame[5] == (byte)command
                && frame[6] == (byte)(value >> 24) && frame[7] == (byte)(value >> 16)
                && frame[8] == (byte)(value >> 8) && frame[9] == (byte)value;
        }

        private static bool OverlapsViewport(RectTransform item, RectTransform viewport)
        {
            if (item == null || viewport == null) return false;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
            return bounds.max.y >= viewport.rect.yMin && bounds.min.y <= viewport.rect.yMax;
        }

        private static async Task<T> WaitActive<T>(double timeoutSeconds) where T : Component
        {
            T result = null;
            await WaitUntil(() => (result = FindActive<T>()) != null, timeoutSeconds);
            return result;
        }

        private static T FindActive<T>() where T : Component
        {
            foreach (T value in Object.FindObjectsByType<T>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (value != null && value.gameObject.activeInHierarchy) return value;
            return null;
        }

        private static async Task<bool> WaitUntil(Func<bool> predicate, double timeoutSeconds)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                if (predicate()) return true;
                await Task.Delay(50);
            }
            return predicate();
        }

        private static void Feed(MethodInfo handler, object target, byte[] packet)
        {
            if (handler == null) throw new MissingMethodException("designation handler missing");
            handler.Invoke(target, new object[] { new NetReader(packet, 0, packet.Length) });
        }

        private static int CountCommand(IEnumerable<byte[]> frames, int command)
            => frames.Count(frame => frame != null && frame.Length >= 6
                && ((frame[4] << 8) | frame[5]) == command);

        private static Dictionary<FieldInfo, object> CaptureAutoFields(DesignationModel model)
        {
            var values = new Dictionary<FieldInfo, object>();
            foreach (FieldInfo field in typeof(DesignationModel).GetFields(InstancePrivate))
                if (field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal))
                    values[field] = field.GetValue(model);
            return values;
        }

        private static void RestoreAutoFields(DesignationModel model,
            Dictionary<FieldInfo, object> values)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in values)
                pair.Key.SetValue(model, pair.Value);
        }

        private static bool SameAutoFields(DesignationModel model,
            Dictionary<FieldInfo, object> values)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in values)
                if (!Equals(pair.Key.GetValue(model), pair.Value)) return false;
            return true;
        }

        private static void RestoreEntries(DesignationModel model,
            IEnumerable<DesignationModel.Entry> entries)
        {
            var list = typeof(DesignationModel).GetField("_entries", InstancePrivate)
                ?.GetValue(model) as List<DesignationModel.Entry>;
            list?.Clear();
            if (list != null) list.AddRange(entries);
        }

        private static bool SameEntries(IReadOnlyList<DesignationModel.Entry> actual,
            IReadOnlyList<DesignationModel.Entry> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++)
                if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static void SetAuto(object target, string name, object value)
            => target.GetType().GetField("<" + name + ">k__BackingField", InstancePrivate)
                ?.SetValue(target, value);

        private static void ResetDesignationFlow()
            => typeof(DesignationFlow).GetMethod("Reset", StaticPrivate)?.Invoke(null, null);
    }
}
