using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚恋模块编排:按需打开/关闭婚恋面板(对标老端 主界面 MainFunc.Marriage → MarriageBaseView)。
    ///
    /// MarriageModule 合并 prefab 含 21 个顶层窗口(主界面 + 求婚/送花/离婚/喜宴/记录/荣耀… 各子流程);本 tick 仅移植
    /// 主面板 <c>MarriageBaseView</c> 的 View 逻辑,其余窗口暂无 View(原始转换内容)→ 打开时**隐藏所有顶层窗口**再只
    /// Show 主面板(不能只关 BaseView,否则默认 active 的子窗口会裸露)。其余 39 子页待后续 tick 接入。
    /// 入口注册见 <see cref="MarriageBootstrap"/>(MainUIRouter "love" = 主界面婚恋图标 res)。主面板自带 _btn_close 可关。
    /// </summary>
    public static class MarriageFlow
    {
        private const string MODULE = "marriage";
        private const string PREFAB = "MarriageModule";

        private static GameObject _moduleRoot;
        private static MarriageBaseView _mainView;
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
                GameLog.Error("Marriage", "MarriageModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            // 仅写了主面板 → 隐藏所有顶层窗口(含未写 View 的裸窗口),再只显主面板(includeInactive 仍能取到组件)。
            foreach (Transform c in root.transform)
            {
                c.gameObject.SetActive(false);
            }

            foreach (BaseView v in root.GetComponentsInChildren<BaseView>(true))
            {
                if (v is MarriageBaseView mv)
                {
                    _mainView = mv;
                    break;
                }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Marriage", "MarriageModule 缺 MarriageBaseView(重跑 marriage 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Marriage", "婚恋面板打开: {0}", key);
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
