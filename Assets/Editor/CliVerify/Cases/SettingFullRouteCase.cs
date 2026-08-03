using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Alert;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Setting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Shenxiao.EditorTools
{
    /// <summary>设置主窗全部固定可达叶子的真实 Prefab 射线点击、回包即时刷新与危险操作取消验收。</summary>
    public static class SettingFullRouteCase
    {
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const string RenameValue = "星河客";

        public static async Task<int> Run()
        {
            bool fallback = ResManager.EditorPreferFallback;
            FieldInfo settingIntercept = typeof(SettingController).GetField("s_outboundIntercept", PrivateStatic);
            FieldInfo renameIntercept = typeof(RoleController).GetField("s_renameOutboundIntercept", PrivateStatic);
            FieldInfo settingRootField = typeof(SettingFlow).GetField("_moduleRoot", PrivateStatic);
            FieldInfo settingMainField = typeof(SettingFlow).GetField("_mainView", PrivateStatic);
            object oldSettingIntercept = settingIntercept?.GetValue(null);
            object oldRenameIntercept = renameIntercept?.GetValue(null);
            object oldSettingRoot = settingRootField?.GetValue(null);
            object oldSettingMain = settingMainField?.GetValue(null);
            bool settingWasInitialized = SettingController.Instance.IsInitialized;
            bool roleWasInitialized = RoleController.Instance.IsInitialized;
            var frames = new List<byte[]>();
            CliVerify.Stage stage = null;
            GameObject eventSystemGo = null;
            GameObject root = null;
            bool pass = true;
            void Check(string tag, bool ok) { Debug.Log("CLIVERIFY setting-full " + tag + " ok=" + ok); if (!ok) pass = false; }

            try
            {
                ResManager.EditorPreferFallback = true;
                if (!settingWasInitialized) SettingController.Instance.Init();
                if (!roleWasInitialized) RoleController.Instance.Init();
                if (settingIntercept == null || renameIntercept == null) throw new MissingFieldException("setting/rename outbound intercept");
                settingIntercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                renameIntercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));

                await SettingConfigs.EnsureLoaded();
                await FuncOpenConfig.EnsureLoaded();
                SeedModels();
                // 该用例会与其它设置/时装用例在同一个常驻 Editor 进程串行执行。上一次中断若留下
                // 10203 pending，第一笔模拟成功回包会错误消费旧事务，导致后续“自动任务”看似点中
                // 却因模型仍是旧值而不发包。这里仅清理编辑器验收残留，确保每条点击与自己的回包配对。
                object pendingQueue = typeof(SettingController).GetField("_pending", PrivateInstance)
                    ?.GetValue(SettingController.Instance);
                pendingQueue?.GetType().GetMethod("Clear")?.Invoke(pendingQueue, null);

                stage = CliVerify.Stage.Create();
                eventSystemGo = new GameObject("SettingFullRoute_EventSystem", typeof(EventSystem));
                EventSystem eventSystem = eventSystemGo.GetComponent<EventSystem>();
                Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
                GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
                Camera camera = canvas.worldCamera;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Setting/SettingModule.prefab");
                root = Object.Instantiate(prefab, ViewManager.GetLayer(UILayer.Window));
                foreach (Transform child in root.transform) child.gameObject.SetActive(false);
                SettingView view = root.GetComponentInChildren<SettingView>(true);
                if (settingRootField == null || settingMainField == null) throw new MissingFieldException("SettingFlow root/main");
                settingRootField.SetValue(null, root);
                settingMainField.SetValue(null, view);
                view.Show();
                await Task.Delay(800);
                Canvas.ForceUpdateCanvases();

                Check("prefab/static-tree", view != null && view._box_base_setting.gameObject.activeInHierarchy
                    && root.GetComponentsInChildren<WithBtnHSlider>(false).Length == 4
                    && ActiveItems(view._list_pick).Length == 3);

                GUIUtility.systemCopyBuffer = string.Empty;
                Check("copy-id", Click(view._btn_copy, camera, raycaster, eventSystem)
                    && GUIUtility.systemCopyBuffer == RoleModel.Instance.RoleId.ToString());

                Check("shield-tab-click", Click(view._box_tab_shield_setting, camera, raycaster, eventSystem));
                await Task.Delay(100);
                SettingShieldItem[] shieldItems = ActiveItems(view._list_shield);
                Check("shield-count", view._box_shield_setting.gameObject.activeInHierarchy && shieldItems.Length == 10);
                foreach (SettingShieldItem item in shieldItems)
                    Check("shield/" + item._lb_text.text, await ClickToggleAndAck(item, view, frames, camera, raycaster, eventSystem));

                Check("base-tab-click", Click(view._box_tab_base_setting, camera, raycaster, eventSystem));
                await Task.Delay(100);
                foreach (SettingShieldItem item in ActiveItems(view._list_pick))
                    Check("pick/" + item._lb_text.text, await ClickToggleAndAck(item, view, frames, camera, raycaster, eventSystem));

                if (view._box_horse.gameObject.activeInHierarchy)
                    Check("horse", await ClickSettingAndAck(view._img_horse_check, frames, camera, raycaster, eventSystem));
                else Check("horse-gated", !FuncOpenConfig.CheckFuncOpenState("HorseComponentView"));
                if (view._box_god.gameObject.activeInHierarchy)
                    Check("godbefall", await ClickSettingAndAck(view._img_god_check, frames, camera, raycaster, eventSystem));
                else Check("godbefall-gated", !FuncOpenConfig.CheckFuncOpenState("GodBefallMainView"));

                Check("auto-task-manual", await ClickSettingAndAck(view._img_task_check2, frames, camera, raycaster, eventSystem));
                Check("auto-task-auto", await ClickSettingAndAck(view._img_task_check1, frames, camera, raycaster, eventSystem));

                GreenHSlider[] sliders = root.GetComponentsInChildren<GreenHSlider>(false);
                bool slidersChanged = sliders.Length == 4;
                foreach (GreenHSlider slider in sliders)
                {
                    float before = slider.Value;
                    slidersChanged &= PointerTo(slider.track, 0.85f, camera, raycaster, eventSystem) && !Mathf.Approximately(before, slider.Value);
                }
                Check("four-slider-pointer", slidersChanged);

                Check("agreement", await OpenAndCloseDocument(view._lb_agree, LoginUserAgreementView.TypeAgreement,
                    camera, raycaster, eventSystem));
                Check("privacy", await OpenAndCloseDocument(view._lb_privacy, LoginUserAgreementView.TypePrivacy,
                    camera, raycaster, eventSystem));

                frames.Clear();
                Check("rename-entry", Click(view._btn_changename, camera, raycaster, eventSystem)
                    && frames.Any(frame => Command(frame) == Proto.RENAME_FREE_CHECK));
                Feed(typeof(RoleController).GetMethod("On42602", PrivateInstance), RoleController.Instance,
                    new CliVerify.Pkt().I(1).Bytes());
                SettingChangeNameView rename = await WaitActive<SettingChangeNameView>(3d);
                await Task.Delay(100);
                Canvas.ForceUpdateCanvases();
                Check("rename-window", rename != null);
                if (rename != null)
                {
                    frames.Clear();
                    rename.InptextDisplay.text = "甲";
                    Check("rename-invalid-length-click", Click(rename.confirmBtn, camera, raycaster, eventSystem));
                    await Task.Delay(150);
                    Check("rename-invalid-length-blocked", !frames.Any(frame => Command(frame) == Proto.RENAME_CHECK));

                    frames.Clear();
                    rename.InptextDisplay.text = "测试甲";
                    Check("rename-sensitive-click", Click(rename.confirmBtn, camera, raycaster, eventSystem));
                    await Task.Delay(300);
                    Check("rename-sensitive-blocked", !frames.Any(frame => Command(frame) == Proto.RENAME_CHECK));

                    rename.InptextDisplay.text = RenameValue;
                    frames.Clear();
                    Check("rename-confirm-click", Click(rename.confirmBtn, camera, raycaster, eventSystem));
                    Check("rename-check-frame", await WaitCommand(frames, Proto.RENAME_CHECK, 3d));
                    Feed(typeof(RoleController).GetMethod("On42604", PrivateInstance), RoleController.Instance,
                        new CliVerify.Pkt().I(1).S(RenameValue).Bytes());
                    AlertTypeTwoBind renameConfirm = await WaitActive<AlertTypeTwoBind>(3d);
                    await Task.Delay(100);
                    Canvas.ForceUpdateCanvases();
                    Check("rename-second-confirm", renameConfirm != null && Click(renameConfirm._ok_btn, camera, raycaster, eventSystem));
                    Check("rename-submit-frame", await WaitCommand(frames, Proto.RENAME_SUBMIT, 2d));
                    Feed(typeof(RoleController).GetMethod("On42601", PrivateInstance), RoleController.Instance,
                        new CliVerify.Pkt().I(1).S(RenameValue).Bytes());
                    Feed(typeof(SceneController).GetMethod("On12086", PrivateInstance), SceneController.Instance,
                        new CliVerify.Pkt().L(RoleModel.Instance.RoleId).S(RenameValue).Bytes());
                    await Task.Delay(100);
                    Check("rename-immediate-parent", view._lb_name.text == RenameValue && !rename.gameObject.activeInHierarchy);

                    SettingFlow.OpenSub("SettingChangeNameView", 1);
                    SettingChangeNameView reopenedRename = await WaitActive<SettingChangeNameView>(3d);
                    await Task.Delay(100);
                    Canvas.ForceUpdateCanvases();
                    bool reopenOk = reopenedRename != null && view._lb_name.text == RenameValue
                        && string.IsNullOrEmpty(reopenedRename.InptextDisplay.text)
                        && Click(reopenedRename.cancleBtn, camera, raycaster, eventSystem);
                    await Task.Delay(50);
                    Check("rename-reopen", reopenOk && !reopenedRename.gameObject.activeInHierarchy);
                }

                Check("restore-default", await ConfirmAndAckSetting(view.simple_mode_btn, Proto.SETTING_WRITE,
                    frames, camera, raycaster, eventSystem));
                Check("escape-stuck", await ConfirmAndAckSetting(view.confirm_flee, Proto.SETTING_FLEE,
                    frames, camera, raycaster, eventSystem));
                Check("switch-role-cancel", await ConfirmAndCancel(view.change_role, camera, raycaster, eventSystem));
                Check("switch-account-cancel", await ConfirmAndCancel(view.return_login, camera, raycaster, eventSystem));
                Check("repair-cancel", await ConfirmAndCancel(view.confirm_res, camera, raycaster, eventSystem));

                frames.Clear();
                Check("close-click", Click(view._img_close, camera, raycaster, eventSystem));
                await Task.Delay(100);
                Check("slider-batch-on-close", !view.gameObject.activeInHierarchy
                    && frames.Count(frame => Command(frame) == Proto.SETTING_WRITE) == 1);
                if (frames.Any(frame => Command(frame) == Proto.SETTING_WRITE)) FeedSettingSuccess();

                view.Show();
                await Task.Delay(300);
                Canvas.ForceUpdateCanvases();
                Check("reopen-current-name", view._lb_name.text == RenameValue);
                stage.ForceCjkFont();
                string shot = stage.Capture("output/settings_full_route/final.png");
                Check("reopen-close", Click(view._img_close, camera, raycaster, eventSystem));
                Debug.Log("CLIVERIFY setting-full shot=" + shot + " VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY setting-full EXCEPTION " + exception);
                return 3;
            }
            finally
            {
                ConfirmDialog.ReloadView();
                ViewManager.Dispose<LoginUserAgreementView>();
                if (root != null) Object.DestroyImmediate(root);
                if (eventSystemGo != null) Object.DestroyImmediate(eventSystemGo);
                stage?.Dispose();
                settingIntercept?.SetValue(null, oldSettingIntercept);
                renameIntercept?.SetValue(null, oldRenameIntercept);
                settingRootField?.SetValue(null, oldSettingRoot);
                settingMainField?.SetValue(null, oldSettingMain);
                if (!settingWasInitialized && SettingController.Instance.IsInitialized) SettingController.Instance.Dispose();
                if (!roleWasInitialized && RoleController.Instance.IsInitialized) RoleController.Instance.Dispose();
                SettingModel.Reset();
                RoleModel.Instance.Reset();
                ResManager.EditorPreferFallback = fallback;
            }
        }

        private static void SeedModels()
        {
            RoleModel role = RoleModel.Instance;
            role.Reset();
            role.RoleId = 123456789;
            role.ServerName = "验收服";
            role.Level = 999;
            role.SceneId = 10103;
            role.Figure = new FigureProto { name = "旧名字", career = 1, turn = 20, level = 999 };
            role.MarkBaseInfoReady();
            var entries = new List<KeyValuePair<int, int>>
            {
                Pair(6, 5), Pair(7, 8), Pair(9, 50), Pair(12, 50), Pair(8, 0),
                Pair(17, 1), Pair(18, 1), Pair(19, 1), Pair(21, 1), Pair(201, 1), Pair(202, 1),
                Pair(1, 1), Pair(2, 1), Pair(3, 1), Pair(10, 1), Pair(14, 1), Pair(20, 1),
                Pair(22, 1), Pair(25, 1), Pair(5, 1), Pair(26, 1),
            };
            SettingModel.Reset();
            SettingModel.Apply10202(SettingModel.TYPE_SYS_SETTING, entries);
        }

        private static KeyValuePair<int, int> Pair(int key, int value) => new KeyValuePair<int, int>(key, value);

        private static async Task<bool> ClickToggleAndAck(SettingShieldItem item, SettingView view, List<byte[]> frames,
            Camera camera, GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            Image target = item.check_img_1 != null && item.check_img_1.gameObject.activeInHierarchy ? item.check_img_1 : item.check_img;
            return await ClickSettingAndAck(target, frames, camera, raycaster, eventSystem);
        }

        private static async Task<bool> ClickSettingAndAck(Component target, List<byte[]> frames,
            Camera camera, GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            int start = frames.Count;
            if (!Click(target, camera, raycaster, eventSystem)) return false;
            if (!await WaitCommand(frames, Proto.SETTING_WRITE, 2d, start)) return false;
            FeedSettingSuccess();
            await Task.Delay(50);
            return true;
        }

        private static async Task<bool> ConfirmAndAckSetting(Component target, int command, List<byte[]> frames,
            Camera camera, GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            int start = frames.Count;
            if (!Click(target, camera, raycaster, eventSystem)) return false;
            AlertTypeTwoBind alert = await WaitActive<AlertTypeTwoBind>(3d);
            await Task.Delay(100);
            Canvas.ForceUpdateCanvases();
            if (alert == null || !Click(alert._ok_btn, camera, raycaster, eventSystem)) return false;
            if (!await WaitCommand(frames, command, 2d, start)) return false;
            if (command == Proto.SETTING_WRITE) FeedSettingSuccess();
            else Feed(typeof(SettingController).GetMethod("On10210", PrivateInstance), SettingController.Instance,
                new CliVerify.Pkt().I(1).Bytes());
            return true;
        }

        private static async Task<bool> ConfirmAndCancel(Component target, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (!Click(target, camera, raycaster, eventSystem)) return false;
            AlertTypeTwoBind alert = await WaitActive<AlertTypeTwoBind>(3d);
            await Task.Delay(100);
            Canvas.ForceUpdateCanvases();
            if (alert == null || !Click(alert._cancel_btn, camera, raycaster, eventSystem)) return false;
            await Task.Delay(50);
            return !alert.gameObject.activeInHierarchy;
        }

        private static async Task<bool> OpenAndCloseDocument(Component target, int type, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (!Click(target, camera, raycaster, eventSystem)) return false;
            LoginUserAgreementView document = await WaitActive<LoginUserAgreementView>(5d);
            await Task.Delay(100);
            Canvas.ForceUpdateCanvases();
            bool marker = document != null && document._lb_content != null
                && (type == LoginUserAgreementView.TypePrivacy
                    ? document._img_privacy.gameObject.activeInHierarchy
                    : document._img_xieyi.gameObject.activeInHierarchy);
            bool closed = marker && Click(document._img_close, camera, raycaster, eventSystem);
            await Task.Delay(50);
            return closed && !document.gameObject.activeInHierarchy;
        }

        private static bool PointerTo(Image track, float fraction, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (track == null || !track.gameObject.activeInHierarchy) return false;
            Canvas.ForceUpdateCanvases();
            Rect rect = track.rectTransform.rect;
            Vector3 local = new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, fraction), rect.center.y, 0f);
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, track.rectTransform.TransformPoint(local)),
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            RaycastResult hit = hits.FirstOrDefault(result => result.gameObject == track.gameObject
                || result.gameObject.transform.IsChildOf(track.transform.parent));
            if (hit.gameObject == null) return false;
            ExecuteEvents.ExecuteHierarchy<IPointerDownHandler>(hit.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            return true;
        }

        private static bool Click(Component target, Camera camera, GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            Graphic surface = target as Graphic ?? target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
            if (surface == null || !surface.enabled || !surface.raycastTarget) return false;
            Canvas.ForceUpdateCanvases();
            RectTransform rect = surface.rectTransform;
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject != surface.gameObject && !hit.gameObject.transform.IsChildOf(target.transform)) continue;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(hit.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return true;
            }
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Debug.LogError("CLIVERIFY setting-full raycast miss " + target.name
                + " screen=" + pointer.position + " rect=" + rect.rect + " anchored=" + rect.anchoredPosition
                + " world=" + string.Join("|", corners.Select(value => value.ToString()))
                + " hits=" + string.Join(",", hits.Select(hit => hit.gameObject.name)));
            return false;
        }

        private static SettingShieldItem[] ActiveItems(ScrollRect list)
            => list == null ? Array.Empty<SettingShieldItem>() : list.GetComponentsInChildren<SettingShieldItem>(false);

        private static async Task<T> WaitActive<T>(double timeoutSeconds) where T : Component
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                foreach (T value in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (value != null && value.gameObject.activeInHierarchy) return value;
                await Task.Delay(40);
            }
            return null;
        }

        private static async Task<bool> WaitCommand(List<byte[]> frames, int command, double timeoutSeconds, int start = 0)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                for (int i = Math.Max(0, start); i < frames.Count; i++) if (Command(frames[i]) == command) return true;
                await Task.Delay(40);
            }
            return false;
        }

        private static int Command(byte[] frame) => frame != null && frame.Length >= 6 ? (frame[4] << 8) | frame[5] : -1;

        private static void FeedSettingSuccess()
            => Feed(typeof(SettingController).GetMethod("On10203", PrivateInstance), SettingController.Instance,
                new CliVerify.Pkt().I(1).Bytes());

        private static void Feed(MethodInfo method, object target, byte[] bytes)
        {
            if (method == null) throw new MissingMethodException(target.GetType().Name);
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(target, new object[] { reader });
            if (reader.Remaining != 0) throw new InvalidDataException(method.Name + " remaining=" + reader.Remaining);
        }
    }
}
