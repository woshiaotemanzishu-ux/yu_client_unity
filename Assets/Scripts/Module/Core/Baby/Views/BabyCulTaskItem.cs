using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Baby;

namespace Shenxiao.Module.Core.Baby
{
    public partial class BabyCulTaskItem : BabyCulTaskItemBind
    {
        private bool _buttonsBound;
        private BabyRaiseConfigs.BabyRaiseTaskCfg _cfg;

        public int TaskId { get; private set; }

        public void SetData(BabyTaskInfo task, BabyRaiseConfigs.BabyRaiseTaskCfg cfg)
        {
            TaskId = task != null ? task.TaskId : 0;
            _cfg = cfg;
            BindButtons();

            if (taskDes != null) taskDes.text = task != null && cfg != null
                ? cfg.Desc + "(<color=" + (task.FinishNum >= cfg.NumCon ? "#0a953e" : "#ff4f50") + ">"
                    + task.FinishNum + "</color>/" + cfg.NumCon + ")"
                : string.Empty;
            if (rewardLb != null) rewardLb.text = cfg != null ? cfg.RaiseExp.ToString() : string.Empty;

            int state = task != null ? task.FinishState : -1;
            if (goBtn != null) goBtn.gameObject.SetActive(state == 0);
            if (getBtn != null) getBtn.gameObject.SetActive(state == 1);
            if (reddot != null) reddot.gameObject.SetActive(state == 1);
            if (finishImg != null) finishImg.gameObject.SetActive(state == 2);
        }

        private void BindButtons()
        {
            if (_buttonsBound) return;
            _buttonsBound = true;
            UIUtil.AddClick(getBtn, () => BabyController.Instance.RequestTaskReward(TaskId));
            UIUtil.AddClick(goBtn, () => GameLog.Info("Baby", "task jump pending: {0}", _cfg != null ? _cfg.JumpId : 0));
        }
    }
}
