namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝系统 182xx 只读协议状态。</summary>
    public sealed class BabyModel
    {
        public static readonly BabyModel Instance = new BabyModel();

        public BabyErrorInfo LastError { get; private set; }
        public BabyBasicInfo Basic { get; private set; }
        public BabyRaiseInfo Raise { get; private set; }
        public BabyStageInfo Stage { get; private set; }
        public BabyEquipInfo Equip { get; private set; }
        public BabyFigureInfo Figures { get; private set; }
        public BabyFamilyInfo Family { get; private set; }

        private BabyModel() { }

        public void ApplyError(BabyErrorInfo value) => LastError = value;
        public void ApplyBasic(BabyBasicInfo value) => Basic = value;
        public void ApplyRaise(BabyRaiseInfo value) => Raise = value;
        public void ApplyStage(BabyStageInfo value) => Stage = value;
        public void ApplyEquip(BabyEquipInfo value) => Equip = value;
        public void ApplyFigures(BabyFigureInfo value) => Figures = value;
        public void ApplyFamily(BabyFamilyInfo value) => Family = value;

        public bool TryApplyTaskProgress(int taskId, int finishNum, int finishState)
        {
            if (Raise == null) return false;
            for (int i = 0; i < Raise.TaskList.Count; i++)
            {
                BabyTaskInfo task = Raise.TaskList[i];
                if (task.TaskId != taskId) continue;
                task.FinishNum = finishNum;
                task.FinishState = finishState;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            LastError = null;
            Basic = null;
            Raise = null;
            Stage = null;
            Equip = null;
            Figures = null;
            Family = null;
        }
    }
}
