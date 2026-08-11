using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Fashion;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 角色→外观→时装(pos=1)专用离线预检。只打开 FashionMain 与 FashionLevel，禁止切换发饰、
    /// Dress/装扮和套装；真实组合窗框、GraphicRaycaster 点击、41312 身份、滚动裁剪、模型 RT、
    /// 空材料态和两条关闭链均在同一舞台验证。它只作为真实 old/Unity Web 前的低成本门禁。
    /// </summary>
    public static class FashionMainPos1Case
    {
        private const int PosId = 1;
        private const int SweetheartId = 12010008;
        private static readonly int[] ForbiddenWrites = { 41301, 41302, 41303, 41304, 41305, 41306, 41316 };
        private static readonly string EvidenceRoot = GetCommandLineValue("-fashionEvidenceRoot",
            "output/ui_route_audit/fashion-main-pos1-cli-manual").TrimEnd('/', '\\');

        public static void RunBatch() => _ = RunBatchAsync();

        private static async Task RunBatchAsync()
        {
            int code;
            try { code = await Run(); }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY fashion-main-pos1 exception: " + exception);
                code = 1;
            }
            UnityEditor.EditorApplication.Exit(code);
        }

        private static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            GameObject eventSystemObject = null;
            object oldOutbound = null;
            FieldInfo outboundField = null;
            var frames = new List<byte[]>();
            bool pass = true;
            try
            {
                await FashionConfigs.EnsureLoaded();
                await GoodsModel.EnsureLoaded();
                await LoginConfigs.EnsureLoaded();
                IReadOnlyList<int> ids = FashionConfigs.GetFashionIds(PosId);
                Check(ref pass, "config pos1 and sweetheart", ids.Count >= 6 && ids.Contains(SweetheartId),
                    "count=" + ids.Count);
                if (ids.Count == 0 || !ids.Contains(SweetheartId)) return 3;

                SeedFigure(ids[0]);
                SeedModel(ids[0]);

                FashionController controller = FashionController.Instance;
                outboundField = controller.GetType().GetField("s_outboundIntercept",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (outboundField != null)
                {
                    oldOutbound = outboundField.GetValue(null);
                    outboundField.SetValue(null, new Func<byte[], bool>(frame =>
                    {
                        frames.Add(frame);
                        return true;
                    }));
                }
                Check(ref pass, "outbound interceptor", outboundField != null);

                eventSystemObject = new GameObject("FashionMainPos1Case_EventSystem", typeof(EventSystem));
                EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
                Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
                Camera camera = canvas != null ? canvas.worldCamera : null;
                GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
                Check(ref pass, "stage input", camera != null && raycaster != null && eventSystem != null);

                FashionFlow.Open(0);
                FashionMainView main = await WaitForActive<FashionMainView>(15000);
                stage.ForceCjkFont();
                string openShot = stage.Capture(Evidence("open.png"));
                Check(ref pass, "combined FashionMain active", main != null, openShot);
                if (main == null) return 3;

                Check(ref pass, "single-tab content only",
                    FindActive<Shenxiao.Module.Core.Dress.DressView>() == null
                    && FindActive<FashionSuitView>() == null && main.PosId == PosId);

                FashionItem[] items = main.GetComponentsInChildren<FashionItem>(true)
                    .Where(item => item != null && item.gameObject.activeInHierarchy && item.FashionId > 0)
                    .ToArray();
                bool identityOk = items.Length == ids.Count
                    && items.Select(item => item.FashionId).Distinct().Count() == items.Length
                    && items.All(item => ids.Contains(item.FashionId))
                    && items.All(HasUniqueClickSurface);
                Check(ref pass, "list item identity and unique click surface", identityOk,
                    "items=" + items.Length + " ids=" + ids.Count);
                Check(ref pass, "selected/unselected state",
                    items.Count(item => item.select != null && item.select.gameObject.activeSelf) == 1
                    && items.Any(item => item.select != null && !item.select.gameObject.activeSelf));
                Check(ref pass, "owned/unowned state",
                    items.Any(item => FashionModel.Instance.IsActivated(PosId, item.FashionId))
                    && items.Any(item => !FashionModel.Instance.IsActivated(PosId, item.FashionId)));
                Check(ref pass, "attribute name/value prefab layout", VerifyAttributeNameValueLayout(main),
                    "pure text row, preferred widths, 8px gap");
                bool costCellLayout = VerifyCostCellLayout(main, out string costCellDetail);
                Check(ref pass, "cost cell fits slot and keeps order label visible", costCellLayout,
                    costCellDetail);

                ScrollRect listScroll = main._list_fashion_item != null
                    ? main._list_fashion_item.GetComponentInParent<ScrollRect>() : null;
                FashionItem sweetheart = items.FirstOrDefault(item => item.FashionId == SweetheartId);
                bool scrollStructure = listScroll != null && listScroll.viewport != null && listScroll.content != null
                    && listScroll.viewport.GetComponent<RectMask2D>() != null
                    && listScroll.content.GetComponent<HorizontalLayoutGroup>() != null
                    && listScroll.content.GetComponent<ContentSizeFitter>() != null;
                Check(ref pass, "fashion ScrollRect/Viewport/Content", scrollStructure);

                float beforeX = listScroll != null && listScroll.content != null
                    ? listScroll.content.anchoredPosition.x : 0f;
                FashionItem firstItem = items.FirstOrDefault();
                FashionItem lastItem = items.LastOrDefault();
                int dragCount = 0;
                while (lastItem != null && !IsInsideViewport(lastItem.ClickSurface, listScroll, camera)
                    && dragCount++ < 32)
                {
                    if (!DragHorizontal(listScroll, camera, raycaster, eventSystem)) break;
                }
                float rawAfterX = listScroll != null && listScroll.content != null
                    ? listScroll.content.anchoredPosition.x : 0f;
                // CLI Stage 运行在 EditMode，ScrollRect.Update 不会像 Player 一样逐帧把 Elastic
                // 过拉回弹到合法边界；真实 PointerDrag 已发生后，显式结算到末端再验裁剪/末项。
                if (listScroll != null)
                {
                    listScroll.horizontalNormalizedPosition = 1f;
                    listScroll.StopMovement();
                    Canvas.ForceUpdateCanvases();
                }
                float settledAfterX = listScroll != null && listScroll.content != null
                    ? listScroll.content.anchoredPosition.x : 0f;
                bool dragOk = Math.Abs(rawAfterX - beforeX) >= 20f
                    && IsInsideViewport(lastItem?.ClickSurface, listScroll, camera)
                    && !IsInsideViewport(firstItem?.ClickSurface, listScroll, camera);
                string dragShot = stage.Capture(Evidence("drag-last-visible.png"));
                Check(ref pass, "real drag clipping and last item reachable", dragOk,
                    "before=" + beforeX + " raw=" + rawAfterX + " settled=" + settledAfterX
                    + " drags=" + dragCount);

                frames.Clear();
                int selectedBeforeDragRelease = main.SelectedFashionId;
                bool dragReleaseSuppressed = DispatchDisplacedPointerClick(
                    lastItem?.ClickSurface, camera, raycaster, eventSystem);
                await Task.Delay(50);
                Check(ref pass, "drag release does not select or send 41312",
                    dragReleaseSuppressed && main.SelectedFashionId == selectedBeforeDragRelease
                    && frames.Count == 0,
                    "selectedBefore=" + selectedBeforeDragRelease + " selectedAfter=" + main.SelectedFashionId
                    + " frames=" + string.Join(",", frames.Select(FrameProtocol)));

                frames.Clear();
                bool lastClick = Click(lastItem?.ClickSurface, camera, raycaster, eventSystem);
                await Task.Delay(50);
                bool lastProtocol = lastItem != null && frames.Any(frame => FrameEquals(frame,
                    Proto.FASHION_POWER, new CliVerify.Pkt().C(PosId).I(lastItem.FashionId).Bytes()));
                Check(ref pass, "click last item after real drag", lastClick && lastProtocol, dragShot);

                // 上面的真实拖动已经证明裁剪、末项和滚动后点击链。身份专项随后把指定格
                // 精确置入 viewport，再继续走真实 GraphicRaycaster 点击，避免一次弹性拖动越过中间格。
                bool sweetheartVisible = BringIntoViewport(sweetheart?.ClickSurface, listScroll, camera);
                Check(ref pass, "sweetheart brought into viewport", sweetheartVisible);

                frames.Clear();
                bool sweetheartClick = Click(sweetheart?.ClickSurface, camera, raycaster, eventSystem);
                await Task.Delay(100);
                string sweetheartShot = stage.Capture(Evidence("sweetheart.png"));
                TMP_Text name = main._lb_name;
                bool protocol41312 = frames.Any(frame => FrameEquals(frame, Proto.FASHION_POWER,
                    new CliVerify.Pkt().C(PosId).I(SweetheartId).Bytes()));
                bool noWrites = frames.All(frame => !ForbiddenWrites.Contains(FrameProtocol(frame)));
                Check(ref pass, "sweetheart real click", sweetheartClick && main.SelectedFashionId == SweetheartId,
                    sweetheartShot);
                Check(ref pass, "sweetheart exact UI identity", name != null && name.text == "甜心宝贝",
                    name != null ? name.text : "<null>");
                Check(ref pass, "sweetheart unowned order state",
                    main._lb_order != null && main._lb_order.gameObject.activeInHierarchy
                    && main._lb_order.text == "[未激活]",
                    main._lb_order != null ? main._lb_order.text : "<null>");
                Check(ref pass, "41312 ci(1,12010008) and no write", protocol41312 && noWrites,
                    string.Join(",", frames.Select(FrameProtocol)));

                RawImage modelImage = main._box_model != null
                    ? main._box_model.GetComponentInChildren<RawImage>(true) : null;
                int modelPixels = await WaitForModelPixels(main, modelImage, 15000,
                    Evidence("model-rt.png"));
                Check(ref pass, "model RT actual pixels", modelPixels >= 64, "pixels=" + modelPixels);
                Check(ref pass, "model parts and persistent effect",
                    main.PreviewHasWeapon && main.PreviewEffectCount > 0,
                    "weapon=" + main.PreviewHasWeapon + " effects=" + main.PreviewEffectCount);

                FashionColorItem color1 = main.GetComponentsInChildren<FashionColorItem>(true)
                    .FirstOrDefault(item => item != null && item.gameObject.activeInHierarchy && item.ColorId == 1);
                frames.Clear();
                bool colorClick = Click(color1?.ClickSurface, camera, raycaster, eventSystem);
                await Task.Delay(100);
                int colorPixels = await WaitForModelPixels(main, modelImage, 15000,
                    Evidence("model-color1-rt.png"));
                Check(ref pass, "locked color preview switch without transaction",
                    colorClick && main.SelectedColorId == 1 && main.RenderedColorId == 1
                    && colorPixels >= 64 && frames.All(frame => !ForbiddenWrites.Contains(FrameProtocol(frame))),
                    "selected=" + main.SelectedColorId + " rendered=" + main.RenderedColorId
                    + " texture=" + main.RenderedTextureName + " pixels=" + colorPixels);

                frames.Clear();
                bool openLevel = Click(main._img_grade, camera, raycaster, eventSystem);
                FashionLevelView level = await WaitForActive<FashionLevelView>(5000);
                string levelShot = stage.Capture(Evidence("level.png"));
                ScrollRect materialScroll = level != null ? level.flv_scroller : null;
                bool materialStructure = materialScroll != null && materialScroll.viewport != null
                    && materialScroll.content != null
                    && materialScroll.viewport.GetComponent<RectMask2D>() != null
                    && materialScroll.content.GetComponent<GridLayoutGroup>() != null
                    && materialScroll.content.GetComponent<ContentSizeFitter>() != null;
                bool inertEffectHost = level != null && level.effect_group != null
                    && level.effect_group.transform.childCount == 0;
                Check(ref pass, "level popup and material structure", openLevel && level != null && materialStructure,
                    levelShot);
                Check(ref pass, "level effect host remains inert", inertEffectHost);
                Check(ref pass, "level open sends no write", frames.All(frame => !ForbiddenWrites.Contains(FrameProtocol(frame))));

                Image modal = level != null ? CliVerify.FindDeep(level.transform, "__modal_mask")?.GetComponent<Image>() : null;
                bool closeByMask = Click(modal, camera, raycaster, eventSystem);
                await Task.Delay(50);
                Check(ref pass, "level modal close", closeByMask && (level == null || !level.gameObject.activeInHierarchy));

                bool reopenLevel = Click(main._img_grade, camera, raycaster, eventSystem);
                level = await WaitForActive<FashionLevelView>(3000);
                bool closeByButton = Click(level?.flv_closebut_image, camera, raycaster, eventSystem);
                await Task.Delay(50);
                Check(ref pass, "level close button", reopenLevel && closeByButton
                    && (level == null || !level.gameObject.activeInHierarchy));

                BaseWindowSkinView window = FindActive<BaseWindowSkinView>();
                Graphic returnGraphic = window != null
                    ? CliVerify.FindDeep(window.transform, "_img_return0")?.GetComponent<Graphic>() : null;
                bool returnClick = Click(returnGraphic, camera, raycaster, eventSystem);
                await Task.Delay(250);
                stage.Capture(Evidence("return.png"));
                Check(ref pass, "Fashion return chain hides page", returnClick && !main.gameObject.activeInHierarchy);

                Debug.Log("CLIVERIFY fashion-main-pos1 VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (outboundField != null) outboundField.SetValue(null, oldOutbound);
                FashionFlow.Reset();
                FashionModel.Instance.Clear();
                if (eventSystemObject != null) UnityEngine.Object.DestroyImmediate(eventSystemObject);
                stage.Dispose();
            }
        }

        private static void SeedFigure(int fashionId)
        {
            int clothe = FashionConfigs.GetModelRow(PosId, fashionId, 1, 1, 0)?.ModelId ?? 0;
            LoginConfigs.CareerRes defaults = LoginConfigs.GetCreateRes(1, 1);
            var figure = new FigureProto { name = "FashionMain验收", career = 1, sex = 1, level = 999, turn = 20 };
            figure.Raw["level_model_list"] = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["part_pos"] = 1, ["level_model_id"] = clothe },
                new Dictionary<string, object> { ["part_pos"] = 2, ["level_model_id"] = defaults?.WeaponRes ?? 0 },
                new Dictionary<string, object> { ["part_pos"] = 3, ["level_model_id"] = defaults?.HeadRes ?? 0 },
            };
            figure.Raw["fashion_model_list"] = new List<Dictionary<string, object>>();
            figure.Raw["figure_list"] = new List<Dictionary<string, object>>();
            RoleModel.Instance.Figure = figure;
        }

        private static void SeedModel(int firstFashionId)
        {
            FashionModel model = FashionModel.Instance;
            model.Clear();
            model.Apply41300(new List<FashionModel.PosWire>
            {
                new FashionModel.PosWire
                {
                    PosId = PosId,
                    WearFashionId = firstFashionId,
                    PosLv = 0,
                    PosUpgradeNum = 0,
                    Fashions = new List<FashionModel.FashionWire>
                    {
                        new FashionModel.FashionWire
                        {
                            FashionId = firstFashionId,
                            StarLv = 1,
                            NowColorId = 0,
                            Colors = new List<FashionModel.ColorWire>
                            {
                                new FashionModel.ColorWire { ColorId = 0, StarLv = 1 },
                            },
                        },
                    },
                },
            });
            model.Apply41312(PosId, firstFashionId, new List<FashionModel.PowerEntry>
            {
                new FashionModel.PowerEntry { ColorId = 0, Power = 500, NextPower = 1000 },
            });
        }

        private static bool HasUniqueClickSurface(FashionItem item)
        {
            Graphic surface = item?.ClickSurface;
            return surface != null && surface.enabled && surface.raycastTarget
                && item.GetComponentsInChildren<Graphic>(true).Count(graphic => graphic.raycastTarget) == 1;
        }

        private static bool VerifyAttributeNameValueLayout(FashionMainView main)
        {
            FashionAttrItem item = main?.GetComponentsInChildren<FashionAttrItem>(true)
                .FirstOrDefault(value => value != null && value.gameObject.activeInHierarchy);
            if (item?.name == null || item._lb_att0 == null) return false;
            Transform row = item.transform.Find("__name_value_row");
            HorizontalLayoutGroup layout = row != null ? row.GetComponent<HorizontalLayoutGroup>() : null;
            if (layout == null || row.childCount != 2 || row.GetChild(0) != item.name.transform
                || row.GetChild(1) != item._lb_att0.transform || !layout.childControlWidth
                || layout.childForceExpandWidth || Mathf.Abs(layout.spacing - 8f) > 0.01f) return false;

            string oldName = item.name.text;
            string oldValue = item._lb_att0.text;
            item.name.text = "命中加成";
            item._lb_att0.text = "+1200000";
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)row);
            Bounds nameBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(row, item.name.rectTransform);
            Bounds valueBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(row, item._lb_att0.rectTransform);
            float gap = valueBounds.min.x - nameBounds.max.x;
            item.name.text = oldName;
            item._lb_att0.text = oldValue;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)row);
            return gap >= 7.5f && gap <= 8.5f;
        }

        private static bool VerifyCostCellLayout(FashionMainView main, out string detail)
        {
            detail = "missing Fashion cost layout binding";
            if (main?._box1 == null || main._box_award == null || main._lb_order == null) return false;
            BaseAwardItem award = main._box_award.GetComponentInChildren<BaseAwardItem>(true);
            RectTransform awardRect = award != null ? award.transform as RectTransform : null;
            string order = main._lb_order.text;
            bool validOrderState = order == "[未激活]"
                || (!string.IsNullOrWhiteSpace(order) && order.StartsWith("[") && order.EndsWith("阶]"));
            if (awardRect == null || !award.gameObject.activeInHierarchy
                || !main._lb_order.gameObject.activeInHierarchy || !validOrderState) return false;

            Bounds awardBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(main._box1, awardRect);
            Bounds orderBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                main._box1, main._lb_order.rectTransform);
            detail = "award=" + awardBounds.size + " awardY=" + awardBounds.min.y + ".." + awardBounds.max.y
                + " order=" + order + " orderY=" + orderBounds.min.y + ".." + orderBounds.max.y
                + " gap=" + (awardBounds.min.y - orderBounds.max.y);
            return Mathf.Abs(awardBounds.size.x - 84f) <= 0.5f
                && Mathf.Abs(awardBounds.size.y - 84f) <= 0.5f
                && orderBounds.max.y <= awardBounds.min.y - 4f;
        }

        private static async Task<T> WaitForActive<T>(int timeoutMs) where T : Component
        {
            int elapsed = 0;
            while (elapsed <= timeoutMs)
            {
                T value = FindActive<T>();
                if (value != null) return value;
                await Task.Delay(100);
                elapsed += 100;
            }
            return null;
        }

        private static T FindActive<T>() where T : Component
        {
            foreach (T value in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                if (value != null && value.gameObject.activeInHierarchy) return value;
            return null;
        }

        private static bool Click(Component target, Camera camera, GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (target == null || camera == null || raycaster == null || eventSystem == null
                || !target.gameObject.activeInHierarchy) return false;
            Graphic surface = target as Graphic ?? target.GetComponent<Graphic>()
                ?? target.GetComponentInChildren<Graphic>(true);
            if (surface == null || !surface.enabled || !surface.raycastTarget) return false;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            Vector2 position = RectTransformUtility.WorldToScreenPoint(camera,
                surface.rectTransform.TransformPoint(surface.rectTransform.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = position,
                pressPosition = position,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject != surface.gameObject
                    && !hit.gameObject.transform.IsChildOf(target.transform)) continue;
                return ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(hit.gameObject, pointer,
                    ExecuteEvents.pointerClickHandler) != null;
            }
            Debug.LogError("CLIVERIFY fashion-main-pos1 raycast miss target=" + target.name
                + " hits=" + string.Join(",", hits.Select(hit => hit.gameObject.name)));
            return false;
        }

        private static bool DragHorizontal(ScrollRect scroll, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (scroll?.viewport == null || scroll.content == null || camera == null
                || raycaster == null || eventSystem == null) return false;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RectTransform viewport = scroll.viewport;
            Vector3 world = viewport.TransformPoint(new Vector3(
                Mathf.Min(viewport.rect.xMax - 60f, viewport.rect.center.x + 120f), viewport.rect.center.y));
            Vector2 start = RectTransformUtility.WorldToScreenPoint(camera, world);
            // CanvasScaler 会把屏幕坐标位移换算到参考分辨率；大步拖动会一次越过目标格，
            // 造成“目标先进入、随后又离开裁剪区”的测试假阴性。按真实短手势逐步拖动，
            // 每步后由调用方重新检查目标中心是否已进入 viewport。
            Vector2 end = start + new Vector2(-30f, 0f);
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
            ExecuteEvents.ExecuteHierarchy<IBeginDragHandler>(hit.gameObject, pointer,
                ExecuteEvents.beginDragHandler);
            pointer.delta = end - start;
            pointer.position = end;
            ExecuteEvents.ExecuteHierarchy<IDragHandler>(hit.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.ExecuteHierarchy<IEndDragHandler>(hit.gameObject, pointer,
                ExecuteEvents.endDragHandler);
            scroll.StopMovement();
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private static bool DispatchDisplacedPointerClick(Component target, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (target == null || camera == null || raycaster == null || eventSystem == null
                || !target.gameObject.activeInHierarchy) return false;
            Graphic surface = target as Graphic ?? target.GetComponent<Graphic>()
                ?? target.GetComponentInChildren<Graphic>(true);
            if (surface == null || !surface.enabled || !surface.raycastTarget) return false;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            Vector2 position = RectTransformUtility.WorldToScreenPoint(camera,
                surface.rectTransform.TransformPoint(surface.rectTransform.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                pressPosition = position + new Vector2(80f, 0f),
                position = position,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            RaycastResult hit = hits.FirstOrDefault(result => result.gameObject == surface.gameObject
                || result.gameObject.transform.IsChildOf(target.transform));
            return hit.gameObject != null
                && ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(hit.gameObject, pointer,
                    ExecuteEvents.pointerClickHandler) != null;
        }

        private static bool IsInsideViewport(Graphic target, ScrollRect scroll, Camera camera)
        {
            if (target == null || scroll?.viewport == null || camera == null
                || !target.gameObject.activeInHierarchy) return false;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera,
                target.rectTransform.TransformPoint(target.rectTransform.rect.center));
            return RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, point, camera);
        }

        private static bool BringIntoViewport(Graphic target, ScrollRect scroll, Camera camera)
        {
            if (target == null || scroll?.viewport == null || scroll.content == null || camera == null)
                return false;
            Canvas.ForceUpdateCanvases();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                scroll.viewport, target.rectTransform);
            Rect viewportRect = scroll.viewport.rect;
            float delta = 0f;
            if (bounds.max.x > viewportRect.xMax) delta = viewportRect.xMax - bounds.max.x - 4f;
            else if (bounds.min.x < viewportRect.xMin) delta = viewportRect.xMin - bounds.min.x + 4f;
            if (Mathf.Abs(delta) > 0.01f)
            {
                Vector2 position = scroll.content.anchoredPosition;
                position.x += delta;
                scroll.content.anchoredPosition = position;
                scroll.StopMovement();
                Canvas.ForceUpdateCanvases();
            }
            return IsInsideViewport(target, scroll, camera);
        }

        private static string Evidence(string fileName) => EvidenceRoot + "/" + fileName;

        private static string GetCommandLineValue(string key, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(args[i + 1])) return args[i + 1];
            return fallback;
        }

        private static async Task<int> WaitForModelPixels(FashionMainView main, RawImage image,
            int timeoutMs, string evidencePath)
        {
            int elapsed = 0;
            int pixels = 0;
            while (elapsed <= timeoutMs)
            {
                UIModelStage.RenderNow();
                pixels = CountRenderedPixels(image, evidencePath);
                if (main != null && main.IsModelPreviewReady && pixels >= 64) return pixels;
                await Task.Delay(100);
                elapsed += 100;
            }
            return pixels;
        }

        private static int CountRenderedPixels(RawImage image, string evidencePath)
        {
            if (image == null || !(image.texture is RenderTexture renderTexture)
                || !renderTexture.IsCreated()) return 0;
            RenderTexture previous = RenderTexture.active;
            Texture2D copy = null;
            try
            {
                RenderTexture.active = renderTexture;
                copy = new Texture2D(renderTexture.width, renderTexture.height,
                    TextureFormat.RGBA32, false, true);
                copy.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0, false);
                copy.Apply(false, false);
                Color32[] data = copy.GetPixels32();
                int count = data.Count(pixel => pixel.a >= 8);
                if (!string.IsNullOrEmpty(evidencePath))
                {
                    string fullPath = Path.GetFullPath(evidencePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? "Temp");
                    File.WriteAllBytes(fullPath, copy.EncodeToPNG());
                }
                return count;
            }
            finally
            {
                RenderTexture.active = previous;
                if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        private static int FrameProtocol(byte[] frame)
        {
            return frame != null && frame.Length >= 6 ? (frame[4] << 8) | frame[5] : -1;
        }

        private static bool FrameEquals(byte[] actual, int protocol, byte[] payload)
        {
            if (actual == null || payload == null || actual.Length != 6 + payload.Length
                || FrameProtocol(actual) != protocol) return false;
            for (int i = 0; i < payload.Length; i++)
                if (actual[i + 6] != payload[i]) return false;
            return true;
        }

        private static void Check(ref bool pass, string name, bool condition, string detail = "")
        {
            if (condition) Debug.Log("CLIVERIFY PASS fashion-main-pos1 " + name
                + (string.IsNullOrEmpty(detail) ? "" : " " + detail));
            else
            {
                pass = false;
                Debug.LogError("CLIVERIFY FAIL fashion-main-pos1 " + name
                    + (string.IsNullOrEmpty(detail) ? "" : " " + detail));
            }
        }
    }
}
