using Shenxiao.Module.Core.Bag;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝系统 182xx 数据状态与最近一次操作结果。</summary>
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
        public BabyActivateResult LastActivateResult { get; private set; }
        public BabyStageUpResult LastStageUpResult { get; private set; }
        public BabyFigureStarResult LastFigureStarResult { get; private set; }
        public BabyFigureWearResult LastFigureWearResult { get; private set; }
        public BabyRenameResult LastRenameResult { get; private set; }
        public BabyTaskRewardResult LastTaskRewardResult { get; private set; }
        public BabyFigurePowerResult LastFigurePowerResult { get; private set; }

        private BabyModel() { }

        public void ApplyError(BabyErrorInfo value) => LastError = value;
        public void ApplyBasic(BabyBasicInfo value) => Basic = value;
        public void ApplyRaise(BabyRaiseInfo value) => Raise = value;
        public void ApplyStage(BabyStageInfo value) => Stage = value;
        public void ApplyEquip(BabyEquipInfo value) => Equip = value;
        public void ApplyFigures(BabyFigureInfo value)
        {
            for (int i = 0; i < value.ActiveList.Count; i++)
            {
                BabyFigureEntry incoming = value.ActiveList[i];
                incoming.IsActivated = true;
                BabyFigureEntry previous = FindFigure(incoming.BabyId);
                if (previous == null) continue;
                incoming.Power = previous.Power;
                incoming.NextPower = previous.NextPower;
            }
            Figures = value;
        }
        public void ApplyFamily(BabyFamilyInfo value) => Family = value;
        public void ApplyActivateResult(BabyActivateResult value) => LastActivateResult = value;

        public void ApplyStageUpResult(BabyStageUpResult value)
        {
            LastStageUpResult = value;
            if (!value.Succeeded) return;
            Stage = new BabyStageInfo
            {
                Stage = value.Stage,
                StageLevel = value.StageLevel,
                StageExp = value.StageExp,
                Power = value.Power
            };
        }

        public void ApplyFigureStarResult(BabyFigureStarResult value) => LastFigureStarResult = value;

        public void ApplyFigureWearResult(BabyFigureWearResult value)
        {
            LastFigureWearResult = value;
            if (!value.Succeeded || Basic == null) return;
            Basic.BabyId = value.Type == 2 ? 0 : value.BabyId;
        }

        public void ApplyRenameResult(BabyRenameResult value)
        {
            LastRenameResult = value;
            if (!value.Succeeded || Basic == null) return;
            Basic.BabyName = value.Name;
            Basic.IsChangeName = true;
        }

        public void ApplyTaskRewardResult(BabyTaskRewardResult value) => LastTaskRewardResult = value;

        public void ApplyFigurePowerResult(BabyFigurePowerResult value)
        {
            BabyFigureEntry entry = FindFigure(value.BabyId);
            value.IsActivated = entry != null;
            LastFigurePowerResult = value;
            if (entry == null) return;
            entry.Power = value.Power;
            entry.NextPower = value.NextPower;
        }

        public bool HasAnyActivatedFigure()
        {
            if (Figures == null) return false;
            for (int i = 0; i < Figures.ActiveList.Count; i++)
            {
                if (Figures.ActiveList[i].IsActivated) return true;
            }
            return false;
        }

        public bool MergeFigure(int babyId, int babyStar, long power, long nextPower)
        {
            BabyFigureEntry entry = FindFigure(babyId);
            bool added = entry == null;
            if (added)
            {
                if (Figures == null) Figures = new BabyFigureInfo();
                entry = new BabyFigureEntry { BabyId = babyId };
                Figures.ActiveList.Add(entry);
            }
            entry.BabyStar = babyStar;
            entry.Power = power;
            entry.NextPower = nextPower;
            entry.IsActivated = true;
            return added;
        }

        public BabyFigureEntry FindFigure(int babyId)
        {
            if (Figures == null) return null;
            for (int i = 0; i < Figures.ActiveList.Count; i++)
            {
                BabyFigureEntry entry = Figures.ActiveList[i];
                if (entry.BabyId == babyId) return entry;
            }
            return null;
        }

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

        public bool HasClaimableRaiseTask()
        {
            if (Raise == null) return false;
            for (int i = 0; i < Raise.TaskList.Count; i++)
                if (Raise.TaskList[i].FinishState == 1) return true;
            return false;
        }

        public bool HasStageUpgradeRed()
        {
            if (Raise == null || Stage == null || !BabyValueConfigs.IsLoaded || !BabyStageConfigs.IsLoaded
                || Raise.RaiseLevel < BabyValueConfigs.StageRaiseLevel) return false;
            BabyStageConfigs.StageCfg next = BabyStageConfigs.GetNext(Stage.Stage, Stage.StageLevel);
            if (next == null) return false;
            long needExp = (long)next.ExpCon - Stage.StageExp;
            if (needExp <= 0) return true;
            for (int i = 0; i < BabyValueConfigs.StageMaterials.Count; i++)
            {
                BabyValueConfigs.StageMaterial material = BabyValueConfigs.StageMaterials[i];
                if (material == null || material.ItemId <= 0 || material.ExpPerItem <= 0) continue;
                long count = BagModel.Instance.GetTypeGoodsNum(material.ItemId);
                if (count <= 0) continue;
                long requiredCount = (needExp + material.ExpPerItem - 1) / material.ExpPerItem;
                if (count >= requiredCount) return true;
                needExp -= count * material.ExpPerItem;
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
            LastActivateResult = null;
            LastStageUpResult = null;
            LastFigureStarResult = null;
            LastFigureWearResult = null;
            LastRenameResult = null;
            LastTaskRewardResult = null;
            LastFigurePowerResult = null;
        }
    }
}
