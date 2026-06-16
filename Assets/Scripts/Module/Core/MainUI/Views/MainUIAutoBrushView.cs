using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.AutoBrush;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 自动闯关入口(对标老客户端 MainUIAutoBrushView.ts)。
    /// AutoBrushModel 尚未移植:红点(_img_red/_img_red2,对标 UpdateRed)与挑战特效盒(_box_effect)
    /// 在拿到数据前隐藏;闯关文案取老客户端非自动默认态「自动闯关」(UpdateAutoBrushStrangeState:state 为
    /// false 时 _lb_auto_level.text = "自动闯关")。位置/缩放/底图等转换产物保持不动。
    /// </summary>
    public sealed class MainUIAutoBrushView : MainUIAutoBrushViewBind
    {
        private float _progressWidth;

        protected override void OnInit()
        {
            _progressWidth = _img_progress.rectTransform.sizeDelta.x;
            _img_red.gameObject.SetActive(false);
            _img_red2.gameObject.SetActive(false);
            _box_effect.gameObject.SetActive(false);

            EventDispatcher.On(GlobalEvent.EVT_AUTOBRUSH_INFO_UPDATED, RefreshBrushInfo);
            EventDispatcher.On(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED, RefreshLevel);
            EventDispatcher.On(GlobalEvent.EVT_AUTOBRUSH_STATE_UPDATED, RefreshAutoState);
            RefreshAll();
        }

        protected override void OnShow(object args)
        {
            RefreshAll();
        }

        protected override void OnDispose()
        {
            UnbindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void UnbindEvents()
        {
            EventDispatcher.Off(GlobalEvent.EVT_AUTOBRUSH_INFO_UPDATED, RefreshBrushInfo);
            EventDispatcher.Off(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED, RefreshLevel);
            EventDispatcher.Off(GlobalEvent.EVT_AUTOBRUSH_STATE_UPDATED, RefreshAutoState);
        }

        private void RefreshAll()
        {
            RefreshAutoState();
            RefreshBrushInfo();
            RefreshLevel();
        }

        private void RefreshAutoState()
        {
            bool state = AutoBrushModel.Instance.AutoBrushState;
            _lb_auto_level.text = state ? "取消自动" : "自动闯关";
        }

        private void RefreshBrushInfo()
        {
            AutoBrushModel.BrushStrangeInfo info = AutoBrushModel.Instance.BrushInfo;
            if (info == null || info.NeedTimes <= 0)
            {
                SetProgressWidth(0f);
                _box_effect.gameObject.SetActive(false);
                return;
            }

            float percent = Mathf.Clamp01((float)info.CurrentTimes / info.NeedTimes);
            SetProgressWidth(_progressWidth * percent);
            _box_effect.gameObject.SetActive(info.CurrentTimes == info.NeedTimes);
        }

        private void RefreshLevel()
        {
            AutoBrushModel model = AutoBrushModel.Instance;
            if (model.CheckDoneState())
            {
                _lb_level.text = "";
                return;
            }

            _lb_level.text = "第" + (model.Level + 1) + "关";
        }

        private void SetProgressWidth(float width)
        {
            Vector2 size = _img_progress.rectTransform.sizeDelta;
            size.x = Mathf.Max(0f, width);
            _img_progress.rectTransform.sizeDelta = size;
        }
    }
}
