using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Pet
{
    /// <summary>
    /// 宠物/坐骑(外观)模块编排:按需打开/关闭(对标老端 主 HUD 宠物图标 → MountPetView tab0=御风云骑 HorseComponentView,布局 OutWardBaseView)。
    ///
    /// MountPetView 容器是 shared-prefab、未随模块生成 → 取默认首页 <c>OutWardBaseView</c>(御风云骑外观,HorseComponentView 复用此布局)
    /// 作 HUD「宠物」入口(同 equip/godBefall/treasure/composite 取首页范式;剑魄同修/神巫/天妖灵魄 等分页属各自模块/同模块其它窗,后续接)。
    /// 打开 PetModule 后隐藏所有顶层窗口再只 Show OutWardBaseView;子窗(属性/幻化/水晶/技能…)经 <see cref="OpenSub"/> 叠主面板上。
    /// 入口注册见 <see cref="PetBootstrap"/>(MainUIRouter "pet")。主面板无关闭按钮 → 宠物图标再点关闭(Toggle)。
    /// </summary>
    public static class PetFlow
    {
        private const string MODULE = "pet";
        private const string PREFAB = "PetModule";

        private static GameObject _moduleRoot;
        private static OutWardBaseView _mainView;
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

        /// <summary>打开宠物模块内子窗(属性/幻化/水晶/技能…),叠在主面板上;按 View 子类名查找。未移植查不到 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Pet", "OpenSub({0}) 时宠物模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Pet", "宠物子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
            GameObject root = await MainUIRouteFallback.InstantiateOrShowAsync(MODULE, "Pet", key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("Pet", "PetModule prefab load failed: {0}", key);
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
                if (v is OutWardBaseView ov) { _mainView = ov; break; }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Pet", "PetModule 缺 OutWardBaseView(重跑 pet 流水线:转换+回填)");
                MainUIRouteFallback.ShowUnavailable(MODULE, "Pet", "PetModule missing OutWardBaseView");
                Reset();
                return;
            }

            _mainView.Show();
            GameLog.Info("Pet", "宠物/坐骑外观面板打开: {0}", key);
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
