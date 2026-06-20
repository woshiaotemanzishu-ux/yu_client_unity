using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Chat
{
    /// <summary>
    /// 聊天模块编排:按需打开/关闭全屏聊天窗口(对标老端 主 HUD 点聊天框 → OPEN_CHAT_VIEW → ChatController.open_chat_view → ChatParentView)。
    ///
    /// ChatModule 合并 prefab 含多个顶层窗口(ChatParentView + 工具面板/喇叭/语音/好友聊天等);本 tick 仅移植主窗 <c>ChatParentView</c>,
    /// 其余暂无 View → 打开时隐藏所有顶层窗口再只 Show 主窗(同 MarriageFlow)。入口注册见 <see cref="ChatBootstrap"/>(MainUIRouter "chat",
    /// 由 HUD 聊天框 MainUIChatView 点击触发)。主窗自带 _close/_btn_close 关闭。
    /// </summary>
    public static class ChatFlow
    {
        private const string MODULE = "chat";
        private const string PREFAB = "ChatModule";

        private static GameObject _moduleRoot;
        private static ChatParentView _mainView;
        private static bool _loading;

        public static void Toggle()
        {
            if (_mainView != null && _mainView.IsShown)
            {
                Close();
                return;
            }
            _ = OpenAsync();
        }

        public static void Open()
        {
            _ = OpenAsync();
        }

        public static void Close()
        {
            if (_mainView != null)
            {
                _mainView.Hide();
            }
        }

        /// <summary>
        /// 切换聊天模块内子面板(表情 ChatToolPanel / 喇叭 ChatTrumpetView / 背包 ChatBagPanel…):已显则关、未显则开(叠在主窗上)。
        /// 按 View 子类名在模块根里查 BaseView;未移植的查不到 → 日志降级。供 ChatParentView 各按钮调用作真实触发。
        /// </summary>
        public static void ToggleSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Chat", "ToggleSub({0}) 时聊天模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName)
                {
                    if (v.IsShown) v.Hide();
                    else v.Show();
                    return;
                }
            }
            GameLog.Info("Chat", "聊天子面板 [{0}] 未移植 View,待对接", viewTypeName);
        }

        private static async Task OpenAsync()
        {
            if (_moduleRoot != null)
            {
                if (_mainView != null)
                {
                    _mainView.Show();
                }
                return;
            }

            if (_loading)
            {
                return;
            }
            _loading = true;

            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("Chat", "ChatModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            // 仅写了主窗 → 隐藏所有顶层窗口(含未写 View 的裸窗口),再只显主窗。
            foreach (Transform c in root.transform)
            {
                c.gameObject.SetActive(false);
            }

            foreach (BaseView v in root.GetComponentsInChildren<BaseView>(true))
            {
                if (v is ChatParentView cv)
                {
                    _mainView = cv;
                    break;
                }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Chat", "ChatModule 缺 ChatParentView(重跑 chat 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Chat", "聊天窗口打开: {0}", key);
        }

        internal static void Reset()
        {
            if (_moduleRoot != null)
            {
                ResManager.ReleaseInstance(_moduleRoot);
            }
            _moduleRoot = null;
            _mainView = null;
            _loading = false;
        }
    }
}
