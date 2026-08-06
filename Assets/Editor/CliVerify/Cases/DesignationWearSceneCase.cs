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
            "output/ui_route_audit/2026-08-06_designation_ghost/cli_designation_scene_main_duplicate/";
        private const long MainRoleId = 920000001;
        private const long OtherRoleId = 920000002;

        /// <summary>
        /// 不依赖称号图片/特效资源的主角同 ID 回归：覆盖 12002 落表边界、脏快照防御和移动跟随。
        /// </summary>
        public static async Task<int> RunMainGhostRegression()
        {
            RoleModel main = RoleModel.Instance;
            long oldRoleId = main.RoleId;
            FigureProto oldFigure = main.Figure;
            int oldSceneId = main.SceneId;
            int oldDunId = main.DunId;
            int oldPkStatus = main.PkStatus;
            int oldX = main.X;
            int oldY = main.Y;
            CliVerify.Stage stage = null;
            bool pass = false;
            bool restored = false;

            try
            {
                SceneDesignationPresenter.EnsureInstalled();
                SceneDesignationPresenter.ClearAll();
                SceneManager.Instance.RemoveRole(MainRoleId);
                await Task.Delay(50);
                stage = CliVerify.Stage.Create();

                Vector2 camera = SceneMapView.CameraPos;
                var mainFigure = new FigureProto();
                var snapshotFigure = new FigureProto();
                mainFigure.SetDesignationId(801001);
                snapshotFigure.SetDesignationId(801001);
                main.RoleId = MainRoleId;
                main.Figure = mainFigure;
                main.SceneId = 1001;
                main.DunId = 0;
                main.X = Mathf.RoundToInt(camera.x + 40f);
                main.Y = Mathf.RoundToInt(camera.y + 20f);
                int snapshotPk = oldPkStatus == 2 ? 3 : 2;
                var mainSnapshot = new RoleVo
                {
                    RoleId = MainRoleId,
                    Figure = snapshotFigure,
                    X = main.X,
                    Y = main.Y,
                    PkStatus = snapshotPk,
                };

                MethodInfo applyParsedRole = typeof(SceneController).GetMethod("ApplyParsedRole", StaticPrivate);
                MethodInfo refreshMain = typeof(SceneDesignationPresenter).GetMethod("RefreshMain", StaticPrivate);
                if (applyParsedRole == null || refreshMain == null)
                    throw new MissingMethodException("designation main ghost hooks missing");

                applyParsedRole.Invoke(null, new object[] { mainSnapshot });
                bool snapshotFiltered = SceneManager.Instance.GetRole(MainRoleId) == null
                    && main.PkStatus == snapshotPk;

                // 模拟热重载前留下的脏表：展示层也不能创建 RoleDesignation_<主角ID>。
                SceneManager.Instance.AddRole(mainSnapshot);
                refreshMain.Invoke(null, null);
                TickPresenter();
                RectTransform mainRect = FindRect("MainRoleDesignation");
                RectTransform duplicateBeforeMove = FindRect("RoleDesignation_" + MainRoleId);
                Vector2 expectedStart = ExpectedPosition(main.X, main.Y);
                bool initialPosition = mainRect != null
                    && Vector2.Distance(mainRect.anchoredPosition, expectedStart) < 1.5f;

                Vector2 start = mainRect != null ? mainRect.anchoredPosition : Vector2.zero;
                main.X += 96;
                main.Y += 44;
                TickPresenter();
                Vector2 expectedMoved = ExpectedPosition(main.X, main.Y);
                RectTransform duplicateAfterMove = FindRect("RoleDesignation_" + MainRoleId);
                bool followsMain = mainRect != null
                    && Vector2.Distance(mainRect.anchoredPosition, expectedMoved) < 1.5f
                    && Vector2.Distance(mainRect.anchoredPosition, start) > 20f;
                bool noStaticGhost = duplicateBeforeMove == null && duplicateAfterMove == null;
                pass = snapshotFiltered && initialPosition && followsMain && noStaticGhost;
                Debug.Log("CLIVERIFY designation-main-ghost snapshotFiltered=" + snapshotFiltered
                    + " initialPosition=" + initialPosition + " followsMain=" + followsMain
                    + " noStaticGhost=" + noStaticGhost);
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY designation-main-ghost EXCEPTION " + e);
            }
            finally
            {
                SceneDesignationPresenter.ClearAll();
                SceneManager.Instance.RemoveRole(MainRoleId);
                main.RoleId = oldRoleId;
                main.Figure = oldFigure;
                main.SceneId = oldSceneId;
                main.DunId = oldDunId;
                main.PkStatus = oldPkStatus;
                main.X = oldX;
                main.Y = oldY;
                stage?.Dispose();
                restored = main.RoleId == oldRoleId && ReferenceEquals(main.Figure, oldFigure)
                    && main.SceneId == oldSceneId && main.DunId == oldDunId
                    && main.PkStatus == oldPkStatus && main.X == oldX && main.Y == oldY
                    && SceneManager.Instance.GetRole(MainRoleId) == null;
            }

            bool finalPass = pass && restored;
            Debug.Log("CLIVERIFY designation-main-ghost restored=" + restored);
            Debug.Log("CLIVERIFY designation-main-ghost VERDICT pass=" + finalPass);
            return finalPass ? 0 : 3;
        }

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
            int oldPkStatus = main.PkStatus;
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
                SceneManager.Instance.RemoveRole(MainRoleId);
                SceneManager.Instance.RemoveRole(OtherRoleId);
                stage = CliVerify.Stage.Create();

                Vector2 camera = SceneMapView.CameraPos;
                var mainFigure = new FigureProto();
                var mainSnapshotFigure = new FigureProto();
                var otherFigure = new FigureProto();
                main.RoleId = MainRoleId;
                main.Figure = mainFigure;
                main.SceneId = 1001;
                main.DunId = 0;
                main.X = Mathf.RoundToInt(camera.x + 48f);
                main.Y = Mathf.RoundToInt(camera.y + 24f);
                mainFigure.SetDesignationId(staticRow.Id);
                mainSnapshotFigure.SetDesignationId(staticRow.Id);
                var mainSnapshot = new RoleVo
                {
                    RoleId = MainRoleId,
                    Figure = mainSnapshotFigure,
                    X = main.X,
                    Y = main.Y,
                    PkStatus = oldPkStatus == 2 ? 3 : 2,
                };
                var other = new RoleVo
                {
                    RoleId = OtherRoleId,
                    Figure = otherFigure,
                    X = Mathf.RoundToInt(camera.x - 72f),
                    Y = Mathf.RoundToInt(camera.y - 36f),
                };
                // 12002 的角色列表会包含主角自身。主角只能由 MainRoleDesignation 消费；
                // 如果又按普通 RoleVo 创建一次，移动后该副本会停在进场坐标形成静态残影。
                MethodInfo applyParsedRole = typeof(SceneController).GetMethod("ApplyParsedRole", StaticPrivate);
                if (applyParsedRole == null) throw new MissingMethodException("ApplyParsedRole missing");
                applyParsedRole.Invoke(null, new object[] { mainSnapshot });
                bool mainSnapshotFiltered = SceneManager.Instance.GetRole(MainRoleId) == null
                    && main.PkStatus == mainSnapshot.PkStatus;
                // 再模拟热重载前已经存在的脏数据，展示层也必须自行拒绝并清理主角同 ID 副本。
                SceneManager.Instance.AddRole(mainSnapshot);
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
                        && otherBoard.HasDesignationVisual;
                }, 20d);

                TickPresenter();
                TickEffectStage();
                await Task.Delay(120);
                TickEffectStage();
                RectTransform duplicateBeforeMove = FindRect("RoleDesignation_" + MainRoleId);
                Vector2 duplicateStart = duplicateBeforeMove != null
                    ? duplicateBeforeMove.anchoredPosition
                    : Vector2.zero;
                main.X += 96;
                main.Y += 44;
                TickPresenter();
                RectTransform duplicateAfterMove = FindRect("RoleDesignation_" + MainRoleId);
                bool noDuplicateMain = duplicateBeforeMove == null && duplicateAfterMove == null
                    && SceneDesignationPresenter.VisibleCount == 2;
                bool stationaryGhostObserved = duplicateAfterMove != null
                    && Vector2.Distance(duplicateAfterMove.anchoredPosition, duplicateStart) < 1.5f;
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
                    && BoardCleared("MainRoleDesignation")
                    && BoardCleared("RoleDesignation_" + MainRoleId);
                SceneManager.Instance.RemoveRole(MainRoleId);
                SceneManager.Instance.RemoveRole(OtherRoleId);
                await Task.Delay(50);
                bool otherRemoved = BoardCleared("RoleDesignation_" + OtherRoleId);

                EventDispatcher.Emit(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED);
                await Task.Delay(50);
                bool sceneCleared = SceneDesignationPresenter.VisibleCount == 0
                    && FindAnyLiveBoard() == null;

                pass &= mainSnapshotFiltered && visualReady && noDuplicateMain
                    && dynamicPixels && positions && noticeData
                    && starHidden && polarHidden && maskHidden
                    && mainCleared && otherRemoved && sceneCleared;
                Debug.Log("CLIVERIFY designation-wear-scene mainSnapshotFiltered=" + mainSnapshotFiltered
                    + " visual=" + visualReady
                    + " noDuplicateMain=" + noDuplicateMain
                    + " stationaryGhostObserved=" + stationaryGhostObserved
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
                SceneManager.Instance.RemoveRole(MainRoleId);
                SceneManager.Instance.RemoveRole(OtherRoleId);
                main.RoleId = oldRoleId;
                main.Figure = oldFigure;
                main.SceneId = oldSceneId;
                main.DunId = oldDunId;
                main.PkStatus = oldPkStatus;
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
                    && main.PkStatus == oldPkStatus
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
