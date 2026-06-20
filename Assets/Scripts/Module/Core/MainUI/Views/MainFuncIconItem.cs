using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面功能图标项(对标老客户端 MainFuncIconItem.ts):底部功能图标条/翻面里的一个功能入口图标
    /// (角色/背包/宠物…),带红点,点击切换到对应功能界面(SWITCH_MAIN_FUNC_VIEW)。
    ///
    /// 降级:MainUIModel(func 配置 / GetMainFuncRedState 红点)、SWITCH_MAIN_FUNC_VIEW + 各功能模块路由、
    /// REFRESH_FUNCTION_ICON_RED_DOT 事件未移植 → 用最小 DTO 填图标 + 点击打日志「待对接」;红点先隐藏,
    /// 由 SetRedState 控制。
    ///
    /// 注:本项是 MainUIDownView 内 _tpl_MainFuncIconItem 模板的业务脚本;它继承自 Bind,故 DownView 既有的
    /// GetComponent&lt;MainFuncIconItemBind&gt;(功能图标条铺设)用法不受影响(子类 is-a Bind)。
    /// </summary>
    public sealed class MainFuncIconItem : MainFuncIconItemBind
    {
        private FuncIconData _data;
        private bool _clickBound;

        /// <summary>填功能图标数据(对标 SetData + UpdateItem)。</summary>
        public void SetData(FuncIconData data)
        {
            _data = data;
            if (data == null) return;

            if (_img_icon != null && !string.IsNullOrEmpty(data.Res))
                _ = ResManager.SetImageAsync(_img_icon, GameResPath.GetIcon("mainUI", data.Res), nativeSize: false);

            SetRedState(false);  // 红点 GetMainFuncRedState 未移植 → 先隐藏

            if (!_clickBound && _img_icon != null)
            {
                _img_icon.raycastTarget = true;
                UIUtil.AddClick(_img_icon, OnClick);
                _clickBound = true;
            }
        }

        /// <summary>对标 SetRedState:红点显隐。</summary>
        public void SetRedState(bool show)
        {
            if (_img_red != null) _img_red.gameObject.SetActive(show);
        }

        private void OnClick()
        {
            // 对标 SWITCH_MAIN_FUNC_VIEW:经主界面功能入口中央路由打开目标面板(已移植则开,未移植则路由打日志)。
            string key = _data != null ? _data.Func : null;
            if (string.IsNullOrEmpty(key))
            {
                GameLog.Info("MainUI", "点击功能图标(无 func)→ 忽略");
                return;
            }
            MainUIRouter.Open(key);
        }
    }

    /// <summary>功能图标项最小数据(待 MainUIModel.MainFunc 配置移植后填充)。</summary>
    public sealed class FuncIconData
    {
        public string Res;
        public string Func;
    }
}
