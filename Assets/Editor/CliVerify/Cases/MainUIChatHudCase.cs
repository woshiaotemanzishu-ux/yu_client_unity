using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Module.Core.Chat;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 主界面聊天条真实 prefab 验收：ChatModel 初始渲染、30 条裁剪、实时事件刷新、系统/普通分区、
    /// 频道徽标首行留位、旧富文本转换，以及设置/好友/商城三个固定入口的
    /// GraphicRaycaster→PointerClick 真点击。直接加载 HudChatBar.prefab，不以手调 OnClick/私有方法代替链路。
    /// </summary>
    public static class MainUIChatHudCase
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudChatBar.prefab";

        public static async Task<int> Run()
        {
            bool fallbackBefore = Shenxiao.Framework.Res.ResManager.EditorPreferFallback;
            Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
            GameObject instance = null;
            GameObject eventSystemGo = null;
            MainUIChatView view = null;
            CliVerify.Stage stage = null;

            try
            {
                ChatModel model = ChatModel.Instance;
                model.Reset();
                model.EnsureWelcomeSystemMessage();
                model.AddMessage(new ChatMessage
                {
                    Channel = ChatModel.ChannelSystem,
                    Message = "<font color='#60aeff'>彩色</font><color@4>橙色</color>",
                    Time = 10
                });

                for (int i = 1; i <= 31; i++)
                {
                    model.AddMessage(new ChatMessage
                    {
                        Channel = i % 2 == 0 ? ChatModel.ChannelGuild : ChatModel.ChannelWorld,
                        PlayerId = 1000 + i,
                        Figure = new Shenxiao.Common.Proto.FigureProto { name = "玩家" + i },
                        Message = "第" + i + "条",
                        Time = (uint)(100 + i)
                    });
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null)
                {
                    Debug.LogError("CLIVERIFY mainui-chat prefab missing: " + PrefabPath);
                    return 3;
                }

                stage = CliVerify.Stage.Create();
                instance = PrefabUtility.InstantiatePrefab(prefab, stage.CanvasRoot) as GameObject;
                view = instance != null ? instance.GetComponentInChildren<MainUIChatView>(true) : null;
                if (view == null)
                {
                    Debug.LogError("CLIVERIFY mainui-chat MainUIChatView missing");
                    return 3;
                }

                view.Show();
                ForceLayout(view);

                EventSystem eventSystem = EventSystem.current;
                if (eventSystem == null)
                {
                    eventSystemGo = new GameObject("MainUIChatHudCase_EventSystem", typeof(EventSystem));
                    eventSystem = eventSystemGo.GetComponent<EventSystem>();
                }

                int settingClicks = 0;
                int friendClicks = 0;
                int shopClicks = 0;
                MainUIRouter.Register("setting", () => settingClicks++);
                MainUIRouter.Register("friend", () => friendClicks++);
                MainUIRouter.Register("shop", () => shopClicks++);
                bool settingClickOk = ClickVisibleEntry(view._img_setting, stage, eventSystem);
                bool friendClickOk = ClickVisibleEntry(view._img_friend, stage, eventSystem);
                bool shopClickOk = ClickVisibleEntry(view._img_shop, stage, eventSystem);
                bool entryClicksOk = settingClickOk && friendClickOk && shopClickOk
                    && settingClicks == 1 && friendClicks == 1 && shopClicks == 1;

                List<MainUIChatItem> chatItems = GetDirectItems(view._box_chat_con);
                List<MainUIChatItem> systemItems = GetDirectItems(view._box_sys_con);
                bool countOk = chatItems.Count == ChatModel.MainHudMessageCap && systemItems.Count == 2;

                MainUIChatItem lastChat = chatItems.Count > 0 ? chatItems[chatItems.Count - 1] : null;
                bool latestOk = lastChat != null && lastChat.contentLabel.text.Contains("玩家31")
                    && lastChat.contentLabel.text.Contains("第31条");
                bool badgeInlineOk = lastChat != null && lastChat.titleBg.gameObject.activeSelf
                    && lastChat.contentLabel.text.StartsWith("<space=46px>")
                    && lastChat.contentLabel.rectTransform.anchoredPosition.x <= 5f;
                float lastHeight = lastChat != null ? ((RectTransform)lastChat.transform).rect.height : 0f;
                bool singleLineHeightOk = lastHeight >= MainUIChatItem.SingleLineHeight && lastHeight < 40f;

                MainUIChatItem formattedSystem = systemItems.Count > 1 ? systemItems[1] : null;
                MainUIChatItem welcomeSystem = systemItems.Count > 0 ? systemItems[0] : null;
                bool welcomeOk = ChatModel.WelcomeSystemMessage ==
                        "欢迎踏入九州大荒。神霄崩灭后，天殒遗骸化作道痕，秘境引劫而生——愿君融痕证道，历尽九天梯劫！"
                    && welcomeSystem != null
                    && welcomeSystem.contentLabel.text.Contains(ChatModel.WelcomeSystemMessage);
                string systemText = formattedSystem != null ? formattedSystem.contentLabel.text : string.Empty;
                bool richTextOk = systemText.Contains("<color=#60aeff>")
                    && systemText.Contains("<color=#F88452>")
                    && !systemText.Contains("<font");

                model.AddMessage(new ChatMessage
                {
                    Channel = ChatModel.ChannelSmallKuafu,
                    PlayerId = 9999,
                    Figure = new Shenxiao.Common.Proto.FigureProto { name = "实时玩家" },
                    Message = "实时刷新",
                    Time = 999
                });
                ForceLayout(view);
                chatItems = GetDirectItems(view._box_chat_con);
                lastChat = chatItems.Count > 0 ? chatItems[chatItems.Count - 1] : null;
                bool realtimeOk = chatItems.Count == ChatModel.MainHudMessageCap
                    && lastChat != null && lastChat.contentLabel.text.Contains("实时刷新");
                bool scrollBottomOk = AtBottomOrNotScrollable(view._panel_chat)
                    && AtBottomOrNotScrollable(view._panel_sys);
                Debug.Log("CLIVERIFY mainui-chat scroll chat=" + DescribeScroll(view._panel_chat)
                    + " system=" + DescribeScroll(view._panel_sys));

                bool pass = countOk && latestOk && badgeInlineOk && singleLineHeightOk && welcomeOk
                    && richTextOk && realtimeOk && scrollBottomOk && entryClicksOk;
                Debug.Log("CLIVERIFY mainui-chat VERDICT count=" + countOk
                    + " latest=" + latestOk + " badgeInline=" + badgeInlineOk
                    + " singleLineHeight=" + singleLineHeightOk + " welcome=" + welcomeOk
                    + " richText=" + richTextOk
                    + " realtime=" + realtimeOk + " scrollBottom=" + scrollBottomOk
                    + " entryClicks=" + entryClicksOk
                    + " entryCounts=" + settingClicks + "/" + friendClicks + "/" + shopClicks
                    + " chatCount=" + chatItems.Count + " systemCount=" + systemItems.Count
                    + " lastHeight=" + lastHeight + " pass=" + pass);
                await Task.CompletedTask;
                return pass ? 0 : 3;
            }
            finally
            {
                MainUIRouter.Unregister("setting");
                MainUIRouter.Unregister("friend");
                MainUIRouter.Unregister("shop");
                if (view != null && view.IsShown) view.Hide();
                if (instance != null) Object.DestroyImmediate(instance);
                if (eventSystemGo != null) Object.DestroyImmediate(eventSystemGo);
                if (stage != null) stage.Dispose();
                ChatModel.Instance.Reset();
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = fallbackBefore;
            }
        }

        private static bool ClickVisibleEntry(Image surface, CliVerify.Stage stage, EventSystem eventSystem)
        {
            if (surface == null || stage == null || eventSystem == null) return false;

            Button button = surface.GetComponent<Button>();
            bool directSurface = surface.enabled && surface.raycastTarget && button != null;
            Canvas canvas = stage.CanvasRoot.GetComponent<Canvas>();
            GraphicRaycaster raycaster = stage.CanvasRoot.GetComponent<GraphicRaycaster>();
            Camera camera = canvas != null ? canvas.worldCamera : null;
            if (!directSurface || raycaster == null || camera == null) return false;

            Canvas.ForceUpdateCanvases();
            camera.Render();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(
                camera, surface.rectTransform.TransformPoint(surface.rectTransform.rect.center));
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
                Debug.Log("CLIVERIFY mainui-chat entry=" + surface.name + " point=" + point
                    + " hits=" + hits.Count + " directSurface=" + directSurface);
                return true;
            }

            Debug.LogError("CLIVERIFY mainui-chat entry raycast miss: " + surface.name
                + " point=" + point + " hits=" + hits.Count + " directSurface=" + directSurface);
            return false;
        }

        private static bool AtBottomOrNotScrollable(ScrollRect scroll)
        {
            if (scroll == null || scroll.content == null) return false;
            RectTransform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
            if (viewport == null) return false;
            bool overflows = scroll.content.rect.height > viewport.rect.height + 0.5f;
            // ScrollRect 在真实 Canvas 几何下会留下 1e-6 量级浮点残差；视觉上仍是精确底部。
            return !overflows || scroll.verticalNormalizedPosition <= 0.0001f;
        }

        private static string DescribeScroll(ScrollRect scroll)
        {
            if (scroll == null || scroll.content == null) return "missing";
            RectTransform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
            return "norm:" + scroll.verticalNormalizedPosition
                + ",content:" + scroll.content.rect.height
                + ",viewport:" + (viewport != null ? viewport.rect.height : -1f)
                + ",vertical:" + scroll.vertical;
        }

        private static void ForceLayout(MainUIChatView view)
        {
            Canvas.ForceUpdateCanvases();
            if (view._box_chat_con != null) LayoutRebuilder.ForceRebuildLayoutImmediate(view._box_chat_con);
            if (view._box_sys_con != null) LayoutRebuilder.ForceRebuildLayoutImmediate(view._box_sys_con);
            Canvas.ForceUpdateCanvases();
        }

        private static List<MainUIChatItem> GetDirectItems(RectTransform parent)
        {
            var result = new List<MainUIChatItem>();
            if (parent == null) return result;
            for (int i = 0; i < parent.childCount; i++)
            {
                MainUIChatItem item = parent.GetChild(i).GetComponent<MainUIChatItem>();
                if (item != null && item.gameObject.activeSelf) result.Add(item);
            }
            return result;
        }
    }
}
