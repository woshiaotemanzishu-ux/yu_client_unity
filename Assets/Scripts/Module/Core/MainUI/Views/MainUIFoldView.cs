using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面折叠协调器(太极)。从 MainUIActivityView 拆出、提到 MainUIModule 根层——因为它收放的不止活动区:
    /// 对标老端 CHANGE_ACTIVITY_STATE,活动图标 / 竞榜卡 / 次要图标 / 任务组队 都跟着折(顶栏 HudTop、底部技能/底栏不折)。
    /// 点击 → 翻转折叠态 → 广播 <see cref="GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD"/>;各区域视图各自监听、收放自己的内容。
    /// 本视图挂在根层、不属任何会折叠的区域,故折叠时太极自身始终可见、可再展开。
    /// </summary>
    public sealed class MainUIFoldView : MainUIFoldViewBind
    {
        private bool _folded;
        private bool _clickBound;

        protected override void OnShow(object args)
        {
            if (_clickBound) return;
            if (_img_turn != null)
            {
                _img_turn.raycastTarget = true; // 太极要能接收点击(对标老端 mouseEnabled)
                UIUtil.AddClick(_img_turn, Toggle);
            }
            _clickBound = true;
        }

        private void Toggle()
        {
            _folded = !_folded;
            // 箭头/太极原地旋转(对标老端只改 rotation);收放各区域交由各视图监听事件自理。
            if (_img_turn != null)
                _img_turn.rectTransform.localEulerAngles = new Vector3(0f, 0f, _folded ? 45f : 0f);
            EventDispatcher.Emit(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, _folded);
        }
    }
}
