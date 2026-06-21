using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary>
    /// VIP 模块编排:VipModule 合并 prefab 含 VipBaseView(VIP 主面板)+ RechargeView(充值)等。
    /// 顶部状态条两入口共用本 Flow:_box_vip → VIP 主面板,_box_recharge → 充值面板(老端 VipModel.OPEN_VIEW 'RechargeView')。
    /// 打开时实例化 VipModule(懒一次)→ 只 Show 请求的视图(隐其它顶层窗);各视图自带关闭按钮(close→Hide)。
    /// 入口注册见 <see cref="VipBootstrap"/>(MainUIRouter "vip"/"recharge")。
    /// </summary>
    public static class VipFlow
    {
        private const string MODULE = "vip";
        private const string PREFAB = "VipModule";

        private static GameObject _moduleRoot;
        private static bool _loading;

        public static void Open(string viewName) => _ = OpenAsync(viewName);

        private static async Task OpenAsync(string viewName)
        {
            if (_moduleRoot == null)
            {
                if (_loading) return;
                _loading = true;
                string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
                _moduleRoot = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
                _loading = false;
                if (_moduleRoot == null)
                {
                    GameLog.Error("Vip", "VipModule prefab load failed: {0}", key);
                    return;
                }
                _moduleRoot.name = PREFAB;
                foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true)) v.gameObject.SetActive(false);
            }

            BaseView target = null;
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewName) { target = v; break; }
            }
            if (target == null)
            {
                GameLog.Info("Vip", "VIP 子窗 [{0}] 未移植 View,待对接", viewName);
                return;
            }
            // 同模块两窗互斥:显请求窗前隐其它顶层 BaseView(子项除外)
            foreach (Transform c in _moduleRoot.transform)
            {
                BaseView bv = c.GetComponent<BaseView>();
                if (bv != null && bv != target) bv.Hide();
            }
            target.Show();
            GameLog.Info("Vip", "VIP 面板打开: {0}", viewName);
        }

        internal static void Reset()
        {
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _loading = false;
        }
    }
}
