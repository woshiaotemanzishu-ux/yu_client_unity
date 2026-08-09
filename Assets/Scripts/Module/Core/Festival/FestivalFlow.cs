using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Festival
{
    /// <summary>
    /// 祭典/宝录(Festival)模块编排(自动循环 轮18 便宜活批 PK3 实做,仿 <see cref="Shenxiao.Module.Core.Halo.HaloFlow"/>
    /// 套路)。老端点击主界面图标经 MainUIRouter 路由 "223" 打开宝录面板(FestivalBaseView,先判断
    /// GetFestivalFirstCheck 弹 GoToAscendingOrderView 引导,否则落 Task/LevelAward/Commodity/GetReward/
    /// UpLevel 等子面板)。**本轮 Flow 只做容器级开关窗**(加载 FestivalModule.prefab 并整体 Show/Hide),
    /// 12 个子面板 Bind 已烤好但均继承 BaseView、彼此无绑定/路由逻辑,子面板选择与数据绑定留尾包,
    /// 不在此臆造导航规则。入口注册见 <see cref="FestivalBootstrap"/>(MainUIRouter "223")。
    /// </summary>
    public static class FestivalFlow
    {
        private const string MODULE = "Festival";
        private const string PREFAB = "FestivalModule";

        private static GameObject _moduleRoot;
        private static bool _loading;
        private static bool _wantShown;
        private static int _requestGeneration;

        public static bool IsShown => _moduleRoot != null && _moduleRoot.activeSelf;

        public static void Toggle()
        {
            if (_wantShown || IsShown) { Close(); return; }
            Open();
        }

        public static void Open()
        {
            _wantShown = true;
            int generation = ++_requestGeneration;
            _ = OpenAsync(generation);
        }

        public static void Close()
        {
            _wantShown = false;
            _requestGeneration++;
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
        }

        private static async Task OpenAsync(int generation)
        {
            FestivalModel.Instance.ClearLoginRedDot();
            ActivityIconManager.Instance.SetIconRedDot(
                FestivalModel.ICON_TYPE,
                FestivalModel.Instance.GetEntranceOpenState() && FestivalModel.Instance.GetEntranceRedDot());

            if (_moduleRoot != null)
            {
                if (_wantShown && generation == _requestGeneration)
                {
                    _moduleRoot.SetActive(true);
                }
                return;
            }

            if (_loading) return;
            _loading = true;
            GameObject root = null;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            try
            {
                root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            }
            finally
            {
                _loading = false;
            }

            if (root == null)
            {
                GameLog.Error("Festival", "FestivalModule prefab load failed: {0}", key);
                if (_wantShown && generation != _requestGeneration)
                {
                    _ = OpenAsync(_requestGeneration);
                }
                return;
            }

            if (!_wantShown || generation != _requestGeneration)
            {
                ResManager.ReleaseInstance(root);
                if (_wantShown)
                {
                    _ = OpenAsync(_requestGeneration);
                }
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;
            _moduleRoot.SetActive(true);
            GameLog.Info("Festival", "宝录面板打开(容器级开关,子面板绑定留尾包): {0}", key);
        }

        internal static void Reset()
        {
            _wantShown = false;
            _requestGeneration++;
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
        }
    }
}
