using Shenxiao.Editor.LayaUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.Friend
{
    /// <summary>
    /// FriendModule.prefab 运行时组件升级器(自动循环 轮7,照 JewelBindUpgrader 范式)。
    ///
    /// 背景(核查实录,r7_unity §3):FriendModule.prefab 本体已含 FriendView/EmailView/FriendAddPopView/
    /// FriendApllyPopView/FriendBlackListPopView(已挂真业务子类)+ EmailPopView/FriendMenuView(只挂基类,
    /// 几何已在对的 prefab 里,不需要嫁接)。**唯独 FriendChatView(私聊窗)整棵被批转换器烤进了
    /// ChatModule.prefab 顶层**(源资源目录 cdn/resource/game/chat/,而非 friend/)——它自带一个局部
    /// __Templates,内含 FriendChatTabItem(Bind 类实际落在 Shenxiao.Generated.UI.Friend,非 Chat)与
    /// FriendMineChatItem(同)两个列表项模板,随母体一并嫁接即可,无需单独摸。
    /// 私聊气泡"对方"(FriendChatItem)反而是**独立烤成单独 prefab**(Assets/Prefabs/UI/Friend/FriendChatItem.prefab,
    /// 放对了目录),但从未被嫁接/回填过——本轮决策:不做二次嫁接,FriendChatView 业务代码在运行时经
    /// Addressable(GameResPath.GetUIPrefab("friend","FriendChatItem"))按需 Instantiate(对标 r7_unity §4 给的
    /// 两个方案之一),独立 prefab 单独跑一次 LayaBindFiller.FillPrefab 升级即可,不需要嫁接进 FriendModule。
    ///
    /// FriendFacePanel(表情面板,同样烤在 ChatModule 顶层)本轮**不嫁接**——功能未移植(faceBtn 点击仅日志降级,
    /// 见 FriendChatView 注释),避免引入无消费方的空转换。
    ///
    /// 两阶段(【阶段A·嫁接,仅首跑】把 ChatModule.prefab 顶层的 FriendChatView 整棵克隆进 FriendModule 顶层,
    /// ChatModule.prefab 只读不改;【阶段B·升级回填,每次幂等】LayaBindFiller.FillPrefab 分别跑 FriendModule.prefab
    /// 与独立 FriendChatItem.prefab,把全部基类 Bind 升级为运行时业务子类 + 按节点名回填引用)。
    /// </summary>
    public static class FriendBindUpgrader
    {
        private const string FriendModulePath = "Assets/Prefabs/UI/Friend/FriendModule.prefab";
        private const string ChatModulePath = "Assets/Prefabs/UI/Chat/ChatModule.prefab";
        private const string FriendChatItemPrefabPath = "Assets/Prefabs/UI/Friend/FriendChatItem.prefab";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Friend",
                Name = "FriendModule(好友+邮件+私聊 Bind 升级)",
                Note = "从 ChatModule.prefab 顶层嫁接 FriendChatView(自带 __Templates:FriendChatTabItem/" +
                       "FriendMineChatItem)进 FriendModule 顶层(ChatModule 只读不改),再经 LayaBindFiller.FillPrefab " +
                       "把 FriendModule.prefab 与独立 FriendChatItem.prefab 的全部 Bind 升级为运行时业务子类",
                Order = 96,
                Generate = () => Generate(),
                PrefabPath = FriendModulePath,
            });
        }

        /// <summary>两阶段执行(嫁接仅首跑;升级回填幂等可重跑)。成功返回 true。</summary>
        public static bool Generate()
        {
            // 【阶段A】嫁接(仅当 FriendModule 顶层还没有 FriendChatView)
            GameObject friendRoot = PrefabUtility.LoadPrefabContents(FriendModulePath);
            if (friendRoot == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + FriendModulePath);
                return false;
            }
            try
            {
                if (friendRoot.transform.Find("FriendChatView") == null)
                {
                    if (!GraftFromChat(friendRoot.transform)) return false;
                    PrefabUtility.SaveAsPrefabAsset(friendRoot, FriendModulePath);
                    Debug.Log("[UiCreator] FriendModule 嫁接完成:FriendChatView(含 Tab/Mine 模板)已入顶层");
                }
                else
                {
                    Debug.Log("[UiCreator] FriendModule 顶层已有 FriendChatView(已嫁接过)→ 只跑升级回填");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(friendRoot);
            }

            // 【阶段B】升级回填(复用流水线回填工具,幂等;FriendModule 本体 + 独立 FriendChatItem.prefab 各跑一次)
            if (!LayaBindFiller.FillPrefab(FriendModulePath))
            {
                Debug.LogError("[UiCreator] LayaBindFiller.FillPrefab(" + FriendModulePath + ") 失败(看 Console 前面的警告)");
                return false;
            }
            if (!LayaBindFiller.FillPrefab(FriendChatItemPrefabPath))
            {
                Debug.LogError("[UiCreator] LayaBindFiller.FillPrefab(" + FriendChatItemPrefabPath + ") 失败(看 Console 前面的警告)");
                return false;
            }

            return Verify();
        }

        /// <summary>从 ChatModule.prefab(只读)克隆顶层活树 FriendChatView(含自带局部 __Templates)到 FriendModule 顶层。</summary>
        private static bool GraftFromChat(Transform friendRoot)
        {
            GameObject chatAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ChatModulePath);
            if (chatAsset == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + ChatModulePath);
                return false;
            }

            Transform srcView = chatAsset.transform.Find("FriendChatView"); // 顶层活树(非 __Templates 死树)
            if (srcView == null)
            {
                Debug.LogError("[UiCreator] ChatModule.prefab 顶层没有 FriendChatView(烤入产物变动?先查顶层子节点)");
                return false;
            }

            GameObject viewClone = Object.Instantiate(srcView.gameObject, friendRoot, false);
            viewClone.name = "FriendChatView"; // 去掉 "(Clone)" 后缀,FriendFlow 按名查找
            viewClone.SetActive(true);         // 与兄弟窗口一致(FriendFlow 打开时统一 SetActive(false) 管理)
            return true;
        }

        /// <summary>验证运行时子类全部真实在挂(FriendModule.prefab 内 + 独立 FriendChatItem.prefab)。</summary>
        private static bool Verify()
        {
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(FriendModulePath);
            if (saved == null)
            {
                Debug.LogError("[UiCreator] 验证失败:" + FriendModulePath + " 加载不到");
                return false;
            }

            bool ok = true;
            // 顶层窗口(既有 + 本轮新写)
            ok &= Check<Shenxiao.Module.Core.Friend.FriendView>(saved, "FriendView");
            ok &= Check<Shenxiao.Module.Core.Friend.EmailView>(saved, "EmailView");
            ok &= Check<Shenxiao.Module.Core.Friend.FriendAddPopView>(saved, "FriendAddPopView");
            ok &= Check<Shenxiao.Module.Core.Friend.FriendApllyPopView>(saved, "FriendApllyPopView");
            ok &= Check<Shenxiao.Module.Core.Friend.FriendBlackListPopView>(saved, "FriendBlackListPopView");
            ok &= Check<Shenxiao.Module.Core.Friend.EmailPopView>(saved, "EmailPopView");
            ok &= Check<Shenxiao.Module.Core.Friend.FriendMenuView>(saved, "FriendMenuView");
            ok &= Check<Shenxiao.Module.Core.Friend.FriendChatView>(saved, "FriendChatView(嫁接)");
            // 列表项(既有 __Templates 内,本轮新写业务子类)
            ok &= Check<Shenxiao.Module.Core.Friend.FriendListItem>(saved, "FriendListItem");
            ok &= Check<Shenxiao.Module.Core.Friend.FriendAddPopItem>(saved, "FriendAddPopItem");
            ok &= Check<Shenxiao.Module.Core.Friend.FriendApllyPopItem>(saved, "FriendApllyPopItem");
            ok &= Check<Shenxiao.Module.Core.Friend.FriendBlackListItm>(saved, "FriendBlackListItm");
            ok &= Check<Shenxiao.Module.Core.Friend.EmailItem>(saved, "EmailItem");
            ok &= Check<Shenxiao.Module.Core.Friend.EmailPopViewItem>(saved, "EmailPopViewItem");
            // FriendChatView 自带局部 __Templates(嫁接带入)
            ok &= Check<Shenxiao.Module.Core.Friend.FriendChatTabItem>(saved, "FriendChatTabItem(嫁接)");
            ok &= Check<Shenxiao.Module.Core.Friend.FriendMineChatItem>(saved, "FriendMineChatItem(嫁接)");

            GameObject chatItemAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FriendChatItemPrefabPath);
            if (chatItemAsset == null)
            {
                Debug.LogError("[UiCreator] 验证失败:" + FriendChatItemPrefabPath + " 加载不到");
                ok = false;
            }
            else
            {
                ok &= Check<Shenxiao.Module.Core.Friend.FriendChatItem>(chatItemAsset, "FriendChatItem(独立 prefab)");
            }

            Debug.Log("[UiCreator] FriendBindUpgrader 验证 " + (ok ? "OK" : "FAILED") + " " + FriendModulePath);
            return ok;
        }

        private static bool Check<T>(GameObject root, string label) where T : Component
        {
            if (root.GetComponentInChildren<T>(true) != null) return true;
            Debug.LogError("[UiCreator] 缺运行时组件 " + typeof(T).Name + "(" + label + ")");
            return false;
        }

        /// <summary>
        /// 批处理入口(供 -executeMethod 调用):
        ///   Unity.exe -batchmode -projectPath . -executeMethod
        ///     Shenxiao.Editor.UiCreator.Friend.FriendBindUpgrader.GenerateBatch -logFile Temp/friend_bind_upgrader.log
        /// 成功判据 = 全部运行时子类真实在挂 → Exit(0);否则 Exit(1)。
        /// </summary>
        public static void GenerateBatch()
        {
            try
            {
                bool ok = Generate();
                Debug.Log("[UiCreator] FriendBindUpgrader.GenerateBatch " + (ok ? "OK " : "FAILED ") + FriendModulePath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] FriendBindUpgrader.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}
