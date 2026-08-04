using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Mail;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 用户截图专项回归：真实 HudActivity/HudNotification prefab 上验证 612xx 不进入活动网格、
    /// 邮件 GAME_START 主动查询与 ui_notice_3 展示/点击，以及全部可见通知的同相 ±20° 摇摆。
    /// </summary>
    public static class MainUINotificationCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;
        private const string ActivityPrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudActivity.prefab";
        private const string NotificationPrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudNotification.prefab";

        public static Task<int> Run() => Task.FromResult(RunCore());

        private static int RunCore()
        {
            bool fallbackBefore = Shenxiao.Framework.Res.ResManager.EditorPreferFallback;
            bool unreadBefore = MailModel.Instance.HasUnread;
            bool mailWasInitialized = MailController.Instance.IsInitialized;
            Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
            CliVerify.Stage stage = null;
            GameObject activityInstance = null;
            GameObject notificationInstance = null;
            GameObject eventSystemGo = null;
            MainUINotificationView notificationView = null;
            bool pass = true;

            try
            {
                stage = CliVerify.Stage.Create();

                bool activityOk = VerifyActivitySuppression(stage, out activityInstance);
                Check("activity-612-suppression", activityOk, ref pass);

                bool mailStartupOk = VerifyMailStartup();
                Check("mail-startup-19008-19001", mailStartupOk, ref pass);

                bool notificationOk = VerifyNotificationPrefab(
                    stage,
                    out notificationInstance,
                    out eventSystemGo,
                    out notificationView);
                Check("notification-prefab-mail-click-swing", notificationOk, ref pass);

                Debug.Log("CLIVERIFY mainui-notification VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY mainui-notification EXCEPTION " + exception);
                return 3;
            }
            finally
            {
                MainUIRouter.Unregister("email");
                if (notificationView != null && notificationView.IsShown) notificationView.Hide();
                if (notificationInstance != null) UnityEngine.Object.DestroyImmediate(notificationInstance);
                if (activityInstance != null) UnityEngine.Object.DestroyImmediate(activityInstance);
                if (eventSystemGo != null) UnityEngine.Object.DestroyImmediate(eventSystemGo);
                if (stage != null) stage.Dispose();
                if (!mailWasInitialized && MailController.Instance.IsInitialized) MailController.Instance.Dispose();
                MailModel.Instance.HasUnread = unreadBefore;
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = fallbackBefore;
            }
        }

        private static bool VerifyActivitySuppression(CliVerify.Stage stage, out GameObject instance)
        {
            instance = null;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActivityPrefabPath);
            if (prefab == null || stage == null) return false;
            instance = PrefabUtility.InstantiatePrefab(prefab, stage.CanvasRoot) as GameObject;
            MainUIActivityView view = instance != null ? instance.GetComponentInChildren<MainUIActivityView>(true) : null;
            if (view == null) return false;

            FieldInfo registryField = typeof(ActivityIconManager).GetField("_iconInfoByType", InstancePrivate);
            MethodInfo collect = typeof(MainUIActivityView).GetMethod("CollectOwnedIconGroups", InstancePrivate);
            MethodInfo isLimitShop = typeof(MainUIActivityView).GetMethod("IsLimitLevelShopIcon", StaticPrivate);
            IDictionary registry = registryField?.GetValue(ActivityIconManager.Instance) as IDictionary;
            if (registry == null || collect == null || isLimitShop == null) return false;

            var backup = new List<DictionaryEntry>();
            foreach (DictionaryEntry entry in registry) backup.Add(entry);
            try
            {
                registry.Clear();
                registry["61207"] = IconInfo("61207", ActivityIconManager.LocationType.ActivityOther);
                registry["419"] = IconInfo("419", ActivityIconManager.LocationType.ActivityOther);

                object groups = collect.Invoke(view, null);
                IList other = groups?.GetType().GetField("Other", BindingFlags.Public | BindingFlags.Instance)?.GetValue(groups) as IList;
                bool prefixOk = (bool)isLimitShop.Invoke(null, new object[] { "612" })
                    && (bool)isLimitShop.Invoke(null, new object[] { "61207" })
                    && !(bool)isLimitShop.Invoke(null, new object[] { "62107" })
                    && !(bool)isLimitShop.Invoke(null, new object[] { null });
                bool groupingOk = other != null && other.Count == 1 && Equals(other[0], "419") && !other.Contains("61207");
                Debug.Log("CLIVERIFY mainui-notification activity prefix=" + prefixOk
                    + " otherCount=" + (other?.Count ?? -1) + " grouping=" + groupingOk);
                return prefixOk && groupingOk;
            }
            finally
            {
                registry.Clear();
                foreach (DictionaryEntry entry in backup) registry[entry.Key] = entry.Value;
            }
        }

        private static ActivityIconManager.IconInfo IconInfo(string iconType, int location)
        {
            return new ActivityIconManager.IconInfo
            {
                IconType = iconType,
                Data = new MainUIConfigs.FunctionIconCfg { IconType = iconType, LocationType = location },
            };
        }

        private static bool VerifyMailStartup()
        {
            MailController controller = MailController.Instance;
            controller.Init();

            IDictionary eventMap = typeof(EventDispatcher).GetField("_handlers", StaticPrivate)?.GetValue(null) as IDictionary;
            IList startHandlers = eventMap?[GlobalEvent.EVT_GAME_START] as IList;
            IDictionary protocols = typeof(NetManager).GetField("_handlers", StaticPrivate)?.GetValue(null) as IDictionary;
            bool subscribed = CountHandler(startHandlers, controller, nameof(MailController.RequestStartup)) == 1;
            bool registered = protocols != null && protocols.Contains(Proto.MAIL_UNREAD) && protocols.Contains(Proto.MAIL_LIST);

            var logs = new List<string>();
            Application.LogCallback callback = (message, stack, type) => logs.Add(message);
            Application.logMessageReceived += callback;
            try
            {
                controller.RequestStartup();
            }
            finally
            {
                Application.logMessageReceived -= callback;
            }
            bool sentUnread = logs.Exists(x => x.Contains("proto=19008"));
            bool sentList = logs.Exists(x => x.Contains("proto=19001"));

            MethodInfo onUnread = typeof(MailController).GetMethod("On19008", InstancePrivate);
            bool packetOk = onUnread != null
                && Feed(onUnread, controller, new CliVerify.Pkt().C(1).Bytes())
                && MailModel.Instance.HasUnread;
            Debug.Log("CLIVERIFY mainui-notification mail subscribed=" + subscribed
                + " registered=" + registered + " sentUnread=" + sentUnread
                + " sentList=" + sentList + " packet=" + packetOk);
            return subscribed && registered && sentUnread && sentList && packetOk;
        }

        private static bool VerifyNotificationPrefab(
            CliVerify.Stage stage,
            out GameObject instance,
            out GameObject eventSystemGo,
            out MainUINotificationView view)
        {
            instance = null;
            eventSystemGo = null;
            view = null;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NotificationPrefabPath);
            if (prefab == null || stage == null) return false;
            instance = PrefabUtility.InstantiatePrefab(prefab, stage.CanvasRoot) as GameObject;
            view = instance != null ? instance.GetComponentInChildren<MainUINotificationView>(true) : null;
            if (view == null) return false;

            MailModel.Instance.HasUnread = true;
            int emailClicks = 0;
            MainUIRouter.Register("email", () => emailClicks++);
            view.Show();
            MethodInfo refresh = typeof(MainUINotificationView).GetMethod("RefreshNotifications", InstancePrivate);
            MethodInfo evaluate = typeof(MainUINotificationView).GetMethod("EvaluateSwingAngle", StaticPrivate);
            MethodInfo apply = typeof(MainUINotificationView).GetMethod("ApplyNotificationSwing", InstancePrivate);
            MethodInfo reset = typeof(MainUINotificationView).GetMethod("ResetNotificationSwing", InstancePrivate);
            FieldInfo itemsField = typeof(MainUINotificationView).GetField("_items", InstancePrivate);
            if (refresh == null || evaluate == null || apply == null || reset == null || itemsField == null) return false;
            refresh.Invoke(view, null);
            Canvas.ForceUpdateCanvases();

            IList items = itemsField.GetValue(view) as IList;
            MainUINotificationItem mailItem = FindMailItem(items);
            bool mailVisible = mailItem != null && mailItem.gameObject.activeSelf && mailItem.Icon != null;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystemGo = new GameObject("MainUINotificationCase_EventSystem", typeof(EventSystem));
                eventSystem = eventSystemGo.GetComponent<EventSystem>();
            }
            bool clickOk = mailVisible && ClickVisibleEntry(mailItem.Icon, stage, eventSystem) && emailClicks == 1;

            float a0 = (float)evaluate.Invoke(null, new object[] { 0f });
            float a1 = (float)evaluate.Invoke(null, new object[] { 1f });
            float a2 = (float)evaluate.Invoke(null, new object[] { 2f });
            float a3 = (float)evaluate.Invoke(null, new object[] { 3f });
            bool curveOk = Approximately(a0, 0f) && Approximately(a1, 20f)
                && Approximately(a2, -20f) && Approximately(a3, 20f);

            apply.Invoke(view, new object[] { 20f });
            bool synchronized = VisibleItemsHaveAngle(items, 20f, out int visibleCount);
            stage.ForceCjkFont();
            string shot = stage.Capture("Temp/mainui_notification_swing.png");
            reset.Invoke(view, null);
            bool resetOk = VisibleItemsHaveAngle(items, 0f, out _);

            Debug.Log("CLIVERIFY mainui-notification prefab mailVisible=" + mailVisible
                + " click=" + clickOk + " curve=" + curveOk + " sync=" + synchronized
                + " reset=" + resetOk + " visibleCount=" + visibleCount + " shot=" + shot);
            return mailVisible && clickOk && curveOk && synchronized && resetOk && visibleCount > 0;
        }

        private static MainUINotificationItem FindMailItem(IList items)
        {
            if (items == null) return null;
            FieldInfo iconPath = typeof(MainUINotificationItem).GetField("_iconPath", InstancePrivate);
            foreach (object entry in items)
            {
                if (entry is MainUINotificationItem item
                    && (iconPath?.GetValue(item) as string)?.EndsWith("ui_notice_3.png", StringComparison.Ordinal) == true)
                {
                    return item;
                }
            }
            return null;
        }

        private static bool VisibleItemsHaveAngle(IList items, float expected, out int count)
        {
            count = 0;
            if (items == null) return false;
            foreach (object entry in items)
            {
                if (!(entry is MainUINotificationItem item) || !item.gameObject.activeSelf) continue;
                count++;
                float angle = Mathf.DeltaAngle(0f, item.transform.localEulerAngles.z);
                if (!Approximately(angle, expected)) return false;
            }
            return count > 0;
        }

        private static bool ClickVisibleEntry(Image surface, CliVerify.Stage stage, EventSystem eventSystem)
        {
            if (surface == null || stage == null || eventSystem == null) return false;
            Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
            GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
            Button button = surface.GetComponent<Button>();
            if (!surface.enabled || !surface.raycastTarget || button == null || canvas == null || raycaster == null) return false;

            Canvas.ForceUpdateCanvases();
            canvas.worldCamera.Render();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(
                canvas.worldCamera,
                surface.rectTransform.TransformPoint(surface.rectTransform.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = point,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                Transform hit = hits[i].gameObject.transform;
                if (hit != surface.transform && !hit.IsChildOf(surface.transform)) continue;
                ExecuteEvents.ExecuteHierarchy(hits[i].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return true;
            }
            return false;
        }

        private static int CountHandler(IList handlers, object target, string methodName)
        {
            if (handlers == null) return 0;
            int count = 0;
            foreach (object item in handlers)
            {
                if (item is Delegate handler && handler.Target == target && handler.Method.Name == methodName) count++;
            }
            return count;
        }

        private static bool Feed(MethodInfo method, object target, byte[] payload)
        {
            var reader = new NetReader(payload, 0, payload.Length);
            method.Invoke(target, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool Approximately(float actual, float expected) => Mathf.Abs(actual - expected) <= 0.05f;

        private static void Check(string tag, bool ok, ref bool pass)
        {
            Debug.Log("CLIVERIFY mainui-notification " + tag + " ok=" + ok);
            if (!ok) pass = false;
        }
    }
}
