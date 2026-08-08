using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>人物→属性说明的真实点击、完整内容、滚动、关闭、遮罩和重开专项。</summary>
    public static class RoleInstructionRouteCase
    {
        private const string EvidenceRoot =
            "output/ui_route_audit/2026-08-06_role_instruction/cli";

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            EventSystem eventSystem = null;
            bool pass = false;
            string detail = string.Empty;
            try
            {
                ResetFlows();
                await InstructionConfigs.EnsureLoaded();
                PrepareRole();

                Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
                GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
                eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
                if (eventSystem == null)
                {
                    eventSystem = new GameObject(
                        "RoleInstructionRouteEventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
                }
                Camera camera = canvas != null ? canvas.worldCamera : null;
                if (camera == null || raycaster == null || eventSystem == null)
                    throw new InvalidOperationException("属性说明路由缺相机/GraphicRaycaster/EventSystem");

                RoleFlow.Open();
                EquipmentView equipment = await WaitForShown<EquipmentView>(12d);
                string entryHit = "not-run";
                double coldStart = EditorApplication.timeSinceStartup;
                bool entryClick = equipment != null
                    && Click(equipment.tipsImg, camera, raycaster, eventSystem, out entryHit);
                InstructionViewBind view = await WaitForShown<InstructionViewBind>(12d);
                long coldMs = MillisecondsSince(coldStart);
                stage.ForceCjkFont();
                Canvas.ForceUpdateCanvases();

                bool identity = view != null && view.transform.IsChildOf(ViewManager.GetLayer(UILayer.Popup));
                bool structure = view != null && view._panel_item != null
                    && view._panel_item.viewport != null
                    && view._panel_item.viewport.GetComponent<RectMask2D>() != null
                    && view._panel_item.content == view._vbox_con
                    && view._panel_item.GetComponent<Image>() != null
                    && view._panel_item.GetComponent<Image>().raycastTarget;

                InstructionItemBind[] sections = ActiveSections(view);
                InstructionSmallItemBind[] lines = ActiveLines(view);
                bool counts = sections.Length == 2 && lines.Length == 34;
                bool titles = view != null && view._lb_title != null
                    && view._lb_title.text == "属性说明"
                    && sections.Length == 2
                    && sections[0]._html_title.text == "极品属性"
                    && sections[1]._html_title.text == "基础属性";
                bool geometry = view != null && Near(view._vbox_con.rect.height, 1033f)
                    && sections.Length == 2
                    && Near(((RectTransform)sections[0].transform).rect.height, 649f)
                    && Near(((RectTransform)sections[1].transform).rect.height, 369f)
                    && Near(((RectTransform)sections[1].transform).anchoredPosition.y, -664f)
                    && lines.All(line => Near(((RectTransform)line.transform).rect.width, 552f));
                bool content = lines.Length == 34
                    && lines[0]._lb_desc.text.Contains("伤害加深")
                    && lines[0]._lb_desc.text.Contains("<color=#d15e00>")
                    && lines[21]._lb_desc.text.Contains("格挡忽视")
                    && lines[22]._lb_desc.text.Contains("攻击")
                    && lines[33]._lb_desc.text.Contains("绝对防御")
                    && lines.All(line => Near(line._lb_desc.fontSize, 18f));

                Image mask = FindActiveMask();
                bool maskFull = mask != null && mask.raycastTarget
                    && Near(mask.rectTransform.anchorMin, Vector2.zero)
                    && Near(mask.rectTransform.anchorMax, Vector2.one)
                    && Near(mask.rectTransform.offsetMin, Vector2.zero)
                    && Near(mask.rectTransform.offsetMax, Vector2.zero);
                bool centered = view != null
                    && Near(((RectTransform)view.transform).rect.size, new Vector2(616f, 498f))
                    && Near(((RectTransform)view.transform).anchoredPosition, Vector2.zero);
                string topShot = stage.Capture(EvidenceRoot + "/instruction_top.png");

                string dragHit = "not-run";
                bool drag = lines.Length > 0 && Drag(
                    view._panel_item,
                    lines[0]._lb_desc,
                    new Vector2(0f, 700f),
                    camera,
                    raycaster,
                    eventSystem,
                    out dragHit);
                bool moved = await WaitUntil(() => view != null && view._vbox_con.anchoredPosition.y >= 600f, 3d);
                view?._panel_item.StopMovement();
                Canvas.ForceUpdateCanvases();
                bool lastReachable = lines.Length == 34
                    && IsInsideViewport((RectTransform)lines[33].transform, view._panel_item.viewport);
                string bottomShot = stage.Capture(EvidenceRoot + "/instruction_bottom.png");

                string closeHit = "not-run";
                bool closeClick = view != null
                    && Click(view._img_close, camera, raycaster, eventSystem, out closeHit);
                bool closed = await WaitUntil(() => view != null && !view.gameObject.activeInHierarchy, 3d);
                bool roleStayed = equipment != null && equipment.IsShown && equipment.gameObject.activeInHierarchy;

                double warmStart = EditorApplication.timeSinceStartup;
                string reopenHit = "not-run";
                bool reopenClick = equipment != null
                    && Click(equipment.tipsImg, camera, raycaster, eventSystem, out reopenHit);
                InstructionViewBind reopened = await WaitForShown<InstructionViewBind>(6d);
                long warmMs = MillisecondsSince(warmStart);
                Canvas.ForceUpdateCanvases();
                bool reopenReady = reopened != null
                    && ActiveSections(reopened).Length == 2
                    && ActiveLines(reopened).Length == 34
                    && reopened._panel_item.verticalNormalizedPosition >= 0.999f
                    && CountActiveMasks() == 1
                    && CountInstructionModules() == 1;
                string reopenShot = stage.Capture(EvidenceRoot + "/instruction_reopen.png");

                Image reopenedMask = FindActiveMask();
                string maskHit = "not-run";
                bool maskClick = reopenedMask != null && ClickMaskCorner(
                    reopenedMask, camera, raycaster, eventSystem, out maskHit);
                bool maskClosed = await WaitUntil(
                    () => reopened != null && !reopened.gameObject.activeInHierarchy, 3d);
                bool maskNoThrough = equipment != null && equipment.IsShown
                    && equipment.gameObject.activeInHierarchy;
                string maskClosedShot = stage.Capture(EvidenceRoot + "/instruction_mask_closed.png");

                pass = entryClick && identity && structure && counts && titles && geometry && content
                    && maskFull && centered && drag && moved && lastReachable
                    && closeClick && closed && roleStayed
                    && reopenClick && reopenReady && maskClick && maskClosed && maskNoThrough;
                detail = "entry=" + entryClick + "/" + entryHit
                    + " identity=" + identity + " structure=" + structure
                    + " counts=" + sections.Length + "/" + lines.Length
                    + " titles=" + titles + " geometry=" + geometry + " content=" + content
                    + " maskFull=" + maskFull + " centered=" + centered
                    + " drag=" + drag + "/" + dragHit + " moved=" + moved
                    + " lastReachable=" + lastReachable
                    + " close=" + closeClick + "/" + closeHit + " closed=" + closed
                    + " roleStayed=" + roleStayed
                    + " reopen=" + reopenClick + "/" + reopenHit + " ready=" + reopenReady
                    + " maskClose=" + maskClick + "/" + maskHit + " closed=" + maskClosed
                    + " noThrough=" + maskNoThrough
                    + " coldMs=" + coldMs + " warmMs=" + warmMs
                    + " shots=" + topShot + "," + bottomShot + "," + reopenShot + "," + maskClosedShot;
            }
            catch (Exception e)
            {
                detail = "exception=" + e;
                pass = false;
            }
            finally
            {
                ResetFlows();
                RoleModel.Instance.Reset();
                if (eventSystem != null) UnityEngine.Object.DestroyImmediate(eventSystem.gameObject);
                stage.Dispose();
            }

            Debug.Log("CLIVERIFY roleinstructionroute " + detail);
            Debug.Log("CLIVERIFY roleinstructionroute VERDICT pass=" + pass + " restored=True");
            return pass ? 0 : 3;
        }

        private static void PrepareRole()
        {
            RoleModel role = RoleModel.Instance;
            role.Reset();
            role.RoleId = 4294967524L;
            role.Level = 630;
            role.Exp = 2070000000000L;
            role.ExpLim = 11750000000000L;
            role.CombatPower = 22868566L;
            role.Figure = new Shenxiao.Common.Proto.FigureProto
            {
                name = "111111",
                career = 1,
                sex = 1,
                level = 630,
                turn = 5,
            };
            role.BattleAttr = new Shenxiao.Common.Proto.BattleAttrProto
            {
                Hp = 1868655,
                HpLim = 1868655,
                Speed = 250,
            };
            role.MarkBaseInfoReady();
        }

        private static InstructionItemBind[] ActiveSections(InstructionViewBind view)
            => view == null
                ? Array.Empty<InstructionItemBind>()
                : view._vbox_con.GetComponentsInChildren<InstructionItemBind>(false)
                    .OrderBy(item => item.transform.GetSiblingIndex()).ToArray();

        private static InstructionSmallItemBind[] ActiveLines(InstructionViewBind view)
            => view == null
                ? Array.Empty<InstructionSmallItemBind>()
                : ActiveSections(view)
                    .SelectMany(section => section._vbox_con
                        .GetComponentsInChildren<InstructionSmallItemBind>(false)
                        .OrderBy(line => line.transform.GetSiblingIndex()))
                    .ToArray();

        private static Image FindActiveMask()
            => UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(image => image != null && image.gameObject.name == "__InstructionMask");

        private static int CountActiveMasks()
            => UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Count(image => image != null && image.gameObject.name == "__InstructionMask");

        private static int CountInstructionModules()
            => UnityEngine.Object.FindObjectsByType<InstructionViewBind>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(view => view != null && HasAncestorNamed(view.transform, "CommonModule(Instruction)"));

        private static bool HasAncestorNamed(Transform transform, string expectedName)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (current.name == expectedName) return true;
            }
            return false;
        }

        private static void ResetFlows()
        {
            typeof(InstructionFlow).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, null);
            typeof(RoleFlow).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, null);
        }

        private static async Task<T> WaitForShown<T>(double timeoutSeconds) where T : BaseView
        {
            T result = null;
            await WaitUntil(() =>
            {
                result = UnityEngine.Object.FindObjectsByType<T>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(v => v != null && v.IsShown && v.gameObject.activeInHierarchy);
                return result != null;
            }, timeoutSeconds);
            return result;
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

        private static bool Click(
            Component target, Camera camera, GraphicRaycaster raycaster,
            EventSystem eventSystem, out string hitName)
        {
            hitName = "missing";
            RectTransform rect = target != null ? target.transform as RectTransform : null;
            if (rect == null || !rect.gameObject.activeInHierarchy) return false;
            Canvas.ForceUpdateCanvases();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            return ClickAt(point, rect, raycaster, eventSystem, out hitName);
        }

        private static bool ClickMaskCorner(
            Image mask, Camera camera, GraphicRaycaster raycaster,
            EventSystem eventSystem, out string hitName)
        {
            RectTransform rect = mask != null ? mask.rectTransform : null;
            if (rect == null)
            {
                hitName = "missing";
                return false;
            }
            Vector3 world = rect.TransformPoint(new Vector3(rect.rect.xMin + 12f, rect.rect.yMin + 12f));
            return ClickAt(RectTransformUtility.WorldToScreenPoint(camera, world), rect, raycaster, eventSystem, out hitName);
        }

        private static bool ClickAt(
            Vector2 point, RectTransform scope, GraphicRaycaster raycaster,
            EventSystem eventSystem, out string hitName)
        {
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = point,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            foreach (RaycastResult hitResult in hits)
            {
                Transform hit = hitResult.gameObject.transform;
                if (hit != scope && !hit.IsChildOf(scope)) continue;
                hitName = hitResult.gameObject.name;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                    hitResult.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return true;
            }
            hitName = string.Join("/", hits.Select(hit => hit.gameObject.name));
            return false;
        }

        private static bool Drag(
            ScrollRect scroll, Component surface, Vector2 delta, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem, out string hitName)
        {
            hitName = "missing";
            RectTransform scrollRect = scroll != null ? scroll.transform as RectTransform : null;
            RectTransform surfaceRect = surface != null ? surface.transform as RectTransform : null;
            if (scrollRect == null || surfaceRect == null) return false;
            Canvas.ForceUpdateCanvases();
            Vector2 start = RectTransformUtility.WorldToScreenPoint(
                camera, surfaceRect.TransformPoint(surfaceRect.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = start,
                pressPosition = start,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            RaycastResult hit = hits.FirstOrDefault(result =>
                result.gameObject.transform == scrollRect
                || result.gameObject.transform.IsChildOf(scrollRect));
            if (hit.gameObject == null)
            {
                hitName = string.Join("/", hits.Select(result => result.gameObject.name));
                return false;
            }
            hitName = hit.gameObject.name;
            pointer.pointerPressRaycast = hit;
            ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.delta = delta;
            pointer.position = start + delta;
            ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.endDragHandler);
            return true;
        }

        private static bool IsInsideViewport(RectTransform item, RectTransform viewport)
        {
            if (item == null || viewport == null || !item.gameObject.activeInHierarchy) return false;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
            Rect rect = viewport.rect;
            return bounds.min.y >= rect.yMin - 1f && bounds.max.y <= rect.yMax + 1f;
        }

        private static long MillisecondsSince(double start)
            => Math.Max(0L, (long)Math.Round((EditorApplication.timeSinceStartup - start) * 1000d));

        private static bool Near(float value, float expected)
            => Mathf.Abs(value - expected) <= 1f;

        private static bool Near(Vector2 value, Vector2 expected)
            => Vector2.Distance(value, expected) <= 1f;
    }
}
