using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.RedPacket
{
    /// <summary>
    /// 红包模块编排:按需打开/关闭红包面板(对标老端 二级 HUD 红包按钮 → RedPacketMainView)。
    ///
    /// RedPacketModule 合并 prefab 含 RedPacketMainView(主) + RedPacketCtrlView/RedPacketDetailView;本 tick 仅移植主窗,
    /// 打开时隐藏所有顶层窗口再只 Show RedPacketMainView。子窗(详情/发包)经 <see cref="OpenSub"/> 后续接。
    /// 入口注册见 <see cref="RedPacketBootstrap"/>(MainUIRouter "redpacket",二级 HUD 红包按钮触发)。主窗自带 _btn_close;再点按钮亦可关(Toggle)。
    /// </summary>
    public static class RedPacketFlow
    {
        private const string MODULE = "redPacket";
        private const string PREFAB = "RedPacketModule";

        private static GameObject _moduleRoot;
        private static RedPacketMainView _mainView;
        private static bool _loading;

        public static void Toggle()
        {
            if (_mainView != null && _mainView.IsShown) { Close(); return; }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_mainView != null) _mainView.Hide();
        }

        /// <summary>打开红包模块内子窗(详情/发包…),叠在主面板上;按 View 子类名查找。未移植查不到 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("RedPacket", "OpenSub({0}) 时红包模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("RedPacket", "红包子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        private static async Task OpenAsync()
        {
            if (_moduleRoot != null)
            {
                if (_mainView != null) _mainView.Show();
                return;
            }

            if (_loading) return;
            _loading = true;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await MainUIRouteFallback.InstantiateOrShowAsync("redpacket", "RedPacket", key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("RedPacket", "RedPacketModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            foreach (Transform c in root.transform)
            {
                c.gameObject.SetActive(false);
            }

            foreach (BaseView v in root.GetComponentsInChildren<BaseView>(true))
            {
                if (v is RedPacketMainView rv) { _mainView = rv; break; }
            }

            if (_mainView == null)
            {
                GameLog.Warn("RedPacket", "RedPacketModule 缺 RedPacketMainView(重跑 redPacket 流水线:转换+回填)");
                MainUIRouteFallback.ShowUnavailable("redpacket", "RedPacket", "RedPacketModule missing RedPacketMainView");
                Reset();
                return;
            }

            _mainView.Show();
            GameLog.Info("RedPacket", "红包面板打开: {0}", key);
        }

        internal static void Reset()
        {
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _mainView = null;
            _loading = false;
        }
    }
}
