using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备模块编排:按需打开/关闭装备面板(对标老端 主界面 MainFunc.Equip(=4) → EquipModel.Fire(OPEN_VIEW,"EquipView") → EquipView)。
    ///
    /// 老端 EquipView 是分页窗(淬炉 EquipStrenView/淬炼 EquipSmeltView/镶嵌/洗魄 EquipWashView/九炼/圣骸);Unity 侧本模块
    /// 已移植 淬炉/淬炼/洗魄 三页(镶嵌/九炼/圣骸 属 equipArmor/equipRefinement 独立模块,待后续生成)。无 EquipView 容器场景 →
    /// 本 Flow 打开 EquipModule 后只 Show 首页 <c>EquipStrenView</c>(= 老端默认 tab0 天殒淬炉)。分页切换待容器/分页栏移植后接。
    /// 手法照抄 BagFlow/RoleFlow:实例化合并 prefab → 全关 BaseView → 只 Show 主页。入口注册见 <see cref="EquipBootstrap"/>(MainUIRouter "equip")。
    /// 无独立关闭按钮 → 再次点击图标 <see cref="Toggle"/> 关闭(与老端 BaseWindowComponent 再点关闭一致)。
    /// </summary>
    public static class EquipFlow
    {
        private const string MODULE = "equip";
        private const string PREFAB = "EquipModule";

        private static GameObject _moduleRoot;
        private static EquipStrenView _mainView;
        private static bool _loading;

        /// <summary>切换显示:已开则关、未开则开。</summary>
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

        /// <summary>打开装备模块内子窗(淬炉宗师 EquipStrenMasterView…),叠在主面板上;按 View 子类名查找。未移植查不到 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Equip", "OpenSub({0}) 时装备模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName)
                {
                    v.Show();
                    return;
                }
            }
            GameLog.Info("Equip", "装备子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
                GameLog.Error("Equip", "EquipModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            BaseView[] views = root.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views)
            {
                v.gameObject.SetActive(false);
            }

            foreach (BaseView v in views)
            {
                if (v is EquipStrenView sv)
                {
                    _mainView = sv;
                    break;
                }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Equip", "EquipModule 缺 EquipStrenView(重跑 equip 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Equip", "装备面板打开(首页 天殒淬炉): {0}", key);
        }

        /// <summary>断线(非游戏内自动重连)/登出时清理整模块根。</summary>
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
