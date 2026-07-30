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
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.GameNotice;
using Shenxiao.Module.Core.Login;
using TMPro;
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
        private const string AgreementPrefab = "Assets/Prefabs/UI/Login/LoginUserAgreementView.prefab";
        private const string ServerEnterCreator = "Assets/Editor/UiCreator/Login/ServerEnterCreator.cs";
        private const string AgreementCreator = "Assets/Editor/UiCreator/Login/LoginUserAgreementCreator.cs";
        private const string Account = "__cliverify_login_notice__";
        private const string Platform = "jzy_case";
        private const long RoleId = 91521001;

        public static async Task<int> Run()
        {
            GameObject canvasGo = null;
            GameObject cameraGo = null;
            GameObject eventSystemGo = null;
            GameObject noticeRoot = null;
            GameObject serverEnterRoot = null;
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
                bool agreementPrefabOk = VerifyAgreementPrefab();
                bool prefabOwnedOk = VerifyPrefabOwnedLayout();

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

                GameObject serverEnterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ServerEnterPrefab);
                serverEnterRoot = PrefabUtility.InstantiatePrefab(serverEnterPrefab, canvasGo.transform) as GameObject;
                ServerEnterView serverEnterView = serverEnterRoot != null
                    ? serverEnterRoot.GetComponent<ServerEnterView>()
                    : null;
                bool agreementLinkPointerOk = VerifyAgreementLinksByPointer(serverEnterView,
                    canvas, camera, raycaster, eventSystem, out string agreementPointerDetail);
                if (serverEnterRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(serverEnterRoot);
                    serverEnterRoot = null;
                }

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
                    && sessionRule && popupRule && protocolOk && serverEnterOk && agreementPrefabOk
                    && prefabOwnedOk && agreementLinkPointerOk && uiOk;
                Debug.Log("CLIVERIFY login-notice VERDICT model=" + modelOk
                    + " detail=[" + initialModelDetail + "]"
                    + " invalidPreserves=" + invalidRejected + " redAggregate=" + firstReadKeepsAggregate
                    + "/" + allReadClearsAggregate + " sessionRule=" + sessionRule
                    + " popupRule=" + popupRule + " protocol10207=" + protocolOk
                    + " serverEnter=" + serverEnterOk + " agreementPrefab=" + agreementPrefabOk
                    + " prefabOwned=" + prefabOwnedOk
                    + " agreementPointer=" + agreementLinkPointerOk + "[" + agreementPointerDetail + "]"
                    + " clickSurface=" + clickSurfaceOk
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
                if (serverEnterRoot != null) UnityEngine.Object.DestroyImmediate(serverEnterRoot);
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

        private static bool VerifyAgreementLinksByPointer(ServerEnterView view, Canvas canvas,
            Camera camera, GraphicRaycaster raycaster, EventSystem eventSystem, out string detail)
        {
            detail = "view=null";
            if (view == null || view.agreementContent == null || view.agreementLinkHandler == null) return false;

            var clicked = new List<string>();
            view.gameObject.SetActive(true);
            view.Show();
            view.agreementLinkHandler.SetHandler(clicked.Add);
            canvas.enabled = false;
            canvas.enabled = true;
            Canvas.ForceUpdateCanvases();
            view.agreementContent.ForceMeshUpdate();

            var descriptor = new RenderTextureDescriptor(Mathf.Max(1, Screen.width),
                Mathf.Max(1, Screen.height), RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = 1,
            };
            RenderTexture warmup = new RenderTexture(descriptor);
            if (!warmup.Create())
            {
                detail = "warmup-create-failed";
                return false;
            }
            camera.targetTexture = warmup;
            camera.Render();
            camera.targetTexture = null;
            warmup.Release();
            UnityEngine.Object.DestroyImmediate(warmup);

            bool agreement = ClickTmpLink("agreement", view.agreementContent, camera, raycaster, eventSystem,
                out string agreementDetail);
            bool privacy = ClickTmpLink("privacy", view.agreementContent, camera, raycaster, eventSystem,
                out string privacyDetail);
            bool callbacks = clicked.Count == 2 && clicked[0] == "agreement" && clicked[1] == "privacy";
            detail = "ray=" + agreement + "/" + privacy + ",ids=" + string.Join(",", clicked)
                + ",a=" + agreementDetail + ",p=" + privacyDetail;
            view.Hide();
            return agreement && privacy && callbacks;
        }

        private static bool ClickTmpLink(string linkId, TMP_Text text, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem, out string detail)
        {
            detail = "link-missing";
            int linkIndex = -1;
            for (int i = 0; i < text.textInfo.linkCount; i++)
            {
                if (text.textInfo.linkInfo[i].GetLinkID() == linkId)
                {
                    linkIndex = i;
                    break;
                }
            }
            if (linkIndex < 0) return false;

            TMP_LinkInfo link = text.textInfo.linkInfo[linkIndex];
            int characterIndex = link.linkTextfirstCharacterIndex;
            int end = characterIndex + link.linkTextLength;
            while (characterIndex < end && !text.textInfo.characterInfo[characterIndex].isVisible)
                characterIndex++;
            if (characterIndex >= end)
            {
                detail = "no-visible-char";
                return false;
            }

            TMP_CharacterInfo character = text.textInfo.characterInfo[characterIndex];
            Vector3 localCenter = (character.bottomLeft + character.topRight) * 0.5f;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera,
                text.transform.TransformPoint(localCenter));
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = screenPoint,
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            int intersecting = TMP_TextUtilities.FindIntersectingLink(text, screenPoint, camera);
            var hitNames = new List<string>();
            for (int i = 0; i < hits.Count; i++) hitNames.Add(hits[i].gameObject.name);
            detail = "screen=" + screenPoint + ",tmp=" + intersecting + ",hits=" + string.Join("/", hitNames);
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].gameObject != text.gameObject) continue;
                pointer.pointerPressRaycast = hits[i];
                pointer.pointerCurrentRaycast = hits[i];
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(hits[i].gameObject,
                    pointer, ExecuteEvents.pointerClickHandler);
                return true;
            }
            return false;
        }

        private static bool VerifyServerEnterPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ServerEnterPrefab);
            ServerEnterView view = prefab != null ? prefab.GetComponent<ServerEnterView>() : null;
            if (view == null) return false;

            Transform root = prefab.transform;
            Transform enterLabel = root.Find("EnterBtn/Label");
            Transform title = root.Find("AgreementAlert/Frame/Title");
            Transform alert = root.Find("AgreementAlert");
            Transform textRow = root.Find("ServerBtn/TextRow");
            HorizontalLayoutGroup textLayout = textRow != null
                ? textRow.GetComponent<HorizontalLayoutGroup>()
                : null;
            bool functionalBindingsOk = IsBound(view.serverBtn, root, "ServerBtn")
                && IsBound(view.serverStateIcon, root, "ServerBtn/ServerStateIcon")
                && IsBound(view.serverNameLabel, root, "ServerBtn/TextRow/ServerNameLabel")
                && IsBound(view.tipLabel, root, "ServerBtn/TextRow/TipLabel")
                && IsBound(view.enterBtn, root, "EnterBtn")
                && IsBound(view.enterBtnLabel, root, "EnterBtn/Label")
                && IsBound(view.noticeBtn, root, "NoticeBtn")
                && view.noticeBtnLabel == null && root.Find("NoticeBtn/Label") == null
                && IsBound(view.agreementCheckBg, root, "AgreementCheckBg")
                && IsBound(view.agreementCheckMark, root, "AgreementCheckBg/AgreementCheckMark")
                && IsBound(view.agreementLabel, root, "AgreementLabel")
                && IsBoundObject(view.agreementAlert, root, "AgreementAlert")
                && IsBound(view.agreementContent, root, "AgreementAlert/Frame/Content")
                && view.agreementLinkHandler != null
                && view.agreementLinkHandler.transform == root.Find("AgreementAlert/Frame/Content")
                && IsBound(view.agreementCancelBtn, root, "AgreementAlert/Frame/CancelBtn")
                && IsBound(view.agreementOkBtn, root, "AgreementAlert/Frame/OkBtn");

            bool adaptiveTextLayoutOk = root.Find("ServerBtn").GetComponent<HorizontalLayoutGroup>() == null
                && textLayout != null
                && textLayout.childControlWidth && textLayout.childControlHeight
                && !textLayout.childForceExpandWidth && !textLayout.childForceExpandHeight
                && view.serverNameLabel.enableAutoSizing && view.tipLabel.enableAutoSizing
                && view.serverNameLabel.textWrappingMode == TextWrappingModes.NoWrap
                && view.tipLabel.textWrappingMode == TextWrappingModes.NoWrap;

            string agreementRichText = view.agreementContent != null ? view.agreementContent.text : string.Empty;
            bool linksOk = agreementRichText.Contains("<link=\"agreement\">")
                && agreementRichText.Contains("<link=\"privacy\">")
                && agreementRichText.Contains("《用户协议》")
                && agreementRichText.Contains("《隐私保护指引》");

            return functionalBindingsOk
                && adaptiveTextLayoutOk && linksOk
                && HasSprite(root, "Bg")
                && HasSprite(root, "Logo")
                && HasSprite(root, "ServerBtn")
                && HasSprite(root, "NoticeBtn")
                && HasSprite(root, "AgreementAlert/Frame")
                && HasSprite(root, "AgreementAlert/Frame/TitleImg")
                && alert != null && !alert.gameObject.activeSelf
                && enterLabel != null && !enterLabel.gameObject.activeSelf
                && title != null && !title.gameObject.activeSelf;
        }

        private static bool VerifyAgreementPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AgreementPrefab);
            LoginUserAgreementView view = prefab != null ? prefab.GetComponent<LoginUserAgreementView>() : null;
            if (view == null) return false;

            Transform root = prefab.transform;
            Transform contentTransform = root.Find("_img_bg/_panel_content/_lb_content");
            ContentSizeFitter fitter = contentTransform != null
                ? contentTransform.GetComponent<ContentSizeFitter>()
                : null;
            bool bindingsOk = IsBound(view.closeMask, root, "CloseMask")
                && IsBound(view._img_bg, root, "_img_bg")
                && IsBound(view._img_close, root, "_img_bg/_img_close")
                && IsBound(view._panel_content, root, "_img_bg/_panel_content")
                && IsBound(view._lb_content, root, "_img_bg/_panel_content/_lb_content")
                && IsBound(view._img_xieyi, root, "_img_bg/_img_xieyi")
                && IsBound(view._img_privacy, root, "_img_bg/_img_privacy");
            bool scrollOk = view._panel_content != null && view._lb_content != null
                && view._panel_content.viewport == view._panel_content.transform
                && view._panel_content.content == view._lb_content.rectTransform
                && !view._panel_content.horizontal && view._panel_content.vertical
                && fitter != null && fitter.verticalFit == ContentSizeFitter.FitMode.PreferredSize;
            TextAsset baseConfig = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/GameRes/resource/config/client/configagreement2.json");
            TextAsset channelConfig = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/GameRes/resource/config/client/configagreement2_shenhai.json");
            bool configOk = baseConfig != null && channelConfig != null;
            if (configOk)
            {
                JObject json = JObject.Parse(channelConfig.text);
                configOk = (string)json["agreenment"]?["title"] == "用户协议"
                    && (string)json["privacy"]?["title"] == "隐私保护指引"
                    && json["agreenment"]?["content"] is JArray agreementLines
                    && agreementLines.Count > 0
                    && json["privacy"]?["content"] is JArray privacyLines
                    && privacyLines.Count > 0;
            }

            return bindingsOk && scrollOk && configOk
                && HasSprite(root, "_img_bg")
                && HasSprite(root, "_img_bg/_img_xieyi")
                && HasSprite(root, "_img_bg/_img_privacy");
        }

        private static bool VerifyPrefabOwnedLayout()
        {
            return AssetDatabase.LoadAssetAtPath<MonoScript>(ServerEnterCreator) == null
                && AssetDatabase.LoadAssetAtPath<MonoScript>(AgreementCreator) == null;
        }

        private static bool IsBound(Component component, Transform root, string path)
        {
            Transform target = root.Find(path);
            return component != null && target != null && component.transform == target;
        }

        private static bool IsBoundObject(GameObject gameObject, Transform root, string path)
        {
            Transform target = root.Find(path);
            return gameObject != null && target != null && gameObject.transform == target;
        }

        private static bool HasSprite(Transform root, string path)
        {
            Transform target = root.Find(path);
            Image image = target != null ? target.GetComponent<Image>() : null;
            return image != null && image.sprite != null;
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
