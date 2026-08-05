using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Module.Core.Designation;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Scene.Vo;
using Shenxiao.Module.Core.UiComponent;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Shenxiao.EditorTools
{
    /// <summary>称号 41105 主角/他人 Figure 与 NameBoard 静态图、动态特效、位置和清场闭环。</summary>
    public static class DesignationWearSceneCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private const string EvidenceRoot =
            "output/ui_route_audit/2026-08-04_role_web_round2/cli_designation_scene_20260805_2501/";
        private const long MainRoleId = 920000001;
        private const long OtherRoleId = 920000002;

        public static async Task<int> Run()
        {
            bool fallbackBefore = ResManager.EditorPreferFallback;
            DesignationController controller = DesignationController.Instance;
            DesignationModel model = DesignationModel.Instance;
            RoleModel main = RoleModel.Instance;
            bool controllerWasInitialized = controller.IsInitialized;
            Dictionary<FieldInfo, object> oldModelFields = CaptureAutoFields(model);
            var oldEntries = new List<DesignationModel.Entry>(model.Entries);
            long oldRoleId = main.RoleId;
            FigureProto oldFigure = main.Figure;
            int oldSceneId = main.SceneId;
            int oldDunId = main.DunId;
            int oldX = main.X;
            int oldY = main.Y;
            CliVerify.Stage stage = null;
            bool pass = true;
            bool restored = false;

            try
            {
                ResManager.EditorPreferFallback = true;
                await DesignationConfigs.EnsureLoaded();
                DesignationConfigs.Row staticRow = DesignationConfigs.Get(801001);
                DesignationConfigs.Row dynamicRow = DesignationConfigs.Get(305414);
                Check(ref pass, "static/dynamic config identity",
                    staticRow != null && staticRow.Type != 1 && !string.IsNullOrWhiteSpace(staticRow.ResourceId)
                    && dynamicRow != null && dynamicRow.Type == 1
                    && !string.IsNullOrWhiteSpace(dynamicRow.ResourceId));
                if (!pass) return 3;

                if (!controllerWasInitialized) controller.Init();
                SceneDesignationPresenter.EnsureInstalled();
                SceneDesignationPresenter.ClearAll();
                SceneManager.Instance.RemoveRole(OtherRoleId);
                stage = CliVerify.Stage.Create();

                Vector2 camera = SceneMapView.CameraPos;
                var mainFigure = new FigureProto();
                var otherFigure = new FigureProto();
                main.RoleId = MainRoleId;
                main.Figure = mainFigure;
                main.SceneId = 1001;
                main.DunId = 0;
                main.X = Mathf.RoundToInt(camera.x + 48f);
                main.Y = Mathf.RoundToInt(camera.y + 24f);
                var other = new RoleVo
                {
                    RoleId = OtherRoleId,
                    Figure = otherFigure,
                    X = Mathf.RoundToInt(camera.x - 72f),
                    Y = Mathf.RoundToInt(camera.y - 36f),
                };
                SceneManager.Instance.AddRole(other);

                MethodInfo on41105 = typeof(DesignationController).GetMethod("On41105", InstancePrivate);
                Feed(on41105, controller, new CliVerify.Pkt().L(MainRoleId).I(staticRow.Id).Bytes());
                Feed(on41105, controller, new CliVerify.Pkt().L(OtherRoleId).I(dynamicRow.Id).Bytes());
                bool visualReady = await WaitUntil(() =>
                {
                    NameBoard mainBoard = FindBoard("MainRoleDesignation");
                    NameBoard otherBoard = FindBoard("RoleDesignation_" + OtherRoleId);
                    return mainBoard != null && mainBoard.DesignationId == staticRow.Id
                        && mainBoard.HasDesignationVisual
                        && otherBoard != null && otherBoard.DesignationId == dynamicRow.Id
                        && otherBoard.HasDesignationVisual
                        && SceneDesignationPresenter.VisibleCount == 2;
                }, 20d);

                TickPresenter();
                TickEffectStage();
                await Task.Delay(120);
                TickEffectStage();
                bool dynamicPixels = CaptureDynamicEffect(dynamicRow.ResourceId,
                    "scene_designation_effect_rt.png", out int alphaPixels, out int litPixels);
                string sceneShot = stage.Capture(EvidenceRoot + "scene_titles.png");

                RectTransform mainRect = FindRect("MainRoleDesignation");
                RectTransform otherRect = FindRect("RoleDesignation_" + OtherRoleId);
                Vector2 expectedMain = ExpectedPosition(main.X, main.Y);
                Vector2 expectedOther = ExpectedPosition(other.X, other.Y);
                bool positions = mainRect != null && otherRect != null
                    && Vector2.Distance(mainRect.anchoredPosition, expectedMain) < 1.5f
                    && Vector2.Distance(otherRect.anchoredPosition, expectedOther) < 1.5f
                    && Vector2.Distance(mainRect.sizeDelta, new Vector2(237f, 150f)) < 0.1f
                    && Vector2.Distance(otherRect.sizeDelta, new Vector2(237f, 150f)) < 0.1f
                    && mainRect.localScale == Vector3.one && otherRect.localScale == Vector3.one;
                bool noticeData = mainFigure.DesignationId == staticRow.Id
                    && otherFigure.DesignationId == dynamicRow.Id
                    && model.SceneNotice != null && model.SceneNotice.PlayerId == (ulong)OtherRoleId
                    && model.SceneNotice.Id == dynamicRow.Id;

                main.SceneId = 5502;
                TickPresenter();
                bool starHidden = SceneDesignationPresenter.VisibleCount == 0;
                main.SceneId = 1001;
                main.DunId = 36001;
                TickPresenter();
                bool polarHidden = SceneDesignationPresenter.VisibleCount == 0;
                main.DunId = 0;
                mainFigure.SetMaskId(1);
                TickPresenter();
                bool maskHidden = SceneDesignationPresenter.VisibleCount == 1
                    && FindRect("MainRoleDesignation") != null
                    && !FindRect("MainRoleDesignation").gameObject.activeSelf;
                mainFigure.SetMaskId(0);
                TickPresenter();

                Feed(on41105, controller, new CliVerify.Pkt().L(MainRoleId).I(0).Bytes());
                await Task.Delay(50);
                bool mainCleared = mainFigure.DesignationId == 0
                    && BoardCleared("MainRoleDesignation");
                SceneManager.Instance.RemoveRole(OtherRoleId);
                await Task.Delay(50);
                bool otherRemoved = BoardCleared("RoleDesignation_" + OtherRoleId);

                EventDispatcher.Emit(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED);
                await Task.Delay(50);
                bool sceneCleared = SceneDesignationPresenter.VisibleCount == 0
                    && FindAnyLiveBoard() == null;

                pass &= visualReady && dynamicPixels && positions && noticeData
                    && starHidden && polarHidden && maskHidden
                    && mainCleared && otherRemoved && sceneCleared;
                Debug.Log("CLIVERIFY designation-wear-scene visual=" + visualReady
                    + " dynamicPixels=" + dynamicPixels + " alpha=" + alphaPixels + " lit=" + litPixels
                    + " positions=" + positions + " notice=" + noticeData
                    + " starHidden=" + starHidden + " polarHidden=" + polarHidden
                    + " maskHidden=" + maskHidden + " mainCleared=" + mainCleared
                    + " otherRemoved=" + otherRemoved + " sceneCleared=" + sceneCleared
                    + " resolution=" + CliVerify.CaptureWidth + "x" + CliVerify.CaptureHeight
                    + " shot=" + sceneShot);
            }
            catch (Exception e)
            {
                pass = false;
                Debug.LogError("CLIVERIFY designation-wear-scene EXCEPTION " + e);
            }
            finally
            {
                EventDispatcher.Emit(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED);
                SceneDesignationPresenter.ClearAll();
                SceneManager.Instance.RemoveRole(OtherRoleId);
                main.RoleId = oldRoleId;
                main.Figure = oldFigure;
                main.SceneId = oldSceneId;
                main.DunId = oldDunId;
                main.X = oldX;
                main.Y = oldY;
                stage?.Dispose();
                if (controller.IsInitialized && !controllerWasInitialized) controller.Dispose();
                model.Reset();
                RestoreEntries(model, oldEntries);
                RestoreAutoFields(model, oldModelFields);
                ResManager.EditorPreferFallback = fallbackBefore;
                restored = controller.IsInitialized == controllerWasInitialized
                    && main.RoleId == oldRoleId && ReferenceEquals(main.Figure, oldFigure)
                    && main.SceneId == oldSceneId && main.DunId == oldDunId
                    && main.X == oldX && main.Y == oldY
                    && SameAutoFields(model, oldModelFields)
                    && SameEntries(model.Entries, oldEntries);
            }

            bool finalPass = pass && restored;
            Debug.Log("CLIVERIFY designation-wear-scene restored=" + restored);
            Debug.Log("CLIVERIFY designation-wear-scene VERDICT pass=" + finalPass);
            return finalPass ? 0 : 3;
        }

        private static Vector2 ExpectedPosition(int x, int y)
        {
            Vector2 camera = SceneMapView.CameraPos;
            return new Vector2(x - camera.x,
                -(y - camera.y) - SceneMapView.SceneLayerYOffset + 180f);
        }

        private static NameBoard FindBoard(string name)
        {
            foreach (NameBoard board in Object.FindObjectsByType<NameBoard>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (board != null && board.gameObject.name == name) return board;
            return null;
        }

        private static bool BoardCleared(string name)
        {
            NameBoard board = FindBoard(name);
            return board == null || (board.DesignationId == 0 && !board.HasDesignationVisual
                && !board.gameObject.activeSelf);
        }

        private static NameBoard FindAnyLiveBoard()
        {
            foreach (NameBoard board in Object.FindObjectsByType<NameBoard>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (board != null && (board.gameObject.name == "MainRoleDesignation"
                    || board.gameObject.name.StartsWith("RoleDesignation_", StringComparison.Ordinal)))
                {
                    if (board.DesignationId != 0 || board.HasDesignationVisual
                        || board.gameObject.activeSelf) return board;
                }
            return null;
        }

        private static RectTransform FindRect(string name) => FindBoard(name)?.transform as RectTransform;

        private static void TickPresenter()
            => typeof(SceneDesignationPresenter).GetMethod("UpdatePositions", StaticPrivate)?.Invoke(null, null);

        private static void TickEffectStage()
            => typeof(UIEffectStage).GetMethod("Tick", StaticPrivate)?.Invoke(null, null);

        private static bool CaptureDynamicEffect(string label, string fileName,
            out int alphaPixels, out int litPixels)
        {
            alphaPixels = 0;
            litPixels = 0;
            UIEffectStage.EffectDiagnostic diagnostic = UIEffectStage.CollectDiagnostics().Find(item =>
                string.Equals(item.Label, label, StringComparison.OrdinalIgnoreCase)
                && item.ParentName == "DesignationEffect");
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
            foreach (Color32 pixel in copy.GetPixels32())
            {
                if (pixel.a > 2) alphaPixels++;
                if (pixel.r > 2 || pixel.g > 2 || pixel.b > 2) litPixels++;
            }
            string path = Path.GetFullPath(CliVerify.AppendResolutionSuffix(EvidenceRoot + fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, copy.EncodeToPNG());
            Object.DestroyImmediate(copy);
            return alphaPixels > 64 && litPixels > 64;
        }

        private static void Feed(MethodInfo handler, object target, byte[] packet)
        {
            if (handler == null) throw new MissingMethodException("On41105 missing");
            var reader = new NetReader(packet, 0, packet.Length);
            handler.Invoke(target, new object[] { reader });
            if (reader.Remaining != 0) throw new InvalidOperationException("41105 unread=" + reader.Remaining);
        }

        private static async Task<bool> WaitUntil(Func<bool> predicate, double timeoutSeconds)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                if (predicate()) return true;
                TickPresenter();
                TickEffectStage();
                await Task.Delay(50);
            }
            return predicate();
        }

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
            foreach (KeyValuePair<FieldInfo, object> pair in values) pair.Key.SetValue(model, pair.Value);
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

        private static void Check(ref bool pass, string name, bool condition)
        {
            pass &= condition;
            Debug.Log("CLIVERIFY designation-wear-scene " + name + "=" + condition);
        }
    }
}
