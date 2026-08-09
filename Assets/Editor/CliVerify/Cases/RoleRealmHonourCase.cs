using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.UI;
using Shenxiao.Common.UI3D;
using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Generated.UI.Medal;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Marriage;
using Shenxiao.Module.Core.Medal;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 人物页“境界/名誉”组合运行验收。必须从真实 EquipmentView 按钮进入，
    /// 由生产 Flow 同时装配窗口与内容；禁止清空 ContentFactory 后拆开自证。
    /// </summary>
    public static class RoleRealmHonourCase
    {
        private const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;
        private const string EvidenceRoot =
            "output/ui_route_audit/2026-08-09_role_person_realm_honour_runtime_v15/unity";

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = null;
            EventSystem eventSystem = null;
            FieldInfo interceptField = typeof(MedalController).GetField(
                "s_outboundIntercept", StaticPrivate);
            object oldIntercept = interceptField?.GetValue(null);
            var outboundFrames = new List<byte[]>();
            try
            {
                stage = CliVerify.Stage.Create();
                eventSystem = EnsureEventSystem();
                interceptField?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    outboundFrames.Add(frame);
                    return true;
                }));

                ResetFlows();
                PrepareRoleAndRealmState();
                MainUIRouter.Register("MedalEnterView", MedalFlow.Toggle);

                Task prerequisites = Task.WhenAll(
                    MedalConfigs.EnsureLoaded(),
                    TitleConfigs.EnsureLoaded(),
                    GoodsModel.EnsureLoaded(),
                    FuncOpenConfig.EnsureLoaded(),
                    MarriageConfigs.EnsureLoaded());

                RoleFlow.Open();
                EquipmentView equipment = await WaitFor(
                    () => stage.CanvasRoot.GetComponentsInChildren<EquipmentView>(false)
                        .FirstOrDefault(view => view.IsShown),
                    45d, "人物页未打开");
                await WaitFrames(8);
                stage.ForceCjkFont();
                string roleShot = stage.Capture(EvidenceRoot + "/00_role_person.png");
                await prerequisites;

                bool realmButtonClicked = Click(stage, eventSystem, equipment._Group3);
                Debug.Log("CLIVERIFY role-realm-honour REALM_CLICK clicked=" + realmButtonClicked
                    + " equipmentShown=" + equipment.IsShown
                    + " targetActive=" + (equipment._Group3 != null
                        && equipment._Group3.gameObject.activeInHierarchy)
                    + " registered=" + MainUIRouter.IsRegistered("MedalEnterView"));
                MedalViewBind ground = await WaitFor(
                    () => stage.CanvasRoot.GetComponentsInChildren<MedalViewBind>(false)
                        .FirstOrDefault(view => view.IsShown),
                    120d, "点击境界后地境未打开");
                BaseWindowSkinView realmWindow = BaseWindowManager.Current;
                await WaitFrames(12);
                stage.ForceCjkFont();
                Canvas.ForceUpdateCanvases();
                string groundShot = stage.Capture(EvidenceRoot + "/10_realm_ground.png");
                bool groundGeometry = VerifyGroundGeometry(ground, stage.CanvasRoot as RectTransform);

                List<TabButtonTwoSkin> tabs = realmWindow != null
                    ? realmWindow.GetComponentsInChildren<TabButtonTwoSkin>(true)
                        .Where(tab => tab.gameObject.activeInHierarchy)
                        .OrderBy(tab => ((RectTransform)tab.transform).anchoredPosition.x)
                        .ToList()
                    : new List<TabButtonTwoSkin>();
                bool skyTabClicked = tabs.Count == 2 && Click(stage, eventSystem, tabs[1]);
                TitleMainView sky = await WaitFor(
                    () => stage.CanvasRoot.GetComponentsInChildren<TitleMainView>(false)
                        .FirstOrDefault(view => view.IsShown),
                    45d, "点击天境后 TitleMainView 未打开");
                await WaitForSkyEffects(60d);
                SimulateSkyParticles(0.35f);
                TickEffectStage();
                await WaitFrames(8);
                stage.ForceCjkFont();
                Canvas.ForceUpdateCanvases();
                string skyShot = stage.Capture(EvidenceRoot + "/11_realm_sky.png");
                bool skyGeometry = VerifySkyGeometry(sky, stage.CanvasRoot as RectTransform);
                bool skyRuntime = VerifySkyRuntime(sky);
                bool skyEffects = await VerifySkyEffects();
                stage.ForceCjkFont();
                Canvas.ForceUpdateCanvases();
                string skyDynamicShot = stage.Capture(EvidenceRoot + "/11_realm_sky_dynamic_b.png");

                Vector2 titleBeforeDrag = sky.Content.content.anchoredPosition;
                bool titleDragged = Drag(stage, eventSystem, sky.Content, new Vector2(-450f, 0f));
                await WaitFrames(6);
                Vector2 titleAfterDrag = sky.Content.content.anchoredPosition;
                TitleItem sixthTitle = sky.Content.content.GetComponentsInChildren<TitleItem>(false)
                    .FirstOrDefault(item => item.name == "TitleItem_1006");
                bool titleScrolled = titleDragged
                    && titleAfterDrag.x < titleBeforeDrag.x - 30f
                    && sixthTitle != null
                    && RelativeRect(sixthTitle.transform as RectTransform, sky.Content.viewport)
                        .Overlaps(new Rect(0f, 0f, sky.Content.viewport.rect.width,
                            sky.Content.viewport.rect.height), true);
                bool titleSelected = sixthTitle != null
                    && Click(stage, eventSystem, sixthTitle._Image1);
                await WaitFor(() => sky.name_lb != null && sky.name_lb.text == "天境·文月",
                    15d, "天境横向滚动后点击称号未刷新主展示");
                await WaitFor(() =>
                {
                    TickEffectStage();
                    return UIEffectStage.CollectDiagnostics().Any(item =>
                        item.Label == "effect_shenmingjiemian_06"
                        && item.ParentName == "title_effect_gp" && item.EffectAlive);
                }, 30d, "天境切换称号后主特效未刷新");
                SimulateSkyParticles(0.35f);
                TickEffectStage();
                await WaitFrames(5);
                string skyScrolledShot = stage.Capture(EvidenceRoot + "/11_realm_sky_scrolled.png");

                int beforeGuardClicks = outboundFrames.Count;
                bool inactiveActivateClicked = Click(stage, eventSystem, sky.up_level_gp);
                bool inactiveIllusionClicked = Click(stage, eventSystem, sky.illsion_gp);
                await WaitFrames(5);
                bool guardedWrites = inactiveActivateClicked && inactiveIllusionClicked
                    && outboundFrames.Count == beforeGuardClicks;

                bool groundTabClicked = Click(stage, eventSystem, tabs[0]);
                await WaitFor(() => realmWindow.CurrentIndex == 0 && ground.IsShown,
                    15d, "天境返回地境失败");
                await WaitFrames(6);
                string groundReopenShot = stage.Capture(EvidenceRoot + "/12_realm_ground_reopen.png");

                MedalFlow.Close();
                RoleFlow.Open();
                equipment = await WaitFor(
                    () => stage.CanvasRoot.GetComponentsInChildren<EquipmentView>(false)
                        .FirstOrDefault(view => view.IsShown),
                    30d, "境界返回人物页失败");
                await WaitFrames(5);

                bool honourButtonClicked = Click(stage, eventSystem, equipment._btn_fame);
                MarriageHonourViewBind honour = await WaitFor(
                    () => stage.CanvasRoot.GetComponentsInChildren<MarriageHonourViewBind>(false)
                        .FirstOrDefault(view => view.IsShown),
                    45d, "点击名誉后 MarriageHonourView 未打开");
                await WaitFrames(10);
                stage.ForceCjkFont();
                Canvas.ForceUpdateCanvases();
                string honourTopShot = stage.Capture(EvidenceRoot + "/20_honour_top.png");
                bool honourGeometry = VerifyHonourGeometry(honour, stage.CanvasRoot as RectTransform);

                Vector2 beforeDrag = honour._gp_con.content.anchoredPosition;
                bool dragged = Drag(stage, eventSystem, honour._gp_con, new Vector2(0f, 390f));
                await WaitFrames(8);
                Vector2 afterDrag = honour._gp_con.content.anchoredPosition;
                bool honourScroll = dragged && afterDrag.y > beforeDrag.y + 30f
                    && LastItemIntersectsViewport(honour);
                string honourDragShot = stage.Capture(EvidenceRoot + "/21_honour_dragged.png");

                bool honourClosed = Click(stage, eventSystem, honour._btn_close);
                await WaitFor(() => !honour.IsShown, 15d, "名誉关闭按钮未生效");
                honourButtonClicked &= Click(stage, eventSystem, equipment._btn_fame);
                await WaitFor(() => honour.IsShown, 30d, "名誉关闭后重开失败");
                await WaitFrames(8);
                bool honourReopen = honour._gp_con.verticalNormalizedPosition >= 0.995f
                    && honour._gp_con.content.anchoredPosition.y <= 1f;
                string honourReopenShot = stage.Capture(EvidenceRoot + "/22_honour_reopen.png");

                bool honourGoClicked = Click(stage, eventSystem, honour._btn_go);
                MarriageFlowerView flower = await WaitFor(
                    () => stage.CanvasRoot.GetComponentsInChildren<MarriageFlowerView>(false)
                        .FirstOrDefault(view => view.IsShown),
                    45d, "名誉获取按钮未打开送花页");
                await WaitFrames(6);
                string honourGoShot = stage.Capture(EvidenceRoot + "/23_honour_go_flower.png");

                bool route = realmButtonClicked && skyTabClicked && groundTabClicked
                    && titleSelected && honourButtonClicked && honourClosed && honourGoClicked;
                bool startupRead = outboundFrames.Count >= 2;
                bool pass = route && startupRead && groundGeometry && skyGeometry && skyRuntime
                    && skyEffects && titleScrolled && guardedWrites
                    && honourGeometry && honourScroll && honourReopen && flower != null;
                Debug.Log("CLIVERIFY role-realm-honour VERDICT route=" + route
                    + " startupRead=" + startupRead
                    + " groundGeometry=" + groundGeometry
                    + " skyGeometry=" + skyGeometry
                    + " skyRuntime=" + skyRuntime
                    + " skyEffects=" + skyEffects
                    + " titleScrolled=" + titleScrolled
                    + " guardedWrites=" + guardedWrites
                    + " honourGeometry=" + honourGeometry
                    + " honourScroll=" + honourScroll
                    + " honourReopen=" + honourReopen
                    + " shots=" + string.Join("|", new[] { roleShot, groundShot, skyShot,
                        skyDynamicShot, skyScrolledShot, groundReopenShot, honourTopShot,
                        honourDragShot, honourReopenShot, honourGoShot })
                    + " pass=" + pass);
                return pass ? 0 : 3;
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY role-realm-honour EXCEPTION " + exception);
                return 3;
            }
            finally
            {
                interceptField?.SetValue(null, oldIntercept);
                ResetFlows();
                MedalModel.Instance.Reset();
                if (eventSystem != null) UnityEngine.Object.DestroyImmediate(eventSystem.gameObject);
                stage?.Dispose();
            }
        }

        private static void PrepareRoleAndRealmState()
        {
            RoleModel.Instance.Level = 630;
            RoleModel.Instance.CombatPower = 2441807L;
            RoleModel.Instance.MarkBaseInfoReady();
            // 老端当前采证账号已激活凡人一阶；config_medal 的 id=1 是“未获得”占位，
            // 凡人一阶的真实当前项是 id=2。
            MedalModel.Instance.ReplaceData(2, 0, 0, 0, 36859, 9999);
            var titles = new List<MedalModel.TitleEntry>();
            for (uint id = 1001; id <= 1010; id++)
            {
                titles.Add(new MedalModel.TitleEntry(id, 0, id == 1001 ? 36859u : 0u, 0));
            }
            MedalModel.Instance.ReplaceTitles(titles);
        }

        private static bool VerifyGroundGeometry(MedalViewBind view, RectTransform canvas)
        {
            Rect page = RelativeRect(view.transform as RectTransform, canvas);
            Rect currentNames = RelativeRect(view._lb_cur_attr.rectTransform,
                view.transform as RectTransform);
            Rect currentValues = RelativeRect(view._lb_cur_attr1.rectTransform,
                view.transform as RectTransform);
            Rect nextNames = RelativeRect(view._lb_cur_next.rectTransform,
                view.transform as RectTransform);
            Rect nextValues = RelativeRect(view._lb_cur_next1.rectTransform,
                view.transform as RectTransform);
            Rect costs = RelativeRect(view.costList.transform as RectTransform,
                view.transform as RectTransform);
            bool pass = Near(page, 0f, 80f, 720f, 1280f, 1.5f)
                && NearPosition(currentNames, 174f, 625f, 2f)
                && NearPosition(currentValues, 283f, 625f, 2f)
                && NearPosition(nextNames, 450f, 625f, 2f)
                && NearPosition(nextValues, 549f, 625f, 2f)
                && Near(costs, 168f, 847f, 385f, 38f, 2f);
            Debug.Log("CLIVERIFY realm ground page=" + RectText(page)
                + " cur=" + RectText(currentNames) + "/" + RectText(currentValues)
                + " next=" + RectText(nextNames) + "/" + RectText(nextValues)
                + " costs=" + RectText(costs) + " pass=" + pass);
            return pass;
        }

        private static bool VerifySkyGeometry(TitleMainView view, RectTransform canvas)
        {
            RectTransform pageRoot = view.transform as RectTransform;
            Rect page = RelativeRect(pageRoot, canvas);
            Rect effect = RelativeRect(view.title_effect_gp, pageRoot);
            Rect attrs = RelativeRect(view._Scroller1.transform as RectTransform, pageRoot);
            Rect list = RelativeRect(view.Content.transform as RectTransform, pageRoot);
            Rect fight = RelativeRect(view._gp_fight, pageRoot);
            bool pass = Near(page, 0f, 80f, 720f, 992f, 1.5f)
                && Near(effect, 11f, 90f, 705f, 409f, 2f)
                && Near(attrs, 19f, 605f, 397f, 172f, 2f)
                && Near(list, 19f, 797f, 683f, 184f, 2f)
                && Near(fight, 1f, 59f, 720f, 41f, 2f);
            Debug.Log("CLIVERIFY realm sky page=" + RectText(page)
                + " effect=" + RectText(effect) + " attrs=" + RectText(attrs)
                + " list=" + RectText(list) + " fight=" + RectText(fight)
                + " pass=" + pass);
            return pass;
        }

        private static bool VerifySkyRuntime(TitleMainView view)
        {
            TitleItem[] items = view.Content.content.GetComponentsInChildren<TitleItem>(false);
            TitleAttrItem[] attrs = view._Scroller1.content.GetComponentsInChildren<TitleAttrItem>(false);
            RectMask2D horizontalMask = view.Content.viewport.GetComponent<RectMask2D>();
            RectMask2D verticalMask = view._Scroller1.viewport.GetComponent<RectMask2D>();
            int visibleTitleCount = 0;
            bool titlesReady = items.Length == 10;
            Rect viewportRect = new Rect(0f, 0f, view.Content.viewport.rect.width,
                view.Content.viewport.rect.height);
            for (int i = 0; i < items.Length; i++)
            {
                TitleItem item = items[i];
                Rect itemRect = RelativeRect(item.transform as RectTransform, view.Content.viewport);
                bool intersects = itemRect.Overlaps(viewportRect, true);
                if (intersects) visibleTitleCount++;
                titlesReady &= item.name_lb != null
                    && !string.IsNullOrWhiteSpace(item.name_lb.text)
                    && item.name_lb.isActiveAndEnabled && item.name_lb.fontSize >= 17.5f
                    && (!intersects || !item.name_lb.canvasRenderer.cull)
                    && item.stage_img != null && item.stage_img.gameObject.activeInHierarchy;
            }
            titlesReady &= visibleTitleCount >= 5;
            bool attrsReady = attrs.Length == 5 && attrs.All(item =>
                item.now_attr_lb != null && !string.IsNullOrWhiteSpace(item.now_attr_lb.text)
                && item.now_attr_lb.fontSize >= 19.5f
                && item.next_attr_lb != null && !string.IsNullOrWhiteSpace(item.next_attr_lb.text)
                && item.next_attr_lb.fontSize >= 19.5f);
            bool pageTextReady = view.name_lb != null && view.name_lb.text == "天境·如月"
                && view.up_level_lb != null && view.up_level_lb.text == "激活"
                && view.select_illsion != null && !view.select_illsion.gameObject.activeSelf;
            bool pass = titlesReady && attrsReady && pageTextReady
                && horizontalMask != null && horizontalMask.enabled
                && verticalMask != null && verticalMask.enabled
                && view.Content.horizontal && !view.Content.vertical
                && !view._Scroller1.horizontal && view._Scroller1.vertical;
            Debug.Log("CLIVERIFY realm sky-runtime titles=" + items.Length
                + " titlesReady=" + titlesReady + " visibleTitles=" + visibleTitleCount
                + " attrs=" + attrs.Length
                + " attrsReady=" + attrsReady + " pageTextReady=" + pageTextReady
                + " hMask=" + (horizontalMask != null)
                + " vMask=" + (verticalMask != null) + " pass=" + pass);
            for (int i = 0; i < items.Length; i++)
            {
                TitleItem item = items[i];
                Debug.Log("CLIVERIFY realm sky-title item=" + item.name
                    + " text=" + item.name_lb?.text + " active="
                    + (item.name_lb != null && item.name_lb.isActiveAndEnabled)
                    + " cull=" + (item.name_lb != null && item.name_lb.canvasRenderer.cull)
                    + " font=" + (item.name_lb != null ? item.name_lb.fontSize : 0f)
                    + " textRect=" + RectText(RelativeRect(item.name_lb?.rectTransform,
                        view.Content.viewport))
                    + " itemRect=" + RectText(RelativeRect(item.transform as RectTransform,
                        view.Content.viewport))
                    + " bgCull=" + (item._Image1 != null && item._Image1.canvasRenderer.cull));
            }
            return pass;
        }

        private static async Task WaitForSkyEffects(double timeoutSeconds)
        {
            await WaitFor(() =>
            {
                TickEffectStage();
                List<UIEffectStage.EffectDiagnostic> diagnostics = UIEffectStage.CollectDiagnostics()
                    .Where(item => item.Label != null
                        && item.Label.StartsWith("effect_shenmingjiemian_", StringComparison.Ordinal))
                    .ToList();
                return diagnostics.Count >= 11 && diagnostics.All(item => item.EffectAlive
                    && item.EffectActiveInHierarchy && item.RendererCount > 0
                    && item.ImageAlive && item.ImageHasTexture);
            }, timeoutSeconds, "天境主特效/十个称号项特效未加载完成");
        }

        private static async Task<bool> VerifySkyEffects()
        {
            TickEffectStage();
            List<UIEffectStage.EffectDiagnostic> diagnostics = UIEffectStage.CollectDiagnostics()
                .Where(item => item.Label != null
                    && item.Label.StartsWith("effect_shenmingjiemian_", StringComparison.Ordinal))
                .ToList();
            string failures = string.Join("|", UIEffectStage.CollectRecentFailures());
            bool mirrored = diagnostics.Count >= 11 && diagnostics.All(item => item.LocalScale.x < 0f);
            bool topology = diagnostics.Count >= 11 && mirrored && diagnostics.All(item => item.EffectAlive
                && item.EffectActiveInHierarchy && item.RendererCount > 0
                && item.ParentActiveInHierarchy && item.ImageActiveInHierarchy
                && item.ImageHasTexture);
            UIEffectStage.EffectDiagnostic main = diagnostics.FirstOrDefault(item =>
                item.Label == "effect_shenmingjiemian_01"
                && item.ParentName == "title_effect_gp");
            SimulateSkyParticles(0.35f);
            TickEffectStage();
            bool frameA = CaptureEffectChannel(main, "/11_sky_effect_rt_a.png",
                out Color32[] pixelsA, out int alphaA, out int litA);
            await WaitFrames(20);
            SimulateSkyParticles(0.2f);
            TickEffectStage();
            main = UIEffectStage.CollectDiagnostics().FirstOrDefault(item =>
                item.Label == "effect_shenmingjiemian_01"
                && item.ParentName == "title_effect_gp");
            bool frameB = CaptureEffectChannel(main, "/11_sky_effect_rt_b.png",
                out Color32[] pixelsB, out int alphaB, out int litB);
            int changed = CountChangedPixels(pixelsA, pixelsB);
            bool dynamic = frameA && frameB && changed > 100;
            bool pass = topology && dynamic && string.IsNullOrEmpty(failures);
            for (int i = 0; i < diagnostics.Count; i++)
            {
                UIEffectStage.EffectDiagnostic item = diagnostics[i];
                Debug.Log("CLIVERIFY realm sky-effect label=" + item.Label
                    + " parent=" + item.ParentName + " parentSize=" + item.ParentRectSize
                    + " scale=" + item.LocalScale + " particles=" + item.ParticleSystemCount
                    + "/" + item.AliveParticleCount + " playing=" + item.AnyParticlePlaying
                    + " renderers=" + item.RendererCount + " visible=" + item.AnyRendererVisible
                    + " bounds=" + item.WorldBoundsSize + " clip=" + item.VisibleClipFraction
                    + " channel=" + item.Channel + " rt=" + item.RtWidth + "x" + item.RtHeight
                    + " camera=" + item.CameraEnabled + "/" + item.CameraOrthoSize
                    + " image=" + item.ImageActiveInHierarchy + "/" + item.ImageHasTexture);
            }
            Debug.Log("CLIVERIFY realm sky-effects count=" + diagnostics.Count
                + " topology=" + topology + " mirrored=" + mirrored
                + " alpha=" + alphaA + "/" + alphaB
                + " lit=" + litA + "/" + litB + " changed=" + changed
                + " failures=" + failures + " pass=" + pass);
            return pass;
        }

        private static void SimulateSkyParticles(float seconds)
        {
            ParticleSystem[] systems = UnityEngine.Object.FindObjectsByType<ParticleSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            int simulated = 0;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                if (system == null || system.gameObject.layer != 31
                    || !system.gameObject.activeInHierarchy) continue;
                system.Simulate(seconds, false, false, true);
                simulated++;
            }
            Debug.Log("CLIVERIFY realm sky-simulate systems=" + simulated
                + " seconds=" + seconds.ToString("0.00"));
        }

        private static bool CaptureEffectChannel(UIEffectStage.EffectDiagnostic diagnostic,
            string fileName, out Color32[] pixels, out int alphaPixels, out int litPixels)
        {
            pixels = Array.Empty<Color32>();
            alphaPixels = 0;
            litPixels = 0;
            UIEffectStage.ChannelDiagnostic channel = UIEffectStage.CollectChannelDiagnostics()
                .FirstOrDefault(item => item.Name == diagnostic.Channel);
            if (!diagnostic.EffectAlive || channel.Camera == null || channel.Texture == null
                || channel.Image == null) return false;
            channel.Image.gameObject.SetActive(true);
            channel.Camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = channel.Texture;
            var copy = new Texture2D(channel.Texture.width, channel.Texture.height,
                TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0f, 0f, channel.Texture.width, channel.Texture.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            pixels = copy.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                if (pixel.a > 2) alphaPixels++;
                if (pixel.r > 2 || pixel.g > 2 || pixel.b > 2) litPixels++;
            }
            string path = Path.GetFullPath(EvidenceRoot + fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, copy.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(copy);
            return alphaPixels > 100 && litPixels > 100;
        }

        private static int CountChangedPixels(Color32[] a, Color32[] b)
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

        private static void TickEffectStage()
        {
            typeof(UIEffectStage).GetMethod("Tick", StaticPrivate)?.Invoke(null, null);
        }

        private static bool VerifyHonourGeometry(MarriageHonourViewBind view, RectTransform canvas)
        {
            RectTransform root = view.transform as RectTransform;
            Rect page = RelativeRect(root, canvas);
            Rect list = RelativeRect(view._gp_con.transform as RectTransform, root);
            RectMask2D mask = view._gp_con.viewport != null
                ? view._gp_con.viewport.GetComponent<RectMask2D>()
                : null;
            RectTransform[] rows = RuntimeHonourRows(view);
            bool rowsOk = rows.Length >= 6;
            for (int i = 0; i < Math.Min(6, rows.Length); i++)
            {
                Rect row = RelativeRect(rows[i], view._gp_con.content);
                rowsOk &= Near(row.x, 0f, 1f) && Near(row.y, i * 103f, 1f)
                    && Near(row.width, 502f, 1f) && Near(row.height, 93f, 1f);
            }
            bool pass = Near(page, 66f, 260f, 588f, 760f, 1.5f)
                && Near(list, 50f, 89f, 502f, 516f, 1.5f)
                && view._gp_con.content != null && mask != null && mask.enabled
                && !view._gp_con.horizontal && view._gp_con.vertical && rowsOk;
            Debug.Log("CLIVERIFY honour geometry page=" + RectText(page)
                + " list=" + RectText(list) + " rows=" + rows.Length
                + " content=" + RectText(RelativeRect(view._gp_con.content,
                    view._gp_con.viewport)) + " pass=" + pass);
            return pass;
        }

        private static bool LastItemIntersectsViewport(MarriageHonourViewBind view)
        {
            RectTransform[] rows = RuntimeHonourRows(view);
            if (rows.Length == 0 || view._gp_con.viewport == null) return false;
            Rect last = RelativeRect(rows[rows.Length - 1], view._gp_con.viewport);
            Rect viewport = new Rect(0f, 0f, view._gp_con.viewport.rect.width,
                view._gp_con.viewport.rect.height);
            return last.Overlaps(viewport, true);
        }

        private static RectTransform[] RuntimeHonourRows(MarriageHonourViewBind view)
        {
            if (view?._gp_con?.content == null) return Array.Empty<RectTransform>();
            return view._gp_con.content.GetComponentsInChildren<MarriageHonourItemBind>(false)
                .Where(row => row.gameObject != view._tpl_MarriageHonourItem)
                .Select(row => row.transform as RectTransform)
                .Where(rect => rect != null)
                .OrderByDescending(rect => rect.anchoredPosition.y)
                .ToArray();
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
            Debug.Log("CLIVERIFY role-realm-honour CLICK target=" + target.name
                + " targetHit=" + targetHit.HasValue
                + " hits=" + string.Join(",", hits.Select(hit => hit.gameObject.name)));
            if (!targetHit.HasValue) return false;

            // 从首个真实命中节点沿层级派发一次；页签的 Button 可能在子 Graphic，
            // 但禁止遍历所有命中项导致同一个 Button 被重复触发。
            return ExecuteEvents.ExecuteHierarchy(targetHit.Value.gameObject, pointer,
                ExecuteEvents.pointerClickHandler) != null;
        }

        private static bool Drag(CliVerify.Stage stage, EventSystem eventSystem,
            ScrollRect scroll, Vector2 screenDelta)
        {
            if (stage == null || eventSystem == null || scroll == null || scroll.content == null)
                return false;
            RectTransform[] rows = scroll.content.GetComponentsInChildren<RectTransform>(false)
                .Where(rect => rect != scroll.content).ToArray();
            RectTransform source = rows.FirstOrDefault();
            if (source == null) return false;
            Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
            Vector2 start = RectTransformUtility.WorldToScreenPoint(
                canvas.worldCamera, source.TransformPoint(source.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = start,
                pressPosition = start,
            };
            ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.position = start + screenDelta;
            pointer.delta = screenDelta;
            ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.endDragHandler);
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private static EventSystem EnsureEventSystem()
        {
            EventSystem existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null) return existing;
            return new GameObject("RoleRealmHonourCase_EventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
        }

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

        private static void ResetFlows()
        {
            typeof(MarriageHonourFlow).GetMethod("Reset", StaticPrivate)?.Invoke(null, null);
            typeof(MarriageFlow).GetMethod("Reset", StaticPrivate)?.Invoke(null, null);
            typeof(MedalFlow).GetMethod("Reset", StaticPrivate)?.Invoke(null, null);
            typeof(RoleFlow).GetMethod("Reset", StaticPrivate)?.Invoke(null, null);
        }

        private static Rect RelativeRect(RectTransform target, RectTransform root)
        {
            if (target == null || root == null) return new Rect(float.NaN, float.NaN, 0f, 0f);
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = root.InverseTransformPoint(corners[i]);
                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);
                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
            }
            return new Rect(minX - root.rect.xMin, root.rect.yMax - maxY,
                maxX - minX, maxY - minY);
        }

        private static bool Near(Rect actual, float x, float y, float width, float height,
            float tolerance)
            => Near(actual.x, x, tolerance) && Near(actual.y, y, tolerance)
                && Near(actual.width, width, tolerance) && Near(actual.height, height, tolerance);

        private static bool NearPosition(Rect actual, float x, float y, float tolerance)
            => Near(actual.x, x, tolerance) && Near(actual.y, y, tolerance);

        private static bool Near(float actual, float expected, float tolerance)
            => Mathf.Abs(actual - expected) <= tolerance;

        private static string RectText(Rect rect)
            => rect.x.ToString("0.##") + "," + rect.y.ToString("0.##") + ","
                + rect.width.ToString("0.##") + "x" + rect.height.ToString("0.##");
    }
}
