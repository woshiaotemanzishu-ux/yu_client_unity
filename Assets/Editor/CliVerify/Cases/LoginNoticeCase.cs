using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Prefs;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.GameNotice;
using Shenxiao.Module.Core.Login;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>10207、运营公告筛选/红点规则及公告标题真实点击链专项。</summary>
    public static class LoginNoticeCase
    {
        private const string NoticePrefab = "Assets/Prefabs/UI/GameNotice/GameNoticeModule.prefab";
        private const string ServerEnterPrefab = "Assets/Prefabs/UI/Login/ServerEnterView.prefab";
        private const string ServerEnterBackground = "Assets/GameRes/resource/game/login/other/组 1.png";
        private const string ServerEnterLogo = "Assets/GameRes/resource/game/login/other/logo.png";
        private const string ServerEnterServerBar = "Assets/GameRes/resource/game/login/other/ui_Login_18.png";
        private const string ServerEnterAlertFrame = "Assets/GameRes/resource/game/login/other/bg_03.png";
        private const string ServerEnterAlertTitle = "Assets/GameRes/resource/game/login/other/uildzdz_008d.png";
        private const string Account = "__cliverify_login_notice__";
        private const string Platform = "jzy_case";
        private const long RoleId = 91521001;

        public static async Task<int> Run()
        {
            GameObject canvasGo = null;
            GameObject cameraGo = null;
            GameObject eventSystemGo = null;
            GameObject noticeRoot = null;
            GameNoticeView noticeView = null;
            RenderTexture warmupTexture = null;
            try
            {
                CleanupPrefs();
                LoginNoticeModel model = LoginNoticeModel.Instance;
                ResetModelSnapshot(model);
                model.BeginSession(Account, Platform);
                model.BeginRoleSession(RoleId);
                long now = Shenxiao.Framework.Util.TimeUtil.NowSec();
                JObject root = BuildSnapshot(now);
                bool parsed = model.TryReplace(root, "r521", "test", Platform, out string error);
                List<LoginNoticeDisplayInfo> login = model.GetLoginNotices();
                List<LoginNoticeDisplayInfo> inside = model.GetInsideNotices();
                bool modelOk = parsed && error == null && model.Loaded && model.Version == "r521"
                    && login.Count == 2 && login[0].Notice.Id == "a" && login[1].Notice.Id == "e"
                    && inside.Count == 2 && inside[0].Notice.Id == "a" && inside[1].Notice.Id == "b"
                    && inside[0].IsUnread && inside[1].IsUnread && model.HasUnreadInside;
                string initialModelDetail = "parsed=" + parsed + "/error=" + error + "/loaded=" + model.Loaded
                    + "/version=" + model.Version + "/login=" + JoinIds(login)
                    + "/inside=" + JoinInside(inside) + "/aggregate=" + model.HasUnreadInside;

                bool invalidRejected = !model.TryReplace(new JObject(), "bad", "test", Platform, out _)
                    && model.Version == "r521" && model.GetInsideNotices().Count == 2;

                model.MarkInsideRead(inside[0].ReadKey);
                bool firstReadKeepsAggregate = model.HasUnreadInside;
                model.MarkInsideRead(inside[1].ReadKey);
                bool allReadClearsAggregate = !model.HasUnreadInside;
                model.BeginRoleSession(RoleId);
                inside = model.GetInsideNotices();
                bool sessionRule = model.HasUnreadInside && inside[0].IsUnread && !inside[1].IsUnread;
                bool popupRule = model.ShouldAutoOpenLogin(true) && !model.ShouldAutoOpenLogin(true);

                bool protocolOk = VerifyProtocol(model);
                bool serverEnterOk = VerifyServerEnterPrefab();

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NoticePrefab);
                if (prefab == null) throw new InvalidOperationException("公告 prefab 不存在: " + NoticePrefab);

                canvasGo = new GameObject("LoginNoticeCase_Canvas", typeof(RectTransform),
                    typeof(Canvas), typeof(GraphicRaycaster));
                RectTransform canvasRt = (RectTransform)canvasGo.transform;
                canvasRt.sizeDelta = new Vector2(720f, 1280f);
                Canvas canvas = canvasGo.GetComponent<Canvas>();
                cameraGo = new GameObject("LoginNoticeCase_Camera", typeof(Camera));
                Camera camera = cameraGo.GetComponent<Camera>();
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.orthographic = true;
                camera.orthographicSize = 640f;
                camera.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
                camera.aspect = 720f / 1280f;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>();
                raycaster.ignoreReversedGraphics = false;
                eventSystemGo = new GameObject("LoginNoticeCase_EventSystem", typeof(EventSystem));
                EventSystem eventSystem = eventSystemGo.GetComponent<EventSystem>();

                noticeRoot = PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform) as GameObject;
                noticeView = noticeRoot != null
                    ? noticeRoot.GetComponentInChildren<GameNoticeView>(true)
                    : null;
                if (noticeView == null) throw new InvalidOperationException("公告 prefab 缺 GameNoticeView");
                noticeRoot.SetActive(true);
                noticeView.gameObject.SetActive(true);
                noticeView.Show(GameNoticeMode.Inside);
                canvas.enabled = false;
                canvas.enabled = true;
                Canvas.ForceUpdateCanvases();
                var descriptor = new RenderTextureDescriptor(Mathf.Max(1, Screen.width),
                    Mathf.Max(1, Screen.height), RenderTextureFormat.ARGB32, 24)
                {
                    msaaSamples = 1,
                };
                warmupTexture = new RenderTexture(descriptor);
                if (!warmupTexture.Create()) throw new InvalidOperationException("公告点击验收RenderTexture创建失败");
                camera.targetTexture = warmupTexture;
                camera.Render();
                camera.targetTexture = null;
                camera.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
                camera.aspect = 720f / 1280f;

                Transform secondTitle = noticeView.transform.Find("NoticeTitle_1");
                if (secondTitle == null)
                    secondTitle = FindDescendant(noticeView.transform, "NoticeTitle_1");
                bool clickSurfaceOk = secondTitle != null;
                Image rootImage = secondTitle != null ? secondTitle.GetComponent<Image>() : null;
                if (rootImage == null || !rootImage.raycastTarget) clickSurfaceOk = false;
                if (secondTitle != null)
                {
                    foreach (Graphic graphic in secondTitle.GetComponentsInChildren<Graphic>(true))
                    {
                        if (graphic != rootImage && graphic.raycastTarget) clickSurfaceOk = false;
                    }
                }

                bool pointerOk = false;
                if (secondTitle is RectTransform titleRt)
                {
                    var pointer = new PointerEventData(eventSystem)
                    {
                        button = PointerEventData.InputButton.Left,
                        position = RectTransformUtility.WorldToScreenPoint(camera, titleRt.TransformPoint(titleRt.rect.center)),
                    };
                    var hits = new List<RaycastResult>();
                    raycaster.Raycast(pointer, hits);
                    for (int i = 0; i < hits.Count; i++)
                    {
                        if (hits[i].gameObject != secondTitle.gameObject) continue;
                        ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(hits[i].gameObject,
                            pointer, ExecuteEvents.pointerClickHandler);
                        pointerOk = true;
                        break;
                    }
                    if (!pointerOk)
                    {
                        var names = new List<string>();
                        for (int i = 0; i < hits.Count; i++) names.Add(hits[i].gameObject.name);
                        Debug.LogWarning("CLIVERIFY login-notice pointer miss screen=" + pointer.position
                            + " titleRect=" + titleRt.rect + " world=" + titleRt.position
                            + " hits=[" + string.Join(",", names) + "]");
                    }
                }
                bool uiOk = clickSurfaceOk && pointerOk && !model.HasUnreadInside;
                bool pass = modelOk && invalidRejected && firstReadKeepsAggregate && allReadClearsAggregate
                    && sessionRule && popupRule && protocolOk && serverEnterOk && uiOk;
                Debug.Log("CLIVERIFY login-notice VERDICT model=" + modelOk
                    + " detail=[" + initialModelDetail + "]"
                    + " invalidPreserves=" + invalidRejected + " redAggregate=" + firstReadKeepsAggregate
                    + "/" + allReadClearsAggregate + " sessionRule=" + sessionRule
                    + " popupRule=" + popupRule + " protocol10207=" + protocolOk
                    + " serverEnter=" + serverEnterOk + " clickSurface=" + clickSurfaceOk
                    + " pointerClick=" + pointerOk + " ui=" + uiOk + " pass=" + pass);
                await Task.CompletedTask;
                return pass ? 0 : 3;
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY login-notice EXCEPTION " + e);
                return 1;
            }
            finally
            {
                noticeView?.Hide();
                if (noticeRoot != null) UnityEngine.Object.DestroyImmediate(noticeRoot);
                if (eventSystemGo != null) UnityEngine.Object.DestroyImmediate(eventSystemGo);
                if (cameraGo != null) UnityEngine.Object.DestroyImmediate(cameraGo);
                if (warmupTexture != null)
                {
                    warmupTexture.Release();
                    UnityEngine.Object.DestroyImmediate(warmupTexture);
                }
                if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);
                ResetModelSnapshot(LoginNoticeModel.Instance);
                CleanupPrefs();
            }
        }

        private static bool VerifyProtocol(LoginNoticeModel model)
        {
            LoginController controller = LoginController.Instance;
            controller.Init();
            FieldInfo handlersField = typeof(NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
            IDictionary handlers = handlersField?.GetValue(null) as IDictionary;
            MethodInfo handler = typeof(LoginController).GetMethod("OnLoginNoticeRefresh",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (handlers == null || !handlers.Contains(Proto.LOGIN_NOTICE_REFRESH) || handler == null) return false;
            byte[] payload = { 0 };
            var reader = new NetReader(payload, 0, payload.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0 && model.HasPush && model.LastPushType == 0;
        }

        private static bool VerifyServerEnterPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ServerEnterPrefab);
            ServerEnterView view = prefab != null ? prefab.GetComponent<ServerEnterView>() : null;
            if (view == null || view.noticeBtn == null || view.noticeBtnLabel == null) return false;

            Transform root = prefab.transform;
            Transform enterLabel = root.Find("EnterBtn/Label");
            Transform title = root.Find("AgreementAlert/Frame/Title");
            Transform alert = root.Find("AgreementAlert");
            return IsImage(root, "Bg", ServerEnterBackground)
                && IsImage(root, "Logo", ServerEnterLogo)
                && IsImage(root, "ServerBtn", ServerEnterServerBar)
                && IsImage(root, "AgreementAlert/Frame", ServerEnterAlertFrame)
                && IsImage(root, "AgreementAlert/Frame/TitleImg", ServerEnterAlertTitle)
                && IsRect(root, "Logo", new Vector2(0.5f, 0.5f), new Vector2(0f, 430f),
                    new Vector2(506f, 166f))
                && IsRect(root, "ServerBtn", new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(470f, 58f))
                && IsRect(root, "EnterBtn", new Vector2(0.5f, 0.5f), new Vector2(0f, -200f),
                    new Vector2(378f, 140f))
                && IsRect(root, "AgreementCheckBg", new Vector2(0.5f, 0f), new Vector2(-186f, 60f),
                    new Vector2(34f, 36f))
                && IsRect(root, "AgreementLabel", new Vector2(0.5f, 0f), new Vector2(24f, 60f),
                    new Vector2(360f, 36f))
                && IsRect(root, "AgreementAlert/Frame", new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(720f, 434f))
                && alert != null && !alert.gameObject.activeSelf
                && enterLabel != null && !enterLabel.gameObject.activeSelf
                && title != null && !title.gameObject.activeSelf;
        }

        private static bool IsImage(Transform root, string path, string expectedAssetPath)
        {
            Transform target = root.Find(path);
            Image image = target != null ? target.GetComponent<Image>() : null;
            return image != null && image.sprite != null
                && AssetDatabase.GetAssetPath(image.sprite) == expectedAssetPath;
        }

        private static bool IsRect(Transform root, string path, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            RectTransform rect = root.Find(path) as RectTransform;
            return rect != null
                && Approximately(rect.anchorMin, anchor)
                && Approximately(rect.anchorMax, anchor)
                && Approximately(rect.anchoredPosition, anchoredPosition)
                && Approximately(rect.sizeDelta, sizeDelta);
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return (left - right).sqrMagnitude < 0.0001f;
        }

        private static JObject BuildSnapshot(long now)
        {
            JObject Content(string type, string text, int red) => new JObject
            {
                ["title"] = type,
                ["content"] = text,
                ["red_dot_rule"] = red,
            };
            JObject Notice(string title, long start, long end, string source, int newReg,
                int showRule, params JObject[] content) => new JObject
            {
                ["title"] = title,
                ["start_time"] = start,
                ["end_time"] = end,
                ["source"] = source,
                ["new_reg"] = newReg,
                ["show_rule"] = showRule,
                ["content"] = new JArray(content),
            };

            return new JObject
            {
                ["belong"] = new JObject { ["test"] = "a,b,c,d,e" },
                ["notice"] = new JObject
                {
                    ["a"] = Notice("公告A", now - 10, now + 3600, "jzy", 1, 3,
                        Content(LoginNoticeModel.LOGIN_CONTENT, "登录A", 0),
                        Content(LoginNoticeModel.INSIDE_CONTENT, "游戏内A", 3)),
                    ["b"] = Notice("公告B", now - 10, now + 3600, "", 0, 0,
                        Content(LoginNoticeModel.INSIDE_CONTENT, "游戏内B", 1)),
                    ["c"] = Notice("未开始", now + 1000, now + 3600, "", 0, 3,
                        Content(LoginNoticeModel.LOGIN_CONTENT, "未来", 0)),
                    ["d"] = Notice("其他平台", now - 10, now + 3600, "other", 0, 3,
                        Content(LoginNoticeModel.LOGIN_CONTENT, "其他", 0)),
                    ["e"] = Notice("公告E", now - 10, now + 3600, "", 0, 3,
                        Content(LoginNoticeModel.LOGIN_CONTENT, "登录E", 0)),
                },
            };
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                Transform found = FindDescendant(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static string JoinIds(List<LoginNoticeDisplayInfo> list)
        {
            var values = new List<string>();
            for (int i = 0; i < list.Count; i++) values.Add(list[i].Notice.Id);
            return string.Join(",", values);
        }

        private static string JoinInside(List<LoginNoticeDisplayInfo> list)
        {
            var values = new List<string>();
            for (int i = 0; i < list.Count; i++) values.Add(list[i].Notice.Id + ":" + list[i].IsUnread);
            return string.Join(",", values);
        }

        private static void CleanupPrefs()
        {
            string accountRoot = "login.notice.account." + Md5((Platform + "\n" + Account).Trim());
            string roleRoot = "login.notice.role." + RoleId;
            string indexKey = roleRoot + ".red.index";
            string[] redKeys = PrefsManager.GetString(indexKey, string.Empty).Split(',');
            for (int i = 0; i < redKeys.Length; i++)
            {
                if (redKeys[i].Length > 0) PrefsManager.Remove(roleRoot + ".red." + redKeys[i]);
            }
            PrefsManager.Remove(indexKey);
            PrefsManager.Remove(roleRoot + ".session.day");
            PrefsManager.Remove(accountRoot + ".popup.day");
            PrefsManager.Remove(accountRoot + ".popup.version");
        }

        private static void ResetModelSnapshot(LoginNoticeModel model)
        {
            model.TryReplace(new JObject
            {
                ["belong"] = new JObject(),
                ["notice"] = new JObject(),
            }, string.Empty, "__cliverify_empty__", string.Empty, out _);
        }

        private static string Md5(string value)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) builder.Append(hash[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
