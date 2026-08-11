using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Medal;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Medal;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 人物→境界→天境称号特效裁切专项。必须从人物页真实入口进入共享窗框，
    /// 并在同一个 ScrollRect 内验证完整、部分和完全离屏三态。
    /// </summary>
    public static class RoleRealmTitleClipCase
    {
        private const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags InstancePrivate = BindingFlags.NonPublic | BindingFlags.Instance;
        private const string EvidenceRoot =
            "output/ui_route_audit/2026-08-09_role_person_realm_title_clip_runtime_v6/unity";

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = null;
            EventSystem eventSystem = null;
            bool createdEventSystem = false;
            FieldInfo interceptField = typeof(MedalController).GetField(
                "s_outboundIntercept", StaticPrivate);
            object oldIntercept = interceptField?.GetValue(null);
            try
            {
                stage = CliVerify.Stage.Create();
                eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
                if (eventSystem == null)
                {
                    eventSystem = new GameObject("RoleRealmTitleClipCase_EventSystem",
                        typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
                    createdEventSystem = true;
                }

                interceptField?.SetValue(null, new Func<byte[], bool>(_ => true));
                ResetFlows();
                PrepareRoleAndRealmState();
                MainUIRouter.Register("MedalEnterView", MedalFlow.Toggle);

                Task prerequisites = Task.WhenAll(
                    MedalConfigs.EnsureLoaded(),
                    TitleConfigs.EnsureLoaded(),
                    GoodsModel.EnsureLoaded(),
                    FuncOpenConfig.EnsureLoaded());

                RoleFlow.Open();
                EquipmentView equipment = await WaitFor(
                    () => stage.CanvasRoot.GetComponentsInChildren<EquipmentView>(false)
                        .FirstOrDefault(view => view.IsShown),
                    45d, "人物页未打开");
                await prerequisites;
                await WaitFrames(8);

                bool realmClicked = Click(stage, eventSystem, equipment._Group3);
                await WaitFor(
                    () => BaseWindowManager.Current != null
                        && BaseWindowManager.Current.GetComponentsInChildren<MedalViewBind>(false)
                            .Any(view => view.IsShown),
                    120d, "点击境界后地境未打开");
                BaseWindowSkinView realmWindow = BaseWindowManager.Current;
                await WaitFrames(12);
                Canvas.ForceUpdateCanvases();
                List<TabButtonTwoSkin> tabs = realmWindow
                    .GetComponentsInChildren<TabButtonTwoSkin>(true)
                    .Where(tab => tab.gameObject.activeInHierarchy)
                    .OrderBy(tab => ((RectTransform)tab.transform).anchoredPosition.x)
                    .ToList();
                bool skyClicked = tabs.Count == 2 && Click(stage, eventSystem, tabs[1]);
                TitleMainView sky = await WaitFor(
                    () => stage.CanvasRoot.GetComponentsInChildren<TitleMainView>(false)
                        .FirstOrDefault(view => view.IsShown),
                    45d, "点击天境后页面未打开");
                await WaitFor(() => sky.Content != null && sky.Content.content != null
                    && sky.Content.content.GetComponentsInChildren<TitleItem>(false).Length == 10,
                    30d, "天境称号列表未生成十项");

                TitleItem initialItem = sky.Content.content.GetComponentsInChildren<TitleItem>(false)
                    .FirstOrDefault(item => item.name == "TitleItem_1001");
                await WaitFor(() => initialItem != null && GetHandle(initialItem, "_effect") != null
                    && GetHandle(sky, "_mainEffect") != null,
                    60d, "initial title effect 01 not loaded");
                await WaitFrames(8);
                TickEffectStage();

                object initialItemHandle = GetHandle(initialItem, "_effect");
                object initialMainHandle = GetHandle(sky, "_mainEffect");
                string initialItemLabel = ReadHandleLabel(initialItemHandle);
                string initialMainLabel = ReadHandleLabel(initialMainHandle);
                bool initialEffectIdentity = initialItemLabel == "effect_shenmingjiemian_01"
                    && initialMainLabel == "effect_shenmingjiemian_01";

                ResetEffectPhase(initialItemHandle);
                CompositeProbe initialItem350 = CaptureCompositeProbe(stage, initialItemHandle, 0.35f,
                    EvidenceRoot + "/01_initial_item_0350_raw_rt.png",
                    EvidenceRoot + "/01_initial_item_0350_canvas.png",
                    EvidenceRoot + "/01_initial_item_0350_background.png");
                CompositeProbe initialItem700 = CaptureCompositeProbe(stage, initialItemHandle, 0.35f,
                    EvidenceRoot + "/02_initial_item_0700_raw_rt.png",
                    EvidenceRoot + "/02_initial_item_0700_canvas.png",
                    EvidenceRoot + "/02_initial_item_0700_background.png");
                int initialItemChanged = CountChanged(initialItem350.RawPixels, initialItem700.RawPixels);

                ResetEffectPhase(initialMainHandle);
                CompositeProbe initialMain350 = CaptureCompositeProbe(stage, initialMainHandle, 0.35f,
                    EvidenceRoot + "/03_initial_main_0350_raw_rt.png",
                    EvidenceRoot + "/03_initial_main_0350_canvas.png",
                    EvidenceRoot + "/03_initial_main_0350_background.png");
                CompositeProbe initialMain700 = CaptureCompositeProbe(stage, initialMainHandle, 0.35f,
                    EvidenceRoot + "/04_initial_main_0700_raw_rt.png",
                    EvidenceRoot + "/04_initial_main_0700_canvas.png",
                    EvidenceRoot + "/04_initial_main_0700_background.png");
                int initialMainChanged = CountChanged(initialMain350.RawPixels, initialMain700.RawPixels);
                bool initialEffectPass = initialEffectIdentity
                    && initialItem350.Valid && initialItem700.Valid
                    && initialMain350.Valid && initialMain700.Valid;

                TitleItem target = sky.Content.content.GetComponentsInChildren<TitleItem>(false)
                    .FirstOrDefault(item => item.name == "TitleItem_1005");
                await WaitFor(() => target != null && GetHandle(target, "_effect") != null
                    && GetHandle(sky, "_mainEffect") != null,
                    60d, "称号列表或主展示特效未加载");
                await WaitFrames(8);
                TickEffectStage();

                bool profiles = VerifyProfiles();
                RectTransform host = target.title_gp;
                RectTransform viewport = sky.Content.viewport;

                MoveHostTo(host, sky.Content.content, viewport, viewport.rect.xMin + 12f);
                await Settle();
                object targetHandle = GetHandle(target, "_effect");
                object mainHandle = GetHandle(sky, "_mainEffect");
                HandleState full = ReadState(targetHandle);
                Rect fullRect = RectInLocal(host, viewport);
                stage.ForceCjkFont();
                string fullPage = stage.Capture(EvidenceRoot + "/10_full.png");
                Frame fullA = RenderIsolated(targetHandle, 0.30f,
                    EvidenceRoot + "/10_full_effect_a.png");
                Frame fullB = RenderIsolated(targetHandle, 0.22f,
                    EvidenceRoot + "/10_full_effect_b.png");
                int changed = CountChanged(fullA.Pixels, fullB.Pixels);
                CompositeProbe fullComposite = CaptureCompositeProbe(stage, targetHandle, 0.18f,
                    EvidenceRoot + "/11_item_raw_rt.png",
                    EvidenceRoot + "/11_item_canvas.png",
                    EvidenceRoot + "/11_item_background.png");
                bool fullPass = full.Visible && full.MaskCount > 0
                    && full.Fraction > 0.98f && fullA.Alpha > 100 && changed > 100;

                float hostWidth = Mathf.Max(1f, host.rect.width);
                MoveHostTo(host, sky.Content.content, viewport,
                    viewport.rect.xMin - hostWidth * 0.5f);
                await Settle();
                HandleState partial = ReadState(targetHandle);
                Rect partialRect = RectInLocal(host, viewport);
                stage.ForceCjkFont();
                string partialPage = stage.Capture(EvidenceRoot + "/20_partial.png");
                Frame partialFrame = RenderIsolated(targetHandle, 0.20f,
                    EvidenceRoot + "/20_partial_effect.png");
                bool partialPass = partial.Visible && partial.MaskCount > 0
                    && partial.Fraction > 0.05f && partial.Fraction < 0.95f
                    && partialFrame.Alpha > 0;

                MoveHostTo(host, sky.Content.content, viewport,
                    viewport.rect.xMin - hostWidth - 24f);
                await Settle();
                HandleState hidden = ReadState(targetHandle);
                Rect hiddenRect = RectInLocal(host, viewport);
                HandleState mainAfterScroll = ReadState(mainHandle);
                stage.ForceCjkFont();
                string hiddenPage = stage.Capture(EvidenceRoot + "/30_hidden.png");
                Frame hiddenFrame = RenderIsolated(targetHandle, 0f,
                    EvidenceRoot + "/30_hidden_effect.png");
                Frame mainFrame = RenderIsolated(mainHandle, 0.20f,
                    EvidenceRoot + "/31_main_persistent.png");
                CompositeProbe mainComposite = CaptureCompositeProbe(stage, mainHandle, 0.18f,
                    EvidenceRoot + "/32_main_raw_rt.png",
                    EvidenceRoot + "/32_main_canvas.png",
                    EvidenceRoot + "/32_main_background.png");
                bool hiddenPass = !hidden.Visible && hidden.MaskCount > 0
                    && hidden.Fraction <= 0.0001f && hiddenFrame.Alpha == 0
                    && hiddenRect.xMax < viewport.rect.xMin - 0.1f;
                bool mainPass = mainAfterScroll.Visible && mainAfterScroll.Fraction > 0.98f
                    && mainFrame.Alpha > 100;

                bool pass = realmClicked && skyClicked && profiles && initialEffectPass && fullPass
                    && partialPass && hiddenPass && mainPass;
                Debug.Log("CLIVERIFY role-realm-title-clip profiles=" + profiles
                    + " route=" + realmClicked + "/" + skyClicked
                    + " initialIdentity=" + initialEffectIdentity
                    + " initialLabels=" + initialItemLabel + "/" + initialMainLabel
                    + " initialItem=" + initialItem350 + " -> " + initialItem700
                    + " changed=" + initialItemChanged
                    + " initialMain=" + initialMain350 + " -> " + initialMain700
                    + " changed=" + initialMainChanged
                    + " full=" + full + " rect=" + fullRect
                    + " alpha=" + fullA.Alpha + "/" + fullB.Alpha
                    + " changed=" + changed
                    + " partial=" + partial + " rect=" + partialRect
                    + " alpha=" + partialFrame.Alpha
                    + " hidden=" + hidden + " rect=" + hiddenRect
                    + " viewport=" + viewport.rect + " alpha=" + hiddenFrame.Alpha
                    + " main=" + mainAfterScroll + " alpha=" + mainFrame.Alpha
                    + " itemComposite=" + fullComposite
                    + " mainComposite=" + mainComposite
                    + " shots=" + fullPage + "|" + partialPage + "|" + hiddenPage
                    + " pass=" + pass);
                return pass ? 0 : 3;
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY role-realm-title-clip EXCEPTION " + exception);
                return 1;
            }
            finally
            {
                interceptField?.SetValue(null, oldIntercept);
                ResetFlows();
                MedalModel.Instance.Reset();
                if (createdEventSystem && eventSystem != null)
                    UnityEngine.Object.DestroyImmediate(eventSystem.gameObject);
                stage?.Dispose();
            }
        }

        private static bool VerifyProfiles()
        {
            UIEffectProfileCatalog catalog = UIEffectProfileCatalog.Runtime;
            bool titles = true;
            for (int i = 1; i <= 10; i++)
            {
                string effect = "effect_shenmingjiemian_" + i.ToString("00");
                UIEffectProfile profile = catalog.Resolve(effect);
                titles &= profile != null && profile.clipToRenderRect;
            }
            UIEffectProfile fallback = catalog.Resolve("__unknown_effect__");
            UIEffectProfile task = catalog.Resolve("ui_renwulan");
            return titles && fallback != null && !fallback.clipToRenderRect
                && task != null && task.clipToRenderRect;
        }

        private static async Task Settle()
        {
            Canvas.ForceUpdateCanvases();
            TickEffectStage();
            await WaitFrames(5);
            Canvas.ForceUpdateCanvases();
            TickEffectStage();
        }

        private static void MoveHostTo(RectTransform host, RectTransform content,
            RectTransform viewport, float desiredLeft)
        {
            Rect rect = RectInLocal(host, viewport);
            content.anchoredPosition += new Vector2(desiredLeft - rect.xMin, 0f);
            Canvas.ForceUpdateCanvases();
        }

        private static Rect RectInLocal(RectTransform target, RectTransform root)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 p0 = root.InverseTransformPoint(corners[0]);
            float minX = p0.x;
            float maxX = p0.x;
            float minY = p0.y;
            float maxY = p0.y;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 point = root.InverseTransformPoint(corners[i]);
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static object GetHandle(object owner, string fieldName)
            => owner?.GetType().GetField(fieldName, InstancePrivate)?.GetValue(owner);

        private static string ReadHandleLabel(object handle)
            => handle?.GetType().GetField("Label", InstancePrivate)?.GetValue(handle) as string
                ?? string.Empty;

        private static void ResetEffectPhase(object handle)
        {
            if (handle == null) return;
            GameObject effect = handle.GetType().GetField("Effect", InstancePrivate)?.GetValue(handle)
                as GameObject;
            if (effect == null) return;
            effect.SetActive(false);
            effect.SetActive(true);
            foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                system.Clear(true);
                system.Simulate(0f, true, true, true);
                system.Play(true);
            }
            foreach (UnityEngine.Animation animation in
                effect.GetComponentsInChildren<UnityEngine.Animation>(true))
            {
                animation.Rewind();
                animation.Play();
            }
        }

        private static HandleState ReadState(object handle)
        {
            if (handle == null) return default;
            Type type = handle.GetType();
            return new HandleState
            {
                Visible = (bool)type.GetField("Visible", InstancePrivate).GetValue(handle),
                Fraction = (float)type.GetField("VisibleClipFraction", InstancePrivate).GetValue(handle),
                MaskCount = (int)type.GetField("ActiveAncestorMaskCount", InstancePrivate).GetValue(handle),
            };
        }

        private static Frame RenderIsolated(object targetHandle, float simulateSeconds,
            string evidencePath)
        {
            if (targetHandle == null) return default;
            Type handleType = targetHandle.GetType();
            FieldInfo wrapperField = handleType.GetField("Wrapper", InstancePrivate);
            FieldInfo effectField = handleType.GetField("Effect", InstancePrivate);
            FieldInfo channelField = handleType.GetField("SharedChannel", InstancePrivate);
            object channel = channelField.GetValue(targetHandle);
            if (channel == null) return default;
            Type channelType = channel.GetType();
            Camera camera = channelType.GetField("Camera").GetValue(channel) as Camera;
            RenderTexture texture = channelType.GetField("Texture").GetValue(channel) as RenderTexture;
            if (camera == null || texture == null) return default;

            var saved = new List<(Transform Wrapper, Vector3 Position)>();
            FieldInfo liveField = typeof(UIEffectStage).GetField("s_live", StaticPrivate);
            if (liveField?.GetValue(null) is IEnumerable handles)
            {
                foreach (object handle in handles)
                {
                    Transform wrapper = wrapperField.GetValue(handle) as Transform;
                    if (wrapper == null) continue;
                    saved.Add((wrapper, wrapper.localPosition));
                    if (!ReferenceEquals(handle, targetHandle))
                    {
                        Vector3 hiddenPosition = wrapper.localPosition;
                        hiddenPosition.z = 2048f;
                        wrapper.localPosition = hiddenPosition;
                    }
                }
            }

            try
            {
                GameObject effect = effectField.GetValue(targetHandle) as GameObject;
                if (simulateSeconds > 0f && effect != null)
                {
                    foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
                        system.Simulate(simulateSeconds, false, false, true);
                }

                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = texture;
                var copy = new Texture2D(texture.width, texture.height,
                    TextureFormat.RGBA32, false, true);
                copy.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                copy.Apply();
                RenderTexture.active = previous;
                Color32[] pixels = copy.GetPixels32();
                int alpha = 0;
                int lit = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    if (pixel.a > 2) alpha++;
                    if (pixel.r > 2 || pixel.g > 2 || pixel.b > 2) lit++;
                }
                string full = Path.GetFullPath(evidencePath);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, copy.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(copy);
                return new Frame
                {
                    Pixels = pixels,
                    Width = texture.width,
                    Height = texture.height,
                    Alpha = alpha,
                    Lit = lit
                };
            }
            finally
            {
                for (int i = 0; i < saved.Count; i++)
                    if (saved[i].Wrapper != null)
                        saved[i].Wrapper.localPosition = saved[i].Position;
            }
        }

        private static int CountChanged(Color32[] a, Color32[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return 0;
            int changed = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (Math.Abs(a[i].r - b[i].r) > 2 || Math.Abs(a[i].g - b[i].g) > 2
                    || Math.Abs(a[i].b - b[i].b) > 2 || Math.Abs(a[i].a - b[i].a) > 2)
                    changed++;
            }
            return changed;
        }

        private static CompositeProbe CaptureCompositeProbe(CliVerify.Stage stage, object targetHandle,
            float simulateSeconds, string rawPath, string canvasPath, string backgroundPath)
        {
            if (stage == null || targetHandle == null) return default;
            Type handleType = targetHandle.GetType();
            FieldInfo wrapperField = handleType.GetField("Wrapper", InstancePrivate);
            FieldInfo effectField = handleType.GetField("Effect", InstancePrivate);
            FieldInfo channelField = handleType.GetField("SharedChannel", InstancePrivate);
            object channel = channelField?.GetValue(targetHandle);
            if (channel == null) return default;
            Type channelType = channel.GetType();
            Camera effectCamera = channelType.GetField("Camera")?.GetValue(channel) as Camera;
            RenderTexture effectTexture = channelType.GetField("Texture")?.GetValue(channel) as RenderTexture;
            RawImage channelImage = channelType.GetField("Image")?.GetValue(channel) as RawImage;
            if (effectCamera == null || effectTexture == null || channelImage == null) return default;

            var saved = new List<(Transform Wrapper, Vector3 Position)>();
            FieldInfo liveField = typeof(UIEffectStage).GetField("s_live", StaticPrivate);
            if (liveField?.GetValue(null) is IEnumerable handles)
            {
                foreach (object handle in handles)
                {
                    Transform wrapper = wrapperField?.GetValue(handle) as Transform;
                    if (wrapper == null) continue;
                    saved.Add((wrapper, wrapper.localPosition));
                    if (!ReferenceEquals(handle, targetHandle))
                    {
                        Vector3 hiddenPosition = wrapper.localPosition;
                        hiddenPosition.z = 2048f;
                        wrapper.localPosition = hiddenPosition;
                    }
                }
            }

            bool imageActive = channelImage.gameObject.activeSelf;
            try
            {
                GameObject effect = effectField?.GetValue(targetHandle) as GameObject;
                if (simulateSeconds > 0f && effect != null)
                {
                    foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
                        system.Simulate(simulateSeconds, false, false, true);
                }

                effectCamera.Render();
                Frame raw = ReadTexture(effectTexture, rawPath, true);
                Frame canvas = CaptureStage(stage, canvasPath);
                channelImage.gameObject.SetActive(false);
                Frame background = CaptureStage(stage, backgroundPath);
                channelImage.gameObject.SetActive(imageActive);

                int sourceIndex = FindColorSample(raw.Pixels);
                if (sourceIndex < 0 || raw.Width <= 0 || raw.Height <= 0
                    || canvas.Width != raw.Width || canvas.Height != raw.Height
                    || background.Width != raw.Width || background.Height != raw.Height)
                    return default;

                int sourceX = sourceIndex % raw.Width;
                int sourceY = sourceIndex / raw.Width;
                int canvasX = raw.Width - 1 - sourceX;
                int canvasIndex = sourceY * raw.Width + canvasX;
                return new CompositeProbe
                {
                    Valid = canvasIndex >= 0 && canvasIndex < canvas.Pixels.Length,
                    RawPixels = raw.Pixels,
                    RawX = sourceX,
                    RawY = sourceY,
                    CanvasX = canvasX,
                    CanvasY = sourceY,
                    Raw = raw.Pixels[sourceIndex],
                    Canvas = canvas.Pixels[canvasIndex],
                    Background = background.Pixels[canvasIndex]
                };
            }
            finally
            {
                if (channelImage != null) channelImage.gameObject.SetActive(imageActive);
                for (int i = 0; i < saved.Count; i++)
                    if (saved[i].Wrapper != null)
                        saved[i].Wrapper.localPosition = saved[i].Position;
            }
        }

        private static Frame CaptureStage(CliVerify.Stage stage, string evidencePath)
        {
            Type type = stage.GetType();
            Camera camera = type.GetField("_cam", InstancePrivate)?.GetValue(stage) as Camera;
            RenderTexture texture = type.GetField("_rt", InstancePrivate)?.GetValue(stage) as RenderTexture;
            if (camera == null || texture == null) return default;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            return ReadTexture(texture, evidencePath, false);
        }

        private static Frame ReadTexture(RenderTexture texture, string evidencePath, bool linear)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, linear);
            copy.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            Color32[] pixels = copy.GetPixels32();
            int alpha = 0;
            int lit = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                if (pixel.a > 2) alpha++;
                if (pixel.r > 2 || pixel.g > 2 || pixel.b > 2) lit++;
            }
            string full = Path.GetFullPath(evidencePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, copy.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(copy);
            return new Frame { Pixels = pixels, Width = texture.width, Height = texture.height,
                Alpha = alpha, Lit = lit };
        }

        private static int FindColorSample(Color32[] pixels)
        {
            if (pixels == null) return -1;
            int best = -1;
            float bestScore = 0f;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                int max = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
                int min = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
                if (pixel.a <= 8 || max <= 16) continue;
                float score = (max - min) * (max / 255f) * (pixel.a / 255f);
                if (score <= bestScore) continue;
                bestScore = score;
                best = i;
            }
            return best;
        }

        private static bool Click(CliVerify.Stage stage, EventSystem eventSystem, Component target)
        {
            if (stage == null || eventSystem == null || target == null) return false;
            Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
            GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
            RectTransform rect = target.transform as RectTransform;
            if (canvas == null || raycaster == null || rect == null) return false;
            Canvas.ForceUpdateCanvases();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(
                canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                position = point,
                button = PointerEventData.InputButton.Left,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            RaycastResult? targetHit = null;
            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject != target.gameObject
                    && !hit.gameObject.transform.IsChildOf(target.transform)) continue;
                targetHit = hit;
                break;
            }
            Debug.Log("CLIVERIFY role-realm-title-clip CLICK target=" + target.name
                + " targetHit=" + targetHit.HasValue
                + " hits=" + string.Join(",", hits.Select(result => result.gameObject.name)));
            return targetHit.HasValue && ExecuteEvents.ExecuteHierarchy(
                targetHit.Value.gameObject, pointer, ExecuteEvents.pointerClickHandler) != null;
        }

        private static void PrepareRoleAndRealmState()
        {
            RoleModel.Instance.Level = 630;
            RoleModel.Instance.CombatPower = 2441807L;
            RoleModel.Instance.MarkBaseInfoReady();
            MedalModel.Instance.ReplaceData(2, 0, 0, 0, 36859, 9999);
            var titles = new List<MedalModel.TitleEntry>();
            for (uint id = 1001; id <= 1010; id++)
                titles.Add(new MedalModel.TitleEntry(id, 0, id == 1001 ? 36859u : 0u, 0));
            MedalModel.Instance.ReplaceTitles(titles);
        }

        private static void ResetFlows()
        {
            typeof(MedalFlow).GetMethod("Reset", StaticPrivate)?.Invoke(null, null);
            typeof(RoleFlow).GetMethod("Reset", StaticPrivate)?.Invoke(null, null);
        }

        private static void TickEffectStage()
            => typeof(UIEffectStage).GetMethod("Tick", StaticPrivate)?.Invoke(null, null);

        private static async Task<T> WaitFor<T>(Func<T> probe, double timeoutSeconds,
            string error) where T : class
        {
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + timeoutSeconds;
            while (UnityEditor.EditorApplication.timeSinceStartup < deadline)
            {
                T value = probe();
                if (value != null) return value;
                await Task.Yield();
            }
            throw new TimeoutException(error);
        }

        private static async Task WaitFor(Func<bool> probe, double timeoutSeconds, string error)
        {
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + timeoutSeconds;
            while (UnityEditor.EditorApplication.timeSinceStartup < deadline)
            {
                if (probe()) return;
                await Task.Yield();
            }
            throw new TimeoutException(error);
        }

        private static async Task WaitFrames(int count)
        {
            for (int i = 0; i < count; i++) await Task.Yield();
        }

        private struct HandleState
        {
            public bool Visible;
            public float Fraction;
            public int MaskCount;
            public override string ToString()
                => Visible + "/" + Fraction.ToString("0.000") + "/m" + MaskCount;
        }

        private struct Frame
        {
            public Color32[] Pixels;
            public int Width;
            public int Height;
            public int Alpha;
            public int Lit;
        }

        private struct CompositeProbe
        {
            public bool Valid;
            public Color32[] RawPixels;
            public int RawX;
            public int RawY;
            public int CanvasX;
            public int CanvasY;
            public Color32 Raw;
            public Color32 Canvas;
            public Color32 Background;

            public override string ToString()
            {
                return $"valid={Valid} raw=({RawX},{RawY})/{Raw} "
                    + $"canvas=({CanvasX},{CanvasY})/{Canvas} background={Background}";
            }
        }
    }
}
