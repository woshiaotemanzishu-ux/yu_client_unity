using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Composite
{
    /// <summary>
    /// 合成(熔铸)模块编排:按需打开/关闭合成面板(对标老端 主 HUD 合成图标 → CompositeView tab0=装备合成 CompositeEquipView)。
    ///
    /// 合成容器 CompositeView 是 shared-prefab、未随模块生成 → 取默认首页 <c>CompositeEquipView</c> 作 HUD「合成」入口
    /// (同 equip/godBefall/treasure 取首页范式;其余合成分页 物品/守护/神兵/圣痕/虚冥… 属同模块其它顶层窗,后续接分页切换)。
    /// 打开 CompositeModule 后隐藏所有顶层窗口再只 Show CompositeEquipView;子窗(排行/选材…)经 <see cref="OpenSub"/> 叠主面板上。
    /// 入口注册见 <see cref="CompositeBootstrap"/>(MainUIRouter "composite")。主面板无关闭按钮 → 合成图标再点关闭(Toggle)。
    /// </summary>
    public static class CompositeFlow
    {
        private const string MODULE = "composite";
        private const string PREFAB = "CompositeModule";

        private static GameObject _moduleRoot;
        private static CompositeEquipView _mainView;
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

        /// <summary>打开合成模块内子窗(排行/选材…),叠在主面板上;按 View 子类名查找。未移植查不到 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Composite", "OpenSub({0}) 时合成模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Composite", "合成子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("Composite", "CompositeModule prefab load failed: {0}", key);
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
                if (v is CompositeEquipView cv) { _mainView = cv; break; }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Composite", "CompositeModule 缺 CompositeEquipView(重跑 composite 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Composite", "合成面板打开: {0}", key);
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
