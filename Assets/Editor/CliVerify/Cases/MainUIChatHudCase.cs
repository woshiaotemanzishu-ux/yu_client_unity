using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Module.Core.Chat;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 主界面聊天条真实 prefab 验收：ChatModel 初始渲染、30 条裁剪、实时事件刷新、系统/普通分区、
    /// 频道徽标首行留位和旧富文本转换。直接加载 HudChatBar.prefab，不以手调 OnClick/私有方法代替链路。
    /// </summary>
    public static class MainUIChatHudCase
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudChatBar.prefab";

        public static async Task<int> Run()
        {
            bool fallbackBefore = Shenxiao.Framework.Res.ResManager.EditorPreferFallback;
            Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
            GameObject instance = null;
            MainUIChatView view = null;

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

                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                view = instance != null ? instance.GetComponentInChildren<MainUIChatView>(true) : null;
                if (view == null)
                {
                    Debug.LogError("CLIVERIFY mainui-chat MainUIChatView missing");
                    return 3;
                }

                view.Show();
                ForceLayout(view);

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
                bool scrollBottomOk = view._panel_chat != null
                    && Mathf.Approximately(view._panel_chat.verticalNormalizedPosition, 0f)
                    && view._panel_sys != null
                    && Mathf.Approximately(view._panel_sys.verticalNormalizedPosition, 0f);

                bool pass = countOk && latestOk && badgeInlineOk && singleLineHeightOk && welcomeOk
                    && richTextOk && realtimeOk && scrollBottomOk;
                Debug.Log("CLIVERIFY mainui-chat VERDICT count=" + countOk
                    + " latest=" + latestOk + " badgeInline=" + badgeInlineOk
                    + " singleLineHeight=" + singleLineHeightOk + " welcome=" + welcomeOk
                    + " richText=" + richTextOk
                    + " realtime=" + realtimeOk + " scrollBottom=" + scrollBottomOk
                    + " chatCount=" + chatItems.Count + " systemCount=" + systemItems.Count
                    + " lastHeight=" + lastHeight + " pass=" + pass);
                await Task.CompletedTask;
                return pass ? 0 : 3;
            }
            finally
            {
                if (view != null && view.IsShown) view.Hide();
                if (instance != null) Object.DestroyImmediate(instance);
                ChatModel.Instance.Reset();
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = fallbackBefore;
            }
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
