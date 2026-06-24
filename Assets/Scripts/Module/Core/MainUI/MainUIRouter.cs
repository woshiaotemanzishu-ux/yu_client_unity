using System;
using System.Collections.Generic;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面功能入口中央路由（对标老客户端 OpenFun.OpenFunHandler / SwitchView）。
    ///
    /// 设计动机：主界面有大量按钮（角色/背包/装备/公会/好友/活动…），它们的目标面板会随移植进度
    /// 陆续到位。让按钮**不直接依赖具体 View**，而是统一调 <see cref="Open"/>(viewKey)：
    ///   - 目标模块已移植 → 它在自己的 Bootstrap 里 <see cref="Register"/>(viewKey, opener)，点击即打开；
    ///   - 尚未移植 → 打日志标注"待对接 {viewKey}"，按钮可点不崩（优雅降级，对标 SOP §4）。
    ///
    /// 这样按钮点击的"对接"是渐进的：移植一个模块就注册一个 opener，按钮自动生效，无需回头改主界面。
    /// </summary>
    public static class MainUIRouter
    {
        private static readonly Dictionary<string, Action> _openers = new Dictionary<string, Action>();

        /// <summary>模块移植后在其 Bootstrap/Flow 里登记打开器(viewKey 用功能视图类名或功能短名,如 "BagView"/"role")。</summary>
        public static void Register(string viewKey, Action opener)
        {
            if (string.IsNullOrEmpty(viewKey) || opener == null) return;
            _openers[viewKey] = opener;
        }

        public static void Unregister(string viewKey)
        {
            if (string.IsNullOrEmpty(viewKey)) return;
            _openers.Remove(viewKey);
        }

        public static bool IsRegistered(string viewKey)
        {
            return !string.IsNullOrEmpty(viewKey) && _openers.ContainsKey(viewKey);
        }

        /// <summary>按钮点击统一入口:已注册则打开,否则日志标注待对接(不崩)。</summary>
        public static void Open(string viewKey)
        {
            if (string.IsNullOrEmpty(viewKey)) return;
            if (_openers.TryGetValue(viewKey, out Action open) && open != null)
            {
                try
                {
                    open();
                }
                catch (Exception e)
                {
                    GameLog.Error("MainUI", "open route [{0}] failed: {1}", viewKey, e.Message);
                    MainUIRoutePlaceholder.Show(viewKey);
                }
                return;
            }
            GameLog.Info("MainUI", "点击功能入口 [{0}] → 目标面板未移植,待对接", viewKey);
            MainUIRoutePlaceholder.Show(viewKey);
        }

        /// <summary>切场景/登出清理:不清注册表(模块生命周期各自管),仅供需要时整体重置。</summary>
        public static void ClearAll()
        {
            _openers.Clear();
        }
    }
}
