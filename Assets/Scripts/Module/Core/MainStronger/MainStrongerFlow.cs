using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.MainStronger
{
    /// <summary>
    /// 158 路线与未来 MainUIStrongerView Prefab 的唯一装配点。
    /// 数字功能跳转由目标模块显式注册，不在这里猜测 OpenFun 映射。
    /// </summary>
    public static class MainStrongerFlow
    {
        private const string Module = "mainStronger";
        private const string Prefab = "MainUIStrongerView";

        private static readonly Dictionary<int, Action> FeatureOpeners
            = new Dictionary<int, Action>();
        private static Action _skillAwakeOpener;
        private static GameObject _root;
        private static MainUIStrongerView _view;
        private static bool _loading;

        public static bool IsOpen => _view != null && _view.IsShown;
        public static bool CanOpenSkillAwake => _skillAwakeOpener != null;

        public static void RegisterFeatureOpener(int func, Action opener)
        {
            if (func <= 0 || opener == null) return;
            FeatureOpeners[func] = opener;
            MainStrongerModel.Instance.Rebuild();
        }

        public static void UnregisterFeatureOpener(int func)
        {
            if (func <= 0) return;
            if (FeatureOpeners.Remove(func)) MainStrongerModel.Instance.Rebuild();
        }

        public static void RegisterSkillAwakeOpener(Action opener)
        {
            _skillAwakeOpener = opener;
            MainStrongerModel.Instance.Rebuild();
        }

        public static bool CanOpenFeature(int func)
            => func > 0 && FeatureOpeners.ContainsKey(func);

        public static void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_view != null && _view.IsShown) _view.Hide();
            ReleaseView();
        }

        internal static bool TryOpen(MainStrongerConfigs.Feature feature)
        {
            if (feature == null) return false;
            Action opener = feature.Id == 10001
                ? _skillAwakeOpener
                : FeatureOpeners.TryGetValue(feature.Func, out Action target) ? target : null;
            if (opener == null)
            {
                GameLog.Warn("MainStronger", "未注册功能跳转 feature={0} func={1}",
                    feature.Id, feature.Func);
                TipsManager.Toast("该变强功能尚未接入");
                return false;
            }
            try
            {
                Close();
                opener();
                return true;
            }
            catch (Exception e)
            {
                GameLog.Error("MainStronger", "功能跳转失败 feature={0} func={1}: {2}",
                    feature.Id, feature.Func, e.Message);
                TipsManager.Toast("功能打开失败");
                return false;
            }
        }

        private static async Task OpenAsync()
        {
            if (IsOpen || _loading) return;
            _loading = true;
            string key = GameResPath.GetUIPrefab(Module, Prefab);
            try
            {
                await Task.WhenAll(MainStrongerConfigs.EnsureLoaded(), FuncOpenConfig.EnsureLoaded());
                if (!FuncOpenConfig.CheckFuncOpenState("MainStronger"))
                {
                    TipsManager.Toast("我要变强功能尚未开放");
                    return;
                }
                if (!MainStrongerConfigs.IsLoaded)
                {
                    TipsManager.Toast("我要变强配置尚未就绪");
                    return;
                }

                _root = await ResManager.InstantiateAsync(key,
                    ViewManager.GetLayer(UILayer.Popup));
                if (_root == null)
                {
                    GameLog.Warn("MainStronger", "MainUIStrongerView Prefab 尚未转换 key={0}", key);
                    TipsManager.Toast("我要变强界面待转换");
                    return;
                }
                _root.name = Prefab;
                _view = _root.GetComponent<MainUIStrongerView>() ??
                    _root.GetComponentInChildren<MainUIStrongerView>(true);
                if (_view == null)
                {
                    GameLog.Error("MainStronger", "Prefab 缺少 MainUIStrongerView: {0}", key);
                    TipsManager.Toast("我要变强界面绑定缺失");
                    ReleaseView();
                    return;
                }
                MainStrongerModel.Instance.Rebuild();
                _view.Show();
            }
            catch (Exception e)
            {
                GameLog.Error("MainStronger", "打开失败 key={0}: {1}", key, e.Message);
                TipsManager.Toast("我要变强打开失败");
                ReleaseView();
            }
            finally
            {
                _loading = false;
            }
        }

        private static void ReleaseView()
        {
            if (_root != null) ResManager.ReleaseInstance(_root);
            _root = null;
            _view = null;
        }

        internal static void Reset()
        {
            Close();
            _loading = false;
            MainStrongerModel.Instance.Reset();
        }
    }
}
