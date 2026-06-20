using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临模块编排:按需打开/关闭神祇面板(对标老端 主界面功能图标 → GodBefallMainView)。
    ///
    /// 老端 GodBefallMainView 是分页容器(神契装备/回收/合成/秘闻/途径);该容器在转换器里是 shared-prefab、
    /// 未随本模块生成独立窗口 → 本 Flow 打开 GodBefallModule 后只 Show 默认页 <c>GodBefallEquipView</c>(神契装备,
    /// 即 prefab 里默认 active 的首窗,等价老端默认 tab)。分页切换待容器/分页栏移植后接。
    /// 手法照抄 EquipFlow/BagFlow。入口注册见 <see cref="GodBefallBootstrap"/>(MainUIRouter "232" = 主界面该功能图标 res)。
    /// 无独立关闭按钮统管 → 再次点击图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class GodBefallFlow
    {
        private const string MODULE = "godBefall";
        private const string PREFAB = "GodBefallModule";

        private static GameObject _moduleRoot;
        private static GodBefallEquipView _mainView;
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
                GameLog.Error("GodBefall", "GodBefallModule prefab load failed: {0}", key);
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
                if (v is GodBefallEquipView gv)
                {
                    _mainView = gv;
                    break;
                }
            }

            if (_mainView == null)
            {
                GameLog.Warn("GodBefall", "GodBefallModule 缺 GodBefallEquipView(重跑 godBefall 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("GodBefall", "神祇面板打开(默认页 神契装备): {0}", key);
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
