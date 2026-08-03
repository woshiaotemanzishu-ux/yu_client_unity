using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dress;
using Shenxiao.Module.Core.Fashion;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Setting;
using Shenxiao.Editor.UiCreator.Dress;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 设置 → 更换头像 → 当前版时装/装扮/头像的真实 Prefab 点击验收。
    /// 同时覆盖外层四页签、装扮三子页、条目预览切换、穿戴/卸下即时刷新与首次/热开耗时。
    /// </summary>
    public static class SettingFashionCurrentCase
    {
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly string[] ResourceRoots =
        {
            "Assets/GameRes/resource/game",
        };

        public static async Task<int> Run()
        {
            bool fallbackBefore = ResManager.EditorPreferFallback;
            FieldInfo interceptField = typeof(DressController).GetField("s_outboundIntercept", StaticNonPublic);
            object oldIntercept = interceptField?.GetValue(null);
            bool controllerWasInitialized = DressController.Instance.IsInitialized;
            var frames = new List<byte[]>();
            CliVerify.Stage stage = null;
            GameObject eventSystemGo = null;
            HashSet<string> resourcesBefore = SnapshotResources();

            try
            {
                ResManager.EditorPreferFallback = true;
                FashionFlow.Reset();
                ResetSettingFlow();
                if (!controllerWasInitialized) DressController.Instance.Init();
                if (interceptField == null) throw new MissingFieldException("DressController.s_outboundIntercept");
                interceptField.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                bool resourcePreflight = DressAssetPreflight.EnsureAddressables();
                if (!resourcePreflight) throw new InvalidOperationException("DressAssetPreflight failed");
                await DressConfigs.EnsureLoaded();
                await GoodsModel.EnsureLoaded();
                int career = RoleModel.Instance.Career > 0 ? RoleModel.Instance.Career : 1;
                IReadOnlyList<DressConfigs.Row> headRows = DressConfigs.GetDisplayRows(DressView.HeadType);
                DressConfigs.Row activeHead = headRows.FirstOrDefault(row =>
                    !string.IsNullOrEmpty(DressConfigs.GetHeadIcon(row, career)));
                if (activeHead == null) throw new InvalidOperationException("头像配置没有可展示的 career icon");
                DressModel.Instance.Replace(DressView.HeadType, activeHead.Id,
                    new List<DressModel.Entry> { new DressModel.Entry(activeHead.Id, 1, 12345, 23456) });

                stage = CliVerify.Stage.Create();
                eventSystemGo = new GameObject("SettingFashionCurrent_EventSystem", typeof(EventSystem));
                EventSystem eventSystem = eventSystemGo.GetComponent<EventSystem>();
                Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
                GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
                Camera camera = canvas.worldCamera;

                SettingFlow.Open();
                SettingView setting = await WaitActive<SettingView>(8d);
                bool settingVisible = setting != null && setting.change_head_btn != null;
                stage.ForceCjkFont();
                string settingShot = stage.Capture("output/settings_fashion_current/setting.png");

                Stopwatch firstWatch = Stopwatch.StartNew();
                bool settingClick = settingVisible && Click(setting.change_head_btn, camera, raycaster, eventSystem);
                DressView dress = await WaitDressVisual(DressView.HeadType, 10d);
                firstWatch.Stop();

                BaseWindowSkinView window = FindActive<BaseWindowSkinView>();
                DressSubView sub = FindActive<DressSubView>();
                TabButtonTwoSkin[] outerTabs = window != null
                    ? window.GetComponentsInChildren<TabButtonTwoSkin>(false)
                    : Array.Empty<TabButtonTwoSkin>();
                string[] expectedOuter = { "时装", "发饰", "装扮", "套装" };
                string[] actualOuter = outerTabs.Select(TabText).ToArray();
                bool outerOk = window != null && window.CurrentIndex == 2 && outerTabs.Length == 4
                    && expectedOuter.SequenceEqual(actualOuter);
                bool headOk = dress != null && dress.SelectedType == DressView.HeadType && sub != null
                    && sub.Type == DressView.HeadType && sub.VisibleItemCount == headRows.Count
                    && sub.SelectedId == activeHead.Id && sub.model_img != null && sub.model_img.sprite != null;
                bool firstFast = firstWatch.ElapsedMilliseconds < 5000;

                stage.ForceCjkFont();
                string avatarShot = stage.Capture("output/settings_fashion_current/avatar.png");

                DressSkillItem[] skillItems = sub != null
                    ? sub.GetComponentsInChildren<DressSkillItem>(false)
                    : Array.Empty<DressSkillItem>();
                bool skillItemsOk = skillItems.Length == 3 && skillItems.All(item => item.IsVisualReady);
                bool skillClick = skillItemsOk && Click(skillItems[0].skill_img, camera, raycaster, eventSystem);
                SkillTipsViewBind skillTip = skillClick ? await WaitActive<SkillTipsViewBind>(5d) : null;
                bool skillTipOk = skillTip != null && skillTip.icon != null && skillTip.icon.sprite != null
                    && !string.IsNullOrWhiteSpace(skillTip.name_text?.text)
                    && !string.IsNullOrWhiteSpace(skillTip.des_text?.text);
                stage.ForceCjkFont();
                string skillTipShot = stage.Capture("output/settings_fashion_current/skill_tip.png");
                bool skillTipClose = skillTipOk && Click(skillTip._Image1, camera, raycaster, eventSystem);
                await Task.Delay(100);
                skillTipClose = skillTipClose && FindActive<SkillTipsViewBind>() == null;

                DressTab[] innerTabs = dress != null
                    ? dress.GetComponentsInChildren<DressTab>(false)
                    : Array.Empty<DressTab>();
                DressTab bubble = innerTabs.FirstOrDefault(tab => tab._lb != null && tab._lb.text == "气泡");
                DressTab photo = innerTabs.FirstOrDefault(tab => tab._lb != null && tab._lb.text == "相框");
                DressTab head = innerTabs.FirstOrDefault(tab => tab._lb != null && tab._lb.text == "头像");

                bool bubbleClick = Click(bubble?._Image1, camera, raycaster, eventSystem);
                bool bubbleOk = bubbleClick && await WaitType(DressView.BubbleType, 5d);
                stage.ForceCjkFont();
                string bubbleShot = stage.Capture("output/settings_fashion_current/bubble.png");

                bool photoClick = Click(photo?._Image1, camera, raycaster, eventSystem);
                bool photoOk = photoClick && await WaitType(DressView.PhotoType, 5d);
                stage.ForceCjkFont();
                string photoShot = stage.Capture("output/settings_fashion_current/photo.png");

                bool headClick = Click(head?._Image1, camera, raycaster, eventSystem);
                bool headBackOk = headClick && await WaitType(DressView.HeadType, 5d);
                sub = FindActive<DressSubView>();
                DressItem[] items = sub != null ? sub.GetComponentsInChildren<DressItem>(false) : Array.Empty<DressItem>();
                DressItem candidate = items.FirstOrDefault(item => item.DressId != sub.SelectedId);
                uint beforeSelected = sub?.SelectedId ?? 0;
                bool uniqueItemSurface = candidate != null && HasUniqueItemSurface(candidate);
                bool itemClick = candidate != null && Click(candidate.ClickSurface, camera, raycaster, eventSystem);
                await Task.Delay(250);
                bool itemChanged = itemClick && sub != null && sub.SelectedId != beforeSelected
                    && sub.model_img != null && sub.model_img.sprite != null && !string.IsNullOrWhiteSpace(sub.dress_name?.text);

                // 返回已激活头像，真实点击“卸下→使用”，分别喂权威回包并断言父页即时刷新。
                DressItem activeItem = items.FirstOrDefault(item => item.DressId == activeHead.Id);
                bool activeItemClick = activeItem != null && Click(activeItem.ClickSurface, camera, raycaster, eventSystem);
                await Task.Delay(150);
                frames.Clear();
                bool takeOffClick = activeItemClick && sub != null && Click(sub.use_btn, camera, raycaster, eventSystem);
                bool takeOffFrame = frames.Count == 1 && Command(frames[0]) == Proto.DRESS_TAKE_OFF;
                MethodInfo on11203 = typeof(DressController).GetMethod("On11203", BindingFlags.Instance | BindingFlags.NonPublic);
                Feed(on11203, new CliVerify.Pkt().I(1).C(DressView.HeadType).I(activeHead.Id).Bytes());
                await Task.Delay(150);
                bool takeOffImmediate = sub != null && sub.use_btn_label != null && sub.use_btn_label.text == "使用"
                    && DressModel.Instance.TryGet(DressView.HeadType, out DressModel.Snapshot takenOff)
                    && takenOff.UsedDressId == 0;

                frames.Clear();
                bool useClick = takeOffImmediate && Click(sub.use_btn, camera, raycaster, eventSystem);
                bool useFrame = frames.Count == 1 && Command(frames[0]) == Proto.DRESS_USE;
                MethodInfo on11202 = typeof(DressController).GetMethod("On11202", BindingFlags.Instance | BindingFlags.NonPublic);
                Feed(on11202, new CliVerify.Pkt().I(1).C(DressView.HeadType).I(activeHead.Id).Bytes());
                await Task.Delay(150);
                bool useImmediate = sub.use_btn_label != null && sub.use_btn_label.text == "卸下"
                    && DressModel.Instance.TryGet(DressView.HeadType, out DressModel.Snapshot wornAgain)
                    && wornAgain.UsedDressId == activeHead.Id;
                bool dressWrites = takeOffClick && takeOffFrame && takeOffImmediate && useClick && useFrame && useImmediate;

                // 进入该窗口后，外层另外三个固定页签同样必须逐个走真实点击，不把“能看到页签”当验收。
                TabButtonTwoSkin fashionTab = outerTabs.FirstOrDefault(tab => TabText(tab) == "时装");
                TabButtonTwoSkin hairTab = outerTabs.FirstOrDefault(tab => TabText(tab) == "发饰");
                TabButtonTwoSkin dressTab = outerTabs.FirstOrDefault(tab => TabText(tab) == "装扮");
                TabButtonTwoSkin suitTab = outerTabs.FirstOrDefault(tab => TabText(tab) == "套装");
                bool fashionClick = Click(fashionTab?._Image1, camera, raycaster, eventSystem);
                FashionMainView fashionView = fashionClick ? await WaitActive<FashionMainView>(5d) : null;
                bool fashionOk = fashionView != null && fashionView.PosId == 1 && window.CurrentIndex == 0;
                stage.ForceCjkFont();
                string fashionShot = stage.Capture("output/settings_fashion_current/fashion.png");

                bool hairClick = Click(hairTab?._Image1, camera, raycaster, eventSystem);
                await Task.Delay(250);
                fashionView = FindActive<FashionMainView>();
                bool hairOk = hairClick && fashionView != null && fashionView.PosId == 3 && window.CurrentIndex == 1;
                stage.ForceCjkFont();
                string hairShot = stage.Capture("output/settings_fashion_current/hair.png");

                bool suitClick = Click(suitTab?._Image1, camera, raycaster, eventSystem);
                FashionSuitView suitView = suitClick ? await WaitActive<FashionSuitView>(5d) : null;
                bool suitOk = suitView != null && window.CurrentIndex == 3;
                stage.ForceCjkFont();
                string suitShot = stage.Capture("output/settings_fashion_current/suit.png");

                bool dressTabClick = Click(dressTab?._Image1, camera, raycaster, eventSystem);
                bool dressReturnOk = dressTabClick && await WaitType(DressView.HeadType, 5d) && window.CurrentIndex == 2;

                FashionFlow.Close();
                await Task.Delay(100);
                Stopwatch warmWatch = Stopwatch.StartNew();
                FashionFlow.OpenDress(DressView.HeadType);
                DressView warmDress = await WaitDressVisual(DressView.HeadType, 3d);
                warmWatch.Stop();
                bool warmFast = warmDress != null && warmWatch.ElapsedMilliseconds < 1000;

                HashSet<string> resourcesAfter = SnapshotResources();
                string[] addedResources = resourcesAfter.Except(resourcesBefore, StringComparer.OrdinalIgnoreCase).ToArray();
                bool noRuntimeImport = addedResources.Length == 0;

                bool pass = resourcePreflight && settingVisible && settingClick && outerOk && headOk && firstFast
                    && innerTabs.Length == 3 && bubbleOk && photoOk && headBackOk
                    && skillItemsOk && skillTipOk && skillTipClose
                    && uniqueItemSurface && itemChanged && dressWrites
                    && fashionOk && hairOk && suitOk && dressReturnOk && warmFast && noRuntimeImport;
                Debug.Log("CLIVERIFY setting-fashion-current shots=" + settingShot + " | " + avatarShot
                    + " | " + skillTipShot
                    + " | " + bubbleShot + " | " + photoShot + " | " + fashionShot
                    + " | " + hairShot + " | " + suitShot);
                Debug.Log("CLIVERIFY setting-fashion-current timing firstMs=" + firstWatch.ElapsedMilliseconds
                    + " warmMs=" + warmWatch.ElapsedMilliseconds + " addedResources="
                    + (addedResources.Length == 0 ? "none" : string.Join(",", addedResources)));
                Debug.Log("CLIVERIFY setting-fashion-current VERDICT setting=" + settingVisible + "/" + settingClick
                    + " outer=" + outerOk + " head=" + headOk + " inner=" + innerTabs.Length
                    + " skills=" + skillItemsOk + "/" + skillTipOk + "/" + skillTipClose
                    + " bubble=" + bubbleOk + " photo=" + photoOk + " headBack=" + headBackOk
                    + " itemSurface=" + uniqueItemSurface + " itemChanged=" + itemChanged
                    + " dressWrites=" + takeOffClick + "/" + takeOffFrame + "/" + takeOffImmediate
                    + "/" + useClick + "/" + useFrame + "/" + useImmediate
                    + " outerChildren="
                    + fashionOk + "/" + hairOk + "/" + suitOk + "/" + dressReturnOk
                    + " firstFast=" + firstFast
                    + " warmFast=" + warmFast + " noRuntimeImport=" + noRuntimeImport + " pass=" + pass);
                return pass ? 0 : 3;
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY setting-fashion-current EXCEPTION " + exception);
                return 3;
            }
            finally
            {
                FashionFlow.Reset();
                DressSkillTipFlow.Reset();
                ResetSettingFlow();
                if (!controllerWasInitialized && DressController.Instance.IsInitialized) DressController.Instance.Dispose();
                interceptField?.SetValue(null, oldIntercept);
                if (eventSystemGo != null) Object.DestroyImmediate(eventSystemGo);
                stage?.Dispose();
                ResManager.EditorPreferFallback = fallbackBefore;
            }
        }

        private static async Task<DressView> WaitDressVisual(byte type, double timeoutSeconds)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                Canvas.ForceUpdateCanvases();
                DressView dress = FindActive<DressView>();
                DressSubView sub = FindActive<DressSubView>();
                if (dress != null && dress.SelectedType == type && sub != null && sub.Type == type
                    && sub.VisibleItemCount > 0 && sub.model_img != null && sub.model_img.sprite != null)
                {
                    DressItem[] items = sub.GetComponentsInChildren<DressItem>(false);
                    DressSkillItem[] skills = sub.GetComponentsInChildren<DressSkillItem>(false);
                    if (items.Length == sub.VisibleItemCount && items.All(item => item.DressType == type)
                        && skills.Length > 0 && skills.All(item => item.IsVisualReady))
                        return dress;
                }
                await Task.Delay(50);
            }
            return null;
        }

        private static int Command(byte[] frame)
            => frame != null && frame.Length >= 6 ? (frame[4] << 8) | frame[5] : -1;

        private static void Feed(MethodInfo method, byte[] bytes)
        {
            if (method == null) throw new MissingMethodException("Dress response handler");
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(DressController.Instance, new object[] { reader });
            if (reader.Remaining != 0) throw new InvalidDataException(method.Name + " remaining=" + reader.Remaining);
        }

        private static async Task<bool> WaitType(byte type, double timeoutSeconds)
        {
            return await WaitDressVisual(type, timeoutSeconds) != null;
        }

        private static async Task<T> WaitActive<T>(double timeoutSeconds) where T : BaseView
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                T view = FindActive<T>();
                if (view != null) return view;
                await Task.Delay(50);
            }
            return null;
        }

        private static T FindActive<T>() where T : Component
        {
            foreach (T value in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (value != null && value.gameObject.activeInHierarchy) return value;
            return null;
        }

        private static bool Click(Component target, Camera camera, GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (target == null || camera == null || raycaster == null || eventSystem == null
                || !target.gameObject.activeInHierarchy) return false;
            Graphic surface = target as Graphic ?? target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
            if (surface == null || !surface.enabled || !surface.raycastTarget) return false;
            Canvas.ForceUpdateCanvases();
            if (surface.depth < 0)
            {
                surface.enabled = false;
                surface.enabled = true;
                surface.SetAllDirty();
                Canvas.ForceUpdateCanvases();
                camera.Render();
            }
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
            Debug.LogError("CLIVERIFY setting-fashion-current raycast miss target=" + target.name
                + " point=" + pointer.position + " hits=" + string.Join(",", hits.Select(x => x.gameObject.name)));
            Debug.LogError("CLIVERIFY setting-fashion-current raycast detail surface=" + surface.name
                + " depth=" + surface.depth + " cull=" + surface.canvasRenderer.cull
                + " contains=" + RectTransformUtility.RectangleContainsScreenPoint(rect, pointer.position, camera)
                + " selfRaycast=" + surface.Raycast(pointer.position, camera)
                + " hierarchy=" + GetHierarchy(surface.transform));
            return false;
        }

        private static string GetHierarchy(Transform value)
        {
            var names = new List<string>();
            for (Transform current = value; current != null; current = current.parent) names.Add(current.name);
            return string.Join("/", names);
        }

        private static bool HasUniqueItemSurface(DressItem item)
        {
            Graphic surface = item?.ClickSurface;
            if (surface == null || !surface.enabled || !surface.raycastTarget) return false;
            return item.GetComponentsInChildren<Graphic>(true).Count(graphic => graphic.raycastTarget) == 1;
        }

        private static string TabText(TabButtonTwoSkin tab)
        {
            if (tab == null) return "";
            TMP_Text text = tab.transform.Find("labelDisplay")?.GetComponent<TMP_Text>();
            return text?.text ?? "";
        }

        private static HashSet<string> SnapshotResources()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in ResourceRoots)
            {
                if (!Directory.Exists(root)) continue;
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                    result.Add(Path.GetFullPath(file));
            }
            return result;
        }

        private static void ResetSettingFlow()
        {
            typeof(SettingFlow).GetMethod("Reset", StaticNonPublic)?.Invoke(null, null);
        }
    }
}
