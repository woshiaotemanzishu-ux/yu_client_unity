using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝已激活后的培养页静态基线；后续培养数据在独立轮次接入。</summary>
    public partial class BabyCultivateView : BabyCultivateViewBind
    {
        private bool _listening;

        protected override void OnInit()
        {
            UIUtil.AddClick(lvBtnGp, () => SelectPage(0));
            UIUtil.AddClick(stageBtnGp, () => SelectPage(1));
            UIUtil.AddClick(upBtn, () => BabyController.Instance.RequestStageUp());
            SelectPage(0);
        }

        protected override void OnShow(object args)
        {
            if (!_listening)
            {
                EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
                _listening = true;
            }
            Refresh();
        }

        protected override void OnHide()
        {
            StopListening();
        }

        protected override void OnDispose()
        {
            StopListening();
        }

        private void OnBabyUpdate(int command)
        {
            Refresh();
        }

        private void Refresh()
        {
            BabyModel model = BabyModel.Instance;
            babyName.text = model.Basic != null ? model.Basic.BabyName ?? string.Empty : string.Empty;
            lvLb.text = model.Raise != null ? model.Raise.RaiseLevel.ToString() : string.Empty;
            lvExpLb.text = model.Raise != null ? model.Raise.RaiseExp.ToString() : string.Empty;
            stageExpLb.text = model.Stage != null ? model.Stage.StageExp.ToString() : string.Empty;
        }

        private void SelectPage(int index)
        {
            if (lvGp != null) lvGp.gameObject.SetActive(index == 0);
            if (stageGp != null) stageGp.gameObject.SetActive(index == 1);
        }

        private void StopListening()
        {
            if (!_listening) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
            _listening = false;
        }
    }
}
